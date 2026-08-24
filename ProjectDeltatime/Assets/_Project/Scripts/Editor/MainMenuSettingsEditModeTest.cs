using System;
using Deltatime.InputSystem;
using Deltatime.Settings;
using Deltatime.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Deltatime.EditorTools
{
    public static class MainMenuSettingsEditModeTest
    {
        [MenuItem("Tools/Main Menu/Run Settings EditMode Test")]
        public static void Run()
        {
            GameSettingsSnapshot original = GameSettingsService.Current.Clone();
            try
            {
                ValidateDraftIsolation();
                ValidateDefaults();
                ValidateRebindingRulesAndJson();
                ValidatePersistenceAndClamping();
                Debug.Log("Main menu settings EditMode test passed.");
            }
            finally
            {
                GameSettingsService.Apply(original);
                InputBindingDisplay.Invalidate();
            }
        }

        public static void RunFromCommandLine()
        {
            SceneBuildCommand.Run(Run);
        }

        private static void ValidateDraftIsolation()
        {
            GameSettingsSnapshot applied = GameSettingsService.Current;
            GameSettingsSnapshot draft = GameSettingsService.CreateDraft();
            draft.Width += 17;
            draft.MasterVolume = 0.13f;
            Require(applied.Width != draft.Width &&
                    !Mathf.Approximately(applied.MasterVolume, draft.MasterVolume),
                "Cancel semantics failed: a draft mutated the applied settings.");
        }

        private static void ValidateDefaults()
        {
            GameSettingsSnapshot defaults = GameSettingsService.CreateDefaults();
            Require(defaults.Width > 0 && defaults.Height > 0 && defaults.Fullscreen,
                "Default graphics values are invalid.");
            Require(defaults.VSyncCount == 1 && defaults.BindingOverridesJson == string.Empty,
                "Reset Defaults must reset VSync and bindings in the draft.");
            Require(Mathf.Approximately(defaults.MasterVolume, 1f) &&
                    Mathf.Approximately(defaults.BgmVolume, 1f) &&
                    Mathf.Approximately(defaults.SfxVolume, 1f),
                "Reset Defaults must restore all audio sliders to 100 percent.");
        }

        private static void ValidateRebindingRulesAndJson()
        {
            using (PlayerControls controls = new PlayerControls())
            {
                InputAction fire = controls.Gameplay.Fire;
                int fireIndex = InputBindingDisplay.FindBindingIndex(fire);
                Require(fireIndex >= 0, "Fire binding is missing.");
                Require(!MainMenuOptionsController.IsAllowedPath(false, "<Keyboard>/escape"),
                    "Escape must be reserved for rebind cancellation.");
                Require(!MainMenuOptionsController.IsAllowedPath(false, "<Mouse>/leftButton"),
                    "Keyboard-only actions must reject mouse buttons.");
                Require(MainMenuOptionsController.IsAllowedPath(true, "<Mouse>/leftButton"),
                    "Fire and Throw must allow mouse buttons.");
                Require(MainMenuOptionsController.HasDuplicatePath(
                        controls.asset, fire, fireIndex, "<Keyboard>/w"),
                    "Duplicate binding detection must include Move composite parts.");

                fire.ApplyBindingOverride(fireIndex, "<Keyboard>/k");
                string json = controls.asset.SaveBindingOverridesAsJson();
                using (PlayerControls restored = new PlayerControls())
                {
                    Require(GameSettingsService.TryApplyBindingOverrides(restored.asset, json),
                        "Binding override JSON could not be loaded.");
                    Require(restored.Gameplay.Fire.bindings[fireIndex].effectivePath == "<Keyboard>/k",
                        "Binding override JSON did not round-trip.");
                }
            }
        }

        private static void ValidatePersistenceAndClamping()
        {
            GameSettingsSnapshot test = GameSettingsService.CreateDraft();
            test.Width = 0;
            test.Height = -1;
            test.QualityLevel = 999;
            test.VSyncCount = 4;
            test.MasterVolume = -2f;
            test.BgmVolume = 0.42f;
            test.SfxVolume = 3f;
            GameSettingsService.Apply(test);
            GameSettingsSnapshot applied = GameSettingsService.Current;
            Require(applied.Width > 0 && applied.Height > 0,
                "Invalid resolutions must be corrected before saving.");
            Require(applied.QualityLevel >= 0 &&
                    applied.QualityLevel < Mathf.Max(1, QualitySettings.names.Length),
                "Quality index was not clamped.");
            Require(applied.VSyncCount == 1 &&
                    Mathf.Approximately(applied.MasterVolume, 0f) &&
                    Mathf.Approximately(applied.BgmVolume, 0.42f) &&
                    Mathf.Approximately(applied.SfxVolume, 1f),
                "Audio or VSync values were not normalized.");
        }

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }
    }
}
