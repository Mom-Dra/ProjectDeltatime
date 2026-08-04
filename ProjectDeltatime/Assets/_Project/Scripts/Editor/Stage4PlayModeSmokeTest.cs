using System;
using Deltatime.Combat;
using Deltatime.Enemies;
using Deltatime.Level;
using Deltatime.Player;
using Deltatime.Replay;
using Deltatime.TimeSystem;
using Deltatime.Visuals;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

namespace Deltatime.EditorTools
{
    [InitializeOnLoad]
    public static class Stage4PlayModeSmokeTest
    {
        private const string ScenePath =
            "Assets/_Project/Scenes/Stage4.unity";
        private const string NavigationPath =
            "Assets/_Project/Scenes/Stage4Navigation.asset";
        private const string RunningKey = "Deltatime.Stage4Smoke.Running";
        private const string FailedKey = "Deltatime.Stage4Smoke.Failed";
        private const string FailureKey = "Deltatime.Stage4Smoke.Failure";
        private const string PhaseKey = "Deltatime.Stage4Smoke.Phase";

        private static bool callbacksAttached;
        private static bool validationRan;
        private static double playStartedAt;

        static Stage4PlayModeSmokeTest()
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
                     SessionState.GetString(PhaseKey, string.Empty) == "stopping")
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
                RecordFailure("Stage4 play-mode smoke test timed out.");
                EditorApplication.isPlaying = false;
            }
        }

        private static void ValidateRuntimeState()
        {
            Scene scene = SceneManager.GetActiveScene();
            PlayerHealth player = UnityEngine.Object.FindFirstObjectByType<PlayerHealth>();
            DeadlineController deadline = UnityEngine.Object.FindFirstObjectByType<DeadlineController>();
            WorldTimeController worldTime = UnityEngine.Object.FindFirstObjectByType<WorldTimeController>();
            StageController stage = UnityEngine.Object.FindFirstObjectByType<StageController>();
            StageReplayController replay = UnityEngine.Object.FindFirstObjectByType<StageReplayController>();
            NavMeshSurface surface = UnityEngine.Object.FindFirstObjectByType<NavMeshSurface>();
            EnemyHealth[] enemies = UnityEngine.Object.FindObjectsByType<EnemyHealth>(
                FindObjectsSortMode.None);
            EnemyMotor[] motors = UnityEngine.Object.FindObjectsByType<EnemyMotor>(
                FindObjectsSortMode.None);
            EnemyShooter[] shooters = UnityEngine.Object.FindObjectsByType<EnemyShooter>(
                FindObjectsSortMode.None);
            EnemyChaser[] chasers = UnityEngine.Object.FindObjectsByType<EnemyChaser>(
                FindObjectsSortMode.None);
            WeaponPickup[] pickups = UnityEngine.Object.FindObjectsByType<WeaponPickup>(
                FindObjectsSortMode.None);
            CharacterVisualController[] visuals =
                UnityEngine.Object.FindObjectsByType<CharacterVisualController>(
                    FindObjectsSortMode.None);

            Require(scene.path == ScenePath, $"Unexpected scene: {scene.path}");
            Require(player != null && player.IsAlive, "Stage4 player did not initialize.");
            Require(deadline != null && deadline.ChargesRemaining == 2,
                "Stage4 Deadline charges did not initialize to 2.");
            Require(worldTime != null && worldTime.enabled,
                "Stage4 world time did not initialize.");
            Require(stage != null && stage.CurrentState == StageController.StageState.Active,
                "Stage4 did not enter the active state.");
            Require(stage != null && stage.RemainingEnemyCount == 5,
                $"Stage4 registered {stage?.RemainingEnemyCount} enemies instead of 5.");
            Require(enemies.Length == 5 && motors.Length == 5 &&
                    shooters.Length == 3 && chasers.Length == 2,
                "Stage4 enemy combat or navigation components are missing.");
            Require(pickups.Length == 2,
                $"Stage4 has {pickups.Length} weapon pickups instead of 2.");
            Require(replay != null && replay.enabled,
                "Stage4 replay did not initialize.");
            Require(replay != null && replay.TrackedLightCount == 2,
                $"Stage4 replay tracked {replay?.TrackedLightCount} vision lights instead of 2.");
            Require(replay != null && replay.TrackedReplayVisionConeCount == 1,
                "Stage4 replay vision-cone track did not initialize.");
            Require(replay != null && replay.TrackedExcludedVisualCount == 0,
                $"Stage4 replay tracked {replay?.TrackedExcludedVisualCount} " +
                "static rooftop renderers.");
            Require(surface != null && surface.navMeshData != null,
                "Stage4 NavMesh data is missing at runtime.");
            Require(surface != null &&
                    AssetDatabase.GetAssetPath(surface.navMeshData) == NavigationPath,
                "Stage4 is not using its dedicated NavMesh data.");
            Require(GameObject.Find("Stage 4 - Last Call Rooftop") != null,
                "Stage4 rooftop environment root is missing.");
            Require(GameObject.Find("Stage 4 - Last Call Rooftop")
                        .GetComponent<ReplayExcluded>() != null,
                "Stage4 static environment is not excluded from replay tracks.");
            Require(visuals.Length == 6,
                $"Stage4 has {visuals.Length} character visual controllers instead of 6.");
            Require(GameObject.Find("Rooftop Character - Player") != null &&
                    GameObject.Find("Rooftop Character - West Gunner") != null &&
                    GameObject.Find("Rooftop Character - North Chaser") != null &&
                    GameObject.Find("Rooftop Character - East Gunner") != null &&
                    GameObject.Find("Rooftop Character - North Gunner") != null &&
                    GameObject.Find("Rooftop Character - South Chaser") != null,
                "Stage4 Polygon Nightclubs character visuals are missing.");

            RequireOnNavMesh(player.transform.position, "player");
            for (int i = 0; i < enemies.Length; i++)
            {
                RequireOnNavMesh(enemies[i].transform.position, enemies[i].name);
            }
        }

        private static void RequireOnNavMesh(Vector3 position, string subject)
        {
            bool found = NavMesh.SamplePosition(
                position,
                out NavMeshHit hit,
                1.5f,
                NavMesh.AllAreas);
            Require(found,
                $"Stage4 {subject} spawn is not on the baked NavMesh ({position}).");
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
                (type != LogType.Error && type != LogType.Exception &&
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
                Debug.LogError("Stage4 play-mode smoke test failed:\n" + failure);
                EditorApplication.Exit(1);
                return;
            }

            Debug.Log("Stage4 play-mode smoke test passed.");
            EditorApplication.Exit(0);
        }
    }
}
