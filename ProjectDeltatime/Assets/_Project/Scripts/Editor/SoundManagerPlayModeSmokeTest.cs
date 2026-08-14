using System;
using Deltatime.Audio;
using Deltatime.Combat;
using Deltatime.Core;
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
        private const string StageSceneName = "Stage1";
        private const string EndingSceneName = "EndingScene";
        private const double BgmCrossfadeSettleDuration = 0.35d;
        private const string RunningKey = "Deltatime.SoundSmoke.Running";
        private const string FailedKey = "Deltatime.SoundSmoke.Failed";
        private const string FailureKey = "Deltatime.SoundSmoke.Failure";
        private static double playModeStartedAt;
        private static double phaseStartedAt;
        private static int phase;
        private static readonly CommandLineSmokeRunner Runner =
            new CommandLineSmokeRunner(
                RunningKey,
                Tick,
                HandlePlayModeChanged);

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

            SessionState.SetBool(RunningKey, true);
            SessionState.SetBool(FailedKey, false);
            SessionState.SetString(FailureKey, string.Empty);
            Runner.OpenSceneAndEnterPlayMode(ScenePath);
        }

        private static void Attach()
        {
            Runner.Attach();
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
                    SceneManager.LoadScene(StageSceneName);
                    phaseStartedAt = EditorApplication.timeSinceStartup;
                    phase = 2;
                    return;
                }

                if (phase == 2)
                {
                    Require(
                        SceneManager.GetActiveScene().name == StageSceneName,
                        "Tutorial did not load Stage1.");
                    Require(
                        manager.CurrentBgmClip == manager.Library.StageBgm,
                        "Stage1 did not select the stage BGM.");
                    phaseStartedAt = EditorApplication.timeSinceStartup;
                    phase = 3;
                    return;
                }

                if (phase == 3)
                {
                    if (EditorApplication.timeSinceStartup - phaseStartedAt < BgmCrossfadeSettleDuration)
                    {
                        return;
                    }

                    ValidateStageBgmVolume(manager);
                    SceneManager.LoadScene(EndingSceneName);
                    phaseStartedAt = EditorApplication.timeSinceStartup;
                    phase = 4;
                    return;
                }

                if (phase == 4)
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
                ValidateBatSwingFeedback(manager);
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
            Runner.Detach();

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

        private static void ValidateBatSwingFeedback(SoundManager manager)
        {
            GameObject source = new GameObject("Bat Swing Miss Validation Source");
            GameObject target = null;
            try
            {
                MeleeAttackExecution execution =
                    source.AddComponent<MeleeAttackExecution>();
                int swingCountBeforeMiss = manager.MeleeSwingPlayCount;
                int impactCountBeforeMiss = manager.MeleeImpactPlayCount;
                Require(
                    execution.BeginAttack(
                        source,
                        CombatFaction.Player,
                        Vector3.forward,
                        1.25f,
                        45f,
                        1,
                        MeleeImpactKind.Bat),
                    "Bat swing miss validation attack did not start.");
                Require(
                    manager.MeleeSwingPlayCount == swingCountBeforeMiss + 1,
                    "A missed bat attack did not play exactly one swing sound.");
                Require(
                    manager.MeleeImpactPlayCount == impactCountBeforeMiss,
                    "A missed bat attack incorrectly played an impact sound.");

                target = new GameObject("Bat Swing Hit Validation Target");
                target.transform.position = new Vector3(0f, 0f, 0.75f);
                target.AddComponent<SphereCollider>().radius = 0.25f;
                SwingValidationTarget damageable =
                    target.AddComponent<SwingValidationTarget>();
                Physics.SyncTransforms();
                int swingCountBeforeHit = manager.MeleeSwingPlayCount;
                int impactCountBeforeHit = manager.MeleeImpactPlayCount;
                Require(
                    execution.BeginAttack(
                        source,
                        CombatFaction.Player,
                        Vector3.forward,
                        1.25f,
                        45f,
                        1,
                        MeleeImpactKind.Bat),
                    "Bat swing hit validation attack did not start.");
                Require(
                    damageable.HitCount == 1,
                    "Bat swing hit validation did not damage its target.");
                Require(
                    manager.MeleeSwingPlayCount == swingCountBeforeHit + 1 &&
                    manager.MeleeImpactPlayCount == impactCountBeforeHit + 1,
                    "A bat hit did not play both the swing and impact sounds.");
            }
            finally
            {
                if (target != null)
                {
                    UnityEngine.Object.Destroy(target);
                }

                UnityEngine.Object.Destroy(source);
            }
        }

        private static void ValidateStageBgmVolume(SoundManager manager)
        {
            AudioSource[] sources = manager.GetComponentsInChildren<AudioSource>(true);
            for (int i = 0; i < sources.Length; i++)
            {
                AudioSource source = sources[i];
                if (source.clip == manager.Library.StageBgm)
                {
                    float stageBgmVolume = source.volume;
                    Require(
                        Mathf.Approximately(stageBgmVolume, 0.35f),
                        $"Stage BGM volume was not reduced to 0.35. Current: {stageBgmVolume:F3}.");
                    return;
                }
            }

            throw new InvalidOperationException("No AudioSource is playing the stage BGM.");
        }

        private sealed class SwingValidationTarget : MonoBehaviour, IDamageable
        {
            public CombatFaction Faction => CombatFaction.Enemy;
            public bool IsAlive => true;
            public int HitCount { get; private set; }

            public void ReceiveHit(DamageHit hit)
            {
                HitCount++;
            }
        }
    }
}
