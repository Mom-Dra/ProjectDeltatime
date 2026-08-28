using System;
using System.Collections.Generic;
using System.IO;
using Deltatime.Replay;
using Deltatime.TimeSystem;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Deltatime.EditorTools
{
    public static class WorldTimeAmbientSceneBuilder
    {
        private const string TutorialScenePath =
            GameBuildSceneCatalog.TutorialScenePath;
        private const string Stage1ScenePath =
            "Assets/_Project/Scenes/Stage1.unity";
        private const string Stage2ScenePath =
            "Assets/_Project/Scenes/Stage2.unity";
        private const string Stage5ScenePath =
            "Assets/_Project/Scenes/Stage5.unity";
        private const string SourceFanPrefabPath =
            "Assets/Synty/PolygonNightclubs/Prefabs/Buildings/" +
            "SM_Bld_Roof_Fan_01.prefab";
        private const string AmbientLoopPath =
            "Assets/_Project/Audio/SFX/Ambience/" +
            "SFX_WorldTime_IndustrialFan_Loop.ogg";
        private const string AmbientPrefabFolder =
            "Assets/_Project/Prefabs/Time";
        private const string AmbientPrefabPath =
            AmbientPrefabFolder + "/WorldTimeAmbientFan.prefab";
        private const string AnchorRootName =
            "World Time Ambient Anchors";
        private const string TutorialEnvironmentName =
            "Tutorial Environment";
        private const string PrototypeEnvironmentName =
            "Industrial Room";
        private const string Stage5EnvironmentName =
            "Stage 5 - Undertow Dive";
        private const string RotatingPartName =
            "SM_Bld_Roof_Fan_01_Fan_01";

        private static readonly Vector3[] TutorialPositions =
        {
            new Vector3(-5.8f, 0f, -19f),
            new Vector3(5.8f, 0f, 13f),
            new Vector3(-5.8f, 0f, 47f)
        };

        private static readonly Vector3[] TutorialReworkPositions =
        {
            new Vector3(-5.25f, 0f, -31.5f),
            new Vector3(5.25f, 0f, 19f),
            new Vector3(-5.25f, 0f, 39f)
        };

        private static readonly Vector3[] PrototypePositions =
        {
            new Vector3(-8.2f, 0f, 5.8f),
            new Vector3(8.2f, 0f, -2f)
        };

        [MenuItem("Tools/Prototype/Apply World Time Ambient Anchors")]
        public static void ApplyToProgressScenes()
        {
            if (!Application.isBatchMode &&
                !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            SceneSetup[] setup = Application.isBatchMode
                ? null
                : EditorSceneManager.GetSceneManagerSetup();
            try
            {
                EnsureAmbientFanPrefab();
                ApplyAndSave(TutorialScenePath);
                ApplyAndSave(Stage1ScenePath);
                ApplyAndSave(Stage2ScenePath);
                ApplyAndSave(Stage5ScenePath);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }
            finally
            {
                if (setup != null && setup.Length > 0)
                {
                    EditorSceneManager.RestoreSceneManagerSetup(setup);
                }
            }

            Debug.Log(
                "World-time ambient anchors applied to Tutorial, Stage1, " +
                "Stage2, and Stage5.");
        }

        public static void ApplyAndValidateFromCommandLine()
        {
            SceneBuildCommand.Run(() =>
            {
                ApplyToProgressScenes();
                ValidateSavedProgressScenes();
            });
        }

        [MenuItem("Tools/Prototype/Validate World Time Ambient Anchors")]
        public static void ValidateSavedProgressScenes()
        {
            ValidateSavedScene(TutorialScenePath, TutorialPositions.Length);
            ValidateSavedScene(Stage1ScenePath, PrototypePositions.Length);
            ValidateSavedScene(Stage2ScenePath, PrototypePositions.Length);
            ValidateSavedScene(Stage5ScenePath, 2);
            Debug.Log("World-time ambient anchor validation passed.");
        }

        public static void CapturePreviewsFromCommandLine()
        {
            SceneBuildCommand.Run(() =>
            {
                CaptureScenePreview(TutorialScenePath);
                CaptureScenePreview(Stage1ScenePath);
                CaptureScenePreview(Stage2ScenePath);
                CaptureScenePreview(Stage5ScenePath);
            });
        }

        public static void ApplyTutorialAnchors(
            Scene scene,
            WorldTimeController worldTime,
            Transform environment)
        {
            ApplyAnchors(
                scene,
                worldTime,
                environment,
                TutorialPositions);
        }

        public static void ApplyTutorialReworkAnchors(
            Scene scene,
            WorldTimeController worldTime,
            Transform environment)
        {
            Require(scene.path == TutorialScenePath,
                "Reworked Tutorial anchors may only be applied to the official scene.");
            ApplyAnchors(
                scene,
                worldTime,
                environment,
                TutorialReworkPositions);
        }

        public static void ApplyPrototypeAnchors(
            Scene scene,
            WorldTimeController worldTime)
        {
            GameObject environment = FindSceneRoot(
                scene,
                PrototypeEnvironmentName);
            Require(environment != null,
                "Industrial Room is missing before ambient anchor placement.");
            ApplyAnchors(
                scene,
                worldTime,
                environment.transform,
                PrototypePositions);
        }

        public static void ApplyStage5Anchors(
            Scene scene,
            WorldTimeController worldTime,
            GameObject environment)
        {
            Require(environment != null,
                "Stage5 environment is missing before ambient anchor placement.");
            Bounds floorBounds = CalculateStage5InteriorFloorBounds(environment);
            ApplyAnchors(
                scene,
                worldTime,
                environment.transform,
                Stage5Positions(floorBounds));
        }

        public static void ValidateScene(Scene scene, int expectedCount)
        {
            ValidateScene(scene, expectedCount, false);
        }

        public static void ValidateScene(
            Scene scene,
            int expectedCount,
            bool useTutorialReworkLayout)
        {
            WorldTimeAmbientAnchor[] anchors =
                FindSceneComponents<WorldTimeAmbientAnchor>(scene);
            Require(
                anchors.Length == expectedCount,
                $"{scene.name} requires {expectedCount} world-time ambient " +
                $"anchors, found {anchors.Length}.");

            WorldTimeController worldTime =
                FindSceneComponent<WorldTimeController>(scene);
            Require(worldTime != null,
                $"{scene.name} is missing WorldTimeController.");

            Vector3[] expectedPositions = ExpectedPositions(
                scene,
                useTutorialReworkLayout);
            Require(
                expectedPositions.Length == expectedCount,
                $"{scene.name} ambient validation has no expected layout.");

            Array.Sort(anchors, (left, right) =>
                string.CompareOrdinal(left.name, right.name));
            for (int i = 0; i < anchors.Length; i++)
            {
                WorldTimeAmbientAnchor anchor = anchors[i];
                SerializedObject serializedAnchor =
                    new SerializedObject(anchor);
                Require(
                    serializedAnchor.FindProperty("worldTime")
                        .objectReferenceValue == worldTime,
                    $"{anchor.name} has the wrong world-time source.");
                Transform rotatingPart = serializedAnchor
                    .FindProperty("rotatingPart")
                    .objectReferenceValue as Transform;
                Require(
                    rotatingPart != null &&
                    rotatingPart.name == RotatingPartName,
                    $"{anchor.name} has no valid rotating fan part.");
                ReplayIncluded[] replayIncluded =
                    anchor.GetComponentsInChildren<ReplayIncluded>(true);
                Require(
                    replayIncluded.Length == 1 &&
                    replayIncluded[0].transform == rotatingPart,
                    $"{anchor.name} must include only its rotating fan part " +
                    "in replay recording.");

                AudioSource source = anchor.GetComponent<AudioSource>();
                AudioLowPassFilter filter =
                    anchor.GetComponent<AudioLowPassFilter>();
                Require(source != null && filter != null,
                    $"{anchor.name} is missing its audio source or filter.");
                Require(
                    source.clip != null &&
                    source.clip.channels == 1 &&
                    AssetDatabase.GetAssetPath(source.clip) == AmbientLoopPath,
                    $"{anchor.name} has the wrong ambient loop.");
                Require(
                    source.loop && !source.playOnAwake &&
                    Mathf.Abs(source.spatialBlend - 1f) < 0.001f &&
                    Mathf.Abs(source.dopplerLevel) < 0.001f &&
                    Mathf.Abs(source.minDistance - 2.5f) < 0.001f &&
                    Mathf.Abs(source.maxDistance - 18f) < 0.001f,
                    $"{anchor.name} has invalid 3D loop settings.");
                Require(
                    anchor.GetComponent<ReplayExcluded>() != null,
                    $"{anchor.name} does not keep its static housing live " +
                    "during replay.");

                Collider[] colliders =
                    anchor.GetComponentsInChildren<Collider>(true);
                for (int colliderIndex = 0;
                     colliderIndex < colliders.Length;
                     colliderIndex++)
                {
                    Require(
                        !colliders[colliderIndex].enabled,
                        $"{anchor.name} still has an enabled collider: " +
                        colliders[colliderIndex].name);
                }

                Require(
                    Vector3.Distance(
                        anchor.transform.position,
                        expectedPositions[i]) < 0.05f,
                    $"{anchor.name} position is " +
                    $"{anchor.transform.position}, expected " +
                    $"{expectedPositions[i]}.");
            }
        }

        private static GameObject EnsureAmbientFanPrefab()
        {
            EnsureFolder(AmbientPrefabFolder);
            AssetDatabase.ImportAsset(
                AmbientLoopPath,
                ImportAssetOptions.ForceSynchronousImport);
            ConfigureAmbientAudioImporter();
            GameObject sourcePrefab =
                AssetDatabase.LoadAssetAtPath<GameObject>(SourceFanPrefabPath);
            AudioClip ambientLoop =
                AssetDatabase.LoadAssetAtPath<AudioClip>(AmbientLoopPath);
            Require(sourcePrefab != null,
                $"Missing Synty roof fan: {SourceFanPrefabPath}");
            Require(ambientLoop != null,
                $"Missing processed ambient loop: {AmbientLoopPath}");

            GameObject root = new GameObject("WorldTimeAmbientFan");
            try
            {
                root.AddComponent<ReplayExcluded>();
                GameObject visual = PrefabUtility.InstantiatePrefab(
                    sourcePrefab) as GameObject;
                Require(visual != null,
                    "Could not instantiate the Synty roof fan prefab.");
                visual.name = "Fan Visual";
                visual.transform.SetParent(root.transform, false);

                Transform rotatingPart = null;
                Transform[] transforms =
                    visual.GetComponentsInChildren<Transform>(true);
                for (int i = 0; i < transforms.Length; i++)
                {
                    if (transforms[i].name == RotatingPartName)
                    {
                        rotatingPart = transforms[i];
                        break;
                    }
                }

                Require(rotatingPart != null,
                    "The Synty roof fan has no separate rotating part.");
                rotatingPart.gameObject.AddComponent<ReplayIncluded>();
                Collider[] colliders =
                    visual.GetComponentsInChildren<Collider>(true);
                for (int i = 0; i < colliders.Length; i++)
                {
                    colliders[i].enabled = false;
                }

                AudioSource source = root.AddComponent<AudioSource>();
                source.clip = ambientLoop;
                source.loop = true;
                source.playOnAwake = false;
                source.volume = 0f;
                source.spatialBlend = 1f;
                source.dopplerLevel = 0f;
                source.rolloffMode = AudioRolloffMode.Logarithmic;
                source.minDistance = 2.5f;
                source.maxDistance = 18f;

                AudioLowPassFilter filter =
                    root.AddComponent<AudioLowPassFilter>();
                filter.cutoffFrequency = 16000f;

                WorldTimeAmbientAnchor anchor =
                    root.AddComponent<WorldTimeAmbientAnchor>();
                SerializedObject serializedAnchor =
                    new SerializedObject(anchor);
                serializedAnchor.FindProperty("rotatingPart")
                    .objectReferenceValue = rotatingPart;
                serializedAnchor.FindProperty("localRotationAxis")
                    .vector3Value = Vector3.up;
                serializedAnchor.FindProperty("rotationDegreesPerWorldSecond")
                    .floatValue = 240f;
                serializedAnchor.FindProperty("loopSource")
                    .objectReferenceValue = source;
                serializedAnchor.FindProperty("lowPassFilter")
                    .objectReferenceValue = filter;
                serializedAnchor.FindProperty("baseVolume")
                    .floatValue = 0.22f;
                serializedAnchor.FindProperty("responseDuration")
                    .floatValue = 0.15f;
                serializedAnchor.ApplyModifiedPropertiesWithoutUndo();

                GameObject saved = PrefabUtility.SaveAsPrefabAsset(
                    root,
                    AmbientPrefabPath,
                    out bool success);
                Require(success && saved != null,
                    $"Failed to save {AmbientPrefabPath}.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }

            AssetDatabase.ImportAsset(
                AmbientPrefabPath,
                ImportAssetOptions.ForceSynchronousImport);
            return AssetDatabase.LoadAssetAtPath<GameObject>(
                AmbientPrefabPath);
        }

        private static void ConfigureAmbientAudioImporter()
        {
            AudioImporter importer =
                AssetImporter.GetAtPath(AmbientLoopPath) as AudioImporter;
            Require(importer != null,
                $"Could not load AudioImporter for {AmbientLoopPath}.");

            AudioImporterSampleSettings settings =
                importer.defaultSampleSettings;
            SerializedObject serializedImporter =
                new SerializedObject(importer);
            SerializedProperty normalize =
                serializedImporter.FindProperty("m_Normalize") ??
                serializedImporter.FindProperty("normalize");
            bool normalizeEnabled =
                normalize != null && normalize.boolValue;
            bool changed = importer.forceToMono ||
                           normalizeEnabled ||
                           !settings.preloadAudioData ||
                           importer.loadInBackground ||
                           settings.loadType !=
                           AudioClipLoadType.CompressedInMemory ||
                           settings.compressionFormat !=
                           AudioCompressionFormat.Vorbis ||
                           Mathf.Abs(settings.quality - 0.7f) > 0.001f ||
                           settings.sampleRateSetting !=
                           AudioSampleRateSetting.PreserveSampleRate;
            if (!changed)
            {
                return;
            }

            // The repository asset is already mono and peak-normalized to
            // -3 dBFS. Keep Unity from normalizing it again on import.
            if (normalizeEnabled)
            {
                normalize.boolValue = false;
                serializedImporter.ApplyModifiedPropertiesWithoutUndo();
            }

            importer.forceToMono = false;
            importer.loadInBackground = false;
            settings.preloadAudioData = true;
            settings.loadType = AudioClipLoadType.CompressedInMemory;
            settings.compressionFormat = AudioCompressionFormat.Vorbis;
            settings.quality = 0.7f;
            settings.sampleRateSetting =
                AudioSampleRateSetting.PreserveSampleRate;
            importer.defaultSampleSettings = settings;
            importer.SaveAndReimport();
        }

        private static void ApplyAndSave(string scenePath)
        {
            Scene scene = EditorSceneManager.OpenScene(
                scenePath,
                OpenSceneMode.Single);
            ApplyLayoutForSavedScene(scene);
            EditorSceneManager.MarkSceneDirty(scene);
            Require(
                EditorSceneManager.SaveScene(scene, scenePath),
                $"Failed to save ambient anchors in {scenePath}.");
            ValidateScene(scene, ExpectedPositions(scene).Length);
        }

        private static void ApplyLayoutForSavedScene(Scene scene)
        {
            WorldTimeController worldTime =
                FindSceneComponent<WorldTimeController>(scene);
            Require(worldTime != null,
                $"{scene.path} is missing WorldTimeController.");
            string fileName = System.IO.Path.GetFileNameWithoutExtension(
                scene.path);
            if (scene.path == TutorialScenePath)
            {
                GameObject environment = FindSceneRoot(
                    scene,
                    TutorialEnvironmentName);
                Require(environment != null,
                    "Tutorial Environment root is missing.");
                ApplyTutorialReworkAnchors(
                    scene,
                    worldTime,
                    environment.transform);
                return;
            }

            if (fileName == "Tutorial")
            {
                GameObject environment = FindSceneRoot(
                    scene,
                    TutorialEnvironmentName);
                Require(environment != null,
                    "Tutorial Environment root is missing.");
                ApplyTutorialAnchors(
                    scene,
                    worldTime,
                    environment.transform);
                return;
            }

            if (fileName == "Stage1" || fileName == "Stage2")
            {
                ApplyPrototypeAnchors(scene, worldTime);
                return;
            }

            if (fileName == "Stage5")
            {
                ApplyStage5Anchors(
                    scene,
                    worldTime,
                    FindSceneRoot(scene, Stage5EnvironmentName));
                return;
            }

            throw new InvalidOperationException(
                $"Unsupported ambient anchor scene: {scene.path}");
        }

        private static void ApplyAnchors(
            Scene scene,
            WorldTimeController worldTime,
            Transform environment,
            Vector3[] positions)
        {
            Require(scene.IsValid() && scene.isLoaded,
                "Ambient anchor target scene is not loaded.");
            Require(worldTime != null && worldTime.gameObject.scene == scene,
                "Ambient anchors require the target scene's WorldTimeController.");
            Require(environment != null && environment.gameObject.scene == scene,
                "Ambient anchors require the target scene's environment root.");

            Transform existing = FindDirectChild(environment, AnchorRootName);
            if (existing != null)
            {
                UnityEngine.Object.DestroyImmediate(existing.gameObject);
            }

            GameObject prefab = EnsureAmbientFanPrefab();
            GameObject root = new GameObject(AnchorRootName);
            SceneManager.MoveGameObjectToScene(root, scene);
            root.transform.SetParent(environment, true);
            root.AddComponent<ReplayExcluded>();

            for (int i = 0; i < positions.Length; i++)
            {
                GameObject instance = PrefabUtility.InstantiatePrefab(
                    prefab,
                    scene) as GameObject;
                Require(instance != null,
                    "Could not instantiate the world-time ambient fan.");
                instance.name = $"World Time Ambient Anchor {i + 1:00}";
                instance.transform.SetParent(root.transform, true);
                instance.transform.position = positions[i];
                instance.transform.rotation = Quaternion.identity;
                WorldTimeAmbientAnchor anchor =
                    instance.GetComponent<WorldTimeAmbientAnchor>();
                Require(anchor != null,
                    $"{AmbientPrefabPath} has no ambient anchor component.");
                anchor.Configure(worldTime);
                EditorUtility.SetDirty(anchor);
            }

            EditorSceneManager.MarkSceneDirty(scene);
        }

        private static Vector3[] ExpectedPositions(
            Scene scene,
            bool useTutorialReworkLayout = false)
        {
            if (useTutorialReworkLayout || scene.path == TutorialScenePath)
            {
                Require(scene.path == TutorialScenePath,
                    "Reworked Tutorial validation requires the official scene path.");
                return TutorialReworkPositions;
            }

            string fileName = System.IO.Path.GetFileNameWithoutExtension(
                scene.path);
            if (fileName == "Tutorial")
            {
                return TutorialPositions;
            }

            if (fileName == "Stage1" || fileName == "Stage2")
            {
                return PrototypePositions;
            }

            if (fileName == "Stage5")
            {
                GameObject environment = FindSceneRoot(
                    scene,
                    Stage5EnvironmentName);
                Require(environment != null,
                    "Stage5 environment is missing during validation.");
                return Stage5Positions(
                    CalculateStage5InteriorFloorBounds(environment));
            }

            return Array.Empty<Vector3>();
        }

        private static Vector3[] Stage5Positions(Bounds floorBounds)
        {
            return new[]
            {
                new Vector3(
                    floorBounds.min.x + 0.8f,
                    floorBounds.max.y,
                    floorBounds.center.z - floorBounds.size.z * 0.25f),
                new Vector3(
                    floorBounds.max.x - 0.8f,
                    floorBounds.max.y,
                    floorBounds.center.z + floorBounds.size.z * 0.25f)
            };
        }

        private static Bounds CalculateStage5InteriorFloorBounds(
            GameObject environment)
        {
            Renderer[] renderers =
                environment.GetComponentsInChildren<Renderer>(true);
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

            Require(
                found && result.size.x >= 15f && result.size.z >= 20f,
                $"Could not derive Stage5 interior floor bounds: {result}");
            return result;
        }

        private static void ValidateSavedScene(
            string scenePath,
            int expectedCount)
        {
            Scene scene = EditorSceneManager.OpenScene(
                scenePath,
                OpenSceneMode.Single);
            ValidateScene(scene, expectedCount);
        }

        private static void CaptureScenePreview(string scenePath)
        {
            Scene scene = EditorSceneManager.OpenScene(
                scenePath,
                OpenSceneMode.Single);
            Vector3[] expected = ExpectedPositions(scene);
            ValidateScene(scene, expected.Length);

            Camera camera = FindSceneComponent<Camera>(scene);
            Require(camera != null,
                $"{scene.name} ambient preview requires a camera.");
            WorldTimeAmbientAnchor[] anchors =
                FindSceneComponents<WorldTimeAmbientAnchor>(scene);
            Array.Sort(anchors, (left, right) =>
                string.CompareOrdinal(left.name, right.name));

            Vector3 layoutCenter = Vector3.zero;
            for (int i = 0; i < expected.Length; i++)
            {
                layoutCenter += expected[i];
            }

            layoutCenter /= expected.Length;
            Vector3 target = anchors[0].transform.position +
                             Vector3.up * 0.8f;
            Vector3 inward = layoutCenter - anchors[0].transform.position;
            inward.y = 0f;
            if (inward.sqrMagnitude < 0.01f)
            {
                inward = Vector3.forward;
            }

            Vector3 cameraPosition = target +
                                     inward.normalized * 7f +
                                     Vector3.up * 4.5f;
            camera.transform.SetPositionAndRotation(
                cameraPosition,
                Quaternion.LookRotation(
                    target - cameraPosition,
                    Vector3.up));
            camera.orthographic = false;
            camera.fieldOfView = 52f;
            camera.nearClipPlane = 0.05f;
            camera.farClipPlane = 120f;

            string outputPath = Path.Combine(
                Path.GetTempPath(),
                $"ProjectDeltatime-WorldTimeAmbient-{scene.name}.png");
            GameObject lightObject = new GameObject("Ambient Preview Light");
            SceneManager.MoveGameObjectToScene(lightObject, scene);
            Light previewLight = lightObject.AddComponent<Light>();
            previewLight.type = LightType.Directional;
            previewLight.intensity = 1.6f;
            previewLight.shadows = LightShadows.None;
            lightObject.transform.rotation = Quaternion.Euler(50f, -35f, 0f);
            try
            {
                PreviewCapture.CapturePng(
                    camera,
                    960,
                    720,
                    outputPath);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(lightObject);
            }
            Debug.Log($"World-time ambient preview captured: {outputPath}");
        }

        private static T FindSceneComponent<T>(Scene scene)
            where T : Component
        {
            T[] components = FindSceneComponents<T>(scene);
            return components.Length == 0 ? null : components[0];
        }

        private static T[] FindSceneComponents<T>(Scene scene)
            where T : Component
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

        private static void EnsureFolder(string path)
        {
            string[] parts = path.Split('/');
            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, parts[i]);
                }

                current = next;
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
