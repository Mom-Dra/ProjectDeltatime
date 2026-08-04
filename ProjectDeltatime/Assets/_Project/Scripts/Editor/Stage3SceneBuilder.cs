using System;
using System.Collections.Generic;
using System.IO;
using Deltatime.Combat;
using Deltatime.Enemies;
using Deltatime.Level;
using Deltatime.Player;
using Deltatime.Replay;
using Deltatime.TimeSystem;
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
    /// Builds Stage 3 as a combat-compatible Polygon Nightclubs scene without
    /// regenerating or rewriting Stage 1 and Stage 2.
    /// </summary>
    public static class Stage3SceneBuilder
    {
        private const string Stage2ScenePath =
            "Assets/_Project/Scenes/Stage2.unity";
        private const string Stage3ScenePath =
            "Assets/_Project/Scenes/Stage3.unity";
        private const string Stage3NavigationPath =
            "Assets/_Project/Scenes/Stage3Navigation.asset";
        private const string PreviewAssetPath =
            "Assets/_Project/Art/Generated/Stage3Preview.png";
        private const string NightclubRootName =
            "Stage 3 - Afterimage Club";
        private const string SyntyRoot =
            "Assets/Synty/PolygonNightclubs/Prefabs";
        private const string CharacterRoot = SyntyRoot + "/Characters";
        private const string PropsRoot = SyntyRoot + "/Props";
        private const string ModularPropsRoot = PropsRoot + "/Modular";
        private const string BuildingsRoot = SyntyRoot + "/Buildings";
        private const string BaseBuildingsRoot =
            SyntyRoot + "/Base_Buildings";
        private const int VisionObstacleLayer = 8;
        private const int DeadlineCharges = 2;

        private const string PlayerCharacterPath =
            CharacterRoot + "/SM_Chr_Party_Female_01.prefab";
        private const string WestEnemyCharacterPath =
            CharacterRoot + "/SM_Chr_Bartender_Male_01.prefab";
        private const string CenterEnemyCharacterPath =
            CharacterRoot + "/SM_Chr_Bouncer_Male_01.prefab";
        private const string EastEnemyCharacterPath =
            CharacterRoot + "/SM_Chr_Party_Male_02.prefab";

        private const string FloorPath =
            BuildingsRoot + "/SM_Bld_Floor_Combined_01.prefab";
        private const string WallPath =
            BaseBuildingsRoot + "/SM_Bld_Base_Wall_01.prefab";
        private const string WallFeaturePath =
            BuildingsRoot + "/SM_Bld_Wall_Feature_01.prefab";
        private const string BarPath =
            ModularPropsRoot + "/SM_Prop_Bar_01.prefab";
        private const string SofaPath =
            ModularPropsRoot + "/SM_Prop_Sofa_01.prefab";
        private const string DjBoothPath =
            PropsRoot + "/SM_Prop_DJ_Booth_01.prefab";
        private const string SpeakerPath =
            PropsRoot + "/SM_Prop_Speaker_Large_01.prefab";
        private const string TablePath =
            PropsRoot + "/SM_Prop_Table_04.prefab";
        private const string ChairPath =
            PropsRoot + "/SM_Prop_Chair_04.prefab";
        private const string DiscoBallPath =
            PropsRoot + "/SM_Prop_Disco_Ball_01.prefab";
        private const string StageLightPath =
            PropsRoot + "/SM_Prop_Light_Stage_05.prefab";

        private const string AccentMaterialPath =
            "Assets/_Project/Materials/PrototypeAccent3D.mat";
        private const string EnemyMaterialPath =
            "Assets/_Project/Materials/PrototypeEnemy3D.mat";
        private const string ChaserMaterialPath =
            "Assets/_Project/Materials/PrototypeChaser3D.mat";

        [MenuItem("Tools/Prototype/Build Stage 3 - Afterimage Club")]
        public static void BuildStage3()
        {
            RequireSourceScene();
            RequireNightclubAssets();

            Scene sourceScene = EditorSceneManager.OpenScene(
                Stage2ScenePath,
                OpenSceneMode.Single);
            if (!EditorSceneManager.SaveScene(
                    sourceScene,
                    Stage3ScenePath,
                    true))
            {
                throw new InvalidOperationException(
                    $"Failed to copy {Stage2ScenePath} to {Stage3ScenePath}.");
            }

            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            Scene scene = EditorSceneManager.OpenScene(
                Stage3ScenePath,
                OpenSceneMode.Single);

            RemoveStage2Environment();
            RepositionGameplayObjects();
            GameObject nightclubRoot = BuildNightclubEnvironment(scene);
            AttachNightclubCharacters(scene);
            ConfigureNightclubLighting(nightclubRoot.transform);
            ConfigureNightclubCamera();
            BuildStage3Navigation();

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, Stage3ScenePath))
            {
                throw new InvalidOperationException(
                    $"Failed to save {Stage3ScenePath}.");
            }

            AddStage3ToBuildSettings();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            ValidateStage3Scene(scene);

            if (!Application.isBatchMode)
            {
                Selection.activeGameObject = GameObject.Find("Player");
            }

            Debug.Log(
                "Stage3 Afterimage Club built and validated successfully.");
        }

        public static void BuildAndValidateFromCommandLine()
        {
            BuildStage3();
        }

        [MenuItem("Tools/Prototype/Validate Stage 3 - Afterimage Club")]
        public static void ValidateSavedStage3()
        {
            Scene scene = EditorSceneManager.OpenScene(
                Stage3ScenePath,
                OpenSceneMode.Single);
            ValidateStage3Scene(scene);
            Debug.Log("Stage3 Afterimage Club validation passed.");
        }

        public static void CapturePreviewFromCommandLine()
        {
            Scene scene = EditorSceneManager.OpenScene(
                Stage3ScenePath,
                OpenSceneMode.Single);
            ValidateStage3Scene(scene);

            Camera camera = UnityEngine.Object.FindFirstObjectByType<Camera>();
            if (camera == null)
            {
                throw new InvalidOperationException(
                    "Stage3 preview requires a camera.");
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
                camera.transform.position = new Vector3(0f, 16.5f, -16.5f);
                camera.transform.LookAt(new Vector3(0f, 0.4f, 0f), Vector3.up);
                camera.fieldOfView = 54f;
                camera.targetTexture = target;
                RenderTexture.active = target;
                camera.Render();
                preview.ReadPixels(
                    new Rect(0f, 0f, width, height),
                    0,
                    0);
                preview.Apply();

                string previewPath = Path.Combine(
                    Application.dataPath,
                    "_Project",
                    "Art",
                    "Generated",
                    "Stage3Preview.png");
                Directory.CreateDirectory(Path.GetDirectoryName(previewPath));
                File.WriteAllBytes(previewPath, preview.EncodeToPNG());
                AssetDatabase.ImportAsset(
                    PreviewAssetPath,
                    ImportAssetOptions.ForceSynchronousImport);
                Debug.Log($"Stage3 preview captured at {previewPath}.");
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
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(Stage2ScenePath) ==
                null)
            {
                throw new InvalidOperationException(
                    $"Stage3 requires the existing source scene at {Stage2ScenePath}.");
            }
        }

        private static void RequireNightclubAssets()
        {
            string[] requiredPaths =
            {
                PlayerCharacterPath,
                WestEnemyCharacterPath,
                CenterEnemyCharacterPath,
                EastEnemyCharacterPath,
                FloorPath,
                WallPath,
                WallFeaturePath,
                BarPath,
                SofaPath,
                DjBoothPath,
                SpeakerPath,
                TablePath,
                ChairPath,
                DiscoBallPath,
                StageLightPath
            };

            for (int i = 0; i < requiredPaths.Length; i++)
            {
                if (AssetDatabase.LoadAssetAtPath<GameObject>(
                        requiredPaths[i]) == null)
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

        private static void RepositionGameplayObjects()
        {
            SetPose("Player", new Vector3(0f, 0.75f, -7.1f), 0f);
            SetPose("Enemy West", new Vector3(-6.5f, 0.75f, 3.3f), 180f);
            SetPose("Enemy Center", new Vector3(0f, 0.78f, 5.2f), 180f);
            SetPose("Enemy East", new Vector3(6.2f, 0.75f, 2.6f), 180f);
            SetPose("Pistol Pickup", new Vector3(-2.25f, 0.18f, -5.6f), 28f);
            SetPose("Shotgun Pickup", new Vector3(2.25f, 0.18f, -5.6f), -28f);
        }

        private static void SetPose(string name, Vector3 position, float yaw)
        {
            GameObject target = GameObject.Find(name);
            if (target == null)
            {
                throw new InvalidOperationException(
                    $"Stage3 source object is missing: {name}");
            }

            target.transform.SetPositionAndRotation(
                position,
                Quaternion.Euler(0f, yaw, 0f));
        }

        private static GameObject BuildNightclubEnvironment(Scene scene)
        {
            GameObject root = new GameObject(NightclubRootName);
            SceneManager.MoveGameObjectToScene(root, scene);
            Transform architecture = CreateGroup("Architecture", root.transform);
            Transform cover = CreateGroup("Gameplay Cover", root.transform);
            Transform decor = CreateGroup("Club Decor", root.transform);

            CreateCollisionBlocker(
                "Club Floor Collision",
                new Vector3(0f, -0.12f, 0f),
                new Vector3(20f, 0.24f, 18f),
                root.transform,
                false);
            CreateCollisionBlocker(
                "North Perimeter",
                new Vector3(0f, 1.5f, 9.2f),
                new Vector3(20.5f, 3f, 0.5f),
                root.transform,
                true);
            CreateCollisionBlocker(
                "South Perimeter",
                new Vector3(0f, 1.5f, -9.2f),
                new Vector3(20.5f, 3f, 0.5f),
                root.transform,
                true);
            CreateCollisionBlocker(
                "West Perimeter",
                new Vector3(-10.2f, 1.5f, 0f),
                new Vector3(0.5f, 3f, 18.8f),
                root.transform,
                true);
            CreateCollisionBlocker(
                "East Perimeter",
                new Vector3(10.2f, 1.5f, 0f),
                new Vector3(0.5f, 3f, 18.8f),
                root.transform,
                true);

            BuildFloorTiles(architecture);
            BuildPerimeterWalls(architecture);
            BuildBarLane(cover, decor);
            BuildDjStage(cover, decor);
            BuildLoungeLane(cover, decor);
            BuildDanceFloorDecor(decor);
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
            for (int x = 0; x < 6; x++)
            {
                for (int z = 0; z < 6; z++)
                {
                    CreateNightclubAsset(
                        FloorPath,
                        $"Club Asset - Floor {tileNumber++}",
                        parent,
                        new Vector3(
                            (x - 2.5f) * 3f,
                            0f,
                            (z - 2.5f) * 3f),
                        0f,
                        Vector3.one);
                }
            }
        }

        private static void BuildPerimeterWalls(Transform parent)
        {
            float[] horizontal = { -9f, -6f, -3f, 0f, 3f, 6f, 9f };
            for (int i = 0; i < horizontal.Length; i++)
            {
                CreateNightclubAsset(
                    i == 2 || i == 4 ? WallFeaturePath : WallPath,
                    $"Club Asset - North Wall {i + 1}",
                    parent,
                    new Vector3(horizontal[i], 0f, 9f),
                    180f,
                    Vector3.one);
                CreateNightclubAsset(
                    WallPath,
                    $"Club Asset - South Wall {i + 1}",
                    parent,
                    new Vector3(horizontal[i], 0f, -9f),
                    0f,
                    Vector3.one);
            }

            float[] vertical = { -7.5f, -4.5f, -1.5f, 1.5f, 4.5f, 7.5f };
            for (int i = 0; i < vertical.Length; i++)
            {
                CreateNightclubAsset(
                    WallPath,
                    $"Club Asset - West Wall {i + 1}",
                    parent,
                    new Vector3(-10f, 0f, vertical[i]),
                    90f,
                    Vector3.one);
                CreateNightclubAsset(
                    WallPath,
                    $"Club Asset - East Wall {i + 1}",
                    parent,
                    new Vector3(10f, 0f, vertical[i]),
                    -90f,
                    Vector3.one);
            }
        }

        private static void BuildBarLane(Transform cover, Transform decor)
        {
            CreateCollisionBlocker(
                "West Bar Cover",
                new Vector3(-6.6f, 0.68f, 1.2f),
                new Vector3(1.4f, 1.36f, 5.5f),
                cover,
                true);

            for (int i = 0; i < 3; i++)
            {
                CreateNightclubAsset(
                    BarPath,
                    $"Club Asset - Bar {i + 1}",
                    decor,
                    new Vector3(-6.6f, 0f, -1.25f + i * 2.45f),
                    90f,
                    Vector3.one);
            }

            CreateNightclubAsset(
                TablePath,
                "Club Asset - West Cocktail Table",
                decor,
                new Vector3(-3.4f, 0f, -2.4f),
                0f,
                Vector3.one);
        }

        private static void BuildDjStage(Transform cover, Transform decor)
        {
            CreateCollisionBlocker(
                "DJ Booth Cover",
                new Vector3(0f, 0.72f, 6.35f),
                new Vector3(4.8f, 1.44f, 1.2f),
                cover,
                true);
            CreateCollisionBlocker(
                "West Speaker Stack",
                new Vector3(-3.35f, 0.85f, 6.75f),
                new Vector3(1.15f, 1.7f, 1.15f),
                cover,
                true);
            CreateCollisionBlocker(
                "East Speaker Stack",
                new Vector3(3.35f, 0.85f, 6.75f),
                new Vector3(1.15f, 1.7f, 1.15f),
                cover,
                true);

            CreateNightclubAsset(
                DjBoothPath,
                "Club Asset - DJ Booth",
                decor,
                new Vector3(0f, 0f, 6.15f),
                180f,
                Vector3.one);
            CreateNightclubAsset(
                SpeakerPath,
                "Club Asset - West Speaker",
                decor,
                new Vector3(-3.35f, 0f, 6.75f),
                180f,
                Vector3.one);
            CreateNightclubAsset(
                SpeakerPath,
                "Club Asset - East Speaker",
                decor,
                new Vector3(3.35f, 0f, 6.75f),
                180f,
                Vector3.one);
        }

        private static void BuildLoungeLane(Transform cover, Transform decor)
        {
            CreateCollisionBlocker(
                "East Lounge Cover North",
                new Vector3(6.2f, 0.55f, 0.8f),
                new Vector3(3.4f, 1.1f, 1.45f),
                cover,
                true);
            CreateCollisionBlocker(
                "East Lounge Cover South",
                new Vector3(5.4f, 0.55f, -3.25f),
                new Vector3(3.5f, 1.1f, 1.35f),
                cover,
                true);

            CreateNightclubAsset(
                SofaPath,
                "Club Asset - North Lounge Sofa",
                decor,
                new Vector3(6.2f, 0f, 0.8f),
                180f,
                Vector3.one);
            CreateNightclubAsset(
                SofaPath,
                "Club Asset - South Lounge Sofa",
                decor,
                new Vector3(5.4f, 0f, -3.25f),
                0f,
                Vector3.one);
            CreateNightclubAsset(
                TablePath,
                "Club Asset - Lounge Table",
                decor,
                new Vector3(7.1f, 0f, -1.65f),
                0f,
                Vector3.one);
            CreateNightclubAsset(
                ChairPath,
                "Club Asset - Lounge Chair",
                decor,
                new Vector3(8f, 0f, -2.2f),
                -45f,
                Vector3.one);
        }

        private static void BuildDanceFloorDecor(Transform decor)
        {
            CreateNightclubAsset(
                DiscoBallPath,
                "Club Asset - Disco Ball",
                decor,
                new Vector3(0f, 4.4f, 0.8f),
                0f,
                Vector3.one * 1.25f);

            Vector3[] lightPositions =
            {
                new Vector3(-6.5f, 3.7f, -5.5f),
                new Vector3(6.5f, 3.7f, -5.5f),
                new Vector3(-4f, 3.7f, 5f),
                new Vector3(4f, 3.7f, 5f)
            };
            for (int i = 0; i < lightPositions.Length; i++)
            {
                CreateNightclubAsset(
                    StageLightPath,
                    $"Club Asset - Stage Light {i + 1}",
                    decor,
                    lightPositions[i],
                    i < 2 ? 0f : 180f,
                    Vector3.one);
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
                    $"Failed to instantiate nightclub prefab: {prefabPath}");
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
            Collider[] colliders = root.GetComponentsInChildren<Collider>(true);
            for (int i = 0; i < colliders.Length; i++)
            {
                colliders[i].enabled = false;
            }
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

        private static void AttachNightclubCharacters(Scene scene)
        {
            AttachCharacter(
                scene,
                "Player",
                PlayerCharacterPath,
                "Club Character - Player");
            AttachCharacter(
                scene,
                "Enemy West",
                WestEnemyCharacterPath,
                "Club Character - Bartender Gunner");
            AttachCharacter(
                scene,
                "Enemy Center",
                CenterEnemyCharacterPath,
                "Club Character - Bouncer Chaser");
            AttachCharacter(
                scene,
                "Enemy East",
                EastEnemyCharacterPath,
                "Club Character - Party Gunner");

            Material playerRing = AssetDatabase.LoadAssetAtPath<Material>(
                AccentMaterialPath);
            Material rangedRing = AssetDatabase.LoadAssetAtPath<Material>(
                EnemyMaterialPath);
            Material chaserRing = AssetDatabase.LoadAssetAtPath<Material>(
                ChaserMaterialPath);
            CreateIdentityRing(GameObject.Find("Player").transform, playerRing);
            CreateIdentityRing(GameObject.Find("Enemy West").transform, rangedRing);
            CreateIdentityRing(GameObject.Find("Enemy Center").transform, chaserRing);
            CreateIdentityRing(GameObject.Find("Enemy East").transform, rangedRing);
        }

        private static void AttachCharacter(
            Scene scene,
            string ownerName,
            string prefabPath,
            string visualName)
        {
            GameObject owner = GameObject.Find(ownerName);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            GameObject visual = PrefabUtility.InstantiatePrefab(
                prefab,
                scene) as GameObject;
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

            Animator[] animators = visual.GetComponentsInChildren<Animator>(true);
            for (int i = 0; i < animators.Length; i++)
            {
                animators[i].applyRootMotion = false;
                animators[i].enabled = false;
            }

            ApplyRelaxedArmPose(visual);
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
                    $"Nightclub character '{visual.name}' is missing arm bones.");
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

        private static void CreateIdentityRing(
            Transform owner,
            Material material)
        {
            GameObject ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            ring.name = "Combat Identity Ring";
            ring.transform.position = new Vector3(
                owner.position.x,
                0.025f,
                owner.position.z);
            ring.transform.localScale = new Vector3(0.72f, 0.025f, 0.72f);
            UnityEngine.Object.DestroyImmediate(ring.GetComponent<Collider>());
            Renderer renderer = ring.GetComponent<Renderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            ring.transform.SetParent(owner, true);
        }

        private static void ConfigureNightclubLighting(Transform nightclubRoot)
        {
            Transform lighting = CreateGroup("Neon Light Rig", nightclubRoot);
            CreatePointLight(
                "Magenta Bar Light",
                lighting,
                new Vector3(-6.2f, 3.1f, 0.8f),
                new Color(1f, 0.04f, 0.52f, 1f),
                3.1f,
                8f);
            CreatePointLight(
                "Cyan Lounge Light",
                lighting,
                new Vector3(6.1f, 3.1f, -1.2f),
                new Color(0.02f, 0.72f, 1f, 1f),
                3f,
                8f);
            CreatePointLight(
                "Violet Dance Floor Light",
                lighting,
                new Vector3(0f, 4.1f, 0.8f),
                new Color(0.56f, 0.08f, 1f, 1f),
                3.2f,
                9f);
            CreatePointLight(
                "Blue Entry Light",
                lighting,
                new Vector3(0f, 2.8f, -6.7f),
                new Color(0.05f, 0.32f, 1f, 1f),
                2.7f,
                7f);

            Light keyLight = GameObject.Find("Directional Key Light")
                ?.GetComponent<Light>();
            if (keyLight != null)
            {
                keyLight.color = new Color(0.55f, 0.65f, 1f, 1f);
                keyLight.intensity = 0.42f;
            }

            Camera camera = UnityEngine.Object.FindFirstObjectByType<Camera>();
            WorldTimeVisualFeedback feedback = camera == null
                ? null
                : camera.GetComponent<WorldTimeVisualFeedback>();
            if (feedback == null)
            {
                throw new InvalidOperationException(
                    "Stage3 requires WorldTimeVisualFeedback on its camera.");
            }

            SerializedObject settings = new SerializedObject(feedback);
            settings.FindProperty("ambientSkyColor").colorValue =
                new Color(0.07f, 0.025f, 0.12f, 1f);
            settings.FindProperty("ambientEquatorColor").colorValue =
                new Color(0.018f, 0.06f, 0.095f, 1f);
            settings.FindProperty("ambientGroundColor").colorValue =
                new Color(0.008f, 0.012f, 0.035f, 1f);
            settings.FindProperty("ambientIntensity").floatValue = 0.95f;
            settings.FindProperty("reflectionIntensity").floatValue = 0.5f;
            settings.FindProperty("directionalLightIntensity").floatValue = 0.42f;
            settings.FindProperty("fogColor").colorValue =
                new Color(0.018f, 0.006f, 0.04f, 1f);
            settings.FindProperty("fogStartDistance").floatValue = 26f;
            settings.FindProperty("fogEndDistance").floatValue = 55f;
            settings.FindProperty("mapFillLightColor").colorValue =
                new Color(0.22f, 0.42f, 1f, 1f);
            settings.FindProperty("mapFillLightIntensity").floatValue = 0.9f;
            settings.FindProperty("nearlyStoppedColor").colorValue =
                new Color(0.01f, 0.008f, 0.025f, 1f);
            settings.FindProperty("activeColor").colorValue =
                new Color(0.018f, 0.006f, 0.04f, 1f);
            settings.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(feedback);

            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor =
                new Color(0.07f, 0.025f, 0.12f, 1f);
            RenderSettings.ambientEquatorColor =
                new Color(0.018f, 0.06f, 0.095f, 1f);
            RenderSettings.ambientGroundColor =
                new Color(0.008f, 0.012f, 0.035f, 1f);
            RenderSettings.ambientIntensity = 0.95f;
            RenderSettings.reflectionIntensity = 0.5f;
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor =
                new Color(0.018f, 0.006f, 0.04f, 1f);
            RenderSettings.fogStartDistance = 26f;
            RenderSettings.fogEndDistance = 55f;
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

        private static void ConfigureNightclubCamera()
        {
            Camera camera = UnityEngine.Object.FindFirstObjectByType<Camera>();
            TopDownCameraController controller = camera == null
                ? null
                : camera.GetComponent<TopDownCameraController>();
            if (camera == null || controller == null)
            {
                throw new InvalidOperationException(
                    "Stage3 requires the existing gameplay camera rig.");
            }

            camera.fieldOfView = 52f;
            camera.backgroundColor = new Color(0.018f, 0.006f, 0.04f, 1f);
            controller.SnapToTarget();
            EditorUtility.SetDirty(camera);
            EditorUtility.SetDirty(controller);
        }

        private static void BuildStage3Navigation()
        {
            NavMeshSurface surface =
                UnityEngine.Object.FindFirstObjectByType<NavMeshSurface>();
            if (surface == null)
            {
                throw new InvalidOperationException(
                    "Stage3 navigation surface is missing.");
            }

            surface.RemoveData();
            surface.navMeshData = null;
            Physics.SyncTransforms();
            surface.BuildNavMesh();
            if (surface.navMeshData == null)
            {
                throw new InvalidOperationException(
                    "Stage3 navigation bake failed.");
            }

            NavMeshData bakedData = surface.navMeshData;
            bakedData.name = "Stage3Navigation";
            NavMeshData savedData = AssetDatabase.LoadAssetAtPath<NavMeshData>(
                Stage3NavigationPath);
            if (savedData == null)
            {
                AssetDatabase.CreateAsset(bakedData, Stage3NavigationPath);
            }
            else
            {
                surface.RemoveData();
                EditorUtility.CopySerialized(bakedData, savedData);
                surface.navMeshData = savedData;
                surface.AddData();
                UnityEngine.Object.DestroyImmediate(bakedData);
                savedData.name = "Stage3Navigation";
                EditorUtility.SetDirty(savedData);
            }

            EditorUtility.SetDirty(surface);
            AssetDatabase.SaveAssets();
        }

        private static void AddStage3ToBuildSettings()
        {
            List<EditorBuildSettingsScene> existing =
                new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            List<EditorBuildSettingsScene> ordered =
                new List<EditorBuildSettingsScene>();
            AddBuildSceneIfPresent(
                ordered,
                "Assets/_Project/Scenes/Stage1.unity");
            AddBuildSceneIfPresent(
                ordered,
                "Assets/_Project/Scenes/Stage2.unity");
            ordered.Add(new EditorBuildSettingsScene(Stage3ScenePath, true));

            for (int i = 0; i < existing.Count; i++)
            {
                string path = existing[i].path;
                if (path == "Assets/_Project/Scenes/Stage1.unity" ||
                    path == "Assets/_Project/Scenes/Stage2.unity" ||
                    path == Stage3ScenePath)
                {
                    continue;
                }

                ordered.Add(existing[i]);
            }

            EditorBuildSettings.scenes = ordered.ToArray();
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

        private static void ValidateStage3Scene(Scene scene)
        {
            GameObject nightclubRoot = GameObject.Find(NightclubRootName);
            PlayerHealth player =
                UnityEngine.Object.FindFirstObjectByType<PlayerHealth>();
            DeadlineController deadline =
                UnityEngine.Object.FindFirstObjectByType<DeadlineController>();
            StageController stage =
                UnityEngine.Object.FindFirstObjectByType<StageController>();
            StageReplayController replay =
                UnityEngine.Object.FindFirstObjectByType<StageReplayController>();
            NavMeshSurface surface =
                UnityEngine.Object.FindFirstObjectByType<NavMeshSurface>();
            int enemyCount =
                UnityEngine.Object.FindObjectsByType<EnemyHealth>(
                    FindObjectsSortMode.None).Length;
            int rangedCount =
                UnityEngine.Object.FindObjectsByType<EnemyShooter>(
                    FindObjectsSortMode.None).Length;
            int chaserCount =
                UnityEngine.Object.FindObjectsByType<EnemyChaser>(
                    FindObjectsSortMode.None).Length;
            int pickupCount =
                UnityEngine.Object.FindObjectsByType<WeaponPickup>(
                    FindObjectsSortMode.None).Length;
            int activeCharacterRenderers = CountActiveCharacterRenderers(scene);
            int syntyPrefabInstances = CountSyntyPrefabInstances(scene);
            int nightclubLights = nightclubRoot == null
                ? 0
                : nightclubRoot.GetComponentsInChildren<Light>(true).Length;

            SerializedObject deadlineSettings = deadline == null
                ? null
                : new SerializedObject(deadline);
            deadlineSettings?.Update();
            SerializedProperty charges = deadlineSettings == null
                ? null
                : deadlineSettings.FindProperty("maximumCharges");
            string navigationPath = surface == null ||
                                    surface.navMeshData == null
                ? string.Empty
                : AssetDatabase.GetAssetPath(surface.navMeshData);
            NavMeshTriangulation triangulation = NavMesh.CalculateTriangulation();

            if (scene.path != Stage3ScenePath ||
                nightclubRoot == null ||
                player == null ||
                deadline == null ||
                stage == null ||
                replay == null ||
                enemyCount != 3 ||
                rangedCount != 2 ||
                chaserCount != 1 ||
                pickupCount != 2 ||
                charges == null ||
                charges.intValue != DeadlineCharges ||
                surface == null ||
                navigationPath != Stage3NavigationPath ||
                triangulation.vertices.Length == 0 ||
                activeCharacterRenderers < 4 ||
                syntyPrefabInstances < 30 ||
                nightclubLights != 4)
            {
                throw new InvalidOperationException(
                    "Stage3 validation failed: " +
                    $"scene={scene.path}, nightclubRoot={nightclubRoot != null}, " +
                    $"player={player != null}, deadline={deadline != null}, " +
                    $"stage={stage != null}, replay={replay != null}, " +
                    $"enemies={enemyCount}, ranged={rangedCount}, chasers={chaserCount}, " +
                    $"pickups={pickupCount}, charges={charges?.intValue}, " +
                    $"navPath={navigationPath}, navVertices={triangulation.vertices.Length}, " +
                    $"characterRenderers={activeCharacterRenderers}, " +
                    $"syntyPrefabInstances={syntyPrefabInstances}, " +
                    $"nightclubLights={nightclubLights}.");
            }
        }

        private static int CountActiveCharacterRenderers(Scene scene)
        {
            int count = 0;
            GameObject[] roots = scene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                SkinnedMeshRenderer[] renderers =
                    roots[i].GetComponentsInChildren<SkinnedMeshRenderer>(true);
                for (int j = 0; j < renderers.Length; j++)
                {
                    if (renderers[j].gameObject.activeInHierarchy)
                    {
                        count++;
                    }
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
                    GameObject nearestRoot = PrefabUtility
                        .GetNearestPrefabInstanceRoot(transforms[j].gameObject);
                    if (nearestRoot == null)
                    {
                        continue;
                    }

                    string path = PrefabUtility
                        .GetPrefabAssetPathOfNearestInstanceRoot(nearestRoot);
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
