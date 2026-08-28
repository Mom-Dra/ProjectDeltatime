using System;
using Deltatime.Player;
using Deltatime.Visuals;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Deltatime.EditorTools
{
    /// <summary>
    /// Keeps the serialized player visual in every playable scene aligned with
    /// the main-character source prefab. Gameplay remains on the Player root;
    /// this tool replaces only its visual child and rebinds animation and held
    /// weapon presentation to the new humanoid skeleton.
    /// </summary>
    public static class PlayerCharacterModelEditorSetup
    {
        public const string BusinessMaleCharacterPath =
            "Assets/Synty/PolygonGeneric/Prefabs/Characters/" +
            "SM_Gen_Chr_Business_Male_01.prefab";

        private static readonly string[] PlayableScenePaths =
        {
            GameBuildSceneCatalog.TutorialScenePath,
            "Assets/_Project/Scenes/Stage1.unity",
            "Assets/_Project/Scenes/Stage2.unity",
            "Assets/_Project/Scenes/Stage3.unity",
            "Assets/_Project/Scenes/Stage4.unity",
            "Assets/_Project/Scenes/Stage5.unity",
            "Assets/_Project/Scenes/Stage6.unity"
        };

        [MenuItem("Deltatime/Characters/Apply Business Male Player Model")]
        public static void ApplyBusinessMalePlayerModel()
        {
            GameObject source = LoadBusinessMaleSource();
            for (int i = 0; i < PlayableScenePaths.Length; i++)
            {
                string scenePath = PlayableScenePaths[i];
                Require(AssetDatabase.LoadAssetAtPath<SceneAsset>(scenePath) != null,
                    $"Playable scene is missing: {scenePath}");

                Scene scene = EditorSceneManager.OpenScene(
                    scenePath,
                    OpenSceneMode.Single);
                ReplacePlayerVisual(scene, source);
                EditorSceneManager.MarkSceneDirty(scene);
                Require(EditorSceneManager.SaveScene(scene, scenePath),
                    $"Failed to save player model change: {scenePath}");
                ValidatePlayerVisual(scene, source);
            }

            AssetDatabase.SaveAssets();
            Debug.Log(
                "Business Male player model applied and validated in Tutorial and Stages 1-6.");
        }

        public static void ApplyBusinessMalePlayerModelFromCommandLine()
        {
            ApplyBusinessMalePlayerModel();
        }

        [MenuItem("Deltatime/Characters/Validate Business Male Player Model")]
        public static void ValidateBusinessMalePlayerModel()
        {
            GameObject source = LoadBusinessMaleSource();
            for (int i = 0; i < PlayableScenePaths.Length; i++)
            {
                Scene scene = EditorSceneManager.OpenScene(
                    PlayableScenePaths[i],
                    OpenSceneMode.Single);
                ValidatePlayerVisual(scene, source);
            }

            Debug.Log(
                "Business Male player model validation passed in Tutorial and Stages 1-6.");
        }

        public static void ValidateBusinessMalePlayerModelFromCommandLine()
        {
            ValidateBusinessMalePlayerModel();
        }

        private static GameObject LoadBusinessMaleSource()
        {
            GameObject source = AssetDatabase.LoadAssetAtPath<GameObject>(
                BusinessMaleCharacterPath);
            Require(source != null,
                $"Main player character prefab is missing: {BusinessMaleCharacterPath}");

            Animator animator = source.GetComponentInChildren<Animator>(true);
            Require(animator != null && animator.avatar != null &&
                    animator.avatar.isValid && animator.isHuman,
                "Business Male player character requires a valid Humanoid Animator avatar.");
            return source;
        }

        private static void ReplacePlayerVisual(Scene scene, GameObject source)
        {
            PlayerHealth player = FindPlayer(scene);
            GameObject owner = player.gameObject;
            CharacterVisualController visualController =
                owner.GetComponent<CharacterVisualController>();
            Transform previousVisual = visualController == null
                ? FindAnimatedVisual(owner.transform)
                : visualController.VisualRoot;
            VisualTransformState transformState = new VisualTransformState(
                previousVisual,
                "Player Character - Business Male");

            if (previousVisual != null)
            {
                UnityEngine.Object.DestroyImmediate(previousVisual.gameObject);
            }

            GameObject visual = PrefabUtility.InstantiatePrefab(source, scene)
                as GameObject;
            Require(visual != null,
                $"Failed to instantiate {BusinessMaleCharacterPath} in {scene.path}.");
            visual.name = transformState.Name;
            visual.transform.SetParent(owner.transform, false);
            visual.transform.localPosition = transformState.LocalPosition;
            visual.transform.localRotation = transformState.LocalRotation;
            visual.transform.localScale = transformState.LocalScale;

            DisableVisualPhysics(visual);
            ConfigureVisualSystems(owner, player, visual);
            EditorUtility.SetDirty(owner);
        }

        private static void ConfigureVisualSystems(
            GameObject owner,
            PlayerHealth player,
            GameObject visual)
        {
            Renderer proxyRenderer = owner.GetComponent<Renderer>();
            if (proxyRenderer != null)
            {
                proxyRenderer.shadowCastingMode =
                    UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly;
                EditorUtility.SetDirty(proxyRenderer);
            }

            Require(CharacterAnimationEditorSetup.ConfigureCharacter(owner, visual),
                $"Failed to configure the Business Male Animator for {owner.name}.");

            CharacterVisualController visualController =
                owner.GetComponent<CharacterVisualController>();
            if (visualController == null)
            {
                visualController = owner.AddComponent<CharacterVisualController>();
            }

            visualController.Configure(visual.transform);
            player.ConfigureVisual(visualController);
            owner.GetComponent<WeaponVisualPresenter>()?.RefreshVisual();
            EditorUtility.SetDirty(visualController);
        }

        private static void DisableVisualPhysics(GameObject visual)
        {
            Collider[] colliders = visual.GetComponentsInChildren<Collider>(true);
            for (int i = 0; i < colliders.Length; i++)
            {
                colliders[i].enabled = false;
                EditorUtility.SetDirty(colliders[i]);
            }

            Rigidbody[] bodies = visual.GetComponentsInChildren<Rigidbody>(true);
            for (int i = 0; i < bodies.Length; i++)
            {
                bodies[i].isKinematic = true;
                bodies[i].detectCollisions = false;
                EditorUtility.SetDirty(bodies[i]);
            }
        }

        private static void ValidatePlayerVisual(Scene scene, GameObject source)
        {
            PlayerHealth player = FindPlayer(scene);
            CharacterVisualController visualController =
                player.GetComponent<CharacterVisualController>();
            Transform visual = visualController == null
                ? null
                : visualController.VisualRoot;
            Require(visual != null,
                $"Player visual root is missing in {scene.path}.");
            Require(PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(
                        visual.gameObject) == BusinessMaleCharacterPath,
                $"Player visual in {scene.path} does not use {source.name}.");

            Animator animator = visual.GetComponentInChildren<Animator>(true);
            CharacterAnimationController driver =
                player.GetComponent<CharacterAnimationController>();
            Require(animator != null && animator.enabled &&
                    animator.avatar != null && animator.avatar.isValid &&
                    animator.isHuman && !animator.applyRootMotion &&
                    animator.updateMode == AnimatorUpdateMode.UnscaledTime &&
                    animator.runtimeAnimatorController != null,
                $"Player Animator is incomplete in {scene.path}.");
            Require(driver != null && driver.Animator == animator,
                $"Player animation driver is not bound to the Business Male Animator in {scene.path}.");
            Require(visual.GetComponentsInChildren<Renderer>(true).Length > 0,
                $"Player visual has no renderers in {scene.path}.");

            Collider[] colliders = visual.GetComponentsInChildren<Collider>(true);
            for (int i = 0; i < colliders.Length; i++)
            {
                Require(!colliders[i].enabled,
                    $"Player visual collider remains enabled in {scene.path}: {colliders[i].name}");
            }
        }

        private static PlayerHealth FindPlayer(Scene scene)
        {
            PlayerHealth result = null;
            GameObject[] roots = scene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                PlayerHealth[] candidates = roots[i].GetComponentsInChildren<PlayerHealth>(true);
                for (int j = 0; j < candidates.Length; j++)
                {
                    Require(result == null,
                        $"Expected exactly one player in {scene.path}.");
                    result = candidates[j];
                }
            }

            Require(result != null, $"Player is missing in {scene.path}.");
            return result;
        }

        private static Transform FindAnimatedVisual(Transform owner)
        {
            Animator[] animators = owner.GetComponentsInChildren<Animator>(true);
            for (int i = 0; i < animators.Length; i++)
            {
                if (animators[i].transform != owner)
                {
                    return animators[i].transform;
                }
            }

            return null;
        }

        private static void Require(bool condition, string message)
        {
            if (!condition)
            {
                throw new InvalidOperationException(message);
            }
        }

        private readonly struct VisualTransformState
        {
            public readonly string Name;
            public readonly Vector3 LocalPosition;
            public readonly Quaternion LocalRotation;
            public readonly Vector3 LocalScale;

            public VisualTransformState(Transform source, string fallbackName)
            {
                Name = source == null ? fallbackName : source.name;
                LocalPosition = source == null
                    ? new Vector3(0f, -1f, 0f)
                    : source.localPosition;
                LocalRotation = source == null
                    ? Quaternion.identity
                    : source.localRotation;
                LocalScale = source == null
                    ? Vector3.one
                    : source.localScale;
            }
        }
    }
}
