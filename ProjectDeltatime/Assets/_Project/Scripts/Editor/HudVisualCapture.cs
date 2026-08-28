using System;
using System.IO;
using System.Reflection;
using Deltatime.Tutorial;
using Deltatime.UI;
using UnityEditor;
using UnityEngine;

namespace Deltatime.EditorTools
{
    [InitializeOnLoad]
    public static class HudVisualCapture
    {
        private const string RunningKey = "Deltatime.HudCapture.Running";
        private const string FailureKey = "Deltatime.HudCapture.Failure";
        private const string IndexKey = "Deltatime.HudCapture.Index";
        private const string PhaseKey = "Deltatime.HudCapture.Phase";

        private static readonly CaptureRequest[] Requests =
        {
            new CaptureRequest(
                "Assets/_Project/Scenes/Stage1.unity",
                "Stage1",
                1920,
                1080),
            new CaptureRequest(
                "Assets/_Project/Scenes/Stage1.unity",
                "Stage1",
                1280,
                720),
            new CaptureRequest(
                GameBuildSceneCatalog.TutorialScenePath,
                "Tutorial",
                1920,
                1080),
            new CaptureRequest(
                GameBuildSceneCatalog.TutorialScenePath,
                "Tutorial",
                1280,
                720)
        };

        private static readonly CommandLineSmokeRunner Runner =
            new CommandLineSmokeRunner(
                RunningKey,
                Tick,
                HandlePlayModeStateChanged);

        private static double playStartedAt;
        private static double captureRequestedAt;
        private static bool captureRequested;
        private static DateTime captureRequestUtc;

        static HudVisualCapture()
        {
            if (!SessionState.GetBool(RunningKey, false))
            {
                return;
            }

            Runner.Attach();
            EditorApplication.delayCall += ResumePendingCapture;
        }

        public static void CaptureAllFromCommandLine()
        {
            if (SessionState.GetBool(RunningKey, false))
            {
                Runner.Attach();
                EditorApplication.delayCall += ResumePendingCapture;
                return;
            }

            Directory.CreateDirectory(GetOutputDirectory());
            SessionState.SetBool(RunningKey, true);
            SessionState.SetString(FailureKey, string.Empty);
            SessionState.SetInt(IndexKey, 0);
            StartCurrentCapture();
        }

        private static void ResumePendingCapture()
        {
            if (!SessionState.GetBool(RunningKey, false) ||
                EditorApplication.isPlayingOrWillChangePlaymode)
            {
                return;
            }

            StartCurrentCapture();
        }

        private static void StartCurrentCapture()
        {
            int index = SessionState.GetInt(IndexKey, 0);
            if (index < 0 || index >= Requests.Length)
            {
                Finish();
                return;
            }

            CaptureRequest request = Requests[index];
            try
            {
                ConfigureGameViewResolution(request.Width, request.Height);
                SessionState.SetString(PhaseKey, "entering");
                Runner.OpenSceneAndEnterPlayMode(request.ScenePath);
            }
            catch (Exception exception)
            {
                RecordFailure(exception.ToString());
                Finish();
            }
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
                captureRequestedAt = 0d;
                captureRequested = false;
                SessionState.SetString(PhaseKey, "playing");
            }
            else if (state == PlayModeStateChange.ExitingPlayMode)
            {
                SessionState.SetString(PhaseKey, "stopping");
            }
            else if (state == PlayModeStateChange.EnteredEditMode &&
                     SessionState.GetString(PhaseKey, string.Empty) == "stopping")
            {
                if (!string.IsNullOrEmpty(
                        SessionState.GetString(FailureKey, string.Empty)))
                {
                    Finish();
                    return;
                }

                int nextIndex = SessionState.GetInt(IndexKey, 0) + 1;
                SessionState.SetInt(IndexKey, nextIndex);
                if (nextIndex >= Requests.Length)
                {
                    Finish();
                }
                else
                {
                    EditorApplication.delayCall += StartCurrentCapture;
                }
            }
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

            CaptureRequest request =
                Requests[SessionState.GetInt(IndexKey, 0)];
            double elapsed = EditorApplication.timeSinceStartup - playStartedAt;
            if (!captureRequested && elapsed >= 2d)
            {
                try
                {
                    ValidateHud(request.SceneLabel);
                    Require(
                        Screen.width == request.Width &&
                        Screen.height == request.Height,
                        $"Game View is {Screen.width}x{Screen.height}; " +
                        $"expected {request.Width}x{request.Height}.");
                    captureRequestUtc = DateTime.UtcNow;
                    ScreenCapture.CaptureScreenshot(GetOutputPath(request));
                    captureRequested = true;
                    captureRequestedAt = EditorApplication.timeSinceStartup;
                }
                catch (Exception exception)
                {
                    RecordFailure(exception.ToString());
                    EditorApplication.isPlaying = false;
                }
            }
            else if (captureRequested &&
                     EditorApplication.timeSinceStartup - captureRequestedAt >= 1.5d)
            {
                try
                {
                    ValidateCapture(request);
                }
                catch (Exception exception)
                {
                    RecordFailure(exception.ToString());
                }

                EditorApplication.isPlaying = false;
            }
            else if (elapsed >= 20d)
            {
                RecordFailure("HUD visual capture timed out.");
                EditorApplication.isPlaying = false;
            }
        }

        private static void ValidateHud(string sceneLabel)
        {
            if (sceneLabel == "Tutorial")
            {
                TutorialHud tutorialHud =
                    UnityEngine.Object.FindFirstObjectByType<TutorialHud>();
                Require(
                    tutorialHud != null &&
                    tutorialHud.isActiveAndEnabled &&
                    tutorialHud.HasRequiredVisualAssets,
                    "Tutorial HUD or its required icons are unavailable.");
                return;
            }

            GameHud gameHud =
                UnityEngine.Object.FindFirstObjectByType<GameHud>();
            Require(
                gameHud != null &&
                gameHud.isActiveAndEnabled &&
                gameHud.HasRequiredVisualAssets,
                "Game HUD or its required icons are unavailable.");
        }

        private static void ValidateCapture(CaptureRequest request)
        {
            string path = GetOutputPath(request);
            FileInfo file = new FileInfo(path);
            Require(file.Exists && file.Length > 0,
                $"HUD capture was not written: {path}");
            Require(file.LastWriteTimeUtc >= captureRequestUtc,
                $"HUD capture is stale: {path}");

            byte[] png = File.ReadAllBytes(path);
            Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            try
            {
                Require(ImageConversion.LoadImage(texture, png, true),
                    $"HUD capture is not a readable PNG: {path}");
                Require(
                    texture.width == request.Width &&
                    texture.height == request.Height,
                    $"HUD capture is {texture.width}x{texture.height}; " +
                    $"expected {request.Width}x{request.Height}.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(texture);
            }

            Debug.Log($"HUD visual capture passed: {path}");
        }

        private static void ConfigureGameViewResolution(int width, int height)
        {
            Assembly editorAssembly = typeof(EditorWindow).Assembly;
            Type gameViewType = editorAssembly.GetType("UnityEditor.GameView");
            Type sizesType = editorAssembly.GetType("UnityEditor.GameViewSizes");
            Type sizeType = editorAssembly.GetType("UnityEditor.GameViewSize");
            Type sizeKindType = editorAssembly.GetType("UnityEditor.GameViewSizeType");
            Require(
                gameViewType != null && sizesType != null &&
                sizeType != null && sizeKindType != null,
                "Unity Game View resolution API is unavailable.");

            EditorWindow gameView = EditorWindow.GetWindow(gameViewType);
            PropertyInfo instanceProperty = sizesType.GetProperty(
                "instance",
                BindingFlags.Public | BindingFlags.NonPublic |
                BindingFlags.Static | BindingFlags.FlattenHierarchy);
            object sizes = instanceProperty?.GetValue(null);
            MethodInfo getGroup = sizesType.GetMethod(
                "GetGroup",
                BindingFlags.Public | BindingFlags.NonPublic |
                BindingFlags.Instance);
            Require(gameView != null && sizes != null && getGroup != null,
                "Unity Game View sizes could not be loaded.");

            Type groupKind = getGroup.GetParameters()[0].ParameterType;
            object standalone = Enum.Parse(groupKind, "Standalone");
            object group = getGroup.Invoke(sizes, new[] { standalone });
            Require(group != null, "Standalone Game View group is unavailable.");

            MethodInfo getTotalCount = group.GetType().GetMethod(
                "GetTotalCount",
                BindingFlags.Public | BindingFlags.NonPublic |
                BindingFlags.Instance);
            MethodInfo getSize = group.GetType().GetMethod(
                "GetGameViewSize",
                BindingFlags.Public | BindingFlags.NonPublic |
                BindingFlags.Instance);
            MethodInfo addCustomSize = group.GetType().GetMethod(
                "AddCustomSize",
                BindingFlags.Public | BindingFlags.NonPublic |
                BindingFlags.Instance);
            Require(
                getTotalCount != null && getSize != null &&
                addCustomSize != null,
                "Unity Game View group API is unavailable.");

            int total = (int)getTotalCount.Invoke(group, null);
            int selectedIndex = FindSizeIndex(
                group,
                getSize,
                total,
                width,
                height);
            if (selectedIndex < 0)
            {
                object fixedResolution =
                    Enum.Parse(sizeKindType, "FixedResolution");
                object customSize = Activator.CreateInstance(
                    sizeType,
                    BindingFlags.Public | BindingFlags.NonPublic |
                    BindingFlags.Instance,
                    null,
                    new object[]
                    {
                        fixedResolution,
                        width,
                        height,
                        $"Cyber HUD {width}x{height}"
                    },
                    null);
                Require(customSize != null,
                    "Unity Game View custom size could not be created.");
                addCustomSize.Invoke(group, new[] { customSize });
                total = (int)getTotalCount.Invoke(group, null);
                selectedIndex = FindSizeIndex(
                    group,
                    getSize,
                    total,
                    width,
                    height);
            }

            Require(selectedIndex >= 0,
                $"Game View size {width}x{height} could not be selected.");
            PropertyInfo selectedSize = gameViewType.GetProperty(
                "selectedSizeIndex",
                BindingFlags.Public | BindingFlags.NonPublic |
                BindingFlags.Instance);
            FieldInfo selectedSizeField = gameViewType.GetField(
                "m_SelectedSizeIndex",
                BindingFlags.NonPublic | BindingFlags.Instance);
            if (selectedSize != null)
            {
                selectedSize.SetValue(gameView, selectedIndex);
            }
            else
            {
                Require(selectedSizeField != null,
                    "Unity Game View selected size API is unavailable.");
                selectedSizeField.SetValue(gameView, selectedIndex);
            }

            gameView.Repaint();
        }

        private static int FindSizeIndex(
            object group,
            MethodInfo getSize,
            int total,
            int width,
            int height)
        {
            for (int i = 0; i < total; i++)
            {
                object size = getSize.Invoke(group, new object[] { i });
                PropertyInfo widthProperty = size?.GetType().GetProperty(
                    "width",
                    BindingFlags.Public | BindingFlags.NonPublic |
                    BindingFlags.Instance);
                PropertyInfo heightProperty = size?.GetType().GetProperty(
                    "height",
                    BindingFlags.Public | BindingFlags.NonPublic |
                    BindingFlags.Instance);
                if (widthProperty != null && heightProperty != null &&
                    (int)widthProperty.GetValue(size) == width &&
                    (int)heightProperty.GetValue(size) == height)
                {
                    return i;
                }
            }

            return -1;
        }

        private static string GetOutputDirectory()
        {
            return Path.Combine(
                Directory.GetParent(Application.dataPath).FullName,
                "Logs",
                "Validation",
                "HudCaptures");
        }

        private static string GetOutputPath(CaptureRequest request)
        {
            return Path.Combine(
                GetOutputDirectory(),
                $"{request.SceneLabel}_{request.Width}x{request.Height}.png");
        }

        private static void RecordFailure(string failure)
        {
            if (string.IsNullOrEmpty(
                    SessionState.GetString(FailureKey, string.Empty)))
            {
                SessionState.SetString(FailureKey, failure);
            }
        }

        private static void Finish()
        {
            string failure = SessionState.GetString(FailureKey, string.Empty);
            SessionState.SetBool(RunningKey, false);
            SessionState.SetString(FailureKey, string.Empty);
            SessionState.SetString(PhaseKey, string.Empty);
            Runner.Detach();
            if (!string.IsNullOrEmpty(failure))
            {
                Debug.LogError("HUD visual capture failed:\n" + failure);
                EditorApplication.Exit(1);
                return;
            }

            Debug.Log("HUD visual capture passed for Stage1 and Tutorial.");
            EditorApplication.Exit(0);
        }

        private static void Require(bool condition, string message)
        {
            if (!condition)
            {
                throw new InvalidOperationException(message);
            }
        }

        private readonly struct CaptureRequest
        {
            public CaptureRequest(
                string scenePath,
                string sceneLabel,
                int width,
                int height)
            {
                ScenePath = scenePath;
                SceneLabel = sceneLabel;
                Width = width;
                Height = height;
            }

            public string ScenePath { get; }
            public string SceneLabel { get; }
            public int Width { get; }
            public int Height { get; }
        }
    }
}
