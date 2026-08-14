using System;
using System.Reflection;
using Deltatime.Combat;
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

        private static readonly CommandLineSmokeRunner Runner =
            new CommandLineSmokeRunner(
                RunningKey,
                Tick,
                HandlePlayModeStateChanged);
        private static double playStartedAt;
        private static bool setupComplete;
        private static bool slowStarted;
        private static bool slowReleased;
        private static bool animationSourceRemoved;
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
        private static Animator originalAnimator;
        private static CharacterAnimationController animationSource;
        private static int legacyBoneTransformsPerFrame;
        private static int trackedVisualsBeforeEquipment;

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
            Runner.OpenSceneAndEnterPlayMode(ScenePath);
        }

        private static void AttachCallbacks()
        {
            Runner.Attach();
        }

        private static void DetachCallbacks()
        {
            Runner.Detach();
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
            animationSourceRemoved = false;
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
            originalAnimator = null;
            animationSource = null;
            legacyBoneTransformsPerFrame = 0;
            trackedVisualsBeforeEquipment = 0;
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
                    DriveAnimationEvent(
                        "strong-slow AttackB",
                        "AttackB",
                        "Assets/_Project/AutomaticRifle.asset");
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
                    DriveAnimationEvent("post-freeze Roll", "Roll", null);
                    Require(!deadline.IsActive,
                        "Replay smoke could not release the strong-slow window.");
                }

                if (slowReleased && !animationSourceRemoved && elapsed >= 2.75d)
                {
                    animationSourceRemoved = true;
                    Require(animationSource != null,
                        "Replay smoke animation source is missing before removal.");
                    UnityEngine.Object.Destroy(animationSource);
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
                    Require(replay.HasAdvancedReplayProxyAnimator,
                        "Replay proxy Animator never advanced on the unscaled replay clock.");
                    Require(replay.HasReplayProxyStateTransition,
                        "Replay proxy Animator never entered a recorded animation state.");
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
            trackedVisualsBeforeEquipment = replay.TrackedVisualCount;
            DriveAnimationEvent(
                "normal-speed AttackA",
                "AttackA",
                "Assets/_Project/MeleeWeapon.asset");

            PlayerHealth player = UnityEngine.Object.FindFirstObjectByType<
                PlayerHealth>();
            Require(player != null, "Replay smoke player is missing.");
            player.transform.position += new Vector3(0.4f, 0f, 0.2f);
            HitFlash.Create(
                player.transform.position + Vector3.up * 0.2f,
                Color.magenta);
            setupComplete = true;
        }

        private static void DriveAnimationEvent(
            string phase,
            string triggerName,
            string weaponAssetPath)
        {
            CharacterAnimationController[] animationControllers =
                UnityEngine.Object.FindObjectsByType<
                    CharacterAnimationController>(
                    FindObjectsInactive.Exclude,
                    FindObjectsSortMode.None);
            bool eventTriggered = false;
            for (int i = 0; i < animationControllers.Length; i++)
            {
                CharacterAnimationController controller =
                    animationControllers[i];
                if (controller.Animator == null ||
                    !HasAnimatorParameter(controller.Animator, triggerName))
                {
                    continue;
                }

                originalAnimator ??= controller.Animator;
                animationSource ??= controller;
                if (weaponAssetPath != null)
                {
                    WeaponDefinition definition =
                        AssetDatabase.LoadAssetAtPath<
                            WeaponDefinition>(weaponAssetPath);
                    WeaponController weapon =
                        controller.GetComponent<WeaponController>();
                    Require(definition != null && weapon != null,
                        $"Replay smoke weapon is missing: {weaponAssetPath}.");
                    weapon.Equip(
                        definition,
                        definition.AmmunitionCapacity);
                }

                controller.SetFloatParameter(
                    Animator.StringToHash("MoveX"),
                    triggerName == "Roll" ? -0.5f : 0.75f);
                controller.SetFloatParameter(
                    Animator.StringToHash("MoveY"),
                    triggerName == "Roll" ? 0.8f : 0.35f);
                controller.SetTriggerParameter(
                    Animator.StringToHash(triggerName));
                SkinnedMeshRenderer[] skinned =
                    controller.VisualRoot.GetComponentsInChildren<
                        SkinnedMeshRenderer>(true);
                legacyBoneTransformsPerFrame = 0;
                for (int rendererIndex = 0;
                     rendererIndex < skinned.Length;
                     rendererIndex++)
                {
                    legacyBoneTransformsPerFrame +=
                        skinned[rendererIndex].bones.Length;
                }

                eventTriggered = true;
                break;
            }

            Require(eventTriggered,
                $"Replay smoke could not trigger {phase}.");
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
                replay.RecordedAnimatedPoseCount == 0 &&
                replay.HasRecordedAnimatedMotion &&
                replay.ActiveAnimatedReplayVisualCount >= 1,
                $"Animator replay proxy is incomplete: tracked=" +
                $"{replay.TrackedAnimatedVisualCount}, bonePoses=" +
                $"{replay.RecordedAnimatedPoseCount}, events=" +
                $"{replay.RecordedAnimationEventCount}, active=" +
                $"{replay.ActiveAnimatedReplayVisualCount}.");
            ReplayMemoryStatistics memory = replay.GetMemoryStatistics();
            Require(
                memory.BonePoseCount == 0 &&
                memory.TrackedActorCount >= 1 &&
                memory.AnimationEventCount >= 7 &&
                replay.RecordedAnimationControllerChangeCount >= 2 &&
                memory.AnimationCheckpointCount >= 2 &&
                memory.AnimationTransformSampleCount >= 1 &&
                memory.EstimatedBytes > 0 &&
                legacyBoneTransformsPerFrame > 0 &&
                replay.TrackedVisualCount >=
                trackedVisualsBeforeEquipment + 2,
                $"Replay memory diagnostics are incomplete: actors=" +
                $"{memory.TrackedActorCount}, events={memory.AnimationEventCount}, " +
                $"checkpoints={memory.AnimationCheckpointCount}, transforms=" +
                $"{memory.AnimationTransformSampleCount}, bones=" +
                $"{memory.BonePoseCount}, controllerChanges=" +
                $"{replay.RecordedAnimationControllerChangeCount}, " +
                $"visuals={replay.TrackedVisualCount}, " +
                $"visualsBeforeEquipment={trackedVisualsBeforeEquipment}, " +
                $"legacyBonesPerFrame={legacyBoneTransformsPerFrame}.");
            Require(
                replay.HasRecordedAnimationTrigger(
                    Animator.StringToHash("AttackA")) &&
                replay.HasRecordedAnimationTrigger(
                    Animator.StringToHash("AttackB")) &&
                replay.HasRecordedAnimationTrigger(
                    Animator.StringToHash("Roll")),
                "Replay did not retain AttackA, AttackB, and Roll trigger order.");
            Animator proxyAnimator = replay.FirstReplayProxyAnimator;
            Require(
                animationSourceRemoved && animationSource == null &&
                proxyAnimator != null &&
                proxyAnimator != originalAnimator &&
                ReplayAnimatorProxyRegistry.IsProxy(proxyAnimator) &&
                !ReplayAnimatorProxyRegistry.IsProxy(originalAnimator) &&
                proxyAnimator.updateMode == AnimatorUpdateMode.UnscaledTime &&
                Mathf.Approximately(proxyAnimator.speed, 0f) &&
                replay.AreReplayProxyGameplayComponentsDisabled,
                "Replay animation did not survive source removal on a distinct " +
                "manual-unscaled proxy Animator.");
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
