using System;
using System.Reflection;
using Deltatime.Player;
using Deltatime.Replay;
using Deltatime.TimeSystem;
using Deltatime.Utilities;
using Deltatime.Visuals;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Deltatime.EditorTools
{
    [InitializeOnLoad]
    public static class ReplayPlayModeSmokeTest
    {
        private const string ScenePath =
            "Assets/_Project/Scenes/Stage2.unity";
        private const string RunningKey =
            "Deltatime.ReplaySmoke.Running";
        private const string PhaseKey = "Deltatime.ReplaySmoke.Phase";

        private static bool callbacksAttached;
        private static double playStartedAt;
        private static bool setupComplete;
        private static bool slowStarted;
        private static bool slowReleased;
        private static bool replayRequested;
        private static bool replayValidated;
        private static bool observedSourceAdvance;
        private static bool observedAftermathRecoveryAdvance;
        private static float previousPlaybackElapsed = -1f;
        private static float previousSourceTimestamp = -1f;
        private static float previousCameraRecoveryBlend = -1f;
        private static StageReplayController replay;
        private static DeadlineController deadline;
        private static WorldTimeActivity activity;
        private static MethodInfo activateDeadline;
        private static MethodInfo releaseDeadline;

        static ReplayPlayModeSmokeTest()
        {
            if (SessionState.GetBool(RunningKey, false))
            {
                AttachCallbacks();
            }
        }

        public static void RunFromCommandLine()
        {
            SessionState.SetBool(RunningKey, true);
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
        }

        private static void DetachCallbacks()
        {
            if (!callbacksAttached)
            {
                return;
            }

            callbacksAttached = false;
            EditorApplication.update -= Tick;
            EditorApplication.playModeStateChanged -=
                HandlePlayModeStateChanged;
        }

        private static void HandlePlayModeStateChanged(
            PlayModeStateChange state)
        {
            if (!SessionState.GetBool(RunningKey, false))
            {
                return;
            }

            if (state == PlayModeStateChange.EnteredPlayMode)
            {
                ResetRuntimeState();
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
                FinishSuccess();
            }
        }

        private static void ResetRuntimeState()
        {
            playStartedAt = EditorApplication.timeSinceStartup;
            setupComplete = false;
            slowStarted = false;
            slowReleased = false;
            replayRequested = false;
            replayValidated = false;
            observedSourceAdvance = false;
            observedAftermathRecoveryAdvance = false;
            previousPlaybackElapsed = -1f;
            previousSourceTimestamp = -1f;
            previousCameraRecoveryBlend = -1f;
            replay = null;
            deadline = null;
            activity = null;
            activateDeadline = null;
            releaseDeadline = null;
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

            try
            {
                double elapsed =
                    EditorApplication.timeSinceStartup - playStartedAt;
                if (!setupComplete && elapsed >= 0.5d)
                {
                    SetupReplayProbe();
                }

                if (setupComplete && !slowStarted && elapsed >= 1d)
                {
                    slowStarted = true;
                    activity.SetAimTurn(1f);
                    activateDeadline.Invoke(deadline, null);
                    Require(deadline.IsActive,
                        "Replay smoke could not start the strong-slow window.");
                    TriggerAttackAnimation("strong-slow");
                    PlayerHealth slowPlayer =
                        UnityEngine.Object.FindFirstObjectByType<PlayerHealth>();
                    Require(slowPlayer != null,
                        "Replay smoke strong-slow player is missing.");
                    slowPlayer.transform.position +=
                        new Vector3(-0.2f, 0f, 0.35f);
                    HitFlash.Create(
                        slowPlayer.transform.position + Vector3.up * 0.2f,
                        Color.yellow);
                }

                if (slowStarted && !slowReleased && elapsed >= 2.1d)
                {
                    slowReleased = true;
                    releaseDeadline.Invoke(deadline, null);
                    activity.SetAimTurn(0f);
                    activity.Pulse(1f, 1f);
                    Require(!deadline.IsActive,
                        "Replay smoke could not release the strong-slow window.");
                }

                if (slowReleased && !replayRequested && elapsed >= 3d)
                {
                    replayRequested = true;
                    Require(replay.RequestReplay(),
                        "Replay smoke request was rejected.");
                }

                if (replayRequested && !replayValidated && elapsed >= 3.35d)
                {
                    ValidateReplay();
                    replayValidated = true;
                }

                if (replayValidated)
                {
                    ValidateSourceOrder();
                }

                if (elapsed >= 6.5d)
                {
                    Require(observedSourceAdvance,
                        "Normalized replay source time never advanced.");
                    Require(observedAftermathRecoveryAdvance,
                        "Deadline camera recovery never advanced smoothly.");
                    SessionState.SetString(PhaseKey, "stopping");
                    EditorApplication.isPlaying = false;
                }
            }
            catch (Exception exception)
            {
                FinishFailure(exception);
            }
        }

        private static void SetupReplayProbe()
        {
            replay = UnityEngine.Object.FindFirstObjectByType<
                StageReplayController>();
            deadline = UnityEngine.Object.FindFirstObjectByType<
                DeadlineController>();
            activity = UnityEngine.Object.FindFirstObjectByType<
                WorldTimeActivity>();
            activateDeadline = typeof(DeadlineController).GetMethod(
                "ActivateDeadline",
                BindingFlags.Instance | BindingFlags.NonPublic);
            releaseDeadline = typeof(DeadlineController).GetMethod(
                "ReleaseDeadline",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Require(
                replay != null &&
                deadline != null &&
                activity != null &&
                activateDeadline != null &&
                releaseDeadline != null,
                "Replay smoke dependencies are missing.");

            activity.Pulse(1f, 1.5f);
            TriggerAttackAnimation("normal-speed");

            PlayerHealth player = UnityEngine.Object.FindFirstObjectByType<
                PlayerHealth>();
            Require(player != null, "Replay smoke player is missing.");
            player.transform.position += new Vector3(0.4f, 0f, 0.2f);
            HitFlash.Create(
                player.transform.position + Vector3.up * 0.2f,
                Color.magenta);
            setupComplete = true;
        }

        private static void TriggerAttackAnimation(string phase)
        {
            CharacterAnimationController[] animationControllers =
                UnityEngine.Object.FindObjectsByType<
                    CharacterAnimationController>(
                    FindObjectsInactive.Exclude,
                    FindObjectsSortMode.None);
            bool attackTriggered = false;
            for (int i = 0; i < animationControllers.Length; i++)
            {
                CharacterAnimationController controller =
                    animationControllers[i];
                attackTriggered |=
                    controller.TryPlayMeleeAttackAnimation();
                if (!attackTriggered &&
                    controller.Animator != null &&
                    HasAnimatorParameter(
                        controller.Animator,
                        "AttackA"))
                {
                    // Pistol gameplay intentionally rejects its placeholder
                    // melee attack. The smoke test drives the existing
                    // trigger directly only to verify replay pose capture.
                    controller.Animator.SetTrigger("AttackA");
                    attackTriggered = true;
                }
            }

            Require(attackTriggered,
                $"Replay smoke could not trigger a {phase} attack Animator state.");
        }

        private static bool HasAnimatorParameter(
            Animator animator,
            string parameterName)
        {
            AnimatorControllerParameter[] parameters = animator.parameters;
            for (int i = 0; i < parameters.Length; i++)
            {
                if (parameters[i].name == parameterName &&
                    parameters[i].type ==
                    AnimatorControllerParameterType.Trigger)
                {
                    return true;
                }
            }

            return false;
        }

        private static void ValidateReplay()
        {
            Require(replay.IsReplaying,
                "Replay smoke did not enter playback.");
            Require(
                replay.SourceRecordedDuration - replay.RecordedDuration >=
                0.8f,
                $"Strong slow was not normalized: source=" +
                $"{replay.SourceRecordedDuration:0.000}, replay=" +
                $"{replay.RecordedDuration:0.000}.");
            Require(
                replay.DeadlineCinematicSegmentCount == 1 &&
                replay.LongestDeadlineCinematicDuration <= 0.08f,
                $"Strong-slow window retained slow duration: " +
                $"{replay.LongestDeadlineCinematicDuration:0.000}s.");
            Require(
                replay.TrackedAnimatedVisualCount >= 1 &&
                replay.RecordedAnimatedPoseCount >
                replay.TrackedAnimatedVisualCount &&
                replay.HasRecordedAnimatedMotion &&
                replay.ActiveAnimatedReplayVisualCount >= 1,
                $"Animated replay proxy is incomplete: tracked=" +
                $"{replay.TrackedAnimatedVisualCount}, poses=" +
                $"{replay.RecordedAnimatedPoseCount}, motion=" +
                $"{replay.HasRecordedAnimatedMotion}, active=" +
                $"{replay.ActiveAnimatedReplayVisualCount}.");
            Require(CountReplayHitFlashes() >= 2,
                "Replay did not retain both normal and strong-slow hit VFX event tracks.");
            Require(Mathf.Approximately(Time.timeScale, 1f),
                "Replay changed global Time.timeScale.");
        }

        private static int CountReplayHitFlashes()
        {
            LineRenderer[] lines = UnityEngine.Object.FindObjectsByType<
                LineRenderer>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None);
            int count = 0;
            for (int i = 0; i < lines.Length; i++)
            {
                if (lines[i].name == "Replay - Hit Flash")
                {
                    count++;
                }
            }

            return count;
        }

        private static void ValidateSourceOrder()
        {
            float elapsed = replay.PlaybackElapsed;
            float source = replay.CurrentSourceTimestamp;
            if (previousPlaybackElapsed >= 0f &&
                elapsed + 0.001f < previousPlaybackElapsed)
            {
                previousSourceTimestamp = -1f;
                previousCameraRecoveryBlend = -1f;
            }

            if (previousSourceTimestamp >= 0f)
            {
                Require(
                    source + 0.001f >= previousSourceTimestamp,
                    "Replay source order moved backwards inside a loop.");
                observedSourceAdvance |=
                    source > previousSourceTimestamp + 0.001f;
            }

            if (replay.CurrentPlaybackPhase ==
                StageReplayController.ReplayPlaybackPhase.DeadlineAftermath)
            {
                float recoveryBlend = replay.CurrentCameraRecoveryBlend;
                if (previousCameraRecoveryBlend >= 0f)
                {
                    Require(
                        recoveryBlend + 0.001f >=
                        previousCameraRecoveryBlend,
                        $"Deadline camera recovery moved backward: " +
                        $"{previousCameraRecoveryBlend:0.000} -> " +
                        $"{recoveryBlend:0.000}.");
                    observedAftermathRecoveryAdvance |=
                        recoveryBlend >
                        previousCameraRecoveryBlend + 0.001f;
                }

                previousCameraRecoveryBlend = recoveryBlend;
            }
            else
            {
                previousCameraRecoveryBlend = -1f;
            }

            previousPlaybackElapsed = elapsed;
            previousSourceTimestamp = source;
        }

        private static void Require(bool condition, string message)
        {
            if (!condition)
            {
                throw new InvalidOperationException(message);
            }
        }

        private static void FinishFailure(Exception exception)
        {
            SessionState.EraseBool(RunningKey);
            SessionState.EraseString(PhaseKey);
            DetachCallbacks();
            Debug.LogError($"Replay play-mode smoke test failed: {exception}");
            if (EditorApplication.isPlaying)
            {
                EditorApplication.isPlaying = false;
            }

            EditorApplication.Exit(1);
        }

        private static void FinishSuccess()
        {
            SessionState.EraseBool(RunningKey);
            SessionState.EraseString(PhaseKey);
            DetachCallbacks();
            Debug.Log("Replay play-mode smoke test passed.");
            EditorApplication.Exit(0);
        }
    }
}
