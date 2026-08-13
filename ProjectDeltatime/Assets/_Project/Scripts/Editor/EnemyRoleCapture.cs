using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace Deltatime.EditorTools
{
    public static class EnemyRoleCapture
    {
        private const string OutputDirectory =
            "Assets/_Project/Art/Generated/EnemyRoles";

        private const int ImageSize = 1024;

        private readonly struct RoleSpec
        {
            public RoleSpec(
                string fileName,
                string characterPath,
                string weaponPath,
                Vector3 localPosition,
                Vector3 localEulerAngles,
                Vector3 localScale)
            {
                FileName = fileName;
                CharacterPath = characterPath;
                WeaponPath = weaponPath;
                LocalPosition = localPosition;
                LocalEulerAngles = localEulerAngles;
                LocalScale = localScale;
            }

            public string FileName { get; }
            public string CharacterPath { get; }
            public string WeaponPath { get; }
            public Vector3 LocalPosition { get; }
            public Vector3 LocalEulerAngles { get; }
            public Vector3 LocalScale { get; }
        }

        private static readonly RoleSpec[] Roles =
        {
            new RoleSpec(
                "EnemyRole_Melee",
                "Assets/Synty/PolygonNightclubs/Prefabs/Characters/" +
                "SM_Chr_Bouncer_Male_01.prefab",
                "Assets/_Project/Animation/BaseballBat_Raw_Wood_Clean.prefab",
                new Vector3(0.019f, 0.021f, 0.093f),
                new Vector3(189.308f, -24.15198f, -6.239014f),
                Vector3.one),
            new RoleSpec(
                "EnemyRole_Firearm",
                "Assets/Synty/PolygonNightclubs/Prefabs/Characters/" +
                "SM_Chr_Bartender_Male_01.prefab",
                "Assets/_Project/Animation/TacticalPistol.prefab",
                new Vector3(0.08f, 0.03f, -0.039f),
                new Vector3(11.737f, 65.521f, -448.114f),
                Vector3.one * 0.65f),
            new RoleSpec(
                "EnemyRole_Unarmed",
                "Assets/Synty/PolygonNightclubs/Prefabs/Characters/" +
                "SM_Chr_Party_Male_02.prefab",
                null,
                Vector3.zero,
                Vector3.zero,
                Vector3.one)
        };

        public static void CaptureFromCommandLine()
        {
            CaptureAll();
        }

        [MenuItem("Tools/Prototype/Capture Enemy Role Images")]
        public static void CaptureAll()
        {
            Directory.CreateDirectory(ToAbsolutePath(OutputDirectory));

            Scene previousScene = SceneManager.GetActiveScene();
            string previousScenePath = previousScene.path;
            if (previousScene.isDirty)
            {
                throw new InvalidOperationException(
                    "Save the current scene before capturing enemy role images.");
            }

            Scene captureScene = EditorSceneManager.NewScene(
                NewSceneSetup.EmptyScene,
                NewSceneMode.Single);

            if (!captureScene.IsValid() || !captureScene.isLoaded)
            {
                throw new InvalidOperationException(
                    "Could not create or activate the temporary capture scene.");
            }

            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.08f, 0.09f, 0.12f, 1f);
            RenderSettings.fog = false;

            try
            {
                for (int i = 0; i < Roles.Length; i++)
                {
                    CaptureRole(captureScene, Roles[i]);
                }

                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
                Debug.Log(
                    $"Captured {Roles.Length} enemy role images to {OutputDirectory}.");
            }
            finally
            {
                if (!string.IsNullOrEmpty(previousScenePath))
                {
                    EditorSceneManager.OpenScene(
                        previousScenePath,
                        OpenSceneMode.Single);
                }
            }
        }

        private static void CaptureRole(Scene scene, RoleSpec role)
        {
            GameObject characterPrefab =
                AssetDatabase.LoadAssetAtPath<GameObject>(role.CharacterPath);
            if (characterPrefab == null)
            {
                throw new InvalidOperationException(
                    $"Missing character prefab: {role.CharacterPath}");
            }

            GameObject character = (GameObject)PrefabUtility.InstantiatePrefab(
                characterPrefab,
                scene);
            character.name = role.FileName;
            character.transform.position = Vector3.zero;
            character.transform.rotation = Quaternion.identity;

            DisablePreviewAnimation(character);

            if (!string.IsNullOrEmpty(role.WeaponPath))
            {
                GameObject weaponPrefab =
                    AssetDatabase.LoadAssetAtPath<GameObject>(role.WeaponPath);
                if (weaponPrefab == null)
                {
                    throw new InvalidOperationException(
                        $"Missing weapon prefab: {role.WeaponPath}");
                }

                Transform hand = FindDeepChild(character.transform, "Hand_R");
                if (hand == null)
                {
                    throw new InvalidOperationException(
                        $"Right hand was not found on {role.CharacterPath}.");
                }

                GameObject weapon = (GameObject)PrefabUtility.InstantiatePrefab(
                    weaponPrefab,
                    scene);
                weapon.name = role.FileName + " Weapon";
                weapon.transform.SetParent(hand, false);
                weapon.transform.localPosition = role.LocalPosition;
                weapon.transform.localEulerAngles = role.LocalEulerAngles;
                weapon.transform.localScale = role.LocalScale;
            }

            Bounds bounds = CalculateRendererBounds(character);
            GameObject floor = CreateFloor(scene, bounds);
            Camera camera = CreateCamera(scene, bounds);
            GameObject[] lights = CreateLights(scene, bounds);

            Debug.Log(
                $"Capturing {role.FileName}: renderers={character.GetComponentsInChildren<Renderer>(true).Length}, " +
                $"bounds={bounds}, camera={camera.transform.position}");

            string assetPath = OutputDirectory + "/" + role.FileName + ".png";
            RenderToPng(camera, assetPath);

            UnityEngine.Object.DestroyImmediate(camera.gameObject);
            for (int i = 0; i < lights.Length; i++)
            {
                UnityEngine.Object.DestroyImmediate(lights[i]);
            }

            Renderer floorRenderer = floor.GetComponent<Renderer>();
            Material floorMaterial = floorRenderer.sharedMaterial;
            floorRenderer.sharedMaterial = null;
            UnityEngine.Object.DestroyImmediate(floorMaterial);
            UnityEngine.Object.DestroyImmediate(floor);
            UnityEngine.Object.DestroyImmediate(character);
        }

        private static void DisablePreviewAnimation(GameObject root)
        {
            Animator[] animators = root.GetComponentsInChildren<Animator>(true);
            for (int i = 0; i < animators.Length; i++)
            {
                animators[i].enabled = false;
            }
        }

        private static GameObject CreateFloor(Scene scene, Bounds characterBounds)
        {
            GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
            floor.name = "Enemy Role Capture Floor";
            SceneManager.MoveGameObjectToScene(floor, scene);
            floor.transform.position = new Vector3(
                characterBounds.center.x,
                characterBounds.min.y - 0.01f,
                characterBounds.center.z);
            floor.transform.localScale = Vector3.one * 0.45f;

            Renderer renderer = floor.GetComponent<Renderer>();
            renderer.sharedMaterial = CreateMaterial(
                new Color(0.055f, 0.065f, 0.08f, 1f));
            return floor;
        }

        private static Camera CreateCamera(Scene scene, Bounds bounds)
        {
            GameObject cameraObject = new GameObject("Enemy Role Capture Camera");
            SceneManager.MoveGameObjectToScene(cameraObject, scene);
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.018f, 0.022f, 0.032f, 1f);
            camera.fieldOfView = 28f;
            camera.aspect = 1f;
            camera.nearClipPlane = 0.01f;
            camera.farClipPlane = 100f;

            float height = Mathf.Max(0.1f, bounds.size.y);
            float distance = height / (2f * Mathf.Tan(camera.fieldOfView * 0.5f * Mathf.Deg2Rad));
            Vector3 target = bounds.center + new Vector3(0f, height * 0.03f, 0f);
            Vector3 viewDirection = new Vector3(0.42f, 0.14f, 1f).normalized;
            camera.transform.position = target + viewDirection * distance * 1.12f;
            camera.transform.LookAt(target, Vector3.up);
            return camera;
        }

        private static GameObject[] CreateLights(Scene scene, Bounds bounds)
        {
            GameObject keyObject = new GameObject("Enemy Role Capture Key");
            SceneManager.MoveGameObjectToScene(keyObject, scene);
            keyObject.transform.rotation = Quaternion.Euler(35f, -35f, 0f);
            Light key = keyObject.AddComponent<Light>();
            key.type = LightType.Directional;
            key.color = new Color(1f, 0.92f, 0.82f, 1f);
            key.intensity = 1.15f;

            GameObject fillObject = new GameObject("Enemy Role Capture Fill");
            SceneManager.MoveGameObjectToScene(fillObject, scene);
            fillObject.transform.rotation = Quaternion.Euler(25f, 140f, 0f);
            Light fill = fillObject.AddComponent<Light>();
            fill.type = LightType.Directional;
            fill.color = new Color(0.55f, 0.72f, 1f, 1f);
            fill.intensity = 0.35f;

            return new[] { keyObject, fillObject };
        }

        private static void RenderToPng(Camera camera, string assetPath)
        {
            RenderTexture target = new RenderTexture(
                ImageSize,
                ImageSize,
                24,
                RenderTextureFormat.ARGB32);
            target.Create();

            RenderTexture previousActive = RenderTexture.active;
            RenderTexture previousTarget = camera.targetTexture;
            Texture2D image = new Texture2D(
                ImageSize,
                ImageSize,
                TextureFormat.RGBA32,
                false);

            try
            {
                camera.targetTexture = target;
                RenderTexture.active = target;
                camera.Render();
                image.ReadPixels(
                    new Rect(0f, 0f, ImageSize, ImageSize),
                    0,
                    0);
                image.Apply();
                File.WriteAllBytes(
                    ToAbsolutePath(assetPath),
                    image.EncodeToPNG());
            }
            finally
            {
                camera.targetTexture = previousTarget;
                RenderTexture.active = previousActive;
                UnityEngine.Object.DestroyImmediate(image);
                target.Release();
                UnityEngine.Object.DestroyImmediate(target);
            }
        }

        private static Bounds CalculateRendererBounds(GameObject root)
        {
            Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
            {
                throw new InvalidOperationException(
                    $"No renderers found on {root.name}.");
            }

            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
            {
                bounds.Encapsulate(renderers[i].bounds);
            }

            return bounds;
        }

        private static Transform FindDeepChild(Transform root, string childName)
        {
            if (root.name == childName)
            {
                return root;
            }

            for (int i = 0; i < root.childCount; i++)
            {
                Transform result = FindDeepChild(root.GetChild(i), childName);
                if (result != null)
                {
                    return result;
                }
            }

            return null;
        }

        private static Material CreateMaterial(Color color)
        {
            Shader shader = Shader.Find("Standard");
            Material material = new Material(shader)
            {
                color = color
            };
            return material;
        }

        private static string ToAbsolutePath(string projectRelativePath)
        {
            return Path.Combine(
                Directory.GetParent(Application.dataPath).FullName,
                projectRelativePath.Replace('/', Path.DirectorySeparatorChar));
        }
    }
}
