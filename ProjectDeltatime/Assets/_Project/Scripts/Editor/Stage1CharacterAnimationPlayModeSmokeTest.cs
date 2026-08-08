using System;
using Deltatime.Combat;
using Deltatime.TimeSystem;
using Deltatime.Visuals;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Deltatime.EditorTools
{
    [InitializeOnLoad]
    public static class Stage1CharacterAnimationPlayModeSmokeTest
    {
        private const string ScenePath =
            "Assets/_Project/Scenes/Stage1.unity";
        private const string PistolPath = "Assets/_Project/Pistol.asset";
        private const string RiflePath =
            "Assets/_Project/AutomaticRifle.asset";
        private const string ShotgunPath = "Assets/_Project/Shotgun.asset";
        private const string MeleePath = "Assets/_Project/MeleeWeapon.asset";
        private const string RunningKey =
            "Deltatime.Stage1CharacterAnimationSmoke.Running";
        private const string FailedKey =
            "Deltatime.Stage1CharacterAnimationSmoke.Failed";
        private const string FailureKey =
            "Deltatime.Stage1CharacterAnimationSmoke.Failure";
        private const string PhaseKey =
            "Deltatime.Stage1CharacterAnimationSmoke.Phase";

        private static bool callbacksAttached;
        private static bool validationRan;
        private static double playStartedAt;

        static Stage1CharacterAnimationPlayModeSmokeTest()
        {
            if (SessionState.GetBool(RunningKey, false))
            {
                AttachCallbacks();
            }
        }

        public static void RunFromCommandLine()
        {
            SessionState.SetBool(RunningKey, true);
            SessionState.SetBool(FailedKey, false);
            SessionState.SetString(FailureKey, string.Empty);
            SessionState.SetString(PhaseKey, "entering");
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            AttachCallbacks();
            EditorApplication.isPlaying = true;
        }

        private static void AttachCallbacks()
        {
            if (callbacksAttached)
            {
                return;
            }

            callbacksAttached = true;
            EditorApplication.update += Tick;
            EditorApplication.playModeStateChanged += HandlePlayModeStateChanged;
            Application.logMessageReceived += HandleLog;
        }

        private static void DetachCallbacks()
        {
            if (!callbacksAttached)
            {
                return;
            }

            callbacksAttached = false;
            EditorApplication.update -= Tick;
            EditorApplication.playModeStateChanged -= HandlePlayModeStateChanged;
            Application.logMessageReceived -= HandleLog;
        }

        private static void HandlePlayModeStateChanged(PlayModeStateChange state)
        {
            if (!SessionState.GetBool(RunningKey, false))
            {
                return;
            }

            if (state == PlayModeStateChange.EnteredPlayMode)
            {
                playStartedAt = EditorApplication.timeSinceStartup;
                validationRan = false;
                SessionState.SetString(PhaseKey, "playing");
            }
            else if (state == PlayModeStateChange.ExitingPlayMode)
            {
                SessionState.SetString(PhaseKey, "stopping");
            }
            else if (state == PlayModeStateChange.EnteredEditMode &&
                     SessionState.GetString(PhaseKey, string.Empty) ==
                         "stopping")
            {
                Finish();
            }
        }

        private static void Tick()
        {
            if (!SessionState.GetBool(RunningKey, false))
            {
                DetachCallbacks();
                return;
            }

            if (!EditorApplication.isPlaying)
            {
                return;
            }

            double elapsed = EditorApplication.timeSinceStartup - playStartedAt;
            if (!validationRan && elapsed >= 0.8d)
            {
                validationRan = true;
                try
                {
                    ValidateRuntimeState();
                }
                catch (Exception exception)
                {
                    RecordFailure(exception.ToString());
                }

                EditorApplication.isPlaying = false;
            }
            else if (elapsed >= 15d)
            {
                RecordFailure(
                    "Stage1 character animation play-mode smoke test timed out.");
                EditorApplication.isPlaying = false;
            }
        }

        private static void ValidateRuntimeState()
        {
            Require(
                SceneManager.GetActiveScene().path == ScenePath,
                $"Unexpected scene: {SceneManager.GetActiveScene().path}");

            CharacterAnimationController[] controllers =
                UnityEngine.Object.FindObjectsByType<CharacterAnimationController>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None);
            Require(
                controllers.Length == 4,
                $"Stage1 initialized {controllers.Length} animated actors instead of 4.");

            CharacterAnimationLibrary library =
                AssetDatabase.LoadAssetAtPath<CharacterAnimationLibrary>(
                    CharacterAnimationEditorSetup.LibraryPath);
            WorldTimeController worldTime =
                UnityEngine.Object.FindFirstObjectByType<WorldTimeController>();
            Require(library != null, "Character animation library is missing.");
            Require(worldTime != null, "Stage1 world time is missing.");

            for (int i = 0; i < controllers.Length; i++)
            {
                CharacterAnimationController controller = controllers[i];
                Animator animator = controller.Animator;
                Require(
                    animator != null &&
                    animator.enabled &&
                    animator.isInitialized &&
                    animator.avatar != null &&
                    animator.avatar.isHuman &&
                    animator.avatar.isValid &&
                    !animator.applyRootMotion &&
                    animator.updateMode == AnimatorUpdateMode.UnscaledTime &&
                    animator.runtimeAnimatorController != null &&
                    animator.runtimeAnimatorController.animationClips.Length >= 7,
                    $"Stage1 Animator is not runtime-ready on {controller.name}.");
                Require(
                    HasParameter(animator, "MoveX") &&
                    HasParameter(animator, "MoveY") &&
                    HasParameter(animator, "Roll") &&
                    HasParameter(animator, "AttackA") &&
                    HasParameter(animator, "AttackB"),
                    $"Stage1 Animator parameters are incomplete on {controller.name}.");
                if (controller.IsEnemy)
                {
                    Require(
                        Mathf.Approximately(
                            animator.speed,
                            worldTime.CurrentTimeScale),
                        $"Enemy animation time scale is incorrect on {controller.name}.");
                }
            }

            GameObject player = GameObject.Find("Player");
            CharacterAnimationController playerAnimation =
                player == null
                    ? null
                    : player.GetComponent<CharacterAnimationController>();
            WeaponController playerWeapon =
                player == null ? null : player.GetComponent<WeaponController>();
            Require(
                playerAnimation != null && playerWeapon != null,
                "Stage1 player animation or weapon controller is missing.");

            WeaponDefinition pistol = LoadWeapon(PistolPath);
            WeaponDefinition rifle = LoadWeapon(RiflePath);
            WeaponDefinition shotgun = LoadWeapon(ShotgunPath);
            WeaponDefinition melee = LoadWeapon(MeleePath);
            WeaponDefinition originalDefinition = playerWeapon.Definition;
            int originalAmmunition = playerWeapon.Ammunition;

            RequireProfile(
                playerWeapon,
                playerAnimation,
                library,
                null,
                CharacterAnimationStyle.Unarmed);
            RequireProfile(
                playerWeapon,
                playerAnimation,
                library,
                pistol,
                CharacterAnimationStyle.Pistol);
            RequireProfile(
                playerWeapon,
                playerAnimation,
                library,
                rifle,
                CharacterAnimationStyle.Rifle);
            RequireProfile(
                playerWeapon,
                playerAnimation,
                library,
                shotgun,
                CharacterAnimationStyle.Rifle);
            RequireProfile(
                playerWeapon,
                playerAnimation,
                library,
                melee,
                CharacterAnimationStyle.Melee);

            playerWeapon.Equip(originalDefinition, originalAmmunition);
            Require(
                playerAnimation.CurrentStyle == CharacterAnimationStyle.Pistol,
                "Stage1 player did not restore the starting pistol profile.");
        }

        private static WeaponDefinition LoadWeapon(string path)
        {
            WeaponDefinition definition =
                AssetDatabase.LoadAssetAtPath<WeaponDefinition>(path);
            Require(definition != null, $"Weapon definition is missing: {path}");
            return definition;
        }

        private static void RequireProfile(
            WeaponController weapon,
            CharacterAnimationController animation,
            CharacterAnimationLibrary library,
            WeaponDefinition definition,
            CharacterAnimationStyle expectedStyle)
        {
            if (definition == null)
            {
                weapon.Clear();
            }
            else
            {
                weapon.Equip(definition, definition.AmmunitionCapacity);
            }

            Require(
                animation.CurrentStyle == expectedStyle &&
                animation.Animator.runtimeAnimatorController ==
                    library.GetController(expectedStyle),
                $"Equipment profile did not switch to {expectedStyle}.");
        }

        private static bool HasParameter(Animator animator, string name)
        {
            AnimatorControllerParameter[] parameters = animator.parameters;
            for (int i = 0; i < parameters.Length; i++)
            {
                if (parameters[i].name == name)
                {
                    return true;
                }
            }

            return false;
        }

        private static void Require(bool condition, string message)
        {
            if (!condition)
            {
                throw new InvalidOperationException(message);
            }
        }

        private static void HandleLog(
            string condition,
            string stackTrace,
            LogType type)
        {
            if (!SessionState.GetBool(RunningKey, false) ||
                (type != LogType.Error &&
                 type != LogType.Exception &&
                 type != LogType.Assert))
            {
                return;
            }

            RecordFailure(condition + Environment.NewLine + stackTrace);
        }

        private static void RecordFailure(string failure)
        {
            if (SessionState.GetBool(FailedKey, false))
            {
                return;
            }

            SessionState.SetBool(FailedKey, true);
            SessionState.SetString(FailureKey, failure);
        }

        private static void Finish()
        {
            bool failed = SessionState.GetBool(FailedKey, false);
            string failure = SessionState.GetString(FailureKey, string.Empty);
            SessionState.SetBool(RunningKey, false);
            SessionState.SetString(PhaseKey, string.Empty);
            DetachCallbacks();

            if (failed)
            {
                Debug.LogError(
                    "Stage1 character animation play-mode smoke test failed:\n" +
                    failure);
                EditorApplication.Exit(1);
                return;
            }

            Debug.Log(
                "Stage1 character animation play-mode smoke test passed.");
            EditorApplication.Exit(0);
        }
    }
}
