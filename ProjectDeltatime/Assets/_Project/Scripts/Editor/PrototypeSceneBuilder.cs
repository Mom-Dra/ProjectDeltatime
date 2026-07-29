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
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
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
        private const string PrototypeScenePath = Scenes + "/PrototypeRoom.unity";
        private const string PistolDefinitionPath = Root + "/Pistol.asset";
        private const string LineMaterialPath = Materials + "/PrototypeLine.mat";
        private const string FloorMaterialPath = Materials + "/PrototypeFloor3D.mat";
        private const string WallMaterialPath = Materials + "/PrototypeWall3D.mat";
        private const string CoverMaterialPath = Materials + "/PrototypeCover3D.mat";
        private const string PlayerMaterialPath = Materials + "/PrototypePlayer3D.mat";
        private const string EnemyMaterialPath = Materials + "/PrototypeEnemy3D.mat";
        private const string WeaponMaterialPath = Materials + "/PrototypeWeapon3D.mat";
        private const string PickupMaterialPath = Materials + "/PrototypePickup3D.mat";
        private const string AccentMaterialPath = Materials + "/PrototypeAccent3D.mat";
        private const string VisionMaterialPath = Materials + "/PrototypeVisionCone3D.mat";
        private const string ProjectilePrefabPath = Prefabs + "/Projectile.prefab";
        private const string PickupPrefabPath = Prefabs + "/WeaponPickup.prefab";
        private const string ThrownWeaponPrefabPath = Prefabs + "/ThrownWeapon.prefab";
        private const string VisionObstacleLayerName = "VisionObstacle";
        private const int VisionObstacleLayer = 8;

        [MenuItem("Tools/Prototype/Build 3D Prototype Room")]
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
            EnsureProjectilePrefab(lineMaterial);
            EnsurePickupPrefab(pickupMaterial);
            EnsureThrownWeaponPrefab(pickupMaterial, lineMaterial);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

            Scene scene = EditorSceneManager.NewScene(
                NewSceneSetup.EmptyScene,
                NewSceneMode.Single);

            Projectile projectilePrefab =
                LoadPrefabComponent<Projectile>(ProjectilePrefabPath);
            WeaponPickup pickupPrefab =
                LoadPrefabComponent<WeaponPickup>(PickupPrefabPath);
            ThrownWeapon thrownPrefab =
                LoadPrefabComponent<ThrownWeapon>(ThrownWeaponPrefabPath);

            WorldTimeActivity activity;
            WorldTimeController worldTime;
            GameObject systems = CreateSystems(out activity, out worldTime);
            Camera gameplayCamera = CreateCamera(worldTime);
            StageReplayController replay =
                systems.AddComponent<StageReplayController>();
            replay.Configure(worldTime, gameplayCamera);

            CreateLightingAndAtmosphere();
            CreateFloorAndWalls(
                floorMaterial,
                wallMaterial,
                coverMaterial,
                accentMaterial);

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
                replay);

            TopDownCameraController cameraController =
                gameplayCamera.gameObject.AddComponent<TopDownCameraController>();
            cameraController.Configure(player.Root.transform, player.Aim, player.Input);
            cameraController.SnapToTarget();

            StageController stage = systems.AddComponent<StageController>();
            stage.Configure(player.Input, player.Health, player.Combat, replay);

            CreatePickup(
                new Vector3(-2.4f, 0.18f, -4.2f),
                pickupPrefab,
                pistol,
                8);

            CreateEnemy(
                "Enemy West",
                new Vector3(-6.3f, 0.75f, 4.7f),
                enemyMaterial,
                weaponMaterial,
                lineMaterial,
                pistol,
                projectilePrefab,
                pickupPrefab,
                thrownPrefab,
                worldTime,
                player.Root.transform,
                player.Health,
                player.Vision,
                stage);
            CreateEnemy(
                "Enemy Center",
                new Vector3(0f, 0.75f, 6.2f),
                enemyMaterial,
                weaponMaterial,
                lineMaterial,
                pistol,
                projectilePrefab,
                pickupPrefab,
                thrownPrefab,
                worldTime,
                player.Root.transform,
                player.Health,
                player.Vision,
                stage);
            CreateEnemy(
                "Enemy East",
                new Vector3(6.3f, 0.75f, 4.7f),
                enemyMaterial,
                weaponMaterial,
                lineMaterial,
                pistol,
                projectilePrefab,
                pickupPrefab,
                thrownPrefab,
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
                player.Weapon,
                replay);

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, PrototypeScenePath))
            {
                throw new InvalidOperationException($"Failed to save {PrototypeScenePath}.");
            }

            AddSceneToBuildSettings();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Selection.activeGameObject = player.Root;

            ValidateScene(scene);
            Debug.Log(
                "3D PrototypeRoom built successfully. Open Assets/_Project/Scenes/PrototypeRoom.unity and press Play.");
        }

        public static void BuildAndValidateFromCommandLine()
        {
            BuildPrototypeRoom();
        }

        public static void CapturePreviewFromCommandLine()
        {
            Scene scene = EditorSceneManager.OpenScene(
                PrototypeScenePath,
                OpenSceneMode.Single);
            ValidateScene(scene);

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
                    "PrototypeRoom3DPreview.png");
                System.IO.Directory.CreateDirectory(
                    System.IO.Path.GetDirectoryName(previewPath));
                System.IO.File.WriteAllBytes(previewPath, preview.EncodeToPNG());
                AssetDatabase.ImportAsset(
                    "Assets/_Project/Art/Generated/PrototypeRoom3DPreview.png",
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

        [MenuItem("Tools/Prototype/Validate 3D Prototype Room")]
        public static void ValidateSavedPrototypeRoom()
        {
            Scene scene = EditorSceneManager.OpenScene(
                PrototypeScenePath,
                OpenSceneMode.Single);
            ValidateScene(scene);
            Debug.Log("3D PrototypeRoom validation passed.");
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

        private static Camera CreateCamera(WorldTimeController worldTime)
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
            feedback.Configure(worldTime, camera);
            return camera;
        }

        private static void CreateLightingAndAtmosphere()
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
            StageReplayController replay)
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
            dash.Configure(input, health, activity);

            PlayerMovement movement = root.AddComponent<PlayerMovement>();
            movement.Configure(input, health, dash);

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
            combat.Configure(input, aim, health, weapon, worldTime, activity);

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
                weapon,
                visionCone);
        }

        private static void CreateEnemy(
            string name,
            Vector3 position,
            Material enemyMaterial,
            Material weaponMaterial,
            Material lineMaterial,
            WeaponDefinition pistol,
            Projectile projectilePrefab,
            WeaponPickup pickupPrefab,
            ThrownWeapon thrownPrefab,
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
                pistol);

            LineRenderer warningLine = root.AddComponent<LineRenderer>();
            ConfigureLine(
                warningLine,
                lineMaterial,
                new Color(1f, 0.08f, 0.04f, 0.85f),
                0.035f);
            warningLine.enabled = false;

            EnemyWeaponDrop drop = root.AddComponent<EnemyWeaponDrop>();
            drop.Configure(pickupPrefab, pistol, 4);

            EnemyShooter shooter = root.AddComponent<EnemyShooter>();
            Renderer bodyRenderer = root.GetComponent<Renderer>();
            shooter.Configure(
                worldTime,
                player,
                playerHealth,
                weapon,
                warningLine,
                playerVision,
                bodyRenderer,
                heldWeaponRenderer);

            EnemyHealth health = root.AddComponent<EnemyHealth>();
            health.Configure(
                shooter,
                drop,
                stage,
                collider,
                bodyRenderer);
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
            WeaponDefinition definition,
            int ammunition)
        {
            WeaponPickup pickup = UnityEngine.Object.Instantiate(
                pickupPrefab,
                position,
                Quaternion.Euler(0f, 28f, 0f));
            pickup.name = "Pistol Pickup";
            pickup.Initialize(definition, ammunition);
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
            root.AddComponent<ThrownWeapon>();

            PrefabUtility.SaveAsPrefabAsset(root, ThrownWeaponPrefabPath);
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

            definition.ConfigurePrototype("Pistol", 8, 0.24f, 17f, 1, 0.08f);
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

        private static void AddSceneToBuildSettings()
        {
            List<EditorBuildSettingsScene> scenes =
                new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            bool found = false;

            for (int i = 0; i < scenes.Count; i++)
            {
                if (scenes[i].path == PrototypeScenePath)
                {
                    scenes[i] = new EditorBuildSettingsScene(PrototypeScenePath, true);
                    found = true;
                    break;
                }
            }

            if (!found)
            {
                scenes.Add(new EditorBuildSettingsScene(PrototypeScenePath, true));
            }

            EditorBuildSettings.scenes = scenes.ToArray();
        }

        private static void ValidateScene(Scene scene)
        {
            int playerCount = CountComponentsInScene<PlayerHealth>(scene);
            int inputCount = CountComponentsInScene<PlayerInputReader>(scene);
            int enemyCount = CountComponentsInScene<EnemyHealth>(scene);
            int stageCount = CountComponentsInScene<StageController>(scene);
            int replayCount = CountComponentsInScene<StageReplayController>(scene);
            int pickupCount = CountComponentsInScene<WeaponPickup>(scene);
            int cameraCount = CountComponentsInScene<Camera>(scene);
            int cameraRigCount = CountComponentsInScene<TopDownCameraController>(scene);
            int rigidbody2DCount = CountComponentsInScene<Rigidbody2D>(scene);

            Camera camera = UnityEngine.Object.FindObjectOfType<Camera>();
            if (playerCount != 1 ||
                inputCount != 1 ||
                enemyCount != 3 ||
                stageCount != 1 ||
                replayCount != 1 ||
                pickupCount < 1 ||
                cameraCount != 1 ||
                cameraRigCount != 1 ||
                rigidbody2DCount != 0 ||
                camera == null ||
                camera.orthographic)
            {
                throw new InvalidOperationException(
                    "3D PrototypeRoom validation failed: " +
                    $"players={playerCount}, inputs={inputCount}, enemies={enemyCount}, " +
                    $"stages={stageCount}, replays={replayCount}, pickups={pickupCount}, cameras={cameraCount}, " +
                    $"cameraRigs={cameraRigCount}, rigidbodies2D={rigidbody2DCount}, " +
                    $"perspective={camera != null && !camera.orthographic}.");
            }

            ValidatePlayerControls();
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
                WeaponController weapon,
                VisionCone vision)
            {
                Root = root;
                Input = input;
                Aim = aim;
                Dash = dash;
                Health = health;
                Combat = combat;
                Weapon = weapon;
                Vision = vision;
            }

            public GameObject Root { get; }
            public PlayerInputReader Input { get; }
            public PlayerAim Aim { get; }
            public PlayerDash Dash { get; }
            public PlayerHealth Health { get; }
            public PlayerCombat Combat { get; }
            public WeaponController Weapon { get; }
            public VisionCone Vision { get; }
        }
    }
}
