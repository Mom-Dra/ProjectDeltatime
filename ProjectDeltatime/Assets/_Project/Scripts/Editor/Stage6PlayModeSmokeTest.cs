using System;
using Deltatime.Combat;
using Deltatime.Enemies;
using Deltatime.Level;
using Deltatime.Performance;
using Deltatime.Player;
using Deltatime.Replay;
using Deltatime.TimeSystem;
using Deltatime.Visuals;
using Deltatime.Vision;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

namespace Deltatime.EditorTools
{
    [InitializeOnLoad]
    public static class Stage6PlayModeSmokeTest
    {
        private const string ScenePath =
            "Assets/_Project/Scenes/Stage6.unity";
        private const string NavigationPath =
            "Assets/_Project/Scenes/Stage6Navigation.asset";
        private const string EnvironmentRootName =
            "Stage 6 - Neon Overlook";
        private const string RunningKey = "Deltatime.Stage6Smoke.Running";
        private const string FailedKey = "Deltatime.Stage6Smoke.Failed";
        private const string FailureKey = "Deltatime.Stage6Smoke.Failure";
        private const string PhaseKey = "Deltatime.Stage6Smoke.Phase";

        private static bool callbacksAttached;
        private static bool validationRan;
        private static double playStartedAt;

        static Stage6PlayModeSmokeTest()
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
            if (!validationRan && elapsed >= 1.1d)
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
            else if (elapsed >= 25d)
            {
                RecordFailure("Stage6 play-mode smoke test timed out.");
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
            Stage6PerformanceController performance =
                UnityEngine.Object.FindFirstObjectByType<Stage6PerformanceController>();
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
                "Stage6 player did not initialize alive.");
            Require(deadline != null && deadline.ChargesRemaining == 2,
                "Stage6 Deadline charges did not initialize to 2.");
            Require(worldTime != null && worldTime.enabled,
                "Stage6 world time did not initialize.");
            Require(stage != null &&
                    stage.CurrentState == StageController.StageState.Active,
                "Stage6 did not enter the active state.");
            Require(stage != null && stage.RemainingEnemyCount == 5,
                $"Stage6 registered {stage?.RemainingEnemyCount} enemies instead of 5.");
            Require(enemies.Length == 5 && motors.Length == 5 &&
                    shooters.Length == 3 && chasers.Length == 2,
                "Stage6 enemy combat or navigation components are missing.");
            Require(pickups.Length == 2,
                $"Stage6 has {pickups.Length} weapon pickups instead of 2.");
            Require(replay != null && replay.enabled,
                "Stage6 replay did not initialize.");
            Require(replay != null && replay.UsesOptimizedRendererDiscovery &&
                    replay.RendererDiscoveryRootCount == 9 &&
                    Mathf.Approximately(
                        replay.FallbackRendererDiscoveryInterval,
                        0.25f),
                "Stage6 replay did not initialize its dynamic-root renderer discovery.");
            Require(replay != null && replay.TrackedLightCount == 2,
                $"Stage6 replay tracked {replay?.TrackedLightCount} vision lights instead of 2.");
            Require(replay != null && replay.TrackedReplayVisionConeCount == 1,
                "Stage6 replay vision-cone track did not initialize.");
            Require(replay != null && replay.TrackedExcludedVisualCount == 0,
                $"Stage6 replay tracked {replay?.TrackedExcludedVisualCount} " +
                "static rooftop renderers.");
            Require(surface != null && surface.navMeshData != null,
                "Stage6 NavMesh data is missing at runtime.");
            Require(surface != null &&
                    AssetDatabase.GetAssetPath(surface.navMeshData) == NavigationPath,
                "Stage6 is not using its dedicated NavMesh data.");
            Require(environmentRoot != null &&
                    environmentRoot.GetComponent<ReplayExcluded>() != null,
                "Stage6 static environment is not excluded from replay tracks.");
            Require(performance != null && performance.enabled &&
                    performance.IsRuntimePerformanceBudgetApplied,
                "Stage6 runtime performance budget did not initialize.");
            Require(performance != null &&
                    performance.EnvironmentPointLightCount > 0 &&
                    performance.ActiveEnvironmentShadowedPointLightCount <= 2 &&
                    performance.MaximumShadowedEnvironmentPointLights == 2,
                "Stage6 environment point-light shadow budget exceeds two lights.");
            Require(QualitySettings.shadowDistance <= 40.001f &&
                    QualitySettings.shadowCascades <= 2 &&
                    (int)QualitySettings.shadowResolution <=
                    (int)ShadowResolution.Medium,
                "Stage6 runtime quality shadow budget was not applied.");
            Require(CountSoftVisionLights(out string visionLightState) == 2,
                "Stage6 did not preserve both soft-shadow vision lights: " +
                visionLightState);
            Require(visuals.Length == 6,
                $"Stage6 has {visuals.Length} character visual controllers instead of 6.");
            Require(GameObject.Find("Overlook Character - Player") != null &&
                    GameObject.Find("Overlook Character - West Gunner") != null &&
                    GameObject.Find("Overlook Character - Center Chaser") != null &&
                    GameObject.Find("Overlook Character - East Gunner") != null &&
                    GameObject.Find("Overlook Character - North Gunner") != null &&
                    GameObject.Find("Overlook Character - South Chaser") != null,
                "Stage6 Polygon Nightclubs character visuals are missing.");

            RequireOnNavMesh(player.transform.position, "player");
            for (int i = 0; i < enemies.Length; i++)
            {
                RequireOnNavMesh(enemies[i].transform.position, enemies[i].name);
                RequireCompletePath(
                    player.transform.position,
                    enemies[i].transform.position,
                    enemies[i].name);
            }

            NavMeshTriangulation triangulation = NavMesh.CalculateTriangulation();
            Require(triangulation.vertices.Length > 0 && triangulation.indices.Length > 0,
                "Stage6 runtime NavMesh triangulation is empty.");
            Debug.Log(
                $"Stage6 play-mode smoke runtime validation passed: " +
                $"NavMesh vertices={triangulation.vertices.Length}, " +
                $"indices={triangulation.indices.Length}, complete paths=5/5.");
        }

        private static int CountSoftVisionLights(out string state)
        {
            VisionCone vision = UnityEngine.Object.FindFirstObjectByType<VisionCone>();
            Light spot = vision == null ? null : vision.RuntimeVisionSpotLight;
            Light near = vision == null ? null : vision.RuntimeNearWallLight;
            int count = 0;
            if (spot != null && spot.isActiveAndEnabled &&
                spot.shadows == LightShadows.Soft)
            {
                count++;
            }

            if (near != null && near.isActiveAndEnabled &&
                near.shadows == LightShadows.Soft)
            {
                count++;
            }

            state = $"spot={spot?.shadows.ToString() ?? "missing"}, " +
                $"near={near?.shadows.ToString() ?? "missing"}";
            return count;
        }

        private static void RequireOnNavMesh(Vector3 position, string subject)
        {
            bool found = NavMesh.SamplePosition(
                position,
                out _,
                1.5f,
                NavMesh.AllAreas);
            Require(found,
                $"Stage6 {subject} spawn is not on the baked NavMesh ({position}).");
        }

        private static void RequireCompletePath(
            Vector3 from,
            Vector3 to,
            string subject)
        {
            Require(NavMesh.SamplePosition(
                    from,
                    out NavMeshHit fromHit,
                    1.5f,
                    NavMesh.AllAreas),
                "Stage6 player path origin is invalid.");
            Require(NavMesh.SamplePosition(
                    to,
                    out NavMeshHit toHit,
                    1.5f,
                    NavMesh.AllAreas),
                $"Stage6 {subject} path destination is invalid.");
            NavMeshPath path = new NavMeshPath();
            bool calculated = NavMesh.CalculatePath(
                fromHit.position,
                toHit.position,
                NavMesh.AllAreas,
                path);
            Require(calculated && path.status == NavMeshPathStatus.PathComplete &&
                    path.corners.Length > 0,
                $"Stage6 {subject} did not create a complete NavMesh path.");
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
                Debug.LogError("Stage6 play-mode smoke test failed:\n" + failure);
                EditorApplication.Exit(1);
                return;
            }

            Debug.Log("Stage6 play-mode smoke test passed.");
            EditorApplication.Exit(0);
        }
    }
}
