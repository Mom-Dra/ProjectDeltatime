using System;
using Deltatime.Combat;
using Deltatime.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TextCore.LowLevel;

namespace Deltatime.EditorTools
{
    /// <summary>
    /// Applies the Korean player-facing text and font assignments without
    /// rebuilding unrelated scene layout or gameplay content.
    /// </summary>
    public static class KoreanUiLocalizationBuilder
    {
        private const string FontFolder = "Assets/_Project/Font/Noto_Sans_KR";
        private const string RegularFontPath = FontFolder + "/NotoSansKR-Regular.otf";
        private const string BoldFontPath = FontFolder + "/NotoSansKR-Bold.otf";
        private const string TmpFontPath = FontFolder + "/NotoSansKR-Bold SDF.asset";
        private const string FontSettingsPath =
            "Assets/_Project/Resources/KoreanUiFontSettings.asset";
        private const string MainScenePath = "Assets/_Project/Scenes/MainScene.unity";
        private const string TutorialScenePath = "Assets/_Project/Scenes/Tutorial.unity";
        private const string GameStartText = "게임 시작";

        private static readonly string[] TutorialFloorLabelTexts =
        {
            "01 시간",
            "02 대시",
            "03 근접",
            "04 권총",
            "05 투척",
            "06 DEADLINE",
            "출구"
        };

        private static readonly string[] WeaponDefinitionPaths =
        {
            "Assets/_Project/Pistol.asset",
            "Assets/_Project/AutomaticRifle.asset",
            "Assets/_Project/Shotgun.asset",
            "Assets/_Project/MeleeWeapon.asset"
        };

        private static readonly string[] WeaponDisplayNames =
        {
            "권총",
            "자동소총",
            "샷건",
            "근접 무기"
        };

        [MenuItem("Tools/UI/Apply Korean Localization")]
        public static void ApplyKoreanUiLocalization()
        {
            KoreanUiFontSettings fontSettings = EnsureFontSettings();
            ApplyWeaponDisplayNames();
            ApplyMainMenu(fontSettings);
            ApplyTutorialFloorLabels(fontSettings);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Korean UI localization applied.");
        }

        public static void ApplyFromCommandLine()
        {
            ApplyKoreanUiLocalization();
        }

        [MenuItem("Tools/UI/Validate Korean Localization")]
        public static void ValidateKoreanUiLocalization()
        {
            KoreanUiFontSettings fontSettings =
                AssetDatabase.LoadAssetAtPath<KoreanUiFontSettings>(
                    FontSettingsPath);
            Require(fontSettings != null && fontSettings.IsConfigured,
                "Korean UI font settings are missing or incomplete.");
            Require(TMP_Settings.defaultFontAsset == fontSettings.TextMeshProFont,
                "TMP default font must use the Korean UI font asset.");
            Require(fontSettings.TextMeshProFont.atlasPopulationMode ==
                    AtlasPopulationMode.Dynamic &&
                    fontSettings.TextMeshProFont.sourceFontFile ==
                    fontSettings.BoldFont,
                "TMP Korean font must be a dynamic asset sourced from Noto Sans KR Bold.");

            ValidateWeaponDisplayNames();
            ValidateMainMenu(fontSettings);
            ValidateTutorialFloorLabels(fontSettings);
            Debug.Log("Korean UI localization validation passed.");
        }

        public static void ValidateFromCommandLine()
        {
            ValidateKoreanUiLocalization();
        }

        private static KoreanUiFontSettings EnsureFontSettings()
        {
            Font regular = LoadAsset<Font>(RegularFontPath);
            Font bold = LoadAsset<Font>(BoldFontPath);
            TMP_FontAsset tmpFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(
                TmpFontPath);
            if (tmpFont == null)
            {
                FontEngine.InitializeFontEngine();
                tmpFont = TMP_FontAsset.CreateFontAsset(
                    bold,
                    90,
                    9,
                    GlyphRenderMode.SDFAA,
                    1024,
                    1024,
                    AtlasPopulationMode.Dynamic,
                    true);
                Require(tmpFont != null,
                    "Could not create the Noto Sans KR TMP font asset.");
                tmpFont.name = "NotoSansKR-Bold SDF";
                AssetDatabase.CreateAsset(tmpFont, TmpFontPath);
                AssetDatabase.AddObjectToAsset(tmpFont.atlasTextures[0], tmpFont);
                AssetDatabase.AddObjectToAsset(tmpFont.material, tmpFont);
            }

            TMP_Settings.defaultFontAsset = tmpFont;
            EditorUtility.SetDirty(TMP_Settings.instance);

            KoreanUiFontSettings fontSettings =
                AssetDatabase.LoadAssetAtPath<KoreanUiFontSettings>(
                    FontSettingsPath);
            if (fontSettings == null)
            {
                fontSettings = ScriptableObject.CreateInstance<KoreanUiFontSettings>();
                AssetDatabase.CreateAsset(fontSettings, FontSettingsPath);
            }

            fontSettings.Configure(regular, bold, tmpFont);
            EditorUtility.SetDirty(fontSettings);
            return fontSettings;
        }

        private static void ApplyMainMenu(KoreanUiFontSettings fontSettings)
        {
            Scene scene = EditorSceneManager.OpenScene(MainScenePath, OpenSceneMode.Single);
            TextMeshProUGUI playLabel = FindRequiredComponent<TextMeshProUGUI>(
                scene,
                "PlayLabel");
            playLabel.text = GameStartText;
            playLabel.font = fontSettings.TextMeshProFont;
            playLabel.fontStyle = FontStyles.Bold;
            EditorUtility.SetDirty(playLabel);
            SaveScene(scene, MainScenePath);
        }

        private static void ApplyTutorialFloorLabels(KoreanUiFontSettings fontSettings)
        {
            Scene scene = EditorSceneManager.OpenScene(
                TutorialScenePath,
                OpenSceneMode.Single);
            for (int i = 0; i < TutorialFloorLabelTexts.Length; i++)
            {
                TextMesh label = FindRequiredComponent<TextMesh>(
                    scene,
                    $"Bay Label {i + 1:00}");
                label.text = TutorialFloorLabelTexts[i];
                label.font = fontSettings.BoldFont;
                label.fontStyle = FontStyle.Bold;
                MeshRenderer renderer = label.GetComponent<MeshRenderer>();
                Require(renderer != null,
                    $"Tutorial floor label {i + 1:00} requires a MeshRenderer.");
                renderer.sharedMaterial = fontSettings.BoldFont.material;
                renderer.enabled = true;
                EditorUtility.SetDirty(label);
                EditorUtility.SetDirty(renderer);
            }

            SaveScene(scene, TutorialScenePath);
        }

        private static void ApplyWeaponDisplayNames()
        {
            for (int i = 0; i < WeaponDefinitionPaths.Length; i++)
            {
                WeaponDefinition definition = LoadAsset<WeaponDefinition>(
                    WeaponDefinitionPaths[i]);
                definition.SetDisplayName(WeaponDisplayNames[i]);
                EditorUtility.SetDirty(definition);
            }
        }

        private static void ValidateMainMenu(KoreanUiFontSettings fontSettings)
        {
            Scene scene = EditorSceneManager.OpenScene(MainScenePath, OpenSceneMode.Single);
            TextMeshProUGUI playLabel = FindRequiredComponent<TextMeshProUGUI>(
                scene,
                "PlayLabel");
            Require(playLabel.text == GameStartText &&
                    playLabel.font == fontSettings.TextMeshProFont,
                "Main menu PlayLabel must use the Korean game-start text and TMP font.");
        }

        private static void ValidateTutorialFloorLabels(
            KoreanUiFontSettings fontSettings)
        {
            Scene scene = EditorSceneManager.OpenScene(
                TutorialScenePath,
                OpenSceneMode.Single);
            for (int i = 0; i < TutorialFloorLabelTexts.Length; i++)
            {
                TextMesh label = FindRequiredComponent<TextMesh>(
                    scene,
                    $"Bay Label {i + 1:00}");
                MeshRenderer renderer = label.GetComponent<MeshRenderer>();
                Require(label.text == TutorialFloorLabelTexts[i] &&
                        label.font == fontSettings.BoldFont &&
                        renderer != null &&
                        renderer.enabled &&
                        renderer.sharedMaterial == fontSettings.BoldFont.material,
                    $"Tutorial floor label {i + 1:00} is missing its Korean font or font material.");
            }
        }

        private static void ValidateWeaponDisplayNames()
        {
            for (int i = 0; i < WeaponDefinitionPaths.Length; i++)
            {
                WeaponDefinition definition = LoadAsset<WeaponDefinition>(
                    WeaponDefinitionPaths[i]);
                Require(definition.DisplayName == WeaponDisplayNames[i],
                    $"Weapon display name is not localized: {WeaponDefinitionPaths[i]}.");
            }
        }

        private static T FindRequiredComponent<T>(Scene scene, string objectName)
            where T : Component
        {
            GameObject[] roots = scene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                T[] components = roots[i].GetComponentsInChildren<T>(true);
                for (int j = 0; j < components.Length; j++)
                {
                    if (components[j].gameObject.name == objectName)
                    {
                        return components[j];
                    }
                }
            }

            throw new InvalidOperationException(
                $"Could not find {typeof(T).Name} on {objectName} in {scene.path}.");
        }

        private static T LoadAsset<T>(string path) where T : UnityEngine.Object
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            Require(asset != null, $"Required asset is missing: {path}");
            return asset;
        }

        private static void SaveScene(Scene scene, string expectedPath)
        {
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, expectedPath))
            {
                throw new InvalidOperationException($"Could not save {expectedPath}.");
            }
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
