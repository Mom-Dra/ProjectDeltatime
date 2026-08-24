using System;
using Deltatime.Audio;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Deltatime.Settings
{
    [Serializable]
    public sealed class GameSettingsSnapshot
    {
        public int Width;
        public int Height;
        public bool Fullscreen;
        public int QualityLevel;
        public int VSyncCount;
        public float MasterVolume = 1f;
        public float BgmVolume = 1f;
        public float SfxVolume = 1f;
        public string BindingOverridesJson = string.Empty;

        public GameSettingsSnapshot Clone()
        {
            return new GameSettingsSnapshot
            {
                Width = Width,
                Height = Height,
                Fullscreen = Fullscreen,
                QualityLevel = QualityLevel,
                VSyncCount = VSyncCount,
                MasterVolume = MasterVolume,
                BgmVolume = BgmVolume,
                SfxVolume = SfxVolume,
                BindingOverridesJson = BindingOverridesJson ?? string.Empty
            };
        }
    }

    /// <summary>
    /// Owns the applied user settings and their PlayerPrefs representation.
    /// Menu screens edit a clone and commit it through <see cref="Apply"/>.
    /// </summary>
    public static class GameSettingsService
    {
        private const string Prefix = "Deltatime.Settings.";
        private const string VersionKey = Prefix + "Version";
        private const string WidthKey = Prefix + "Width";
        private const string HeightKey = Prefix + "Height";
        private const string FullscreenKey = Prefix + "Fullscreen";
        private const string QualityKey = Prefix + "Quality";
        private const string VSyncKey = Prefix + "VSync";
        private const string MasterVolumeKey = Prefix + "MasterVolume";
        private const string BgmVolumeKey = Prefix + "BgmVolume";
        private const string SfxVolumeKey = Prefix + "SfxVolume";
        private const string BindingOverridesKey = Prefix + "BindingOverrides";
        private const int SettingsVersion = 1;

        private static GameSettingsSnapshot current;

        public static event Action SettingsApplied;

        public static GameSettingsSnapshot Current
        {
            get
            {
                current ??= Load();
                return current;
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void ApplySavedSettingsBeforeSceneLoad()
        {
            current = Load();
            ApplyRuntimeValues(current);
        }

        public static GameSettingsSnapshot CreateDraft()
        {
            return Current.Clone();
        }

        public static GameSettingsSnapshot CreateDefaults()
        {
            Resolution native = Screen.currentResolution;
            int width = native.width > 0 ? native.width : Mathf.Max(1, Screen.width);
            int height = native.height > 0 ? native.height : Mathf.Max(1, Screen.height);
            int quality = Mathf.Max(0, QualitySettings.names.Length - 1);
            return new GameSettingsSnapshot
            {
                Width = width,
                Height = height,
                Fullscreen = true,
                QualityLevel = quality,
                VSyncCount = 1,
                MasterVolume = 1f,
                BgmVolume = 1f,
                SfxVolume = 1f,
                BindingOverridesJson = string.Empty
            };
        }

        public static void Apply(GameSettingsSnapshot snapshot)
        {
            if (snapshot == null)
            {
                throw new ArgumentNullException(nameof(snapshot));
            }

            current = Validate(snapshot);
            Save(current);
            ApplyRuntimeValues(current);
            SettingsApplied?.Invoke();
        }

        public static bool TryApplyBindingOverrides(
            InputActionAsset asset,
            string overridesJson = null)
        {
            if (asset == null)
            {
                return false;
            }

            string json = overridesJson ?? Current.BindingOverridesJson;
            if (string.IsNullOrWhiteSpace(json))
            {
                asset.RemoveAllBindingOverrides();
                return true;
            }

            try
            {
                asset.LoadBindingOverridesFromJson(json);
                return true;
            }
            catch (Exception exception)
            {
                asset.RemoveAllBindingOverrides();
                Debug.LogWarning(
                    $"Ignored invalid saved input bindings: {exception.Message}");
                return false;
            }
        }

        private static GameSettingsSnapshot Load()
        {
            GameSettingsSnapshot defaults = CreateDefaults();
            if (PlayerPrefs.GetInt(VersionKey, 0) != SettingsVersion)
            {
                return defaults;
            }

            return Validate(new GameSettingsSnapshot
            {
                Width = PlayerPrefs.GetInt(WidthKey, defaults.Width),
                Height = PlayerPrefs.GetInt(HeightKey, defaults.Height),
                Fullscreen = PlayerPrefs.GetInt(
                    FullscreenKey,
                    defaults.Fullscreen ? 1 : 0) != 0,
                QualityLevel = PlayerPrefs.GetInt(
                    QualityKey,
                    defaults.QualityLevel),
                VSyncCount = PlayerPrefs.GetInt(VSyncKey, defaults.VSyncCount),
                MasterVolume = PlayerPrefs.GetFloat(
                    MasterVolumeKey,
                    defaults.MasterVolume),
                BgmVolume = PlayerPrefs.GetFloat(
                    BgmVolumeKey,
                    defaults.BgmVolume),
                SfxVolume = PlayerPrefs.GetFloat(
                    SfxVolumeKey,
                    defaults.SfxVolume),
                BindingOverridesJson = PlayerPrefs.GetString(
                    BindingOverridesKey,
                    string.Empty)
            });
        }

        private static GameSettingsSnapshot Validate(GameSettingsSnapshot source)
        {
            GameSettingsSnapshot defaults = CreateDefaults();
            GameSettingsSnapshot result = source.Clone();
            result.Width = result.Width > 0 ? result.Width : defaults.Width;
            result.Height = result.Height > 0 ? result.Height : defaults.Height;
            result.QualityLevel = Mathf.Clamp(
                result.QualityLevel,
                0,
                Mathf.Max(0, QualitySettings.names.Length - 1));
            result.VSyncCount = result.VSyncCount > 0 ? 1 : 0;
            result.MasterVolume = Mathf.Clamp01(result.MasterVolume);
            result.BgmVolume = Mathf.Clamp01(result.BgmVolume);
            result.SfxVolume = Mathf.Clamp01(result.SfxVolume);
            result.BindingOverridesJson ??= string.Empty;
            return result;
        }

        private static void Save(GameSettingsSnapshot settings)
        {
            PlayerPrefs.SetInt(VersionKey, SettingsVersion);
            PlayerPrefs.SetInt(WidthKey, settings.Width);
            PlayerPrefs.SetInt(HeightKey, settings.Height);
            PlayerPrefs.SetInt(FullscreenKey, settings.Fullscreen ? 1 : 0);
            PlayerPrefs.SetInt(QualityKey, settings.QualityLevel);
            PlayerPrefs.SetInt(VSyncKey, settings.VSyncCount);
            PlayerPrefs.SetFloat(MasterVolumeKey, settings.MasterVolume);
            PlayerPrefs.SetFloat(BgmVolumeKey, settings.BgmVolume);
            PlayerPrefs.SetFloat(SfxVolumeKey, settings.SfxVolume);
            PlayerPrefs.SetString(
                BindingOverridesKey,
                settings.BindingOverridesJson ?? string.Empty);
            PlayerPrefs.Save();
        }

        private static void ApplyRuntimeValues(GameSettingsSnapshot settings)
        {
            if (settings == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                QualitySettings.SetQualityLevel(settings.QualityLevel, true);
                QualitySettings.vSyncCount = settings.VSyncCount;
                Screen.SetResolution(
                    settings.Width,
                    settings.Height,
                    settings.Fullscreen
                        ? FullScreenMode.FullScreenWindow
                        : FullScreenMode.Windowed);
            }
            SoundManager.Instance?.ApplyUserVolumes(
                settings.MasterVolume,
                settings.BgmVolume,
                settings.SfxVolume);
        }
    }
}
