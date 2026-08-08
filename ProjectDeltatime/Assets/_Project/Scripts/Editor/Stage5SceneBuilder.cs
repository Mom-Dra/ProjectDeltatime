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
    /// Builds Stage 5 by preserving the official Synty dive-bar demo scene and
    /// moving only Stage 4's proven gameplay roots into that independent copy.
    /// Neither source scene is saved by this builder.
    /// </summary>
    public static class Stage5SceneBuilder
    {
        private const string DiveBarScenePath =
            "Assets/Synty/PolygonNightclubs/Scenes/Demo_DiveBar_01.unity";
        private const string Stage4ScenePath =
            "Assets/_Project/Scenes/Stage4.unity";
        private const string Stage5ScenePath =
            "Assets/_Project/Scenes/Stage5.unity";
        private const string Stage5NavigationPath =
            "Assets/_Project/Scenes/Stage5Navigation.asset";
        private const string PreviewAssetPath =
            "Assets/_Project/Art/Generated/Stage5Preview.png";
        private const string EnvironmentRootName =
            "Stage 5 - Undertow Dive";
        private const string CharacterRoot =
            "Assets/Synty/PolygonNightclubs/Prefabs/Characters";
        private const int VisionObstacleLayer = 8;
        private const int DeadlineCharges = 2;
        private const float ActorRootHeight = 0.75f;
        private const float PickupHeight = 0.18f;
        private const float CameraHeightDepthRatio = 0.47f;
        private const float CameraDownwardAngle = 60f;
        private const float CameraFieldOfView = 48f;
        private const float CameraLookHeight = 0.55f;
        private const float CameraFocusDepthRatio = 0.06f;
        private const float CameraMaximumFocusOffset = 1.5f;
        private const float CameraAimLeadDistance = 1.25f;
        private const float RightAnnexBoundaryX = 5f;
        private const float RightAnnexMinimumZ = -2.5f;
        private const float AnnexBoundaryTolerance = 0.01f;
        private const float SouthCutawayHideInset = 3f;
        private const float SouthCutawayRestoreInset = 3.75f;
        private const float SouthCutawayRendererBand = 1.25f;
        private const float ForegroundCutawayRendererBand = 6f;
        private const float GroundMovementSampleDistance = 1.25f;
        private const float GroundMovementSegmentLength = 0.12f;
        private const int NotWalkableNavMeshArea = 1;
        private const float FurnitureTargetTolerance = 0.2f;
        private const int SeatsPerRetainedTable = 2;
        private const int RetainedBarSeatCount = 4;

        private const string PlayerRingMaterialPath =
            "Assets/_Project/Materials/Stage5PlayerMarker.mat";
        private const string RangedRingMaterialPath =
            "Assets/_Project/Materials/Stage5RangedEnemyMarker.mat";
        private const string ChaserRingMaterialPath =
            "Assets/_Project/Materials/Stage5ChaserEnemyMarker.mat";

        private static readonly Color PlayerRingColor =
            new Color(0.02f, 0.6f, 0.8f, 1f);
        private static readonly Color RangedRingColor =
            new Color(0.85f, 0.08f, 0.055f, 1f);
        private static readonly Color ChaserRingColor =
            new Color(1f, 0.38f, 0.035f, 1f);

        private static readonly Vector2[] RetainedTablePositions =
        {
            new Vector2(-2.675f, -8.35f),
            new Vector2(-4.375f, -4.875f),
            new Vector2(-8.335f, -1.221f),
            new Vector2(-5.3f, 2.115f),
            new Vector2(-8.934f, 4.785f),
            new Vector2(-4.279f, 5.083f),
            new Vector2(0.5f, 9.5f)
        };

        private const string PlayerCharacterPath =
            CharacterRoot + "/SM_Chr_Party_Female_03.prefab";
        private const string WestCharacterPath =
            CharacterRoot + "/SM_Chr_Bartender_Male_01.prefab";
        private const string CenterCharacterPath =
            CharacterRoot + "/SM_Chr_Bouncer_Male_01.prefab";
        private const string EastCharacterPath =
            CharacterRoot + "/SM_Chr_Party_Female_02.prefab";
        private const string NorthCharacterPath =
            CharacterRoot + "/SM_Chr_Bartender_Female_01.prefab";
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

        private static readonly string[] EnvironmentSourceRootNames =
        {
            "Scene",
            "Roof_Layer",
            "Lighting (URP)",
            "Lighting (BIRP)",
            "Reflection Probe",
            "Global Volume"
        };

        private static readonly string[] DiveBarVisualNames =
        {
            "Dive Bar Character - Player",
            "Dive Bar Character - West Gunner",
            "Dive Bar Character - Center Chaser",
            "Dive Bar Character - East Gunner",
            "Dive Bar Character - North Gunner",
            "Dive Bar Character - South Chaser"
        };

        [MenuItem("Tools/Prototype/Build Stage 5 - Undertow Dive")]
        public static void BuildStage5()
        {
            RequireSourceAssets();
            EnsureCombatIdentityRingMaterials();

            Scene demoScene = EditorSceneManager.OpenScene(
                DiveBarScenePath,
                OpenSceneMode.Single);
            if (!EditorSceneManager.SaveScene(
                    demoScene,
                    Stage5ScenePath,
                    true))
            {
                throw new InvalidOperationException(
                    $"Failed to copy {DiveBarScenePath} to {Stage5ScenePath}.");
            }

            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            Scene stage5 = EditorSceneManager.OpenScene(
                Stage5ScenePath,
                OpenSceneMode.Single);
            EditorSceneManager.SetActiveScene(stage5);

            CameraSettings demoCameraSettings = CaptureDemoCameraSettings(stage5);
            GameObject environmentRoot = PrepareDiveBarEnvironment(stage5);
            CurateDiveBarEnvironment(environmentRoot);
            List<GameObject> gameplayRoots = MoveStage4GameplayRoots(stage5);
            AttachDiveBarCharacters(stage5);
            ConfigureDiveBarVisualFeedback(demoCameraSettings);
            ConfigureDiveBarColliders(environmentRoot);

            NavMeshSurface surface = BuildStage5Navigation(
                environmentRoot,
                gameplayRoots);
            ConfigureStage5ElevationMovement(stage5, environmentRoot);
            PositionEncounterOnBakedNavigation();
            ConfigureCombatIdentityRings();
            ConfigureSouthExteriorCutaway(environmentRoot);
            ConfigureDiveBarCamera(surface);

            EditorSceneManager.MarkSceneDirty(stage5);
            if (!EditorSceneManager.SaveScene(stage5, Stage5ScenePath))
            {
                throw new InvalidOperationException(
                    $"Failed to save {Stage5ScenePath}.");
            }

            AddStage5ToBuildSettings();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            ValidateStage5Scene(stage5);

            if (!Application.isBatchMode)
            {
                Selection.activeGameObject = FindSceneRoot(stage5, "Player");
            }

            Debug.Log("Stage5 Undertow Dive built and validated successfully.");
        }

        public static void BuildAndValidateFromCommandLine()
        {
            BuildStage5();
        }

        public static void ValidateStage1Through4RegressionFromCommandLine()
        {
            PrototypeSceneBuilder.ValidateSavedPrototypeRoom();
            Stage3SceneBuilder.ValidateSavedStage3();
            Stage4SceneBuilder.ValidateSavedStage4();
            Debug.Log("Stage1 through Stage4 regression validation passed.");
        }

        [MenuItem("Tools/Prototype/Validate Stage 5 - Undertow Dive")]
        public static void ValidateSavedStage5()
        {
            Scene scene = EditorSceneManager.OpenScene(
                Stage5ScenePath,
                OpenSceneMode.Single);
            ValidateStage5Scene(scene);
            Debug.Log("Stage5 Undertow Dive validation passed.");
        }

        public static void CapturePreviewFromCommandLine()
        {
            Scene scene = EditorSceneManager.OpenScene(
                Stage5ScenePath,
                OpenSceneMode.Single);
            ValidateStage5Scene(scene);

            Camera camera = FindActiveGameplayCamera(scene);
            if (camera == null)
            {
                throw new InvalidOperationException(
                    "Stage5 preview requires its active gameplay camera.");
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
                TopDownCameraController controller =
                    camera.GetComponent<TopDownCameraController>();
                controller?.SnapToTarget();
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
                    "Stage5Preview.png");
                Directory.CreateDirectory(Path.GetDirectoryName(previewPath));
                File.WriteAllBytes(previewPath, preview.EncodeToPNG());
                AssetDatabase.ImportAsset(
                    PreviewAssetPath,
                    ImportAssetOptions.ForceSynchronousImport);
                Debug.Log($"Stage5 preview captured at {previewPath}.");
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
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(DiveBarScenePath) == null)
            {
                throw new InvalidOperationException(
                    $"Stage5 requires the official dive-bar scene: {DiveBarScenePath}");
            }

            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(Stage4ScenePath) == null)
            {
                throw new InvalidOperationException(
                    $"Stage5 requires the gameplay-root source: {Stage4ScenePath}");
            }

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
                if (AssetDatabase.LoadAssetAtPath<GameObject>(characterPaths[i]) == null)
                {
                    throw new InvalidOperationException(
                        $"Required dive-bar character is missing: {characterPaths[i]}");
                }
            }
        }

        private static CameraSettings CaptureDemoCameraSettings(Scene scene)
        {
            GameObject cameraRoot = FindSceneRoot(scene, "Main Camera");
            Camera demoCamera = cameraRoot == null
                ? null
                : cameraRoot.GetComponent<Camera>();
            CameraSettings settings = demoCamera == null
                ? new CameraSettings(CameraClearFlags.Skybox, Color.black)
                : new CameraSettings(
                    demoCamera.clearFlags,
                    demoCamera.backgroundColor);

            if (cameraRoot != null)
            {
                UnityEngine.Object.DestroyImmediate(cameraRoot);
            }

            return settings;
        }

        private static GameObject PrepareDiveBarEnvironment(Scene scene)
        {
            GameObject root = new GameObject(EnvironmentRootName);
            SceneManager.MoveGameObjectToScene(root, scene);
            root.AddComponent<ReplayExcluded>();

            for (int i = 0; i < EnvironmentSourceRootNames.Length; i++)
            {
                GameObject sourceRoot = FindSceneRoot(
                    scene,
                    EnvironmentSourceRootNames[i]);
                if (sourceRoot == null)
                {
                    throw new InvalidOperationException(
                        $"Dive-bar environment root is missing: {EnvironmentSourceRootNames[i]}");
                }

                sourceRoot.transform.SetParent(root.transform, true);
            }

            RemoveMissingScriptsFromCopiedEnvironment(root);

            Transform roof = FindDirectChild(root.transform, "Roof_Layer");
            if (roof != null)
            {
                Renderer[] roofRenderers =
                    roof.GetComponentsInChildren<Renderer>(true);
                for (int i = 0; i < roofRenderers.Length; i++)
                {
                    roofRenderers[i].shadowCastingMode =
                        ShadowCastingMode.ShadowsOnly;
                }
            }

            Light[] environmentLights = root.GetComponentsInChildren<Light>(true);
            for (int i = 0; i < environmentLights.Length; i++)
            {
                if (environmentLights[i].type == LightType.Directional)
                {
                    environmentLights[i].enabled = false;
                }
            }

            return root;
        }

        private static void CurateDiveBarEnvironment(GameObject environmentRoot)
        {
            DisableRightAnnex(environmentRoot);
            CurateFurniture(environmentRoot);
        }

        private static void DisableRightAnnex(GameObject environmentRoot)
        {
            List<GameObject> prefabRoots = FindOutermostPrefabRoots(environmentRoot);
            int disabledPrefabCount = 0;
            for (int i = 0; i < prefabRoots.Count; i++)
            {
                GameObject prefabRoot = prefabRoots[i];
                if (prefabRoot.activeSelf && ShouldExcludeRightAnnex(prefabRoot.transform))
                {
                    prefabRoot.SetActive(false);
                    disabledPrefabCount++;
                }
            }

            // Some demo lighting and rendered objects are scene objects rather
            // than individual prefab instances. Disable their local children
            // with the same boundary policy so they cannot remain visible.
            Renderer[] renderers = environmentRoot.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer renderer = renderers[i];
                if (renderer.gameObject.activeInHierarchy &&
                    IsRightAnnexContent(renderer.transform, renderer.bounds))
                {
                    renderer.gameObject.SetActive(false);
                }
            }

            Light[] lights = environmentRoot.GetComponentsInChildren<Light>(true);
            for (int i = 0; i < lights.Length; i++)
            {
                Light light = lights[i];
                if (light.gameObject.activeInHierarchy &&
                    IsRightAnnexContent(
                        light.transform,
                        new Bounds(light.transform.position, Vector3.zero)))
                {
                    light.gameObject.SetActive(false);
                }
            }

            ReflectionProbe[] probes =
                environmentRoot.GetComponentsInChildren<ReflectionProbe>(true);
            for (int i = 0; i < probes.Length; i++)
            {
                ReflectionProbe probe = probes[i];
                if (probe.gameObject.activeInHierarchy &&
                    IsRightAnnexContent(
                        probe.transform,
                        new Bounds(probe.transform.position, probe.size)))
                {
                    probe.gameObject.SetActive(false);
                }
            }

            Debug.Log(
                $"Stage5 excluded the right annex with {disabledPrefabCount} prefab roots.");
        }

        private static bool ShouldExcludeRightAnnex(Transform transform)
        {
            return IsRightAnnexContent(
                transform,
                new Bounds(transform.position, Vector3.zero));
        }

        private static bool IsRightAnnexContent(Transform transform, Bounds bounds)
        {
            if (bounds.center.z < RightAnnexMinimumZ - AnnexBoundaryTolerance ||
                bounds.center.x < RightAnnexBoundaryX - AnnexBoundaryTolerance)
            {
                return false;
            }

            if (bounds.center.x > RightAnnexBoundaryX + AnnexBoundaryTolerance)
            {
                return true;
            }

            return !HasStructuralDividerName(transform, null);
        }

        private static bool HasStructuralDividerName(Transform transform, Transform stopAt)
        {
            Transform current = transform;
            while (current != null && current != stopAt)
            {
                string lower = current.name.ToLowerInvariant();
                if (lower.Contains("wall") || lower.Contains("door") ||
                    lower.Contains("pillar"))
                {
                    return true;
                }

                current = current.parent;
            }

            return false;
        }

        private static void CurateFurniture(GameObject environmentRoot)
        {
            List<GameObject> tables = FindFurnitureRoots(
                environmentRoot,
                FurnitureKind.Table);
            List<GameObject> seats = FindFurnitureRoots(
                environmentRoot,
                FurnitureKind.Seat);
            List<GameObject> retainedTables = new List<GameObject>();

            for (int i = 0; i < tables.Count; i++)
            {
                GameObject table = tables[i];
                bool retain = table.activeInHierarchy &&
                    IsRetainedTable(table.transform.position);
                table.SetActive(retain);
                if (retain)
                {
                    retainedTables.Add(table);
                }
            }

            if (retainedTables.Count != RetainedTablePositions.Length)
            {
                throw new InvalidOperationException(
                    $"Stage5 retained {retainedTables.Count} tables instead of " +
                    $"{RetainedTablePositions.Length}. The source layout changed.");
            }

            HashSet<GameObject> retainedSeats = new HashSet<GameObject>();
            for (int i = 0; i < retainedTables.Count; i++)
            {
                RetainNearestSeats(
                    retainedTables[i],
                    seats,
                    retainedSeats,
                    SeatsPerRetainedTable);
            }

            int retainedBarSeats = 0;
            for (int i = 0; i < seats.Count; i++)
            {
                GameObject seat = seats[i];
                if (seat.activeInHierarchy && IsBarStool(seat))
                {
                    retainedSeats.Add(seat);
                    retainedBarSeats++;
                }
            }

            if (retainedBarSeats != RetainedBarSeatCount)
            {
                throw new InvalidOperationException(
                    $"Stage5 retained {retainedBarSeats} bar stools instead of " +
                    $"{RetainedBarSeatCount}. The source layout changed.");
            }

            for (int i = 0; i < seats.Count; i++)
            {
                GameObject seat = seats[i];
                seat.SetActive(retainedSeats.Contains(seat));
            }

            int expectedSeatCount =
                RetainedTablePositions.Length * SeatsPerRetainedTable +
                RetainedBarSeatCount;
            if (retainedSeats.Count != expectedSeatCount)
            {
                throw new InvalidOperationException(
                    $"Stage5 retained {retainedSeats.Count} seats instead of " +
                    $"{expectedSeatCount}.");
            }
        }

        private static bool IsRetainedTable(Vector3 position)
        {
            Vector2 tablePosition = new Vector2(position.x, position.z);
            for (int i = 0; i < RetainedTablePositions.Length; i++)
            {
                if (Vector2.Distance(tablePosition, RetainedTablePositions[i]) <=
                    FurnitureTargetTolerance)
                {
                    return true;
                }
            }

            return false;
        }

        private static void RetainNearestSeats(
            GameObject table,
            List<GameObject> seats,
            HashSet<GameObject> retainedSeats,
            int count)
        {
            for (int selection = 0; selection < count; selection++)
            {
                GameObject nearest = null;
                float nearestDistance = float.PositiveInfinity;
                for (int i = 0; i < seats.Count; i++)
                {
                    GameObject candidate = seats[i];
                    if (!candidate.activeInHierarchy || retainedSeats.Contains(candidate))
                    {
                        continue;
                    }

                    float distance = Vector2.SqrMagnitude(
                        new Vector2(
                            candidate.transform.position.x - table.transform.position.x,
                            candidate.transform.position.z - table.transform.position.z));
                    if (distance < nearestDistance - 0.0001f ||
                        (Mathf.Abs(distance - nearestDistance) <= 0.0001f &&
                         nearest != null &&
                         string.CompareOrdinal(candidate.name, nearest.name) < 0))
                    {
                        nearest = candidate;
                        nearestDistance = distance;
                    }
                }

                if (nearest == null)
                {
                    throw new InvalidOperationException(
                        $"Stage5 could not retain {count} seats for {table.name}.");
                }

                retainedSeats.Add(nearest);
            }
        }

        private static bool IsBarStool(GameObject seat)
        {
            return seat.name.StartsWith("SM_Prop_Stool_02", StringComparison.Ordinal) &&
                   seat.transform.position.x < RightAnnexBoundaryX;
        }

        private static List<GameObject> FindFurnitureRoots(
            GameObject environmentRoot,
            FurnitureKind kind)
        {
            List<GameObject> result = new List<GameObject>();
            HashSet<GameObject> seen = new HashSet<GameObject>();
            Transform[] transforms =
                environmentRoot.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < transforms.Length; i++)
            {
                Transform transform = transforms[i];
                if (!MatchesFurnitureKind(transform.name, kind))
                {
                    continue;
                }

                GameObject root = GetOutermostPrefabRoot(transform.gameObject);
                if (root != null && seen.Add(root))
                {
                    result.Add(root);
                }
            }

            return result;
        }

        private static bool MatchesFurnitureKind(string name, FurnitureKind kind)
        {
            string lower = name.ToLowerInvariant();
            return kind == FurnitureKind.Table
                ? lower.Contains("table_")
                : lower.Contains("chair_") || lower.Contains("stool_");
        }

        private static List<GameObject> FindOutermostPrefabRoots(GameObject environmentRoot)
        {
            List<GameObject> result = new List<GameObject>();
            HashSet<GameObject> seen = new HashSet<GameObject>();
            Transform[] transforms =
                environmentRoot.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < transforms.Length; i++)
            {
                GameObject root = GetOutermostPrefabRoot(transforms[i].gameObject);
                if (root != null && root != environmentRoot && seen.Add(root))
                {
                    result.Add(root);
                }
            }

            return result;
        }

        private static GameObject GetOutermostPrefabRoot(GameObject gameObject)
        {
            GameObject root = PrefabUtility.GetOutermostPrefabInstanceRoot(gameObject);
            return root == null ? gameObject : root;
        }

        private enum FurnitureKind
        {
            Table,
            Seat
        }

        private static void RemoveMissingScriptsFromCopiedEnvironment(GameObject root)
        {
            Transform[] transforms = root.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < transforms.Length; i++)
            {
                GameObject gameObject = transforms[i].gameObject;
                if (GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(gameObject) > 0)
                {
                    GameObjectUtility.RemoveMonoBehavioursWithMissingScript(gameObject);
                    EditorUtility.SetDirty(gameObject);
                }
            }
        }

        private static List<GameObject> MoveStage4GameplayRoots(Scene stage5)
        {
            EditorSceneManager.SetActiveScene(stage5);
            Scene stage4 = EditorSceneManager.OpenScene(
                Stage4ScenePath,
                OpenSceneMode.Additive);
            List<GameObject> moved = new List<GameObject>();

            try
            {
                RemoveStage4VisualChildren(stage4);
                for (int i = 0; i < GameplayRootNames.Length; i++)
                {
                    GameObject root = FindSceneRoot(stage4, GameplayRootNames[i]);
                    if (root == null)
                    {
                        throw new InvalidOperationException(
                            $"Stage4 gameplay root is missing: {GameplayRootNames[i]}");
                    }

                    SceneManager.MoveGameObjectToScene(root, stage5);
                    moved.Add(root);
                }
            }
            finally
            {
                EditorSceneManager.SetActiveScene(stage5);
                if (stage4.IsValid() && stage4.isLoaded)
                {
                    EditorSceneManager.CloseScene(stage4, true);
                }
            }

            return moved;
        }

        private static void RemoveStage4VisualChildren(Scene stage4)
        {
            GameObject[] roots = stage4.GetRootGameObjects();
            List<GameObject> remove = new List<GameObject>();
            for (int i = 0; i < roots.Length; i++)
            {
                Transform[] transforms = roots[i].GetComponentsInChildren<Transform>(true);
                for (int j = 0; j < transforms.Length; j++)
                {
                    if (transforms[j].name.StartsWith(
                            "Rooftop Character - ",
                            StringComparison.Ordinal))
                    {
                        remove.Add(transforms[j].gameObject);
                    }
                }
            }

            for (int i = 0; i < remove.Count; i++)
            {
                UnityEngine.Object.DestroyImmediate(remove[i]);
            }
        }

        private static void AttachDiveBarCharacters(Scene scene)
        {
            AttachCharacter(
                scene,
                "Player",
                PlayerCharacterPath,
                DiveBarVisualNames[0]);
            AttachCharacter(
                scene,
                "Enemy West",
                WestCharacterPath,
                DiveBarVisualNames[1]);
            AttachCharacter(
                scene,
                "Enemy Center",
                CenterCharacterPath,
                DiveBarVisualNames[2]);
            AttachCharacter(
                scene,
                "Enemy East",
                EastCharacterPath,
                DiveBarVisualNames[3]);
            AttachCharacter(
                scene,
                "Enemy North Gunner",
                NorthCharacterPath,
                DiveBarVisualNames[4]);
            AttachCharacter(
                scene,
                "Enemy South Chaser",
                SouthCharacterPath,
                DiveBarVisualNames[5]);
        }

        private static void AttachCharacter(
            Scene scene,
            string ownerName,
            string prefabPath,
            string visualName)
        {
            GameObject owner = FindSceneRoot(scene, ownerName);
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

            Collider[] colliders = visual.GetComponentsInChildren<Collider>(true);
            for (int i = 0; i < colliders.Length; i++)
            {
                colliders[i].enabled = false;
            }

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
                    $"Dive-bar character '{visual.name}' is missing arm bones.");
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

            bone.rotation = Quaternion.FromToRotation(
                currentDirection.normalized,
                targetDirection) * bone.rotation;
        }

        private static void ConfigureDiveBarVisualFeedback(
            CameraSettings demoCameraSettings)
        {
            Camera camera = UnityEngine.Object.FindFirstObjectByType<Camera>();
            WorldTimeVisualFeedback feedback = camera == null
                ? null
                : camera.GetComponent<WorldTimeVisualFeedback>();
            Light keyLight = FindSceneRoot(
                    SceneManager.GetActiveScene(),
                    "Directional Key Light")
                ?.GetComponent<Light>();
            if (camera == null || feedback == null || keyLight == null)
            {
                throw new InvalidOperationException(
                    "Stage5 requires its gameplay camera, visual feedback, and key light.");
            }

            camera.clearFlags = demoCameraSettings.ClearFlags;
            camera.backgroundColor = demoCameraSettings.BackgroundColor;

            SerializedObject settings = new SerializedObject(feedback);
            settings.FindProperty("directionalKeyLight").objectReferenceValue = keyLight;
            settings.FindProperty("preserveSceneRenderSettings").boolValue = true;
            settings.FindProperty("mapFillLightIntensity").floatValue = 0f;
            settings.FindProperty("mapFillLightPositions").arraySize = 0;
            settings.FindProperty("nearlyStoppedColor").colorValue =
                demoCameraSettings.BackgroundColor;
            settings.FindProperty("activeColor").colorValue =
                demoCameraSettings.BackgroundColor;
            settings.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(feedback);
            EditorUtility.SetDirty(camera);
        }

        private static void ConfigureDiveBarColliders(GameObject environmentRoot)
        {
            Collider[] colliders =
                environmentRoot.GetComponentsInChildren<Collider>(true);
            for (int i = 0; i < colliders.Length; i++)
            {
                Collider collider = colliders[i];
                bool keep = ShouldKeepEnvironmentCollider(collider);
                collider.enabled = keep;
                collider.gameObject.layer = keep && ShouldBlockVision(collider)
                    ? VisionObstacleLayer
                    : 0;
                EditorUtility.SetDirty(collider);
                EditorUtility.SetDirty(collider.gameObject);
            }
        }

        private static bool ShouldKeepEnvironmentCollider(Collider collider)
        {
            string lower = collider.name.ToLowerInvariant();
            if (lower.Contains("ceiling") || lower.Contains("roof"))
            {
                return false;
            }

            if (lower.StartsWith("sm_bld_", StringComparison.Ordinal))
            {
                return true;
            }

            return lower.Contains("bar_01") ||
                   lower.Contains("bar_sink") ||
                   lower.Contains("sofa") ||
                   lower.Contains("booth") ||
                   lower.Contains("counter") ||
                   lower.Contains("drinks_fridge") ||
                   lower.Contains("mechanical_bull_pit") ||
                   lower.Contains("table_02") ||
                   lower.Contains("table_06") ||
                   lower.Contains("table_07") ||
                   lower.Contains("table_15");
        }

        private static bool ShouldBlockVision(Collider collider)
        {
            string lower = collider.name.ToLowerInvariant();
            if (lower.Contains("floor") || lower.Contains("steps") ||
                lower.Contains("stairs"))
            {
                return false;
            }

            return lower.Contains("wall") || lower.Contains("door") ||
                   lower.Contains("bar_01") || lower.Contains("sofa") ||
                   lower.Contains("booth") || lower.Contains("counter") ||
                   lower.Contains("drinks_fridge") ||
                   lower.Contains("mechanical_bull_pit") ||
                   (lower.Contains("table_") && collider.bounds.size.y >= 0.75f);
        }

        private static NavMeshSurface BuildStage5Navigation(
            GameObject environmentRoot,
            List<GameObject> gameplayRoots)
        {
            NavMeshSurface surface =
                UnityEngine.Object.FindFirstObjectByType<NavMeshSurface>();
            if (surface == null)
            {
                throw new InvalidOperationException(
                    "Stage5 navigation surface is missing from the moved gameplay roots.");
            }

            Bounds floorBounds = CalculateInteriorFloorBounds(environmentRoot);
            surface.RemoveData();
            surface.navMeshData = null;
            surface.collectObjects = CollectObjects.Volume;
            surface.center = new Vector3(
                floorBounds.center.x,
                1.5f,
                floorBounds.center.z);
            surface.size = new Vector3(
                floorBounds.size.x,
                5f,
                floorBounds.size.z);
            surface.layerMask = ~0;
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
                "Stage5");

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

            if (surface.navMeshData == null)
            {
                throw new InvalidOperationException(
                    "Stage5 navigation bake did not produce NavMeshData.");
            }

            NavMeshData bakedData = surface.navMeshData;
            bakedData.name = "Stage5Navigation";
            NavMeshData savedData = AssetDatabase.LoadAssetAtPath<NavMeshData>(
                Stage5NavigationPath);
            if (savedData == null)
            {
                AssetDatabase.CreateAsset(bakedData, Stage5NavigationPath);
            }
            else
            {
                surface.RemoveData();
                EditorUtility.CopySerialized(bakedData, savedData);
                surface.navMeshData = savedData;
                surface.AddData();
                UnityEngine.Object.DestroyImmediate(bakedData);
                savedData.name = "Stage5Navigation";
                EditorUtility.SetDirty(savedData);
            }

            EditorUtility.SetDirty(surface);
            AssetDatabase.SaveAssets();
            return surface;
        }

        /// <summary>
        /// Physics-collider baking otherwise treats a table, chair, counter, or
        /// other furniture's horizontal faces as a second floor. Keep the
        /// colliders for gameplay and line of sight, but temporarily mark their
        /// own bake sources Not Walkable so no upper surface is added to NavMesh.
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

            if (modifiers.Count == 0)
            {
                throw new InvalidOperationException(
                    $"{stageName} found no non-ground collider to exclude from NavMesh.");
            }

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
            return lower.Contains("table") || lower.Contains("chair") ||
                   lower.Contains("stool") || lower.Contains("sofa") ||
                   lower.Contains("booth") || lower.Contains("lounge") ||
                   lower.Contains("bar_") || lower.Contains("counter") ||
                   lower.Contains("fridge") || lower.Contains("shelf") ||
                   lower.Contains("cabinet") || lower.Contains("desk") ||
                   lower.Contains("planter") || lower.Contains("pillar") ||
                   lower.Contains("column") || lower.Contains("prop_") ||
                   lower.Contains("mechanical_bull_pit");
        }

        private static Bounds CalculateInteriorFloorBounds(GameObject environmentRoot)
        {
            Renderer[] renderers =
                environmentRoot.GetComponentsInChildren<Renderer>(true);
            Bounds result = default;
            bool found = false;
            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer renderer = renderers[i];
                Bounds bounds = renderer.bounds;
                string lower = renderer.name.ToLowerInvariant();
                if (!renderer.gameObject.activeInHierarchy ||
                    !lower.Contains("floor") || lower.Contains("ceiling") ||
                    Mathf.Abs(bounds.center.x) > 10f ||
                    Mathf.Abs(bounds.center.z) > 12.5f ||
                    bounds.center.y > 0.6f)
                {
                    continue;
                }

                if (!found)
                {
                    result = bounds;
                    found = true;
                }
                else
                {
                    result.Encapsulate(bounds);
                }
            }

            if (!found || result.size.x < 15f || result.size.z < 20f)
            {
                throw new InvalidOperationException(
                    $"Could not derive the dive-bar floor bounds: {result}");
            }

            // NavMeshSurface's PhysicsCollider collection can retain collider
            // bounds from disabled copied prefab children. Keep its collection
            // volume on the playable side of the preserved east divider.
            Vector3 clippedMaximum = result.max;
            clippedMaximum.x = Mathf.Min(
                clippedMaximum.x,
                RightAnnexBoundaryX - AnnexBoundaryTolerance);
            result.max = clippedMaximum;

            return result;
        }

        private static void PositionEncounterOnBakedNavigation()
        {
            // These candidates were measured against Demo_DiveBar_01's actual
            // PhysicsCollider NavMesh. They share one complete connected region.
            SetActorPose("Player", new Vector3(0f, 0f, -7f), 180f);
            SetActorPose("Enemy West", new Vector3(-2f, 0f, -4.5f), 145f);
            SetActorPose("Enemy Center", new Vector3(-3.2f, 0f, 0.5f), 180f);
            SetActorPose("Enemy East", new Vector3(3.5f, 0f, -6.5f), 250f);
            SetActorPose("Enemy North Gunner", new Vector3(-1f, 0f, 6.5f), 180f);
            SetActorPose("Enemy South Chaser", new Vector3(0f, 0f, -9f), 0f);
            SetPickupPose("Pistol Pickup", new Vector3(-1.1f, 0f, -7.4f), 25f);
            SetPickupPose("Shotgun Pickup", new Vector3(1.1f, 0f, -7.4f), -25f);
        }

        private static void SetActorPose(
            string name,
            Vector3 requestedPosition,
            float yaw)
        {
            GameObject target = FindSceneRoot(SceneManager.GetActiveScene(), name);
            if (target == null)
            {
                throw new InvalidOperationException(
                    $"Stage5 gameplay object is missing: {name}");
            }

            Vector3 navPosition = FindGroundNavPosition(requestedPosition, name);
            target.transform.SetPositionAndRotation(
                navPosition + Vector3.up * ActorRootHeight,
                Quaternion.Euler(0f, yaw, 0f));
        }

        private static void SetPickupPose(
            string name,
            Vector3 requestedPosition,
            float yaw)
        {
            GameObject target = FindSceneRoot(SceneManager.GetActiveScene(), name);
            if (target == null)
            {
                throw new InvalidOperationException(
                    $"Stage5 pickup is missing: {name}");
            }

            Vector3 navPosition = FindGroundNavPosition(requestedPosition, name);
            target.transform.SetPositionAndRotation(
                navPosition + Vector3.up * PickupHeight,
                Quaternion.Euler(0f, yaw, 0f));
        }

        private static Vector3 FindGroundNavPosition(
            Vector3 requested,
            string subject)
        {
            Vector3 best = default;
            float bestHeight = float.PositiveInfinity;
            float bestDistance = float.PositiveInfinity;
            for (float radius = 0f; radius <= 1.5f; radius += 0.25f)
            {
                int samples = radius <= 0f ? 1 : Mathf.CeilToInt(radius * 16f);
                for (int i = 0; i < samples; i++)
                {
                    float angle = samples == 1 ? 0f : i * Mathf.PI * 2f / samples;
                    Vector3 candidate = requested + new Vector3(
                        Mathf.Cos(angle) * radius,
                        0f,
                        Mathf.Sin(angle) * radius);
                    if (!NavMesh.SamplePosition(
                            candidate,
                            out NavMeshHit hit,
                            0.35f,
                            NavMesh.AllAreas))
                    {
                        continue;
                    }

                    float distance = Vector2.Distance(
                        new Vector2(hit.position.x, hit.position.z),
                        new Vector2(requested.x, requested.z));
                    if (hit.position.y < bestHeight - 0.01f ||
                        (Mathf.Abs(hit.position.y - bestHeight) <= 0.01f &&
                         distance < bestDistance))
                    {
                        best = hit.position;
                        bestHeight = hit.position.y;
                        bestDistance = distance;
                    }
                }

                if (bestHeight <= 0.2f)
                {
                    return best;
                }
            }

            if (bestHeight < float.PositiveInfinity)
            {
                return best;
            }

            throw new InvalidOperationException(
                $"Stage5 {subject} candidate is not on the baked NavMesh: {requested}");
        }

        private static void EnsureCombatIdentityRingMaterials()
        {
            Shader shader = Shader.Find("Unlit/Color");
            if (shader == null)
            {
                throw new InvalidOperationException(
                    "Stage5 requires the built-in Unlit/Color shader for identity markers.");
            }

            EnsureUnlitMaterial(PlayerRingMaterialPath, PlayerRingColor, shader);
            EnsureUnlitMaterial(RangedRingMaterialPath, RangedRingColor, shader);
            EnsureUnlitMaterial(ChaserRingMaterialPath, ChaserRingColor, shader);
            AssetDatabase.SaveAssets();
        }

        private static void EnsureUnlitMaterial(
            string path,
            Color color,
            Shader shader)
        {
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(shader)
                {
                    name = Path.GetFileNameWithoutExtension(path)
                };
                AssetDatabase.CreateAsset(material, path);
            }

            material.shader = shader;
            material.color = color;
            material.renderQueue = -1;
            EditorUtility.SetDirty(material);
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
                if (owner == null || ring == null)
                {
                    throw new InvalidOperationException(
                        $"Stage5 combat identity ring is missing for {owners[i]}.");
                }

                if (materials[i] == null)
                {
                    throw new InvalidOperationException(
                        $"Stage5 combat identity material is missing for {owners[i]}.");
                }

                ring.position = new Vector3(
                    owner.transform.position.x,
                    owner.transform.position.y - ActorRootHeight + 0.025f,
                    owner.transform.position.z);
                Renderer renderer = ring.GetComponent<Renderer>();
                if (renderer == null)
                {
                    throw new InvalidOperationException(
                        $"Stage5 combat identity renderer is missing for {owners[i]}.");
                }

                renderer.sharedMaterial = materials[i];
                renderer.shadowCastingMode = ShadowCastingMode.Off;
                renderer.receiveShadows = false;
                renderer.lightProbeUsage = LightProbeUsage.Off;
                renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
                renderer.motionVectorGenerationMode =
                    MotionVectorGenerationMode.ForceNoMotion;
                EditorUtility.SetDirty(renderer);
            }
        }

        private static void ConfigureSouthExteriorCutaway(GameObject environmentRoot)
        {
            NavMeshTriangulation triangulation = NavMesh.CalculateTriangulation();
            if (triangulation.vertices.Length == 0)
            {
                throw new InvalidOperationException(
                    "Stage5 south cutaway requires the baked NavMesh bounds.");
            }

            Bounds navBounds = CalculateBounds(triangulation.vertices);
            List<Renderer> southExterior = new List<Renderer>();
            List<Renderer> foreground = new List<Renderer>();
            Renderer[] renderers = environmentRoot.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer renderer = renderers[i];
                if (IsSouthExteriorOccluder(renderer, navBounds, environmentRoot.transform))
                {
                    southExterior.Add(renderer);
                }
                else if (IsForegroundCutawayOccluder(renderer, navBounds))
                {
                    foreground.Add(renderer);
                }
            }

            if (southExterior.Count == 0 || foreground.Count == 0)
            {
                throw new InvalidOperationException(
                    "Stage5 could not find south exterior and foreground renderers for the cutaway.");
            }

            Transform player = FindSceneRoot(
                SceneManager.GetActiveScene(),
                "Player")?.transform;
            if (player == null)
            {
                throw new InvalidOperationException(
                    "Stage5 south cutaway requires the player transform.");
            }

            Camera camera = FindActiveGameplayCamera(SceneManager.GetActiveScene());
            if (camera == null)
            {
                throw new InvalidOperationException(
                    "Stage5 foreground cutaway requires the gameplay camera.");
            }

            Stage5SouthExteriorCutaway cutaway =
                environmentRoot.GetComponent<Stage5SouthExteriorCutaway>();
            if (cutaway == null)
            {
                cutaway = environmentRoot.AddComponent<Stage5SouthExteriorCutaway>();
            }

            cutaway.Configure(
                player,
                camera,
                CombineRenderers(southExterior, foreground),
                southExterior.Count,
                navBounds.min.z + SouthCutawayHideInset,
                navBounds.min.z + SouthCutawayRestoreInset,
                0.45f);
            EditorUtility.SetDirty(cutaway);
        }

        private static Renderer[] CombineRenderers(
            List<Renderer> first,
            List<Renderer> second)
        {
            List<Renderer> combined = new List<Renderer>(first.Count + second.Count);
            HashSet<Renderer> seen = new HashSet<Renderer>();
            for (int i = 0; i < first.Count; i++)
            {
                if (seen.Add(first[i]))
                {
                    combined.Add(first[i]);
                }
            }

            for (int i = 0; i < second.Count; i++)
            {
                if (seen.Add(second[i]))
                {
                    combined.Add(second[i]);
                }
            }

            return combined.ToArray();
        }

        private static bool IsSouthExteriorOccluder(
            Renderer renderer,
            Bounds navBounds,
            Transform environmentRoot)
        {
            if (renderer == null || !renderer.gameObject.activeInHierarchy ||
                renderer.shadowCastingMode == ShadowCastingMode.ShadowsOnly)
            {
                return false;
            }

            Bounds bounds = renderer.bounds;
            if (bounds.max.z > navBounds.min.z + SouthCutawayRendererBand ||
                bounds.max.x < navBounds.min.x || bounds.min.x > navBounds.max.x ||
                bounds.max.y <= navBounds.center.y + 0.25f)
            {
                return false;
            }

            return HasStructuralDividerName(renderer.transform, environmentRoot);
        }

        private static bool IsForegroundCutawayOccluder(
            Renderer renderer,
            Bounds navBounds)
        {
            if (renderer == null || !renderer.gameObject.activeInHierarchy ||
                renderer.shadowCastingMode == ShadowCastingMode.ShadowsOnly)
            {
                return false;
            }

            Bounds bounds = renderer.bounds;
            if (bounds.max.z > navBounds.min.z + ForegroundCutawayRendererBand ||
                bounds.max.x < navBounds.min.x || bounds.min.x > navBounds.max.x ||
                bounds.max.y <= navBounds.center.y + 0.25f)
            {
                return false;
            }

            string lower = renderer.name.ToLowerInvariant();
            return lower.Contains("table") || lower.Contains("chair") ||
                   lower.Contains("stool") || lower.Contains("sofa") ||
                   lower.Contains("plant") || lower.Contains("bar_") ||
                   lower.Contains("counter") || lower.Contains("prop_");
        }

        private static void ConfigureStage5ElevationMovement(
            Scene scene,
            GameObject environmentRoot)
        {
            int disabledTraversalColliders = DisableRuntimeTraversalColliders(environmentRoot);
            PlayerHealth player = UnityEngine.Object.FindFirstObjectByType<PlayerHealth>();
            EnemyMotor[] motors = UnityEngine.Object.FindObjectsByType<EnemyMotor>(
                FindObjectsSortMode.None);
            if (player == null || motors.Length != 5)
            {
                throw new InvalidOperationException(
                    "Stage5 elevation movement requires the player and five enemy motors.");
            }

            ConfigureActorGroundMovement(player.gameObject);
            for (int i = 0; i < motors.Length; i++)
            {
                ConfigureActorGroundMovement(motors[i].gameObject);
            }

            if (disabledTraversalColliders == 0)
            {
                throw new InvalidOperationException(
                    "Stage5 did not disable any baked stair/step colliders for runtime traversal.");
            }

            Physics.SyncTransforms();
            Debug.Log(
                $"Stage5 elevation traversal: actors=6, runtime stair/step colliders disabled={disabledTraversalColliders}.");
        }

        private static void ConfigureActorGroundMovement(GameObject actor)
        {
            Rigidbody body = actor.GetComponent<Rigidbody>();
            if (body == null)
            {
                throw new InvalidOperationException(
                    $"Stage5 elevation traversal actor has no Rigidbody: {actor.name}.");
            }

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

        private static void ConfigureDiveBarCamera(NavMeshSurface surface)
        {
            Camera camera = FindActiveGameplayCamera(SceneManager.GetActiveScene());
            TopDownCameraController controller = camera == null
                ? null
                : camera.GetComponent<TopDownCameraController>();
            if (camera == null || controller == null)
            {
                throw new InvalidOperationException(
                    "Stage5 requires the moved gameplay camera rig.");
            }

            NavMeshTriangulation triangulation = NavMesh.CalculateTriangulation();
            if (triangulation.vertices.Length == 0)
            {
                throw new InvalidOperationException(
                    "Stage5 camera configuration requires the baked NavMesh bounds.");
            }

            Bounds navBounds = new Bounds(triangulation.vertices[0], Vector3.zero);
            for (int i = 1; i < triangulation.vertices.Length; i++)
            {
                navBounds.Encapsulate(triangulation.vertices[i]);
            }

            CalculateDiveBarCameraFraming(
                navBounds,
                out Vector3 offset,
                out Vector3 focusOffset);
            Bounds cameraBounds = CalculateCameraBounds(navBounds);
            SerializedObject cameraSettings = new SerializedObject(controller);
            cameraSettings.FindProperty("cameraOffset").vector3Value = offset;
            cameraSettings.FindProperty("cameraFocusOffset").vector3Value = focusOffset;
            cameraSettings.FindProperty("aimLeadDistance").floatValue =
                CameraAimLeadDistance;
            cameraSettings.FindProperty("constrainToBounds").boolValue = true;
            cameraSettings.FindProperty("cameraBounds").boundsValue = cameraBounds;
            cameraSettings.ApplyModifiedPropertiesWithoutUndo();
            camera.fieldOfView = CameraFieldOfView;
            controller.SnapToTarget();
            EditorUtility.SetDirty(camera);
            EditorUtility.SetDirty(controller);
            EditorUtility.SetDirty(surface);
            Debug.Log(
                $"Stage5 camera derived from NavMesh bounds {navBounds}: " +
                $"offset={offset}, focusOffset={focusOffset}, " +
                $"FOV={CameraFieldOfView:0.0}, cameraBounds={cameraBounds}, " +
                "constrained=true.");
        }

        private static void CalculateDiveBarCameraFraming(
            Bounds navBounds,
            out Vector3 offset,
            out Vector3 focusOffset)
        {
            float height = navBounds.size.z * CameraHeightDepthRatio;
            float backward =
                (height - CameraLookHeight) /
                Mathf.Tan(CameraDownwardAngle * Mathf.Deg2Rad);
            offset = new Vector3(0f, height, -backward);
            focusOffset = new Vector3(
                0f,
                0f,
                Mathf.Min(
                    navBounds.size.z * CameraFocusDepthRatio,
                    CameraMaximumFocusOffset));
        }

        private static Bounds CalculateCameraBounds(Bounds navBounds)
        {
            return navBounds;
        }

        private static void AddStage5ToBuildSettings()
        {
            List<EditorBuildSettingsScene> existing =
                new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            List<EditorBuildSettingsScene> ordered =
                new List<EditorBuildSettingsScene>();
            AddBuildSceneIfPresent(ordered, "Assets/_Project/Scenes/Tutorial.unity");
            AddBuildSceneIfPresent(ordered, "Assets/_Project/Scenes/Stage1.unity");
            AddBuildSceneIfPresent(ordered, "Assets/_Project/Scenes/Stage2.unity");
            AddBuildSceneIfPresent(ordered, "Assets/_Project/Scenes/Stage3.unity");
            AddBuildSceneIfPresent(ordered, "Assets/_Project/Scenes/Stage4.unity");
            ordered.Add(new EditorBuildSettingsScene(Stage5ScenePath, true));

            for (int i = 0; i < existing.Count; i++)
            {
                string path = existing[i].path;
                if (path == "Assets/_Project/Scenes/Tutorial.unity" ||
                    path == "Assets/_Project/Scenes/Stage1.unity" ||
                    path == "Assets/_Project/Scenes/Stage2.unity" ||
                    path == "Assets/_Project/Scenes/Stage3.unity" ||
                    path == "Assets/_Project/Scenes/Stage4.unity" ||
                    path == Stage5ScenePath)
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

        private static void ValidateStage5Scene(Scene scene)
        {
            GameObject environmentRoot = FindSceneRoot(scene, EnvironmentRootName);
            PlayerHealth player = UnityEngine.Object.FindFirstObjectByType<PlayerHealth>();
            DeadlineController deadline =
                UnityEngine.Object.FindFirstObjectByType<DeadlineController>();
            WorldTimeController worldTime =
                UnityEngine.Object.FindFirstObjectByType<WorldTimeController>();
            StageController stage =
                UnityEngine.Object.FindFirstObjectByType<StageController>();
            StageReplayController replay =
                UnityEngine.Object.FindFirstObjectByType<StageReplayController>();
            NavMeshSurface surface =
                UnityEngine.Object.FindFirstObjectByType<NavMeshSurface>();
            EnemyHealth[] enemies = UnityEngine.Object.FindObjectsByType<EnemyHealth>(
                FindObjectsSortMode.None);
            EnemyMotor[] motors = UnityEngine.Object.FindObjectsByType<EnemyMotor>(
                FindObjectsSortMode.None);
            EnemyShooter[] shooters = UnityEngine.Object.FindObjectsByType<EnemyShooter>(
                FindObjectsSortMode.None);
            EnemyChaser[] chasers = UnityEngine.Object.FindObjectsByType<EnemyChaser>(
                FindObjectsSortMode.None);
            WeaponPickup[] pickups = UnityEngine.Object.FindObjectsByType<WeaponPickup>(
                FindObjectsSortMode.None);
            CharacterVisualController[] visualControllers =
                UnityEngine.Object.FindObjectsByType<CharacterVisualController>(
                    FindObjectsSortMode.None);

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
            Bounds navBounds = triangulation.vertices.Length == 0
                ? default
                : CalculateBounds(triangulation.vertices);
            int activeCameraCount = CountActiveCameras(scene);
            int diveBarVisualCount = CountNamedDiveBarVisuals(scene);
            int visionObstacleCount = environmentRoot == null
                ? 0
                : CountEnabledCollidersOnLayer(
                    environmentRoot.transform,
                    VisionObstacleLayer);
            int environmentRendererCount = environmentRoot == null
                ? 0
                : CountActiveRenderers(environmentRoot.transform);
            string missingReference = FindFirstMissingReference(scene);

            Require(scene.path == Stage5ScenePath,
                $"Unexpected Stage5 scene path: {scene.path}");
            Require(environmentRoot != null,
                "Stage5 dive-bar environment root is missing.");
            Require(environmentRoot != null &&
                    environmentRoot.GetComponent<ReplayExcluded>() != null,
                "Stage5 static environment is not excluded from replay tracks.");
            Require(environmentRendererCount >= 1000,
                $"Stage5 preserved only {environmentRendererCount} active demo renderers.");
            Require(activeCameraCount == 1,
                $"Stage5 has {activeCameraCount} active cameras instead of one.");
            Require(player != null &&
                    player.GetComponent<PlayerMovement>() != null &&
                    player.GetComponent<PlayerCombat>() != null,
                "Stage5 player gameplay root did not initialize structurally.");
            Require(enemies.Length == 5 && motors.Length == 5,
                $"Stage5 enemies={enemies.Length}, motors={motors.Length}; expected 5 each.");
            Require(shooters.Length == 3 && chasers.Length == 2,
                $"Stage5 ranged={shooters.Length}, chasers={chasers.Length}; expected 3/2.");
            Require(pickups.Length == 2,
                $"Stage5 has {pickups.Length} weapon pickups instead of 2.");
            Require(charges != null && charges.intValue == DeadlineCharges,
                $"Stage5 Deadline maximum charges are {charges?.intValue} instead of 2.");
            Require(worldTime != null && worldTime.enabled &&
                    worldTime.gameObject.activeInHierarchy,
                "Stage5 WorldTimeController is not active.");
            Require(stage != null && stage.enabled &&
                    stage.gameObject.activeInHierarchy &&
                    stage.CurrentState == StageController.StageState.Active,
                "Stage5 StageController is not active.");
            Require(replay != null && replay.enabled && replay.gameObject.activeInHierarchy,
                "Stage5 StageReplayController is not active.");
            Require(surface != null && navigationPath == Stage5NavigationPath,
                $"Stage5 uses the wrong NavMesh data: {navigationPath}");
            Require(triangulation.vertices.Length > 0,
                "Stage5 baked NavMesh has no triangles.");
            Require(diveBarVisualCount == 6 && visualControllers.Length == 6,
                $"Stage5 dive-bar visuals={diveBarVisualCount}, " +
                $"visual controllers={visualControllers.Length}; expected 6 each.");
            Require(visionObstacleCount > 0,
                "Stage5 has no enabled structural VisionObstacle colliders.");
            Require(string.IsNullOrEmpty(missingReference),
                "Stage5 contains a missing script or object reference: " + missingReference);

            ValidateEnvironmentCuration(environmentRoot, navBounds);
            ValidateStage5ElevationMovement(player, motors, environmentRoot, navBounds);
            ValidateFurnitureNavMeshExclusion(environmentRoot, "Stage5");

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
                "Stage5 player cannot initially see any enemy through the dive-bar structures.");
            ValidateDiveBarCamera(scene, player.transform, navBounds);
            ValidateCombatIdentityRings(scene);
            ValidateVisualChildren(scene);
            ValidateBuildOrder();
        }

        private static void ValidateEnvironmentCuration(
            GameObject environmentRoot,
            Bounds navBounds)
        {
            List<GameObject> tables = FindFurnitureRoots(
                environmentRoot,
                FurnitureKind.Table);
            List<GameObject> seats = FindFurnitureRoots(
                environmentRoot,
                FurnitureKind.Seat);
            int activeTableCount = CountActiveObjects(tables);
            int activeSeatCount = CountActiveObjects(seats);

            Require(activeTableCount == RetainedTablePositions.Length,
                $"Stage5 has {activeTableCount} active tables instead of " +
                $"{RetainedTablePositions.Length}.");
            Require(activeSeatCount ==
                    RetainedTablePositions.Length * SeatsPerRetainedTable +
                    RetainedBarSeatCount,
                $"Stage5 has {activeSeatCount} active seats instead of 18.");

            for (int i = 0; i < tables.Count; i++)
            {
                GameObject table = tables[i];
                if (table.activeInHierarchy)
                {
                    Require(IsRetainedTable(table.transform.position),
                        $"Stage5 retained an unexpected table: {table.name} at " +
                        table.transform.position);
                }
            }

            int annexRendererCount = CountActiveAnnexRenderers(environmentRoot);
            int annexLightCount = CountActiveAnnexLights(environmentRoot);
            int annexColliderCount = CountActiveAnnexColliders(environmentRoot);
            Require(annexRendererCount == 0 && annexLightCount == 0 &&
                    annexColliderCount == 0,
                $"Stage5 right annex remains active: renderers={annexRendererCount}, " +
                $"lights={annexLightCount}, colliders={annexColliderCount}.");
            Require(HasActiveEastDivider(environmentRoot),
                "Stage5 removed the east divider wall while excluding the right annex.");
            Require(navBounds.max.x <= RightAnnexBoundaryX + 0.1f,
                $"Stage5 NavMesh still enters the excluded right annex: {navBounds}.");

            Stage5SouthExteriorCutaway cutaway =
                environmentRoot.GetComponent<Stage5SouthExteriorCutaway>();
            Require(cutaway != null && cutaway.Occluders != null &&
                    cutaway.Occluders.Length > cutaway.SouthExteriorOccluderCount &&
                    cutaway.SouthExteriorOccluderCount > 0,
                "Stage5 foreground cutaway is not configured.");
            Require(Mathf.Abs(cutaway.HideBelowZ -
                              (navBounds.min.z + SouthCutawayHideInset)) < 0.01f &&
                    Mathf.Abs(cutaway.RestoreAboveZ -
                              (navBounds.min.z + SouthCutawayRestoreInset)) < 0.01f,
                "Stage5 south exterior cutaway thresholds do not match the NavMesh.");
            for (int i = 0; i < cutaway.Occluders.Length; i++)
            {
                Renderer renderer = cutaway.Occluders[i];
                Require(renderer != null && renderer.gameObject.activeInHierarchy &&
                        renderer.shadowCastingMode != ShadowCastingMode.ShadowsOnly,
                    "Stage5 south exterior cutaway renderer is invalid in edit mode.");
            }
        }

        private static void ValidateStage5ElevationMovement(
            PlayerHealth player,
            EnemyMotor[] motors,
            GameObject environmentRoot,
            Bounds navBounds)
        {
            Require(navBounds.size.y >= 0.35f,
                $"Stage5 baked NavMesh has no usable elevation span: {navBounds}");
            Require(player != null && motors.Length == 5,
                "Stage5 elevation validation requires the player and five enemy motors.");

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
                "Stage5 did not preserve disabled runtime stair/step colliders.");
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
                $"Stage5 actor is not configured for NavMesh height traversal: {actor.name}.");
        }

        private static int CountActiveObjects(List<GameObject> objects)
        {
            int count = 0;
            for (int i = 0; i < objects.Count; i++)
            {
                if (objects[i].activeInHierarchy)
                {
                    count++;
                }
            }

            return count;
        }

        private static int CountActiveAnnexRenderers(GameObject environmentRoot)
        {
            Renderer[] renderers = environmentRoot.GetComponentsInChildren<Renderer>(true);
            int count = 0;
            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i].gameObject.activeInHierarchy &&
                    IsRightAnnexContent(renderers[i].transform, renderers[i].bounds))
                {
                    count++;
                }
            }

            return count;
        }

        private static int CountActiveAnnexLights(GameObject environmentRoot)
        {
            Light[] lights = environmentRoot.GetComponentsInChildren<Light>(true);
            int count = 0;
            for (int i = 0; i < lights.Length; i++)
            {
                if (lights[i].gameObject.activeInHierarchy &&
                    IsRightAnnexContent(
                        lights[i].transform,
                        new Bounds(lights[i].transform.position, Vector3.zero)))
                {
                    count++;
                }
            }

            return count;
        }

        private static int CountActiveAnnexColliders(GameObject environmentRoot)
        {
            Collider[] colliders = environmentRoot.GetComponentsInChildren<Collider>(true);
            int count = 0;
            for (int i = 0; i < colliders.Length; i++)
            {
                if (colliders[i].enabled && colliders[i].gameObject.activeInHierarchy &&
                    IsRightAnnexContent(colliders[i].transform, colliders[i].bounds))
                {
                    count++;
                }
            }

            return count;
        }

        private static bool HasActiveEastDivider(GameObject environmentRoot)
        {
            Renderer[] renderers = environmentRoot.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer renderer = renderers[i];
                if (!renderer.gameObject.activeInHierarchy ||
                    Mathf.Abs(renderer.bounds.center.x - RightAnnexBoundaryX) > 0.25f ||
                    renderer.bounds.center.z < RightAnnexMinimumZ ||
                    !HasStructuralDividerName(renderer.transform, environmentRoot.transform))
                {
                    continue;
                }

                return true;
            }

            return false;
        }

        private static void ValidateDiveBarCamera(
            Scene scene,
            Transform player,
            Bounds navBounds)
        {
            Camera camera = FindActiveGameplayCamera(scene);
            TopDownCameraController controller = camera == null
                ? null
                : camera.GetComponent<TopDownCameraController>();
            Require(camera != null && controller != null,
                "Stage5 constrained camera rig is missing.");

            CalculateDiveBarCameraFraming(
                navBounds,
                out Vector3 expectedOffset,
                out Vector3 expectedFocusOffset);
            SerializedObject settings = new SerializedObject(controller);
            settings.Update();
            Vector3 actualOffset = settings.FindProperty("cameraOffset").vector3Value;
            Vector3 actualFocusOffset =
                settings.FindProperty("cameraFocusOffset").vector3Value;
            float actualAimLead =
                settings.FindProperty("aimLeadDistance").floatValue;
            bool actualConstrain =
                settings.FindProperty("constrainToBounds").boolValue;
            Bounds actualBounds = settings.FindProperty("cameraBounds").boundsValue;
            Bounds expectedBounds = CalculateCameraBounds(navBounds);

            Require(Vector3.Distance(actualOffset, expectedOffset) < 0.01f &&
                    Vector3.Distance(actualFocusOffset, expectedFocusOffset) < 0.01f &&
                    Mathf.Abs(actualAimLead - CameraAimLeadDistance) < 0.01f &&
                    Mathf.Abs(camera.fieldOfView - CameraFieldOfView) < 0.01f,
                $"Stage5 camera framing regressed. offset={actualOffset}, " +
                $"focusOffset={actualFocusOffset}, aimLead={actualAimLead:0.00}, " +
                $"FOV={camera.fieldOfView:0.0}.");
            Require(actualConstrain &&
                    Vector3.Distance(actualBounds.center, expectedBounds.center) < 0.01f &&
                    Vector3.Distance(actualBounds.size, expectedBounds.size) < 0.01f,
                $"Stage5 camera bounds do not match the NavMesh AABB. " +
                $"camera={actualBounds}, expected={expectedBounds}.");

            Vector3 lookDirection =
                -actualOffset + Vector3.up * CameraLookHeight;
            float downwardAngle = Mathf.Atan2(
                -lookDirection.y,
                new Vector2(lookDirection.x, lookDirection.z).magnitude) *
                Mathf.Rad2Deg;
            Require(Mathf.Abs(downwardAngle - CameraDownwardAngle) < 0.1f,
                $"Stage5 camera angle is {downwardAngle:0.0} instead of " +
                $"{CameraDownwardAngle:0.0} degrees.");

            float previousAspect = camera.aspect;
            try
            {
                camera.aspect = 16f / 9f;
                controller.SnapToTarget();
                Require(TryCalculateCameraGroundBounds(
                        camera,
                        expectedBounds.center.y,
                        out Bounds visibleBounds),
                    "Stage5 camera viewport does not intersect the NavMesh ground plane.");
                RequireFootprintConstrained(visibleBounds, expectedBounds, "initial");
                Vector3 playerViewport = camera.WorldToViewportPoint(
                    player.position + Vector3.up * ActorRootHeight);
                Require(playerViewport.z > 0f &&
                        playerViewport.x >= 0f && playerViewport.x <= 1f &&
                        playerViewport.y >= 0f && playerViewport.y <= 1f,
                    $"Stage5 player is outside the balanced camera frame: " +
                    $"viewport={playerViewport}.");
            }
            finally
            {
                camera.aspect = previousAspect;
                controller.SnapToTarget();
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
            Color[] expectedColors =
            {
                PlayerRingColor,
                RangedRingColor,
                ChaserRingColor,
                RangedRingColor,
                RangedRingColor,
                ChaserRingColor
            };

            for (int i = 0; i < owners.Length; i++)
            {
                GameObject owner = FindSceneRoot(scene, owners[i]);
                Transform ring = owner == null
                    ? null
                    : FindDirectChild(owner.transform, "Combat Identity Ring");
                Renderer renderer = ring == null ? null : ring.GetComponent<Renderer>();
                Material material = renderer == null ? null : renderer.sharedMaterial;
                Require(renderer != null && material != null,
                    $"Stage5 combat identity marker is missing for {owners[i]}.");
                Require(AssetDatabase.GetAssetPath(material) == expectedPaths[i] &&
                        material.shader != null &&
                        material.shader.name == "Unlit/Color" &&
                        ColorsApproximatelyEqual(material.color, expectedColors[i]),
                    $"Stage5 combat identity material regressed for {owners[i]}: " +
                    $"{AssetDatabase.GetAssetPath(material)}.");
                Require(renderer.shadowCastingMode == ShadowCastingMode.Off &&
                        !renderer.receiveShadows &&
                        renderer.lightProbeUsage == LightProbeUsage.Off &&
                        renderer.reflectionProbeUsage == ReflectionProbeUsage.Off,
                    $"Stage5 combat identity marker still receives scene lighting: " +
                    owners[i]);
            }
        }

        private static bool TryCalculateCameraGroundBounds(
            Camera camera,
            float groundHeight,
            out Bounds bounds)
        {
            bounds = default;
            bool found = false;
            Plane ground = new Plane(Vector3.up, new Vector3(0f, groundHeight, 0f));
            Vector2[] corners =
            {
                new Vector2(0f, 0f),
                new Vector2(1f, 0f),
                new Vector2(0f, 1f),
                new Vector2(1f, 1f)
            };
            for (int i = 0; i < corners.Length; i++)
            {
                Ray ray = camera.ViewportPointToRay(corners[i]);
                if (!ground.Raycast(ray, out float distance))
                {
                    return false;
                }

                Vector3 point = ray.GetPoint(distance);
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

            return found;
        }

        private static void RequireFootprintConstrained(
            Bounds visibleBounds,
            Bounds navBounds,
            string sample)
        {
            const float tolerance = 0.05f;
            if (visibleBounds.size.x <= navBounds.size.x + tolerance)
            {
                Require(visibleBounds.min.x >= navBounds.min.x - tolerance &&
                        visibleBounds.max.x <= navBounds.max.x + tolerance,
                    $"Stage5 camera escaped the horizontal map bounds at {sample}: " +
                    visibleBounds);
            }
            else
            {
                Require(Mathf.Abs(visibleBounds.center.x - navBounds.center.x) <= tolerance,
                    $"Stage5 oversized horizontal viewport is not centered at {sample}: " +
                    visibleBounds);
            }

            if (visibleBounds.size.z <= navBounds.size.z + tolerance)
            {
                Require(visibleBounds.min.z >= navBounds.min.z - tolerance &&
                        visibleBounds.max.z <= navBounds.max.z + tolerance,
                    $"Stage5 camera escaped the vertical map bounds at {sample}: " +
                    visibleBounds);
            }
            else
            {
                Require(Mathf.Abs(visibleBounds.center.z - navBounds.center.z) <= tolerance,
                    $"Stage5 oversized vertical viewport is not centered at {sample}: " +
                    visibleBounds);
            }
        }

        private static Bounds CalculateBounds(Vector3[] points)
        {
            Bounds bounds = new Bounds(points[0], Vector3.zero);
            for (int i = 1; i < points.Length; i++)
            {
                bounds.Encapsulate(points[i]);
            }

            return bounds;
        }

        private static bool ColorsApproximatelyEqual(Color first, Color second)
        {
            return Mathf.Abs(first.r - second.r) < 0.001f &&
                   Mathf.Abs(first.g - second.g) < 0.001f &&
                   Mathf.Abs(first.b - second.b) < 0.001f &&
                   Mathf.Abs(first.a - second.a) < 0.001f;
        }

        private static void RequireOnNavMesh(Vector3 position, string subject)
        {
            bool found = NavMesh.SamplePosition(
                position,
                out _,
                1.5f,
                NavMesh.AllAreas);
            Require(found,
                $"Stage5 {subject} is not on the baked NavMesh ({position}).");
        }

        private static void RequireCompletePath(
            Vector3 from,
            Vector3 to,
            string subject)
        {
            Require(NavMesh.SamplePosition(from, out NavMeshHit fromHit, 1.5f, NavMesh.AllAreas),
                "Stage5 player path origin is not on the NavMesh.");
            Require(NavMesh.SamplePosition(to, out NavMeshHit toHit, 1.5f, NavMesh.AllAreas),
                $"Stage5 {subject} path target is not on the NavMesh.");
            NavMeshPath path = new NavMeshPath();
            bool calculated = NavMesh.CalculatePath(
                fromHit.position,
                toHit.position,
                NavMesh.AllAreas,
                path);
            Require(calculated && path.status == NavMeshPathStatus.PathComplete,
                $"Stage5 player cannot reach {subject}; path status={path.status}.");
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
                RaycastHit[] hits = Physics.RaycastAll(
                    origin,
                    direction.normalized,
                    direction.magnitude,
                    ~0,
                    QueryTriggerInteraction.Ignore);
                bool blocked = false;
                for (int j = 0; j < hits.Length; j++)
                {
                    Transform hitTransform = hits[j].transform;
                    if (hitTransform.IsChildOf(player.transform) ||
                        hitTransform.IsChildOf(enemies[i].transform))
                    {
                        continue;
                    }

                    blocked = true;
                    break;
                }

                if (!blocked)
                {
                    return true;
                }
            }

            return false;
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
                    : FindDirectChild(owner.transform, DiveBarVisualNames[i]);
                Require(visual != null,
                    $"Stage5 visual is missing for {owners[i]}.");
                Require(visual.GetComponentInChildren<EnemyHealth>(true) == null &&
                        visual.GetComponentInChildren<EnemyMotor>(true) == null &&
                        visual.GetComponentInChildren<Rigidbody>(true) == null &&
                        visual.GetComponentInChildren<NavMeshAgent>(true) == null,
                    $"Stage5 visual child {visual.name} contains gameplay components.");

                Collider[] colliders = visual.GetComponentsInChildren<Collider>(true);
                for (int j = 0; j < colliders.Length; j++)
                {
                    Require(!colliders[j].enabled,
                        $"Stage5 visual collider is enabled: {colliders[j].name}");
                }

                Animator[] animators = visual.GetComponentsInChildren<Animator>(true);
                for (int j = 0; j < animators.Length; j++)
                {
                    Require(animators[j].enabled &&
                            !animators[j].applyRootMotion &&
                            animators[j].runtimeAnimatorController != null,
                        $"Stage5 visual Animator is not configured: {animators[j].name}");
                }

                Require(
                    visual.GetComponentInParent<CharacterAnimationController>() != null,
                    $"Stage5 character animation driver is missing: {visual.name}");
            }
        }

        private static void ValidateBuildOrder()
        {
            string[] expected =
            {
                "Assets/_Project/Scenes/Tutorial.unity",
                "Assets/_Project/Scenes/Stage1.unity",
                "Assets/_Project/Scenes/Stage2.unity",
                "Assets/_Project/Scenes/Stage3.unity",
                "Assets/_Project/Scenes/Stage4.unity",
                Stage5ScenePath
            };
            EditorBuildSettingsScene[] scenes = EditorBuildSettings.scenes;
            Require(scenes.Length >= expected.Length,
                "Build settings do not contain Tutorial through Stage5.");
            for (int i = 0; i < expected.Length; i++)
            {
                Require(scenes[i].enabled && scenes[i].path == expected[i],
                    $"Build index {i} is {scenes[i].path}; expected {expected[i]}.");
            }
        }

        private static int CountActiveCameras(Scene scene)
        {
            int count = 0;
            GameObject[] roots = scene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                Camera[] cameras = roots[i].GetComponentsInChildren<Camera>(true);
                for (int j = 0; j < cameras.Length; j++)
                {
                    if (cameras[j].isActiveAndEnabled)
                    {
                        count++;
                    }
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

        private static int CountNamedDiveBarVisuals(Scene scene)
        {
            HashSet<string> names = new HashSet<string>(DiveBarVisualNames);
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

        private static int CountActiveRenderers(Transform root)
        {
            Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
            int count = 0;
            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i].gameObject.activeInHierarchy)
                {
                    count++;
                }
            }

            return count;
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
                            if (property.propertyType == SerializedPropertyType.ObjectReference &&
                                property.objectReferenceValue == null &&
                                property.objectReferenceInstanceIDValue != 0)
                            {
                                return GetPath(transforms[j]) + "/" +
                                       component.GetType().Name + "." + property.propertyPath;
                            }
                        }
                    }
                }
            }

            return string.Empty;
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
    }
}
