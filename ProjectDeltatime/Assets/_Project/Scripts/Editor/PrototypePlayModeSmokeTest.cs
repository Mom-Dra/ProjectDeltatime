using System;
using System.Text;
using Deltatime.Combat;
using Deltatime.Core;
using Deltatime.Enemies;
using Deltatime.Level;
using Deltatime.Player;
using Deltatime.Replay;
using Deltatime.TimeSystem;
using Deltatime.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Deltatime.EditorTools
{
    [InitializeOnLoad]
    public static class PrototypePlayModeSmokeTest
    {
        private const string ScenePath = "Assets/_Project/Scenes/PrototypeRoom.unity";
        private const string RunningKey = "Deltatime.Smoke.Running";
        private const string FailedKey = "Deltatime.Smoke.Failed";
        private const string FailureTextKey = "Deltatime.Smoke.FailureText";
        private const string PhaseKey = "Deltatime.Smoke.Phase";

        private static double playStartedAt;
        private static bool checksRan;
        private static bool replayChecksRan;
        private static bool callbacksAttached;

        static PrototypePlayModeSmokeTest()
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
            SessionState.SetString(FailureTextKey, string.Empty);
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
                checksRan = false;
                replayChecksRan = false;
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

            string phase = SessionState.GetString(PhaseKey, string.Empty);
            if (EditorApplication.isPlaying)
            {
                if (phase != "playing")
                {
                    playStartedAt = EditorApplication.timeSinceStartup;
                    checksRan = false;
                    replayChecksRan = false;
                    SessionState.SetString(PhaseKey, "playing");
                }

                double elapsed = EditorApplication.timeSinceStartup - playStartedAt;
                if (!checksRan && elapsed >= 0.5d)
                {
                    checksRan = true;
                    ValidateRuntimeState();
                    ClearStage();
                }

                if (!replayChecksRan && elapsed >= 0.85d)
                {
                    replayChecksRan = true;
                    ValidateReplayState();
                }

                if (elapsed >= 1.4d)
                {
                    SessionState.SetString(PhaseKey, "stopping");
                    EditorApplication.isPlaying = false;
                }
            }
            else if (phase == "stopping")
            {
                Finish();
            }
        }

        private static void ClearStage()
        {
            EnemyHealth[] enemies =
                UnityEngine.Object.FindObjectsOfType<EnemyHealth>();
            for (int i = 0; i < enemies.Length; i++)
            {
                EnemyHealth enemy = enemies[i];
                enemy.ReceiveHit(new DamageHit(
                    1,
                    enemy.transform.position,
                    Vector3.forward,
                    null));
            }
        }

        private static void ValidateReplayState()
        {
            StageController stage =
                UnityEngine.Object.FindObjectOfType<StageController>();
            StageReplayController replay =
                UnityEngine.Object.FindObjectOfType<StageReplayController>();
            TopDownCameraController cameraRig =
                UnityEngine.Object.FindObjectOfType<TopDownCameraController>();

            Require(
                stage != null &&
                stage.CurrentState == StageController.StageState.Replaying,
                "Clearing all enemies did not put the stage into replay state.");
            Require(
                replay != null && replay.IsReplaying,
                "Clearing all enemies did not start replay playback.");
            Require(
                replay != null && replay.RecordedDuration > 0f,
                "The replay did not retain a playable recording.");
            Require(
                cameraRig != null && !cameraRig.enabled,
                "Live camera simulation remained enabled during replay.");
            Require(
                Mathf.Approximately(UnityEngine.Time.timeScale, 1f),
                "Replay changed global Time.timeScale.");
        }

        private static void ValidateRuntimeState()
        {
            StageController stage = UnityEngine.Object.FindObjectOfType<StageController>();
            WorldTimeController worldTime =
                UnityEngine.Object.FindObjectOfType<WorldTimeController>();
            PlayerHealth player = UnityEngine.Object.FindObjectOfType<PlayerHealth>();
            WeaponController weapon =
                UnityEngine.Object.FindObjectOfType<WeaponController>();
            GameHud hud = UnityEngine.Object.FindObjectOfType<GameHud>();
            StageReplayController replay =
                UnityEngine.Object.FindObjectOfType<StageReplayController>();
            EnemyShooter[] enemies =
                UnityEngine.Object.FindObjectsOfType<EnemyShooter>();
            TopDownCameraController cameraRig =
                UnityEngine.Object.FindObjectOfType<TopDownCameraController>();
            Camera gameplayCamera = Camera.main;
            Rigidbody2D[] legacyBodies =
                UnityEngine.Object.FindObjectsOfType<Rigidbody2D>();

            Require(stage != null, "StageController is missing at runtime.");
            Require(worldTime != null, "WorldTimeController is missing at runtime.");
            Require(player != null && player.IsAlive, "The player did not initialize alive.");
            Require(weapon != null && weapon.HasWeapon, "The player did not initialize with a weapon.");
            Require(hud != null && hud.enabled, "GameHud did not initialize.");
            Require(replay != null && replay.enabled, "Stage replay did not initialize.");
            Require(
                replay != null && Mathf.Approximately(replay.CaptureRate, 20f),
                "Stage replay capture rate is not configured to 20 Hz.");
            Require(enemies.Length == 3, $"Expected 3 enemies, found {enemies.Length}.");
            Require(
                gameplayCamera != null && !gameplayCamera.orthographic,
                "The gameplay camera is not a perspective camera.");
            Require(cameraRig != null && cameraRig.enabled, "The 3D camera rig did not initialize.");
            Require(legacyBodies.Length == 0, "Legacy 2D rigidbodies remain in the 3D scene.");

            if (stage != null)
            {
                Require(
                    stage.RemainingEnemyCount == 3,
                    $"Stage registered {stage.RemainingEnemyCount} enemies instead of 3.");
            }

            if (worldTime != null)
            {
                Require(
                    worldTime.CurrentTimeScale >= 0.019f &&
                    worldTime.CurrentTimeScale < 0.2f,
                    $"Idle world scale was {worldTime.CurrentTimeScale:0.000}.");
            }

            Require(
                Mathf.Approximately(UnityEngine.Time.timeScale, 1f),
                "Global Time.timeScale was modified.");

            if (replay != null)
            {
                Require(
                    replay.CapturedFrameCount > 0,
                    "Stage replay did not capture any frames.");
            }
        }

        private static void Require(bool condition, string message)
        {
            if (!condition)
            {
                RecordFailure(message);
            }
        }

        private static void HandleLog(string condition, string stackTrace, LogType type)
        {
            if (type == LogType.Error ||
                type == LogType.Exception ||
                type == LogType.Assert)
            {
                RecordFailure($"{type}: {condition}\n{stackTrace}");
            }
        }

        private static void RecordFailure(string message)
        {
            SessionState.SetBool(FailedKey, true);
            string existing = SessionState.GetString(FailureTextKey, string.Empty);
            StringBuilder builder = new StringBuilder(existing);
            if (builder.Length > 0)
            {
                builder.AppendLine();
            }

            builder.Append(message);
            SessionState.SetString(FailureTextKey, builder.ToString());
        }

        private static void Finish()
        {
            bool failed = SessionState.GetBool(FailedKey, false);
            string failureText = SessionState.GetString(FailureTextKey, string.Empty);

            SessionState.EraseBool(RunningKey);
            SessionState.EraseBool(FailedKey);
            SessionState.EraseString(FailureTextKey);
            SessionState.EraseString(PhaseKey);
            DetachCallbacks();

            if (failed)
            {
                Debug.LogError($"Prototype play-mode smoke test failed:\n{failureText}");
                EditorApplication.Exit(1);
            }
            else
            {
                Debug.Log("Prototype play-mode smoke test passed.");
                EditorApplication.Exit(0);
            }
        }
    }
}
