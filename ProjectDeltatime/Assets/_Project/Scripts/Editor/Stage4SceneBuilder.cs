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
    /// Builds Stage 4 without rewriting the existing Stage 1, Stage 2, or
    /// Stage 3 content. It starts from Stage 2 only to retain its proven game
    /// systems, then replaces that industrial room with a rooftop lounge.
    /// </summary>
    public static class Stage4SceneBuilder
    {
        private const string Stage2ScenePath =
            "Assets/_Project/Scenes/Stage2.unity";
        private const string Stage4ScenePath =
            "Assets/_Project/Scenes/Stage4.unity";
        private const string Stage4NavigationPath =
            "Assets/_Project/Scenes/Stage4Navigation.asset";
        private const string PreviewAssetPath =
            "Assets/_Project/Art/Generated/Stage4Preview.png";
        private const string RooftopRootName =
            "Stage 4 - Last Call Rooftop";
        private const string SyntyRoot =
            "Assets/Synty/PolygonNightclubs/Prefabs";
        private const string CharacterRoot = SyntyRoot + "/Characters";
        private const string PropsRoot = SyntyRoot + "/Props";
        private const string ModularPropsRoot = PropsRoot + "/Modular";
        private const string BuildingsRoot = SyntyRoot + "/Buildings";
        private const int VisionObstacleLayer = 8;
        private const int DeadlineCharges = 2;

        private const string PlayerCharacterPath =
            "Assets/Synty/PolygonGeneric/Prefabs/Characters/" +
            "SM_Gen_Chr_Business_Male_01.prefab";
        private const string WestGunnerCharacterPath =
            CharacterRoot + "/SM_Chr_Bartender_Male_01.prefab";
        private const string NorthChaserCharacterPath =
            CharacterRoot + "/SM_Chr_Bouncer_Male_01.prefab";
        private const string EastGunnerCharacterPath =
            CharacterRoot + "/SM_Chr_Party_Female_02.prefab";
        private const string NorthGunnerCharacterPath =
            CharacterRoot + "/SM_Chr_Bartender_Female_01.prefab";
        private const string SouthChaserCharacterPath =
            CharacterRoot + "/SM_Chr_Party_Male_02.prefab";

        private const string FloorPath =
            BuildingsRoot + "/SM_Bld_Floor_Combined_01.prefab";
        private const string RailingPath =
            PropsRoot + "/SM_Prop_Railing_01.prefab";
        private const string BarPath =
            ModularPropsRoot + "/SM_Prop_Bar_01.prefab";
        private const string SofaPath =
            ModularPropsRoot + "/SM_Prop_Sofa_02.prefab";
        private const string TablePath =
            PropsRoot + "/SM_Prop_Table_04.prefab";
        private const string ChairPath =
            PropsRoot + "/SM_Prop_Chair_04.prefab";
        private const string UmbrellaPath =
            PropsRoot + "/SM_Prop_Umbrella_01.prefab";
        private const string PlanterPath =
            PropsRoot + "/SM_Prop_Planter_03.prefab";
        private const string FirePitPath =
            PropsRoot + "/SM_Prop_Fire_Pit_01.prefab";
        private const string SignPath =
            PropsRoot + "/SM_Prop_Sign_Bar_01.prefab";
        private const string StringLightPath =
            PropsRoot + "/SM_Prop_Light_String_01.prefab";

        private const string AccentMaterialPath =
            "Assets/_Project/Materials/PrototypeAccent3D.mat";
        private const string EnemyMaterialPath =
            "Assets/_Project/Materials/PrototypeEnemy3D.mat";
        private const string ChaserMaterialPath =
            "Assets/_Project/Materials/PrototypeChaser3D.mat";

        [MenuItem("Tools/Prototype/Build Stage 4 - Last Call Rooftop")]
        public static void BuildStage4()
        {
            RequireSourceScene();
            RequireRooftopAssets();

            Scene sourceScene = EditorSceneManager.OpenScene(
                Stage2ScenePath,
                OpenSceneMode.Single);
            if (!EditorSceneManager.SaveScene(sourceScene, Stage4ScenePath, true))
            {
                throw new InvalidOperationException(
                    $"Failed to copy {Stage2ScenePath} to {Stage4ScenePath}.");
            }

            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            Scene scene = EditorSceneManager.OpenScene(
                Stage4ScenePath,
                OpenSceneMode.Single);

            RemoveStage2Environment();
            EnsureFiveEnemyEncounter(scene);
            RepositionGameplayObjects();
            GameObject rooftopRoot = BuildRooftopEnvironment(scene);
            AttachRooftopCharacters(scene);
            ConfigureRooftopLighting(rooftopRoot.transform);
            ConfigureRooftopCamera();
            BuildStage4Navigation();

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, Stage4ScenePath))
            {
                throw new InvalidOperationException(
                    $"Failed to save {Stage4ScenePath}.");
            }

            AddStage4ToBuildSettings();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            ValidateStage4Scene(scene);

            if (!Application.isBatchMode)
            {
                Selection.activeGameObject = GameObject.Find("Player");
            }

            Debug.Log("Stage4 Last Call Rooftop built and validated successfully.");
        }

        public static void BuildAndValidateFromCommandLine()
        {
            SceneBuildCommand.Run(BuildStage4);
        }

        [MenuItem("Tools/Prototype/Validate Stage 4 - Last Call Rooftop")]
        public static void ValidateSavedStage4()
        {
            Scene scene = EditorSceneManager.OpenScene(
                Stage4ScenePath,
                OpenSceneMode.Single);
            ValidateStage4Scene(scene);
            Debug.Log("Stage4 Last Call Rooftop validation passed.");
        }

        public static void CapturePreviewFromCommandLine()
        {
            Scene scene = EditorSceneManager.OpenScene(
                Stage4ScenePath,
                OpenSceneMode.Single);
            ValidateStage4Scene(scene);

            Camera camera = UnityEngine.Object.FindFirstObjectByType<Camera>();
            if (camera == null)
            {
                throw new InvalidOperationException(
                    "Stage4 preview requires a camera.");
            }

            const int width = 1280;
            const int height = 720;
            RenderTexture target = new RenderTexture(width, height, 24);
            Texture2D preview = new Texture2D(
                width,
                height,
                TextureFormat.RGB24,
                false);
            RenderTexture previousActive = RenderTexture.active;
            RenderTexture previousTarget = camera.targetTexture;
            Vector3 previousPosition = camera.transform.position;
            Quaternion previousRotation = camera.transform.rotation;
            float previousFieldOfView = camera.fieldOfView;

            try
            {
                camera.transform.position = new Vector3(0f, 18.5f, -19f);
                camera.transform.LookAt(new Vector3(0f, 0.45f, 0.5f), Vector3.up);
                camera.fieldOfView = 58f;
                camera.targetTexture = target;
                RenderTexture.active = target;
                camera.Render();
                preview.ReadPixels(new Rect(0f, 0f, width, height), 0, 0);
                preview.Apply();

                string previewPath = Path.Combine(
                    Application.dataPath,
                    "_Project",
                    "Art",
                    "Generated",
                    "Stage4Preview.png");
                Directory.CreateDirectory(Path.GetDirectoryName(previewPath));
                File.WriteAllBytes(previewPath, preview.EncodeToPNG());
                AssetDatabase.ImportAsset(
                    PreviewAssetPath,
                    ImportAssetOptions.ForceSynchronousImport);
                Debug.Log($"Stage4 preview captured at {previewPath}.");
            }
            finally
            {
                camera.targetTexture = previousTarget;
                camera.transform.SetPositionAndRotation(
                    previousPosition,
                    previousRotation);
                camera.fieldOfView = previousFieldOfView;
                RenderTexture.active = previousActive;
                UnityEngine.Object.DestroyImmediate(preview);
                UnityEngine.Object.DestroyImmediate(target);
            }
        }

        private static void RequireSourceScene()
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(Stage2ScenePath) == null)
            {
                throw new InvalidOperationException(
                    $"Stage4 requires {Stage2ScenePath} as its gameplay-system source.");
            }
        }

        private static void RequireRooftopAssets()
        {
            string[] requiredPaths =
            {
                PlayerCharacterPath,
                WestGunnerCharacterPath,
                NorthChaserCharacterPath,
                EastGunnerCharacterPath,
                NorthGunnerCharacterPath,
                SouthChaserCharacterPath,
                FloorPath,
                RailingPath,
                BarPath,
                SofaPath,
                TablePath,
                ChairPath,
                UmbrellaPath,
                PlanterPath,
                FirePitPath,
                SignPath,
                StringLightPath
            };

            for (int i = 0; i < requiredPaths.Length; i++)
            {
                if (AssetDatabase.LoadAssetAtPath<GameObject>(requiredPaths[i]) == null)
                {
                    throw new InvalidOperationException(
                    $"Required Polygon Nightclubs prefab is missing: {requiredPaths[i]}");
                }
            }

        }

        private static void RemoveStage2Environment()
        {
            DestroySceneObject("Industrial Room");
            DestroySceneObject("Blue Bay Light");
            DestroySceneObject("Red Alert Light");
        }

        private static void DestroySceneObject(string name)
        {
            GameObject target = GameObject.Find(name);
            if (target != null)
            {
                UnityEngine.Object.DestroyImmediate(target);
            }
        }

        private static void EnsureFiveEnemyEncounter(Scene scene)
        {
            CloneEnemy(scene, "Enemy East", "Enemy North Gunner");
            CloneEnemy(scene, "Enemy Center", "Enemy South Chaser");
        }

        private static void CloneEnemy(Scene scene, string sourceName, string cloneName)
        {
            GameObject source = GameObject.Find(sourceName);
            if (source == null)
            {
                throw new InvalidOperationException(
                    $"Stage4 source enemy is missing: {sourceName}");
            }

            GameObject clone = UnityEngine.Object.Instantiate(
                source,
                source.transform.position,
                source.transform.rotation);
            clone.name = cloneName;
            SceneManager.MoveGameObjectToScene(clone, scene);
        }

        private static void RepositionGameplayObjects()
        {
            SetPose("Player", new Vector3(0f, 0.75f, -7.6f), 0f);
            SetPose("Enemy West", new Vector3(-8f, 0.75f, 3.7f), 180f);
            SetPose("Enemy Center", new Vector3(0.4f, 0.78f, 5.5f), 180f);
            SetPose("Enemy East", new Vector3(8f, 0.75f, 3.4f), 180f);
            SetPose("Enemy North Gunner", new Vector3(-1.8f, 0.75f, 7.3f), 180f);
            SetPose("Enemy South Chaser", new Vector3(4.6f, 0.78f, -2.8f), 180f);
            SetPose("Pistol Pickup", new Vector3(-3.8f, 0.18f, -6.25f), 28f);
            SetPose("Shotgun Pickup", new Vector3(3.8f, 0.18f, -6.25f), -28f);
        }

        private static void SetPose(string name, Vector3 position, float yaw)
        {
            GameObject target = GameObject.Find(name);
            if (target == null)
            {
                throw new InvalidOperationException(
                    $"Stage4 source object is missing: {name}");
            }

            target.transform.SetPositionAndRotation(
                position,
                Quaternion.Euler(0f, yaw, 0f));
        }

        private static GameObject BuildRooftopEnvironment(Scene scene)
        {
            GameObject root = new GameObject(RooftopRootName);
            SceneManager.MoveGameObjectToScene(root, scene);
            root.AddComponent<ReplayExcluded>();
            Transform architecture = CreateGroup("Rooftop Architecture", root.transform);
            Transform cover = CreateGroup("Gameplay Cover", root.transform);
            Transform decor = CreateGroup("Rooftop Decor", root.transform);

            CreateCollisionBlocker(
                "Rooftop Floor Collision",
                new Vector3(0f, -0.12f, 0f),
                new Vector3(24f, 0.24f, 20f),
                root.transform,
                false);
            CreateCollisionBlocker(
                "North Safety Rail Collision",
                new Vector3(0f, 1.35f, 10f),
                new Vector3(24.5f, 2.7f, 0.45f),
                root.transform,
                true);
            CreateCollisionBlocker(
                "South Safety Rail Collision",
                new Vector3(0f, 1.35f, -10f),
                new Vector3(24.5f, 2.7f, 0.45f),
                root.transform,
                true);
            CreateCollisionBlocker(
                "West Safety Rail Collision",
                new Vector3(-12f, 1.35f, 0f),
                new Vector3(0.45f, 2.7f, 20.5f),
                root.transform,
                true);
            CreateCollisionBlocker(
                "East Safety Rail Collision",
                new Vector3(12f, 1.35f, 0f),
                new Vector3(0.45f, 2.7f, 20.5f),
                root.transform,
                true);

            BuildFloorTiles(architecture);
            BuildSafetyRails(architecture);
            BuildWestServingLane(cover, decor);
            BuildEastLounge(cover, decor);
            BuildNorthBar(cover, decor);
            BuildCentralTerrace(cover, decor);
            BuildRooftopDecor(decor);
            return root;
        }

        private static Transform CreateGroup(string name, Transform parent)
        {
            GameObject group = new GameObject(name);
            group.transform.SetParent(parent, false);
            return group.transform;
        }

        private static void BuildFloorTiles(Transform parent)
        {
            int tileNumber = 1;
            for (int x = -3; x <= 3; x++)
            {
                for (int z = -3; z <= 3; z++)
                {
                    CreateRooftopAsset(
                        FloorPath,
                        $"Rooftop Asset - Floor {tileNumber++}",
                        parent,
                        new Vector3(x * 3f, 0f, z * 3f),
                        0f,
                        Vector3.one);
                }
            }
        }

        private static void BuildSafetyRails(Transform parent)
        {
            for (int x = -9; x <= 9; x += 3)
            {
                CreateRooftopAsset(
                    RailingPath,
                    $"Rooftop Asset - North Rail {x}",
                    parent,
                    new Vector3(x, 0f, 9.7f),
                    180f,
                    Vector3.one);
                CreateRooftopAsset(
                    RailingPath,
                    $"Rooftop Asset - South Rail {x}",
                    parent,
                    new Vector3(x, 0f, -9.7f),
                    0f,
                    Vector3.one);
            }

            for (int z = -7; z <= 7; z += 3)
            {
                CreateRooftopAsset(
                    RailingPath,
                    $"Rooftop Asset - West Rail {z}",
                    parent,
                    new Vector3(-10.8f, 0f, z),
                    90f,
                    Vector3.one);
                CreateRooftopAsset(
                    RailingPath,
                    $"Rooftop Asset - East Rail {z}",
                    parent,
                    new Vector3(10.8f, 0f, z),
                    -90f,
                    Vector3.one);
            }
        }

        private static void BuildWestServingLane(Transform cover, Transform decor)
        {
            CreateCollisionBlocker(
                "West Serving Counter Cover",
                new Vector3(-7.4f, 0.68f, 1.1f),
                new Vector3(1.5f, 1.36f, 6.8f),
                cover,
                true);

            for (int i = 0; i < 3; i++)
            {
                CreateRooftopAsset(
                    BarPath,
                    $"Rooftop Asset - West Counter {i + 1}",
                    decor,
                    new Vector3(-7.4f, 0f, -1.55f + i * 2.65f),
                    90f,
                    Vector3.one);
            }

            CreateRooftopAsset(
                SignPath,
                "Rooftop Asset - West Bar Sign",
                decor,
                new Vector3(-8.6f, 2.2f, 1.15f),
                90f,
                Vector3.one);
        }

        private static void BuildEastLounge(Transform cover, Transform decor)
        {
            CreateCollisionBlocker(
                "East Lounge Cover North",
                new Vector3(7.15f, 0.55f, 1.8f),
                new Vector3(3.65f, 1.1f, 1.4f),
                cover,
                true);
            CreateCollisionBlocker(
                "East Lounge Cover South",
                new Vector3(6.5f, 0.55f, -3.2f),
                new Vector3(3.7f, 1.1f, 1.35f),
                cover,
                true);

            CreateRooftopAsset(
                SofaPath,
                "Rooftop Asset - East Lounge North",
                decor,
                new Vector3(7.15f, 0f, 1.8f),
                180f,
                Vector3.one);
            CreateRooftopAsset(
                SofaPath,
                "Rooftop Asset - East Lounge South",
                decor,
                new Vector3(6.5f, 0f, -3.2f),
                0f,
                Vector3.one);
            CreateRooftopAsset(
                TablePath,
                "Rooftop Asset - East Lounge Table",
                decor,
                new Vector3(7.7f, 0f, -0.8f),
                0f,
                Vector3.one);
            CreateRooftopAsset(
                ChairPath,
                "Rooftop Asset - East Lounge Chair",
                decor,
                new Vector3(9.1f, 0f, -1.35f),
                -45f,
                Vector3.one);
        }

        private static void BuildNorthBar(Transform cover, Transform decor)
        {
            CreateCollisionBlocker(
                "North Bar Cover",
                new Vector3(0f, 0.68f, 7.9f),
                new Vector3(6.6f, 1.36f, 1.45f),
                cover,
                true);

            for (int i = 0; i < 3; i++)
            {
                CreateRooftopAsset(
                    BarPath,
                    $"Rooftop Asset - North Counter {i + 1}",
                    decor,
                    new Vector3(-2.6f + i * 2.6f, 0f, 7.9f),
                    180f,
                    Vector3.one);
            }

            CreateRooftopAsset(
                SignPath,
                "Rooftop Asset - North Bar Sign",
                decor,
                new Vector3(0f, 2.15f, 8.85f),
                180f,
                Vector3.one);
        }

        private static void BuildCentralTerrace(Transform cover, Transform decor)
        {
            Vector3[] tablePositions =
            {
                new Vector3(-2.6f, 0f, 1.4f),
                new Vector3(2.2f, 0f, 1.5f),
                new Vector3(-2.3f, 0f, -3.1f)
            };

            for (int i = 0; i < tablePositions.Length; i++)
            {
                Vector3 position = tablePositions[i];
                CreateCollisionBlocker(
                    $"Terrace Table Cover {i + 1}",
                    new Vector3(position.x, 0.52f, position.z),
                    new Vector3(1.4f, 1.04f, 1.4f),
                    cover,
                    true);
                CreateRooftopAsset(
                    TablePath,
                    $"Rooftop Asset - Terrace Table {i + 1}",
                    decor,
                    position,
                    0f,
                    Vector3.one);
                CreateRooftopAsset(
                    UmbrellaPath,
                    $"Rooftop Asset - Terrace Umbrella {i + 1}",
                    decor,
                    position,
                    i == 1 ? 45f : 0f,
                    Vector3.one);
            }

            CreateCollisionBlocker(
                "Southwest Planter Cover",
                new Vector3(-6.8f, 0.72f, -5.5f),
                new Vector3(2.5f, 1.44f, 1.1f),
                cover,
                true);
            CreateCollisionBlocker(
                "Southeast Planter Cover",
                new Vector3(6.8f, 0.72f, -5.5f),
                new Vector3(2.5f, 1.44f, 1.1f),
                cover,
                true);
            CreateRooftopAsset(
                PlanterPath,
                "Rooftop Asset - Southwest Planter",
                decor,
                new Vector3(-6.8f, 0f, -5.5f),
                0f,
                Vector3.one);
            CreateRooftopAsset(
                PlanterPath,
                "Rooftop Asset - Southeast Planter",
                decor,
                new Vector3(6.8f, 0f, -5.5f),
                180f,
                Vector3.one);
        }

        private static void BuildRooftopDecor(Transform decor)
        {
            CreateRooftopAsset(
                FirePitPath,
                "Rooftop Asset - Fire Pit",
                decor,
                new Vector3(0f, 0f, -5.6f),
                0f,
                Vector3.one);
            CreateRooftopAsset(
                StringLightPath,
                "Rooftop Asset - West String Lights",
                decor,
                new Vector3(-4.5f, 4.2f, 0f),
                90f,
                Vector3.one);
            CreateRooftopAsset(
                StringLightPath,
                "Rooftop Asset - East String Lights",
                decor,
                new Vector3(4.5f, 4.2f, 0f),
                -90f,
                Vector3.one);
        }

        private static GameObject CreateRooftopAsset(
            string prefabPath,
            string instanceName,
            Transform parent,
            Vector3 position,
            float yaw,
            Vector3 scale)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            GameObject instance = PrefabUtility.InstantiatePrefab(
                prefab,
                parent.gameObject.scene) as GameObject;
            if (instance == null)
            {
                throw new InvalidOperationException(
                    $"Failed to instantiate rooftop prefab: {prefabPath}");
            }

            instance.name = instanceName;
            instance.transform.SetParent(parent, true);
            instance.transform.SetPositionAndRotation(
                position,
                Quaternion.Euler(0f, yaw, 0f));
            instance.transform.localScale = scale;
            DisableColliders(instance);
            return instance;
        }

        private static void DisableColliders(GameObject root)
        {
            CharacterSceneSetup.DisableColliders(root);
        }

        private static void CreateCollisionBlocker(
            string name,
            Vector3 position,
            Vector3 scale,
            Transform parent,
            bool blocksVision)
        {
            GameObject blocker = GameObject.CreatePrimitive(PrimitiveType.Cube);
            blocker.name = name;
            blocker.transform.SetParent(parent, true);
            blocker.transform.position = position;
            blocker.transform.localScale = scale;
            blocker.GetComponent<Renderer>().enabled = false;
            blocker.layer = blocksVision ? VisionObstacleLayer : 0;
        }

        private static void AttachRooftopCharacters(Scene scene)
        {
            AttachCharacter(scene, "Player", PlayerCharacterPath, "Rooftop Character - Player");
            AttachCharacter(scene, "Enemy West", WestGunnerCharacterPath, "Rooftop Character - West Gunner");
            AttachCharacter(scene, "Enemy Center", NorthChaserCharacterPath, "Rooftop Character - North Chaser");
            AttachCharacter(scene, "Enemy East", EastGunnerCharacterPath, "Rooftop Character - East Gunner");
            AttachCharacter(scene, "Enemy North Gunner", NorthGunnerCharacterPath, "Rooftop Character - North Gunner");
            AttachCharacter(scene, "Enemy South Chaser", SouthChaserCharacterPath, "Rooftop Character - South Chaser");

            Material playerRing = AssetDatabase.LoadAssetAtPath<Material>(AccentMaterialPath);
            Material rangedRing = AssetDatabase.LoadAssetAtPath<Material>(EnemyMaterialPath);
            Material chaserRing = AssetDatabase.LoadAssetAtPath<Material>(ChaserMaterialPath);
            CreateIdentityRing(GameObject.Find("Player").transform, playerRing);
            CreateIdentityRing(GameObject.Find("Enemy West").transform, rangedRing);
            CreateIdentityRing(GameObject.Find("Enemy Center").transform, chaserRing);
            CreateIdentityRing(GameObject.Find("Enemy East").transform, rangedRing);
            CreateIdentityRing(GameObject.Find("Enemy North Gunner").transform, rangedRing);
            CreateIdentityRing(GameObject.Find("Enemy South Chaser").transform, chaserRing);
        }

        private static void AttachCharacter(
            Scene scene,
            string ownerName,
            string prefabPath,
            string visualName)
        {
            GameObject owner = GameObject.Find(ownerName);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            GameObject visual = PrefabUtility.InstantiatePrefab(prefab, scene) as GameObject;
            if (owner == null || visual == null)
            {
                throw new InvalidOperationException(
                    $"Failed to attach {prefabPath} to {ownerName}.");
            }

            Renderer proxyRenderer = owner.GetComponent<Renderer>();
            if (proxyRenderer != null)
            {
                proxyRenderer.shadowCastingMode = ShadowCastingMode.ShadowsOnly;
            }

            visual.name = visualName;
            visual.transform.SetParent(owner.transform, false);
            visual.transform.localPosition = new Vector3(0f, -1f, 0f);
            visual.transform.localRotation = Quaternion.identity;
            Vector3 ownerScale = owner.transform.localScale;
            visual.transform.localScale = new Vector3(
                1f / ownerScale.x,
                1f / ownerScale.y,
                1f / ownerScale.z);
            DisableColliders(visual);

            if (!CharacterAnimationEditorSetup.ConfigureCharacter(
                    owner,
                    visual))
            {
                ApplyRelaxedArmPose(visual);
            }
            CharacterVisualController visualController =
                owner.GetComponent<CharacterVisualController>();
            if (visualController == null)
            {
                visualController = owner.AddComponent<CharacterVisualController>();
            }

            visualController.Configure(visual.transform);
            owner.GetComponent<EnemyCombatant>()?.ConfigureVisual(visualController);
            owner.GetComponent<EnemyHealth>()?.ConfigureVisual(visualController);
            owner.GetComponent<PlayerHealth>()?.ConfigureVisual(visualController);
        }

        private static void ApplyRelaxedArmPose(GameObject visual)
        {
            Transform[] transforms = visual.GetComponentsInChildren<Transform>(true);
            Transform leftShoulder = null;
            Transform rightShoulder = null;
            Transform leftElbow = null;
            Transform rightElbow = null;
            for (int i = 0; i < transforms.Length; i++)
            {
                if (transforms[i].name == "Shoulder_L")
                {
                    leftShoulder = transforms[i];
                }
                else if (transforms[i].name == "Shoulder_R")
                {
                    rightShoulder = transforms[i];
                }
                else if (transforms[i].name == "Elbow_L")
                {
                    leftElbow = transforms[i];
                }
                else if (transforms[i].name == "Elbow_R")
                {
                    rightElbow = transforms[i];
                }
            }

            if (leftShoulder == null || rightShoulder == null ||
                leftElbow == null || rightElbow == null)
            {
                throw new InvalidOperationException(
                    $"Rooftop character '{visual.name}' is missing arm bones.");
            }

            Vector3 relaxedDirection =
                (Vector3.down + visual.transform.forward * 0.12f).normalized;
            RotateBoneToward(leftShoulder, leftElbow, relaxedDirection);
            RotateBoneToward(rightShoulder, rightElbow, relaxedDirection);
        }

        private static void RotateBoneToward(
            Transform bone,
            Transform child,
            Vector3 targetDirection)
        {
            Vector3 currentDirection = child.position - bone.position;
            if (currentDirection.sqrMagnitude <= 0.000001f)
            {
                return;
            }

            Quaternion correction = Quaternion.FromToRotation(
                currentDirection.normalized,
                targetDirection);
            bone.rotation = correction * bone.rotation;
        }

        private static void CreateIdentityRing(Transform owner, Material material)
        {
            GameObject ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            ring.name = "Combat Identity Ring";
            ring.transform.position = new Vector3(owner.position.x, 0.025f, owner.position.z);
            ring.transform.localScale = new Vector3(0.72f, 0.025f, 0.72f);
            UnityEngine.Object.DestroyImmediate(ring.GetComponent<Collider>());
            Renderer renderer = ring.GetComponent<Renderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            ring.transform.SetParent(owner, true);
        }

        private static void ConfigureRooftopLighting(Transform rooftopRoot)
        {
            Transform lighting = CreateGroup("Rooftop Light Rig", rooftopRoot);
            CreatePointLight(
                "Amber North Bar Light",
                lighting,
                new Vector3(0f, 3.2f, 7.2f),
                new Color(1f, 0.32f, 0.06f, 1f),
                3.2f,
                8f);
            CreatePointLight(
                "Cyan East Lounge Light",
                lighting,
                new Vector3(7.1f, 3.1f, -0.8f),
                new Color(0.02f, 0.68f, 1f, 1f),
                3.1f,
                8f);
            CreatePointLight(
                "Magenta West Counter Light",
                lighting,
                new Vector3(-7.1f, 3.1f, 1.2f),
                new Color(1f, 0.05f, 0.45f, 1f),
                3.1f,
                8f);
            CreatePointLight(
                "Moonlight Terrace Light",
                lighting,
                new Vector3(0f, 4.2f, -2.4f),
                new Color(0.33f, 0.48f, 1f, 1f),
                2.7f,
                10f);

            Light keyLight = GameObject.Find("Directional Key Light")?.GetComponent<Light>();
            if (keyLight != null)
            {
                keyLight.color = new Color(0.5f, 0.63f, 1f, 1f);
                keyLight.intensity = 0.5f;
            }

            Camera camera = UnityEngine.Object.FindFirstObjectByType<Camera>();
            WorldTimeVisualFeedback feedback = camera == null
                ? null
                : camera.GetComponent<WorldTimeVisualFeedback>();
            if (feedback == null)
            {
                throw new InvalidOperationException(
                    "Stage4 requires WorldTimeVisualFeedback on its camera.");
            }

            SerializedObject settings = new SerializedObject(feedback);
            settings.FindProperty("ambientSkyColor").colorValue =
                new Color(0.055f, 0.075f, 0.16f, 1f);
            settings.FindProperty("ambientEquatorColor").colorValue =
                new Color(0.018f, 0.035f, 0.075f, 1f);
            settings.FindProperty("ambientGroundColor").colorValue =
                new Color(0.006f, 0.01f, 0.025f, 1f);
            settings.FindProperty("ambientIntensity").floatValue = 0.9f;
            settings.FindProperty("reflectionIntensity").floatValue = 0.45f;
            settings.FindProperty("directionalLightIntensity").floatValue = 0.5f;
            settings.FindProperty("fogColor").colorValue =
                new Color(0.012f, 0.018f, 0.05f, 1f);
            settings.FindProperty("fogStartDistance").floatValue = 30f;
            settings.FindProperty("fogEndDistance").floatValue = 65f;
            settings.FindProperty("mapFillLightColor").colorValue =
                new Color(0.28f, 0.42f, 1f, 1f);
            settings.FindProperty("mapFillLightIntensity").floatValue = 0.8f;
            settings.FindProperty("nearlyStoppedColor").colorValue =
                new Color(0.006f, 0.009f, 0.025f, 1f);
            settings.FindProperty("activeColor").colorValue =
                new Color(0.012f, 0.018f, 0.05f, 1f);
            settings.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(feedback);

            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.055f, 0.075f, 0.16f, 1f);
            RenderSettings.ambientEquatorColor = new Color(0.018f, 0.035f, 0.075f, 1f);
            RenderSettings.ambientGroundColor = new Color(0.006f, 0.01f, 0.025f, 1f);
            RenderSettings.ambientIntensity = 0.9f;
            RenderSettings.reflectionIntensity = 0.45f;
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = new Color(0.012f, 0.018f, 0.05f, 1f);
            RenderSettings.fogStartDistance = 30f;
            RenderSettings.fogEndDistance = 65f;
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

        private static void ConfigureRooftopCamera()
        {
            Camera camera = UnityEngine.Object.FindFirstObjectByType<Camera>();
            TopDownCameraController controller = camera == null
                ? null
                : camera.GetComponent<TopDownCameraController>();
            if (camera == null || controller == null)
            {
                throw new InvalidOperationException(
                    "Stage4 requires the existing gameplay camera rig.");
            }

            camera.fieldOfView = 56f;
            camera.backgroundColor = new Color(0.012f, 0.018f, 0.05f, 1f);
            controller.SnapToTarget();
            EditorUtility.SetDirty(camera);
            EditorUtility.SetDirty(controller);
        }

        private static void BuildStage4Navigation()
        {
            NavMeshSurface surface =
                UnityEngine.Object.FindFirstObjectByType<NavMeshSurface>();
            if (surface == null)
            {
                throw new InvalidOperationException("Stage4 navigation surface is missing.");
            }

            surface.RemoveData();
            surface.navMeshData = null;
            Physics.SyncTransforms();
            surface.BuildNavMesh();
            if (surface.navMeshData == null)
            {
                throw new InvalidOperationException("Stage4 navigation bake failed.");
            }

            NavMeshData bakedData = surface.navMeshData;
            bakedData.name = "Stage4Navigation";
            NavMeshData savedData = AssetDatabase.LoadAssetAtPath<NavMeshData>(
                Stage4NavigationPath);
            if (savedData == null)
            {
                AssetDatabase.CreateAsset(bakedData, Stage4NavigationPath);
            }
            else
            {
                surface.RemoveData();
                EditorUtility.CopySerialized(bakedData, savedData);
                surface.navMeshData = savedData;
                surface.AddData();
                UnityEngine.Object.DestroyImmediate(bakedData);
                savedData.name = "Stage4Navigation";
                EditorUtility.SetDirty(savedData);
            }

            EditorUtility.SetDirty(surface);
            AssetDatabase.SaveAssets();
        }

        private static void AddStage4ToBuildSettings()
        {
            GameBuildSceneCatalog.Apply();
        }

        private static void AddBuildSceneIfPresent(
            List<EditorBuildSettingsScene> scenes,
            string path)
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(path) != null)
            {
                scenes.Add(new EditorBuildSettingsScene(path, true));
            }
        }

        private static void ValidateStage4Scene(Scene scene)
        {
            GameObject rooftopRoot = GameObject.Find(RooftopRootName);
            PlayerHealth player = UnityEngine.Object.FindFirstObjectByType<PlayerHealth>();
            DeadlineController deadline = UnityEngine.Object.FindFirstObjectByType<DeadlineController>();
            StageController stage = UnityEngine.Object.FindFirstObjectByType<StageController>();
            StageReplayController replay = UnityEngine.Object.FindFirstObjectByType<StageReplayController>();
            NavMeshSurface surface = UnityEngine.Object.FindFirstObjectByType<NavMeshSurface>();
            int enemyCount = UnityEngine.Object.FindObjectsByType<EnemyHealth>(
                FindObjectsSortMode.None).Length;
            int rangedCount = UnityEngine.Object.FindObjectsByType<EnemyShooter>(
                FindObjectsSortMode.None).Length;
            int chaserCount = UnityEngine.Object.FindObjectsByType<EnemyChaser>(
                FindObjectsSortMode.None).Length;
            int pickupCount = UnityEngine.Object.FindObjectsByType<WeaponPickup>(
                FindObjectsSortMode.None).Length;
            int visualControllerCount =
                UnityEngine.Object.FindObjectsByType<CharacterVisualController>(
                    FindObjectsSortMode.None).Length;
            int syntyPrefabInstances = CountSyntyPrefabInstances(scene);
            int rooftopLights = rooftopRoot == null
                ? 0
                : rooftopRoot.GetComponentsInChildren<Light>(true).Length;
            int visionBlockerCount = rooftopRoot == null
                ? 0
                : CountObjectsOnLayer(rooftopRoot.transform, VisionObstacleLayer);

            SerializedObject deadlineSettings = deadline == null
                ? null
                : new SerializedObject(deadline);
            deadlineSettings?.Update();
            SerializedProperty charges = deadlineSettings == null
                ? null
                : deadlineSettings.FindProperty("maximumCharges");
            string navigationPath = surface == null || surface.navMeshData == null
                ? string.Empty
                : AssetDatabase.GetAssetPath(surface.navMeshData);
            NavMeshTriangulation triangulation = NavMesh.CalculateTriangulation();

            if (scene.path != Stage4ScenePath ||
                rooftopRoot == null ||
                rooftopRoot.GetComponent<ReplayExcluded>() == null ||
                player == null ||
                deadline == null ||
                stage == null ||
                replay == null ||
                enemyCount != 5 ||
                rangedCount != 3 ||
                chaserCount != 2 ||
                pickupCount != 2 ||
                charges == null ||
                charges.intValue != DeadlineCharges ||
                surface == null ||
                navigationPath != Stage4NavigationPath ||
                triangulation.vertices.Length == 0 ||
                visualControllerCount != 6 ||
                syntyPrefabInstances < 70 ||
                rooftopLights != 4 ||
                visionBlockerCount < 12)
            {
                throw new InvalidOperationException(
                    "Stage4 validation failed: " +
                    $"scene={scene.path}, rooftopRoot={rooftopRoot != null}, " +
                    $"player={player != null}, deadline={deadline != null}, " +
                    $"stage={stage != null}, replay={replay != null}, " +
                    $"enemies={enemyCount}, ranged={rangedCount}, chasers={chaserCount}, " +
                    $"pickups={pickupCount}, charges={charges?.intValue}, " +
                    $"navPath={navigationPath}, navVertices={triangulation.vertices.Length}, " +
                    $"visualControllers={visualControllerCount}, " +
                    $"syntyPrefabInstances={syntyPrefabInstances}, " +
                    $"rooftopLights={rooftopLights}, visionBlockers={visionBlockerCount}.");
            }
        }

        private static int CountObjectsOnLayer(Transform root, int layer)
        {
            int count = 0;
            Transform[] transforms = root.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < transforms.Length; i++)
            {
                if (transforms[i].gameObject.layer == layer)
                {
                    count++;
                }
            }

            return count;
        }

        private static int CountSyntyPrefabInstances(Scene scene)
        {
            HashSet<int> instanceIds = new HashSet<int>();
            GameObject[] roots = scene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                Transform[] transforms = roots[i].GetComponentsInChildren<Transform>(true);
                for (int j = 0; j < transforms.Length; j++)
                {
                    GameObject nearestRoot = PrefabUtility.GetNearestPrefabInstanceRoot(
                        transforms[j].gameObject);
                    if (nearestRoot == null)
                    {
                        continue;
                    }

                    string path = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(
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
