using System;
using System.Collections.Generic;
using Deltatime.Combat;
using Deltatime.Enemies;
using Deltatime.Level;
using Deltatime.Player;
using Deltatime.Replay;
using Deltatime.TimeSystem;
using Deltatime.UI;
using Deltatime.Vision;
using Deltatime.Visuals;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Deltatime.EditorTools
{
    /// <summary>
    /// Creates a safe, enemy-free scene for tuning player weapon visuals.
    /// The source Stage1 scene is never modified by this builder.
    /// </summary>
    public static class WeaponCalibrationSceneBuilder
    {
        private const string Stage1ScenePath = "Assets/_Project/Scenes/Stage1.unity";
        private const string CalibrationScenePath =
            "Assets/_Project/Scenes/WeaponCalibration.unity";

        [MenuItem("Tools/Prototype/Animation/Build Weapon Calibration Scene")]
        public static void BuildWeaponCalibrationScene()
        {
            Require(
                AssetDatabase.LoadAssetAtPath<SceneAsset>(Stage1ScenePath) != null,
                "Stage1 scene is required to build WeaponCalibration.");

            if (!Application.isBatchMode &&
                !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            Scene scene = EditorSceneManager.OpenScene(
                Stage1ScenePath,
                OpenSceneMode.Single);
            if (!EditorSceneManager.SaveScene(scene, CalibrationScenePath))
            {
                throw new InvalidOperationException(
                    $"Failed to create {CalibrationScenePath} from Stage1.");
            }

            RemoveCombatPressure(scene);
            ConfigureForWeaponCalibration(scene);

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene))
            {
                throw new InvalidOperationException(
                    $"Failed to save {CalibrationScenePath}.");
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            ValidateCalibrationScene(scene);
            SelectPlayer(scene);

            if (!Application.isBatchMode)
            {
                WeaponModelCalibrationWindow.Open();
            }

            Debug.Log(
                "WeaponCalibration built and validated successfully: " +
                "Stage1 player/camera/room retained; enemies, stage flow, replay, " +
                "and legacy HUD removed.");
        }

        [MenuItem("Tools/Prototype/Animation/Open Weapon Calibration Scene")]
        public static void OpenWeaponCalibrationScene()
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(CalibrationScenePath) == null)
            {
                BuildWeaponCalibrationScene();
                return;
            }

            if (!Application.isBatchMode &&
                !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            Scene scene = EditorSceneManager.OpenScene(
                CalibrationScenePath,
                OpenSceneMode.Single);
            ValidateCalibrationScene(scene);
            SelectPlayer(scene);

            if (!Application.isBatchMode)
            {
                WeaponModelCalibrationWindow.Open();
            }
        }

        public static void BuildAndValidateFromCommandLine()
        {
            try
            {
                BuildWeaponCalibrationScene();
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                EditorApplication.Exit(1);
            }
        }

        public static void ValidateFromCommandLine()
        {
            try
            {
                Scene scene = EditorSceneManager.OpenScene(
                    CalibrationScenePath,
                    OpenSceneMode.Single);
                ValidateCalibrationScene(scene);
                Debug.Log("WeaponCalibration static validation passed.");
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                EditorApplication.Exit(1);
            }
        }

        private static void RemoveCombatPressure(Scene scene)
        {
            DestroyEnemyObjects<EnemyBehavior>(scene);
            DestroyEnemyObjects<EnemyHealth>(scene);

            VisionCone[] visionCones = FindSceneComponents<VisionCone>(scene);
            for (int i = 0; i < visionCones.Length; i++)
            {
                visionCones[i].SetUnlimitedVision(true);
            }

            GameHud[] legacyHuds = FindSceneComponents<GameHud>(scene);
            for (int i = 0; i < legacyHuds.Length; i++)
            {
                if (legacyHuds[i] != null)
                {
                    UnityEngine.Object.DestroyImmediate(legacyHuds[i].gameObject);
                }
            }

            DestroyComponents<StageController>(scene);
            DestroyComponents<StageReplayController>(scene);
        }

        private static void ConfigureForWeaponCalibration(Scene scene)
        {
            PlayerHealth player = FindSceneComponent<PlayerHealth>(scene);
            Require(player != null,
                "WeaponCalibration requires the Stage1 PlayerHealth component.");

            WeaponController weapon = player.GetComponent<WeaponController>();
            Require(weapon != null && weapon.StartingDefinition != null,
                "WeaponCalibration player requires a configured starting weapon.");

            CharacterAnimationController animation =
                player.GetComponent<CharacterAnimationController>();
            Require(animation != null,
                "WeaponCalibration player requires CharacterAnimationController.");
        }

        private static void ValidateCalibrationScene(Scene scene)
        {
            PlayerHealth[] players = FindSceneComponents<PlayerHealth>(scene);
            WeaponController playerWeapon = players.Length == 1
                ? players[0].GetComponent<WeaponController>()
                : null;
            WorldTimeController[] worldTimes =
                FindSceneComponents<WorldTimeController>(scene);
            Camera[] cameras = FindSceneComponents<Camera>(scene);
            VisionCone[] visionCones = FindSceneComponents<VisionCone>(scene);

            Require(scene.path == CalibrationScenePath,
                $"Validated scene path is {scene.path}, expected {CalibrationScenePath}.");
            Require(players.Length == 1 && playerWeapon != null &&
                    playerWeapon.StartingDefinition != null,
                "WeaponCalibration requires exactly one armed player.");
            Require(worldTimes.Length == 1 && cameras.Length == 1,
                "WeaponCalibration requires exactly one world-time controller and camera.");
            Require(visionCones.Length == 1 && visionCones[0].HasUnlimitedVision,
                "WeaponCalibration must use unlimited vision without replay lighting.");
            Require(FindSceneComponents<EnemyBehavior>(scene).Length == 0 &&
                    FindSceneComponents<EnemyHealth>(scene).Length == 0,
                "WeaponCalibration must not contain enemies.");
            Require(FindSceneComponents<StageController>(scene).Length == 0 &&
                    FindSceneComponents<StageReplayController>(scene).Length == 0 &&
                    FindSceneComponents<GameHud>(scene).Length == 0,
                "WeaponCalibration must not contain stage completion, replay, or GameHud.");
            Require(FindSceneComponents<CharacterAnimationController>(scene).Length == 1,
                "WeaponCalibration player requires one character animation controller.");
        }

        private static void SelectPlayer(Scene scene)
        {
            PlayerHealth player = FindSceneComponent<PlayerHealth>(scene);
            if (player != null)
            {
                Selection.activeGameObject = player.gameObject;
            }
        }

        private static void DestroyEnemyObjects<T>(Scene scene)
            where T : Component
        {
            T[] values = FindSceneComponents<T>(scene);
            HashSet<GameObject> objects = new HashSet<GameObject>();
            for (int i = 0; i < values.Length; i++)
            {
                if (values[i] != null)
                {
                    objects.Add(values[i].gameObject);
                }
            }

            foreach (GameObject gameObject in objects)
            {
                if (gameObject != null)
                {
                    UnityEngine.Object.DestroyImmediate(gameObject);
                }
            }
        }

        private static void DestroyComponents<T>(Scene scene)
            where T : Component
        {
            T[] values = FindSceneComponents<T>(scene);
            for (int i = 0; i < values.Length; i++)
            {
                if (values[i] != null)
                {
                    UnityEngine.Object.DestroyImmediate(values[i]);
                }
            }
        }

        private static T FindSceneComponent<T>(Scene scene)
            where T : Component
        {
            T[] values = FindSceneComponents<T>(scene);
            return values.Length == 0 ? null : values[0];
        }

        private static T[] FindSceneComponents<T>(Scene scene)
            where T : Component
        {
            List<T> values = new List<T>();
            GameObject[] roots = scene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                values.AddRange(roots[i].GetComponentsInChildren<T>(true));
            }

            return values.ToArray();
        }

        private static void Require(bool condition, string message)
        {
            if (!condition)
            {
                throw new InvalidOperationException(message);
            }
        }
    }
}
