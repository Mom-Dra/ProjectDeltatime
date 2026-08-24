using System;
using System.IO;
using System.Reflection;
using Deltatime.UI;
using UnityEditor;
using UnityEngine;

namespace Deltatime.EditorTools
{
    [InitializeOnLoad]
    public static class MainMenuVisualCapture
    {
        private const string ScenePath = "Assets/_Project/Scenes/MainScene.unity";
        private const string RunningKey = "Deltatime.MainMenuCapture.Running";
        private const string FailureKey = "Deltatime.MainMenuCapture.Failure";
        private const string IndexKey = "Deltatime.MainMenuCapture.Index";
        private const string PhaseKey = "Deltatime.MainMenuCapture.Phase";

        private static readonly CaptureRequest[] Requests =
        {
            new CaptureRequest(1920, 1080, PanelKind.Main),
            new CaptureRequest(1920, 1080, PanelKind.Graphics),
            new CaptureRequest(1920, 1080, PanelKind.Keys),
            new CaptureRequest(1920, 1080, PanelKind.Audio),
            new CaptureRequest(1920, 1080, PanelKind.Credits),
            new CaptureRequest(1280, 720, PanelKind.Main),
            new CaptureRequest(1280, 720, PanelKind.Graphics),
            new CaptureRequest(1280, 720, PanelKind.Keys),
            new CaptureRequest(1280, 720, PanelKind.Audio),
            new CaptureRequest(1280, 720, PanelKind.Credits),
            new CaptureRequest(2560, 1080, PanelKind.Main),
            new CaptureRequest(2560, 1080, PanelKind.Graphics),
            new CaptureRequest(2560, 1080, PanelKind.Keys),
            new CaptureRequest(2560, 1080, PanelKind.Audio),
            new CaptureRequest(2560, 1080, PanelKind.Credits),
            new CaptureRequest(1024, 768, PanelKind.Main),
            new CaptureRequest(1024, 768, PanelKind.Graphics),
            new CaptureRequest(1024, 768, PanelKind.Keys),
            new CaptureRequest(1024, 768, PanelKind.Audio),
            new CaptureRequest(1024, 768, PanelKind.Credits)
        };

        private static readonly CommandLineSmokeRunner Runner =
            new CommandLineSmokeRunner(RunningKey, Tick, HandlePlayModeStateChanged);
        private static double playStartedAt;
        private static bool resolutionApplied;
        private static bool panelApplied;
        private static bool captureRequested;
        private static DateTime captureRequestUtc;

        static MainMenuVisualCapture()
        {
            if (!SessionState.GetBool(RunningKey, false)) return;
            Runner.Attach();
            EditorApplication.delayCall += Resume;
        }

        public static void CaptureAllFromCommandLine()
        {
            if (SessionState.GetBool(RunningKey, false))
            {
                Runner.Attach();
                EditorApplication.delayCall += Resume;
                return;
            }

            Directory.CreateDirectory(OutputDirectory);
            SessionState.SetBool(RunningKey, true);
            SessionState.SetString(FailureKey, string.Empty);
            SessionState.SetInt(IndexKey, 0);
            StartCurrent();
        }

        private static void Resume()
        {
            if (SessionState.GetBool(RunningKey, false) &&
                !EditorApplication.isPlayingOrWillChangePlaymode)
            {
                StartCurrent();
            }
        }

        private static void StartCurrent()
        {
            int index = SessionState.GetInt(IndexKey, 0);
            if (index >= Requests.Length)
            {
                Finish();
                return;
            }

            try
            {
                ConfigureGameView(Requests[index]);
                SessionState.SetString(PhaseKey, "entering");
                Runner.OpenSceneAndEnterPlayMode(ScenePath);
            }
            catch (Exception exception)
            {
                Fail(exception.ToString());
                Finish();
            }
        }

        private static void HandlePlayModeStateChanged(PlayModeStateChange state)
        {
            if (!SessionState.GetBool(RunningKey, false)) return;
            if (state == PlayModeStateChange.EnteredPlayMode)
            {
                playStartedAt = EditorApplication.timeSinceStartup;
                resolutionApplied = false;
                panelApplied = false;
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
                if (!string.IsNullOrEmpty(SessionState.GetString(FailureKey, string.Empty)))
                {
                    Finish();
                    return;
                }

                int next = SessionState.GetInt(IndexKey, 0) + 1;
                SessionState.SetInt(IndexKey, next);
                if (next >= Requests.Length) Finish();
                else EditorApplication.delayCall += StartCurrent;
            }
        }

        private static void Tick()
        {
            if (!SessionState.GetBool(RunningKey, false))
            {
                Runner.Detach();
                return;
            }
            if (!EditorApplication.isPlaying) return;

            CaptureRequest request = Requests[SessionState.GetInt(IndexKey, 0)];
            double elapsed = EditorApplication.timeSinceStartup - playStartedAt;
            try
            {
                if (!resolutionApplied && elapsed >= 0.2d)
                {
                    ConfigureGameView(request);
                    resolutionApplied = true;
                }
                else if (!panelApplied && elapsed >= 1.0d)
                {
                    MainMenuController controller =
                        UnityEngine.Object.FindFirstObjectByType<MainMenuController>();
                    Require(controller != null, "MainMenuController is unavailable.");
                    if (request.Panel == PanelKind.Graphics ||
                        request.Panel == PanelKind.Keys ||
                        request.Panel == PanelKind.Audio)
                    {
                        controller.OpenOptions();
                        MainMenuOptionsController options =
                            UnityEngine.Object.FindFirstObjectByType<MainMenuOptionsController>();
                        if (request.Panel == PanelKind.Keys) options.ShowKeys();
                        else if (request.Panel == PanelKind.Audio) options.ShowAudio();
                    }
                    else if (request.Panel == PanelKind.Credits) controller.OpenCredits();
                    panelApplied = true;
                }
                else if (!captureRequested && elapsed >= 1.7d)
                {
                    Require(Screen.width == request.Width && Screen.height == request.Height,
                        $"Game View is {Screen.width}x{Screen.height}; expected {request.Width}x{request.Height}.");
                    captureRequestUtc = DateTime.UtcNow;
                    ScreenCapture.CaptureScreenshot(GetOutputPath(request));
                    captureRequested = true;
                }
                else if (captureRequested && elapsed >= 3.2d)
                {
                    ValidateCapture(request);
                    EditorApplication.isPlaying = false;
                }
                else if (elapsed >= 20d)
                {
                    throw new TimeoutException("Main menu visual capture timed out.");
                }
            }
            catch (Exception exception)
            {
                Fail(exception.ToString());
                EditorApplication.isPlaying = false;
            }
        }

        private static void ConfigureGameView(CaptureRequest request)
        {
            MethodInfo configure = typeof(HudVisualCapture).GetMethod(
                "ConfigureGameViewResolution",
                BindingFlags.NonPublic | BindingFlags.Static);
            Require(configure != null, "Shared Game View resolution helper is unavailable.");
            configure.Invoke(null, new object[] { request.Width, request.Height });
        }

        private static void ValidateCapture(CaptureRequest request)
        {
            string path = GetOutputPath(request);
            FileInfo file = new FileInfo(path);
            Require(file.Exists && file.Length > 0 && file.LastWriteTimeUtc >= captureRequestUtc,
                $"Main menu capture was not written: {path}");
            Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            try
            {
                Require(ImageConversion.LoadImage(texture, File.ReadAllBytes(path), true),
                    $"Capture is not a readable PNG: {path}");
                Require(texture.width == request.Width && texture.height == request.Height,
                    $"Capture has the wrong dimensions: {path}");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(texture);
            }
            Debug.Log($"Main menu visual capture passed: {path}");
        }

        private static string OutputDirectory => Path.Combine(
            Directory.GetParent(Application.dataPath).FullName,
            "Logs", "Validation", "MainMenuCaptures");

        private static string GetOutputPath(CaptureRequest request) => Path.Combine(
            OutputDirectory,
            $"{request.Panel}_{request.Width}x{request.Height}.png");

        private static void Fail(string failure)
        {
            if (string.IsNullOrEmpty(SessionState.GetString(FailureKey, string.Empty)))
                SessionState.SetString(FailureKey, failure);
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
                Debug.LogError("Main menu visual capture failed:\n" + failure);
                EditorApplication.Exit(1);
                return;
            }
            Debug.Log("Main menu visual capture passed at four resolutions for menu, Graphics, Keys, Audio and Credits.");
            EditorApplication.Exit(0);
        }

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }

        private enum PanelKind { Main, Graphics, Keys, Audio, Credits }

        private readonly struct CaptureRequest
        {
            public CaptureRequest(int width, int height, PanelKind panel)
            {
                Width = width;
                Height = height;
                Panel = panel;
            }
            public int Width { get; }
            public int Height { get; }
            public PanelKind Panel { get; }
        }
    }
}
