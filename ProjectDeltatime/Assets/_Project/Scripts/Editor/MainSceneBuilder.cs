using System.Collections.Generic;
using Deltatime.UI;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Deltatime.EditorTools
{
    /// <summary>
    /// Applies the title-screen layout to the artist-authored MainScene without recreating its artwork.
    /// </summary>
    public static class MainSceneBuilder
    {
        private const string MainScenePath = "Assets/_Project/Scenes/MainScene.unity";
        private const string PlaySceneName = "Tutorial";
        private const string PlayButtonText = "게임 시작";
        private static readonly Vector2 ReferenceResolution = new Vector2(1920f, 1080f);
        private static readonly Vector2 TitleSize = new Vector2(600f, 400f);
        private static readonly Vector2 TitlePosition = new Vector2(72f, -56f);
        private static readonly Vector2 PlayButtonSize = new Vector2(256f, 72f);
        private static readonly Vector2 PlayButtonPosition = new Vector2(108f, -458f);
        private const float PlayHoverScale = 1.08f;
        private static readonly Color PlayPressedColor = new Color(224f / 255f, 28f / 255f, 28f / 255f, 1f);

        [MenuItem("Tools/Main Menu/Build Main Scene")]
        public static void BuildMainScene()
        {
            Scene scene = EditorSceneManager.OpenScene(MainScenePath, OpenSceneMode.Single);
            Canvas canvas = FindRequiredComponent<Canvas>(scene, "Canvas");
            RectTransform canvasTransform = canvas.GetComponent<RectTransform>();

            ConfigureCanvas(canvas);
            ConfigureBackground(canvasTransform);
            ConfigureTitle(canvasTransform);
            ConfigurePlayButton(canvasTransform);
            ConfigureEventSystem(scene);
            GameBuildSceneCatalog.Apply();

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene))
            {
                throw new System.InvalidOperationException($"Could not save {MainScenePath}.");
            }
        }

        public static void BuildAndValidateFromCommandLine()
        {
            BuildMainScene();
            ValidateMainScene();
            Debug.Log("MainScene build and validation completed.");
        }

        [MenuItem("Tools/Main Menu/Validate Main Scene")]
        public static void ValidateMainScene()
        {
            Scene scene = EditorSceneManager.OpenScene(MainScenePath, OpenSceneMode.Single);
            Canvas canvas = FindRequiredComponent<Canvas>(scene, "Canvas");
            CanvasScaler scaler = RequireComponent<CanvasScaler>(canvas.gameObject, "Canvas");
            Require(scaler.uiScaleMode == CanvasScaler.ScaleMode.ScaleWithScreenSize,
                "Canvas must use Scale With Screen Size.");
            Require(scaler.referenceResolution == ReferenceResolution,
                "Canvas reference resolution must be 1920x1080.");
            Require(Mathf.Approximately(scaler.matchWidthOrHeight, 0.5f),
                "Canvas width/height match must be 0.5.");

            Image background = FindRequiredComponent<Image>(scene, "BackgroundImage");
            AspectRatioFitter fitter = RequireComponent<AspectRatioFitter>(background.gameObject, "BackgroundImage");
            Require(fitter.aspectMode == AspectRatioFitter.AspectMode.EnvelopeParent,
                "Background must cover its parent without aspect distortion.");
            Require(!background.raycastTarget, "Background must not intercept Play button input.");

            Image title = FindRequiredComponent<Image>(scene, "TitleImage");
            RectTransform titleTransform = title.rectTransform;
            Require(titleTransform.parent == canvas.transform, "TitleImage must be a direct Canvas child.");
            Require(title.preserveAspect, "TitleImage must preserve the logo aspect ratio.");
            Require(titleTransform.anchorMin == Vector2.up && titleTransform.anchorMax == Vector2.up,
                "TitleImage must stay anchored to the upper-left corner.");
            Require(titleTransform.sizeDelta == TitleSize && titleTransform.anchoredPosition == TitlePosition,
                "TitleImage must use the title-screen layout values.");

            Button playButton = FindRequiredComponent<Button>(scene, "PlayButton");
            Image playBackground = RequireComponent<Image>(playButton.gameObject, "PlayButton");
            TextMeshProUGUI playLabel = FindRequiredComponent<TextMeshProUGUI>(scene, "PlayLabel");
            MainMenuButtonFeedback playFeedback = RequireComponent<MainMenuButtonFeedback>(playButton.gameObject, "PlayButton");
            MainMenuController menuController = RequireComponent<MainMenuController>(canvas.gameObject, "Canvas");
            RectTransform buttonTransform = playButton.GetComponent<RectTransform>();
            Require(buttonTransform.parent == canvas.transform, "PlayButton must be a direct Canvas child.");
            Require(buttonTransform.anchorMin == Vector2.up && buttonTransform.anchorMax == Vector2.up,
                "PlayButton must stay anchored to the upper-left corner.");
            Require(buttonTransform.sizeDelta == PlayButtonSize && buttonTransform.anchoredPosition == PlayButtonPosition,
                "PlayButton must use the title-screen layout values.");
            Require(Mathf.Approximately(playBackground.color.a, 0f) && playBackground.raycastTarget,
                "PlayButton must keep a transparent clickable background graphic.");
            Require(playButton.transition == Selectable.Transition.None,
                "PlayButton must not tint a visible background during interaction.");
            KoreanUiFontSettings fontSettings = KoreanUiFontSettings.Load();
            Require(fontSettings != null &&
                    playLabel.text == PlayButtonText &&
                    playLabel.font == fontSettings.TextMeshProFont &&
                    playLabel.color == Color.white,
                "PlayButton must use the Korean game-start text and Noto Sans KR TMP font.");
            Require(playFeedback.Label == playLabel && Mathf.Approximately(playFeedback.HoverScale, PlayHoverScale),
                "PlayButton must scale its Play label while hovered.");
            Require(playFeedback.PressedColor == PlayPressedColor,
                "PlayButton must use the sampled logo red while pressed.");
            Require(menuController.PlaySceneName == PlaySceneName,
                "PlayButton must target the Tutorial scene.");
            Require(playButton.onClick.GetPersistentEventCount() == 1,
                "PlayButton must have exactly one persistent action.");
            Require(playButton.onClick.GetPersistentTarget(0) == menuController &&
                    playButton.onClick.GetPersistentMethodName(0) == nameof(MainMenuController.Play),
                "PlayButton must invoke MainMenuController.Play.");

            Require(EditorBuildSettings.scenes.Length > 0 &&
                    EditorBuildSettings.scenes[0].path == MainScenePath &&
                    EditorBuildSettings.scenes[0].enabled,
                "MainScene must be the first enabled Build Settings scene.");
            Require(SceneUtility.GetBuildIndexByScenePath("Assets/_Project/Scenes/Tutorial.unity") >= 0,
                "Tutorial must remain in Build Settings for the Play action.");

            ValidateResponsiveLayout();
        }

        private static void ConfigureCanvas(Canvas canvas)
        {
            CanvasScaler scaler = GetOrAddComponent<CanvasScaler>(canvas.gameObject);
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = ReferenceResolution;
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            MainMenuController controller = GetOrAddComponent<MainMenuController>(canvas.gameObject);
            SerializedObject controllerData = new SerializedObject(controller);
            controllerData.FindProperty("playSceneName").stringValue = PlaySceneName;
            controllerData.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ConfigureBackground(RectTransform canvasTransform)
        {
            Image background = FindRequiredComponent<Image>(canvasTransform.gameObject.scene, "BackgroundImage");
            RectTransform backgroundTransform = background.rectTransform;
            backgroundTransform.SetParent(canvasTransform, false);
            backgroundTransform.anchorMin = Vector2.zero;
            backgroundTransform.anchorMax = Vector2.one;
            backgroundTransform.offsetMin = Vector2.zero;
            backgroundTransform.offsetMax = Vector2.zero;
            backgroundTransform.pivot = new Vector2(0.5f, 0.5f);

            AspectRatioFitter fitter = GetOrAddComponent<AspectRatioFitter>(background.gameObject);
            fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            fitter.aspectRatio = background.sprite != null
                ? background.sprite.rect.width / background.sprite.rect.height
                : ReferenceResolution.x / ReferenceResolution.y;
            background.preserveAspect = true;
            background.raycastTarget = false;
            backgroundTransform.SetAsFirstSibling();
        }

        private static void ConfigureTitle(RectTransform canvasTransform)
        {
            Image title = FindRequiredComponent<Image>(canvasTransform.gameObject.scene, "TitleImage");
            RectTransform titleTransform = title.rectTransform;
            titleTransform.SetParent(canvasTransform, false);
            titleTransform.anchorMin = Vector2.up;
            titleTransform.anchorMax = Vector2.up;
            titleTransform.pivot = new Vector2(0f, 1f);
            titleTransform.anchoredPosition = TitlePosition;
            titleTransform.sizeDelta = TitleSize;
            titleTransform.localScale = Vector3.one;
            title.preserveAspect = true;
            title.raycastTarget = false;
            titleTransform.SetAsLastSibling();
        }

        private static void ConfigurePlayButton(RectTransform canvasTransform)
        {
            Transform existing = canvasTransform.Find("PlayButton");
            GameObject buttonObject = existing != null
                ? existing.gameObject
                : new GameObject("PlayButton", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            RectTransform buttonTransform = buttonObject.GetComponent<RectTransform>();
            buttonTransform.SetParent(canvasTransform, false);
            buttonTransform.anchorMin = Vector2.up;
            buttonTransform.anchorMax = Vector2.up;
            buttonTransform.pivot = new Vector2(0f, 1f);
            buttonTransform.anchoredPosition = PlayButtonPosition;
            buttonTransform.sizeDelta = PlayButtonSize;
            buttonTransform.localScale = Vector3.one;

            Image buttonImage = RequireComponent<Image>(buttonObject, "PlayButton");
            buttonImage.sprite = null;
            buttonImage.type = Image.Type.Simple;
            buttonImage.color = new Color(1f, 1f, 1f, 0f);
            buttonImage.raycastTarget = true;

            Button button = RequireComponent<Button>(buttonObject, "PlayButton");
            button.targetGraphic = buttonImage;
            button.transition = Selectable.Transition.None;
            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(0.92f, 0.92f, 0.92f, 1f);
            colors.pressedColor = new Color(0.74f, 0.74f, 0.74f, 1f);
            colors.selectedColor = Color.white;
            colors.disabledColor = new Color(1f, 1f, 1f, 0.5f);
            colors.colorMultiplier = 1f;
            colors.fadeDuration = 0.1f;
            button.colors = colors;
            button.navigation = new Navigation { mode = Navigation.Mode.None };

            TextMeshProUGUI label = GetOrCreatePlayLabel(buttonTransform);
            KoreanUiFontSettings fontSettings = KoreanUiFontSettings.Load();
            Require(fontSettings != null && fontSettings.TextMeshProFont != null,
                "Main menu requires KoreanUiFontSettings with a TMP font asset.");
            label.font = fontSettings.TextMeshProFont;
            label.text = PlayButtonText;
            label.fontStyle = FontStyles.Bold;
            label.fontSize = 30;
            label.alignment = TextAlignmentOptions.Center;
            label.color = Color.white;
            label.raycastTarget = false;
            label.enableWordWrapping = false;
            label.overflowMode = TextOverflowModes.Overflow;

            MainMenuButtonFeedback feedback = GetOrAddComponent<MainMenuButtonFeedback>(buttonObject);
            feedback.Configure(label, PlayHoverScale, PlayPressedColor);

            MainMenuController controller = RequireComponent<MainMenuController>(canvasTransform.gameObject, "Canvas");
            button.onClick = new Button.ButtonClickedEvent();
            UnityEventTools.AddPersistentListener(button.onClick, controller.Play);
            buttonTransform.SetAsLastSibling();
        }

        private static TextMeshProUGUI GetOrCreatePlayLabel(RectTransform buttonTransform)
        {
            Transform existing = buttonTransform.Find("PlayLabel");
            GameObject labelObject = existing != null
                ? existing.gameObject
                : new GameObject("PlayLabel", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            Text legacyLabel = labelObject.GetComponent<Text>();
            if (legacyLabel != null)
            {
                Object.DestroyImmediate(legacyLabel);
            }

            RectTransform labelTransform = labelObject.GetComponent<RectTransform>();
            labelTransform.SetParent(buttonTransform, false);
            labelTransform.anchorMin = Vector2.zero;
            labelTransform.anchorMax = Vector2.one;
            labelTransform.offsetMin = Vector2.zero;
            labelTransform.offsetMax = Vector2.zero;
            labelTransform.pivot = new Vector2(0.5f, 0.5f);
            return GetOrAddComponent<TextMeshProUGUI>(labelObject);
        }

        private static void ConfigureEventSystem(Scene scene)
        {
            EventSystem eventSystem = FindRequiredComponent<EventSystem>(scene, "EventSystem");
            eventSystem.firstSelectedGameObject = null;
        }

        private static void ValidateResponsiveLayout()
        {
            Vector2[] displaySizes =
            {
                new Vector2(1920f, 1080f), // 16:9 baseline
                new Vector2(2560f, 1080f), // 21:9 ultrawide
                new Vector2(1080f, 1920f), // portrait display
                new Vector2(1024f, 768f),  // 4:3 display
                new Vector2(3840f, 2160f), // 4K 16:9
            };

            foreach (Vector2 displaySize in displaySizes)
            {
                float widthScale = displaySize.x / ReferenceResolution.x;
                float heightScale = displaySize.y / ReferenceResolution.y;
                float scale = Mathf.Sqrt(widthScale * heightScale);
                Vector2 canvasSize = displaySize / scale;

                Require(TitlePosition.x >= 0f && -TitlePosition.y + TitleSize.y <= canvasSize.y &&
                        TitlePosition.x + TitleSize.x <= canvasSize.x,
                    $"Title does not fit the {displaySize.x}x{displaySize.y} display layout.");
                Require(PlayButtonPosition.x >= 0f && -PlayButtonPosition.y + PlayButtonSize.y <= canvasSize.y &&
                        PlayButtonPosition.x + PlayButtonSize.x <= canvasSize.x,
                    $"Play button does not fit the {displaySize.x}x{displaySize.y} display layout.");
            }
        }

        private static T FindRequiredComponent<T>(Scene scene, string objectName) where T : Component
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                T[] components = root.GetComponentsInChildren<T>(true);
                foreach (T component in components)
                {
                    if (component.gameObject.name == objectName)
                    {
                        return component;
                    }
                }
            }

            throw new System.InvalidOperationException($"Could not find {typeof(T).Name} on '{objectName}' in {scene.path}.");
        }

        private static T GetOrAddComponent<T>(GameObject gameObject) where T : Component
        {
            T component = gameObject.GetComponent<T>();
            return component != null ? component : gameObject.AddComponent<T>();
        }

        private static T RequireComponent<T>(GameObject gameObject, string objectName) where T : Component
        {
            T component = gameObject.GetComponent<T>();
            if (component == null)
            {
                throw new System.InvalidOperationException($"'{objectName}' is missing {typeof(T).Name}.");
            }

            return component;
        }

        private static void Require(bool condition, string message)
        {
            if (!condition)
            {
                throw new System.InvalidOperationException(message);
            }
        }
    }
}
