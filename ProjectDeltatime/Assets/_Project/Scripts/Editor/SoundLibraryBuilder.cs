using System;
using Deltatime.Audio;
using Deltatime.Combat;
using UnityEditor;
using UnityEngine;

namespace Deltatime.EditorTools
{
    public static class SoundLibraryBuilder
    {
        private const string ResourceFolder = "Assets/_Project/Resources";
        private const string LibraryPath = ResourceFolder + "/DeltatimeSoundLibrary.asset";

        [MenuItem("Tools/Prototype/Audio/Build Sound Library")]
        public static void Build()
        {
            AssetDatabase.Refresh();
            EnsureResourceFolder();

            SoundLibrary library = AssetDatabase.LoadAssetAtPath<SoundLibrary>(LibraryPath);
            if (library == null)
            {
                library = ScriptableObject.CreateInstance<SoundLibrary>();
                AssetDatabase.CreateAsset(library, LibraryPath);
            }

            WeaponDefinition pistol = Load<WeaponDefinition>("Assets/_Project/Pistol.asset");
            WeaponDefinition rifle = Load<WeaponDefinition>("Assets/_Project/AutomaticRifle.asset");
            WeaponDefinition shotgun = Load<WeaponDefinition>("Assets/_Project/Shotgun.asset");

            library.Configure(
                Load<AudioClip>("Assets/_Project/Audio/BGM/BGM_MainMenu.mp3"),
                Load<AudioClip>("Assets/_Project/Audio/BGM/BGM_Tutorial.mp3"),
                Load<AudioClip>("Assets/_Project/Audio/BGM/BGM_Stage_Action.mp3"),
                Load<AudioClip>("Assets/_Project/Audio/BGM/BGM_Ending.mp3"),
                new[]
                {
                    new SoundLibrary.WeaponFireSet(
                        pistol,
                        LoadClips(
                            "Assets/_Project/Audio/SFX/Weapons/Pistol/SFX_Pistol_Fire_01.wav",
                            "Assets/_Project/Audio/SFX/Weapons/Pistol/SFX_Pistol_Fire_02.wav")),
                    new SoundLibrary.WeaponFireSet(
                        rifle,
                        LoadClips(
                            "Assets/_Project/Audio/SFX/Weapons/Rifle/SFX_Rifle_Fire_01.wav",
                            "Assets/_Project/Audio/SFX/Weapons/Rifle/SFX_Rifle_Fire_02.wav")),
                    new SoundLibrary.WeaponFireSet(
                        shotgun,
                        LoadClips(
                            "Assets/_Project/Audio/SFX/Weapons/Shotgun/SFX_Shotgun_Fire_01.wav",
                            "Assets/_Project/Audio/SFX/Weapons/Shotgun/SFX_Shotgun_Fire_02.wav",
                            "Assets/_Project/Audio/SFX/Weapons/Shotgun/SFX_Shotgun_Fire_03.wav"))
                },
                LoadClips(
                    "Assets/_Project/Audio/SFX/Combat/Impact/SFX_Punch_Hit_01.ogg",
                    "Assets/_Project/Audio/SFX/Combat/Impact/SFX_Punch_Hit_02.ogg"),
                LoadClips(
                    "Assets/_Project/Audio/SFX/Combat/Impact/SFX_Bat_Hit_01.ogg",
                    "Assets/_Project/Audio/SFX/Combat/Impact/SFX_Bat_Hit_02.ogg"),
                LoadClips(
                    "Assets/_Project/Audio/SFX/Combat/Swing/SFX_Bat_Swing_01.wav",
                    "Assets/_Project/Audio/SFX/Combat/Swing/SFX_Bat_Swing_02.wav"),
                Load<AudioClip>("Assets/_Project/Audio/SFX/Combat/SFX_Weapon_Throw.ogg"),
                Load<AudioClip>("Assets/_Project/Audio/SFX/Click/click.ogg"),
                Load<AudioClip>("Assets/_Project/Audio/SFX/Deadline/SFX_Deadline_Enter_Impact.mp3"),
                Load<AudioClip>("Assets/_Project/Audio/SFX/Deadline/SFX_Deadline_Enter_TimeWarp.mp3"),
                LoadClips(
                    "Assets/_Project/Audio/SFX/Deadline/SFX_Deadline_Release2.mp3"));

            EditorUtility.SetDirty(library);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            if (!library.IsConfigured(out string error))
            {
                throw new InvalidOperationException($"Sound library validation failed: {error}");
            }

            Debug.Log($"Sound library built and validated: {LibraryPath}");
        }

        public static void BuildAndValidateFromCommandLine()
        {
            try
            {
                Build();
                EditorApplication.Exit(0);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                EditorApplication.Exit(1);
            }
        }

        private static T Load<T>(string path) where T : UnityEngine.Object
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset == null)
            {
                throw new InvalidOperationException($"Required sound asset is missing: {path}");
            }

            return asset;
        }

        private static AudioClip[] LoadClips(params string[] paths)
        {
            AudioClip[] clips = new AudioClip[paths.Length];
            for (int i = 0; i < paths.Length; i++)
            {
                clips[i] = Load<AudioClip>(paths[i]);
            }

            return clips;
        }

        private static void EnsureResourceFolder()
        {
            if (!AssetDatabase.IsValidFolder(ResourceFolder))
            {
                AssetDatabase.CreateFolder("Assets/_Project", "Resources");
            }
        }
    }
}
