using System;
using System.Reflection;
using Deltatime.Player;
using Deltatime.Replay;
using Deltatime.TimeSystem;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Deltatime.EditorTools
{
    [InitializeOnLoad]
    public static class DeadlineVisualFeedbackPlayModeSmokeTest
    {
        private const string Stage1ScenePath =
            "Assets/_Project/Scenes/Stage1.unity";
        private const string RunningKey =
            "Deltatime.DeadlineVisualSmoke.Running";
        private const string FailedKey =
            "Deltatime.DeadlineVisualSmoke.Failed";
        private const string FailureKey =
            "Deltatime.DeadlineVisualSmoke.Failure";
        private const double TestTimeout = 75d;

        private static readonly string[] AttachmentSceneNames =
        {
            "Tutorial",
            "Stage2",
            "Stage5",
            "Stage6"
        };

        private static readonly MethodInfo ActivateDeadline =
            typeof(DeadlineController).GetMethod(
                "ActivateDeadline",
                BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly MethodInfo ReleaseDeadline =
            typeof(DeadlineController).GetMethod(
                "ReleaseDeadline",
                BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly MethodInfo DisableLiveSimulation =
            typeof(StageReplayController).GetMethod(
                "DisableLiveSimulation",
                BindingFlags.Instance | BindingFlags.NonPublic);

        private static double playModeStartedAt;
        private static double phaseStartedAt;
        private static int phase;
        private static int attachmentSceneIndex;
        private static DeadlineController deadline;
        private static DeadlineVisualFeedback visualFeedback;

        static DeadlineVisualFeedbackPlayModeSmokeTest()
        {
            if (SessionState.GetBool(RunningKey, false))
            {
                Attach();
            }
        }

        public static void RunFromCommandLine()
        {
            if (SessionState.GetBool(RunningKey, false))
            {
                return;
            }

            EditorSceneManager.OpenScene(Stage1ScenePath, OpenSceneMode.Single);
            SessionState.SetBool(RunningKey, true);
            SessionState.SetBool(FailedKey, false);
            SessionState.SetString(FailureKey, string.Empty);
            Attach();
            EditorApplication.EnterPlaymode();
        }

        private static void Attach()
        {
            EditorApplication.update -= Tick;
            EditorApplication.update += Tick;
            EditorApplication.playModeStateChanged -= HandlePlayModeChanged;
            EditorApplication.playModeStateChanged += HandlePlayModeChanged;
        }

        private static void HandlePlayModeChanged(PlayModeStateChange state)
        {
            if (!SessionState.GetBool(RunningKey, false))
            {
                return;
            }

            if (state == PlayModeStateChange.EnteredPlayMode)
            {
                playModeStartedAt = EditorApplication.timeSinceStartup;
                phaseStartedAt = playModeStartedAt;
                phase = 0;
                attachmentSceneIndex = 0;
                deadline = null;
                visualFeedback = null;
            }
            else if (state == PlayModeStateChange.EnteredEditMode)
            {
                Finish();
            }
        }

        private static void Tick()
        {
            if (!SessionState.GetBool(RunningKey, false) ||
                !EditorApplication.isPlaying)
            {
                return;
            }

            double now = EditorApplication.timeSinceStartup;
            if (now - playModeStartedAt > TestTimeout)
            {
                Fail(new TimeoutException(
                    "DEADLINE visual smoke exceeded its 75-second timeout."));
                return;
            }

            try
            {
                switch (phase)
                {
                    case 0:
                        if (now - playModeStartedAt < 0.5d)
                        {
                            return;
                        }

                        ResolveAndValidateAttachment("Stage1");
                        Require(
                            ActivateDeadline != null && ReleaseDeadline != null,
                            "Deadline activation or release method could not be resolved.");
                        ActivateDeadline.Invoke(deadline, null);
                        Require(deadline.IsActive,
                            "DEADLINE did not activate for visual validation.");
                        Require(
                            visualFeedback.CurrentPhase ==
                            DeadlineVisualFeedback.VisualPhase.Entering &&
                            visualFeedback.IsVisualActive,
                            "DEADLINE entry did not start the visual transition.");
                        AdvancePhase(1);
                        break;

                    case 1:
                        if (now - phaseStartedAt < 0.04d)
                        {
                            return;
                        }

                        Require(
                            visualFeedback.EffectBlend > 0f &&
                            visualFeedback.EffectBlend < 1f,
                            "DEADLINE entry blend did not advance in unscaled time.");
                        Require(deadline.RegisterStagedAction(),
                            "DEADLINE rejected the first staged action.");
                        Require(deadline.RegisterStagedAction(),
                            "DEADLINE rejected the second staged action.");
                        Require(!deadline.RegisterStagedAction(),
                            "DEADLINE accepted more than two staged actions.");
                        Require(
                            visualFeedback.DisplayedActionCount == 2 &&
                            deadline.RejectedActionFeedback,
                            "DEADLINE action nodes or rejected feedback did not update.");
                        AdvancePhase(2);
                        break;

                    case 2:
                        if (now - phaseStartedAt < 0.14d)
                        {
                            return;
                        }

                        Require(
                            visualFeedback.CurrentPhase ==
                            DeadlineVisualFeedback.VisualPhase.Active &&
                            visualFeedback.EffectBlend >= 0.999f,
                            "DEADLINE visual did not settle into its active state.");
                        ReleaseDeadline.Invoke(deadline, null);
                        Require(
                            !deadline.IsActive &&
                            deadline.ReleasedThisFrame &&
                            visualFeedback.CurrentPhase ==
                            DeadlineVisualFeedback.VisualPhase.Releasing,
                            "Normal DEADLINE release did not start the restore wave.");
                        AdvancePhase(3);
                        break;

                    case 3:
                        if (now - phaseStartedAt < 0.08d)
                        {
                            return;
                        }

                        Require(
                            visualFeedback.CurrentPhase ==
                            DeadlineVisualFeedback.VisualPhase.Releasing &&
                            visualFeedback.EffectBlend > 0f &&
                            visualFeedback.EffectBlend < 1f,
                            "DEADLINE release blend did not advance in unscaled time.");
                        AdvancePhase(4);
                        break;

                    case 4:
                        if (now - phaseStartedAt < 0.22d)
                        {
                            return;
                        }

                        Require(
                            visualFeedback.CurrentPhase ==
                            DeadlineVisualFeedback.VisualPhase.Inactive &&
                            Mathf.Approximately(visualFeedback.EffectBlend, 0f),
                            "DEADLINE visual did not return to its inactive state.");
                        Require(Mathf.Approximately(UnityEngine.Time.timeScale, 1f),
                            "DEADLINE visual changed global Time.timeScale.");
                        ActivateDeadline.Invoke(deadline, null);
                        AdvancePhase(5);
                        break;

                    case 5:
                        if (now - phaseStartedAt < 0.03d)
                        {
                            return;
                        }

                        Require(deadline.IsActive && visualFeedback.IsVisualActive,
                            "DEADLINE abort setup did not activate.");
                        deadline.enabled = false;
                        Require(
                            !deadline.IsActive &&
                            visualFeedback.CurrentPhase ==
                            DeadlineVisualFeedback.VisualPhase.Inactive &&
                            Mathf.Approximately(visualFeedback.EffectBlend, 0f),
                            "Aborted DEADLINE played a release transition instead of resetting.");
                        deadline.enabled = true;
                        BeginAttachmentSceneLoad();
                        break;

                    case 6:
                        if (now - phaseStartedAt < 0.5d)
                        {
                            return;
                        }

                        string expectedScene =
                            AttachmentSceneNames[attachmentSceneIndex];
                        Require(
                            SceneManager.GetActiveScene().name == expectedScene,
                            $"Expected {expectedScene}, found " +
                            $"{SceneManager.GetActiveScene().name}.");
                        ResolveAndValidateAttachment(expectedScene);

                        attachmentSceneIndex++;
                        if (attachmentSceneIndex < AttachmentSceneNames.Length)
                        {
                            BeginAttachmentSceneLoad();
                            return;
                        }

                        ValidateReplayDisablesVisual();
                        Require(Mathf.Approximately(UnityEngine.Time.timeScale, 1f),
                            "Scene attachment validation changed global Time.timeScale.");
                        Debug.Log(
                            "DEADLINE visual PlayMode smoke passed: entry, hold, " +
                            "two action nodes, rejection, release, abort, scene " +
                            "attachment, shader readiness, and replay disablement.");
                        EditorApplication.ExitPlaymode();
                        break;
                }
            }
            catch (TargetInvocationException exception)
            {
                Fail(exception.InnerException ?? exception);
            }
            catch (Exception exception)
            {
                Fail(exception);
            }
        }

        private static void ResolveAndValidateAttachment(string sceneName)
        {
            Camera camera = Camera.main;
            deadline = UnityEngine.Object.FindFirstObjectByType<
                DeadlineController>();
            visualFeedback = camera == null
                ? null
                : camera.GetComponent<DeadlineVisualFeedback>();
            WorldTimeVisualFeedback worldVisual = camera == null
                ? null
                : camera.GetComponent<WorldTimeVisualFeedback>();

            Require(camera != null && deadline != null && worldVisual != null,
                $"{sceneName} is missing its gameplay camera, DEADLINE, or world visual.");
            Require(visualFeedback != null && visualFeedback.enabled,
                $"{sceneName} did not attach DeadlineVisualFeedback at runtime.");
            Require(visualFeedback.IsShaderReady,
                $"{sceneName} could not load the DEADLINE screen-effect shader.");
            Require(
                visualFeedback.CurrentPhase ==
                DeadlineVisualFeedback.VisualPhase.Inactive &&
                Mathf.Approximately(visualFeedback.EffectBlend, 0f),
                $"{sceneName} initialized with a stale DEADLINE visual state.");
        }

        private static void BeginAttachmentSceneLoad()
        {
            string sceneName = AttachmentSceneNames[attachmentSceneIndex];
            SceneManager.LoadScene(sceneName, LoadSceneMode.Single);
            phase = 6;
            phaseStartedAt = EditorApplication.timeSinceStartup;
        }

        private static void ValidateReplayDisablesVisual()
        {
            StageReplayController replay =
                UnityEngine.Object.FindFirstObjectByType<StageReplayController>();
            Require(replay != null && DisableLiveSimulation != null,
                "Replay live-simulation disable path could not be resolved.");
            DisableLiveSimulation.Invoke(replay, null);
            Require(
                visualFeedback != null &&
                !visualFeedback.enabled &&
                visualFeedback.CurrentPhase ==
                DeadlineVisualFeedback.VisualPhase.Inactive,
                "Replay did not disable and reset the live DEADLINE visual.");
        }

        private static void AdvancePhase(int nextPhase)
        {
            phase = nextPhase;
            phaseStartedAt = EditorApplication.timeSinceStartup;
        }

        private static void Fail(Exception exception)
        {
            SessionState.SetBool(FailedKey, true);
            SessionState.SetString(FailureKey, exception.ToString());
            Debug.LogException(exception);
            EditorApplication.ExitPlaymode();
        }

        private static void Finish()
        {
            bool failed = SessionState.GetBool(FailedKey, false);
            string failure = SessionState.GetString(FailureKey, string.Empty);
            SessionState.EraseBool(RunningKey);
            SessionState.EraseBool(FailedKey);
            SessionState.EraseString(FailureKey);
            EditorApplication.update -= Tick;
            EditorApplication.playModeStateChanged -= HandlePlayModeChanged;

            if (failed)
            {
                Debug.LogError(
                    $"DEADLINE visual PlayMode smoke failed: {failure}");
                EditorApplication.Exit(1);
                return;
            }

            EditorApplication.Exit(0);
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
