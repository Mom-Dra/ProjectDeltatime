using System;
using System.Collections.Generic;
using Deltatime.Combat;
using Deltatime.Enemies;
using Deltatime.InputSystem;
using Deltatime.Level;
using Deltatime.Player;
using Deltatime.Replay;
using Deltatime.TimeSystem;
using Deltatime.UI;
using Deltatime.Vision;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace Deltatime.EditorTools
{
    public static class PrototypeSceneBuilder
    {
        private const string Root = "Assets/_Project";
        private const string PlayerControlsPath = Root + "/Input/PlayerControls.inputactions";
        private const string Materials = Root + "/Materials";
        private const string Prefabs = Root + "/Prefabs";
        private const string Scenes = Root + "/Scenes";
        private const string TutorialScenePath = Scenes + "/Tutorial.unity";
        private const string Stage1ScenePath = Scenes + "/Stage1.unity";
        private const string Stage2ScenePath = Scenes + "/Stage2.unity";
        private const string NavigationDataPath =
            Scenes + "/StageNavigation.asset";
        private const string PistolDefinitionPath = Root + "/Pistol.asset";
        private const string AutomaticRifleDefinitionPath =
            Root + "/AutomaticRifle.asset";
        private const string ShotgunDefinitionPath = Root + "/Shotgun.asset";
        private const string MeleeWeaponDefinitionPath =
            Root + "/MeleeWeapon.asset";
        private const string LineMaterialPath = Materials + "/PrototypeLine.mat";
        private const string FloorMaterialPath = Materials + "/PrototypeFloor3D.mat";
        private const string WallMaterialPath = Materials + "/PrototypeWall3D.mat";
        private const string CoverMaterialPath = Materials + "/PrototypeCover3D.mat";
        private const string PlayerMaterialPath = Materials + "/PrototypePlayer3D.mat";
        private const string EnemyMaterialPath = Materials + "/PrototypeEnemy3D.mat";
        private const string ChaserMaterialPath =
            Materials + "/PrototypeChaser3D.mat";
        private const string WeaponMaterialPath = Materials + "/PrototypeWeapon3D.mat";
        private const string PickupMaterialPath = Materials + "/PrototypePickup3D.mat";
        private const string AccentMaterialPath = Materials + "/PrototypeAccent3D.mat";
        private const string VisionMaterialPath = Materials + "/PrototypeVisionCone3D.mat";
        private const string ProjectilePrefabPath = Prefabs + "/Projectile.prefab";
        private const string PickupPrefabPath = Prefabs + "/WeaponPickup.prefab";
        private const string PistolPickupPrefabPath = Prefabs + "/PistolPickup.prefab";
        private const string ShotgunPickupPrefabPath = Prefabs + "/ShotgunPickup.prefab";
        private const string ThrownWeaponPrefabPath = Prefabs + "/ThrownWeapon.prefab";
        private const string InterceptableWeaponPrefabPath =
            Prefabs + "/InterceptableWeapon.prefab";
        private const string VisionObstacleLayerName = "VisionObstacle";
        private const int VisionObstacleLayer = 8;
        private const int Stage1DeadlineCharges = 2;
        private const int Stage2DeadlineCharges = 2;

        [MenuItem("Tools/Prototype/Build Stage 1 + Stage 2")]
        public static void BuildPrototypeRoom()
        {
            EnsureFolders();
            EnsureVisionObstacleLayer();

            Material lineMaterial = EnsureLineMaterial();
            Material floorMaterial = EnsureStandardMaterial(
                FloorMaterialPath,
                new Color(0.055f, 0.07f, 0.09f, 1f),
                0.72f,
                0.2f);
            Material wallMaterial = EnsureStandardMaterial(
                WallMaterialPath,
                new Color(0.17f, 0.2f, 0.24f, 1f),
                0.8f,
                0.3f);
            Material coverMaterial = EnsureStandardMaterial(
                CoverMaterialPath,
                new Color(0.25f, 0.29f, 0.34f, 1f),
                0.65f,
                0.36f);
            Material playerMaterial = EnsureStandardMaterial(
                PlayerMaterialPath,
                new Color(0.04f, 0.72f, 0.86f, 1f),
                0.42f,
                0.58f,
                new Color(0.006f, 0.06f, 0.09f, 1f));
            Material enemyMaterial = EnsureStandardMaterial(
                EnemyMaterialPath,
                new Color(0.85f, 0.08f, 0.055f, 1f),
                0.38f,
                0.52f,
                new Color(0.06f, 0.004f, 0.003f, 1f));
            Material chaserMaterial = EnsureStandardMaterial(
                ChaserMaterialPath,
                new Color(1f, 0.38f, 0.035f, 1f),
                0.32f,
                0.48f,
                new Color(0.08f, 0.012f, 0.001f, 1f));
            Material weaponMaterial = EnsureStandardMaterial(
                WeaponMaterialPath,
                new Color(0.5f, 0.55f, 0.62f, 1f),
                0.9f,
                0.65f);
            Material pickupMaterial = EnsureStandardMaterial(
                PickupMaterialPath,
                new Color(1f, 0.55f, 0.035f, 1f),
                0.45f,
                0.55f,
                new Color(0.08f, 0.018f, 0.002f, 1f));
            Material accentMaterial = EnsureStandardMaterial(
                AccentMaterialPath,
                new Color(0.02f, 0.6f, 0.8f, 1f),
                0.25f,
                0.7f,
                new Color(0.004f, 0.06f, 0.1f, 1f));
            Material visionMaterial = EnsureTransparentMaterial(
                VisionMaterialPath,
                new Color(0.08f, 0.85f, 1f, 0.13f));

            WeaponDefinition pistol = EnsurePistolDefinition();
            WeaponDefinition automaticRifle =
                EnsureAutomaticRifleDefinition();
            WeaponDefinition shotgun = EnsureShotgunDefinition();
            WeaponDefinition meleeWeapon = EnsureMeleeWeaponDefinition();
            EnsureProjectilePrefab(lineMaterial);
            EnsurePickupPrefab(pickupMaterial);
            EnsureThrownWeaponPrefab(pickupMaterial, lineMaterial);
            EnsureInterceptableWeaponPrefab(pickupMaterial, lineMaterial);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

            pistol = AssetDatabase.LoadAssetAtPath<WeaponDefinition>(
                PistolDefinitionPath);
            automaticRifle = AssetDatabase.LoadAssetAtPath<WeaponDefinition>(
                AutomaticRifleDefinitionPath);
            shotgun = AssetDatabase.LoadAssetAtPath<WeaponDefinition>(
                ShotgunDefinitionPath);
            meleeWeapon = AssetDatabase.LoadAssetAtPath<WeaponDefinition>(
                MeleeWeaponDefinitionPath);
            EnsureConfiguredPickupPrefab(
                PistolPickupPrefabPath,
                "Pistol Pickup",
                pickupMaterial,
                pistol,
                8);
            EnsureConfiguredPickupPrefab(
                ShotgunPickupPrefabPath,
                "Shotgun Pickup",
                pickupMaterial,
                shotgun,
                6);
            AssetDatabase.SaveAssets();

            Scene scene = EditorSceneManager.NewScene(
                NewSceneSetup.EmptyScene,
                NewSceneMode.Single);

            Projectile projectilePrefab =
                LoadPrefabComponent<Projectile>(ProjectilePrefabPath);
            WeaponPickup pickupPrefab =
                LoadPrefabComponent<WeaponPickup>(PickupPrefabPath);
            WeaponPickup pistolPickupPrefab =
                LoadPrefabComponent<WeaponPickup>(PistolPickupPrefabPath);
            WeaponPickup shotgunPickupPrefab =
                LoadPrefabComponent<WeaponPickup>(ShotgunPickupPrefabPath);
            ThrownWeapon thrownPrefab =
                LoadPrefabComponent<ThrownWeapon>(ThrownWeaponPrefabPath);
            InterceptableWeapon interceptablePrefab =
                LoadPrefabComponent<InterceptableWeapon>(
                    InterceptableWeaponPrefabPath);

            WorldTimeActivity activity;
            WorldTimeController worldTime;
            GameObject systems = CreateSystems(out activity, out worldTime);
            Light keyLight = CreateLightingAndAtmosphere();
            Camera gameplayCamera = CreateCamera(worldTime, keyLight);
            StageReplayController replay =
                systems.AddComponent<StageReplayController>();
            ConfigureReplayOmniscientView(replay);

            CreateFloorAndWalls(
                floorMaterial,
                wallMaterial,
                coverMaterial,
                accentMaterial);
            CreateNavigationSurface();

            PlayerBundle player = CreatePlayer(
                playerMaterial,
                weaponMaterial,
                lineMaterial,
                visionMaterial,
                pistol,
                projectilePrefab,
                pickupPrefab,
                thrownPrefab,
                activity,
                worldTime,
                gameplayCamera,
                replay,
                Stage1DeadlineCharges);
            replay.Configure(worldTime, gameplayCamera, player.Deadline);

            TopDownCameraController cameraController =
                gameplayCamera.gameObject.AddComponent<TopDownCameraController>();
            cameraController.Configure(player.Root.transform, player.Aim, player.Input);
            cameraController.SnapToTarget();

            StageController stage = systems.AddComponent<StageController>();
            stage.Configure(player.Input, player.Health, player.Combat, replay);

            CreatePickup(
                new Vector3(-2.4f, 0.18f, -4.2f),
                pistolPickupPrefab,
                "Pistol Pickup");
            CreatePickup(
                new Vector3(2.4f, 0.18f, -4.2f),
                shotgunPickupPrefab,
                "Shotgun Pickup");

            CreateRangedEnemy(
                "Enemy West",
                new Vector3(-6.3f, 0.75f, 4.7f),
                enemyMaterial,
                weaponMaterial,
                lineMaterial,
                automaticRifle,
                projectilePrefab,
                pickupPrefab,
                thrownPrefab,
                interceptablePrefab,
                worldTime,
                player.Root.transform,
                player.Health,
                player.Vision,
                stage);
            CreateChasingEnemy(
                "Enemy Center",
                new Vector3(2.8f, 0.78f, 5.8f),
                chaserMaterial,
                weaponMaterial,
                lineMaterial,
                meleeWeapon,
                projectilePrefab,
                pickupPrefab,
                thrownPrefab,
                interceptablePrefab,
                worldTime,
                player.Root.transform,
                player.Health,
                player.Vision,
                stage);
            CreateRangedEnemy(
                "Enemy East",
                new Vector3(6.3f, 0.75f, 4.7f),
                enemyMaterial,
                weaponMaterial,
                lineMaterial,
                automaticRifle,
                projectilePrefab,
                pickupPrefab,
                thrownPrefab,
                interceptablePrefab,
                worldTime,
                player.Root.transform,
                player.Health,
                player.Vision,
                stage);

            GameObject hudObject = new GameObject("Debug HUD");
            GameHud hud = hudObject.AddComponent<GameHud>();
            hud.Configure(
                stage,
                worldTime,
                player.Health,
                player.Dash,
                player.Deadline,
                player.Weapon,
                replay);

            WorldTimeVisualFeedback visualFeedback =
                gameplayCamera.GetComponent<WorldTimeVisualFeedback>();

            ApplyStageLightingProfile(visualFeedback, keyLight, true);
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, Stage1ScenePath))
            {
                throw new InvalidOperationException($"Failed to save {Stage1ScenePath}.");
            }

            BuildNavigationSurface();
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, Stage1ScenePath))
            {
                throw new InvalidOperationException(
                    $"Failed to save baked navigation data for {Stage1ScenePath}.");
            }

            ApplyStageLightingProfile(visualFeedback, keyLight, false);
            player.Deadline.SetMaximumCharges(Stage2DeadlineCharges);
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, Stage2ScenePath))
            {
                throw new InvalidOperationException($"Failed to save {Stage2ScenePath}.");
            }

            AddStageScenesToBuildSettings();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            ValidateSavedPrototypeRoom();
            if (!Application.isBatchMode)
            {
                Selection.activeGameObject = GameObject.Find("Player");
            }
            Debug.Log(
                "Stage1 and Stage2 built successfully. Stage2 remains open.");
        }

        public static void BuildAndValidateFromCommandLine()
        {
            BuildPrototypeRoom();
        }

        public static void CapturePreviewFromCommandLine()
        {
            Scene scene = EditorSceneManager.OpenScene(
                Stage1ScenePath,
                OpenSceneMode.Single);
            ValidateScene(scene, Stage1DeadlineCharges);

            Camera camera = UnityEngine.Object.FindObjectOfType<Camera>();
            const int width = 1280;
            const int height = 720;
            RenderTexture target = new RenderTexture(width, height, 24);
            Texture2D preview = new Texture2D(width, height, TextureFormat.RGB24, false);
            RenderTexture previousActive = RenderTexture.active;
            RenderTexture previousTarget = camera.targetTexture;

            try
            {
                camera.targetTexture = target;
                RenderTexture.active = target;
                camera.Render();
                preview.ReadPixels(new Rect(0f, 0f, width, height), 0, 0);
                preview.Apply();

                string previewPath = System.IO.Path.Combine(
                    Application.dataPath,
                    "_Project",
                    "Art",
                    "Generated",
                    "Stage1Preview.png");
                System.IO.Directory.CreateDirectory(
                    System.IO.Path.GetDirectoryName(previewPath));
                System.IO.File.WriteAllBytes(previewPath, preview.EncodeToPNG());
                AssetDatabase.ImportAsset(
                    "Assets/_Project/Art/Generated/Stage1Preview.png",
                    ImportAssetOptions.ForceSynchronousImport);
                Debug.Log($"3D preview captured at {previewPath}.");
            }
            finally
            {
                camera.targetTexture = previousTarget;
                RenderTexture.active = previousActive;
                UnityEngine.Object.DestroyImmediate(preview);
                UnityEngine.Object.DestroyImmediate(target);
            }
        }

        [MenuItem("Tools/Prototype/Validate Stage 1 + Stage 2")]
        public static void ValidateSavedPrototypeRoom()
        {
            Scene stage1 = EditorSceneManager.OpenScene(
                Stage1ScenePath,
                OpenSceneMode.Single);
            ValidateScene(stage1, Stage1DeadlineCharges);

            Scene stage2 = EditorSceneManager.OpenScene(
                Stage2ScenePath,
                OpenSceneMode.Single);
            ValidateScene(stage2, Stage2DeadlineCharges);
            Debug.Log("Stage1 and Stage2 validation passed.");
        }

        private static GameObject CreateSystems(
            out WorldTimeActivity activity,
            out WorldTimeController worldTime)
        {
            GameObject systems = new GameObject("Systems");
            activity = systems.AddComponent<WorldTimeActivity>();
            worldTime = systems.AddComponent<WorldTimeController>();
            worldTime.Configure(activity);
            return systems;
        }

        private static Camera CreateCamera(
            WorldTimeController worldTime,
            Light keyLight)
        {
            GameObject cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            cameraObject.transform.position = new Vector3(0f, 13.5f, -18.7f);
            cameraObject.transform.rotation = Quaternion.Euler(47f, 0f, 0f);

            Camera camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = false;
            camera.fieldOfView = 49f;
            camera.nearClipPlane = 0.15f;
            camera.farClipPlane = 80f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.004f, 0.007f, 0.012f, 1f);
            camera.allowHDR = true;

            WorldTimeVisualFeedback feedback =
                cameraObject.AddComponent<WorldTimeVisualFeedback>();
            feedback.Configure(worldTime, camera, keyLight);
            return camera;
        }

        private static Light CreateLightingAndAtmosphere()
        {
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.012f, 0.016f, 0.022f, 1f);
            RenderSettings.ambientEquatorColor = new Color(0.006f, 0.008f, 0.012f, 1f);
            RenderSettings.ambientGroundColor = new Color(0.002f, 0.003f, 0.005f, 1f);
            RenderSettings.ambientIntensity = 0.35f;
            RenderSettings.reflectionIntensity = 0.08f;
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = new Color(0.004f, 0.007f, 0.012f, 1f);
            RenderSettings.fogStartDistance = 19f;
            RenderSettings.fogEndDistance = 42f;

            GameObject keyLightObject = new GameObject("Directional Key Light");
            keyLightObject.transform.rotation = Quaternion.Euler(52f, -32f, 0f);
            Light keyLight = keyLightObject.AddComponent<Light>();
            keyLight.type = LightType.Directional;
            keyLight.color = new Color(0.72f, 0.84f, 1f, 1f);
            keyLight.intensity = 0.06f;
            keyLight.shadows = LightShadows.None;

            CreatePointLight(
                "Blue Bay Light",
                new Vector3(-6f, 3.4f, -1f),
                new Color(0.05f, 0.55f, 1f, 1f),
                0.15f,
                4f);
            CreatePointLight(
                "Red Alert Light",
                new Vector3(6f, 3.4f, 3f),
                new Color(1f, 0.12f, 0.045f, 1f),
                0.12f,
                4f);

            return keyLight;
        }

        private static void ConfigureReplayOmniscientView(
            StageReplayController replay)
        {
            SerializedObject settings = new SerializedObject(replay);
            settings.FindProperty("omniscientAmbientSkyColor").colorValue =
                new Color(0.30f, 0.34f, 0.40f, 1f);
            settings.FindProperty("omniscientAmbientEquatorColor").colorValue =
                new Color(0.22f, 0.25f, 0.30f, 1f);
            settings.FindProperty("omniscientAmbientGroundColor").colorValue =
                new Color(0.12f, 0.14f, 0.17f, 1f);
            settings.FindProperty("omniscientAmbientIntensity").floatValue = 1f;
            settings.FindProperty("omniscientReflectionIntensity").floatValue =
                0.35f;
            settings.FindProperty("omniscientBackgroundColor").colorValue =
                new Color(0.025f, 0.04f, 0.065f, 1f);
            settings.FindProperty("omniscientFillLightColor").colorValue =
                new Color(0.78f, 0.86f, 1f, 1f);
            settings.FindProperty("omniscientFillLightIntensity").floatValue =
                0.65f;
            settings.FindProperty("omniscientFillLightRotation").vector3Value =
                new Vector3(50f, -30f, 0f);
            settings.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ApplyStageLightingProfile(
            WorldTimeVisualFeedback visualFeedback,
            Light keyLight,
            bool brightStage)
        {
            if (visualFeedback == null || keyLight == null)
            {
                throw new InvalidOperationException(
                    "Stage lighting requires visual feedback and a key light.");
            }

            Color ambientSky = brightStage
                ? new Color(0.12f, 0.17f, 0.22f, 1f)
                : new Color(0.012f, 0.016f, 0.022f, 1f);
            Color ambientEquator = brightStage
                ? new Color(0.055f, 0.075f, 0.095f, 1f)
                : new Color(0.006f, 0.008f, 0.012f, 1f);
            Color ambientGround = brightStage
                ? new Color(0.018f, 0.024f, 0.032f, 1f)
                : new Color(0.002f, 0.003f, 0.005f, 1f);
            Color stageFogColor = brightStage
                ? new Color(0.035f, 0.055f, 0.09f, 1f)
                : new Color(0.004f, 0.007f, 0.012f, 1f);
            float stageAmbientIntensity = brightStage ? 1f : 0.35f;
            float stageReflectionIntensity = brightStage ? 0.65f : 0.08f;
            float stageDirectionalIntensity = brightStage ? 0.9f : 0.06f;
            float stageFogStart = brightStage ? 35f : 19f;
            float stageFogEnd = brightStage ? 70f : 42f;
            float stageMapFillIntensity = brightStage ? 1.5f : 0f;

            SerializedObject settings = new SerializedObject(visualFeedback);
            settings.FindProperty("ambientSkyColor").colorValue = ambientSky;
            settings.FindProperty("ambientEquatorColor").colorValue =
                ambientEquator;
            settings.FindProperty("ambientGroundColor").colorValue =
                ambientGround;
            settings.FindProperty("ambientIntensity").floatValue =
                stageAmbientIntensity;
            settings.FindProperty("reflectionIntensity").floatValue =
                stageReflectionIntensity;
            settings.FindProperty("directionalLightIntensity").floatValue =
                stageDirectionalIntensity;
            settings.FindProperty("fogColor").colorValue = stageFogColor;
            settings.FindProperty("fogStartDistance").floatValue =
                stageFogStart;
            settings.FindProperty("fogEndDistance").floatValue = stageFogEnd;
            settings.FindProperty("mapFillLightIntensity").floatValue =
                stageMapFillIntensity;
            settings.ApplyModifiedPropertiesWithoutUndo();

            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = ambientSky;
            RenderSettings.ambientEquatorColor = ambientEquator;
            RenderSettings.ambientGroundColor = ambientGround;
            RenderSettings.ambientIntensity = stageAmbientIntensity;
            RenderSettings.reflectionIntensity = stageReflectionIntensity;
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = stageFogColor;
            RenderSettings.fogStartDistance = stageFogStart;
            RenderSettings.fogEndDistance = stageFogEnd;

            keyLight.intensity = stageDirectionalIntensity;
            keyLight.shadows = LightShadows.None;
            EditorUtility.SetDirty(visualFeedback);
            EditorUtility.SetDirty(keyLight);
        }

        private static void CreatePointLight(
            string name,
            Vector3 position,
            Color color,
            float intensity,
            float range)
        {
            GameObject lightObject = new GameObject(name);
            lightObject.transform.position = position;
            Light light = lightObject.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = color;
            light.intensity = intensity;
            light.range = range;
            light.shadows = LightShadows.None;
        }

        private static PlayerBundle CreatePlayer(
            Material playerMaterial,
            Material weaponMaterial,
            Material lineMaterial,
            Material visionMaterial,
            WeaponDefinition pistol,
            Projectile projectilePrefab,
            WeaponPickup pickupPrefab,
            ThrownWeapon thrownPrefab,
            WorldTimeActivity activity,
            WorldTimeController worldTime,
            Camera gameplayCamera,
            StageReplayController replay,
            int deadlineMaximumCharges)
        {
            GameObject root = CreatePrimitiveObject(
                "Player",
                PrimitiveType.Capsule,
                new Vector3(0f, 0.75f, -6.2f),
                new Vector3(0.82f, 0.75f, 0.82f),
                playerMaterial,
                true);

            Rigidbody body = root.AddComponent<Rigidbody>();
            body.useGravity = false;
            body.linearDamping = 8f;
            body.angularDamping = 10f;
            body.collisionDetectionMode = CollisionDetectionMode.Continuous;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.constraints =
                RigidbodyConstraints.FreezePositionY |
                RigidbodyConstraints.FreezeRotationX |
                RigidbodyConstraints.FreezeRotationZ;

            PlayerInputReader input = root.AddComponent<PlayerInputReader>();
            input.Configure(activity);

            Renderer playerRenderer = root.GetComponent<Renderer>();
            PlayerHealth health = root.AddComponent<PlayerHealth>();
            health.Configure(playerRenderer);

            PlayerDash dash = root.AddComponent<PlayerDash>();
            dash.Configure(input, health, activity, worldTime);

            PlayerMovement movement = root.AddComponent<PlayerMovement>();
            movement.Configure(input, health, dash, worldTime);

            LineRenderer aimLine = root.AddComponent<LineRenderer>();
            ConfigureLine(
                aimLine,
                lineMaterial,
                new Color(0.2f, 1f, 1f, 0.85f),
                0.045f);

            PlayerAim aim = root.AddComponent<PlayerAim>();
            aim.Configure(input, activity, gameplayCamera, aimLine);

            Transform muzzle;
            Renderer heldWeaponRenderer;
            CreateWeaponVisual(
                root.transform,
                weaponMaterial,
                out muzzle,
                out heldWeaponRenderer);

            WeaponController weapon = root.AddComponent<WeaponController>();
            weapon.Configure(
                muzzle,
                heldWeaponRenderer,
                projectilePrefab,
                pickupPrefab,
                thrownPrefab,
                pistol);

            PlayerCombat combat = root.AddComponent<PlayerCombat>();
            DeadlineController deadline =
                root.AddComponent<DeadlineController>();
            combat.Configure(
                input,
                aim,
                health,
                weapon,
                worldTime,
                activity,
                deadline);
            deadline.Configure(
                input,
                movement,
                health,
                combat,
                worldTime,
                deadlineMaximumCharges);

            GameObject visionObject = new GameObject("Vision Cone");
            visionObject.transform.SetParent(root.transform, false);
            visionObject.transform.localPosition = new Vector3(0f, -1f, 0f);
            visionObject.transform.localScale = new Vector3(
                1f / root.transform.localScale.x,
                1f / root.transform.localScale.y,
                1f / root.transform.localScale.z);
            visionObject.AddComponent<MeshFilter>();
            visionObject.AddComponent<MeshRenderer>();
            VisionCone visionCone = visionObject.AddComponent<VisionCone>();
            visionCone.Configure(
                1 << VisionObstacleLayer,
                visionMaterial,
                replay);

            return new PlayerBundle(
                root,
                input,
                aim,
                dash,
                health,
                combat,
                deadline,
                weapon,
                visionCone);
        }

        private static void CreateRangedEnemy(
            string name,
            Vector3 position,
            Material enemyMaterial,
            Material weaponMaterial,
            Material lineMaterial,
            WeaponDefinition automaticRifle,
            Projectile projectilePrefab,
            WeaponPickup pickupPrefab,
            ThrownWeapon thrownPrefab,
            InterceptableWeapon interceptablePrefab,
            WorldTimeController worldTime,
            Transform player,
            PlayerHealth playerHealth,
            VisionCone playerVision,
            StageController stage)
        {
            GameObject root = CreatePrimitiveObject(
                name,
                PrimitiveType.Capsule,
                position,
                new Vector3(0.85f, 0.75f, 0.85f),
                enemyMaterial,
                true);

            Rigidbody body = root.AddComponent<Rigidbody>();
            body.isKinematic = true;
            body.useGravity = false;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.constraints =
                RigidbodyConstraints.FreezePositionY |
                RigidbodyConstraints.FreezeRotationX |
                RigidbodyConstraints.FreezeRotationZ;

            Collider collider = root.GetComponent<Collider>();

            Transform muzzle;
            Renderer heldWeaponRenderer;
            CreateWeaponVisual(
                root.transform,
                weaponMaterial,
                out muzzle,
                out heldWeaponRenderer);

            WeaponController weapon = root.AddComponent<WeaponController>();
            weapon.Configure(
                muzzle,
                heldWeaponRenderer,
                projectilePrefab,
                pickupPrefab,
                thrownPrefab,
                automaticRifle);

            LineRenderer warningLine = root.AddComponent<LineRenderer>();
            ConfigureLine(
                warningLine,
                lineMaterial,
                new Color(1f, 0.08f, 0.04f, 0.85f),
                0.035f);
            warningLine.enabled = false;

            EnemyWeaponDrop drop = root.AddComponent<EnemyWeaponDrop>();
            drop.Configure(
                pickupPrefab,
                interceptablePrefab,
                weapon,
                worldTime);

            EnemyMotor motor = root.AddComponent<EnemyMotor>();
            motor.Configure(worldTime, 3.4f, 220f);

            EnemyPerception perception =
                root.AddComponent<EnemyPerception>();
            perception.Configure(
                player,
                playerHealth,
                root.transform,
                18f);

            EnemyShooter shooter = root.AddComponent<EnemyShooter>();
            Renderer bodyRenderer = root.GetComponent<Renderer>();
            shooter.Configure(
                worldTime,
                perception,
                motor,
                weapon,
                drop,
                warningLine,
                playerVision,
                bodyRenderer,
                heldWeaponRenderer,
                ~0);

            EnemyHealth health = root.AddComponent<EnemyHealth>();
            health.Configure(
                shooter,
                drop,
                stage,
                collider,
                bodyRenderer);
        }

        private static void CreateChasingEnemy(
            string name,
            Vector3 position,
            Material enemyMaterial,
            Material weaponMaterial,
            Material lineMaterial,
            WeaponDefinition meleeWeapon,
            Projectile projectilePrefab,
            WeaponPickup pickupPrefab,
            ThrownWeapon thrownPrefab,
            InterceptableWeapon interceptablePrefab,
            WorldTimeController worldTime,
            Transform player,
            PlayerHealth playerHealth,
            VisionCone playerVision,
            StageController stage)
        {
            GameObject root = CreatePrimitiveObject(
                name,
                PrimitiveType.Capsule,
                position,
                new Vector3(0.9f, 0.78f, 0.9f),
                enemyMaterial,
                true);

            Rigidbody body = root.AddComponent<Rigidbody>();
            body.isKinematic = true;
            body.useGravity = false;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.constraints =
                RigidbodyConstraints.FreezePositionY |
                RigidbodyConstraints.FreezeRotationX |
                RigidbodyConstraints.FreezeRotationZ;

            Collider collider = root.GetComponent<Collider>();

            Transform weaponTip;
            Renderer heldWeaponRenderer;
            CreateWeaponVisual(
                root.transform,
                weaponMaterial,
                out weaponTip,
                out heldWeaponRenderer);
            heldWeaponRenderer.transform.localScale =
                meleeWeapon.HeldVisualScale;

            WeaponController weapon = root.AddComponent<WeaponController>();
            weapon.Configure(
                weaponTip,
                heldWeaponRenderer,
                projectilePrefab,
                pickupPrefab,
                thrownPrefab,
                meleeWeapon);

            EnemyWeaponDrop drop = root.AddComponent<EnemyWeaponDrop>();
            drop.Configure(
                pickupPrefab,
                interceptablePrefab,
                weapon,
                worldTime);

            LineRenderer warningLine = root.AddComponent<LineRenderer>();
            ConfigureLine(
                warningLine,
                lineMaterial,
                new Color(1f, 0.55f, 0.03f, 0.9f),
                0.065f);
            warningLine.enabled = false;

            EnemyMotor motor = root.AddComponent<EnemyMotor>();
            motor.Configure(worldTime, 4.8f, 260f, 0.1f);

            EnemyPerception perception =
                root.AddComponent<EnemyPerception>();
            perception.Configure(
                player,
                playerHealth,
                root.transform,
                20f);

            EnemyChaser chaser = root.AddComponent<EnemyChaser>();
            Renderer bodyRenderer = root.GetComponent<Renderer>();
            chaser.Configure(
                worldTime,
                perception,
                motor,
                weapon,
                drop,
                warningLine,
                playerVision,
                bodyRenderer,
                heldWeaponRenderer,
                ~0);

            EnemyHealth health = root.AddComponent<EnemyHealth>();
            health.Configure(
                chaser,
                drop,
                stage,
                collider,
                bodyRenderer);
        }

        private static void CreateNavigationSurface()
        {
            GameObject navigationObject = new GameObject("Navigation");
            NavMeshSurface surface =
                navigationObject.AddComponent<NavMeshSurface>();
            surface.collectObjects = CollectObjects.All;
            surface.useGeometry =
                NavMeshCollectGeometry.PhysicsColliders;
            surface.layerMask = ~0;
        }

        private static void BuildNavigationSurface()
        {
            NavMeshSurface surface =
                UnityEngine.Object.FindObjectOfType<NavMeshSurface>();
            if (surface == null)
            {
                throw new InvalidOperationException(
                    "Navigation surface is missing.");
            }

            Physics.SyncTransforms();
            surface.BuildNavMesh();
            if (surface.navMeshData == null)
            {
                throw new InvalidOperationException(
                    "Navigation surface failed to build.");
            }

            NavMeshData bakedData = surface.navMeshData;
            NavMeshData savedData =
                AssetDatabase.LoadAssetAtPath<NavMeshData>(
                    NavigationDataPath);
            if (savedData == null)
            {
                AssetDatabase.CreateAsset(
                    bakedData,
                    NavigationDataPath);
            }
            else
            {
                surface.RemoveData();
                EditorUtility.CopySerialized(bakedData, savedData);
                surface.navMeshData = savedData;
                surface.AddData();
                UnityEngine.Object.DestroyImmediate(bakedData);
                EditorUtility.SetDirty(savedData);
            }

            EditorUtility.SetDirty(surface);
            AssetDatabase.SaveAssets();
        }

        private static void CreateFloorAndWalls(
            Material floorMaterial,
            Material wallMaterial,
            Material coverMaterial,
            Material accentMaterial)
        {
            GameObject environment = new GameObject("Industrial Room");

            GameObject floor = CreatePrimitiveObject(
                "Floor",
                PrimitiveType.Cube,
                new Vector3(0f, -0.12f, 0f),
                new Vector3(20f, 0.24f, 18f),
                floorMaterial,
                true);
            floor.transform.SetParent(environment.transform);

            CreateWall(
                environment.transform,
                "North Wall",
                new Vector3(0f, 1.15f, 9f),
                new Vector3(20.5f, 2.3f, 0.5f),
                wallMaterial);
            CreateWall(
                environment.transform,
                "South Wall",
                new Vector3(0f, 1.15f, -9f),
                new Vector3(20.5f, 2.3f, 0.5f),
                wallMaterial);
            CreateWall(
                environment.transform,
                "West Wall",
                new Vector3(-10f, 1.15f, 0f),
                new Vector3(0.5f, 2.3f, 18.5f),
                wallMaterial);
            CreateWall(
                environment.transform,
                "East Wall",
                new Vector3(10f, 1.15f, 0f),
                new Vector3(0.5f, 2.3f, 18.5f),
                wallMaterial);

            CreateWall(
                environment.transform,
                "West Cover",
                new Vector3(-5.2f, 0.72f, -0.5f),
                new Vector3(2.7f, 1.44f, 0.65f),
                coverMaterial);
            CreateWall(
                environment.transform,
                "East Cover",
                new Vector3(5.2f, 0.72f, 0.8f),
                new Vector3(2.7f, 1.44f, 0.65f),
                coverMaterial);
            CreateWall(
                environment.transform,
                "Center Cover",
                new Vector3(0f, 0.58f, 1.4f),
                new Vector3(2.2f, 1.16f, 0.55f),
                coverMaterial);

            CreateCrateStack(
                environment.transform,
                new Vector3(-7.5f, 0f, 5.9f),
                coverMaterial);
            CreateCrateStack(
                environment.transform,
                new Vector3(7.4f, 0f, -4.8f),
                coverMaterial);
            CreateFloorAccents(environment.transform, accentMaterial);
        }

        private static void CreateCrateStack(
            Transform parent,
            Vector3 basePosition,
            Material material)
        {
            Vector3[] offsets =
            {
                new Vector3(-0.48f, 0.45f, 0f),
                new Vector3(0.48f, 0.45f, 0f),
                new Vector3(0f, 1.35f, 0f)
            };

            for (int i = 0; i < offsets.Length; i++)
            {
                GameObject crate = CreatePrimitiveObject(
                    $"Cargo Crate {i + 1}",
                    PrimitiveType.Cube,
                    basePosition + offsets[i],
                    new Vector3(0.86f, 0.86f, 0.86f),
                    material,
                    true);
                crate.layer = VisionObstacleLayer;
                crate.transform.SetParent(parent);
            }
        }

        private static void CreateFloorAccents(Transform parent, Material material)
        {
            for (int i = -3; i <= 3; i++)
            {
                GameObject strip = CreatePrimitiveObject(
                    $"Floor Guide {i + 4}",
                    PrimitiveType.Cube,
                    new Vector3(i * 2.5f, 0.015f, -7.45f),
                    new Vector3(1.25f, 0.03f, 0.08f),
                    material,
                    false);
                strip.transform.SetParent(parent);
            }

            GameObject centerStrip = CreatePrimitiveObject(
                "Center Guide",
                PrimitiveType.Cube,
                new Vector3(0f, 0.015f, -1.5f),
                new Vector3(0.08f, 0.03f, 7f),
                material,
                false);
            centerStrip.transform.SetParent(parent);
        }

        private static void CreateWall(
            Transform parent,
            string name,
            Vector3 position,
            Vector3 scale,
            Material material)
        {
            GameObject wall = CreatePrimitiveObject(
                name,
                PrimitiveType.Cube,
                position,
                scale,
                material,
                true);
            wall.layer = VisionObstacleLayer;
            wall.transform.SetParent(parent);
        }

        private static void CreatePickup(
            Vector3 position,
            WeaponPickup pickupPrefab,
            string pickupName)
        {
            GameObject pickupObject = PrefabUtility.InstantiatePrefab(
                pickupPrefab.gameObject) as GameObject;
            if (pickupObject == null)
            {
                throw new InvalidOperationException(
                    $"Failed to instantiate {pickupName} prefab.");
            }

            pickupObject.name = pickupName;
            pickupObject.transform.SetPositionAndRotation(
                position,
                Quaternion.Euler(0f, 28f, 0f));
            EditorSceneManager.MarkSceneDirty(pickupObject.scene);
        }

        private static GameObject CreatePrimitiveObject(
            string name,
            PrimitiveType primitiveType,
            Vector3 position,
            Vector3 scale,
            Material material,
            bool keepCollider)
        {
            GameObject gameObject = GameObject.CreatePrimitive(primitiveType);
            gameObject.name = name;
            gameObject.transform.position = position;
            gameObject.transform.localScale = scale;
            Renderer renderer = gameObject.GetComponent<Renderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.On;
            renderer.receiveShadows = true;

            if (!keepCollider)
            {
                Collider collider = gameObject.GetComponent<Collider>();
                if (collider != null)
                {
                    UnityEngine.Object.DestroyImmediate(collider);
                }
            }

            return gameObject;
        }

        private static void CreateWeaponVisual(
            Transform owner,
            Material weaponMaterial,
            out Transform muzzle,
            out Renderer heldWeaponRenderer)
        {
            GameObject visual = CreatePrimitiveObject(
                "Held Weapon",
                PrimitiveType.Cube,
                Vector3.zero,
                new Vector3(0.18f, 0.16f, 0.78f),
                weaponMaterial,
                false);
            visual.transform.SetParent(owner, false);
            visual.transform.localPosition = new Vector3(0.24f, 0.48f, 0.58f);
            visual.transform.localScale = new Vector3(0.18f, 0.16f, 0.78f);
            heldWeaponRenderer = visual.GetComponent<Renderer>();

            GameObject muzzleObject = new GameObject("Muzzle");
            muzzleObject.transform.SetParent(owner, false);
            muzzleObject.transform.localPosition = new Vector3(0.24f, 0.48f, 1.08f);
            muzzle = muzzleObject.transform;
        }

        private static void EnsureProjectilePrefab(Material lineMaterial)
        {
            GameObject root = new GameObject("Projectile");
            LineRenderer line = root.AddComponent<LineRenderer>();
            ConfigureLine(line, lineMaterial, Color.white, 0.075f);
            root.AddComponent<Projectile>();

            PrefabUtility.SaveAsPrefabAsset(root, ProjectilePrefabPath);
            UnityEngine.Object.DestroyImmediate(root);
        }

        private static void EnsurePickupPrefab(Material pickupMaterial)
        {
            GameObject root = CreatePrimitiveObject(
                "WeaponPickup",
                PrimitiveType.Cube,
                new Vector3(0f, 0.18f, 0f),
                new Vector3(0.82f, 0.16f, 0.26f),
                pickupMaterial,
                true);
            BoxCollider collider = root.GetComponent<BoxCollider>();
            collider.isTrigger = true;
            root.AddComponent<WeaponPickup>();

            PrefabUtility.SaveAsPrefabAsset(root, PickupPrefabPath);
            UnityEngine.Object.DestroyImmediate(root);
        }

        private static void EnsureConfiguredPickupPrefab(
            string path,
            string pickupName,
            Material pickupMaterial,
            WeaponDefinition definition,
            int ammunition)
        {
            GameObject root = CreatePrimitiveObject(
                pickupName,
                PrimitiveType.Cube,
                new Vector3(0f, 0.18f, 0f),
                new Vector3(0.82f, 0.16f, 0.26f),
                pickupMaterial,
                true);
            BoxCollider collider = root.GetComponent<BoxCollider>();
            collider.isTrigger = true;
            WeaponPickup pickup = root.AddComponent<WeaponPickup>();
            pickup.Initialize(definition, ammunition);

            PrefabUtility.SaveAsPrefabAsset(root, path);
            UnityEngine.Object.DestroyImmediate(root);
        }

        private static void EnsureThrownWeaponPrefab(
            Material pickupMaterial,
            Material lineMaterial)
        {
            GameObject root = CreatePrimitiveObject(
                "ThrownWeapon",
                PrimitiveType.Cube,
                new Vector3(0f, 0.55f, 0f),
                new Vector3(0.82f, 0.14f, 0.24f),
                pickupMaterial,
                false);
            LineRenderer line = root.AddComponent<LineRenderer>();
            ConfigureLine(
                line,
                lineMaterial,
                new Color(1f, 0.65f, 0.08f, 1f),
                0.055f);
            ThrownWeapon thrownWeapon = root.AddComponent<ThrownWeapon>();
            thrownWeapon.ConfigurePrototype(7f, 6f, 2f);

            PrefabUtility.SaveAsPrefabAsset(root, ThrownWeaponPrefabPath);
            UnityEngine.Object.DestroyImmediate(root);
        }

        private static void EnsureInterceptableWeaponPrefab(
            Material pickupMaterial,
            Material lineMaterial)
        {
            GameObject root = new GameObject("InterceptableWeapon");

            GameObject body = CreatePrimitiveObject(
                "Body",
                PrimitiveType.Cube,
                Vector3.zero,
                new Vector3(0.82f, 0.14f, 0.24f),
                pickupMaterial,
                false);
            UnityEngine.Object.DestroyImmediate(body.GetComponent<Collider>());
            body.transform.SetParent(root.transform, false);

            SphereCollider catchCollider = root.AddComponent<SphereCollider>();
            catchCollider.radius = 0.42f;
            catchCollider.isTrigger = true;

            GameObject trailObject = new GameObject("Trail");
            trailObject.transform.SetParent(root.transform, false);
            LineRenderer trail = trailObject.AddComponent<LineRenderer>();
            ConfigureLine(
                trail,
                lineMaterial,
                new Color(1f, 0.65f, 0.08f, 1f),
                0.055f);

            GameObject predictionObject = new GameObject("Prediction");
            predictionObject.transform.SetParent(root.transform, false);
            LineRenderer prediction =
                predictionObject.AddComponent<LineRenderer>();
            ConfigureLine(
                prediction,
                lineMaterial,
                new Color(1f, 0.78f, 0.15f, 0.72f),
                0.035f);
            prediction.startWidth = 0.035f;
            prediction.endWidth = 0.035f;
            prediction.enabled = false;

            GameObject landingMarker = GameObject.CreatePrimitive(
                PrimitiveType.Cylinder);
            landingMarker.name = "Landing Marker";
            landingMarker.transform.SetParent(root.transform, false);
            landingMarker.transform.localScale =
                new Vector3(0.7f, 0.012f, 0.7f);
            UnityEngine.Object.DestroyImmediate(
                landingMarker.GetComponent<Collider>());
            Renderer landingRenderer =
                landingMarker.GetComponent<Renderer>();
            landingRenderer.sharedMaterial = pickupMaterial;
            landingRenderer.enabled = false;

            InterceptableWeapon interceptable =
                root.AddComponent<InterceptableWeapon>();
            interceptable.ConfigureVisuals(
                trail,
                prediction,
                landingRenderer,
                body.GetComponent<Renderer>(),
                1 << VisionObstacleLayer);

            PrefabUtility.SaveAsPrefabAsset(
                root,
                InterceptableWeaponPrefabPath);
            UnityEngine.Object.DestroyImmediate(root);
        }

        private static T LoadPrefabComponent<T>(string path) where T : Component
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null)
            {
                throw new InvalidOperationException($"Prefab was not created at {path}.");
            }

            T component = prefab.GetComponent<T>();
            if (component == null)
            {
                throw new InvalidOperationException(
                    $"Prefab at {path} does not contain {typeof(T).Name}.");
            }

            return component;
        }

        private static void ConfigureLine(
            LineRenderer line,
            Material material,
            Color color,
            float width)
        {
            line.useWorldSpace = true;
            line.positionCount = 2;
            line.startWidth = width;
            line.endWidth = width * 0.35f;
            line.startColor = color;
            line.endColor = new Color(color.r, color.g, color.b, color.a * 0.25f);
            line.sharedMaterial = material;
            line.numCapVertices = 3;
            line.numCornerVertices = 2;
            line.alignment = LineAlignment.View;
        }

        private static WeaponDefinition EnsurePistolDefinition()
        {
            WeaponDefinition definition =
                AssetDatabase.LoadAssetAtPath<WeaponDefinition>(PistolDefinitionPath);
            if (definition == null)
            {
                definition = ScriptableObject.CreateInstance<WeaponDefinition>();
                AssetDatabase.CreateAsset(definition, PistolDefinitionPath);
            }

            definition.ConfigureFirearmPrototype(
                "Pistol",
                8,
                0.24f,
                17f,
                3,
                0.08f,
                1,
                WeaponFireMode.SemiAutomatic,
                1,
                0f,
                1.5f,
                101,
                0f,
                0f);
            EditorUtility.SetDirty(definition);
            return definition;
        }

        private static WeaponDefinition EnsureAutomaticRifleDefinition()
        {
            WeaponDefinition definition =
                AssetDatabase.LoadAssetAtPath<WeaponDefinition>(
                    AutomaticRifleDefinitionPath);
            if (definition == null)
            {
                definition =
                    ScriptableObject.CreateInstance<WeaponDefinition>();
                AssetDatabase.CreateAsset(
                    definition,
                    AutomaticRifleDefinitionPath);
            }

            definition.ConfigureFirearmPrototype(
                "Automatic Rifle",
                30,
                0.12f,
                16f,
                3,
                0.075f,
                4,
                WeaponFireMode.Automatic,
                1,
                0f,
                1.5f,
                211,
                0f,
                0f);
            EditorUtility.SetDirty(definition);
            return definition;
        }

        private static WeaponDefinition EnsureShotgunDefinition()
        {
            WeaponDefinition definition =
                AssetDatabase.LoadAssetAtPath<WeaponDefinition>(
                    ShotgunDefinitionPath);
            if (definition == null)
            {
                definition = ScriptableObject.CreateInstance<WeaponDefinition>();
                AssetDatabase.CreateAsset(definition, ShotgunDefinitionPath);
            }

            definition.ConfigureFirearmPrototype(
                "Shotgun",
                6,
                0.75f,
                16f,
                1,
                0.075f,
                1,
                WeaponFireMode.SemiAutomatic,
                8,
                18f,
                1f,
                307,
                0.35f,
                14f);
            EditorUtility.SetDirty(definition);
            return definition;
        }

        private static WeaponDefinition EnsureMeleeWeaponDefinition()
        {
            WeaponDefinition definition =
                AssetDatabase.LoadAssetAtPath<WeaponDefinition>(
                    MeleeWeaponDefinitionPath);
            if (definition == null)
            {
                definition =
                    ScriptableObject.CreateInstance<WeaponDefinition>();
                AssetDatabase.CreateAsset(
                    definition,
                    MeleeWeaponDefinitionPath);
            }

            definition.ConfigureMeleePrototype(
                "Melee Weapon",
                0.72f,
                3,
                1.45f,
                35f);
            EditorUtility.SetDirty(definition);
            return definition;
        }

        private static Material EnsureLineMaterial()
        {
            Shader shader = Shader.Find("Sprites/Default");
            if (shader == null)
            {
                shader = Shader.Find("Unlit/Color");
            }

            if (shader == null)
            {
                throw new InvalidOperationException("No built-in unlit shader was available.");
            }

            Material material = AssetDatabase.LoadAssetAtPath<Material>(LineMaterialPath);
            if (material == null)
            {
                material = new Material(shader)
                {
                    name = "Prototype Line Material"
                };
                AssetDatabase.CreateAsset(material, LineMaterialPath);
            }

            material.shader = shader;
            material.color = Color.white;
            EditorUtility.SetDirty(material);
            return material;
        }

        private static Material EnsureStandardMaterial(
            string assetPath,
            Color color,
            float metallic,
            float smoothness,
            Color? emission = null)
        {
            Shader shader = Shader.Find("Standard");
            if (shader == null)
            {
                throw new InvalidOperationException("The built-in Standard shader is missing.");
            }

            Material material = AssetDatabase.LoadAssetAtPath<Material>(assetPath);
            if (material == null)
            {
                material = new Material(shader)
                {
                    name = System.IO.Path.GetFileNameWithoutExtension(assetPath)
                };
                AssetDatabase.CreateAsset(material, assetPath);
            }

            material.shader = shader;
            material.color = color;
            material.SetFloat("_Metallic", metallic);
            material.SetFloat("_Glossiness", smoothness);
            if (emission.HasValue)
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", emission.Value);
            }
            else
            {
                material.DisableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", Color.black);
            }

            EditorUtility.SetDirty(material);
            return material;
        }

        private static Material EnsureTransparentMaterial(string assetPath, Color color)
        {
            Material material = EnsureStandardMaterial(
                assetPath,
                color,
                0f,
                0.1f);
            material.SetFloat("_Mode", 3f);
            material.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
            material.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
            material.SetInt("_ZWrite", 0);
            material.DisableKeyword("_ALPHATEST_ON");
            material.EnableKeyword("_ALPHABLEND_ON");
            material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            material.renderQueue = (int)RenderQueue.Transparent;
            EditorUtility.SetDirty(material);
            return material;
        }

        private static void EnsureVisionObstacleLayer()
        {
            UnityEngine.Object[] tagManagerAssets =
                AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset");
            if (tagManagerAssets.Length == 0)
            {
                throw new InvalidOperationException("TagManager asset could not be loaded.");
            }

            SerializedObject tagManager = new SerializedObject(tagManagerAssets[0]);
            SerializedProperty layers = tagManager.FindProperty("layers");
            SerializedProperty layer = layers.GetArrayElementAtIndex(VisionObstacleLayer);
            if (!string.IsNullOrEmpty(layer.stringValue) &&
                layer.stringValue != VisionObstacleLayerName)
            {
                throw new InvalidOperationException(
                    $"Layer {VisionObstacleLayer} is already used by '{layer.stringValue}'.");
            }

            layer.stringValue = VisionObstacleLayerName;
            tagManager.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void EnsureFolders()
        {
            EnsureFolder(Root);
            EnsureFolder(Materials);
            EnsureFolder(Prefabs);
            EnsureFolder(Scenes);
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
            {
                return;
            }

            int slash = path.LastIndexOf('/');
            if (slash <= 0)
            {
                return;
            }

            string parent = path.Substring(0, slash);
            string child = path.Substring(slash + 1);
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, child);
        }

        private static void AddStageScenesToBuildSettings()
        {
            List<EditorBuildSettingsScene> existingScenes =
                new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            List<EditorBuildSettingsScene> stageScenes =
                new List<EditorBuildSettingsScene>();

            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(
                    TutorialScenePath) != null)
            {
                stageScenes.Add(new EditorBuildSettingsScene(
                    TutorialScenePath,
                    true));
            }

            stageScenes.Add(new EditorBuildSettingsScene(Stage1ScenePath, true));
            stageScenes.Add(new EditorBuildSettingsScene(Stage2ScenePath, true));

            for (int i = 0; i < existingScenes.Count; i++)
            {
                string path = existingScenes[i].path;
                if (path == Stage1ScenePath ||
                    path == Stage2ScenePath ||
                    path == TutorialScenePath ||
                    path == Scenes + "/PrototypeRoom.unity")
                {
                    continue;
                }

                stageScenes.Add(existingScenes[i]);
            }

            EditorBuildSettings.scenes = stageScenes.ToArray();
        }

        private static void ValidateScene(
            Scene scene,
            int expectedDeadlineCharges)
        {
            int playerCount = CountComponentsInScene<PlayerHealth>(scene);
            int inputCount = CountComponentsInScene<PlayerInputReader>(scene);
            int movementCount = CountComponentsInScene<PlayerMovement>(scene);
            int deadlineCount = CountComponentsInScene<DeadlineController>(scene);
            int worldTimeCount = CountComponentsInScene<WorldTimeController>(scene);
            int enemyCount = CountComponentsInScene<EnemyHealth>(scene);
            int rangedEnemyCount =
                CountComponentsInScene<EnemyShooter>(scene);
            int chasingEnemyCount =
                CountComponentsInScene<EnemyChaser>(scene);
            int enemyMotorCount =
                CountComponentsInScene<EnemyMotor>(scene);
            int perceptionCount =
                CountComponentsInScene<EnemyPerception>(scene);
            int combatantCount =
                CountComponentsInScene<EnemyCombatant>(scene);
            int weaponControllerCount =
                CountComponentsInScene<WeaponController>(scene);
            int enemyWeaponDropCount =
                CountComponentsInScene<EnemyWeaponDrop>(scene);
            int navigationSurfaceCount =
                CountComponentsInScene<NavMeshSurface>(scene);
            int stageCount = CountComponentsInScene<StageController>(scene);
            int replayCount = CountComponentsInScene<StageReplayController>(scene);
            int pickupCount = CountComponentsInScene<WeaponPickup>(scene);
            int cameraCount = CountComponentsInScene<Camera>(scene);
            int cameraRigCount = CountComponentsInScene<TopDownCameraController>(scene);
            int rigidbody2DCount = CountComponentsInScene<Rigidbody2D>(scene);

            Camera camera = UnityEngine.Object.FindObjectOfType<Camera>();
            PlayerAim playerAim = UnityEngine.Object.FindObjectOfType<PlayerAim>();
            DeadlineController deadline =
                UnityEngine.Object.FindObjectOfType<DeadlineController>();
            StageReplayController replay =
                UnityEngine.Object.FindObjectOfType<StageReplayController>();
            NavMeshSurface navigationSurface =
                UnityEngine.Object.FindObjectOfType<NavMeshSurface>();
            WeaponDefinition pistolDefinition =
                AssetDatabase.LoadAssetAtPath<WeaponDefinition>(
                    PistolDefinitionPath);
            WeaponDefinition automaticRifleDefinition =
                AssetDatabase.LoadAssetAtPath<WeaponDefinition>(
                    AutomaticRifleDefinitionPath);
            WeaponDefinition shotgunDefinition =
                AssetDatabase.LoadAssetAtPath<WeaponDefinition>(
                    ShotgunDefinitionPath);
            WeaponPickup pistolPickup = GameObject.Find("Pistol Pickup")
                ?.GetComponent<WeaponPickup>();
            WeaponPickup shotgunPickup = GameObject.Find("Shotgun Pickup")
                ?.GetComponent<WeaponPickup>();
            bool hasConfiguredWeaponDefinitions =
                pistolDefinition != null &&
                pistolDefinition.FireMode == WeaponFireMode.SemiAutomatic &&
                pistolDefinition.ProjectileCount == 1 &&
                Mathf.Approximately(pistolDefinition.SpreadAngle, 0f) &&
                Mathf.Approximately(pistolDefinition.SpreadJitterAngle, 1.5f) &&
                pistolDefinition.SpreadSeed == 101 &&
                Mathf.Approximately(pistolDefinition.PlayerRecoilDistance, 0f) &&
                Mathf.Approximately(
                    pistolDefinition.MaximumProjectileDistance,
                    0f) &&
                automaticRifleDefinition != null &&
                automaticRifleDefinition.FireMode == WeaponFireMode.Automatic &&
                automaticRifleDefinition.ProjectileCount == 1 &&
                Mathf.Approximately(
                    automaticRifleDefinition.SpreadAngle,
                    0f) &&
                Mathf.Approximately(
                    automaticRifleDefinition.SpreadJitterAngle,
                    1.5f) &&
                automaticRifleDefinition.SpreadSeed == 211 &&
                Mathf.Approximately(
                    automaticRifleDefinition.PlayerRecoilDistance,
                    0f) &&
                Mathf.Approximately(
                    automaticRifleDefinition.MaximumProjectileDistance,
                    0f) &&
                shotgunDefinition != null &&
                shotgunDefinition.FireMode == WeaponFireMode.SemiAutomatic &&
                shotgunDefinition.AmmunitionCapacity == 6 &&
                Mathf.Approximately(shotgunDefinition.FireInterval, 0.75f) &&
                Mathf.Approximately(shotgunDefinition.ProjectileSpeed, 16f) &&
                shotgunDefinition.Damage == 1 &&
                shotgunDefinition.ProjectileCount == 8 &&
                Mathf.Approximately(shotgunDefinition.SpreadAngle, 18f) &&
                Mathf.Approximately(shotgunDefinition.SpreadJitterAngle, 1f) &&
                shotgunDefinition.SpreadSeed == 307 &&
                Mathf.Approximately(
                    shotgunDefinition.PlayerRecoilDistance,
                    0.35f) &&
                Mathf.Approximately(
                    shotgunDefinition.MaximumProjectileDistance,
                    14f);
            bool hasCircularShotgunSpread =
                HasCircularShotgunSpread(shotgunDefinition);
            bool hasConfiguredWeaponPickups =
                pistolPickup != null &&
                pistolPickup.Definition == pistolDefinition &&
                pistolPickup.Ammunition == 8 &&
                shotgunPickup != null &&
                shotgunPickup.Definition == shotgunDefinition &&
                shotgunPickup.Ammunition == 6;
            SerializedObject deadlineSerialized = deadline == null
                ? null
                : new SerializedObject(deadline);
            deadlineSerialized?.Update();
            SerializedProperty deadlineInput = deadlineSerialized == null
                ? null
                : deadlineSerialized.FindProperty("input");
            SerializedProperty deadlineCharges = deadlineSerialized == null
                ? null
                : deadlineSerialized.FindProperty("maximumCharges");
            SerializedProperty deadlineMovement = deadlineSerialized == null
                ? null
                : deadlineSerialized.FindProperty("movement");
            SerializedProperty deadlineCombat = deadlineSerialized == null
                ? null
                : deadlineSerialized.FindProperty("combat");
            SerializedProperty deadlineWorldTime = deadlineSerialized == null
                ? null
                : deadlineSerialized.FindProperty("worldTime");
            SerializedProperty deadlineRearm = deadlineSerialized == null
                ? null
                : deadlineSerialized.FindProperty("rearmWorldDuration");
            SerializedProperty deadlineStagedActions = deadlineSerialized == null
                ? null
                : deadlineSerialized.FindProperty("maximumStagedActions");
            SerializedProperty deadlineDangerRadius = deadlineSerialized == null
                ? null
                : deadlineSerialized.FindProperty("dangerRadius");
            SerializedProperty deadlineMaximumImpact = deadlineSerialized == null
                ? null
                : deadlineSerialized.FindProperty("maximumImpactWorldTime");
            SerializedProperty deadlineMovementThreshold = deadlineSerialized == null
                ? null
                : deadlineSerialized.FindProperty("movementThreshold");
            SerializedObject replaySerialized = replay == null
                ? null
                : new SerializedObject(replay);
            replaySerialized?.Update();
            SerializedProperty replayWorldTime = replaySerialized == null
                ? null
                : replaySerialized.FindProperty("worldTime");
            SerializedProperty replayCamera = replaySerialized == null
                ? null
                : replaySerialized.FindProperty("gameplayCamera");
            SerializedProperty replayDeadline = replaySerialized == null
                ? null
                : replaySerialized.FindProperty("deadline");
            SerializedProperty replayDeadlineRate = replaySerialized == null
                ? null
                : replaySerialized.FindProperty("deadlineCinematicPlaybackRate");
            SerializedProperty replayDeadlineMinimumDuration = replaySerialized == null
                ? null
                : replaySerialized.FindProperty("minimumDeadlineCinematicDuration");
            SerializedProperty replayDeadlineMaximumDuration = replaySerialized == null
                ? null
                : replaySerialized.FindProperty("maximumDeadlineCinematicDuration");
            SerializedProperty replayAftermathWorldDuration = replaySerialized == null
                ? null
                : replaySerialized.FindProperty("deadlineAftermathWorldDuration");
            SerializedProperty replayAftermathRate = replaySerialized == null
                ? null
                : replaySerialized.FindProperty("deadlineAftermathPlaybackRate");
            SerializedProperty replayCameraRecoveryDuration = replaySerialized == null
                ? null
                : replaySerialized.FindProperty("deadlineCameraRecoveryDuration");
            PlayerControls inputActions = new PlayerControls();
            InputAction deadlineAction = inputActions.Gameplay.Deadline;
            InputAction fireAction = inputActions.Gameplay.Fire;
            bool hasDeadlineKeyBinding = false;
            bool hasFireLeftMouseBinding = false;
            for (int i = 0; i < deadlineAction.bindings.Count; i++)
            {
                if (deadlineAction.bindings[i].path == "<Keyboard>/q")
                {
                    hasDeadlineKeyBinding = true;
                    break;
                }
            }
            for (int i = 0; i < fireAction.bindings.Count; i++)
            {
                if (fireAction.bindings[i].path == "<Mouse>/leftButton")
                {
                    hasFireLeftMouseBinding = true;
                    break;
                }
            }
            UnityEngine.Object.DestroyImmediate(inputActions.asset);
            if (playerCount != 1 ||
                inputCount != 1 ||
                movementCount != 1 ||
                deadlineCount != 1 ||
                worldTimeCount != 1 ||
                enemyCount != 3 ||
                rangedEnemyCount != 2 ||
                chasingEnemyCount != 1 ||
                enemyMotorCount != 3 ||
                perceptionCount != 3 ||
                combatantCount != 3 ||
                weaponControllerCount != 4 ||
                enemyWeaponDropCount != 3 ||
                navigationSurfaceCount != 1 ||
                stageCount != 1 ||
                replayCount != 1 ||
                pickupCount != 2 ||
                cameraCount != 1 ||
                cameraRigCount != 1 ||
                rigidbody2DCount != 0 ||
                navigationSurface == null ||
                navigationSurface.navMeshData == null ||
                camera == null ||
                camera.orthographic ||
                deadlineInput == null ||
                deadlineInput.objectReferenceValue == null ||
                deadlineCharges == null ||
                deadlineCharges.intValue != expectedDeadlineCharges ||
                deadlineMovement == null ||
                deadlineMovement.objectReferenceValue == null ||
                deadlineCombat == null ||
                deadlineCombat.objectReferenceValue == null ||
                deadlineWorldTime == null ||
                deadlineWorldTime.objectReferenceValue == null ||
                deadlineRearm == null ||
                !Mathf.Approximately(deadlineRearm.floatValue, 0.35f) ||
                deadlineStagedActions == null ||
                deadlineStagedActions.intValue != 2 ||
                deadlineDangerRadius != null ||
                deadlineMaximumImpact != null ||
                deadlineMovementThreshold != null ||
                replay == null ||
                replayWorldTime == null ||
                replayWorldTime.objectReferenceValue == null ||
                replayCamera == null ||
                replayCamera.objectReferenceValue == null ||
                replayDeadline == null ||
                replayDeadline.objectReferenceValue == null ||
                replayDeadlineRate == null ||
                !Mathf.Approximately(replayDeadlineRate.floatValue, 0.5f) ||
                replayDeadlineMinimumDuration == null ||
                !Mathf.Approximately(
                    replayDeadlineMinimumDuration.floatValue,
                    0.8f) ||
                replayDeadlineMaximumDuration == null ||
                !Mathf.Approximately(
                    replayDeadlineMaximumDuration.floatValue,
                    2f) ||
                replayAftermathWorldDuration == null ||
                !Mathf.Approximately(
                    replayAftermathWorldDuration.floatValue,
                    0.75f) ||
                replayAftermathRate == null ||
                !Mathf.Approximately(replayAftermathRate.floatValue, 0.5f) ||
                replayCameraRecoveryDuration == null ||
                !Mathf.Approximately(
                    replayCameraRecoveryDuration.floatValue,
                    0.2f) ||
                !hasDeadlineKeyBinding ||
                !hasFireLeftMouseBinding ||
                !hasConfiguredWeaponDefinitions ||
                !hasCircularShotgunSpread ||
                !hasConfiguredWeaponPickups)
            {
                throw new InvalidOperationException(
                    "3D stage validation failed: " +
                    $"players={playerCount}, inputs={inputCount}, movements={movementCount}, " +
                    $"deadlines={deadlineCount}, worldTimes={worldTimeCount}, " +
                    $"deadlineInput={deadlineInput?.objectReferenceValue != null}, " +
                    $"deadlineCharges={deadlineCharges?.intValue}, " +
                    $"deadlineMovement={deadlineMovement?.objectReferenceValue != null}, " +
                    $"deadlineCombat={deadlineCombat?.objectReferenceValue != null}, " +
                    $"deadlineWorldTime={deadlineWorldTime?.objectReferenceValue != null}, " +
                    $"deadlineRearm={deadlineRearm?.floatValue}, " +
                    $"deadlineStagedActions={deadlineStagedActions?.intValue}, " +
                    $"legacyDeadlineTriggerFields={deadlineDangerRadius != null || deadlineMaximumImpact != null || deadlineMovementThreshold != null}, " +
                    $"replayWorldTime={replayWorldTime?.objectReferenceValue != null}, " +
                    $"replayCamera={replayCamera?.objectReferenceValue != null}, " +
                    $"replayDeadline={replayDeadline?.objectReferenceValue != null}, " +
                    $"replayDeadlineRate={replayDeadlineRate?.floatValue}, " +
                    $"replayDeadlineMin={replayDeadlineMinimumDuration?.floatValue}, " +
                    $"replayDeadlineMax={replayDeadlineMaximumDuration?.floatValue}, " +
                    $"replayAftermathWorld={replayAftermathWorldDuration?.floatValue}, " +
                    $"replayAftermathRate={replayAftermathRate?.floatValue}, " +
                    $"replayCameraRecovery={replayCameraRecoveryDuration?.floatValue}, " +
                    $"deadlineQBinding={hasDeadlineKeyBinding}, " +
                    $"fireLmbBinding={hasFireLeftMouseBinding}, " +
                    $"weaponDefinitions={hasConfiguredWeaponDefinitions}, " +
                    $"circularShotgunSpread={hasCircularShotgunSpread}, " +
                    $"weaponPickups={hasConfiguredWeaponPickups}, " +
                    $"expectedDeadlineCharges={expectedDeadlineCharges}, enemies={enemyCount}, " +
                    $"ranged={rangedEnemyCount}, chasers={chasingEnemyCount}, motors={enemyMotorCount}, " +
                    $"perception={perceptionCount}, combatants={combatantCount}, " +
                    $"weapons={weaponControllerCount}, enemyDrops={enemyWeaponDropCount}, " +
                    $"navSurfaces={navigationSurfaceCount}, " +
                    $"stages={stageCount}, replays={replayCount}, pickups={pickupCount}, cameras={cameraCount}, " +
                    $"cameraRigs={cameraRigCount}, rigidbodies2D={rigidbody2DCount}, " +
                    $"navData={navigationSurface != null && navigationSurface.navMeshData != null}, " +
                    $"perspective={camera != null && !camera.orthographic}.");
            }

            ValidatePlayerControls();
        }

        private static bool HasCircularShotgunSpread(
            WeaponDefinition shotgunDefinition)
        {
            if (shotgunDefinition == null ||
                shotgunDefinition.ProjectileCount < 2 ||
                shotgunDefinition.SpreadAngle <= 0f)
            {
                return false;
            }

            Vector3 forward = Vector3.forward;
            float maximumAngle = (shotgunDefinition.SpreadAngle * 0.5f) +
                0.01f;
            bool hasPositiveHorizontal = false;
            bool hasNegativeHorizontal = false;
            bool hasPositiveVertical = false;
            bool hasNegativeVertical = false;

            for (int pelletIndex = 0;
                 pelletIndex < shotgunDefinition.ProjectileCount;
                 pelletIndex++)
            {
                Vector3 direction = WeaponSpreadPattern.GetProjectileDirection(
                    forward,
                    pelletIndex,
                    shotgunDefinition.ProjectileCount,
                    shotgunDefinition.SpreadAngle,
                    shotgunDefinition.SpreadJitterAngle,
                    shotgunDefinition.SpreadSeed,
                    0);
                Vector3 repeatedDirection =
                    WeaponSpreadPattern.GetProjectileDirection(
                        forward,
                        pelletIndex,
                        shotgunDefinition.ProjectileCount,
                        shotgunDefinition.SpreadAngle,
                        shotgunDefinition.SpreadJitterAngle,
                        shotgunDefinition.SpreadSeed,
                        0);
                if (Vector3.Angle(forward, direction) > maximumAngle ||
                    (direction - repeatedDirection).sqrMagnitude > 0.00000001f)
                {
                    return false;
                }

                hasPositiveHorizontal |= direction.x > 0.01f;
                hasNegativeHorizontal |= direction.x < -0.01f;
                hasPositiveVertical |= direction.y > 0.01f;
                hasNegativeVertical |= direction.y < -0.01f;
            }

            return hasPositiveHorizontal &&
                   hasNegativeHorizontal &&
                   hasPositiveVertical &&
                   hasNegativeVertical;
        }

        private static void ValidatePlayerControls()
        {
            InputActionAsset controls =
                AssetDatabase.LoadAssetAtPath<InputActionAsset>(PlayerControlsPath);
            if (controls == null)
            {
                throw new InvalidOperationException(
                    $"Player input actions are missing at {PlayerControlsPath}.");
            }

            InputActionMap gameplay = controls.FindActionMap("Gameplay", false);
            if (gameplay == null)
            {
                throw new InvalidOperationException(
                    "Player input actions require a Gameplay action map.");
            }

            RequireBindings(
                gameplay,
                "Move",
                "<Keyboard>/w",
                "<Keyboard>/a",
                "<Keyboard>/s",
                "<Keyboard>/d");
            RequireBindings(gameplay, "Point", "<Mouse>/position");
            RequireBindings(gameplay, "Fire", "<Mouse>/leftButton");
            RequireBindings(gameplay, "Throw", "<Mouse>/rightButton");
            RequireBindings(gameplay, "Dash", "<Keyboard>/space");
            RequireBindings(gameplay, "Interact", "<Keyboard>/e");
            RequireBindings(gameplay, "Restart", "<Keyboard>/r");
        }

        private static void RequireBindings(
            InputActionMap actionMap,
            string actionName,
            params string[] requiredPaths)
        {
            InputAction action = actionMap.FindAction(actionName, false);
            if (action == null)
            {
                throw new InvalidOperationException(
                    $"Gameplay input action '{actionName}' is missing.");
            }

            for (int pathIndex = 0; pathIndex < requiredPaths.Length; pathIndex++)
            {
                string requiredPath = requiredPaths[pathIndex];
                bool found = false;

                for (int bindingIndex = 0; bindingIndex < action.bindings.Count; bindingIndex++)
                {
                    if (string.Equals(
                        action.bindings[bindingIndex].path,
                        requiredPath,
                        StringComparison.OrdinalIgnoreCase))
                    {
                        found = true;
                        break;
                    }
                }

                if (!found)
                {
                    throw new InvalidOperationException(
                        $"Gameplay input action '{actionName}' requires binding '{requiredPath}'.");
                }
            }
        }

        private static int CountComponentsInScene<T>(Scene scene) where T : Component
        {
            int count = 0;
            GameObject[] roots = scene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                count += roots[i].GetComponentsInChildren<T>(true).Length;
            }

            return count;
        }

        private readonly struct PlayerBundle
        {
            public PlayerBundle(
                GameObject root,
                PlayerInputReader input,
                PlayerAim aim,
                PlayerDash dash,
                PlayerHealth health,
                PlayerCombat combat,
                DeadlineController deadline,
                WeaponController weapon,
                VisionCone vision)
            {
                Root = root;
                Input = input;
                Aim = aim;
                Dash = dash;
                Health = health;
                Combat = combat;
                Deadline = deadline;
                Weapon = weapon;
                Vision = vision;
            }

            public GameObject Root { get; }
            public PlayerInputReader Input { get; }
            public PlayerAim Aim { get; }
            public PlayerDash Dash { get; }
            public PlayerHealth Health { get; }
            public PlayerCombat Combat { get; }
            public DeadlineController Deadline { get; }
            public WeaponController Weapon { get; }
            public VisionCone Vision { get; }
        }
    }
}
