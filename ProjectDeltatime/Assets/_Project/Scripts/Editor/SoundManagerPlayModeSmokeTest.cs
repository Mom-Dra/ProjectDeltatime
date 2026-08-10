using System;
using Deltatime.Audio;
using Deltatime.Combat;
using Deltatime.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Deltatime.EditorTools
{
    [InitializeOnLoad]
    public static class SoundManagerPlayModeSmokeTest
    {
        private const string ScenePath = "Assets/_Project/Scenes/MainScene.unity";
        private const string EndingSceneName = "EndingScene";
        private const string RunningKey = "Deltatime.SoundSmoke.Running";
        private const string FailedKey = "Deltatime.SoundSmoke.Failed";
        private const string FailureKey = "Deltatime.SoundSmoke.Failure";
        private static double playModeStartedAt;
        private static int phase;

        static SoundManagerPlayModeSmokeTest()
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

            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
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
                phase = 0;
            }
            else if (state == PlayModeStateChange.EnteredEditMode)
            {
                Finish();
            }
        }

        private static void Tick()
        {
            if (!SessionState.GetBool(RunningKey, false) || !EditorApplication.isPlaying ||
                EditorApplication.timeSinceStartup - playModeStartedAt < 0.5d)
            {
                return;
            }

            try
            {
                SoundManager manager = UnityEngine.Object.FindFirstObjectByType<SoundManager>();
                Require(manager != null, "SoundManager was not bootstrapped.");
                Require(manager.Library != null, "SoundLibrary was not loaded from Resources.");

                if (phase == 1)
                {
                    Require(
                        SceneManager.GetActiveScene().name == "Tutorial",
                        "MainMenuController.Play did not load Tutorial.");
                    Require(
                        manager.CurrentBgmClip == manager.Library.TutorialBgm,
                        "Tutorial did not select the tutorial BGM.");
                    SceneManager.LoadScene(EndingSceneName);
                    phase = 2;
                    return;
                }

                if (phase == 2)
                {
                    Require(
                        SceneManager.GetActiveScene().name == EndingSceneName,
                        "Tutorial did not load EndingScene.");
                    Require(
                        manager.CurrentBgmClip == manager.Library.EndingBgm,
                        "EndingScene did not select BGM_Ending.");
                    Debug.Log("SoundManager PlayMode smoke passed.");
                    EditorApplication.ExitPlaymode();
                    return;
                }

                Require(manager.Library.IsConfigured(out string error), error);
                Require(
                    manager.CurrentBgmClip == manager.Library.MainMenuBgm,
                    "MainScene did not select the main-menu BGM.");

                WeaponDefinition pistol = AssetDatabase.LoadAssetAtPath<WeaponDefinition>(
                    "Assets/_Project/Pistol.asset");
                Require(pistol != null, "Pistol definition is missing.");
                manager.PlayWeaponFire(pistol, Vector3.zero);
                manager.PlayMeleeImpact(MeleeImpactKind.Punch, Vector3.zero);
                manager.PlayMeleeImpact(MeleeImpactKind.Bat, Vector3.zero);
                manager.PlayWeaponThrow(Vector3.zero);
                manager.PlayUiClick();
                manager.PlayDeadlineEnter();
                Require(manager.IsDeadlineAudioActive, "DEADLINE enter audio did not activate.");
                Require(
                    !manager.IsDeadlineTimeWarpLooping,
                    "DEADLINE time-warp audio must only play once per entry.");
                manager.PlayDeadlineRelease();
                Require(!manager.IsDeadlineAudioActive, "DEADLINE release audio did not stop.");

                MainMenuController menuController =
                    UnityEngine.Object.FindFirstObjectByType<MainMenuController>();
                Require(menuController != null, "MainMenuController is missing from MainScene.");
                int clickCountBeforePlay = manager.UiClickPlayCount;
                menuController.Play();
                Require(
                    manager.UiClickPlayCount == clickCountBeforePlay + 1,
                    "MainMenuController.Play did not play the UI click sound.");
                phase = 1;
            }
            catch (Exception exception)
            {
                SessionState.SetBool(FailedKey, true);
                SessionState.SetString(FailureKey, exception.ToString());
                Debug.LogException(exception);
                EditorApplication.ExitPlaymode();
            }
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
                Debug.LogError($"SoundManager PlayMode smoke failed: {failure}");
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
