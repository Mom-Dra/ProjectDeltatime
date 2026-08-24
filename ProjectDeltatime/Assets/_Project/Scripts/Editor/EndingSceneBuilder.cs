using System;
using Deltatime.UI;
using Deltatime.InputSystem;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Deltatime.EditorTools
{
    /// <summary>
    /// Creates the completion screen from the artist-authored title scene while
    /// replacing its Play action with the keyboard-only return to the title.
    /// </summary>
    public static class EndingSceneBuilder
    {
        private const string MainScenePath =
            "Assets/_Project/Scenes/MainScene.unity";
        private const string EndingScenePath =
            "Assets/_Project/Scenes/EndingScene.unity";
        private static readonly Vector2 EndingTitleSize =
            new Vector2(920f, 108f);
        private static readonly Vector2 EndingTitlePosition =
            new Vector2(0f, 96f);
        private static readonly Vector2 EndingInstructionSize =
            new Vector2(920f, 64f);
        private static readonly Vector2 EndingInstructionPosition =
            new Vector2(0f, 8f);

        [MenuItem("Tools/Ending/Build Ending Scene")]
        public static void BuildEndingScene()
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(MainScenePath) == null)
            {
                throw new InvalidOperationException(
                    $"Ending scene requires {MainScenePath}.");
            }

            Scene mainScene = EditorSceneManager.OpenScene(
                MainScenePath,
                OpenSceneMode.Single);
            if (!EditorSceneManager.SaveScene(
                    mainScene,
                    EndingScenePath,
                    true))
            {
                throw new InvalidOperationException(
                    $"Failed to create {EndingScenePath}.");
            }

            Scene endingScene = EditorSceneManager.OpenScene(
                EndingScenePath,
                OpenSceneMode.Single);
            ConfigureEndingScene(endingScene);
            EditorSceneManager.MarkSceneDirty(endingScene);
            if (!EditorSceneManager.SaveScene(endingScene))
            {
                throw new InvalidOperationException(
                    $"Failed to save {EndingScenePath}.");
            }

            GameBuildSceneCatalog.Apply();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        public static void BuildFromCommandLine()
        {
            BuildEndingScene();
            Debug.Log("Ending scene built successfully.");
        }

        public static void BuildAndValidateFromCommandLine()
        {
            SceneBuildCommand.Run(() =>
            {
                BuildEndingScene();
                ValidateEndingScene();
                Debug.Log("Ending scene build and validation completed.");
            });
        }

        [MenuItem("Tools/Ending/Validate Ending Scene")]
        public static void ValidateEndingScene()
        {
            Scene scene = EditorSceneManager.OpenScene(
                EndingScenePath,
                OpenSceneMode.Single);
            Canvas canvas = FindRequiredComponent<Canvas>(scene, "Canvas");
            Require(canvas.GetComponent<MainMenuController>() == null,
                "EndingScene must not retain MainMenuController.");
            Require(canvas.GetComponent<EndingSceneController>() != null,
                "EndingScene is missing EndingSceneController.");
            Require(FindRequiredComponent<TextMeshProUGUI>(scene, "EndingTitle").text ==
                    "STAGE CLEAR",
                "EndingScene must show STAGE CLEAR.");
            Require(
                FindRequiredComponent<TextMeshProUGUI>(
                    scene,
                    "EndingInstruction").text ==
                $"Press {InputBindingDisplay.Get("NextStage")} to return to Main Menu",
                "EndingScene must explain the current Next Stage binding.");
            Require(FindObject(scene, "MenuRoot") == null &&
                    FindObject(scene, "OptionPanel") == null &&
                    FindObject(scene, "CreditsPanel") == null,
                "EndingScene must remove all main-menu interaction hierarchies.");
            GameBuildSceneCatalog.Validate();
        }

        private static void ConfigureEndingScene(Scene scene)
        {
            Canvas canvas = FindRequiredComponent<Canvas>(scene, "Canvas");
            MainMenuController menuController =
                canvas.GetComponent<MainMenuController>();
            if (menuController != null)
            {
                UnityEngine.Object.DestroyImmediate(menuController);
            }

            DestroyChild(canvas.transform, "MenuRoot");
            DestroyChild(canvas.transform, "OptionPanel");
            DestroyChild(canvas.transform, "CreditsPanel");

            Button[] buttons = canvas.GetComponentsInChildren<Button>(true);
            for (int i = 0; i < buttons.Length; i++)
            {
                UnityEngine.Object.DestroyImmediate(buttons[i].gameObject);
            }

            EndingSceneController controller = GetOrAddComponent<EndingSceneController>(canvas.gameObject);
            RectTransform canvasTransform = canvas.GetComponent<RectTransform>();
            ConfigureLabel(
                canvasTransform,
                "EndingTitle",
                "STAGE CLEAR",
                EndingTitleSize,
                EndingTitlePosition,
                56f);
            TextMeshProUGUI instruction = ConfigureLabel(
                canvasTransform,
                "EndingInstruction",
                $"Press {InputBindingDisplay.Get("NextStage")} to return to Main Menu",
                EndingInstructionSize,
                EndingInstructionPosition,
                28f);
            controller.Configure(instruction);
            EditorUtility.SetDirty(controller);
        }

        private static TextMeshProUGUI ConfigureLabel(
            RectTransform canvasTransform,
            string objectName,
            string text,
            Vector2 size,
            Vector2 position,
            float fontSize)
        {
            Transform existing = canvasTransform.Find(objectName);
            GameObject labelObject = existing != null
                ? existing.gameObject
                : new GameObject(
                    objectName,
                    typeof(RectTransform),
                    typeof(CanvasRenderer),
                    typeof(TextMeshProUGUI));
            RectTransform labelTransform =
                labelObject.GetComponent<RectTransform>();
            labelTransform.SetParent(canvasTransform, false);
            labelTransform.anchorMin = new Vector2(0.5f, 0.5f);
            labelTransform.anchorMax = new Vector2(0.5f, 0.5f);
            labelTransform.pivot = new Vector2(0.5f, 0.5f);
            labelTransform.anchoredPosition = position;
            labelTransform.sizeDelta = size;
            labelTransform.localScale = Vector3.one;

            Require(TMP_Settings.defaultFontAsset != null,
                "TextMeshPro default font asset is required for EndingScene.");
            TextMeshProUGUI label =
                GetOrAddComponent<TextMeshProUGUI>(labelObject);
            label.font = TMP_Settings.defaultFontAsset;
            label.text = text;
            label.fontStyle = FontStyles.Bold;
            label.fontSize = fontSize;
            label.alignment = TextAlignmentOptions.Center;
            label.color = Color.white;
            label.raycastTarget = false;
            label.enableWordWrapping = false;
            label.overflowMode = TextOverflowModes.Overflow;
            labelTransform.SetAsLastSibling();
            return label;
        }

        private static void DestroyChild(Transform parent, string name)
        {
            Transform child = parent.Find(name);
            if (child != null) UnityEngine.Object.DestroyImmediate(child.gameObject);
        }

        private static GameObject FindObject(Scene scene, string name)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                Transform[] transforms = root.GetComponentsInChildren<Transform>(true);
                for (int i = 0; i < transforms.Length; i++)
                {
                    if (transforms[i].name == name) return transforms[i].gameObject;
                }
            }
            return null;
        }

        private static T FindRequiredComponent<T>(Scene scene, string objectName)
            where T : Component
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                T[] components = root.GetComponentsInChildren<T>(true);
                for (int i = 0; i < components.Length; i++)
                {
                    if (components[i].gameObject.name == objectName)
                    {
                        return components[i];
                    }
                }
            }

            throw new InvalidOperationException(
                $"Could not find {typeof(T).Name} on '{objectName}' in {scene.path}.");
        }

        private static T GetOrAddComponent<T>(GameObject gameObject)
            where T : Component
        {
            T component = gameObject.GetComponent<T>();
            return component != null ? component : gameObject.AddComponent<T>();
        }

        private static void Require(bool condition, string message)
        {
            SceneValidation.Require(condition, message);
        }
    }
}
