using System;
using System.Collections.Generic;
using System.IO;
using Deltatime.Combat;
using Deltatime.Enemies;
using Deltatime.InputSystem;
using Deltatime.Level;
using Deltatime.Player;
using Deltatime.Replay;
using Deltatime.TimeSystem;
using Deltatime.Tutorial;
using Deltatime.UI;
using Deltatime.Vision;
using Deltatime.Visuals;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace Deltatime.EditorTools
{
    public static class TutorialSceneBuilder
    {
        private const string Root = "Assets/_Project";
        private const string Scenes = Root + "/Scenes";
        private const string TutorialScenePath = Scenes + "/Tutorial.unity";
        private const string Stage1ScenePath = Scenes + "/Stage1.unity";
        private const string NavigationDataPath =
            Scenes + "/TutorialNavigation.asset";
        private const string PistolDefinitionPath = Root + "/Pistol.asset";
        private const string MeleeDefinitionPath = Root + "/MeleeWeapon.asset";
        private const string PickupPrefabPath = Root + "/Prefabs/WeaponPickup.prefab";
        private const string FloorMaterialPath =
            Root + "/Materials/PrototypeFloor3D.mat";
        private const string WallMaterialPath =
            Root + "/Materials/PrototypeWall3D.mat";
        private const string CoverMaterialPath =
            Root + "/Materials/PrototypeCover3D.mat";
        private const string AccentMaterialPath =
            Root + "/Materials/PrototypeAccent3D.mat";
        private const string EnemyMaterialPath =
            Root + "/Materials/PrototypeEnemy3D.mat";
        private const string SyntyBuildingsRoot =
            "Assets/Synty/PolygonNightclubs/Prefabs/Buildings";
        private const string SyntyPropsRoot =
            "Assets/Synty/PolygonNightclubs/Prefabs/Props";
        private const string SyntyModularPropsRoot =
            SyntyPropsRoot + "/Modular";
        private const string SyntyFloorPath =
            SyntyBuildingsRoot + "/SM_Bld_Floor_Combined_01.prefab";
        private const string SyntyWallFeaturePath =
            SyntyBuildingsRoot + "/SM_Bld_Wall_Feature_01.prefab";
        private const string SyntyWallFeatureAlternatePath =
            SyntyBuildingsRoot + "/SM_Bld_Wall_Feature_03.prefab";
        private const string SyntyPillarPath =
            SyntyBuildingsRoot + "/SM_Bld_Pillar_01.prefab";
        private const string SyntyBarPath =
            SyntyModularPropsRoot + "/SM_Prop_Bar_02.prefab";
        private const string SyntyBenchPath =
            SyntyModularPropsRoot + "/SM_Prop_Bench_01_Straight_01.prefab";
        private const string SyntyBoxStackAPath =
            SyntyPropsRoot + "/SM_Prop_Box_Stack_01.prefab";
        private const string SyntyBoxStackBPath =
            SyntyPropsRoot + "/SM_Prop_Box_Stack_04.prefab";
        private const string SyntyFloorLightPath =
            SyntyPropsRoot + "/SM_Prop_Floor_Light_02.prefab";
        private const string SyntyExitSignPath =
            SyntyPropsRoot + "/SM_Prop_Exit_Sign_01.prefab";
        private const string SyntyDjBoothPath =
            SyntyPropsRoot + "/SM_Prop_DJ_Booth_01.prefab";
        private const string SyntyFridgePath =
            SyntyPropsRoot + "/SM_Prop_Drinks_Fridge_01.prefab";
        private const string SyntyDumpsterPath =
            SyntyPropsRoot + "/SM_Prop_Dumpster_01.prefab";
        private const string SyntyVisualRootName = "Synty Tutorial Set";
        private const int VisionObstacleLayer = 8;
        private const int ExpectedEnemyCount = 5;
        private const int ExpectedAnimatedActorCount = 6;
        private const int MinimumSyntyPrefabCount = 120;

        private static readonly string[] OrderedBuildScenes =
        {
            TutorialScenePath,
            Scenes + "/Stage1.unity",
            Scenes + "/Stage2.unity",
            Scenes + "/Stage3.unity",
            Scenes + "/Stage4.unity",
            Scenes + "/Stage5.unity",
            Scenes + "/Stage6.unity"
        };

        [MenuItem("Tools/Prototype/Build Tutorial")]
        public static void BuildTutorial()
        {
            Require(
                AssetDatabase.LoadAssetAtPath<SceneAsset>(Stage1ScenePath) != null,
                "Stage1 scene is missing; Tutorial requires its serialized gameplay base.");

            WeaponDefinition pistol = LoadAsset<WeaponDefinition>(
                PistolDefinitionPath);
            WeaponDefinition melee = LoadAsset<WeaponDefinition>(
                MeleeDefinitionPath);
            WeaponPickup pickupPrefab = LoadAsset<GameObject>(
                    PickupPrefabPath)
                .GetComponent<WeaponPickup>();
            Material floorMaterial = LoadAsset<Material>(FloorMaterialPath);
            Material wallMaterial = LoadAsset<Material>(WallMaterialPath);
            Material coverMaterial = LoadAsset<Material>(CoverMaterialPath);
            Material accentMaterial = LoadAsset<Material>(AccentMaterialPath);
            Material enemyMaterial = LoadAsset<Material>(EnemyMaterialPath);

            Scene scene = EditorSceneManager.OpenScene(
                Stage1ScenePath,
                OpenSceneMode.Single);
            if (!EditorSceneManager.SaveScene(scene, TutorialScenePath))
            {
                throw new InvalidOperationException(
                    $"Failed to create {TutorialScenePath} from Stage1.");
            }

            RemovePrototypeContent(scene);

            WorldTimeController worldTime = FindSceneComponent<WorldTimeController>(scene);
            PlayerInputReader input = FindSceneComponent<PlayerInputReader>(scene);
            PlayerMovement movement = FindSceneComponent<PlayerMovement>(scene);
            PlayerAim aim = FindSceneComponent<PlayerAim>(scene);
            PlayerDash dash = FindSceneComponent<PlayerDash>(scene);
            PlayerCombat combat = FindSceneComponent<PlayerCombat>(scene);
            PlayerHealth playerHealth = FindSceneComponent<PlayerHealth>(scene);
            DeadlineController deadline = FindSceneComponent<DeadlineController>(scene);
            WeaponController playerWeapon = playerHealth.GetComponent<WeaponController>();
            Rigidbody playerBody = playerHealth.GetComponent<Rigidbody>();
            VisionCone playerVision = FindSceneComponent<VisionCone>(scene);
            NavMeshSurface navigation = FindSceneComponent<NavMeshSurface>(scene);

            Require(worldTime != null && input != null && movement != null &&
                    aim != null && dash != null && combat != null &&
                    playerHealth != null && deadline != null &&
                    playerWeapon != null && playerBody != null &&
                    playerVision != null &&
                    navigation != null,
                "Tutorial gameplay base is missing required Stage1 references.");

            playerHealth.transform.SetPositionAndRotation(
                new Vector3(0f, 0.75f, -34f),
                Quaternion.identity);
            SetObjectReference(playerWeapon, "startingDefinition", null);
            deadline.SetMaximumCharges(2);
            playerVision.SetUnlimitedVision(true);

            GameHud gameHud = FindSceneComponent<GameHud>(scene);
            if (gameHud != null)
            {
                UnityEngine.Object.DestroyImmediate(gameHud);
            }

            StageController stageController =
                FindSceneComponent<StageController>(scene);
            if (stageController != null)
            {
                UnityEngine.Object.DestroyImmediate(stageController);
            }

            ConfigureLighting(scene);
            GameObject environment = CreateEnvironment(
                floorMaterial,
                wallMaterial,
                coverMaterial,
                accentMaterial);
            environment.AddComponent<ReplayExcluded>();

            TutorialGate timeGate = CreateGate(
                environment.transform,
                "Gate 1 - Time",
                new Vector3(0f, 1.4f, -25f),
                accentMaterial);
            TutorialGate dashGate = CreateGate(
                environment.transform,
                "Gate 2 - Dash",
                new Vector3(0f, 1.4f, -13f),
                accentMaterial);
            TutorialGate meleeGate = CreateGate(
                environment.transform,
                "Gate 3 - Melee",
                new Vector3(0f, 1.4f, -1f),
                accentMaterial);
            TutorialGate pistolGate = CreateGate(
                environment.transform,
                "Gate 4 - Pistol",
                new Vector3(0f, 1.4f, 13f),
                accentMaterial);
            TutorialGate arenaEntranceGate = CreateGate(
                environment.transform,
                "Gate 5 - Arena Entrance",
                new Vector3(0f, 1.4f, 34f),
                accentMaterial);
            TutorialGate arenaExitGate = CreateGate(
                environment.transform,
                "Gate 6 - Arena Exit",
                new Vector3(0f, 1.4f, 57f),
                accentMaterial);

            TutorialTimeProbe timeProbe = CreateTimeProbe(
                environment.transform,
                worldTime,
                accentMaterial);
            TutorialTargetDummy meleeTarget = CreateTargetDummy(
                environment.transform,
                "Melee Training Target",
                new Vector3(0f, 0.8f, -5.2f),
                TutorialTargetDummy.AcceptedAttack.Melee,
                enemyMaterial);
            TutorialTargetDummy pistolTarget = CreateTargetDummy(
                environment.transform,
                "Pistol Training Target",
                new Vector3(0f, 0.8f, 9f),
                TutorialTargetDummy.AcceptedAttack.Firearm,
                enemyMaterial);

            TutorialWeaponDispenser meleeDispenser = CreateDispenser(
                "Melee Weapon Dispenser",
                new Vector3(0f, 0.18f, -10f),
                playerWeapon,
                pickupPrefab,
                melee,
                0);
            TutorialWeaponDispenser pistolDispenser = CreateDispenser(
                "Pistol Dispenser",
                new Vector3(0f, 0.18f, 3f),
                playerWeapon,
                pickupPrefab,
                pistol,
                pistol.AmmunitionCapacity);
            TutorialWeaponDispenser deadlinePistolDispenser = CreateDispenser(
                "Deadline Pistol Dispenser",
                new Vector3(0f, 0.18f, 39.5f),
                playerWeapon,
                pickupPrefab,
                pistol,
                pistol.AmmunitionCapacity);

            ConfigureEnemies(
                scene,
                out EnemyHealth throwEnemyHealth,
                out EnemyCombatant throwEnemyBehavior,
                out WeaponController throwEnemyWeapon,
                out EnemyWeaponDrop throwEnemyDrop,
                out EnemyCombatant[] deadlineEnemies);
            ConfigureTutorialCharacterAnimations(scene);

            GameObject systems = FindSceneRoot(scene, "Systems");
            Require(systems != null, "Tutorial Systems root is missing.");
            TutorialDirector director = systems.AddComponent<TutorialDirector>();

            Transform resetPoint = CreateMarker(
                environment.transform,
                "Deadline Reset Point",
                new Vector3(0f, 0.75f, 47f));
            director.Configure(
                input,
                movement,
                aim,
                dash,
                combat,
                playerWeapon,
                deadline,
                worldTime,
                playerBody,
                playerHealth,
                timeGate,
                dashGate,
                meleeGate,
                pistolGate,
                arenaEntranceGate,
                arenaExitGate,
                resetPoint,
                meleeTarget,
                pistolTarget,
                meleeDispenser,
                pistolDispenser,
                deadlinePistolDispenser,
                pistol,
                throwEnemyHealth,
                throwEnemyBehavior,
                throwEnemyWeapon,
                throwEnemyDrop,
                deadlineEnemies);

            CreateTrigger(
                "Dash Exit Trigger",
                new Vector3(0f, 0.8f, -16.5f),
                new Vector3(10f, 1.6f, 1.2f),
                director,
                TutorialTrigger.TriggerKind.DashExit);
            CreateTrigger(
                "Deadline Entry Trigger",
                new Vector3(0f, 0.8f, 47f),
                new Vector3(5f, 1.6f, 2f),
                director,
                TutorialTrigger.TriggerKind.DeadlineEntry);
            CreateTrigger(
                "Tutorial Exit Trigger",
                new Vector3(0f, 0.8f, 60.5f),
                new Vector3(10f, 1.6f, 2f),
                director,
                TutorialTrigger.TriggerKind.TutorialExit);

            GameObject tutorialHudObject = new GameObject("Tutorial HUD");
            TutorialHud tutorialHud = tutorialHudObject.AddComponent<TutorialHud>();
            tutorialHud.Configure(director, worldTime, playerWeapon, deadline);

            BuildTutorialNavigation(navigation, scene);
            ConfigureBuildSettings();

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, TutorialScenePath))
            {
                throw new InvalidOperationException(
                    $"Failed to save completed {TutorialScenePath}.");
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            ValidateTutorialScene(scene);
            Debug.Log(
                "Tutorial built and validated successfully: movement/time, aim/dash, " +
                "melee, pistol, throw/disarm, and four-enemy DEADLINE escape.");
        }

        public static void BuildAndValidateFromCommandLine()
        {
            BuildTutorial();
        }

        [MenuItem("Tools/Prototype/Validate Tutorial")]
        public static void ValidateSavedTutorial()
        {
            Scene scene = EditorSceneManager.OpenScene(
                TutorialScenePath,
                OpenSceneMode.Single);
            ValidateTutorialScene(scene);
            Debug.Log("Tutorial static validation passed.");
        }

        public static void ValidateFromCommandLine()
        {
            ValidateSavedTutorial();
        }

        public static void CapturePreviewFromCommandLine()
        {
            Scene scene = EditorSceneManager.OpenScene(
                TutorialScenePath,
                OpenSceneMode.Single);
            ValidateTutorialScene(scene);
            Camera camera = FindSceneComponent<Camera>(scene);
            Require(camera != null, "Tutorial preview requires its gameplay camera.");

            string firstPath = Path.Combine(
                Path.GetTempPath(),
                "ProjectDeltatime-Tutorial-South.png");
            string secondPath = Path.Combine(
                Path.GetTempPath(),
                "ProjectDeltatime-Tutorial-North.png");
            CapturePreviewSegment(camera, -25f, firstPath);
            CapturePreviewSegment(camera, 47f, secondPath);
            Debug.Log(
                $"Tutorial preview captured: {firstPath} and {secondPath}");
        }

        private static void CapturePreviewSegment(
            Camera camera,
            float centerZ,
            string outputPath)
        {
            const int size = 768;
            RenderTexture texture = new RenderTexture(
                size,
                size,
                24,
                RenderTextureFormat.ARGB32);
            Texture2D image = new Texture2D(
                size,
                size,
                TextureFormat.RGB24,
                false);
            RenderTexture previousActive = RenderTexture.active;
            RenderTexture previousTarget = camera.targetTexture;
            Vector3 previousPosition = camera.transform.position;
            Quaternion previousRotation = camera.transform.rotation;
            bool previousOrthographic = camera.orthographic;
            float previousOrthographicSize = camera.orthographicSize;
            float previousFieldOfView = camera.fieldOfView;
            float previousFarClip = camera.farClipPlane;
            try
            {
                camera.transform.SetPositionAndRotation(
                    new Vector3(0f, 13.5f, centerZ - 12.5f),
                    Quaternion.Euler(46f, 0f, 0f));
                camera.orthographic = false;
                camera.fieldOfView = 49f;
                camera.farClipPlane = 100f;
                camera.targetTexture = texture;
                camera.Render();
                RenderTexture.active = texture;
                image.ReadPixels(new Rect(0f, 0f, size, size), 0, 0);
                image.Apply();
                File.WriteAllBytes(outputPath, image.EncodeToPNG());
            }
            finally
            {
                RenderTexture.active = previousActive;
                camera.targetTexture = previousTarget;
                camera.transform.SetPositionAndRotation(
                    previousPosition,
                    previousRotation);
                camera.orthographic = previousOrthographic;
                camera.orthographicSize = previousOrthographicSize;
                camera.fieldOfView = previousFieldOfView;
                camera.farClipPlane = previousFarClip;
                UnityEngine.Object.DestroyImmediate(texture);
                UnityEngine.Object.DestroyImmediate(image);
            }
        }

        private static void RemovePrototypeContent(Scene scene)
        {
            GameObject industrialRoom = FindSceneRoot(scene, "Industrial Room");
            if (industrialRoom != null)
            {
                UnityEngine.Object.DestroyImmediate(industrialRoom);
            }

            WeaponPickup[] pickups = FindSceneComponents<WeaponPickup>(scene);
            for (int i = 0; i < pickups.Length; i++)
            {
                if (pickups[i] != null)
                {
                    UnityEngine.Object.DestroyImmediate(pickups[i].gameObject);
                }
            }

            string[] obsoleteLights = { "Blue Bay Light", "Red Alert Light" };
            for (int i = 0; i < obsoleteLights.Length; i++)
            {
                GameObject light = FindSceneRoot(scene, obsoleteLights[i]);
                if (light != null)
                {
                    UnityEngine.Object.DestroyImmediate(light);
                }
            }
        }

        private static void ConfigureLighting(Scene scene)
        {
            RenderSettings.fog = false;
            RenderSettings.ambientIntensity = 0.72f;
            RenderSettings.ambientSkyColor = new Color(0.18f, 0.23f, 0.3f, 1f);
            RenderSettings.ambientEquatorColor = new Color(0.1f, 0.13f, 0.18f, 1f);
            RenderSettings.ambientGroundColor = new Color(0.04f, 0.05f, 0.07f, 1f);

            Light key = FindSceneRoot(scene, "Directional Key Light")
                ?.GetComponent<Light>();
            if (key != null)
            {
                key.intensity = 0.75f;
                key.color = new Color(0.8f, 0.9f, 1f, 1f);
            }

            Camera camera = FindSceneComponent<Camera>(scene);
            if (camera != null)
            {
                camera.backgroundColor = new Color(0.015f, 0.025f, 0.045f, 1f);
                camera.farClipPlane = 120f;
            }
        }

        private static GameObject CreateEnvironment(
            Material floorMaterial,
            Material wallMaterial,
            Material coverMaterial,
            Material accentMaterial)
        {
            GameObject root = new GameObject("Tutorial Environment");
            GameObject floorProxy = CreatePrimitive(
                root.transform,
                "Tutorial Floor",
                PrimitiveType.Cube,
                new Vector3(0f, -0.15f, 12.5f),
                new Vector3(14f, 0.3f, 97f),
                floorMaterial,
                true,
                0);
            floorProxy.GetComponent<Renderer>().enabled = false;
            CreateWall(
                root.transform,
                "West Tutorial Wall",
                new Vector3(-7f, 1.5f, 12.5f),
                new Vector3(0.5f, 3f, 97f),
                wallMaterial);
            CreateWall(
                root.transform,
                "East Tutorial Wall",
                new Vector3(7f, 1.5f, 12.5f),
                new Vector3(0.5f, 3f, 97f),
                wallMaterial);
            CreateWall(
                root.transform,
                "South Tutorial Wall",
                new Vector3(0f, 1.5f, -36f),
                new Vector3(14.5f, 3f, 0.5f),
                wallMaterial);
            CreateWall(
                root.transform,
                "North Tutorial Wall",
                new Vector3(0f, 1.5f, 61f),
                new Vector3(14.5f, 3f, 0.5f),
                wallMaterial);

            float[] zoneCenters = { -30f, -19f, -7f, 6f, 23f, 47f, 59f };
            for (int i = 0; i < zoneCenters.Length; i++)
            {
                GameObject stripe = CreatePrimitive(
                    root.transform,
                    $"Zone Guide {i + 1}",
                    PrimitiveType.Cube,
                    new Vector3(0f, 0.015f, zoneCenters[i]),
                    new Vector3(10f, 0.03f, 0.15f),
                    accentMaterial,
                    false,
                    0);
                stripe.transform.rotation = Quaternion.identity;
            }

            CreatePrimitive(
                root.transform,
                "Pistol Firing Rail West",
                PrimitiveType.Cube,
                new Vector3(-3.7f, 0.5f, 5.5f),
                new Vector3(5.5f, 1f, 0.5f),
                coverMaterial,
                true,
                VisionObstacleLayer);
            CreatePrimitive(
                root.transform,
                "Pistol Firing Rail East",
                PrimitiveType.Cube,
                new Vector3(3.7f, 0.5f, 5.5f),
                new Vector3(5.5f, 1f, 0.5f),
                coverMaterial,
                true,
                VisionObstacleLayer);

            CreatePointLight(root.transform, "Time Lesson Light", new Vector3(0f, 4f, -30f), new Color(0.1f, 0.65f, 1f, 1f));
            CreatePointLight(root.transform, "Melee Lesson Light", new Vector3(0f, 4f, -7f), new Color(1f, 0.38f, 0.08f, 1f));
            CreatePointLight(root.transform, "Pistol Lesson Light", new Vector3(0f, 4f, 7f), new Color(0.1f, 0.9f, 1f, 1f));
            CreatePointLight(root.transform, "Throw Lesson Light", new Vector3(0f, 4f, 24f), new Color(1f, 0.72f, 0.08f, 1f));
            CreatePointLight(root.transform, "Deadline Arena Light", new Vector3(0f, 5f, 47f), new Color(0.9f, 0.08f, 0.12f, 1f), 1.2f, 16f);
            ConfigureProxyRenderers(root);
            CreateSyntyEnvironment(root.transform, zoneCenters);
            return root;
        }

        private static void ConfigureProxyRenderers(GameObject root)
        {
            Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer renderer = renderers[i];
                string objectName = renderer.gameObject.name;
                if (objectName == "Tutorial Floor")
                {
                    renderer.enabled = false;
                }
                else if (objectName.EndsWith("Tutorial Wall", StringComparison.Ordinal) ||
                         objectName.StartsWith("Pistol Firing Rail", StringComparison.Ordinal))
                {
                    renderer.shadowCastingMode = ShadowCastingMode.ShadowsOnly;
                    renderer.receiveShadows = false;
                }
            }
        }

        private static void CreateSyntyEnvironment(
            Transform environment,
            float[] zoneCenters)
        {
            RequireSyntyTutorialAssets();
            GameObject visualRoot = new GameObject(SyntyVisualRootName);
            visualRoot.transform.SetParent(environment, false);

            const int floorRows = 20;
            for (int row = 0; row < floorRows; row++)
            {
                float z = -33.5f + row * 5f;
                for (int column = -1; column <= 1; column++)
                {
                    PlaceSyntyVisual(
                        visualRoot.transform,
                        SyntyFloorPath,
                        $"Floor {row + 1:00}-{column + 2}",
                        new Vector3(column * 5f, -0.1f, z),
                        Quaternion.identity,
                        new Vector3(2f, 1f, 2f));
                }

                string wallPath = row % 2 == 0
                    ? SyntyWallFeaturePath
                    : SyntyWallFeatureAlternatePath;
                PlaceSyntyVisual(
                    visualRoot.transform,
                    wallPath,
                    $"West Wall {row + 1:00}",
                    new Vector3(-7f, 0f, z),
                    Quaternion.Euler(0f, -90f, 0f),
                    new Vector3(2f, 1f, 1f));
                PlaceSyntyVisual(
                    visualRoot.transform,
                    wallPath,
                    $"East Wall {row + 1:00}",
                    new Vector3(7f, 0f, z),
                    Quaternion.Euler(0f, 90f, 0f),
                    new Vector3(2f, 1f, 1f));
            }

            for (int column = -1; column <= 1; column++)
            {
                PlaceSyntyVisual(
                    visualRoot.transform,
                    SyntyWallFeatureAlternatePath,
                    $"South Wall {column + 2}",
                    new Vector3(column * 5f, 0f, -36f),
                    Quaternion.Euler(0f, 180f, 0f),
                    new Vector3(2f, 1f, 1f));
                PlaceSyntyVisual(
                    visualRoot.transform,
                    SyntyWallFeaturePath,
                    $"North Wall {column + 2}",
                    new Vector3(column * 5f, 0f, 61f),
                    Quaternion.identity,
                    new Vector3(2f, 1f, 1f));
            }

            for (int i = 0; i < zoneCenters.Length; i++)
            {
                float z = zoneCenters[i];
                PlaceSyntyVisual(
                    visualRoot.transform,
                    SyntyPillarPath,
                    $"West Zone Pillar {i + 1}",
                    new Vector3(-6.7f, 0f, z),
                    Quaternion.identity,
                    Vector3.one);
                PlaceSyntyVisual(
                    visualRoot.transform,
                    SyntyPillarPath,
                    $"East Zone Pillar {i + 1}",
                    new Vector3(6.7f, 0f, z),
                    Quaternion.identity,
                    Vector3.one);
                PlaceSyntyVisual(
                    visualRoot.transform,
                    SyntyFloorLightPath,
                    $"West Route Light {i + 1}",
                    new Vector3(-4.8f, 0f, z),
                    Quaternion.identity,
                    Vector3.one);
                PlaceSyntyVisual(
                    visualRoot.transform,
                    SyntyFloorLightPath,
                    $"East Route Light {i + 1}",
                    new Vector3(4.8f, 0f, z),
                    Quaternion.Euler(0f, 180f, 0f),
                    Vector3.one);
            }

            PlaceSyntyProp(
                visualRoot.transform,
                SyntyDjBoothPath,
                "World Time Control Booth",
                new Vector3(-5.25f, 0f, -29.5f),
                Quaternion.Euler(0f, 90f, 0f),
                Vector3.one);
            PlaceSyntyProp(
                visualRoot.transform,
                SyntyFridgePath,
                "World Time Equipment Cabinet",
                new Vector3(5.35f, 0f, -29.5f),
                Quaternion.Euler(0f, -90f, 0f),
                Vector3.one);
            PlaceSyntyProp(
                visualRoot.transform,
                SyntyBenchPath,
                "Melee Briefing Bench",
                new Vector3(-5.4f, 0f, -7f),
                Quaternion.Euler(0f, 90f, 0f),
                Vector3.one);
            PlaceSyntyProp(
                visualRoot.transform,
                SyntyBoxStackAPath,
                "Melee Equipment Stack",
                new Vector3(5.35f, 0f, -7f),
                Quaternion.Euler(0f, -20f, 0f),
                Vector3.one);

            PlaceSyntyVisual(
                visualRoot.transform,
                SyntyBarPath,
                "Pistol Range West Cover",
                new Vector3(-3.7f, 0f, 5.5f),
                Quaternion.identity,
                new Vector3(2f, 1f, 1f));
            PlaceSyntyVisual(
                visualRoot.transform,
                SyntyBarPath,
                "Pistol Range East Cover",
                new Vector3(3.7f, 0f, 5.5f),
                Quaternion.identity,
                new Vector3(2f, 1f, 1f));

            PlaceSyntyProp(
                visualRoot.transform,
                SyntyBoxStackBPath,
                "Throw Lane West Stack",
                new Vector3(-5.4f, 0f, 21f),
                Quaternion.Euler(0f, 12f, 0f),
                Vector3.one);
            PlaceSyntyProp(
                visualRoot.transform,
                SyntyBoxStackAPath,
                "Throw Lane East Stack",
                new Vector3(5.4f, 0f, 29f),
                Quaternion.Euler(0f, -18f, 0f),
                Vector3.one);
            PlaceSyntyProp(
                visualRoot.transform,
                SyntyDumpsterPath,
                "Deadline Arena West Cover",
                new Vector3(-5.45f, 0f, 39.2f),
                Quaternion.Euler(0f, 90f, 0f),
                Vector3.one);
            PlaceSyntyProp(
                visualRoot.transform,
                SyntyBenchPath,
                "Deadline Arena East Cover",
                new Vector3(5.5f, 0f, 54.5f),
                Quaternion.Euler(0f, -90f, 0f),
                Vector3.one);
            PlaceSyntyVisual(
                visualRoot.transform,
                SyntyExitSignPath,
                "Tutorial Exit Sign",
                new Vector3(0f, 2.15f, 60.7f),
                Quaternion.identity,
                new Vector3(1.8f, 1.8f, 1.8f));
        }

        private static void RequireSyntyTutorialAssets()
        {
            string[] paths =
            {
                SyntyFloorPath,
                SyntyWallFeaturePath,
                SyntyWallFeatureAlternatePath,
                SyntyPillarPath,
                SyntyBarPath,
                SyntyBenchPath,
                SyntyBoxStackAPath,
                SyntyBoxStackBPath,
                SyntyFloorLightPath,
                SyntyExitSignPath,
                SyntyDjBoothPath,
                SyntyFridgePath,
                SyntyDumpsterPath
            };
            for (int i = 0; i < paths.Length; i++)
            {
                Require(
                    AssetDatabase.LoadAssetAtPath<GameObject>(paths[i]) != null,
                    $"Tutorial Synty prefab is missing: {paths[i]}");
            }
        }

        private static GameObject PlaceSyntyProp(
            Transform parent,
            string path,
            string name,
            Vector3 placement,
            Quaternion rotation,
            Vector3 scale)
        {
            GameObject visual = PlaceSyntyVisual(
                parent,
                path,
                name,
                placement,
                rotation,
                scale);
            Bounds bounds = CalculateRendererBounds(visual);
            GameObject collision = new GameObject(name + " Collision");
            collision.layer = VisionObstacleLayer;
            collision.transform.SetParent(parent, false);
            collision.transform.position = bounds.center;
            BoxCollider collider = collision.AddComponent<BoxCollider>();
            collider.size = new Vector3(
                Mathf.Max(0.2f, bounds.size.x * 0.88f),
                Mathf.Max(0.3f, bounds.size.y),
                Mathf.Max(0.2f, bounds.size.z * 0.88f));
            return visual;
        }

        private static GameObject PlaceSyntyVisual(
            Transform parent,
            string path,
            string name,
            Vector3 placement,
            Quaternion rotation,
            Vector3 scale)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            GameObject instance = PrefabUtility.InstantiatePrefab(
                prefab,
                parent) as GameObject;
            Require(instance != null, $"Failed to instantiate Tutorial Synty prefab: {path}");
            instance.name = name;
            instance.transform.SetPositionAndRotation(Vector3.zero, rotation);
            instance.transform.localScale = scale;

            Collider[] colliders = instance.GetComponentsInChildren<Collider>(true);
            for (int i = 0; i < colliders.Length; i++)
            {
                colliders[i].enabled = false;
            }

            Bounds bounds = CalculateRendererBounds(instance);
            Vector3 offset = new Vector3(
                placement.x - bounds.center.x,
                placement.y - bounds.min.y,
                placement.z - bounds.center.z);
            instance.transform.position += offset;
            return instance;
        }

        private static Bounds CalculateRendererBounds(GameObject root)
        {
            Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
            Require(renderers.Length > 0,
                $"Tutorial Synty prefab has no Renderer: {root.name}");
            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
            {
                bounds.Encapsulate(renderers[i].bounds);
            }

            return bounds;
        }

        private static TutorialGate CreateGate(
            Transform parent,
            string name,
            Vector3 position,
            Material material)
        {
            GameObject gateObject = CreatePrimitive(
                parent,
                name,
                PrimitiveType.Cube,
                position,
                new Vector3(13.5f, 2.8f, 0.45f),
                material,
                true,
                VisionObstacleLayer);
            TutorialGate gate = gateObject.AddComponent<TutorialGate>();
            gate.Configure(
                gateObject.GetComponent<Collider>(),
                gateObject.GetComponent<Renderer>());
            return gate;
        }

        private static TutorialTimeProbe CreateTimeProbe(
            Transform parent,
            WorldTimeController worldTime,
            Material accentMaterial)
        {
            GameObject hub = new GameObject("World Time Demonstration Clock");
            hub.transform.SetParent(parent);
            hub.transform.position = new Vector3(0f, 0.18f, -29.5f);
            TutorialTimeProbe probe = hub.AddComponent<TutorialTimeProbe>();
            probe.Configure(worldTime, 210f);

            for (int i = 0; i < 4; i++)
            {
                GameObject hand = CreatePrimitive(
                    hub.transform,
                    $"Clock Hand {i + 1}",
                    PrimitiveType.Cube,
                    hub.transform.position,
                    new Vector3(0.16f, 0.12f, 3.8f),
                    accentMaterial,
                    false,
                    0);
                hand.transform.localPosition = Vector3.zero;
                hand.transform.localRotation = Quaternion.Euler(0f, i * 45f, 0f);
            }

            return probe;
        }

        private static TutorialTargetDummy CreateTargetDummy(
            Transform parent,
            string name,
            Vector3 position,
            TutorialTargetDummy.AcceptedAttack acceptedAttack,
            Material material)
        {
            GameObject target = CreatePrimitive(
                parent,
                name,
                PrimitiveType.Cylinder,
                position,
                new Vector3(0.9f, 0.8f, 0.9f),
                material,
                true,
                0);
            TutorialTargetDummy dummy = target.AddComponent<TutorialTargetDummy>();
            dummy.Configure(acceptedAttack, target.GetComponent<Renderer>());
            return dummy;
        }

        private static TutorialWeaponDispenser CreateDispenser(
            string name,
            Vector3 position,
            WeaponController playerWeapon,
            WeaponPickup pickupPrefab,
            WeaponDefinition definition,
            int ammunition)
        {
            GameObject dispenserObject = new GameObject(name);
            dispenserObject.transform.position = position;
            GameObject anchorObject = new GameObject("Pickup Anchor");
            anchorObject.transform.SetParent(dispenserObject.transform, false);
            anchorObject.transform.localPosition = Vector3.zero;
            anchorObject.transform.localRotation = Quaternion.Euler(0f, 25f, 0f);

            TutorialWeaponDispenser dispenser =
                dispenserObject.AddComponent<TutorialWeaponDispenser>();
            dispenser.Configure(
                playerWeapon,
                pickupPrefab,
                definition,
                anchorObject.transform,
                ammunition);
            return dispenser;
        }

        private static void ConfigureEnemies(
            Scene scene,
            out EnemyHealth throwEnemyHealth,
            out EnemyCombatant throwEnemyBehavior,
            out WeaponController throwEnemyWeapon,
            out EnemyWeaponDrop throwEnemyDrop,
            out EnemyCombatant[] deadlineEnemies)
        {
            EnemyShooter[] shooters = FindSceneComponents<EnemyShooter>(scene);
            EnemyChaser[] chasers = FindSceneComponents<EnemyChaser>(scene);
            Require(shooters.Length == 2 && chasers.Length == 1,
                "Tutorial requires the two ranged and one chasing Stage1 enemy templates.");

            throwEnemyBehavior = shooters[0];
            throwEnemyHealth = throwEnemyBehavior.GetComponent<EnemyHealth>();
            throwEnemyWeapon = throwEnemyBehavior.GetComponent<WeaponController>();
            throwEnemyDrop = throwEnemyBehavior.GetComponent<EnemyWeaponDrop>();
            PrepareEnemy(
                throwEnemyBehavior,
                "Throw Lesson Armed Enemy",
                new Vector3(0f, 0.75f, 25f),
                new Vector3(0f, 0.75f, 20f));

            EnemyCombatant northEast = shooters[1];
            EnemyCombatant northWest = chasers[0];
            EnemyCombatant southEast = UnityEngine.Object.Instantiate(shooters[1]);
            EnemyCombatant southWest = UnityEngine.Object.Instantiate(chasers[0]);

            deadlineEnemies = new[]
            {
                northEast,
                northWest,
                southEast,
                southWest
            };
            Vector3 center = new Vector3(0f, 0.75f, 47f);
            Vector3[] positions =
            {
                new Vector3(2.7f, 0.75f, 50.2f),
                new Vector3(-2.7f, 0.75f, 50.2f),
                new Vector3(2.7f, 0.75f, 43.8f),
                new Vector3(-2.7f, 0.75f, 43.8f)
            };
            string[] names =
            {
                "Deadline Enemy North East",
                "Deadline Enemy North West",
                "Deadline Enemy South East",
                "Deadline Enemy South West"
            };
            for (int i = 0; i < deadlineEnemies.Length; i++)
            {
                PrepareEnemy(deadlineEnemies[i], names[i], positions[i], center);
            }
        }

        private static void PrepareEnemy(
            EnemyCombatant enemy,
            string name,
            Vector3 position,
            Vector3 lookAt)
        {
            enemy.gameObject.name = name;
            Vector3 direction = lookAt - position;
            direction.y = 0f;
            Quaternion rotation = direction.sqrMagnitude <= 0.0001f
                ? Quaternion.identity
                : Quaternion.LookRotation(direction.normalized, Vector3.up);
            enemy.transform.SetPositionAndRotation(position, rotation);
            enemy.enabled = false;
        }

        private static void ConfigureTutorialCharacterAnimations(Scene scene)
        {
            CharacterAnimationLibrary library =
                AssetDatabase.LoadAssetAtPath<CharacterAnimationLibrary>(
                    CharacterAnimationEditorSetup.LibraryPath);
            Require(library != null,
                "Tutorial requires the generated character animation library.");

            List<GameObject> owners = new List<GameObject>();
            PlayerHealth player = FindSceneComponent<PlayerHealth>(scene);
            Require(player != null, "Tutorial player is missing during character setup.");
            owners.Add(player.gameObject);
            EnemyHealth[] enemies = FindSceneComponents<EnemyHealth>(scene);
            for (int i = 0; i < enemies.Length; i++)
            {
                owners.Add(enemies[i].gameObject);
            }

            Require(owners.Count == ExpectedAnimatedActorCount,
                $"Tutorial character setup found {owners.Count} actors; " +
                $"expected {ExpectedAnimatedActorCount}.");
            for (int i = 0; i < owners.Count; i++)
            {
                GameObject owner = owners[i];
                CharacterVisualController visual =
                    owner.GetComponent<CharacterVisualController>();
                Require(visual != null && visual.VisualRoot != null,
                    $"Tutorial actor {owner.name} has no inherited Synty character model.");
                Require(
                    CharacterAnimationEditorSetup.ConfigureCharacter(
                        owner,
                        visual.VisualRoot.gameObject),
                    $"Failed to configure Tutorial Animator on {owner.name}.");
                visual.Configure(visual.VisualRoot);
                owner.GetComponent<EnemyCombatant>()?.ConfigureVisual(visual);
                owner.GetComponent<EnemyHealth>()?.ConfigureVisual(visual);
                owner.GetComponent<PlayerHealth>()?.ConfigureVisual(visual);
                EditorUtility.SetDirty(owner);
                EditorUtility.SetDirty(visual);
            }
        }

        private static TutorialTrigger CreateTrigger(
            string name,
            Vector3 position,
            Vector3 size,
            TutorialDirector director,
            TutorialTrigger.TriggerKind kind)
        {
            GameObject triggerObject = new GameObject(name);
            triggerObject.transform.position = position;
            BoxCollider collider = triggerObject.AddComponent<BoxCollider>();
            collider.isTrigger = true;
            collider.size = size;
            TutorialTrigger trigger = triggerObject.AddComponent<TutorialTrigger>();
            trigger.Configure(director, kind);
            return trigger;
        }

        private static Transform CreateMarker(
            Transform parent,
            string name,
            Vector3 position)
        {
            GameObject marker = new GameObject(name);
            marker.transform.SetParent(parent);
            marker.transform.position = position;
            marker.transform.rotation = Quaternion.identity;
            return marker.transform;
        }

        private static void BuildTutorialNavigation(
            NavMeshSurface surface,
            Scene scene)
        {
            surface.RemoveData();
            SerializedObject surfaceSettings = new SerializedObject(surface);
            surfaceSettings.Update();
            SerializedProperty dataProperty =
                surfaceSettings.FindProperty("m_NavMeshData");
            if (dataProperty != null)
            {
                dataProperty.objectReferenceValue = null;
                surfaceSettings.ApplyModifiedPropertiesWithoutUndo();
            }

            List<Collider> disabled = new List<Collider>();
            Collider[] colliders = FindSceneComponents<Collider>(scene);
            for (int i = 0; i < colliders.Length; i++)
            {
                Collider collider = colliders[i];
                if (collider == null || !collider.enabled)
                {
                    continue;
                }

                if (collider.GetComponentInParent<PlayerHealth>() != null ||
                    collider.GetComponentInParent<EnemyHealth>() != null ||
                    collider.GetComponentInParent<TutorialTargetDummy>() != null ||
                    collider.GetComponentInParent<TutorialGate>() != null ||
                    collider.GetComponentInParent<TutorialTrigger>() != null)
                {
                    collider.enabled = false;
                    disabled.Add(collider);
                }
            }

            try
            {
                Physics.SyncTransforms();
                surface.BuildNavMesh();
            }
            finally
            {
                for (int i = 0; i < disabled.Count; i++)
                {
                    if (disabled[i] != null)
                    {
                        disabled[i].enabled = true;
                    }
                }
            }

            Require(surface.navMeshData != null,
                "Tutorial NavMeshSurface failed to build data.");
            NavMeshData bakedData = surface.navMeshData;
            bakedData.name = "TutorialNavigation";
            NavMeshData savedData = AssetDatabase.LoadAssetAtPath<NavMeshData>(
                NavigationDataPath);
            if (savedData == null)
            {
                AssetDatabase.CreateAsset(bakedData, NavigationDataPath);
            }
            else
            {
                surface.RemoveData();
                EditorUtility.CopySerialized(bakedData, savedData);
                savedData.name = "TutorialNavigation";
                surface.navMeshData = savedData;
                surface.AddData();
                UnityEngine.Object.DestroyImmediate(bakedData);
                EditorUtility.SetDirty(savedData);
            }

            EditorUtility.SetDirty(surface);
            AssetDatabase.SaveAssets();
        }

        private static void ConfigureBuildSettings()
        {
            List<EditorBuildSettingsScene> existing =
                new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            List<EditorBuildSettingsScene> ordered =
                new List<EditorBuildSettingsScene>();
            for (int i = 0; i < OrderedBuildScenes.Length; i++)
            {
                if (AssetDatabase.LoadAssetAtPath<SceneAsset>(
                        OrderedBuildScenes[i]) != null)
                {
                    ordered.Add(new EditorBuildSettingsScene(
                        OrderedBuildScenes[i],
                        true));
                }
            }

            for (int i = 0; i < existing.Count; i++)
            {
                bool known = false;
                for (int j = 0; j < OrderedBuildScenes.Length; j++)
                {
                    if (existing[i].path == OrderedBuildScenes[j])
                    {
                        known = true;
                        break;
                    }
                }

                if (!known)
                {
                    ordered.Add(existing[i]);
                }
            }

            EditorBuildSettings.scenes = ordered.ToArray();
        }

        private static void ValidateTutorialScene(Scene scene)
        {
            TutorialDirector director = FindSceneComponent<TutorialDirector>(scene);
            TutorialHud hud = FindSceneComponent<TutorialHud>(scene);
            TutorialTimeProbe[] probes = FindSceneComponents<TutorialTimeProbe>(scene);
            TutorialGate[] gates = FindSceneComponents<TutorialGate>(scene);
            TutorialTrigger[] triggers = FindSceneComponents<TutorialTrigger>(scene);
            TutorialTargetDummy[] targets =
                FindSceneComponents<TutorialTargetDummy>(scene);
            TutorialWeaponDispenser[] dispensers =
                FindSceneComponents<TutorialWeaponDispenser>(scene);
            EnemyHealth[] enemies = FindSceneComponents<EnemyHealth>(scene);
            PlayerHealth[] players = FindSceneComponents<PlayerHealth>(scene);
            WorldTimeController[] times =
                FindSceneComponents<WorldTimeController>(scene);
            DeadlineController[] deadlines =
                FindSceneComponents<DeadlineController>(scene);
            StageController[] stages =
                FindSceneComponents<StageController>(scene);
            StageReplayController[] replays =
                FindSceneComponents<StageReplayController>(scene);
            VisionCone[] visionCones = FindSceneComponents<VisionCone>(scene);
            Camera[] cameras = FindSceneComponents<Camera>(scene);
            NavMeshSurface surface = FindSceneComponent<NavMeshSurface>(scene);
            GameHud gameHud = FindSceneComponent<GameHud>(scene);
            CharacterAnimationController[] animationControllers =
                FindSceneComponents<CharacterAnimationController>(scene);
            CharacterVisualController[] characterVisuals =
                FindSceneComponents<CharacterVisualController>(scene);
            WeaponController playerWeapon = players.Length == 1
                ? players[0].GetComponent<WeaponController>()
                : null;
            SerializedObject playerWeaponSettings = playerWeapon == null
                ? null
                : new SerializedObject(playerWeapon);
            playerWeaponSettings?.Update();
            SerializedProperty startingDefinition = playerWeaponSettings
                ?.FindProperty("startingDefinition");

            Require(scene.path == TutorialScenePath,
                $"Validated scene path is {scene.path}, expected {TutorialScenePath}.");
            string directorError = "TutorialDirector is missing.";
            bool directorIsValid = director != null &&
                director.ValidateConfiguration(out directorError);
            Require(directorIsValid, directorError);
            Require(hud != null && hud.enabled,
                "TutorialHud is missing or disabled.");
            Require(probes.Length == 1,
                $"Tutorial requires one WorldDeltaTime probe, found {probes.Length}.");
            Require(gates.Length == 6,
                $"Tutorial requires six progression gates, found {gates.Length}.");
            Require(triggers.Length == 3,
                $"Tutorial requires three progression triggers, found {triggers.Length}.");
            Require(targets.Length == 2 &&
                    Array.Exists(targets, t =>
                        t.RequiredAttack == TutorialTargetDummy.AcceptedAttack.Melee) &&
                    Array.Exists(targets, t =>
                        t.RequiredAttack == TutorialTargetDummy.AcceptedAttack.Firearm),
                "Tutorial requires one melee-only and one firearm-only target.");
            Require(dispensers.Length == 3,
                $"Tutorial requires three recoverable weapon dispensers, found {dispensers.Length}.");
            Require(enemies.Length == ExpectedEnemyCount,
                $"Tutorial requires one throw enemy and four DEADLINE enemies, found {enemies.Length}.");
            Require(players.Length == 1 && times.Length == 1 &&
                    deadlines.Length == 1 && cameras.Length == 1,
                "Tutorial requires exactly one player, world time, deadline, and camera.");
            Require(stages.Length == 0 && replays.Length == 1,
                "Tutorial must replace StageController completion while preserving one replay dependency for VisionCone.");
            Require(visionCones.Length == 1 && visionCones[0].HasUnlimitedVision &&
                    !visionCones[0].GetComponent<MeshRenderer>().enabled,
                "Tutorial must disable the VisionCone limit and its overlay.");
            Require(startingDefinition != null &&
                    startingDefinition.objectReferenceValue == null,
                "Tutorial player must start unarmed.");
            Require(surface != null && surface.navMeshData != null &&
                    AssetDatabase.GetAssetPath(surface.navMeshData) ==
                    NavigationDataPath,
                "Tutorial must use its dedicated TutorialNavigation.asset.");
            Require(gameHud == null,
                "Legacy GameHud must be removed from Tutorial.");
            Require(animationControllers.Length == ExpectedAnimatedActorCount &&
                    characterVisuals.Length == ExpectedAnimatedActorCount,
                $"Tutorial requires {ExpectedAnimatedActorCount} animated Synty actors; " +
                $"found {animationControllers.Length} animation drivers and " +
                $"{characterVisuals.Length} character visuals.");

            ValidateTutorialArtAndCharacters(scene, animationControllers);
            ValidateTutorialNavigationRoute(surface);
            ValidateVisionObstaclePolicy(scene);
            ValidateBuildSettings();
        }

        private static void ValidateTutorialArtAndCharacters(
            Scene scene,
            CharacterAnimationController[] animationControllers)
        {
            GameObject environment = FindSceneRoot(scene, "Tutorial Environment");
            Transform visualRoot = environment == null
                ? null
                : environment.transform.Find(SyntyVisualRootName);
            Require(visualRoot != null,
                $"Tutorial is missing its {SyntyVisualRootName} root.");

            int prefabCount = 0;
            for (int i = 0; i < visualRoot.childCount; i++)
            {
                GameObject child = visualRoot.GetChild(i).gameObject;
                if (PrefabUtility.GetCorrespondingObjectFromSource(child) != null)
                {
                    prefabCount++;
                }
            }

            Require(prefabCount >= MinimumSyntyPrefabCount,
                $"Tutorial contains {prefabCount} Synty prefab instances; " +
                $"expected at least {MinimumSyntyPrefabCount}.");
            Require(visualRoot.Find("Pistol Range West Cover") != null &&
                    visualRoot.Find("Pistol Range East Cover") != null &&
                    visualRoot.Find("Tutorial Exit Sign") != null,
                "Tutorial Synty landmarks are incomplete.");

            for (int i = 0; i < animationControllers.Length; i++)
            {
                CharacterAnimationController controller =
                    animationControllers[i];
                Animator animator = controller.Animator;
                CharacterVisualController visual =
                    controller.GetComponent<CharacterVisualController>();
                Require(animator != null && animator.enabled &&
                        animator.runtimeAnimatorController != null &&
                        animator.applyRootMotion == false &&
                        animator.updateMode == AnimatorUpdateMode.UnscaledTime,
                    $"Tutorial Animator is not configured on {controller.name}.");
                Require(visual != null && visual.VisualRoot != null &&
                        visual.VisualRoot.GetComponentsInChildren<
                            SkinnedMeshRenderer>(true).Length > 0,
                    $"Tutorial actor {controller.name} has no rendered Synty model.");
            }
        }

        private static void ValidateTutorialNavigationRoute(
            NavMeshSurface surface)
        {
            Require(surface != null && surface.navMeshData != null,
                "Tutorial navigation route validation requires baked NavMesh data.");
            Vector3[] checkpoints =
            {
                new Vector3(0f, 0.75f, -34f),
                new Vector3(0f, 0.75f, -18f),
                new Vector3(0f, 0.75f, -7f),
                new Vector3(0f, 0.75f, 6f),
                new Vector3(0f, 0.75f, 23f),
                new Vector3(0f, 0.75f, 47f),
                new Vector3(0f, 0.75f, 59.5f)
            };

            NavMeshHit previousHit;
            Require(NavMesh.SamplePosition(
                    checkpoints[0],
                    out previousHit,
                    1.5f,
                    NavMesh.AllAreas),
                "Tutorial start is not on the baked NavMesh.");
            for (int i = 1; i < checkpoints.Length; i++)
            {
                NavMeshHit nextHit;
                Require(NavMesh.SamplePosition(
                        checkpoints[i],
                        out nextHit,
                        1.5f,
                        NavMesh.AllAreas),
                    $"Tutorial checkpoint {i + 1} is not on the baked NavMesh.");
                NavMeshPath path = new NavMeshPath();
                bool foundPath = NavMesh.CalculatePath(
                    previousHit.position,
                    nextHit.position,
                    NavMesh.AllAreas,
                    path);
                Require(foundPath && path.status == NavMeshPathStatus.PathComplete,
                    $"Tutorial route is blocked between checkpoints {i} and {i + 1}.");
                previousHit = nextHit;
            }
        }

        private static void ValidateVisionObstaclePolicy(Scene scene)
        {
            GameObject environment = FindSceneRoot(scene, "Tutorial Environment");
            Require(environment != null, "Tutorial Environment root is missing.");
            Collider[] colliders = environment.GetComponentsInChildren<Collider>(true);
            int obstacleCount = 0;
            for (int i = 0; i < colliders.Length; i++)
            {
                if (colliders[i].gameObject.layer == VisionObstacleLayer)
                {
                    obstacleCount++;
                }
            }

            Require(obstacleCount >= 10,
                $"Tutorial VisionObstacle geometry count is {obstacleCount}; expected at least 10.");
        }

        private static void ValidateBuildSettings()
        {
            EditorBuildSettingsScene[] scenes = EditorBuildSettings.scenes;
            Require(scenes.Length >= OrderedBuildScenes.Length,
                "Build Settings do not contain Tutorial and Stage1 through Stage6.");
            for (int i = 0; i < OrderedBuildScenes.Length; i++)
            {
                Require(scenes[i].enabled &&
                        scenes[i].path == OrderedBuildScenes[i],
                    $"Build index {i} is {scenes[i].path}; " +
                    $"expected {OrderedBuildScenes[i]}.");
            }
        }

        private static GameObject CreatePrimitive(
            Transform parent,
            string name,
            PrimitiveType type,
            Vector3 position,
            Vector3 scale,
            Material material,
            bool keepCollider,
            int layer)
        {
            GameObject gameObject = GameObject.CreatePrimitive(type);
            gameObject.name = name;
            gameObject.transform.SetParent(parent);
            gameObject.transform.SetPositionAndRotation(position, Quaternion.identity);
            gameObject.transform.localScale = scale;
            gameObject.layer = layer;
            Renderer renderer = gameObject.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = material;
            }

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

        private static void CreateWall(
            Transform parent,
            string name,
            Vector3 position,
            Vector3 scale,
            Material material)
        {
            CreatePrimitive(
                parent,
                name,
                PrimitiveType.Cube,
                position,
                scale,
                material,
                true,
                VisionObstacleLayer);
        }

        private static void CreatePointLight(
            Transform parent,
            string name,
            Vector3 position,
            Color color,
            float intensity = 0.7f,
            float range = 12f)
        {
            GameObject lightObject = new GameObject(name);
            lightObject.transform.SetParent(parent);
            lightObject.transform.position = position;
            Light light = lightObject.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = color;
            light.intensity = intensity;
            light.range = range;
            light.shadows = LightShadows.None;
        }

        private static void SetObjectReference(
            UnityEngine.Object target,
            string propertyName,
            UnityEngine.Object value)
        {
            SerializedObject serialized = new SerializedObject(target);
            serialized.Update();
            SerializedProperty property = serialized.FindProperty(propertyName);
            Require(property != null,
                $"{target.name} is missing serialized property {propertyName}.");
            property.objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(target);
        }

        private static T LoadAsset<T>(string path) where T : UnityEngine.Object
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            Require(asset != null, $"Required asset is missing: {path}");
            return asset;
        }

        private static GameObject FindSceneRoot(Scene scene, string name)
        {
            GameObject[] roots = scene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                if (roots[i].name == name)
                {
                    return roots[i];
                }
            }

            return null;
        }

        private static T FindSceneComponent<T>(Scene scene)
            where T : Component
        {
            T[] values = FindSceneComponents<T>(scene);
            return values.Length == 0 ? null : values[0];
        }

        private static T[] FindSceneComponents<T>(Scene scene)
            where T : Component
        {
            List<T> values = new List<T>();
            GameObject[] roots = scene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                values.AddRange(roots[i].GetComponentsInChildren<T>(true));
            }

            return values.ToArray();
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
