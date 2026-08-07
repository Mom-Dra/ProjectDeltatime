using System;
using System.Collections.Generic;
using System.IO;
using Deltatime.Combat;
using Deltatime.Enemies;
using Deltatime.Level;
using Deltatime.Performance;
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
    /// Builds Stage 6 from the official rooftop-bar demo and moves only Stage 5's
    /// proven gameplay roots into the independent copy. Neither source scene is saved.
    /// Encounter positions and the camera profile are derived from the baked rooftop
    /// NavMesh rather than copied from an earlier stage.
    /// </summary>
    public static class Stage6SceneBuilder
    {
        private const string RooftopDemoScenePath =
            "Assets/Synty/PolygonNightclubs/Scenes/Demo_RooftopBar_01.unity";
        private const string Stage5ScenePath =
            "Assets/_Project/Scenes/Stage5.unity";
        private const string Stage6ScenePath =
            "Assets/_Project/Scenes/Stage6.unity";
        private const string Stage6NavigationPath =
            "Assets/_Project/Scenes/Stage6Navigation.asset";
        private const string PreviewAssetPath =
            "Assets/_Project/Art/Generated/Stage6Preview.png";
        private const string FallbackVolumeProfilePath =
            "Assets/Synty/PolygonNightclubs/Scenes/Demo_DanceClub_01/NightClub_Overview.asset";
        private const string Stage6VolumeProfilePath =
            "Assets/_Project/Scenes/Stage6/Stage6VolumeProfile.asset";
        private const string EnvironmentRootName =
            "Stage 6 - Neon Overlook";
        private const string CharacterRoot =
            "Assets/Synty/PolygonNightclubs/Prefabs/Characters";
        private const int VisionObstacleLayer = 8;
        private const int DeadlineCharges = 2;
        private const float ActorRootHeight = 0.75f;
        private const float PickupHeight = 0.18f;
        private const float Stage5StyleCameraHeight = 11.12f;
        private const float Stage5StyleCameraBackwardDistance = 6.10f;
        private const float Stage5StyleCameraFocusZ = 1.42f;
        private const float Stage5StyleCameraFieldOfView = 48f;
        private const float Stage5StyleCameraAimLeadDistance = 1.25f;
        private const float GroundMovementSampleDistance = 1.25f;
        private const float GroundMovementSegmentLength = 0.12f;
        private const int NotWalkableNavMeshArea = 1;
        private const int OffscreenBackgroundCarCount = 8;
        private const float Stage6ShadowDistance = 40f;
        private const int Stage6MaximumShadowCascades = 2;
        private const int Stage6MaximumShadowedEnvironmentPointLights = 2;
        private const float Stage6ShadowSelectionInterval = 0.25f;
        private const float Stage6FallbackRendererDiscoveryInterval = 0.25f;

        private const string PlayerRingMaterialPath =
            "Assets/_Project/Materials/PrototypeAccent3D.mat";
        private const string RangedRingMaterialPath =
            "Assets/_Project/Materials/PrototypeEnemy3D.mat";
        private const string ChaserRingMaterialPath =
            "Assets/_Project/Materials/PrototypeChaser3D.mat";

        private const string PlayerCharacterPath =
            CharacterRoot + "/SM_Chr_Party_Male_01.prefab";
        private const string WestCharacterPath =
            CharacterRoot + "/SM_Chr_Bartender_Female_01.prefab";
        private const string CenterCharacterPath =
            CharacterRoot + "/SM_Chr_Bouncer_Male_01.prefab";
        private const string EastCharacterPath =
            CharacterRoot + "/SM_Chr_Party_Female_01.prefab";
        private const string NorthCharacterPath =
            CharacterRoot + "/SM_Chr_Party_Female_02.prefab";
        private const string SouthCharacterPath =
            CharacterRoot + "/SM_Chr_Party_Male_02.prefab";

        private static readonly string[] GameplayRootNames =
        {
            "Systems",
            "Debug HUD",
            "Player",
            "Enemy West",
            "Enemy Center",
            "Enemy East",
            "Enemy North Gunner",
            "Enemy South Chaser",
            "Pistol Pickup",
            "Shotgun Pickup",
            "Navigation",
            "Main Camera",
            "Directional Key Light"
        };

        private static readonly string[] RequiredEnvironmentNames =
        {
            "Scene",
            "Roof_Layer",
            "Roof_Layer_02",
            "Background_FX",
            "Background_Planes",
            "BackgroundCity",
            "Lighting (URP)",
            "Lighting (BIRP)",
            "Global Volume",
            "Reflection Probe",
            "Reflection Probe (1)"
        };

        private static readonly string[] Stage5VisualNames =
        {
            "Dive Bar Character - Player",
            "Dive Bar Character - West Gunner",
            "Dive Bar Character - Center Chaser",
            "Dive Bar Character - East Gunner",
            "Dive Bar Character - North Gunner",
            "Dive Bar Character - South Chaser"
        };

        private static readonly string[] OverlookVisualNames =
        {
            "Overlook Character - Player",
            "Overlook Character - West Gunner",
            "Overlook Character - Center Chaser",
            "Overlook Character - East Gunner",
            "Overlook Character - North Gunner",
            "Overlook Character - South Chaser"
        };

        [MenuItem("Tools/Prototype/Build Stage 6 - Neon Overlook")]
        public static void BuildStage6()
        {
            RequireSourceAssets();

            Scene demoScene = EditorSceneManager.OpenScene(
                RooftopDemoScenePath,
                OpenSceneMode.Single);
            EditorSceneManager.SetActiveScene(demoScene);
            DemoSnapshot demoSnapshot = CaptureDemoSnapshot(demoScene, true);

            if (!EditorSceneManager.SaveScene(
                    demoScene,
                    Stage6ScenePath,
                    true))
            {
                throw new InvalidOperationException(
                    $"Failed to copy {RooftopDemoScenePath} to {Stage6ScenePath}.");
            }

            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            Scene stage6 = EditorSceneManager.OpenScene(
                Stage6ScenePath,
                OpenSceneMode.Single);
            EditorSceneManager.SetActiveScene(stage6);

            GameObject demoCameraRoot = FindSceneRoot(stage6, "Main Camera");
            if (demoCameraRoot != null)
            {
                UnityEngine.Object.DestroyImmediate(demoCameraRoot);
            }

            GameObject environmentRoot = PrepareRooftopEnvironment(stage6);
            EnsureStage6VolumeProfile(environmentRoot);
            List<GameObject> gameplayRoots = MoveStage5GameplayRoots(stage6);
            AttachOverlookCharacters(stage6);
            ConfigureRooftopVisualFeedback(demoSnapshot);
            ConfigureStage6Performance(stage6, environmentRoot);
            ColliderSummary colliderSummary = ConfigureRooftopColliders(environmentRoot);

            NavMeshSurface surface = BuildStage6Navigation(
                environmentRoot,
                gameplayRoots,
                out Bounds playableFloorBounds);
            NavMeshRegion combatRegion = FindLargestConnectedNavMeshRegion();
            ConfigureStage6ElevationMovement(stage6, environmentRoot);
            DisableOffscreenBackgroundCars(environmentRoot);
            EncounterLayout layout = PositionEncounterOnBakedNavigation(combatRegion);
            ConfigureCombatIdentityRings();
            ConfigureRooftopCamera(surface, combatRegion.Bounds);

            EditorSceneManager.MarkSceneDirty(stage6);
            if (!EditorSceneManager.SaveScene(stage6, Stage6ScenePath))
            {
                throw new InvalidOperationException(
                    $"Failed to save {Stage6ScenePath}.");
            }

            AddStage6ToBuildSettings();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            ValidateStage6Scene(stage6, demoSnapshot);

            NavMeshTriangulation triangulation = NavMesh.CalculateTriangulation();
            Debug.Log(
                $"Stage6 Neon Overlook built and validated successfully. " +
                $"Demo renderers={demoSnapshot.RendererCount}, " +
                $"outermost prefabs={demoSnapshot.OutermostPrefabCount}, " +
                $"kept colliders={colliderSummary.EnabledCount}, " +
                $"VisionObstacle colliders={colliderSummary.VisionCount}, " +
                $"playable floor bounds={playableFloorBounds}, " +
                $"combat triangles={combatRegion.TriangleCount}, " +
                $"NavMesh vertices={triangulation.vertices.Length}, " +
                $"indices={triangulation.indices.Length}, " +
                $"player={layout.Player}, west={layout.West}, center={layout.Center}, " +
                $"east={layout.East}, north={layout.North}, south={layout.South}.");

            if (!Application.isBatchMode)
            {
                Selection.activeGameObject = FindSceneRoot(stage6, "Player");
            }
        }

        public static void BuildAndValidateFromCommandLine()
        {
            BuildStage6();
        }

        public static void ValidateStage1Through5RegressionFromCommandLine()
        {
            PrototypeSceneBuilder.ValidateSavedPrototypeRoom();
            Stage3SceneBuilder.ValidateSavedStage3();
            Stage4SceneBuilder.ValidateSavedStage4();
            Stage5SceneBuilder.ValidateSavedStage5();
            Debug.Log("Stage1 through Stage5 read-only regression validation passed.");
        }

        [MenuItem("Tools/Prototype/Validate Stage 6 - Neon Overlook")]
        public static void ValidateSavedStage6()
        {
            Scene scene = EditorSceneManager.OpenScene(
                Stage6ScenePath,
                OpenSceneMode.Single);
            DemoSnapshot sourceSnapshot = CaptureSourceSnapshotAdditively(scene);
            ValidateStage6Scene(scene, sourceSnapshot);
            Debug.Log("Stage6 Neon Overlook validation passed.");
        }

        public static void CapturePreviewFromCommandLine()
        {
            Scene scene = EditorSceneManager.OpenScene(
                Stage6ScenePath,
                OpenSceneMode.Single);
            DemoSnapshot sourceSnapshot = CaptureSourceSnapshotAdditively(scene);
            ValidateStage6Scene(scene, sourceSnapshot);

            Camera camera = FindActiveGameplayCamera(scene);
            if (camera == null)
            {
                throw new InvalidOperationException(
                    "Stage6 preview requires its active gameplay camera.");
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

            try
            {
                camera.GetComponent<TopDownCameraController>()?.SnapToTarget();
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
                    "Stage6Preview.png");
                Directory.CreateDirectory(Path.GetDirectoryName(previewPath));
                File.WriteAllBytes(previewPath, preview.EncodeToPNG());
                AssetDatabase.ImportAsset(
                    PreviewAssetPath,
                    ImportAssetOptions.ForceSynchronousImport);
                Debug.Log($"Stage6 preview captured at {previewPath} ({width}x{height}).");
            }
            finally
            {
                camera.targetTexture = previousTarget;
                RenderTexture.active = previousActive;
                UnityEngine.Object.DestroyImmediate(preview);
                UnityEngine.Object.DestroyImmediate(target);
            }
        }

        private static void RequireSourceAssets()
        {
            Require(
                AssetDatabase.LoadAssetAtPath<SceneAsset>(RooftopDemoScenePath) != null,
                $"Stage6 requires the official rooftop scene: {RooftopDemoScenePath}");
            Require(
                AssetDatabase.LoadAssetAtPath<SceneAsset>(Stage5ScenePath) != null,
                $"Stage6 requires the gameplay-root source: {Stage5ScenePath}");

            string[] characterPaths =
            {
                PlayerCharacterPath,
                WestCharacterPath,
                CenterCharacterPath,
                EastCharacterPath,
                NorthCharacterPath,
                SouthCharacterPath
            };
            for (int i = 0; i < characterPaths.Length; i++)
            {
                Require(
                    AssetDatabase.LoadAssetAtPath<GameObject>(characterPaths[i]) != null,
                    $"Required overlook character is missing: {characterPaths[i]}");
            }
        }

        private static DemoSnapshot CaptureDemoSnapshot(Scene scene, bool logDetails)
        {
            GameObject cameraRoot = FindSceneRoot(scene, "Main Camera");
            Camera demoCamera = cameraRoot == null
                ? null
                : cameraRoot.GetComponent<Camera>();
            CameraSettings cameraSettings = demoCamera == null
                ? new CameraSettings(CameraClearFlags.Skybox, Color.black)
                : new CameraSettings(demoCamera.clearFlags, demoCamera.backgroundColor);

            Transform urpLighting = FindSceneTransform(scene, "Lighting (URP)");
            DirectionalLightSettings directional = CaptureActiveDirectionalLight(urpLighting);
            Require(directional != null,
                "The rooftop demo has no active URP Directional Light to measure.");

            DemoSnapshot snapshot = new DemoSnapshot
            {
                Camera = cameraSettings,
                Directional = directional,
                RendererCount = CountSceneComponents<Renderer>(scene, cameraRoot),
                OutermostPrefabCount = CountOutermostPrefabRoots(scene, cameraRoot),
                PointLightCount = CountSceneLights(scene, LightType.Point, cameraRoot),
                ReflectionProbeCount = CountSceneComponents<ReflectionProbe>(scene, cameraRoot),
                RequiredNameCounts = new Dictionary<string, int>(),
                RequiredActiveStates = new Dictionary<string, bool>(),
                RenderSettings = CaptureRenderSettings()
            };

            for (int i = 0; i < RequiredEnvironmentNames.Length; i++)
            {
                string requiredName = RequiredEnvironmentNames[i];
                List<Transform> matches = FindSceneTransforms(scene, requiredName);
                snapshot.RequiredNameCounts[requiredName] = matches.Count;
                snapshot.RequiredActiveStates[requiredName] =
                    matches.Count > 0 && matches[0].gameObject.activeSelf;
                Require(matches.Count > 0,
                    $"Rooftop demo required hierarchy is missing: {requiredName}");
            }

            if (logDetails)
            {
                Debug.Log(
                    $"Measured Demo_RooftopBar_01 before copy: " +
                    $"renderers={snapshot.RendererCount}, " +
                    $"outermost prefabs={snapshot.OutermostPrefabCount}, " +
                    $"point lights={snapshot.PointLightCount}, " +
                    $"reflection probes={snapshot.ReflectionProbeCount}, " +
                    $"camera clearFlags={snapshot.Camera.ClearFlags}, " +
                    $"camera background={snapshot.Camera.BackgroundColor}, " +
                    $"directional color={directional.Color}, " +
                    $"intensity={directional.Intensity}, " +
                    $"rotation={directional.Rotation.eulerAngles}, " +
                    $"shadows={directional.Shadows}.");
            }

            return snapshot;
        }

        private static DemoSnapshot CaptureSourceSnapshotAdditively(Scene stage6)
        {
            EditorSceneManager.SetActiveScene(stage6);
            RenderSettingsSnapshot stage6Settings = CaptureRenderSettings();
            Scene source = EditorSceneManager.OpenScene(
                RooftopDemoScenePath,
                OpenSceneMode.Additive);
            try
            {
                EditorSceneManager.SetActiveScene(source);
                DemoSnapshot sourceSnapshot = CaptureDemoSnapshot(source, false);
                sourceSnapshot.Stage6RenderSettings = stage6Settings;
                return sourceSnapshot;
            }
            finally
            {
                EditorSceneManager.SetActiveScene(stage6);
                if (source.IsValid() && source.isLoaded)
                {
                    EditorSceneManager.CloseScene(source, true);
                }
            }
        }

        private static GameObject PrepareRooftopEnvironment(Scene scene)
        {
            GameObject[] sourceRoots = scene.GetRootGameObjects();
            GameObject root = new GameObject(EnvironmentRootName);
            SceneManager.MoveGameObjectToScene(root, scene);
            root.AddComponent<ReplayExcluded>();

            for (int i = 0; i < sourceRoots.Length; i++)
            {
                GameObject sourceRoot = sourceRoots[i];
                if (sourceRoot == null || sourceRoot.name == "Main Camera")
                {
                    continue;
                }

                sourceRoot.transform.SetParent(root.transform, true);
            }

            RemoveMissingScriptsFromCopiedEnvironment(root);

            Transform urpLighting = FindDescendant(root.transform, "Lighting (URP)");
            Require(urpLighting != null && urpLighting.gameObject.activeSelf,
                "Stage6 did not preserve the active URP lighting hierarchy.");
            Light[] environmentLights = urpLighting.GetComponentsInChildren<Light>(true);
            int disabledDirectionals = 0;
            for (int i = 0; i < environmentLights.Length; i++)
            {
                Light light = environmentLights[i];
                if (light.type == LightType.Directional && light.isActiveAndEnabled)
                {
                    light.enabled = false;
                    EditorUtility.SetDirty(light);
                    disabledDirectionals++;
                }
            }

            Require(disabledDirectionals > 0,
                "Stage6 did not disable the measured demo Directional Light component.");
            return root;
        }

        private static void RemoveMissingScriptsFromCopiedEnvironment(GameObject root)
        {
            Transform[] transforms = root.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < transforms.Length; i++)
            {
                GameObject gameObject = transforms[i].gameObject;
                if (GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(gameObject) <= 0)
                {
                    continue;
                }

                GameObjectUtility.RemoveMonoBehavioursWithMissingScript(gameObject);
                EditorUtility.SetDirty(gameObject);
            }
        }

        private static void EnsureStage6VolumeProfile(GameObject environmentRoot)
        {
            Volume[] volumes = environmentRoot.GetComponentsInChildren<Volume>(true);
            bool requiresFallback = false;
            for (int i = 0; i < volumes.Length; i++)
            {
                if (volumes[i].sharedProfile == null)
                {
                    requiresFallback = true;
                    break;
                }
            }

            if (!requiresFallback)
            {
                return;
            }

            Require(
                AssetDatabase.LoadAssetAtPath<VolumeProfile>(FallbackVolumeProfilePath) != null,
                $"Stage6 fallback Volume Profile is missing: {FallbackVolumeProfilePath}");
            if (!AssetDatabase.IsValidFolder("Assets/_Project/Scenes/Stage6"))
            {
                AssetDatabase.CreateFolder("Assets/_Project/Scenes", "Stage6");
            }

            VolumeProfile stage6Profile =
                AssetDatabase.LoadAssetAtPath<VolumeProfile>(Stage6VolumeProfilePath);
            if (stage6Profile == null)
            {
                Require(AssetDatabase.CopyAsset(
                        FallbackVolumeProfilePath,
                        Stage6VolumeProfilePath),
                    $"Failed to create the Stage6 Volume Profile at {Stage6VolumeProfilePath}.");
                AssetDatabase.ImportAsset(
                    Stage6VolumeProfilePath,
                    ImportAssetOptions.ForceSynchronousImport);
                stage6Profile =
                    AssetDatabase.LoadAssetAtPath<VolumeProfile>(Stage6VolumeProfilePath);
            }

            Require(stage6Profile != null,
                "Stage6 Volume Profile could not be loaded after creation.");
            for (int i = 0; i < volumes.Length; i++)
            {
                if (volumes[i].sharedProfile != null)
                {
                    continue;
                }

                volumes[i].sharedProfile = stage6Profile;
                EditorUtility.SetDirty(volumes[i]);
            }

            Debug.Log(
                "Stage6 repaired the source demo's missing Global Volume profile " +
                $"with the official Synty overview profile copy at {Stage6VolumeProfilePath}.");
        }

        private static List<GameObject> MoveStage5GameplayRoots(Scene stage6)
        {
            EditorSceneManager.SetActiveScene(stage6);
            Scene stage5 = EditorSceneManager.OpenScene(
                Stage5ScenePath,
                OpenSceneMode.Additive);
            List<GameObject> moved = new List<GameObject>();

            try
            {
                RemoveStage5VisualChildren(stage5);
                for (int i = 0; i < GameplayRootNames.Length; i++)
                {
                    GameObject root = FindSceneRoot(stage5, GameplayRootNames[i]);
                    Require(root != null,
                        $"Stage5 gameplay root is missing: {GameplayRootNames[i]}");
                    SceneManager.MoveGameObjectToScene(root, stage6);
                    moved.Add(root);
                }
            }
            finally
            {
                EditorSceneManager.SetActiveScene(stage6);
                if (stage5.IsValid() && stage5.isLoaded)
                {
                    EditorSceneManager.CloseScene(stage5, true);
                }
            }

            return moved;
        }

        private static void RemoveStage5VisualChildren(Scene stage5)
        {
            HashSet<string> stage5Names = new HashSet<string>(Stage5VisualNames);
            List<GameObject> remove = new List<GameObject>();
            GameObject[] roots = stage5.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                Transform[] transforms = roots[i].GetComponentsInChildren<Transform>(true);
                for (int j = 0; j < transforms.Length; j++)
                {
                    if (stage5Names.Contains(transforms[j].name))
                    {
                        remove.Add(transforms[j].gameObject);
                    }
                }
            }

            Require(remove.Count == Stage5VisualNames.Length,
                $"Expected six Stage5 visual children before movement; found {remove.Count}.");
            for (int i = 0; i < remove.Count; i++)
            {
                UnityEngine.Object.DestroyImmediate(remove[i]);
            }
        }

        private static void AttachOverlookCharacters(Scene scene)
        {
            AttachCharacter(scene, "Player", PlayerCharacterPath, OverlookVisualNames[0]);
            AttachCharacter(scene, "Enemy West", WestCharacterPath, OverlookVisualNames[1]);
            AttachCharacter(scene, "Enemy Center", CenterCharacterPath, OverlookVisualNames[2]);
            AttachCharacter(scene, "Enemy East", EastCharacterPath, OverlookVisualNames[3]);
            AttachCharacter(scene, "Enemy North Gunner", NorthCharacterPath, OverlookVisualNames[4]);
            AttachCharacter(scene, "Enemy South Chaser", SouthCharacterPath, OverlookVisualNames[5]);
        }

        private static void AttachCharacter(
            Scene scene,
            string ownerName,
            string prefabPath,
            string visualName)
        {
            GameObject owner = FindSceneRoot(scene, ownerName);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            GameObject visual = prefab == null
                ? null
                : PrefabUtility.InstantiatePrefab(prefab, scene) as GameObject;
            Require(owner != null && visual != null,
                $"Failed to attach {prefabPath} to {ownerName}.");

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

            Collider[] colliders = visual.GetComponentsInChildren<Collider>(true);
            for (int i = 0; i < colliders.Length; i++)
            {
                colliders[i].enabled = false;
            }

            Rigidbody[] bodies = visual.GetComponentsInChildren<Rigidbody>(true);
            for (int i = 0; i < bodies.Length; i++)
            {
                bodies[i].isKinematic = true;
                bodies[i].detectCollisions = false;
            }

            Animator[] animators = visual.GetComponentsInChildren<Animator>(true);
            for (int i = 0; i < animators.Length; i++)
            {
                animators[i].applyRootMotion = false;
                animators[i].enabled = false;
            }

            ApplyRelaxedArmPose(visual);
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
                switch (transforms[i].name)
                {
                    case "Shoulder_L":
                        leftShoulder = transforms[i];
                        break;
                    case "Shoulder_R":
                        rightShoulder = transforms[i];
                        break;
                    case "Elbow_L":
                        leftElbow = transforms[i];
                        break;
                    case "Elbow_R":
                        rightElbow = transforms[i];
                        break;
                }
            }

            Require(leftShoulder != null && rightShoulder != null &&
                    leftElbow != null && rightElbow != null,
                $"Overlook character '{visual.name}' is missing arm bones.");
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

            bone.rotation = Quaternion.FromToRotation(
                currentDirection.normalized,
                targetDirection) * bone.rotation;
        }

        private static void ConfigureRooftopVisualFeedback(DemoSnapshot demoSnapshot)
        {
            Scene scene = SceneManager.GetActiveScene();
            Camera camera = FindActiveGameplayCamera(scene);
            WorldTimeVisualFeedback feedback = camera == null
                ? null
                : camera.GetComponent<WorldTimeVisualFeedback>();
            Light keyLight = FindSceneRoot(scene, "Directional Key Light")
                ?.GetComponent<Light>();
            Require(camera != null && feedback != null && keyLight != null,
                "Stage6 requires its gameplay camera, visual feedback, and key light.");

            camera.clearFlags = demoSnapshot.Camera.ClearFlags;
            camera.backgroundColor = demoSnapshot.Camera.BackgroundColor;
            ApplyDirectionalSettings(keyLight, demoSnapshot.Directional);

            SerializedObject settings = new SerializedObject(feedback);
            settings.FindProperty("directionalKeyLight").objectReferenceValue = keyLight;
            settings.FindProperty("preserveSceneRenderSettings").boolValue = true;
            settings.FindProperty("mapFillLightIntensity").floatValue = 0f;
            settings.FindProperty("mapFillLightPositions").arraySize = 0;
            settings.FindProperty("nearlyStoppedColor").colorValue =
                demoSnapshot.Camera.BackgroundColor;
            settings.FindProperty("activeColor").colorValue =
                demoSnapshot.Camera.BackgroundColor;
            settings.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(feedback);
            EditorUtility.SetDirty(camera);
            EditorUtility.SetDirty(keyLight);
        }

        private static void ConfigureStage6Performance(
            Scene scene,
            GameObject environmentRoot)
        {
            GameObject systems = FindSceneRoot(scene, "Systems");
            GameObject player = FindSceneRoot(scene, "Player");
            Require(systems != null && player != null && environmentRoot != null,
                "Stage6 performance setup requires Systems, Player, and the environment root.");

            Stage6PerformanceController performance =
                systems.GetComponent<Stage6PerformanceController>();
            if (performance == null)
            {
                performance = systems.AddComponent<Stage6PerformanceController>();
            }

            performance.Configure(
                environmentRoot.transform,
                player.transform,
                Stage6ShadowDistance,
                Stage6MaximumShadowCascades,
                ShadowResolution.Medium,
                Stage6MaximumShadowedEnvironmentPointLights,
                Stage6ShadowSelectionInterval);

            StageReplayController replay =
                systems.GetComponent<StageReplayController>();
            Require(replay != null,
                "Stage6 performance setup requires StageReplayController on Systems.");
            Transform[] dynamicRoots =
            {
                systems.transform,
                player.transform,
                FindSceneRoot(scene, "Enemy West")?.transform,
                FindSceneRoot(scene, "Enemy Center")?.transform,
                FindSceneRoot(scene, "Enemy East")?.transform,
                FindSceneRoot(scene, "Enemy North Gunner")?.transform,
                FindSceneRoot(scene, "Enemy South Chaser")?.transform,
                FindSceneRoot(scene, "Pistol Pickup")?.transform,
                FindSceneRoot(scene, "Shotgun Pickup")?.transform
            };
            for (int i = 0; i < dynamicRoots.Length; i++)
            {
                Require(dynamicRoots[i] != null,
                    "Stage6 performance setup is missing a replay discovery root.");
            }

            replay.ConfigureRendererDiscovery(
                dynamicRoots,
                Stage6FallbackRendererDiscoveryInterval);
            EditorUtility.SetDirty(performance);
            EditorUtility.SetDirty(replay);
        }

        private static DirectionalLightSettings CaptureActiveDirectionalLight(
            Transform lightingRoot)
        {
            if (lightingRoot == null)
            {
                return null;
            }

            Light[] lights = lightingRoot.GetComponentsInChildren<Light>(true);
            for (int i = 0; i < lights.Length; i++)
            {
                Light light = lights[i];
                if (light.type != LightType.Directional || !light.isActiveAndEnabled)
                {
                    continue;
                }

                return new DirectionalLightSettings
                {
                    Color = light.color,
                    Intensity = light.intensity,
                    Rotation = light.transform.rotation,
                    Shadows = light.shadows,
                    ShadowStrength = light.shadowStrength,
                    ShadowBias = light.shadowBias,
                    ShadowNormalBias = light.shadowNormalBias,
                    ShadowNearPlane = light.shadowNearPlane,
                    CullingMask = light.cullingMask,
                    RenderingLayerMask = light.renderingLayerMask,
                    BounceIntensity = light.bounceIntensity,
                    UseColorTemperature = light.useColorTemperature,
                    ColorTemperature = light.colorTemperature
                };
            }

            return null;
        }

        private static void ApplyDirectionalSettings(
            Light light,
            DirectionalLightSettings settings)
        {
            light.type = LightType.Directional;
            light.color = settings.Color;
            light.intensity = settings.Intensity;
            light.transform.rotation = settings.Rotation;
            light.shadows = settings.Shadows;
            light.shadowStrength = settings.ShadowStrength;
            light.shadowBias = settings.ShadowBias;
            light.shadowNormalBias = settings.ShadowNormalBias;
            light.shadowNearPlane = settings.ShadowNearPlane;
            light.cullingMask = settings.CullingMask;
            light.renderingLayerMask = settings.RenderingLayerMask;
            light.bounceIntensity = settings.BounceIntensity;
            light.useColorTemperature = settings.UseColorTemperature;
            light.colorTemperature = settings.ColorTemperature;
            light.enabled = true;
        }

        private static ColliderSummary ConfigureRooftopColliders(
            GameObject environmentRoot)
        {
            Collider[] colliders =
                environmentRoot.GetComponentsInChildren<Collider>(true);
            int enabledCount = 0;
            int visionCount = 0;
            int backgroundDisabled = 0;
            int smallDecorationDisabled = 0;
            for (int i = 0; i < colliders.Length; i++)
            {
                Collider collider = colliders[i];
                bool background = IsUnderNamedHierarchy(
                    collider.transform,
                    environmentRoot.transform,
                    "BackgroundCity") ||
                    IsUnderNamedHierarchy(
                        collider.transform,
                        environmentRoot.transform,
                        "Background_Planes") ||
                    IsUnderNamedHierarchy(
                        collider.transform,
                        environmentRoot.transform,
                        "Background_FX");
                bool smallDecoration = IsSmallMovementDecoration(collider);
                bool keep = collider.enabled && !background && !smallDecoration &&
                    ShouldKeepRooftopCollider(collider);
                collider.enabled = keep;
                collider.gameObject.layer = keep && ShouldBlockVision(collider)
                    ? VisionObstacleLayer
                    : 0;

                if (keep)
                {
                    enabledCount++;
                    if (collider.gameObject.layer == VisionObstacleLayer)
                    {
                        visionCount++;
                    }
                }
                else if (background)
                {
                    backgroundDisabled++;
                }
                else if (smallDecoration)
                {
                    smallDecorationDisabled++;
                }

                EditorUtility.SetDirty(collider);
                EditorUtility.SetDirty(collider.gameObject);
            }

            Require(enabledCount > 0,
                "Stage6 did not preserve any playable rooftop Physics Colliders.");
            Require(visionCount > 0,
                "Stage6 did not identify any structural VisionObstacle Colliders.");
            Debug.Log(
                $"Stage6 collider policy: enabled={enabledCount}, " +
                $"vision={visionCount}, background disabled={backgroundDisabled}, " +
                $"small decoration disabled={smallDecorationDisabled}.");
            return new ColliderSummary(enabledCount, visionCount);
        }

        private static bool ShouldKeepRooftopCollider(Collider collider)
        {
            string lower = collider.name.ToLowerInvariant();
            Bounds bounds = collider.bounds;
            if (lower.Contains("ceiling") ||
                (lower.Contains("roof") && !lower.Contains("rooftop")))
            {
                return false;
            }

            if (ContainsAny(lower,
                    "floor", "base_", "wall", "stairs", "stair", "steps",
                    "railing", "bar_", "counter", "sofa", "booth", "lounge",
                    "fridge", "pillar", "column", "planter", "table"))
            {
                return true;
            }

            float horizontal = Mathf.Max(bounds.size.x, bounds.size.z);
            return horizontal >= 1.1f || bounds.size.y >= 1.2f;
        }

        private static bool IsSmallMovementDecoration(Collider collider)
        {
            string lower = collider.name.ToLowerInvariant();
            if (ContainsAny(lower,
                    "glass", "bottle", "cup", "can_", "ashtray", "candle",
                    "plate", "bowl", "food", "napkin", "cutlery", "speaker_small"))
            {
                return true;
            }

            Bounds bounds = collider.bounds;
            return Mathf.Max(bounds.size.x, bounds.size.z) < 0.42f &&
                   bounds.size.y < 0.8f;
        }

        private static void ConfigureStage6ElevationMovement(
            Scene scene,
            GameObject environmentRoot)
        {
            int disabledTraversalColliders = DisableRuntimeTraversalColliders(environmentRoot);
            PlayerHealth player = FindSceneComponent<PlayerHealth>(scene);
            EnemyMotor[] motors = FindSceneComponents<EnemyMotor>(scene);
            Require(player != null && motors.Length == 5,
                "Stage6 elevation movement requires the player and five enemy motors.");

            ConfigureActorGroundMovement(player.gameObject);
            for (int i = 0; i < motors.Length; i++)
            {
                ConfigureActorGroundMovement(motors[i].gameObject);
            }

            Require(disabledTraversalColliders > 0,
                "Stage6 did not disable any baked stair/step colliders for runtime traversal.");
            Physics.SyncTransforms();
            Debug.Log(
                $"Stage6 elevation traversal: actors=6, runtime stair/step colliders disabled={disabledTraversalColliders}.");
        }

        private static void ConfigureActorGroundMovement(GameObject actor)
        {
            Rigidbody body = actor.GetComponent<Rigidbody>();
            Require(body != null,
                $"Stage6 elevation traversal actor has no Rigidbody: {actor.name}.");
            body.useGravity = false;
            body.constraints &= ~RigidbodyConstraints.FreezePositionY;
            NavMeshGroundMovement movement = actor.GetComponent<NavMeshGroundMovement>();
            if (movement == null)
            {
                movement = actor.AddComponent<NavMeshGroundMovement>();
            }

            movement.Configure(
                GroundMovementSampleDistance,
                GroundMovementSegmentLength);
            EditorUtility.SetDirty(body);
            EditorUtility.SetDirty(movement);
        }

        private static int DisableRuntimeTraversalColliders(GameObject environmentRoot)
        {
            int disabled = 0;
            Collider[] colliders = environmentRoot.GetComponentsInChildren<Collider>(true);
            for (int i = 0; i < colliders.Length; i++)
            {
                Collider collider = colliders[i];
                if (!collider.enabled || !IsRuntimeTraversalCollider(collider))
                {
                    continue;
                }

                collider.enabled = false;
                collider.gameObject.layer = 0;
                EditorUtility.SetDirty(collider);
                EditorUtility.SetDirty(collider.gameObject);
                disabled++;
            }

            return disabled;
        }

        private static bool IsRuntimeTraversalCollider(Collider collider)
        {
            string lower = collider.name.ToLowerInvariant();
            return (lower.Contains("stairs") || lower.Contains("steps")) &&
                   !lower.Contains("railing") && !lower.Contains("rail");
        }

        private static void DisableOffscreenBackgroundCars(GameObject environmentRoot)
        {
            Transform backgroundFx = FindDescendant(
                environmentRoot.transform,
                "Background_FX");
            Require(backgroundFx != null,
                "Stage6 background vehicle culling requires Background_FX.");

            Transform[] transforms = backgroundFx.GetComponentsInChildren<Transform>(true);
            int disabled = 0;
            for (int i = 0; i < transforms.Length; i++)
            {
                Transform transform = transforms[i];
                if (!transform.name.StartsWith(
                        "FX_Background_Cars_01",
                        StringComparison.Ordinal))
                {
                    continue;
                }

                transform.gameObject.SetActive(false);
                EditorUtility.SetDirty(transform.gameObject);
                disabled++;
            }

            Require(disabled == OffscreenBackgroundCarCount,
                $"Stage6 disabled {disabled} offscreen background cars instead of " +
                $"{OffscreenBackgroundCarCount}.");
        }

        private static bool ShouldBlockVision(Collider collider)
        {
            string lower = collider.name.ToLowerInvariant();
            Bounds bounds = collider.bounds;
            if (ContainsAny(lower,
                    "floor", "stairs", "stair", "steps", "railing"))
            {
                return false;
            }

            if (bounds.size.y < 0.9f)
            {
                return false;
            }

            return ContainsAny(lower,
                       "wall", "bar_", "counter", "sofa", "booth", "fridge",
                       "pillar", "column", "lounge", "planter") ||
                   (Mathf.Max(bounds.size.x, bounds.size.z) >= 1.4f &&
                    bounds.size.y >= 1.25f);
        }

        private static bool ContainsAny(string value, params string[] tokens)
        {
            for (int i = 0; i < tokens.Length; i++)
            {
                if (value.Contains(tokens[i]))
                {
                    return true;
                }
            }

            return false;
        }

        private static NavMeshSurface BuildStage6Navigation(
            GameObject environmentRoot,
            List<GameObject> gameplayRoots,
            out Bounds playableFloorBounds)
        {
            NavMeshSurface surface =
                FindSceneRoot(SceneManager.GetActiveScene(), "Navigation")
                    ?.GetComponent<NavMeshSurface>();
            Require(surface != null,
                "Stage6 Navigation root is missing its moved NavMeshSurface.");

            playableFloorBounds = CalculatePlayableFloorBounds(environmentRoot);
            surface.RemoveData();
            surface.navMeshData = null;
            surface.collectObjects = CollectObjects.Volume;
            surface.center = surface.transform.InverseTransformPoint(
                new Vector3(
                    playableFloorBounds.center.x,
                    playableFloorBounds.center.y + 2.5f,
                    playableFloorBounds.center.z));
            surface.size = new Vector3(
                playableFloorBounds.size.x + 1.5f,
                Mathf.Max(8f, playableFloorBounds.size.y + 6f),
                playableFloorBounds.size.z + 1.5f);
            surface.layerMask = (1 << 0) | (1 << VisionObstacleLayer);
            surface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;

            List<GameObject> temporarilyDisabled = new List<GameObject>();
            for (int i = 0; i < gameplayRoots.Count; i++)
            {
                GameObject root = gameplayRoots[i];
                if (root == surface.gameObject || !root.activeSelf)
                {
                    continue;
                }

                root.SetActive(false);
                temporarilyDisabled.Add(root);
            }

            List<NavMeshModifier> obstacleModifiers =
                CreateFurnitureExclusionModifiers(
                environmentRoot,
                "Stage6");

            try
            {
                Physics.SyncTransforms();
                surface.BuildNavMesh();
            }
            finally
            {
                for (int i = 0; i < temporarilyDisabled.Count; i++)
                {
                    temporarilyDisabled[i].SetActive(true);
                }

                for (int i = 0; i < obstacleModifiers.Count; i++)
                {
                    UnityEngine.Object.DestroyImmediate(obstacleModifiers[i]);
                }
            }

            Require(surface.navMeshData != null,
                "Stage6 navigation bake did not produce NavMeshData.");

            NavMeshData bakedData = surface.navMeshData;
            bakedData.name = "Stage6Navigation";
            NavMeshData savedData = AssetDatabase.LoadAssetAtPath<NavMeshData>(
                Stage6NavigationPath);
            if (savedData == null)
            {
                AssetDatabase.CreateAsset(bakedData, Stage6NavigationPath);
            }
            else
            {
                surface.RemoveData();
                EditorUtility.CopySerialized(bakedData, savedData);
                surface.navMeshData = savedData;
                surface.AddData();
                UnityEngine.Object.DestroyImmediate(bakedData);
                savedData.name = "Stage6Navigation";
                EditorUtility.SetDirty(savedData);
            }

            EditorUtility.SetDirty(surface);
            AssetDatabase.SaveAssets();
            return surface;
        }

        /// <summary>
        /// Preserve environment Physics Colliders for movement and sight while
        /// preventing furniture upper faces from becoming walkable NavMesh. The
        /// temporary modifiers are removed after baking, leaving no generated
        /// helper components in the saved scene hierarchy.
        /// </summary>
        private static List<NavMeshModifier> CreateFurnitureExclusionModifiers(
            GameObject environmentRoot,
            string stageName)
        {
            Collider[] colliders =
                environmentRoot.GetComponentsInChildren<Collider>(true);
            List<NavMeshModifier> modifiers = new List<NavMeshModifier>();
            for (int i = 0; i < colliders.Length; i++)
            {
                Collider collider = colliders[i];
                if (!collider.enabled || !collider.gameObject.activeInHierarchy ||
                    !ShouldExcludeColliderFromNavMesh(collider))
                {
                    continue;
                }

                NavMeshModifier existing =
                    collider.GetComponent<NavMeshModifier>();
                if (existing != null)
                {
                    continue;
                }

                NavMeshModifier modifier =
                    collider.gameObject.AddComponent<NavMeshModifier>();
                modifier.overrideArea = true;
                modifier.area = NotWalkableNavMeshArea;
                modifier.applyToChildren = false;
                modifiers.Add(modifier);
            }

            Require(modifiers.Count > 0,
                $"{stageName} found no non-ground collider to exclude from NavMesh.");
            Debug.Log(
                $"{stageName} NavMesh bake: {modifiers.Count} furniture collider sources " +
                "marked Not Walkable.");
            return modifiers;
        }

        private static bool IsWalkableNavigationGround(Collider collider)
        {
            string lower = collider.name.ToLowerInvariant();
            return (lower.Contains("floor") || lower.Contains("stairs") ||
                    lower.Contains("stair") || lower.Contains("steps")) &&
                   !lower.Contains("ceiling") && !lower.Contains("roof");
        }

        private static bool ShouldExcludeColliderFromNavMesh(Collider collider)
        {
            if (IsWalkableNavigationGround(collider))
            {
                return false;
            }

            string lower = collider.name.ToLowerInvariant();
            return ContainsAny(lower,
                "table", "chair", "stool", "sofa", "booth", "lounge",
                "bar_", "counter", "fridge", "shelf", "cabinet", "desk",
                "planter", "pillar", "column", "prop_");
        }

        private static Bounds CalculatePlayableFloorBounds(GameObject environmentRoot)
        {
            Transform sceneRoot = FindDescendant(environmentRoot.transform, "Scene");
            Require(sceneRoot != null,
                "Stage6 cannot derive playable bounds without the demo Scene hierarchy.");

            Bounds result = default;
            bool found = false;
            Collider[] colliders = sceneRoot.GetComponentsInChildren<Collider>(true);
            for (int i = 0; i < colliders.Length; i++)
            {
                Collider collider = colliders[i];
                string lower = collider.name.ToLowerInvariant();
                if (!collider.enabled || !collider.gameObject.activeInHierarchy ||
                    !ContainsAny(lower, "floor", "base_floor") ||
                    lower.Contains("ceiling") || lower.Contains("roof"))
                {
                    continue;
                }

                Encapsulate(ref result, ref found, collider.bounds);
            }

            if (!found)
            {
                Renderer[] renderers = sceneRoot.GetComponentsInChildren<Renderer>(true);
                for (int i = 0; i < renderers.Length; i++)
                {
                    Renderer renderer = renderers[i];
                    string lower = renderer.name.ToLowerInvariant();
                    if (!renderer.gameObject.activeInHierarchy ||
                        !ContainsAny(lower, "floor", "base_floor") ||
                        lower.Contains("ceiling") || lower.Contains("roof"))
                    {
                        continue;
                    }

                    Encapsulate(ref result, ref found, renderer.bounds);
                }
            }

            Require(found && result.size.x >= 8f && result.size.z >= 8f,
                $"Could not derive the official rooftop playable bounds: {result}");
            Require(result.size.x <= 120f && result.size.z <= 120f,
                $"Stage6 playable bounds include distant city geometry: {result}");
            return result;
        }

        private static void Encapsulate(
            ref Bounds result,
            ref bool found,
            Bounds next)
        {
            if (!found)
            {
                result = next;
                found = true;
            }
            else
            {
                result.Encapsulate(next);
            }
        }

        private static NavMeshRegion FindLargestConnectedNavMeshRegion()
        {
            NavMeshTriangulation triangulation = NavMesh.CalculateTriangulation();
            Require(triangulation.vertices.Length > 0 && triangulation.indices.Length >= 3,
                "Stage6 baked NavMesh has no triangles.");

            DisjointSet sets = new DisjointSet(triangulation.vertices.Length);
            Dictionary<Vector3Int, int> coordinateOwners =
                new Dictionary<Vector3Int, int>();
            for (int i = 0; i < triangulation.vertices.Length; i++)
            {
                Vector3 vertex = triangulation.vertices[i];
                Vector3Int coordinateKey = new Vector3Int(
                    Mathf.RoundToInt(vertex.x * 1000f),
                    Mathf.RoundToInt(vertex.y * 1000f),
                    Mathf.RoundToInt(vertex.z * 1000f));
                if (coordinateOwners.TryGetValue(coordinateKey, out int owner))
                {
                    sets.Union(i, owner);
                }
                else
                {
                    coordinateOwners.Add(coordinateKey, i);
                }
            }

            int triangleCount = triangulation.indices.Length / 3;
            for (int i = 0; i < triangleCount; i++)
            {
                int a = triangulation.indices[i * 3];
                int b = triangulation.indices[i * 3 + 1];
                int c = triangulation.indices[i * 3 + 2];
                sets.Union(a, b);
                sets.Union(b, c);
            }

            Dictionary<int, int> componentCounts = new Dictionary<int, int>();
            int largestRoot = -1;
            int largestCount = 0;
            for (int i = 0; i < triangleCount; i++)
            {
                int root = sets.Find(triangulation.indices[i * 3]);
                componentCounts.TryGetValue(root, out int count);
                count++;
                componentCounts[root] = count;
                if (count > largestCount)
                {
                    largestCount = count;
                    largestRoot = root;
                }
            }

            List<Vector3> candidates = new List<Vector3>();
            HashSet<Vector3Int> candidateKeys = new HashSet<Vector3Int>();
            Bounds bounds = default;
            bool foundBounds = false;
            for (int i = 0; i < triangleCount; i++)
            {
                int ia = triangulation.indices[i * 3];
                if (sets.Find(ia) != largestRoot)
                {
                    continue;
                }

                int ib = triangulation.indices[i * 3 + 1];
                int ic = triangulation.indices[i * 3 + 2];
                Vector3 a = triangulation.vertices[ia];
                Vector3 b = triangulation.vertices[ib];
                Vector3 c = triangulation.vertices[ic];
                EncapsulatePoint(ref bounds, ref foundBounds, a);
                EncapsulatePoint(ref bounds, ref foundBounds, b);
                EncapsulatePoint(ref bounds, ref foundBounds, c);

                Vector3 centroid = (a + b + c) / 3f;
                if (!NavMesh.SamplePosition(
                        centroid,
                        out NavMeshHit navHit,
                        0.5f,
                        NavMesh.AllAreas) ||
                    !NavMesh.FindClosestEdge(
                        navHit.position,
                        out NavMeshHit edgeHit,
                        NavMesh.AllAreas) ||
                    edgeHit.distance < 0.28f)
                {
                    continue;
                }

                Vector3Int key = new Vector3Int(
                    Mathf.RoundToInt(navHit.position.x * 3f),
                    Mathf.RoundToInt(navHit.position.y * 3f),
                    Mathf.RoundToInt(navHit.position.z * 3f));
                if (candidateKeys.Add(key))
                {
                    candidates.Add(navHit.position);
                }
            }

            if (candidates.Count < 18)
            {
                AddVertexCandidates(
                    triangulation,
                    sets,
                    largestRoot,
                    candidates,
                    candidateKeys);
            }

            Debug.Log(
                $"Stage6 connected NavMesh analysis: global vertices=" +
                $"{triangulation.vertices.Length}, indices={triangulation.indices.Length}, " +
                $"components={componentCounts.Count}, largest triangles={largestCount}, " +
                $"safe candidates={candidates.Count}, bounds={bounds}.");
            Require(foundBounds && candidates.Count >= 10,
                $"Stage6 largest NavMesh region has only {candidates.Count} safe candidates.");
            Require(bounds.size.y >= 0.35f,
                $"Stage6 NavMesh did not preserve multiple rooftop elevations: {bounds}");
            return new NavMeshRegion(bounds, candidates, largestCount);
        }

        private static void AddVertexCandidates(
            NavMeshTriangulation triangulation,
            DisjointSet sets,
            int componentRoot,
            List<Vector3> candidates,
            HashSet<Vector3Int> candidateKeys)
        {
            for (int i = 0; i < triangulation.vertices.Length; i++)
            {
                if (sets.Find(i) != componentRoot ||
                    !NavMesh.SamplePosition(
                        triangulation.vertices[i],
                        out NavMeshHit navHit,
                        0.4f,
                        NavMesh.AllAreas) ||
                    !NavMesh.FindClosestEdge(
                        navHit.position,
                        out NavMeshHit edgeHit,
                        NavMesh.AllAreas) ||
                    edgeHit.distance < 0.12f)
                {
                    continue;
                }

                Vector3Int key = new Vector3Int(
                    Mathf.RoundToInt(navHit.position.x * 3f),
                    Mathf.RoundToInt(navHit.position.y * 3f),
                    Mathf.RoundToInt(navHit.position.z * 3f));
                if (candidateKeys.Add(key))
                {
                    candidates.Add(navHit.position);
                }
            }
        }

        private static void EncapsulatePoint(
            ref Bounds bounds,
            ref bool found,
            Vector3 point)
        {
            if (!found)
            {
                bounds = new Bounds(point, Vector3.zero);
                found = true;
            }
            else
            {
                bounds.Encapsulate(point);
            }
        }

        private static EncounterLayout PositionEncounterOnBakedNavigation(
            NavMeshRegion region)
        {
            List<Vector3> used = new List<Vector3>();
            Bounds bounds = region.Bounds;
            float spanX = Mathf.Max(0.01f, bounds.size.x);
            float spanY = Mathf.Max(0.01f, bounds.size.y);
            float spanZ = Mathf.Max(0.01f, bounds.size.z);

            Vector3 player = ChooseCandidate(
                region.Candidates,
                used,
                p =>
                    Normalized(p.z, bounds.min.z, spanZ) * 3f +
                    Normalized(p.y, bounds.min.y, spanY) * 1.5f +
                    Mathf.Abs(p.x - bounds.center.x) / spanX,
                0f,
                p => IsCameraSafeEncounterEntry(p, bounds),
                "Player south/lower entry");
            used.Add(player);
            Physics.SyncTransforms();

            Vector3 west = ChooseCandidate(
                region.Candidates,
                used,
                p =>
                    Normalized(p.x, bounds.min.x, spanX) * 3f -
                    Vector3.Distance(player, p) * 0.03f +
                    Mathf.Abs(p.z - bounds.center.z) / spanZ,
                4.5f,
                p => HasClearSight(player, p),
                "Enemy West firing line");
            used.Add(west);

            Vector3 east = ChooseCandidate(
                region.Candidates,
                used,
                p =>
                    (1f - Normalized(p.x, bounds.min.x, spanX)) * 3f -
                    Vector3.Distance(player, p) * 0.03f +
                    Mathf.Abs(p.z - bounds.center.z) / spanZ,
                4.5f,
                p => FiringLineAngle(player, west, p) >= 18f,
                "Enemy East side cover");
            used.Add(east);

            Vector3 north = ChooseCandidate(
                region.Candidates,
                used,
                p =>
                    (1f - Normalized(p.z, bounds.min.z, spanZ)) * 2.8f +
                    (1f - Normalized(p.y, bounds.min.y, spanY)) * 2.2f +
                    Mathf.Abs(p.x - bounds.center.x) / spanX,
                4.5f,
                p => FiringLineAngle(player, west, p) >= 14f &&
                     FiringLineAngle(player, east, p) >= 14f,
                "Enemy North high firing line");
            used.Add(north);

            Vector3 center = ChooseCandidate(
                region.Candidates,
                used,
                p => HorizontalDistance(p, bounds.center) +
                    Mathf.Abs(p.y - bounds.center.y) * 0.3f,
                3f,
                null,
                "Enemy Center connector");
            used.Add(center);

            Vector3 southTarget = Vector3.Lerp(player, center, 0.48f);
            Vector3 south = ChooseCandidate(
                region.Candidates,
                used,
                p => HorizontalDistance(p, southTarget) +
                    Mathf.Abs(p.y - player.y) * 0.25f,
                2.5f,
                p => HorizontalDistance(p, player) <=
                     Mathf.Max(9f, bounds.size.z * 0.48f),
                "Enemy South approach");
            used.Add(south);

            Vector3 pistol = ChoosePickupCandidate(
                region.Candidates,
                used,
                player,
                -1f,
                "Pistol Pickup");
            used.Add(pistol);
            Vector3 shotgun = ChoosePickupCandidate(
                region.Candidates,
                used,
                player,
                1f,
                "Shotgun Pickup");

            Vector3 encounterCenter = (west + east + north + center + south) / 5f;
            SetActorPose("Player", player, encounterCenter);
            SetActorPose("Enemy West", west, player);
            SetActorPose("Enemy Center", center, player);
            SetActorPose("Enemy East", east, player);
            SetActorPose("Enemy North Gunner", north, player);
            SetActorPose("Enemy South Chaser", south, player);
            SetPickupPose("Pistol Pickup", pistol, -18f);
            SetPickupPose("Shotgun Pickup", shotgun, 18f);
            Physics.SyncTransforms();

            return new EncounterLayout(
                player,
                west,
                center,
                east,
                north,
                south,
                pistol,
                shotgun);
        }

        private static float Normalized(float value, float minimum, float span)
        {
            return Mathf.Clamp01((value - minimum) / span);
        }

        private static bool IsCameraSafeEncounterEntry(
            Vector3 candidate,
            Bounds bounds)
        {
            // The Stage5-scale camera must clamp near the outer NavMesh edge.
            // Reserve an inset here so its initial target cannot place the
            // player at the edge of the rendered combat frame.
            float horizontalInset = Mathf.Min(3f, bounds.extents.x * 0.25f);
            float depthInset = Mathf.Min(3f, bounds.extents.z * 0.25f);
            return candidate.x >= bounds.min.x + horizontalInset &&
                   candidate.x <= bounds.max.x - horizontalInset &&
                   candidate.z >= bounds.min.z + depthInset &&
                   candidate.z <= bounds.max.z - depthInset;
        }

        private static Vector3 ChooseCandidate(
            List<Vector3> candidates,
            List<Vector3> used,
            Func<Vector3, float> score,
            float minimumSeparation,
            Predicate<Vector3> predicate,
            string subject)
        {
            Vector3 best = default;
            float bestScore = float.PositiveInfinity;
            bool found = false;
            for (int i = 0; i < candidates.Count; i++)
            {
                Vector3 candidate = candidates[i];
                if (!IsSeparated(candidate, used, minimumSeparation) ||
                    (predicate != null && !predicate(candidate)))
                {
                    continue;
                }

                float candidateScore = score(candidate);
                if (!found || candidateScore < bestScore)
                {
                    best = candidate;
                    bestScore = candidateScore;
                    found = true;
                }
            }

            if (!found && predicate != null)
            {
                return ChooseCandidate(
                    candidates,
                    used,
                    score,
                    minimumSeparation,
                    null,
                    subject + " fallback");
            }

            Require(found,
                $"Stage6 could not find a NavMesh candidate for {subject}.");
            return best;
        }

        private static Vector3 ChoosePickupCandidate(
            List<Vector3> candidates,
            List<Vector3> used,
            Vector3 player,
            float side,
            string subject)
        {
            return ChooseCandidate(
                candidates,
                used,
                p =>
                    Mathf.Abs(HorizontalDistance(p, player) - 2.1f) +
                    (side < 0f
                        ? Mathf.Max(0f, p.x - player.x)
                        : Mathf.Max(0f, player.x - p.x)) +
                    Mathf.Abs(p.y - player.y) * 2f,
                0.8f,
                p => HorizontalDistance(p, player) >= 0.9f &&
                     HorizontalDistance(p, player) <= 4.2f &&
                     Mathf.Abs(p.y - player.y) <= 0.35f,
                subject);
        }

        private static bool IsSeparated(
            Vector3 candidate,
            List<Vector3> used,
            float minimumSeparation)
        {
            for (int i = 0; i < used.Count; i++)
            {
                if (HorizontalDistance(candidate, used[i]) < minimumSeparation)
                {
                    return false;
                }
            }

            return true;
        }

        private static float HorizontalDistance(Vector3 a, Vector3 b)
        {
            return Vector2.Distance(
                new Vector2(a.x, a.z),
                new Vector2(b.x, b.z));
        }

        private static float FiringLineAngle(
            Vector3 player,
            Vector3 first,
            Vector3 second)
        {
            Vector3 a = first - player;
            Vector3 b = second - player;
            a.y = 0f;
            b.y = 0f;
            return Vector3.Angle(a, b);
        }

        private static bool HasClearSight(Vector3 fromNav, Vector3 toNav)
        {
            Vector3 origin = fromNav + Vector3.up * 0.82f;
            Vector3 target = toNav + Vector3.up * 0.82f;
            Vector3 direction = target - origin;
            return !Physics.Raycast(
                origin,
                direction.normalized,
                direction.magnitude,
                1 << VisionObstacleLayer,
                QueryTriggerInteraction.Ignore);
        }

        private static void SetActorPose(
            string name,
            Vector3 navPosition,
            Vector3 facePosition)
        {
            GameObject target = FindSceneRoot(SceneManager.GetActiveScene(), name);
            Require(target != null, $"Stage6 gameplay object is missing: {name}");
            Vector3 facing = facePosition - navPosition;
            facing.y = 0f;
            Quaternion rotation = facing.sqrMagnitude <= 0.001f
                ? Quaternion.identity
                : Quaternion.LookRotation(facing.normalized, Vector3.up);
            target.transform.SetPositionAndRotation(
                navPosition + Vector3.up * ActorRootHeight,
                rotation);
        }

        private static void SetPickupPose(
            string name,
            Vector3 navPosition,
            float yaw)
        {
            GameObject target = FindSceneRoot(SceneManager.GetActiveScene(), name);
            Require(target != null, $"Stage6 pickup is missing: {name}");
            target.transform.SetPositionAndRotation(
                navPosition + Vector3.up * PickupHeight,
                Quaternion.Euler(0f, yaw, 0f));
        }

        private static void ConfigureCombatIdentityRings()
        {
            string[] owners =
            {
                "Player",
                "Enemy West",
                "Enemy Center",
                "Enemy East",
                "Enemy North Gunner",
                "Enemy South Chaser"
            };
            Material[] materials =
            {
                AssetDatabase.LoadAssetAtPath<Material>(PlayerRingMaterialPath),
                AssetDatabase.LoadAssetAtPath<Material>(RangedRingMaterialPath),
                AssetDatabase.LoadAssetAtPath<Material>(ChaserRingMaterialPath),
                AssetDatabase.LoadAssetAtPath<Material>(RangedRingMaterialPath),
                AssetDatabase.LoadAssetAtPath<Material>(RangedRingMaterialPath),
                AssetDatabase.LoadAssetAtPath<Material>(ChaserRingMaterialPath)
            };
            Scene scene = SceneManager.GetActiveScene();
            for (int i = 0; i < owners.Length; i++)
            {
                GameObject owner = FindSceneRoot(scene, owners[i]);
                Transform ring = owner == null
                    ? null
                    : FindDirectChild(owner.transform, "Combat Identity Ring");
                Require(owner != null && ring != null,
                    $"Stage6 combat identity ring is missing for {owners[i]}.");
                Require(materials[i] != null,
                    $"Stage6 combat identity material is missing for {owners[i]}.");
                ring.position = new Vector3(
                    owner.transform.position.x,
                    owner.transform.position.y - ActorRootHeight + 0.025f,
                    owner.transform.position.z);
                Renderer renderer = ring.GetComponent<Renderer>();
                Require(renderer != null,
                    $"Stage6 combat identity renderer is missing for {owners[i]}.");
                renderer.sharedMaterial = materials[i];
                renderer.shadowCastingMode = ShadowCastingMode.Off;
                renderer.receiveShadows = false;
                renderer.lightProbeUsage = LightProbeUsage.BlendProbes;
                renderer.reflectionProbeUsage = ReflectionProbeUsage.BlendProbes;
                renderer.motionVectorGenerationMode =
                    MotionVectorGenerationMode.Object;
                EditorUtility.SetDirty(renderer);
            }
        }

        private static void ConfigureRooftopCamera(
            NavMeshSurface surface,
            Bounds navBounds)
        {
            Camera camera = FindActiveGameplayCamera(SceneManager.GetActiveScene());
            TopDownCameraController controller = camera == null
                ? null
                : camera.GetComponent<TopDownCameraController>();
            Require(camera != null && controller != null,
                "Stage6 requires the moved gameplay camera rig.");

            CalculateCombatReadableCameraFraming(
                navBounds,
                out Vector3 offset,
                out float fieldOfView);

            Transform player = FindSceneRoot(
                SceneManager.GetActiveScene(),
                "Player").transform;
            Vector3 focusOffset = CalculateCombatReadableFocusOffset(
                navBounds,
                player.position);
            SerializedObject cameraSettings = new SerializedObject(controller);
            cameraSettings.FindProperty("cameraOffset").vector3Value = offset;
            cameraSettings.FindProperty("cameraFocusOffset").vector3Value = focusOffset;
            cameraSettings.FindProperty("aimLeadDistance").floatValue =
                Stage5StyleCameraAimLeadDistance;
            cameraSettings.FindProperty("constrainToBounds").boolValue = true;
            cameraSettings.FindProperty("cameraBounds").boundsValue = navBounds;
            cameraSettings.ApplyModifiedPropertiesWithoutUndo();
            camera.fieldOfView = fieldOfView;
            camera.nearClipPlane = Mathf.Min(camera.nearClipPlane, 0.2f);
            camera.farClipPlane = Mathf.Max(camera.farClipPlane, 500f);
            controller.SnapToTarget();

            Require(!Physics.CheckSphere(
                    camera.transform.position,
                    0.2f,
                    (1 << 0) | (1 << VisionObstacleLayer),
                    QueryTriggerInteraction.Ignore),
                $"Stage6 camera starts inside rooftop geometry at {camera.transform.position}.");
            EditorUtility.SetDirty(camera);
            EditorUtility.SetDirty(controller);
            EditorUtility.SetDirty(surface);
            Debug.Log(
                $"Stage6 Stage5-style camera from NavMesh bounds {navBounds}: " +
                $"offset={offset}, focusOffset={focusOffset}, FOV={fieldOfView:0.0}, " +
                "constrained=true.");
        }

        private static void CalculateCombatReadableCameraFraming(
            Bounds navBounds,
            out Vector3 offset,
            out float fieldOfView)
        {
            offset = new Vector3(
                0f,
                Stage5StyleCameraHeight,
                -Stage5StyleCameraBackwardDistance);
            fieldOfView = Stage5StyleCameraFieldOfView;
        }

        private static Vector3 CalculateCombatReadableFocusOffset(
            Bounds navBounds,
            Vector3 playerPosition)
        {
            return new Vector3(0f, 0f, Stage5StyleCameraFocusZ);
        }

        private static void AddStage6ToBuildSettings()
        {
            List<EditorBuildSettingsScene> existing =
                new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            List<EditorBuildSettingsScene> ordered =
                new List<EditorBuildSettingsScene>();
            string[] expected =
            {
                "Assets/_Project/Scenes/Stage1.unity",
                "Assets/_Project/Scenes/Stage2.unity",
                "Assets/_Project/Scenes/Stage3.unity",
                "Assets/_Project/Scenes/Stage4.unity",
                Stage5ScenePath,
                Stage6ScenePath
            };
            for (int i = 0; i < expected.Length; i++)
            {
                if (i == expected.Length - 1 ||
                    AssetDatabase.LoadAssetAtPath<SceneAsset>(expected[i]) != null)
                {
                    ordered.Add(new EditorBuildSettingsScene(expected[i], true));
                }
            }

            for (int i = 0; i < existing.Count; i++)
            {
                bool stageScene = false;
                for (int j = 0; j < expected.Length; j++)
                {
                    if (existing[i].path == expected[j])
                    {
                        stageScene = true;
                        break;
                    }
                }

                if (!stageScene)
                {
                    ordered.Add(existing[i]);
                }
            }

            EditorBuildSettings.scenes = ordered.ToArray();
        }

        private static void ValidateStage6Scene(
            Scene scene,
            DemoSnapshot sourceSnapshot)
        {
            EditorSceneManager.SetActiveScene(scene);
            GameObject environmentRoot = FindSceneRoot(scene, EnvironmentRootName);
            PlayerHealth player = FindSceneComponent<PlayerHealth>(scene);
            DeadlineController deadline = FindSceneComponent<DeadlineController>(scene);
            WorldTimeController worldTime = FindSceneComponent<WorldTimeController>(scene);
            StageController stage = FindSceneComponent<StageController>(scene);
            StageReplayController replay = FindSceneComponent<StageReplayController>(scene);
            Stage6PerformanceController performance =
                FindSceneComponent<Stage6PerformanceController>(scene);
            NavMeshSurface surface = FindSceneComponent<NavMeshSurface>(scene);
            EnemyHealth[] enemies = FindSceneComponents<EnemyHealth>(scene);
            EnemyMotor[] motors = FindSceneComponents<EnemyMotor>(scene);
            EnemyShooter[] shooters = FindSceneComponents<EnemyShooter>(scene);
            EnemyChaser[] chasers = FindSceneComponents<EnemyChaser>(scene);
            WeaponPickup[] pickups = FindSceneComponents<WeaponPickup>(scene);
            CharacterVisualController[] visualControllers =
                FindSceneComponents<CharacterVisualController>(scene);

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
            Bounds navBounds = CalculateBounds(triangulation.vertices);
            NavMeshRegion combatRegion = FindLargestConnectedNavMeshRegion();
            int activeCameraCount = CountActiveCameras(scene);
            int visualNameCount = CountNamedVisuals(scene, OverlookVisualNames);
            int visionObstacleCount = environmentRoot == null
                ? 0
                : CountEnabledCollidersOnLayer(
                    environmentRoot.transform,
                    VisionObstacleLayer);
            int environmentRendererCount = environmentRoot == null
                ? 0
                : environmentRoot.GetComponentsInChildren<Renderer>(true).Length;
            int environmentPrefabCount = environmentRoot == null
                ? 0
                : CountOutermostPrefabRoots(environmentRoot.transform);
            int environmentPointLights = environmentRoot == null
                ? 0
                : CountLights(environmentRoot.transform, LightType.Point);
            int environmentReflectionProbes = environmentRoot == null
                ? 0
                : environmentRoot.GetComponentsInChildren<ReflectionProbe>(true).Length;
            string missingReference = FindFirstMissingReference(scene);

            Require(scene.path == Stage6ScenePath,
                $"Unexpected Stage6 scene path: {scene.path}");
            Require(environmentRoot != null,
                "Stage6 Neon Overlook environment root is missing.");
            Require(environmentRoot != null &&
                    environmentRoot.GetComponent<ReplayExcluded>() != null,
                "Stage6 static environment is not excluded from replay tracks.");

            ValidateRequiredEnvironmentHierarchy(environmentRoot, sourceSnapshot);
            Require(environmentRendererCount == sourceSnapshot.RendererCount,
                $"Stage6 preserved {environmentRendererCount} environment renderers; " +
                $"source has {sourceSnapshot.RendererCount}.");
            Require(environmentPrefabCount == sourceSnapshot.OutermostPrefabCount,
                $"Stage6 preserved {environmentPrefabCount} outermost prefab instances; " +
                $"source has {sourceSnapshot.OutermostPrefabCount}.");
            Require(environmentPointLights == sourceSnapshot.PointLightCount,
                $"Stage6 preserved {environmentPointLights} point lights; " +
                $"source has {sourceSnapshot.PointLightCount}.");
            Require(environmentReflectionProbes == sourceSnapshot.ReflectionProbeCount,
                $"Stage6 preserved {environmentReflectionProbes} reflection probes; " +
                $"source has {sourceSnapshot.ReflectionProbeCount}.");

            RenderSettingsSnapshot stage6RenderSettings =
                sourceSnapshot.Stage6RenderSettings ?? CaptureRenderSettings();
            Require(stage6RenderSettings.Matches(sourceSnapshot.RenderSettings),
                "Stage6 did not preserve the demo skybox, fog, ambient, or reflection settings.");
            ValidateDirectionalLighting(environmentRoot, sourceSnapshot.Directional);
            ValidateWorldTimeVisualFeedback(scene);
            ValidateStage6Performance(scene, environmentRoot, player, replay, performance);

            Require(activeCameraCount == 1,
                $"Stage6 has {activeCameraCount} active cameras instead of one.");
            Require(player != null &&
                    player.GetComponent<PlayerMovement>() != null &&
                    player.GetComponent<PlayerCombat>() != null,
                "Stage6 player gameplay root did not initialize structurally.");
            ValidateCombatReadableCamera(scene, player.transform, combatRegion.Bounds);
            ValidateStage6ElevationMovement(player, motors, environmentRoot, navBounds);
            ValidateFurnitureNavMeshExclusion(environmentRoot, "Stage6");
            ValidateOffscreenBackgroundCars(environmentRoot);
            Require(enemies.Length == 5 && motors.Length == 5,
                $"Stage6 enemies={enemies.Length}, motors={motors.Length}; expected 5 each.");
            Require(shooters.Length == 3 && chasers.Length == 2,
                $"Stage6 ranged={shooters.Length}, chasers={chasers.Length}; expected 3/2.");
            Require(pickups.Length == 2,
                $"Stage6 has {pickups.Length} weapon pickups instead of 2.");
            Require(charges != null && charges.intValue == DeadlineCharges,
                $"Stage6 Deadline maximum charges are {charges?.intValue} instead of 2.");
            Require(worldTime != null && worldTime.enabled &&
                    worldTime.gameObject.activeInHierarchy,
                "Stage6 WorldTimeController is not active.");
            Require(stage != null && stage.enabled &&
                    stage.gameObject.activeInHierarchy &&
                    stage.CurrentState == StageController.StageState.Active,
                "Stage6 StageController is not active.");
            Require(replay != null && replay.enabled && replay.gameObject.activeInHierarchy,
                "Stage6 StageReplayController is not active.");
            Require(surface != null && navigationPath == Stage6NavigationPath,
                $"Stage6 uses the wrong NavMesh data: {navigationPath}");
            Require(triangulation.vertices.Length > 0 && triangulation.indices.Length > 0,
                "Stage6 baked NavMesh has no triangles.");
            Require(navBounds.size.y >= 0.35f,
                $"Stage6 baked NavMesh has no multi-elevation span: {navBounds}");
            ValidateNavMeshWithinSurface(surface, triangulation.vertices);
            Require(visualNameCount == 6 && visualControllers.Length == 6,
                $"Stage6 overlook visuals={visualNameCount}, " +
                $"visual controllers={visualControllers.Length}; expected 6 each.");
            Require(visionObstacleCount > 0,
                "Stage6 has no enabled structural VisionObstacle colliders.");
            Require(string.IsNullOrEmpty(missingReference),
                "Stage6 contains a missing script or object reference: " + missingReference);

            RequireOnNavMesh(player.transform.position, "Player");
            for (int i = 0; i < enemies.Length; i++)
            {
                RequireOnNavMesh(enemies[i].transform.position, enemies[i].name);
                RequireCompletePath(
                    player.transform.position,
                    enemies[i].transform.position,
                    enemies[i].name);
            }

            Require(HasClearInitialSight(player.gameObject, enemies),
                "Stage6 player cannot initially see any enemy through structural cover.");
            ValidateDistinctShooterLines(player.transform.position, shooters);
            ValidatePickupPlacement(pickups);
            ValidateCombatIdentityRings(scene);
            ValidateVisualChildren(scene);
            ValidateBuildOrder();

            Debug.Log(
                $"Stage6 static validation: renderers={environmentRendererCount}/" +
                $"{sourceSnapshot.RendererCount}, prefabs={environmentPrefabCount}/" +
                $"{sourceSnapshot.OutermostPrefabCount}, NavMesh vertices=" +
                $"{triangulation.vertices.Length}, indices={triangulation.indices.Length}, " +
                $"bounds={navBounds}, complete player paths=5/5.");
        }

        private static void ValidateStage6ElevationMovement(
            PlayerHealth player,
            EnemyMotor[] motors,
            GameObject environmentRoot,
            Bounds navBounds)
        {
            Require(navBounds.size.y >= 0.35f,
                $"Stage6 baked NavMesh has no usable elevation span: {navBounds}");
            Require(player != null && motors.Length == 5,
                "Stage6 elevation validation requires the player and five enemy motors.");

            ValidateActorGroundMovement(player.gameObject);
            for (int i = 0; i < motors.Length; i++)
            {
                ValidateActorGroundMovement(motors[i].gameObject);
            }

            Collider[] colliders = environmentRoot.GetComponentsInChildren<Collider>(true);
            int disabledTraversalColliders = 0;
            for (int i = 0; i < colliders.Length; i++)
            {
                if (!colliders[i].enabled && IsRuntimeTraversalCollider(colliders[i]))
                {
                    disabledTraversalColliders++;
                }
            }

            Require(disabledTraversalColliders > 0,
                "Stage6 did not preserve disabled runtime stair/step colliders.");
        }

        internal static void ValidateFurnitureNavMeshExclusion(
            GameObject environmentRoot,
            string stageName)
        {
            const float sampleDistance = 0.08f;
            Collider[] colliders =
                environmentRoot.GetComponentsInChildren<Collider>(true);
            int validated = 0;
            for (int i = 0; i < colliders.Length; i++)
            {
                Collider collider = colliders[i];
                if (!collider.enabled || !collider.gameObject.activeInHierarchy ||
                    !ShouldExcludeColliderFromNavMesh(collider))
                {
                    continue;
                }

                Bounds bounds = collider.bounds;
                Vector3 topProbe = new Vector3(
                    bounds.center.x,
                    bounds.max.y,
                    bounds.center.z);
                // A hanging or open-bottom prop may validly have ground below
                // it, but its upper surface must never be a NavMesh platform.
                Require(!NavMesh.SamplePosition(
                            topProbe,
                            out _,
                            sampleDistance,
                            NavMesh.AllAreas),
                    $"{stageName} NavMesh still enters furniture collider " +
                    $"'{GetPath(collider.transform)}'.");
                validated++;
            }

            Require(validated > 0,
                $"{stageName} has no furniture collider to validate against NavMesh.");
        }

        private static void ValidateActorGroundMovement(GameObject actor)
        {
            Rigidbody body = actor.GetComponent<Rigidbody>();
            NavMeshGroundMovement movement = actor.GetComponent<NavMeshGroundMovement>();
            Require(body != null && movement != null && !body.useGravity &&
                    (body.constraints & RigidbodyConstraints.FreezePositionY) == 0,
                $"Stage6 actor is not configured for NavMesh height traversal: {actor.name}.");
        }

        private static void ValidateOffscreenBackgroundCars(GameObject environmentRoot)
        {
            Transform backgroundFx = FindDescendant(
                environmentRoot.transform,
                "Background_FX");
            Require(backgroundFx != null,
                "Stage6 background vehicle validation requires Background_FX.");

            Transform[] transforms = backgroundFx.GetComponentsInChildren<Transform>(true);
            int carCount = 0;
            for (int i = 0; i < transforms.Length; i++)
            {
                Transform transform = transforms[i];
                if (!transform.name.StartsWith(
                        "FX_Background_Cars_01",
                        StringComparison.Ordinal))
                {
                    continue;
                }

                carCount++;
                Require(!transform.gameObject.activeSelf,
                    $"Stage6 offscreen background car remains active: {transform.name}.");
            }

            Require(carCount == OffscreenBackgroundCarCount,
                $"Stage6 found {carCount} background cars instead of " +
                $"{OffscreenBackgroundCarCount}.");
        }

        private static void ValidateRequiredEnvironmentHierarchy(
            GameObject environmentRoot,
            DemoSnapshot sourceSnapshot)
        {
            for (int i = 0; i < RequiredEnvironmentNames.Length; i++)
            {
                string name = RequiredEnvironmentNames[i];
                List<Transform> matches = FindDescendants(environmentRoot.transform, name);
                int expectedCount = sourceSnapshot.RequiredNameCounts[name];
                Require(matches.Count == expectedCount,
                    $"Stage6 environment '{name}' count={matches.Count}; " +
                    $"source count={expectedCount}.");
                Require(matches.Count > 0 &&
                        matches[0].gameObject.activeSelf ==
                        sourceSnapshot.RequiredActiveStates[name],
                    $"Stage6 changed the source active state of '{name}'.");
            }

            Transform backgroundCity = FindDescendant(
                environmentRoot.transform,
                "BackgroundCity");
            Transform backgroundPlanes = FindDescendant(
                environmentRoot.transform,
                "Background_Planes");
            Transform backgroundFx = FindDescendant(
                environmentRoot.transform,
                "Background_FX");
            Require(backgroundCity != null && backgroundPlanes != null &&
                    backgroundFx != null &&
                    backgroundPlanes.IsChildOf(backgroundCity) &&
                    backgroundFx.IsChildOf(backgroundCity),
                "Stage6 did not preserve the demo BackgroundCity child hierarchy.");
        }

        private static void ValidateDirectionalLighting(
            GameObject environmentRoot,
            DirectionalLightSettings expected)
        {
            Light keyLight = FindSceneRoot(
                    SceneManager.GetActiveScene(),
                    "Directional Key Light")
                ?.GetComponent<Light>();
            Require(keyLight != null && keyLight.isActiveAndEnabled,
                "Stage6 Directional Key Light is not active.");
            Require(ColorApproximately(keyLight.color, expected.Color) &&
                    Mathf.Approximately(keyLight.intensity, expected.Intensity) &&
                    Quaternion.Angle(keyLight.transform.rotation, expected.Rotation) < 0.05f &&
                    keyLight.shadows == expected.Shadows &&
                    Mathf.Approximately(keyLight.shadowStrength, expected.ShadowStrength),
                "Stage6 gameplay Directional Key Light does not match the measured demo light.");

            Transform urp = FindDescendant(environmentRoot.transform, "Lighting (URP)");
            Light[] demoLights = urp.GetComponentsInChildren<Light>(true);
            for (int i = 0; i < demoLights.Length; i++)
            {
                if (demoLights[i].type == LightType.Directional)
                {
                    Require(!demoLights[i].enabled,
                        "Stage6 left a duplicate demo URP Directional Light enabled.");
                }
            }
        }

        private static void ValidateWorldTimeVisualFeedback(Scene scene)
        {
            Camera camera = FindActiveGameplayCamera(scene);
            WorldTimeVisualFeedback feedback = camera == null
                ? null
                : camera.GetComponent<WorldTimeVisualFeedback>();
            Require(feedback != null,
                "Stage6 camera is missing WorldTimeVisualFeedback.");
            SerializedObject settings = new SerializedObject(feedback);
            settings.Update();
            Require(settings.FindProperty("preserveSceneRenderSettings").boolValue,
                "Stage6 WorldTimeVisualFeedback does not preserve scene render settings.");
            Require(Mathf.Approximately(
                    settings.FindProperty("mapFillLightIntensity").floatValue,
                    0f) &&
                    settings.FindProperty("mapFillLightPositions").arraySize == 0,
                "Stage6 WorldTimeVisualFeedback still creates map fill lights.");
        }

        private static void ValidateStage6Performance(
            Scene scene,
            GameObject environmentRoot,
            PlayerHealth player,
            StageReplayController replay,
            Stage6PerformanceController performance)
        {
            Require(performance != null && performance.enabled &&
                    performance.gameObject == FindSceneRoot(scene, "Systems"),
                "Stage6 is missing its Systems-bound performance controller.");
            Require(player != null &&
                    performance.EnvironmentRoot == environmentRoot.transform &&
                    performance.Player == player.transform,
                "Stage6 performance controller has stale scene references.");
            Require(Mathf.Approximately(
                        performance.ShadowDistance,
                        Stage6ShadowDistance) &&
                    performance.MaximumShadowCascades == Stage6MaximumShadowCascades &&
                    performance.MaximumShadowResolution == ShadowResolution.Medium &&
                    performance.MaximumShadowedEnvironmentPointLights ==
                        Stage6MaximumShadowedEnvironmentPointLights &&
                    Mathf.Approximately(
                        performance.EnvironmentShadowSelectionInterval,
                        Stage6ShadowSelectionInterval),
                "Stage6 performance controller does not have the required shadow budget.");
            Require(replay != null && replay.UsesOptimizedRendererDiscovery &&
                    replay.RendererDiscoveryRootCount == 9 &&
                    Mathf.Approximately(
                        replay.FallbackRendererDiscoveryInterval,
                        Stage6FallbackRendererDiscoveryInterval),
                "Stage6 replay renderer discovery is not configured for dynamic roots.");

            string[] expectedRoots =
            {
                "Systems",
                "Player",
                "Enemy West",
                "Enemy Center",
                "Enemy East",
                "Enemy North Gunner",
                "Enemy South Chaser",
                "Pistol Pickup",
                "Shotgun Pickup"
            };
            for (int i = 0; i < expectedRoots.Length; i++)
            {
                GameObject root = FindSceneRoot(scene, expectedRoots[i]);
                Require(root != null && replay.HasRendererDiscoveryRoot(root.transform),
                    "Stage6 replay is missing dynamic discovery root: " +
                    expectedRoots[i]);
            }
        }

        private static void ValidateCombatReadableCamera(
            Scene scene,
            Transform player,
            Bounds navBounds)
        {
            Camera camera = FindActiveGameplayCamera(scene);
            TopDownCameraController controller = camera == null
                ? null
                : camera.GetComponent<TopDownCameraController>();
            Require(camera != null && controller != null,
                "Stage6 combat-readable camera rig is missing.");

            CalculateCombatReadableCameraFraming(
                navBounds,
                out Vector3 expectedOffset,
                out float expectedFov);
            Vector3 expectedFocusOffset = CalculateCombatReadableFocusOffset(
                navBounds,
                player.position);
            SerializedObject settings = new SerializedObject(controller);
            settings.Update();
            Vector3 actualOffset = settings.FindProperty("cameraOffset").vector3Value;
            Vector3 actualFocusOffset =
                settings.FindProperty("cameraFocusOffset").vector3Value;
            bool constrainToBounds =
                settings.FindProperty("constrainToBounds").boolValue;
            Bounds actualBounds = settings.FindProperty("cameraBounds").boundsValue;
            Require(Vector3.Distance(actualOffset, expectedOffset) < 0.01f &&
                    Vector3.Distance(actualFocusOffset, expectedFocusOffset) < 0.01f &&
                    Mathf.Abs(camera.fieldOfView - expectedFov) < 0.01f,
                $"Stage6 camera framing regressed. offset={actualOffset}, " +
                $"focusOffset={actualFocusOffset}, FOV={camera.fieldOfView:0.0}.");
            Require(Mathf.Abs(actualOffset.y - Stage5StyleCameraHeight) < 0.01f &&
                    Mathf.Abs(camera.fieldOfView - Stage5StyleCameraFieldOfView) < 0.01f,
                "Stage6 camera is no longer using the Stage5-scale framing.");
            Require(constrainToBounds &&
                    Vector3.Distance(actualBounds.center, navBounds.center) < 0.01f &&
                    Vector3.Distance(actualBounds.size, navBounds.size) < 0.01f,
                "Stage6 camera bounds do not match the playable NavMesh region.");

            controller.SnapToTarget();
            Vector3 playerViewport = camera.WorldToViewportPoint(
                player.position + Vector3.up * ActorRootHeight);
            Require(playerViewport.z > 0f &&
                    playerViewport.x >= -0.01f && playerViewport.x <= 1.01f &&
                    playerViewport.y >= -0.01f && playerViewport.y <= 1.01f,
                $"Stage6 player is not visible in the Stage5-scale combat frame: " +
                $"viewport={playerViewport}.");
        }

        private static void ValidateNavMeshWithinSurface(
            NavMeshSurface surface,
            Vector3[] vertices)
        {
            Vector3 center = surface.transform.TransformPoint(surface.center);
            Vector3 half = surface.size * 0.5f + Vector3.one * 0.2f;
            for (int i = 0; i < vertices.Length; i++)
            {
                Vector3 delta = vertices[i] - center;
                Require(Mathf.Abs(delta.x) <= half.x &&
                        Mathf.Abs(delta.y) <= half.y &&
                        Mathf.Abs(delta.z) <= half.z,
                    $"Stage6 NavMesh vertex escaped the playable volume: {vertices[i]}");
            }
        }

        private static void RequireOnNavMesh(Vector3 position, string subject)
        {
            bool found = NavMesh.SamplePosition(
                position,
                out _,
                1.5f,
                NavMesh.AllAreas);
            Require(found,
                $"Stage6 {subject} is not on the baked NavMesh ({position}).");
        }

        private static void RequireCompletePath(
            Vector3 from,
            Vector3 to,
            string subject)
        {
            Require(NavMesh.SamplePosition(
                    from,
                    out NavMeshHit fromHit,
                    1.5f,
                    NavMesh.AllAreas),
                "Stage6 player path origin is not on the NavMesh.");
            Require(NavMesh.SamplePosition(
                    to,
                    out NavMeshHit toHit,
                    1.5f,
                    NavMesh.AllAreas),
                $"Stage6 {subject} path target is not on the NavMesh.");
            NavMeshPath path = new NavMeshPath();
            bool calculated = NavMesh.CalculatePath(
                fromHit.position,
                toHit.position,
                NavMesh.AllAreas,
                path);
            Require(calculated && path.status == NavMeshPathStatus.PathComplete &&
                    path.corners.Length > 0,
                $"Stage6 player cannot reach {subject}; path status={path.status}.");
        }

        private static bool HasClearInitialSight(
            GameObject player,
            EnemyHealth[] enemies)
        {
            for (int i = 0; i < enemies.Length; i++)
            {
                Vector3 origin = player.transform.position + Vector3.up * 0.05f;
                Vector3 target = enemies[i].transform.position + Vector3.up * 0.05f;
                Vector3 direction = target - origin;
                if (!Physics.Raycast(
                        origin,
                        direction.normalized,
                        direction.magnitude,
                        1 << VisionObstacleLayer,
                        QueryTriggerInteraction.Ignore))
                {
                    return true;
                }
            }

            return false;
        }

        private static void ValidateDistinctShooterLines(
            Vector3 player,
            EnemyShooter[] shooters)
        {
            float minimumAngle = 180f;
            for (int i = 0; i < shooters.Length; i++)
            {
                for (int j = i + 1; j < shooters.Length; j++)
                {
                    minimumAngle = Mathf.Min(
                        minimumAngle,
                        FiringLineAngle(
                            player,
                            shooters[i].transform.position,
                            shooters[j].transform.position));
                }
            }

            Require(minimumAngle >= 10f,
                $"Stage6 ranged enemies do not have distinct firing lines; " +
                $"minimum angle={minimumAngle:0.0} degrees.");
        }

        private static void ValidatePickupPlacement(WeaponPickup[] pickups)
        {
            for (int i = 0; i < pickups.Length; i++)
            {
                RequireOnNavMesh(pickups[i].transform.position, pickups[i].name);
                Collider[] overlaps = Physics.OverlapSphere(
                    pickups[i].transform.position + Vector3.up * 0.25f,
                    0.18f,
                    (1 << 0) | (1 << VisionObstacleLayer),
                    QueryTriggerInteraction.Ignore);
                for (int j = 0; j < overlaps.Length; j++)
                {
                    if (overlaps[j].transform.IsChildOf(pickups[i].transform))
                    {
                        continue;
                    }

                    Bounds bounds = overlaps[j].bounds;
                    bool floorLike = bounds.max.y <=
                        pickups[i].transform.position.y + 0.08f;
                    Require(floorLike,
                        $"Stage6 pickup '{pickups[i].name}' overlaps " +
                        $"'{GetPath(overlaps[j].transform)}'.");
                }
            }
        }

        private static void ValidateCombatIdentityRings(Scene scene)
        {
            string[] owners =
            {
                "Player",
                "Enemy West",
                "Enemy Center",
                "Enemy East",
                "Enemy North Gunner",
                "Enemy South Chaser"
            };
            string[] expectedPaths =
            {
                PlayerRingMaterialPath,
                RangedRingMaterialPath,
                ChaserRingMaterialPath,
                RangedRingMaterialPath,
                RangedRingMaterialPath,
                ChaserRingMaterialPath
            };
            for (int i = 0; i < owners.Length; i++)
            {
                GameObject owner = FindSceneRoot(scene, owners[i]);
                Transform ring = owner == null
                    ? null
                    : FindDirectChild(owner.transform, "Combat Identity Ring");
                Renderer renderer = ring == null ? null : ring.GetComponent<Renderer>();
                Material material = renderer == null ? null : renderer.sharedMaterial;
                Require(material != null &&
                        AssetDatabase.GetAssetPath(material) == expectedPaths[i],
                    $"Stage6 inherited a Stage5 identity material for {owners[i]}.");
                Require(renderer.shadowCastingMode == ShadowCastingMode.Off &&
                        !renderer.receiveShadows &&
                        renderer.lightProbeUsage == LightProbeUsage.BlendProbes &&
                        renderer.reflectionProbeUsage == ReflectionProbeUsage.BlendProbes &&
                        renderer.motionVectorGenerationMode ==
                            MotionVectorGenerationMode.Object,
                    $"Stage6 identity renderer settings regressed for {owners[i]}.");
            }
        }

        private static void ValidateVisualChildren(Scene scene)
        {
            string[] owners =
            {
                "Player",
                "Enemy West",
                "Enemy Center",
                "Enemy East",
                "Enemy North Gunner",
                "Enemy South Chaser"
            };
            for (int i = 0; i < owners.Length; i++)
            {
                GameObject owner = FindSceneRoot(scene, owners[i]);
                Transform visual = owner == null
                    ? null
                    : FindDirectChild(owner.transform, OverlookVisualNames[i]);
                Require(visual != null,
                    $"Stage6 visual is missing for {owners[i]}.");
                Require(visual.GetComponentInChildren<EnemyHealth>(true) == null &&
                        visual.GetComponentInChildren<EnemyMotor>(true) == null &&
                        visual.GetComponentInChildren<NavMeshAgent>(true) == null,
                    $"Stage6 visual child {visual.name} contains gameplay components.");

                Collider[] colliders = visual.GetComponentsInChildren<Collider>(true);
                for (int j = 0; j < colliders.Length; j++)
                {
                    Require(!colliders[j].enabled,
                        $"Stage6 visual collider is enabled: {colliders[j].name}");
                }

                Rigidbody[] bodies = visual.GetComponentsInChildren<Rigidbody>(true);
                for (int j = 0; j < bodies.Length; j++)
                {
                    Require(!bodies[j].detectCollisions,
                        $"Stage6 visual Rigidbody still detects collisions: {bodies[j].name}");
                }

                Animator[] animators = visual.GetComponentsInChildren<Animator>(true);
                for (int j = 0; j < animators.Length; j++)
                {
                    Require(!animators[j].enabled && !animators[j].applyRootMotion,
                        $"Stage6 visual Animator is active: {animators[j].name}");
                }
            }
        }

        private static void ValidateBuildOrder()
        {
            string[] expected =
            {
                "Assets/_Project/Scenes/Stage1.unity",
                "Assets/_Project/Scenes/Stage2.unity",
                "Assets/_Project/Scenes/Stage3.unity",
                "Assets/_Project/Scenes/Stage4.unity",
                Stage5ScenePath,
                Stage6ScenePath
            };
            EditorBuildSettingsScene[] scenes = EditorBuildSettings.scenes;
            Require(scenes.Length >= expected.Length,
                "Build settings do not contain Stage1 through Stage6.");
            for (int i = 0; i < expected.Length; i++)
            {
                Require(scenes[i].enabled && scenes[i].path == expected[i],
                    $"Build index {i} is {scenes[i].path}; expected {expected[i]}.");
            }
        }

        private static int CountActiveCameras(Scene scene)
        {
            int count = 0;
            Camera[] cameras = FindSceneComponents<Camera>(scene);
            for (int i = 0; i < cameras.Length; i++)
            {
                if (cameras[i].isActiveAndEnabled)
                {
                    count++;
                }
            }

            return count;
        }

        private static Camera FindActiveGameplayCamera(Scene scene)
        {
            GameObject root = FindSceneRoot(scene, "Main Camera");
            Camera camera = root == null ? null : root.GetComponent<Camera>();
            return camera != null && camera.isActiveAndEnabled ? camera : null;
        }

        private static int CountNamedVisuals(Scene scene, string[] visualNames)
        {
            HashSet<string> names = new HashSet<string>(visualNames);
            int count = 0;
            GameObject[] roots = scene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                Transform[] transforms = roots[i].GetComponentsInChildren<Transform>(true);
                for (int j = 0; j < transforms.Length; j++)
                {
                    if (names.Contains(transforms[j].name))
                    {
                        count++;
                    }
                }
            }

            return count;
        }

        private static int CountEnabledCollidersOnLayer(Transform root, int layer)
        {
            int count = 0;
            Collider[] colliders = root.GetComponentsInChildren<Collider>(true);
            for (int i = 0; i < colliders.Length; i++)
            {
                if (colliders[i].enabled && colliders[i].gameObject.layer == layer)
                {
                    count++;
                }
            }

            return count;
        }

        private static int CountSceneComponents<T>(Scene scene, GameObject excludedRoot)
            where T : Component
        {
            int count = 0;
            GameObject[] roots = scene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                if (roots[i] != excludedRoot)
                {
                    count += roots[i].GetComponentsInChildren<T>(true).Length;
                }
            }

            return count;
        }

        private static int CountSceneLights(
            Scene scene,
            LightType type,
            GameObject excludedRoot)
        {
            int count = 0;
            GameObject[] roots = scene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                if (roots[i] == excludedRoot)
                {
                    continue;
                }

                Light[] lights = roots[i].GetComponentsInChildren<Light>(true);
                for (int j = 0; j < lights.Length; j++)
                {
                    if (lights[j].type == type)
                    {
                        count++;
                    }
                }
            }

            return count;
        }

        private static int CountLights(Transform root, LightType type)
        {
            int count = 0;
            Light[] lights = root.GetComponentsInChildren<Light>(true);
            for (int i = 0; i < lights.Length; i++)
            {
                if (lights[i].type == type)
                {
                    count++;
                }
            }

            return count;
        }

        private static int CountOutermostPrefabRoots(
            Scene scene,
            GameObject excludedRoot)
        {
            int count = 0;
            GameObject[] roots = scene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                if (roots[i] != excludedRoot)
                {
                    count += CountOutermostPrefabRoots(roots[i].transform);
                }
            }

            return count;
        }

        private static int CountOutermostPrefabRoots(Transform root)
        {
            int count = 0;
            Transform[] transforms = root.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < transforms.Length; i++)
            {
                if (PrefabUtility.IsOutermostPrefabInstanceRoot(
                        transforms[i].gameObject))
                {
                    count++;
                }
            }

            return count;
        }

        private static bool IsUnderNamedHierarchy(
            Transform transform,
            Transform stopAt,
            string hierarchyName)
        {
            Transform current = transform;
            while (current != null && current != stopAt)
            {
                if (current.name == hierarchyName)
                {
                    return true;
                }

                current = current.parent;
            }

            return false;
        }

        private static Bounds CalculateBounds(Vector3[] points)
        {
            if (points == null || points.Length == 0)
            {
                return default;
            }

            Bounds bounds = new Bounds(points[0], Vector3.zero);
            for (int i = 1; i < points.Length; i++)
            {
                bounds.Encapsulate(points[i]);
            }

            return bounds;
        }

        private static bool ColorApproximately(Color a, Color b)
        {
            return Mathf.Abs(a.r - b.r) < 0.0001f &&
                   Mathf.Abs(a.g - b.g) < 0.0001f &&
                   Mathf.Abs(a.b - b.b) < 0.0001f &&
                   Mathf.Abs(a.a - b.a) < 0.0001f;
        }

        private static RenderSettingsSnapshot CaptureRenderSettings()
        {
            return new RenderSettingsSnapshot
            {
                SkyboxPath = AssetDatabase.GetAssetPath(RenderSettings.skybox),
                Fog = RenderSettings.fog,
                FogMode = RenderSettings.fogMode,
                FogColor = RenderSettings.fogColor,
                FogDensity = RenderSettings.fogDensity,
                FogStart = RenderSettings.fogStartDistance,
                FogEnd = RenderSettings.fogEndDistance,
                AmbientMode = RenderSettings.ambientMode,
                AmbientSky = RenderSettings.ambientSkyColor,
                AmbientEquator = RenderSettings.ambientEquatorColor,
                AmbientGround = RenderSettings.ambientGroundColor,
                AmbientIntensity = RenderSettings.ambientIntensity,
                ReflectionIntensity = RenderSettings.reflectionIntensity,
                DefaultReflectionMode = RenderSettings.defaultReflectionMode,
                CustomReflectionPath =
                    AssetDatabase.GetAssetPath(RenderSettings.customReflectionTexture)
            };
        }

        private static string FindFirstMissingReference(Scene scene)
        {
            GameObject[] roots = scene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                Transform[] transforms = roots[i].GetComponentsInChildren<Transform>(true);
                for (int j = 0; j < transforms.Length; j++)
                {
                    GameObject gameObject = transforms[j].gameObject;
                    if (GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(gameObject) > 0)
                    {
                        return GetPath(transforms[j]) + " (Missing Script)";
                    }

                    Component[] components = gameObject.GetComponents<Component>();
                    for (int k = 0; k < components.Length; k++)
                    {
                        Component component = components[k];
                        if (component == null)
                        {
                            return GetPath(transforms[j]) + " (Missing Component)";
                        }

                        SerializedObject serialized = new SerializedObject(component);
                        SerializedProperty property = serialized.GetIterator();
                        bool enterChildren = true;
                        while (property.NextVisible(enterChildren))
                        {
                            enterChildren = false;
                            if (property.propertyType ==
                                    SerializedPropertyType.ObjectReference &&
                                property.objectReferenceValue == null &&
                                property.objectReferenceInstanceIDValue != 0)
                            {
                                return GetPath(transforms[j]) + "/" +
                                       component.GetType().Name + "." +
                                       property.propertyPath;
                            }
                        }
                    }
                }
            }

            return string.Empty;
        }

        private static T FindSceneComponent<T>(Scene scene) where T : Component
        {
            T[] components = FindSceneComponents<T>(scene);
            return components.Length > 0 ? components[0] : null;
        }

        private static T[] FindSceneComponents<T>(Scene scene) where T : Component
        {
            List<T> result = new List<T>();
            GameObject[] roots = scene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                result.AddRange(roots[i].GetComponentsInChildren<T>(true));
            }

            return result.ToArray();
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

        private static Transform FindSceneTransform(Scene scene, string name)
        {
            List<Transform> matches = FindSceneTransforms(scene, name);
            return matches.Count > 0 ? matches[0] : null;
        }

        private static List<Transform> FindSceneTransforms(Scene scene, string name)
        {
            List<Transform> result = new List<Transform>();
            GameObject[] roots = scene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                Transform[] transforms = roots[i].GetComponentsInChildren<Transform>(true);
                for (int j = 0; j < transforms.Length; j++)
                {
                    if (transforms[j].name == name)
                    {
                        result.Add(transforms[j]);
                    }
                }
            }

            return result;
        }

        private static Transform FindDescendant(Transform root, string name)
        {
            List<Transform> matches = FindDescendants(root, name);
            return matches.Count > 0 ? matches[0] : null;
        }

        private static List<Transform> FindDescendants(Transform root, string name)
        {
            List<Transform> result = new List<Transform>();
            Transform[] transforms = root.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < transforms.Length; i++)
            {
                if (transforms[i].name == name)
                {
                    result.Add(transforms[i]);
                }
            }

            return result;
        }

        private static Transform FindDirectChild(Transform parent, string name)
        {
            for (int i = 0; i < parent.childCount; i++)
            {
                Transform child = parent.GetChild(i);
                if (child.name == name)
                {
                    return child;
                }
            }

            return null;
        }

        private static string GetPath(Transform transform)
        {
            string path = transform.name;
            while (transform.parent != null)
            {
                transform = transform.parent;
                path = transform.name + "/" + path;
            }

            return path;
        }

        private static void Require(bool condition, string message)
        {
            if (!condition)
            {
                throw new InvalidOperationException(message);
            }
        }

        private sealed class DemoSnapshot
        {
            public CameraSettings Camera;
            public DirectionalLightSettings Directional;
            public int RendererCount;
            public int OutermostPrefabCount;
            public int PointLightCount;
            public int ReflectionProbeCount;
            public Dictionary<string, int> RequiredNameCounts;
            public Dictionary<string, bool> RequiredActiveStates;
            public RenderSettingsSnapshot RenderSettings;
            public RenderSettingsSnapshot Stage6RenderSettings;
        }

        private sealed class DirectionalLightSettings
        {
            public Color Color;
            public float Intensity;
            public Quaternion Rotation;
            public LightShadows Shadows;
            public float ShadowStrength;
            public float ShadowBias;
            public float ShadowNormalBias;
            public float ShadowNearPlane;
            public int CullingMask;
            public int RenderingLayerMask;
            public float BounceIntensity;
            public bool UseColorTemperature;
            public float ColorTemperature;
        }

        private sealed class RenderSettingsSnapshot
        {
            public string SkyboxPath;
            public bool Fog;
            public FogMode FogMode;
            public Color FogColor;
            public float FogDensity;
            public float FogStart;
            public float FogEnd;
            public AmbientMode AmbientMode;
            public Color AmbientSky;
            public Color AmbientEquator;
            public Color AmbientGround;
            public float AmbientIntensity;
            public float ReflectionIntensity;
            public DefaultReflectionMode DefaultReflectionMode;
            public string CustomReflectionPath;

            public bool Matches(RenderSettingsSnapshot other)
            {
                return other != null &&
                       SkyboxPath == other.SkyboxPath &&
                       Fog == other.Fog &&
                       FogMode == other.FogMode &&
                       ColorApproximately(FogColor, other.FogColor) &&
                       Mathf.Approximately(FogDensity, other.FogDensity) &&
                       Mathf.Approximately(FogStart, other.FogStart) &&
                       Mathf.Approximately(FogEnd, other.FogEnd) &&
                       AmbientMode == other.AmbientMode &&
                       ColorApproximately(AmbientSky, other.AmbientSky) &&
                       ColorApproximately(AmbientEquator, other.AmbientEquator) &&
                       ColorApproximately(AmbientGround, other.AmbientGround) &&
                       Mathf.Approximately(AmbientIntensity, other.AmbientIntensity) &&
                       Mathf.Approximately(ReflectionIntensity, other.ReflectionIntensity) &&
                       DefaultReflectionMode == other.DefaultReflectionMode &&
                       CustomReflectionPath == other.CustomReflectionPath;
            }
        }

        private readonly struct CameraSettings
        {
            public CameraSettings(
                CameraClearFlags clearFlags,
                Color backgroundColor)
            {
                ClearFlags = clearFlags;
                BackgroundColor = backgroundColor;
            }

            public CameraClearFlags ClearFlags { get; }
            public Color BackgroundColor { get; }
        }

        private readonly struct ColliderSummary
        {
            public ColliderSummary(int enabledCount, int visionCount)
            {
                EnabledCount = enabledCount;
                VisionCount = visionCount;
            }

            public int EnabledCount { get; }
            public int VisionCount { get; }
        }

        private readonly struct NavMeshRegion
        {
            public NavMeshRegion(
                Bounds bounds,
                List<Vector3> candidates,
                int triangleCount)
            {
                Bounds = bounds;
                Candidates = candidates;
                TriangleCount = triangleCount;
            }

            public Bounds Bounds { get; }
            public List<Vector3> Candidates { get; }
            public int TriangleCount { get; }
        }

        private readonly struct EncounterLayout
        {
            public EncounterLayout(
                Vector3 player,
                Vector3 west,
                Vector3 center,
                Vector3 east,
                Vector3 north,
                Vector3 south,
                Vector3 pistol,
                Vector3 shotgun)
            {
                Player = player;
                West = west;
                Center = center;
                East = east;
                North = north;
                South = south;
                Pistol = pistol;
                Shotgun = shotgun;
            }

            public Vector3 Player { get; }
            public Vector3 West { get; }
            public Vector3 Center { get; }
            public Vector3 East { get; }
            public Vector3 North { get; }
            public Vector3 South { get; }
            public Vector3 Pistol { get; }
            public Vector3 Shotgun { get; }
        }

        private sealed class DisjointSet
        {
            private readonly int[] parent;
            private readonly byte[] rank;

            public DisjointSet(int count)
            {
                parent = new int[count];
                rank = new byte[count];
                for (int i = 0; i < count; i++)
                {
                    parent[i] = i;
                }
            }

            public int Find(int item)
            {
                if (parent[item] != item)
                {
                    parent[item] = Find(parent[item]);
                }

                return parent[item];
            }

            public void Union(int a, int b)
            {
                int rootA = Find(a);
                int rootB = Find(b);
                if (rootA == rootB)
                {
                    return;
                }

                if (rank[rootA] < rank[rootB])
                {
                    parent[rootA] = rootB;
                }
                else if (rank[rootA] > rank[rootB])
                {
                    parent[rootB] = rootA;
                }
                else
                {
                    parent[rootB] = rootA;
                    rank[rootA]++;
                }
            }
        }
    }
}
