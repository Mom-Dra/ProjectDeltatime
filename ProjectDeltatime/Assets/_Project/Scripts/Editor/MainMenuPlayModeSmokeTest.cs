using System;
using Deltatime.Audio;
using Deltatime.InputSystem;
using Deltatime.Settings;
using Deltatime.UI;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Deltatime.EditorTools
{
    [InitializeOnLoad]
    public static class MainMenuPlayModeSmokeTest
    {
        private const string RunningKey = "Deltatime.MainMenuSmoke.Running";
        private const string FailureKey = "Deltatime.MainMenuSmoke.Failure";
        private const string OriginalKey = "Deltatime.MainMenuSmoke.Original";
        private static readonly CommandLineSmokeRunner Runner =
            new CommandLineSmokeRunner(RunningKey, Tick, HandlePlayModeStateChanged);
        private static int phase;
        private static double phaseStartedAt;

        static MainMenuPlayModeSmokeTest()
        {
            if (!SessionState.GetBool(RunningKey, false)) return;
            Runner.Attach();
            EditorApplication.delayCall += Resume;
        }

        public static void RunFromCommandLine()
        {
            if (SessionState.GetBool(RunningKey, false))
            {
                Runner.Attach();
                EditorApplication.delayCall += Resume;
                return;
            }

            try
            {
                GameSettingsSnapshot original = GameSettingsService.Current.Clone();
                SessionState.SetString(OriginalKey, JsonUtility.ToJson(original));
                GameSettingsSnapshot test = original.Clone();
                test.MasterVolume = 0.8f;
                test.BgmVolume = 0.6f;
                test.SfxVolume = 0.4f;
                using (PlayerControls controls = new PlayerControls())
                {
                    int index = InputBindingDisplay.FindBindingIndex(controls.Gameplay.NextStage);
                    controls.Gameplay.NextStage.ApplyBindingOverride(index, "<Keyboard>/k");
                    test.BindingOverridesJson = controls.asset.SaveBindingOverridesAsJson();
                }
                GameSettingsService.Apply(test);
                InputBindingDisplay.Invalidate();
                EndingSceneBuilder.BuildEndingScene();

                SessionState.SetBool(RunningKey, true);
                SessionState.SetString(FailureKey, string.Empty);
                Runner.OpenSceneAndEnterPlayMode("Assets/_Project/Scenes/MainScene.unity");
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                RestoreSettings();
                EditorApplication.Exit(1);
            }
        }

        private static void Resume()
        {
            if (SessionState.GetBool(RunningKey, false) &&
                !EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Runner.OpenSceneAndEnterPlayMode("Assets/_Project/Scenes/MainScene.unity");
            }
        }

        private static void HandlePlayModeStateChanged(PlayModeStateChange state)
        {
            if (!SessionState.GetBool(RunningKey, false)) return;
            if (state == PlayModeStateChange.EnteredPlayMode)
            {
                phase = 0;
                phaseStartedAt = EditorApplication.timeSinceStartup;
            }
            else if (state == PlayModeStateChange.EnteredEditMode)
            {
                Finish();
            }
        }

        private static void Tick()
        {
            if (!EditorApplication.isPlaying ||
                !SessionState.GetBool(RunningKey, false)) return;
            if (EditorApplication.timeSinceStartup - phaseStartedAt > 20d)
            {
                Fail("Main menu PlayMode smoke timed out.");
                return;
            }

            try
            {
                string scene = SceneManager.GetActiveScene().name;
                if (phase == 0 && scene == "MainScene" && Elapsed(1d))
                {
                    ValidateMainMenuInteractions();
                    SceneManager.LoadScene("EndingScene");
                    Advance();
                }
                else if (phase == 1 && scene == "EndingScene" && Elapsed(0.7d))
                {
                    TextMeshProUGUI instruction = Find<TextMeshProUGUI>("EndingInstruction");
                    Require(instruction != null && instruction.text.Contains("Press K "),
                        "EndingScene did not show the saved Next Stage binding.");
                    SceneManager.LoadScene("MainScene");
                    Advance();
                }
                else if (phase == 2 && scene == "MainScene" && Elapsed(0.7d))
                {
                    Find<Button>("StartButton").onClick.Invoke();
                    Advance();
                }
                else if (phase == 3 && scene == "Tutorial" && Elapsed(0.7d))
                {
                    RestoreSettings();
                    Debug.Log("Main menu PlayMode smoke passed.");
                    EditorApplication.isPlaying = false;
                }
            }
            catch (Exception exception)
            {
                Fail(exception.ToString());
            }
        }

        private static void ValidateMainMenuInteractions()
        {
            MainMenuController controller = UnityEngine.Object.FindFirstObjectByType<MainMenuController>();
            Require(controller != null && !controller.IsModalOpen, "Main menu controller is unavailable.");
            Require(Find<Button>("StartButton") != null && Find<Button>("OptionButton") != null &&
                    Find<Button>("CreditsButton") != null && Find<Button>("ExitButton") != null,
                "One or more main menu buttons are missing.");
            Require(Find<TextMeshProUGUI>("StartShortcut").text.Contains("PRESS K TO START"),
                "MainScene did not display the saved Next Stage binding.");
            SoundManager sound = SoundManager.Instance;
            Require(sound != null && Mathf.Approximately(sound.UserMasterVolume, 0.8f) &&
                    Mathf.Approximately(sound.UserBgmVolume, 0.6f) &&
                    Mathf.Approximately(sound.UserSfxVolume, 0.4f),
                "SoundManager did not apply user Master/BGM/SFX multipliers.");

            controller.OpenOptions();
            Require(controller.IsModalOpen && FindObject("OptionPanel").activeSelf,
                "OPTION did not open its modal.");
            controller.Play();
            Require(SceneManager.GetActiveScene().name == "MainScene",
                "Start shortcut was not blocked while OPTION was open.");
            UnityEngine.Object.FindFirstObjectByType<MainMenuOptionsController>().Cancel();
            Require(!controller.IsModalOpen, "CANCEL did not close OPTION.");
            controller.OpenCredits();
            Require(controller.IsModalOpen && FindObject("CreditsPanel").activeSelf,
                "CREDITS did not open its modal.");
            controller.CloseCredits();
            Require(!controller.IsModalOpen, "CREDITS did not close.");
        }

        private static bool Elapsed(double seconds) =>
            EditorApplication.timeSinceStartup - phaseStartedAt >= seconds;

        private static void Advance()
        {
            phase++;
            phaseStartedAt = EditorApplication.timeSinceStartup;
        }

        private static void Fail(string message)
        {
            if (string.IsNullOrEmpty(SessionState.GetString(FailureKey, string.Empty)))
                SessionState.SetString(FailureKey, message);
            RestoreSettings();
            EditorApplication.isPlaying = false;
        }

        private static void Finish()
        {
            string failure = SessionState.GetString(FailureKey, string.Empty);
            RestoreSettings();
            SessionState.SetBool(RunningKey, false);
            SessionState.SetString(FailureKey, string.Empty);
            Runner.Detach();
            if (!string.IsNullOrEmpty(failure))
            {
                Debug.LogError("Main menu PlayMode smoke failed:\n" + failure);
                EditorApplication.Exit(1);
            }
            else
            {
                EditorApplication.Exit(0);
            }
        }

        private static void RestoreSettings()
        {
            string json = SessionState.GetString(OriginalKey, string.Empty);
            if (string.IsNullOrEmpty(json)) return;
            GameSettingsSnapshot original = JsonUtility.FromJson<GameSettingsSnapshot>(json);
            GameSettingsService.Apply(original);
            InputBindingDisplay.Invalidate();
            SessionState.SetString(OriginalKey, string.Empty);
        }

        private static T Find<T>(string name) where T : Component
        {
            T[] components = UnityEngine.Object.FindObjectsByType<T>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < components.Length; i++)
                if (components[i].name == name) return components[i];
            return null;
        }

        private static GameObject FindObject(string name)
        {
            Transform[] transforms = UnityEngine.Object.FindObjectsByType<Transform>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < transforms.Length; i++)
                if (transforms[i].name == name) return transforms[i].gameObject;
            return null;
        }

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }
    }
}
