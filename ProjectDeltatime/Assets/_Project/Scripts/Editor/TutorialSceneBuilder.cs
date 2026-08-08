using System;
using System.Collections.Generic;
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
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
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
        private const int VisionObstacleLayer = 8;
        private const int ExpectedEnemyCount = 5;

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
            CreatePrimitive(
                root.transform,
                "Tutorial Floor",
                PrimitiveType.Cube,
                new Vector3(0f, -0.15f, 12.5f),
                new Vector3(14f, 0.3f, 97f),
                floorMaterial,
                true,
                0);
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
            return root;
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

            ValidateVisionObstaclePolicy(scene);
            ValidateBuildSettings();
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
