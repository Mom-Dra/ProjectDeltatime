using System;
using System.Collections.Generic;
using System.IO;
using Deltatime.Combat;
using Deltatime.Enemies;
using Deltatime.Level;
using Deltatime.Player;
using Deltatime.Replay;
using Deltatime.TimeSystem;
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
    /// <summary>
    /// Builds the active-route batting-cage encounter from Stage2's proven
    /// gameplay roots without rewriting any existing stage.
    /// </summary>
    public static class StageBattingCageSceneBuilder
    {
        public const string ScenePath =
            "Assets/_Project/Scenes/StageBattingCage.unity";
        public const string NavigationPath =
            "Assets/_Project/Scenes/StageBattingCageNavigation.asset";
        public const string EnvironmentRootName =
            "Stage 3 - Underground Batting Cage";

        private const string SourceScenePath =
            "Assets/_Project/Scenes/Stage2.unity";
        private const string PreviewAssetPath =
            "Assets/_Project/Art/Generated/StageBattingCagePreview.png";
        private const string MeleeDefinitionPath =
            "Assets/_Project/MeleeWeapon.asset";
        private const string NightclubRoot =
            "Assets/Synty/PolygonNightclubs/Prefabs";
        private const string PropsRoot = NightclubRoot + "/Props";
        private const string BuildingsRoot = NightclubRoot + "/Buildings";
        private const string BaseBuildingsRoot =
            NightclubRoot + "/Base_Buildings";
        private const string CharacterRoot = NightclubRoot + "/Characters";
        private const string FloorPath =
            BuildingsRoot + "/SM_Bld_Floor_Combined_01.prefab";
        private const string FencePath =
            PropsRoot + "/SM_Prop_Fence_Wire_01.prefab";
        private const string CagePath =
            PropsRoot + "/SM_Prop_Dancing_Cage_01.prefab";
        private const string SportsBagPath =
            PropsRoot + "/SM_Prop_Bag_Sports_01.prefab";
        private const string SpeakerPath =
            PropsRoot + "/SM_Prop_Speaker_Large_01.prefab";
        private const string StageLightPath =
            PropsRoot + "/SM_Prop_Light_Stage_05.prefab";
        private const string PillarPath =
            BaseBuildingsRoot + "/SM_Bld_Base_Pillar_Metal_01.prefab";
        private const string PlayerCharacterPath =
            "Assets/Synty/PolygonGeneric/Prefabs/Characters/" +
            "SM_Gen_Chr_Business_Male_01.prefab";
        private const string AccentMaterialPath =
            "Assets/_Project/Materials/PrototypeAccent3D.mat";
        private const string ChaserMaterialPath =
            "Assets/_Project/Materials/PrototypeChaser3D.mat";
        private const int VisionObstacleLayer = 8;
        private const int EnemyCount = 6;
        private const int DeadlineCharges = 2;
        private const float FloorTileSize = 2.5f;
        private const float FenceSegmentLength = 2.6678f;
        private const float FenceHalfDepth = 0.254f;
        private const float NorthSouthFenceLine = 9.8f;
        private const float EastWestFenceLine = 10.8f;
        private static readonly Vector3 FenceBodyCenterLocal =
            new Vector3(-1.2409f, 0f, -0.0072f);

        private static readonly string[] EnemyNames =
        {
            "Enemy Bat East",
            "Enemy Bat North East",
            "Enemy Bat North West",
            "Enemy Bat West",
            "Enemy Bat South West",
            "Enemy Bat South East"
        };

        private static readonly Vector3[] EnemyPositions =
        {
            new Vector3(6.5f, 0.78f, 0f),
            new Vector3(4.5f, 0.78f, 7.794f),
            new Vector3(-3.25f, 0.78f, 5.629f),
            new Vector3(-9f, 0.78f, 0f),
            new Vector3(-3.25f, 0.78f, -5.629f),
            new Vector3(4.5f, 0.78f, -7.794f)
        };

        private static readonly Vector3[] PillarPositions =
        {
            new Vector3(1.6f, 0.85f, 2.771f),
            new Vector3(-3.2f, 0.85f, 0f),
            new Vector3(1.6f, 0.85f, -2.771f)
        };

        private static readonly string[] EnemyCharacterPaths =
        {
            CharacterRoot + "/SM_Chr_Bouncer_Male_01.prefab",
            CharacterRoot + "/SM_Chr_Bartender_Male_01.prefab",
            CharacterRoot + "/SM_Chr_Party_Female_01.prefab",
            CharacterRoot + "/SM_Chr_Party_Male_01.prefab",
            CharacterRoot + "/SM_Chr_Bartender_Female_01.prefab",
            CharacterRoot + "/SM_Chr_Party_Male_02.prefab"
        };

        [MenuItem("Tools/Prototype/Build Stage - Underground Batting Cage")]
        public static void BuildStageBattingCage()
        {
            RequireAssets();

            Scene source = EditorSceneManager.OpenScene(
                SourceScenePath,
                OpenSceneMode.Single);
            if (!EditorSceneManager.SaveScene(source, ScenePath, true))
            {
                throw new InvalidOperationException(
                    $"Failed to copy {SourceScenePath} to {ScenePath}.");
            }

            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            Scene scene = EditorSceneManager.OpenScene(
                ScenePath,
                OpenSceneMode.Single);

            RemoveSourceEncounter();
            ConfigurePlayer();
            ConfigureSixEnemyEncounter(scene);
            GameObject environment = BuildEnvironment(scene);
            ConfigureCharacterVisuals(scene);
            ConfigureLighting(environment.transform);
            ConfigureCamera();
            BuildNavigation();

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, ScenePath))
            {
                throw new InvalidOperationException(
                    $"Failed to save {ScenePath}.");
            }

            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            GameBuildSceneCatalog.Apply();
            AssetDatabase.SaveAssets();
            ValidateScene(scene);

            if (!Application.isBatchMode)
            {
                Selection.activeGameObject = GameObject.Find("Player");
            }

            Debug.Log(
                "StageBattingCage underground batting arena built and " +
                "validated successfully.");
        }

        public static void BuildAndValidateFromCommandLine()
        {
            SceneBuildCommand.Run(BuildStageBattingCage);
        }

        [MenuItem("Tools/Prototype/Rebake Stage - Underground Batting Cage Navigation")]
        public static void RebakeNavigationOnly()
        {
            Scene scene = EditorSceneManager.OpenScene(
                ScenePath,
                OpenSceneMode.Single);
            BuildNavigation();
            NavMeshSurface surface =
                UnityEngine.Object.FindFirstObjectByType<NavMeshSurface>();
            ValidateNavigation(surface, scene);
            Debug.Log(
                "StageBattingCage navigation rebaked and validated without " +
                "rebuilding the scene.");
        }

        public static void RebakeNavigationOnlyFromCommandLine()
        {
            SceneBuildCommand.Run(RebakeNavigationOnly);
        }

        [MenuItem("Tools/Prototype/Validate Stage - Underground Batting Cage")]
        public static void ValidateSavedScene()
        {
            Scene scene = EditorSceneManager.OpenScene(
                ScenePath,
                OpenSceneMode.Single);
            ValidateScene(scene);
            Debug.Log("StageBattingCage static validation passed.");
        }

        public static void CapturePreviewFromCommandLine()
        {
            Scene scene = EditorSceneManager.OpenScene(
                ScenePath,
                OpenSceneMode.Single);
            ValidateScene(scene);

            Camera camera = UnityEngine.Object.FindFirstObjectByType<Camera>();
            SceneValidation.Require(
                camera != null,
                "StageBattingCage preview requires the gameplay camera.");

            Vector3 previousPosition = camera.transform.position;
            Quaternion previousRotation = camera.transform.rotation;
            float previousFieldOfView = camera.fieldOfView;
            try
            {
                string outputPath = Path.Combine(
                    Application.dataPath,
                    "_Project",
                    "Art",
                    "Generated",
                    "StageBattingCagePreview.png");
                PreviewCapture.CapturePng(
                    camera,
                    1280,
                    720,
                    outputPath,
                    () =>
                    {
                        camera.transform.position =
                            new Vector3(0f, 18.5f, -18.5f);
                        camera.transform.LookAt(
                            new Vector3(0f, 0.35f, 0f),
                            Vector3.up);
                        camera.fieldOfView = 56f;
                    });
                AssetDatabase.ImportAsset(
                    PreviewAssetPath,
                    ImportAssetOptions.ForceSynchronousImport);
            }
            finally
            {
                camera.transform.SetPositionAndRotation(
                    previousPosition,
                    previousRotation);
                camera.fieldOfView = previousFieldOfView;
            }

            Debug.Log(
                "StageBattingCage preview captured at " +
                PreviewAssetPath + ".");
        }

        private static void RequireAssets()
        {
            string[] requiredPaths =
            {
                SourceScenePath,
                MeleeDefinitionPath,
                FloorPath,
                FencePath,
                CagePath,
                SportsBagPath,
                SpeakerPath,
                StageLightPath,
                PillarPath,
                PlayerCharacterPath,
                AccentMaterialPath,
                ChaserMaterialPath
            };

            for (int i = 0; i < EnemyCharacterPaths.Length; i++)
            {
                SceneValidation.Require(
                    AssetDatabase.LoadAssetAtPath<GameObject>(
                        EnemyCharacterPaths[i]) != null,
                    "Missing batting-cage character prefab: " +
                    EnemyCharacterPaths[i]);
            }

            for (int i = 0; i < requiredPaths.Length; i++)
            {
                SceneValidation.Require(
                    AssetDatabase.LoadMainAssetAtPath(requiredPaths[i]) != null,
                    "Missing StageBattingCage dependency: " +
                    requiredPaths[i]);
            }
        }

        private static void RemoveSourceEncounter()
        {
            DestroySceneObject("Industrial Room");
            DestroySceneObject("Blue Bay Light");
            DestroySceneObject("Red Alert Light");
            DestroySceneObject("Enemy West");
            DestroySceneObject("Enemy East");

            WeaponPickup[] pickups =
                UnityEngine.Object.FindObjectsByType<WeaponPickup>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None);
            for (int i = 0; i < pickups.Length; i++)
            {
                if (pickups[i] != null &&
                    pickups[i].gameObject.scene.path == ScenePath)
                {
                    UnityEngine.Object.DestroyImmediate(
                        pickups[i].gameObject);
                }
            }
        }

        private static void DestroySceneObject(string name)
        {
            GameObject target = GameObject.Find(name);
            if (target != null)
            {
                UnityEngine.Object.DestroyImmediate(target);
            }
        }

        private static void ConfigurePlayer()
        {
            GameObject player = GameObject.Find("Player");
            SceneValidation.Require(
                player != null,
                "StageBattingCage requires the Stage2 player root.");
            player.transform.SetPositionAndRotation(
                new Vector3(0f, 0.75f, 0f),
                Quaternion.identity);
            SetStartingDefinition(
                player.GetComponent<WeaponController>(),
                LoadMeleeDefinition());
        }

        private static void ConfigureSixEnemyEncounter(Scene scene)
        {
            GameObject source = GameObject.Find("Enemy Center");
            SceneValidation.Require(
                source != null && source.GetComponent<EnemyChaser>() != null,
                "StageBattingCage requires Stage2's center melee enemy.");

            source.name = EnemyNames[0];
            GameObject[] enemies = new GameObject[EnemyCount];
            enemies[0] = source;
            for (int i = 1; i < enemies.Length; i++)
            {
                GameObject clone = UnityEngine.Object.Instantiate(
                    source,
                    source.transform.position,
                    source.transform.rotation);
                clone.name = EnemyNames[i];
                SceneManager.MoveGameObjectToScene(clone, scene);
                enemies[i] = clone;
            }

            WeaponDefinition melee = LoadMeleeDefinition();
            for (int i = 0; i < enemies.Length; i++)
            {
                Vector3 position = EnemyPositions[i];
                Vector3 direction = -new Vector3(
                    position.x,
                    0f,
                    position.z);
                enemies[i].transform.SetPositionAndRotation(
                    position,
                    Quaternion.LookRotation(direction.normalized, Vector3.up));
                SetStartingDefinition(
                    enemies[i].GetComponent<WeaponController>(),
                    melee);
            }
        }

        private static WeaponDefinition LoadMeleeDefinition()
        {
            WeaponDefinition melee =
                AssetDatabase.LoadAssetAtPath<WeaponDefinition>(
                    MeleeDefinitionPath);
            SceneValidation.Require(
                melee != null,
                "MeleeWeapon.asset is missing.");
            return melee;
        }

        private static void SetStartingDefinition(
            WeaponController weapon,
            WeaponDefinition definition)
        {
            SceneValidation.Require(
                weapon != null,
                "A batting-cage actor is missing WeaponController.");
            SerializedObject settings = new SerializedObject(weapon);
            SerializedProperty startingDefinition =
                settings.FindProperty("startingDefinition");
            SceneValidation.Require(
                startingDefinition != null,
                "WeaponController.startingDefinition could not be serialized.");
            startingDefinition.objectReferenceValue = definition;
            settings.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(weapon);
        }

        private static GameObject BuildEnvironment(Scene scene)
        {
            GameObject root = new GameObject(EnvironmentRootName);
            SceneManager.MoveGameObjectToScene(root, scene);
            root.AddComponent<ReplayExcluded>();

            Transform architecture = CreateGroup("Architecture", root.transform);
            Transform blockers = CreateGroup("Vision Blockers", root.transform);
            Transform decor = CreateGroup("Cage Decor", root.transform);

            CreateCollisionBlocker(
                "Batting Cage Floor Collision",
                new Vector3(0f, -0.12f, 0f),
                new Vector3(22f, 0.24f, 20f),
                root.transform,
                false);
            CreateCollisionBlocker(
                "North Cage Boundary",
                new Vector3(0f, 1.5f, 10f),
                new Vector3(22.5f, 3f, 0.4f),
                root.transform,
                true);
            CreateCollisionBlocker(
                "South Cage Boundary",
                new Vector3(0f, 1.5f, -10f),
                new Vector3(22.5f, 3f, 0.4f),
                root.transform,
                true);
            CreateCollisionBlocker(
                "West Cage Boundary",
                new Vector3(-11f, 1.5f, 0f),
                new Vector3(0.4f, 3f, 20.5f),
                root.transform,
                true);
            CreateCollisionBlocker(
                "East Cage Boundary",
                new Vector3(11f, 1.5f, 0f),
                new Vector3(0.4f, 3f, 20.5f),
                root.transform,
                true);

            BuildFloor(architecture);
            BuildFence(architecture);
            BuildPillars(blockers);
            BuildDecor(decor);
            BuildFloorMarking(decor);
            return root;
        }

        private static Transform CreateGroup(string name, Transform parent)
        {
            GameObject group = new GameObject(name);
            group.transform.SetParent(parent, false);
            return group.transform;
        }

        private static void BuildFloor(Transform parent)
        {
            int number = 1;
            for (int x = 0; x < 9; x++)
            {
                for (int z = 0; z < 8; z++)
                {
                    CreateNightclubAsset(
                        FloorPath,
                        $"Cage Floor {number++:00}",
                        parent,
                        new Vector3(
                            (x - 3.5f) * FloorTileSize,
                            0f,
                            (z - 4f) * FloorTileSize),
                        0f,
                        Vector3.one);
                }
            }
        }

        private static void BuildFence(Transform parent)
        {
            float northSouthSpan = 2f * (EastWestFenceLine + FenceHalfDepth);
            float eastWestSpan = 2f * (NorthSouthFenceLine - FenceHalfDepth);
            int number = 1;
            for (int i = 0; i < 8; i++)
            {
                float position =
                    FenceRunCenter(-northSouthSpan * 0.5f, northSouthSpan, 8, i);
                PlaceFence(
                    $"North Wire Fence {number:00}",
                    parent,
                    new Vector3(position, 0f, NorthSouthFenceLine),
                    0f);
                PlaceFence(
                    $"South Wire Fence {number++:00}",
                    parent,
                    new Vector3(position, 0f, -NorthSouthFenceLine),
                    180f);
            }

            for (int i = 0; i < 7; i++)
            {
                float position =
                    FenceRunCenter(-eastWestSpan * 0.5f, eastWestSpan, 7, i);
                PlaceFence(
                    $"West Wire Fence {number:00}",
                    parent,
                    new Vector3(-EastWestFenceLine, 0f, position),
                    90f);
                PlaceFence(
                    $"East Wire Fence {number++:00}",
                    parent,
                    new Vector3(EastWestFenceLine, 0f, position),
                    -90f);
            }
        }

        private static float FenceRunCenter(
            float spanStart,
            float spanLength,
            int count,
            int index)
        {
            float gap = (spanLength - count * FenceSegmentLength) / (count - 1);
            return spanStart +
                   FenceSegmentLength * 0.5f +
                   index * (FenceSegmentLength + gap);
        }

        private static void PlaceFence(
            string instanceName,
            Transform parent,
            Vector3 bodyCenter,
            float yaw)
        {
            Quaternion rotation = Quaternion.Euler(0f, yaw, 0f);
            Vector3 position = bodyCenter - rotation * FenceBodyCenterLocal;
            CreateNightclubAsset(
                FencePath,
                instanceName,
                parent,
                position,
                yaw,
                Vector3.one);
        }

        private static void BuildPillars(Transform parent)
        {
            for (int i = 0; i < PillarPositions.Length; i++)
            {
                Vector3 position = PillarPositions[i];
                CreateCollisionBlocker(
                    $"Initial Sight Pillar {i + 1}",
                    position,
                    new Vector3(1.05f, 1.7f, 1.05f),
                    parent,
                    true);
                CreateNightclubAsset(
                    PillarPath,
                    $"Padded Cage Pillar {i + 1}",
                    parent,
                    new Vector3(position.x, 0f, position.z),
                    0f,
                    Vector3.one);
            }
        }

        private static void BuildDecor(Transform parent)
        {
            CreateNightclubAsset(
                CagePath,
                "West Practice Cage",
                parent,
                new Vector3(-7.5f, 0f, 7.5f),
                0f,
                Vector3.one);
            CreateNightclubAsset(
                CagePath,
                "East Practice Cage",
                parent,
                new Vector3(7.5f, 0f, -7.5f),
                180f,
                Vector3.one);

            Vector3[] bagPositions =
            {
                new Vector3(-7.5f, 0f, -7.5f),
                new Vector3(7.5f, 0f, 7.5f),
                new Vector3(-10f, 0f, -2.5f),
                new Vector3(10f, 0f, 2.5f)
            };
            float[] bagYaws = { 45f, 225f, 135f, 315f };
            for (int i = 0; i < bagPositions.Length; i++)
            {
                CreateNightclubAsset(
                    SportsBagPath,
                    $"Sports Bag {i + 1}",
                    parent,
                    bagPositions[i],
                    bagYaws[i],
                    Vector3.one);
            }

            CreateNightclubAsset(
                SpeakerPath,
                "North West Arena Speaker",
                parent,
                new Vector3(-10f, 0f, 7.5f),
                135f,
                Vector3.one);
            CreateNightclubAsset(
                SpeakerPath,
                "South East Arena Speaker",
                parent,
                new Vector3(10f, 0f, -7.5f),
                -45f,
                Vector3.one);

            Vector3[] lightPositions =
            {
                new Vector3(-7.5f, 3.5f, -5f),
                new Vector3(7.5f, 3.5f, -5f),
                new Vector3(-7.5f, 3.5f, 5f),
                new Vector3(7.5f, 3.5f, 5f)
            };
            for (int i = 0; i < lightPositions.Length; i++)
            {
                CreateNightclubAsset(
                    StageLightPath,
                    $"Cage Stage Light {i + 1}",
                    parent,
                    lightPositions[i],
                    i < 2 ? 0f : 180f,
                    Vector3.one);
            }
        }

        private static void BuildFloorMarking(Transform parent)
        {
            Material material = AssetDatabase.LoadAssetAtPath<Material>(
                AccentMaterialPath);
            Vector3[] centers =
            {
                new Vector3(2f, 0.025f, 2f),
                new Vector3(-2f, 0.025f, 2f),
                new Vector3(-2f, 0.025f, -2f),
                new Vector3(2f, 0.025f, -2f)
            };
            float[] yaws = { 45f, -45f, 45f, -45f };
            for (int i = 0; i < centers.Length; i++)
            {
                GameObject stripe = GameObject.CreatePrimitive(
                    PrimitiveType.Cube);
                stripe.name = $"Fight Diamond Stripe {i + 1}";
                stripe.transform.SetParent(parent, true);
                stripe.transform.SetPositionAndRotation(
                    centers[i],
                    Quaternion.Euler(0f, yaws[i], 0f));
                stripe.transform.localScale =
                    new Vector3(0.12f, 0.025f, 5.65f);
                UnityEngine.Object.DestroyImmediate(
                    stripe.GetComponent<Collider>());
                Renderer renderer = stripe.GetComponent<Renderer>();
                renderer.sharedMaterial = material;
                renderer.shadowCastingMode = ShadowCastingMode.Off;
                renderer.receiveShadows = false;
            }
        }

        private static GameObject CreateNightclubAsset(
            string prefabPath,
            string instanceName,
            Transform parent,
            Vector3 position,
            float yaw,
            Vector3 scale)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                prefabPath);
            GameObject instance = PrefabUtility.InstantiatePrefab(
                prefab,
                parent.gameObject.scene) as GameObject;
            if (instance == null)
            {
                throw new InvalidOperationException(
                    "Failed to instantiate nightclub prefab: " +
                    prefabPath);
            }

            instance.name = instanceName;
            instance.transform.SetParent(parent, true);
            instance.transform.SetPositionAndRotation(
                position,
                Quaternion.Euler(0f, yaw, 0f));
            instance.transform.localScale = scale;
            CharacterSceneSetup.DisableColliders(instance);
            return instance;
        }

        private static void CreateCollisionBlocker(
            string name,
            Vector3 position,
            Vector3 scale,
            Transform parent,
            bool blocksVision)
        {
            GameObject blocker = GameObject.CreatePrimitive(
                PrimitiveType.Cube);
            blocker.name = name;
            blocker.transform.SetParent(parent, true);
            blocker.transform.position = position;
            blocker.transform.localScale = scale;
            blocker.GetComponent<Renderer>().enabled = false;
            blocker.layer = blocksVision ? VisionObstacleLayer : 0;
        }

        private static void ConfigureCharacterVisuals(Scene scene)
        {
            ReplaceCharacterVisual(
                scene,
                GameObject.Find("Player"),
                PlayerCharacterPath,
                "Batting Cage Character - Player");

            Material chaserMaterial =
                AssetDatabase.LoadAssetAtPath<Material>(
                    ChaserMaterialPath);
            for (int i = 0; i < EnemyNames.Length; i++)
            {
                GameObject enemy = GameObject.Find(EnemyNames[i]);
                ReplaceCharacterVisual(
                    scene,
                    enemy,
                    EnemyCharacterPaths[i],
                    $"Batting Cage Character - Enemy {i + 1}");
                Transform ring = enemy.transform.Find("Combat Identity Ring");
                SceneValidation.Require(
                    ring != null,
                    "Batting-cage enemy is missing its identity ring: " +
                    enemy.name);
                Renderer renderer = ring.GetComponent<Renderer>();
                renderer.sharedMaterial = chaserMaterial;
                renderer.shadowCastingMode = ShadowCastingMode.Off;
                renderer.receiveShadows = false;
            }
        }

        private static void ReplaceCharacterVisual(
            Scene scene,
            GameObject owner,
            string prefabPath,
            string visualName)
        {
            SceneValidation.Require(
                owner != null,
                "Character owner is missing for " + visualName + ".");
            CharacterVisualController controller =
                owner.GetComponent<CharacterVisualController>();
            SceneValidation.Require(
                controller != null,
                owner.name + " is missing CharacterVisualController.");
            if (controller.VisualRoot != null)
            {
                UnityEngine.Object.DestroyImmediate(
                    controller.VisualRoot.gameObject);
            }

            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                prefabPath);
            GameObject visual = PrefabUtility.InstantiatePrefab(
                prefab,
                scene) as GameObject;
            SceneValidation.Require(
                visual != null,
                "Failed to instantiate character prefab: " + prefabPath);

            visual.name = visualName;
            visual.transform.SetParent(owner.transform, false);
            visual.transform.localPosition = new Vector3(0f, -1f, 0f);
            visual.transform.localRotation = Quaternion.identity;
            Vector3 ownerScale = owner.transform.localScale;
            visual.transform.localScale = new Vector3(
                1f / ownerScale.x,
                1f / ownerScale.y,
                1f / ownerScale.z);
            CharacterSceneSetup.DisableColliders(visual);

            Renderer proxy = owner.GetComponent<Renderer>();
            if (proxy != null)
            {
                proxy.shadowCastingMode = ShadowCastingMode.ShadowsOnly;
            }

            controller.Configure(visual.transform);
            SceneValidation.Require(
                CharacterAnimationEditorSetup.ConfigureCharacter(
                    owner,
                    visual),
                "Failed to configure melee animation for " + owner.name);
            owner.GetComponent<EnemyCombatant>()?.ConfigureVisual(controller);
            owner.GetComponent<EnemyHealth>()?.ConfigureVisual(controller);
            EditorUtility.SetDirty(controller);
        }

        private static void ConfigureLighting(Transform environment)
        {
            Transform lighting = CreateGroup("Cage Lighting", environment);
            CreatePointLight(
                "Amber North Light",
                lighting,
                new Vector3(0f, 4f, 6.5f),
                new Color(1f, 0.34f, 0.06f, 1f),
                3.2f,
                10f);
            CreatePointLight(
                "Cyan South Light",
                lighting,
                new Vector3(0f, 4f, -6.5f),
                new Color(0.02f, 0.72f, 1f, 1f),
                3.1f,
                10f);
            CreatePointLight(
                "Red West Light",
                lighting,
                new Vector3(-7.5f, 3.2f, 0f),
                new Color(1f, 0.06f, 0.04f, 1f),
                2.7f,
                8f);
            CreatePointLight(
                "Blue East Light",
                lighting,
                new Vector3(7.5f, 3.2f, 0f),
                new Color(0.08f, 0.25f, 1f, 1f),
                2.7f,
                8f);

            Light keyLight = GameObject.Find("Directional Key Light")
                ?.GetComponent<Light>();
            if (keyLight != null)
            {
                keyLight.color = new Color(0.72f, 0.78f, 1f, 1f);
                keyLight.intensity = 0.38f;
            }

            Camera camera = UnityEngine.Object.FindFirstObjectByType<Camera>();
            WorldTimeVisualFeedback feedback = camera == null
                ? null
                : camera.GetComponent<WorldTimeVisualFeedback>();
            SceneValidation.Require(
                feedback != null,
                "StageBattingCage requires WorldTimeVisualFeedback.");
            SerializedObject settings = new SerializedObject(feedback);
            settings.FindProperty("ambientSkyColor").colorValue =
                new Color(0.055f, 0.03f, 0.08f, 1f);
            settings.FindProperty("ambientEquatorColor").colorValue =
                new Color(0.02f, 0.055f, 0.075f, 1f);
            settings.FindProperty("ambientGroundColor").colorValue =
                new Color(0.008f, 0.012f, 0.025f, 1f);
            settings.FindProperty("ambientIntensity").floatValue = 0.9f;
            settings.FindProperty("reflectionIntensity").floatValue = 0.45f;
            settings.FindProperty("directionalLightIntensity").floatValue =
                0.38f;
            settings.FindProperty("fogColor").colorValue =
                new Color(0.018f, 0.008f, 0.025f, 1f);
            settings.FindProperty("fogStartDistance").floatValue = 24f;
            settings.FindProperty("fogEndDistance").floatValue = 50f;
            settings.FindProperty("mapFillLightColor").colorValue =
                new Color(0.16f, 0.34f, 0.72f, 1f);
            settings.FindProperty("mapFillLightIntensity").floatValue = 0.85f;
            settings.FindProperty("nearlyStoppedColor").colorValue =
                new Color(0.01f, 0.006f, 0.02f, 1f);
            settings.FindProperty("activeColor").colorValue =
                new Color(0.018f, 0.008f, 0.025f, 1f);
            settings.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(feedback);

            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor =
                new Color(0.055f, 0.03f, 0.08f, 1f);
            RenderSettings.ambientEquatorColor =
                new Color(0.02f, 0.055f, 0.075f, 1f);
            RenderSettings.ambientGroundColor =
                new Color(0.008f, 0.012f, 0.025f, 1f);
            RenderSettings.ambientIntensity = 0.9f;
            RenderSettings.reflectionIntensity = 0.45f;
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor =
                new Color(0.018f, 0.008f, 0.025f, 1f);
            RenderSettings.fogStartDistance = 24f;
            RenderSettings.fogEndDistance = 50f;
        }

        private static void CreatePointLight(
            string name,
            Transform parent,
            Vector3 position,
            Color color,
            float intensity,
            float range)
        {
            GameObject lightObject = new GameObject(name);
            lightObject.transform.SetParent(parent, true);
            lightObject.transform.position = position;
            Light light = lightObject.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = color;
            light.intensity = intensity;
            light.range = range;
            light.shadows = LightShadows.None;
            light.renderMode = LightRenderMode.ForcePixel;
        }

        private static void ConfigureCamera()
        {
            Camera camera = UnityEngine.Object.FindFirstObjectByType<Camera>();
            TopDownCameraController controller = camera == null
                ? null
                : camera.GetComponent<TopDownCameraController>();
            SceneValidation.Require(
                camera != null && controller != null,
                "StageBattingCage requires the Stage2 camera rig.");
            camera.fieldOfView = 58f;
            camera.backgroundColor =
                new Color(0.018f, 0.008f, 0.025f, 1f);
            controller.SnapToTarget();
            EditorUtility.SetDirty(camera);
            EditorUtility.SetDirty(controller);
        }

        private static void BuildNavigation()
        {
            NavMeshSurface surface =
                UnityEngine.Object.FindFirstObjectByType<NavMeshSurface>();
            SceneValidation.Require(
                surface != null,
                "StageBattingCage navigation surface is missing.");
            surface.collectObjects = CollectObjects.All;
            surface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
            surface.RemoveData();
            surface.navMeshData = null;
            NavigationSceneSetup.BuildNavMeshExcludingDynamicGameplayColliders(
                surface,
                surface.gameObject.scene);
            SceneValidation.Require(
                surface.navMeshData != null,
                "StageBattingCage NavMesh bake failed.");

            NavMeshData bakedData = surface.navMeshData;
            bakedData.name = "StageBattingCageNavigation";
            NavMeshData savedData =
                AssetDatabase.LoadAssetAtPath<NavMeshData>(NavigationPath);
            if (savedData == null)
            {
                AssetDatabase.CreateAsset(bakedData, NavigationPath);
            }
            else
            {
                surface.RemoveData();
                EditorUtility.CopySerialized(bakedData, savedData);
                surface.navMeshData = savedData;
                surface.AddData();
                UnityEngine.Object.DestroyImmediate(bakedData);
                savedData.name = "StageBattingCageNavigation";
                EditorUtility.SetDirty(savedData);
            }

            EditorUtility.SetDirty(surface);
            AssetDatabase.SaveAssets();
        }

        private static void ValidateScene(Scene scene)
        {
            GameObject environment = GameObject.Find(EnvironmentRootName);
            PlayerHealth player =
                UnityEngine.Object.FindFirstObjectByType<PlayerHealth>();
            DeadlineController deadline =
                UnityEngine.Object.FindFirstObjectByType<DeadlineController>();
            StageController stage =
                UnityEngine.Object.FindFirstObjectByType<StageController>();
            StageReplayController replay =
                UnityEngine.Object.FindFirstObjectByType<StageReplayController>();
            WorldTimeController worldTime =
                UnityEngine.Object.FindFirstObjectByType<WorldTimeController>();
            NavMeshSurface surface =
                UnityEngine.Object.FindFirstObjectByType<NavMeshSurface>();
            EnemyHealth[] enemies =
                UnityEngine.Object.FindObjectsByType<EnemyHealth>(
                    FindObjectsSortMode.None);
            EnemyChaser[] chasers =
                UnityEngine.Object.FindObjectsByType<EnemyChaser>(
                    FindObjectsSortMode.None);
            EnemyShooter[] shooters =
                UnityEngine.Object.FindObjectsByType<EnemyShooter>(
                    FindObjectsSortMode.None);
            WeaponPickup[] pickups =
                UnityEngine.Object.FindObjectsByType<WeaponPickup>(
                    FindObjectsSortMode.None);
            CharacterVisualController[] visuals =
                UnityEngine.Object.FindObjectsByType<CharacterVisualController>(
                    FindObjectsSortMode.None);
            WeaponDefinition melee = LoadMeleeDefinition();

            SceneValidation.Require(
                scene.path == ScenePath,
                "Unexpected StageBattingCage scene: " + scene.path);
            SceneValidation.Require(
                environment != null &&
                environment.GetComponent<ReplayExcluded>() != null,
                "StageBattingCage static environment is not replay-excluded.");
            SceneValidation.Require(
                player != null && deadline != null && stage != null &&
                replay != null && worldTime != null,
                "StageBattingCage gameplay roots are incomplete.");
            SceneValidation.Require(
                enemies.Length == EnemyCount &&
                chasers.Length == EnemyCount &&
                shooters.Length == 0,
                $"StageBattingCage enemies={enemies.Length}, " +
                $"chasers={chasers.Length}, shooters={shooters.Length}; " +
                "expected 6/6/0.");
            SceneValidation.Require(
                pickups.Length == 0,
                $"StageBattingCage has {pickups.Length} initial pickups.");
            SceneValidation.Require(
                visuals.Length == EnemyCount + 1,
                $"StageBattingCage has {visuals.Length} character visuals; " +
                "expected 7.");
            SceneValidation.Require(
                Mathf.Approximately(melee.Damage, 3) &&
                Mathf.Approximately(melee.UseInterval, 0.72f) &&
                Mathf.Approximately(melee.MeleeRange, 1.45f) &&
                Mathf.Approximately(melee.MeleeHalfAngle, 35f),
                "Shared melee weapon balance no longer matches the encounter.");

            ValidateStartingWeapons(player.gameObject, enemies, melee);
            ValidateDeadline(deadline);
            ValidateNavigation(surface, scene);
            ValidateEnvironment(environment);
            ValidateInitialSightStagger(player.transform);
            GameBuildSceneCatalog.Validate();
        }

        private static void ValidateStartingWeapons(
            GameObject player,
            EnemyHealth[] enemies,
            WeaponDefinition melee)
        {
            WeaponController playerWeapon =
                player.GetComponent<WeaponController>();
            SceneValidation.Require(
                playerWeapon != null &&
                playerWeapon.StartingDefinition == melee,
                "StageBattingCage player does not start with the bat.");
            for (int i = 0; i < enemies.Length; i++)
            {
                WeaponController weapon =
                    enemies[i].GetComponent<WeaponController>();
                EnemyMotor motor = enemies[i].GetComponent<EnemyMotor>();
                SceneValidation.Require(
                    weapon != null && weapon.StartingDefinition == melee,
                    enemies[i].name + " does not start with the bat.");
                SceneValidation.Require(
                    motor != null && Mathf.Approximately(motor.MoveSpeed, 4.8f),
                    enemies[i].name + " does not use melee move speed 4.8.");
            }
        }

        private static void ValidateDeadline(DeadlineController deadline)
        {
            SerializedObject settings = new SerializedObject(deadline);
            SerializedProperty charges =
                settings.FindProperty("maximumCharges");
            SceneValidation.Require(
                charges != null && charges.intValue == DeadlineCharges,
                "StageBattingCage must retain two DEADLINE charges.");
        }

        private static void ValidateNavigation(
            NavMeshSurface surface,
            Scene scene)
        {
            string navigationPath = surface == null ||
                                    surface.navMeshData == null
                ? string.Empty
                : AssetDatabase.GetAssetPath(surface.navMeshData);
            SceneValidation.Require(
                surface != null && navigationPath == NavigationPath,
                "StageBattingCage uses the wrong NavMesh data: " +
                navigationPath);
            NavMeshTriangulation triangulation =
                NavMesh.CalculateTriangulation();
            SceneValidation.Require(
                triangulation.vertices.Length > 0,
                "StageBattingCage NavMesh has no triangles.");
            NavigationSceneSetup.ValidateDynamicGameplayCoverage(
                scene,
                "StageBattingCage");
        }

        private static void ValidateEnvironment(GameObject environment)
        {
            int visionBlockers = 0;
            Collider[] colliders =
                environment.GetComponentsInChildren<Collider>(true);
            for (int i = 0; i < colliders.Length; i++)
            {
                if (colliders[i].enabled &&
                    colliders[i].gameObject.layer == VisionObstacleLayer)
                {
                    visionBlockers++;
                }
            }

            int syntyInstances = CountSyntyPrefabInstances(environment.scene);
            int lights = environment.GetComponentsInChildren<Light>(true).Length;
            SceneValidation.Require(
                visionBlockers == 7,
                $"StageBattingCage has {visionBlockers} structural " +
                "VisionObstacle colliders; expected 7.");
            SceneValidation.Require(
                syntyInstances >= 75,
                $"StageBattingCage has only {syntyInstances} Polygon " +
                "Nightclubs prefab instances.");
            SceneValidation.Require(
                lights == 4,
                $"StageBattingCage has {lights} environment point lights; " +
                "expected 4.");
            for (int i = 0; i < PillarPositions.Length; i++)
            {
                GameObject pillar = GameObject.Find(
                    $"Initial Sight Pillar {i + 1}");
                SceneValidation.Require(
                    pillar != null &&
                    pillar.layer == VisionObstacleLayer,
                    "Initial sight pillar is missing or on the wrong layer.");
            }
        }

        private static void ValidateInitialSightStagger(Transform player)
        {
            Physics.SyncTransforms();
            for (int i = 0; i < EnemyNames.Length; i++)
            {
                GameObject enemy = GameObject.Find(EnemyNames[i]);
                SceneValidation.Require(
                    enemy != null,
                    "Missing batting-cage enemy: " + EnemyNames[i]);
                Vector3 origin = enemy.transform.position + Vector3.up * 0.2f;
                Vector3 target = player.position + Vector3.up * 0.2f;
                Vector3 offset = target - origin;
                bool hitSomething = Physics.Raycast(
                    origin,
                    offset.normalized,
                    out RaycastHit hit,
                    offset.magnitude + 0.05f,
                    ~0,
                    QueryTriggerInteraction.Ignore);
                SceneValidation.Require(
                    hitSomething,
                    enemy.name + " has no initial sight-ray result.");
                bool blockedByStructure =
                    hit.collider.gameObject.layer == VisionObstacleLayer;
                bool expectedBlocked = i % 2 == 1;
                SceneValidation.Require(
                    blockedByStructure == expectedBlocked,
                    enemy.name + " initial sight stagger is incorrect. " +
                    $"blocked={blockedByStructure}, " +
                    $"expected={expectedBlocked}, hit={hit.collider.name}.");
            }
        }

        private static int CountSyntyPrefabInstances(Scene scene)
        {
            HashSet<int> instanceIds = new HashSet<int>();
            GameObject[] roots = scene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                Transform[] transforms =
                    roots[i].GetComponentsInChildren<Transform>(true);
                for (int j = 0; j < transforms.Length; j++)
                {
                    GameObject nearestRoot =
                        PrefabUtility.GetNearestPrefabInstanceRoot(
                            transforms[j].gameObject);
                    if (nearestRoot == null)
                    {
                        continue;
                    }

                    string path =
                        PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(
                            nearestRoot);
                    if (path.StartsWith(
                            "Assets/Synty/PolygonNightclubs/",
                            StringComparison.Ordinal))
                    {
                        instanceIds.Add(nearestRoot.GetInstanceID());
                    }
                }
            }

            return instanceIds.Count;
        }
    }
}
