using System;
using System.IO;
using Deltatime.Combat;
using Deltatime.UI;
using UnityEditor;
using UnityEngine;

namespace Deltatime.EditorTools
{
    public static class HudAssetBuilder
    {
        private const string IconFolder = "Assets/_Project/Art/UI/HudIcons";
        private const string ResourceFolder = "Assets/_Project/Resources/Hud";
        private const string IconSetPath = ResourceFolder + "/HudIconSet.asset";

        private const string HealthIconPath = IconFolder + "/HudHealth.png";
        private const string DeadlineIconPath = IconFolder + "/HudDeadline.png";
        private const string ClockIconPath = IconFolder + "/HudClockDial.png";
        private const string UnarmedIconPath = IconFolder + "/HudUnarmed.png";
        private const string PistolIconPath = IconFolder + "/HudPistol.png";
        private const string RifleIconPath = IconFolder + "/HudAutomaticRifle.png";
        private const string ShotgunIconPath = IconFolder + "/HudShotgun.png";
        private const string MeleeIconPath = IconFolder + "/HudMelee.png";

        private const string PistolDefinitionPath = "Assets/_Project/Pistol.asset";
        private const string RifleDefinitionPath =
            "Assets/_Project/AutomaticRifle.asset";
        private const string ShotgunDefinitionPath = "Assets/_Project/Shotgun.asset";
        private const string MeleeDefinitionPath = "Assets/_Project/MeleeWeapon.asset";

        private static readonly string[] AllIconPaths =
        {
            HealthIconPath,
            DeadlineIconPath,
            ClockIconPath,
            UnarmedIconPath,
            PistolIconPath,
            RifleIconPath,
            ShotgunIconPath,
            MeleeIconPath
        };

        [MenuItem("Tools/UI/Build Cyber HUD Assets")]
        public static void BuildCyberHudAssets()
        {
            EnsureFolder("Assets/_Project/Resources", "Hud");
            for (int i = 0; i < AllIconPaths.Length; i++)
            {
                ConfigureIconImporter(AllIconPaths[i]);
            }

            Sprite health = LoadSprite(HealthIconPath);
            Sprite deadline = LoadSprite(DeadlineIconPath);
            Sprite clock = LoadSprite(ClockIconPath);
            Sprite unarmed = LoadSprite(UnarmedIconPath);
            Sprite pistol = LoadSprite(PistolIconPath);
            Sprite rifle = LoadSprite(RifleIconPath);
            Sprite shotgun = LoadSprite(ShotgunIconPath);
            Sprite melee = LoadSprite(MeleeIconPath);

            HudIconSet iconSet =
                AssetDatabase.LoadAssetAtPath<HudIconSet>(IconSetPath);
            if (iconSet == null)
            {
                iconSet = ScriptableObject.CreateInstance<HudIconSet>();
                AssetDatabase.CreateAsset(iconSet, IconSetPath);
            }

            iconSet.Configure(health, deadline, clock, unarmed);
            EditorUtility.SetDirty(iconSet);
            ConfigureWeaponIcon(PistolDefinitionPath, pistol);
            ConfigureWeaponIcon(RifleDefinitionPath, rifle);
            ConfigureWeaponIcon(ShotgunDefinitionPath, shotgun);
            ConfigureWeaponIcon(MeleeDefinitionPath, melee);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            ValidateCyberHudAssets();
            Debug.Log("Cyber HUD assets built and validated.");
        }

        public static void BuildAndValidateFromCommandLine()
        {
            BuildCyberHudAssets();
        }

        [MenuItem("Tools/UI/Validate Cyber HUD Assets")]
        public static void ValidateCyberHudAssets()
        {
            HudIconSet iconSet =
                AssetDatabase.LoadAssetAtPath<HudIconSet>(IconSetPath);
            Require(iconSet != null && iconSet.IsConfigured,
                "Cyber HUD icon set is missing or incomplete.");

            for (int i = 0; i < AllIconPaths.Length; i++)
            {
                TextureImporter importer =
                    AssetImporter.GetAtPath(AllIconPaths[i]) as TextureImporter;
                Require(importer != null,
                    $"HUD icon importer is missing: {AllIconPaths[i]}");
                Require(
                    importer.textureType == TextureImporterType.Sprite &&
                    importer.spriteImportMode == SpriteImportMode.Single &&
                    !importer.mipmapEnabled &&
                    importer.alphaIsTransparency &&
                    importer.maxTextureSize == 256 &&
                    importer.filterMode == FilterMode.Bilinear &&
                    importer.wrapMode == TextureWrapMode.Clamp,
                    $"HUD icon import settings are invalid: {AllIconPaths[i]}");
                ValidatePngTransparency(AllIconPaths[i]);
            }

            ValidateWeaponIcon(PistolDefinitionPath, PistolIconPath);
            ValidateWeaponIcon(RifleDefinitionPath, RifleIconPath);
            ValidateWeaponIcon(ShotgunDefinitionPath, ShotgunIconPath);
            ValidateWeaponIcon(MeleeDefinitionPath, MeleeIconPath);
        }

        private static void ConfigureIconImporter(string assetPath)
        {
            AssetDatabase.ImportAsset(
                assetPath,
                ImportAssetOptions.ForceSynchronousImport |
                ImportAssetOptions.ForceUpdate);
            TextureImporter importer =
                AssetImporter.GetAtPath(assetPath) as TextureImporter;
            Require(importer != null,
                $"HUD icon could not be imported: {assetPath}");

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 100f;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Bilinear;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.maxTextureSize = 256;
            importer.textureCompression = TextureImporterCompression.CompressedHQ;
            importer.SaveAndReimport();
        }

        private static void ConfigureWeaponIcon(
            string definitionPath,
            Sprite icon)
        {
            WeaponDefinition definition =
                AssetDatabase.LoadAssetAtPath<WeaponDefinition>(definitionPath);
            Require(definition != null,
                $"Weapon definition is missing: {definitionPath}");
            SerializedObject serializedDefinition =
                new SerializedObject(definition);
            SerializedProperty hudIcon =
                serializedDefinition.FindProperty("hudIcon");
            Require(hudIcon != null,
                $"Weapon HUD icon property is missing: {definitionPath}");
            hudIcon.objectReferenceValue = icon;
            serializedDefinition.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ValidateWeaponIcon(
            string definitionPath,
            string iconPath)
        {
            WeaponDefinition definition =
                AssetDatabase.LoadAssetAtPath<WeaponDefinition>(definitionPath);
            Sprite icon = AssetDatabase.LoadAssetAtPath<Sprite>(iconPath);
            Require(
                definition != null &&
                icon != null &&
                definition.HudIcon == icon,
                $"Weapon HUD icon is invalid: {definitionPath}");
        }

        private static Sprite LoadSprite(string assetPath)
        {
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
            Require(sprite != null, $"HUD sprite is missing: {assetPath}");
            return sprite;
        }

        private static void ValidatePngTransparency(string assetPath)
        {
            string projectRoot = Directory.GetParent(Application.dataPath).FullName;
            string fullPath = Path.Combine(
                projectRoot,
                assetPath.Replace('/', Path.DirectorySeparatorChar));
            Require(File.Exists(fullPath),
                $"HUD icon file is missing: {assetPath}");

            byte[] png = File.ReadAllBytes(fullPath);
            Texture2D texture = new Texture2D(
                2,
                2,
                TextureFormat.RGBA32,
                false);
            try
            {
                Require(ImageConversion.LoadImage(texture, png, false),
                    $"HUD icon is not a readable PNG: {assetPath}");
                Color32[] pixels = texture.GetPixels32();
                bool hasTransparentPixel = false;
                bool hasVisiblePixel = false;
                for (int i = 0; i < pixels.Length; i++)
                {
                    hasTransparentPixel |= pixels[i].a == 0;
                    hasVisiblePixel |= pixels[i].a >= 64;
                    if (hasTransparentPixel && hasVisiblePixel)
                    {
                        break;
                    }
                }

                Require(hasTransparentPixel && hasVisiblePixel,
                    $"HUD icon must contain transparent padding and visible pixels: {assetPath}");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(texture);
            }
        }

        private static void EnsureFolder(string parentPath, string folderName)
        {
            string path = parentPath + "/" + folderName;
            if (!AssetDatabase.IsValidFolder(path))
            {
                AssetDatabase.CreateFolder(parentPath, folderName);
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
