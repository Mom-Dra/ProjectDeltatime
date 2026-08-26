using System;
using System.IO;
using Deltatime.Replay;
using Deltatime.TimeSystem;
using UnityEditor;
using UnityEngine;

namespace Deltatime.EditorTools
{
    [InitializeOnLoad]
    public static class WorldTimeAmbientReplayPlayModeSmokeTest
    {
        private const string ScenePath =
            "Assets/_Project/Scenes/Stage2.unity";
        private const string RunningKey =
            "Deltatime.WorldTimeAmbientReplaySmoke.Running";
        private const string PhaseKey =
            "Deltatime.WorldTimeAmbientReplaySmoke.Phase";

        private static readonly CommandLineSmokeRunner Runner =
            new CommandLineSmokeRunner(
                RunningKey,
                Tick,
                HandlePlayModeStateChanged);

        private static double playStartedAt;
        private static bool setupComplete;
        private static bool replayRequested;
        private static bool firstReplayFrameCaptured;
        private static StageReplayController replay;
        private static WorldTimeActivity activity;
        private static WorldTimeAmbientAnchor anchor;
        private static Renderer liveRotor;
        private static Renderer replayRotor;
        private static Camera gameplayCamera;
        private static Quaternion firstReplayRotation;

        static WorldTimeAmbientReplayPlayModeSmokeTest()
        {
            if (SessionState.GetBool(RunningKey, false))
            {
                Runner.Attach();
            }
        }

        public static void RunFromCommandLine()
        {
            SessionState.SetBool(RunningKey, true);
            SessionState.SetString(PhaseKey, "entering");
            Runner.OpenSceneAndEnterPlayMode(ScenePath);
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
            replayRequested = false;
            firstReplayFrameCaptured = false;
            replay = null;
            activity = null;
            anchor = null;
            liveRotor = null;
            replayRotor = null;
            gameplayCamera = null;
            firstReplayRotation = Quaternion.identity;
        }

        private static void Tick()
        {
            if (!SessionState.GetBool(RunningKey, false))
            {
                Runner.Detach();
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
                    Setup();
                }

                if (setupComplete && !replayRequested && elapsed >= 1.4d)
                {
                    replayRequested = true;
                    Require(replay.RequestReplay(),
                        "Stage2 ambient replay request was rejected.");
                }

                if (replayRequested &&
                    !firstReplayFrameCaptured &&
                    elapsed >= 1.65d)
                {
                    ValidateReplayStart();
                    firstReplayFrameCaptured = true;
                }

                if (firstReplayFrameCaptured && elapsed >= 1.95d)
                {
                    ValidateReplayAdvance();
                    SessionState.SetString(PhaseKey, "stopping");
                    EditorApplication.isPlaying = false;
                    return;
                }

                if (elapsed >= 6d)
                {
                    throw new TimeoutException(
                        "Stage2 ambient replay smoke timed out.");
                }
            }
            catch (Exception exception)
            {
                FinishFailure(exception);
            }
        }

        private static void Setup()
        {
            replay = UnityEngine.Object.FindFirstObjectByType<
                StageReplayController>();
            activity = UnityEngine.Object.FindFirstObjectByType<
                WorldTimeActivity>();
            gameplayCamera = UnityEngine.Object.FindFirstObjectByType<Camera>();
            WorldTimeAmbientAnchor[] anchors =
                UnityEngine.Object.FindObjectsByType<WorldTimeAmbientAnchor>(
                    FindObjectsInactive.Exclude,
                    FindObjectsSortMode.None);
            Require(
                replay != null && activity != null &&
                gameplayCamera != null && anchors.Length == 2,
                $"Stage2 ambient replay dependencies are incomplete: " +
                $"replay={replay != null}, activity={activity != null}, " +
                $"camera={gameplayCamera != null}, anchors={anchors.Length}.");

            anchor = anchors[0];
            SerializedObject serializedAnchor = new SerializedObject(anchor);
            Transform rotatingPart = serializedAnchor
                .FindProperty("rotatingPart")
                .objectReferenceValue as Transform;
            liveRotor = rotatingPart != null
                ? rotatingPart.GetComponent<Renderer>()
                : null;
            Require(
                rotatingPart != null &&
                rotatingPart.GetComponent<ReplayIncluded>() != null &&
                liveRotor != null &&
                replay.TrackedExcludedVisualCount == 0,
                "Stage2 fan rotor was not included as a replayable renderer.");

            activity.Pulse(1f, 2f);
            setupComplete = true;
        }

        private static void ValidateReplayStart()
        {
            Require(
                replay.IsReplaying &&
                !anchor.enabled &&
                !anchor.IsLoopPlaying &&
                !liveRotor.enabled &&
                replay.TrackedExcludedVisualCount == 0,
                "Stage2 ambient replay did not hide live rotation or stop " +
                "the live loop cleanly.");

            Renderer[] renderers =
                UnityEngine.Object.FindObjectsByType<Renderer>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None);
            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer candidate = renderers[i];
                if (candidate.name ==
                    $"Replay - {liveRotor.gameObject.name}" &&
                    candidate.enabled &&
                    candidate.gameObject.activeInHierarchy)
                {
                    replayRotor = candidate;
                    break;
                }
            }

            Require(replayRotor != null,
                "Stage2 fan rotor replay proxy is not visible.");
            firstReplayRotation = replayRotor.transform.rotation;
            CaptureRotor("01");
        }

        private static void ValidateReplayAdvance()
        {
            Require(
                replayRotor != null &&
                replayRotor.enabled &&
                replayRotor.gameObject.activeInHierarchy,
                "Stage2 fan rotor replay proxy disappeared.");
            float angle = Quaternion.Angle(
                firstReplayRotation,
                replayRotor.transform.rotation);
            Require(angle > 0.5f,
                $"Stage2 fan rotor replay proxy did not advance: " +
                $"{angle:0.000} degrees.");
            CaptureRotor("02");
            Debug.Log(
                $"World-time ambient Stage2 replay smoke passed: " +
                $"rotor advanced {angle:0.000} degrees, live audio stopped.");
        }

        private static void CaptureRotor(string suffix)
        {
            Vector3 target = replayRotor.transform.position;
            Vector3 position = target + new Vector3(2.6f, 2.2f, 2.6f);
            gameplayCamera.transform.SetPositionAndRotation(
                position,
                Quaternion.LookRotation(target - position, Vector3.up));
            gameplayCamera.fieldOfView = 48f;
            string outputPath = Path.Combine(
                Path.GetTempPath(),
                $"ProjectDeltatime-WorldTimeAmbient-Replay-{suffix}.png");
            GameObject lightObject =
                new GameObject("Ambient Replay Capture Light");
            Light captureLight = lightObject.AddComponent<Light>();
            captureLight.type = LightType.Directional;
            captureLight.intensity = 2f;
            captureLight.shadows = LightShadows.None;
            lightObject.transform.rotation = Quaternion.Euler(45f, -35f, 0f);
            try
            {
                PreviewCapture.CapturePng(
                    gameplayCamera,
                    960,
                    720,
                    outputPath);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(lightObject);
            }
            Debug.Log($"World-time ambient replay captured: {outputPath}");
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
            Runner.Detach();
            Debug.LogError(
                $"World-time ambient Stage2 replay smoke failed: " +
                $"{exception}");
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
            Runner.Detach();
            EditorApplication.Exit(0);
        }
    }
}
