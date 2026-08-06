using System;
using System.Collections.Generic;
using System.Reflection;
using Deltatime.Performance;
using Deltatime.Replay;
using Deltatime.Vision;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Deltatime.EditorTools
{
    /// <summary>
    /// A reproducible Stage 6-only play-mode measurement. It records CPU timing on
    /// every sample and records GPU timing when the active editor/GPU exposes it.
    /// The latter is intentionally reported as unverifiable rather than guessed.
    /// </summary>
    [InitializeOnLoad]
    public static class Stage6PerformanceBenchmark
    {
        private const string ScenePath = "Assets/_Project/Scenes/Stage6.unity";
        private const int TargetWidth = 1920;
        private const int TargetHeight = 1080;
        private const int WarmupFrames = 90;
        private const int SampleFrames = 300;
        private const double TimeoutSeconds = 60d;
        private const float TargetFrameMilliseconds = 16.7f;
        private const string RunningKey = "Deltatime.Stage6Benchmark.Running";
        private const string FailedKey = "Deltatime.Stage6Benchmark.Failed";
        private const string FailureKey = "Deltatime.Stage6Benchmark.Failure";
        private const string PhaseKey = "Deltatime.Stage6Benchmark.Phase";

        private static readonly List<double> cpuFrameTimes = new List<double>(SampleFrames);
        private static readonly List<double> gpuFrameTimes = new List<double>(SampleFrames);
        private static readonly FrameTiming[] latestTimings = new FrameTiming[1];

        private static bool callbacksAttached;
        private static int frameCount;
        private static bool resolutionRequested;
        private static bool runtimeConfigurationCaptured;
        private static int measuredWidth;
        private static int measuredHeight;
        private static int measuredRendererCount;
        private static int measuredEnvironmentShadowedPoints;
        private static int measuredVisionSoftLights;
        private static int measuredReplayDynamicRootCount;
        private static float measuredFallbackInterval;
        private static string gameViewResolutionStatus;
        private static double playStartedAt;

        static Stage6PerformanceBenchmark()
        {
            if (SessionState.GetBool(RunningKey, false))
            {
                AttachCallbacks();
            }
        }

        [MenuItem("Tools/Prototype/Benchmark Stage 6 - Neon Overlook")]
        public static void RunFromCommandLine()
        {
            SessionState.SetBool(RunningKey, true);
            SessionState.SetBool(FailedKey, false);
            SessionState.SetString(FailureKey, string.Empty);
            SessionState.SetString(PhaseKey, "entering");
            cpuFrameTimes.Clear();
            gpuFrameTimes.Clear();
            frameCount = 0;
            resolutionRequested = false;
            runtimeConfigurationCaptured = false;
            measuredWidth = 0;
            measuredHeight = 0;
            measuredRendererCount = 0;
            measuredEnvironmentShadowedPoints = -1;
            measuredVisionSoftLights = -1;
            measuredReplayDynamicRootCount = 0;
            measuredFallbackInterval = 0f;
            gameViewResolutionStatus = "pending";
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
                SessionState.SetString(PhaseKey, "playing");
                FrameTimingManager.CaptureFrameTimings();
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

            if (!resolutionRequested)
            {
                resolutionRequested = true;
                Screen.SetResolution(TargetWidth, TargetHeight, FullScreenMode.Windowed);
                gameViewResolutionStatus = ConfigureGameViewResolution();
            }

            if (EditorApplication.timeSinceStartup - playStartedAt > TimeoutSeconds)
            {
                RecordFailure("Stage6 performance benchmark timed out.");
                EditorApplication.isPlaying = false;
                return;
            }

            FrameTimingManager.CaptureFrameTimings();
            frameCount++;
            if (frameCount <= WarmupFrames)
            {
                return;
            }

            if (!runtimeConfigurationCaptured)
            {
                CaptureRuntimeConfiguration();
            }

            CaptureSample();
            if (cpuFrameTimes.Count >= SampleFrames)
            {
                EditorApplication.isPlaying = false;
            }
        }

        private static void CaptureSample()
        {
            double cpuMilliseconds = Math.Max(0.0, Time.unscaledDeltaTime * 1000.0);
            uint timingCount = FrameTimingManager.GetLatestTimings(1, latestTimings);
            if (timingCount > 0)
            {
                FrameTiming timing = latestTimings[0];
                if (timing.cpuFrameTime > 0.0)
                {
                    cpuMilliseconds = timing.cpuFrameTime;
                }

                if (timing.gpuFrameTime > 0.0)
                {
                    gpuFrameTimes.Add(timing.gpuFrameTime);
                }
            }

            cpuFrameTimes.Add(cpuMilliseconds);
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
                Debug.LogError("Stage6 performance benchmark failed:\n" + failure);
                EditorApplication.Exit(1);
                return;
            }

            LogResult();
            EditorApplication.Exit(0);
        }

        private static void LogResult()
        {
            double cpuAverage = Average(cpuFrameTimes);
            double cpuP95 = Percentile95(cpuFrameTimes);

            string gpuSummary = gpuFrameTimes.Count == 0
                ? "GPU timing=unavailable"
                : $"GPU avg={Average(gpuFrameTimes):F2}ms, " +
                  $"p95={Percentile95(gpuFrameTimes):F2}ms, " +
                  $"samples={gpuFrameTimes.Count}";
            string lowResolutionResult = string.Empty;
            bool isTargetResolution = measuredWidth == TargetWidth &&
                measuredHeight == TargetHeight;
            string result;
            if (!isTargetResolution)
            {
                if (gpuFrameTimes.Count > 0)
                {
                    double gpuAverage = Average(gpuFrameTimes);
                    double gpuP95 = Percentile95(gpuFrameTimes);
                    lowResolutionResult = gpuAverage <= TargetFrameMilliseconds &&
                        gpuP95 <= TargetFrameMilliseconds &&
                        cpuAverage <= TargetFrameMilliseconds &&
                        cpuP95 <= TargetFrameMilliseconds
                        ? " (non-1080p sample passed)"
                        : " (non-1080p sample missed 16.7ms)";
                }

                result = "1080p 60 FPS 판정=확인 불가 (actual resolution mismatch)";
            }
            else if (gpuFrameTimes.Count == 0)
            {
                result = "60 FPS 판정=확인 불가 (GPU frame timing unavailable)";
            }
            else
            {
                double gpuAverage = Average(gpuFrameTimes);
                double gpuP95 = Percentile95(gpuFrameTimes);
                bool meetsTarget = cpuAverage <= TargetFrameMilliseconds &&
                    cpuP95 <= TargetFrameMilliseconds &&
                    gpuAverage <= TargetFrameMilliseconds &&
                    gpuP95 <= TargetFrameMilliseconds;
                result = meetsTarget
                    ? "60 FPS 판정=통과"
                    : "60 FPS 판정=목표 미달";
            }

            Debug.Log(
                "Stage6 performance benchmark: " +
                $"requested={TargetWidth}x{TargetHeight}, actual={measuredWidth}x{measuredHeight}, " +
                $"game view={gameViewResolutionStatus}, " +
                $"warmup={WarmupFrames}, samples={cpuFrameTimes.Count}, " +
                $"CPU avg={cpuAverage:F2}ms, p95={cpuP95:F2}ms, {gpuSummary}, " +
                $"renderers={measuredRendererCount}, environment shadow points=" +
                $"{measuredEnvironmentShadowedPoints}, vision soft lights=" +
                $"{measuredVisionSoftLights}, replay dynamic roots=" +
                $"{measuredReplayDynamicRootCount}, fallback={measuredFallbackInterval:F2}s, " +
                $"{result}{lowResolutionResult}.");
        }

        private static void CaptureRuntimeConfiguration()
        {
            Stage6PerformanceController performance =
                UnityEngine.Object.FindFirstObjectByType<Stage6PerformanceController>();
            StageReplayController replay =
                UnityEngine.Object.FindFirstObjectByType<StageReplayController>();
            measuredWidth = Screen.width;
            measuredHeight = Screen.height;
            measuredRendererCount = UnityEngine.Object.FindObjectsByType<Renderer>(
                FindObjectsSortMode.None).Length;
            measuredEnvironmentShadowedPoints = performance == null
                ? -1
                : performance.ActiveEnvironmentShadowedPointLightCount;
            measuredVisionSoftLights = CountSoftVisionLights();
            measuredReplayDynamicRootCount = replay == null
                ? 0
                : replay.RendererDiscoveryRootCount;
            measuredFallbackInterval = replay == null
                ? 0f
                : replay.FallbackRendererDiscoveryInterval;
            runtimeConfigurationCaptured = true;
        }

        private static string ConfigureGameViewResolution()
        {
            try
            {
                Assembly editorAssembly = typeof(EditorWindow).Assembly;
                Type gameViewType = editorAssembly.GetType("UnityEditor.GameView");
                Type gameViewSizesType =
                    editorAssembly.GetType("UnityEditor.GameViewSizes");
                if (gameViewType == null || gameViewSizesType == null)
                {
                    return "unavailable";
                }

                EditorWindow gameView = EditorWindow.GetWindow(gameViewType);
                PropertyInfo instanceProperty = gameViewSizesType.GetProperty(
                    "instance",
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
                object sizes = instanceProperty?.GetValue(null);
                MethodInfo getGroup = gameViewSizesType.GetMethod(
                    "GetGroup",
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (gameView == null || sizes == null || getGroup == null)
                {
                    return "unavailable";
                }

                Type groupType = getGroup.GetParameters()[0].ParameterType;
                object standaloneGroup = Enum.Parse(groupType, "Standalone");
                object group = getGroup.Invoke(sizes, new[] { standaloneGroup });
                MethodInfo totalCount = group?.GetType().GetMethod(
                    "GetTotalCount",
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                MethodInfo getSize = group?.GetType().GetMethod(
                    "GetGameViewSize",
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (group == null || totalCount == null || getSize == null)
                {
                    return "unavailable";
                }

                int count = (int)totalCount.Invoke(group, null);
                int matchedIndex = -1;
                for (int i = 0; i < count; i++)
                {
                    object size = getSize.Invoke(group, new object[] { i });
                    PropertyInfo width = size?.GetType().GetProperty(
                        "width",
                        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                    PropertyInfo height = size?.GetType().GetProperty(
                        "height",
                        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                    if (width != null && height != null &&
                        (int)width.GetValue(size) == TargetWidth &&
                        (int)height.GetValue(size) == TargetHeight)
                    {
                        matchedIndex = i;
                        break;
                    }
                }

                if (matchedIndex < 0)
                {
                    return "1920x1080 preset unavailable";
                }

                PropertyInfo selectedSize = gameViewType.GetProperty(
                    "selectedSizeIndex",
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (selectedSize != null)
                {
                    selectedSize.SetValue(gameView, matchedIndex);
                    gameView.Repaint();
                    return "1920x1080 preset selected";
                }

                FieldInfo selectedSizeField = gameViewType.GetField(
                    "m_SelectedSizeIndex",
                    BindingFlags.NonPublic | BindingFlags.Instance);
                if (selectedSizeField != null)
                {
                    selectedSizeField.SetValue(gameView, matchedIndex);
                    gameView.Repaint();
                    return "1920x1080 preset selected";
                }

                return "1920x1080 selection API unavailable";
            }
            catch (Exception exception)
            {
                return "resolution setup unavailable: " + exception.GetType().Name;
            }
        }

        private static int CountSoftVisionLights()
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

            return count;
        }

        private static double Average(List<double> values)
        {
            if (values.Count == 0)
            {
                return 0.0;
            }

            double sum = 0.0;
            for (int i = 0; i < values.Count; i++)
            {
                sum += values[i];
            }

            return sum / values.Count;
        }

        private static double Percentile95(List<double> values)
        {
            if (values.Count == 0)
            {
                return 0.0;
            }

            List<double> sorted = new List<double>(values);
            sorted.Sort();
            int index = Mathf.Clamp(
                Mathf.CeilToInt(sorted.Count * 0.95f) - 1,
                0,
                sorted.Count - 1);
            return sorted[index];
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
    }
}
