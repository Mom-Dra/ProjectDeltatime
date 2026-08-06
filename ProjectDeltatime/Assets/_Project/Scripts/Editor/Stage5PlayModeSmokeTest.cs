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
    public static class Stage5PlayModeSmokeTest
    {
        private const string ScenePath =
            "Assets/_Project/Scenes/Stage5.unity";
        private const string NavigationPath =
            "Assets/_Project/Scenes/Stage5Navigation.asset";
        private const string EnvironmentRootName =
            "Stage 5 - Undertow Dive";
        private const string RunningKey = "Deltatime.Stage5Smoke.Running";
        private const string FailedKey = "Deltatime.Stage5Smoke.Failed";
        private const string FailureKey = "Deltatime.Stage5Smoke.Failure";
        private const string PhaseKey = "Deltatime.Stage5Smoke.Phase";

        private static bool callbacksAttached;
        private static bool validationRan;
        private static double playStartedAt;

        static Stage5PlayModeSmokeTest()
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
            if (!validationRan && elapsed >= 0.9d)
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
            else if (elapsed >= 20d)
            {
                RecordFailure("Stage5 play-mode smoke test timed out.");
                EditorApplication.isPlaying = false;
            }
        }

        private static void ValidateRuntimeState()
        {
            Scene scene = SceneManager.GetActiveScene();
            PlayerHealth player = UnityEngine.Object.FindFirstObjectByType<PlayerHealth>();
            DeadlineController deadline =
                UnityEngine.Object.FindFirstObjectByType<DeadlineController>();
            WorldTimeController worldTime =
                UnityEngine.Object.FindFirstObjectByType<WorldTimeController>();
            StageController stage =
                UnityEngine.Object.FindFirstObjectByType<StageController>();
            StageReplayController replay =
                UnityEngine.Object.FindFirstObjectByType<StageReplayController>();
            NavMeshSurface surface =
                UnityEngine.Object.FindFirstObjectByType<NavMeshSurface>();
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
            GameObject environmentRoot = GameObject.Find(EnvironmentRootName);

            Require(scene.path == ScenePath, $"Unexpected scene: {scene.path}");
            Require(player != null && player.IsAlive,
                "Stage5 player did not initialize alive.");
            Require(deadline != null && deadline.ChargesRemaining == 2,
                "Stage5 Deadline charges did not initialize to 2.");
            Require(worldTime != null && worldTime.enabled,
                "Stage5 world time did not initialize.");
            Require(stage != null &&
                    stage.CurrentState == StageController.StageState.Active,
                "Stage5 did not enter the active state.");
            Require(stage != null && stage.RemainingEnemyCount == 5,
                $"Stage5 registered {stage?.RemainingEnemyCount} enemies instead of 5.");
            Require(enemies.Length == 5 && motors.Length == 5 &&
                    shooters.Length == 3 && chasers.Length == 2,
                "Stage5 enemy combat or navigation components are missing.");
            Require(pickups.Length == 2,
                $"Stage5 has {pickups.Length} weapon pickups instead of 2.");
            Require(replay != null && replay.enabled,
                "Stage5 replay did not initialize.");
            Require(replay != null && replay.TrackedLightCount == 2,
                $"Stage5 replay tracked {replay?.TrackedLightCount} vision lights instead of 2.");
            Require(replay != null && replay.TrackedReplayVisionConeCount == 1,
                "Stage5 replay vision-cone track did not initialize.");
            Require(replay != null && replay.TrackedExcludedVisualCount == 0,
                $"Stage5 replay tracked {replay?.TrackedExcludedVisualCount} " +
                "static dive-bar renderers.");
            Require(surface != null && surface.navMeshData != null,
                "Stage5 NavMesh data is missing at runtime.");
            Require(surface != null &&
                    AssetDatabase.GetAssetPath(surface.navMeshData) == NavigationPath,
                "Stage5 is not using its dedicated NavMesh data.");
            Require(environmentRoot != null &&
                    environmentRoot.GetComponent<ReplayExcluded>() != null,
                "Stage5 static environment is not excluded from replay tracks.");
            Require(visuals.Length == 6,
                $"Stage5 has {visuals.Length} character visual controllers instead of 6.");
            Require(GameObject.Find("Dive Bar Character - Player") != null &&
                    GameObject.Find("Dive Bar Character - West Gunner") != null &&
                    GameObject.Find("Dive Bar Character - Center Chaser") != null &&
                    GameObject.Find("Dive Bar Character - East Gunner") != null &&
                    GameObject.Find("Dive Bar Character - North Gunner") != null &&
                    GameObject.Find("Dive Bar Character - South Chaser") != null,
                "Stage5 Polygon Nightclubs character visuals are missing.");

            RequireOnNavMesh(player.transform.position, "player");
            for (int i = 0; i < enemies.Length; i++)
            {
                RequireOnNavMesh(enemies[i].transform.position, enemies[i].name);
                RequireCompletePath(
                    player.transform.position,
                    enemies[i].transform.position,
                    enemies[i].name);
            }
        }

        private static void RequireOnNavMesh(Vector3 position, string subject)
        {
            bool found = NavMesh.SamplePosition(
                position,
                out _,
                1.5f,
                NavMesh.AllAreas);
            Require(found,
                $"Stage5 {subject} spawn is not on the baked NavMesh ({position}).");
        }

        private static void RequireCompletePath(
            Vector3 from,
            Vector3 to,
            string subject)
        {
            Require(NavMesh.SamplePosition(from, out NavMeshHit fromHit, 1.5f, NavMesh.AllAreas),
                "Stage5 player path origin is invalid.");
            Require(NavMesh.SamplePosition(to, out NavMeshHit toHit, 1.5f, NavMesh.AllAreas),
                $"Stage5 {subject} path destination is invalid.");
            NavMeshPath path = new NavMeshPath();
            bool calculated = NavMesh.CalculatePath(
                fromHit.position,
                toHit.position,
                NavMesh.AllAreas,
                path);
            Require(calculated && path.status == NavMeshPathStatus.PathComplete &&
                    path.corners.Length > 0,
                $"Stage5 {subject} did not create a complete NavMesh path.");
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
                Debug.LogError("Stage5 play-mode smoke test failed:\n" + failure);
                EditorApplication.Exit(1);
                return;
            }

            Debug.Log("Stage5 play-mode smoke test passed.");
            EditorApplication.Exit(0);
        }
    }
}
