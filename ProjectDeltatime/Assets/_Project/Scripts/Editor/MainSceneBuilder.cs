using System;
using System.Collections.Generic;
using Deltatime.UI;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Deltatime.EditorTools
{
    /// <summary>Builds and validates the asset-backed, responsive title menu.</summary>
    public static class MainSceneBuilder
    {
        private const string MainScenePath = "Assets/_Project/Scenes/MainScene.unity";
        private const string BackgroundPath = "Assets/_Project/Image/mainMenuBackground.png";
        private const string LogoPath = "Assets/_Project/Image/titleLogoWide.png";
        private const string PlayScenePath = GameBuildSceneCatalog.TutorialScenePath;
        private static readonly Vector2 ReferenceResolution = new Vector2(1920f, 1080f);
        private static readonly Vector2 TitleSize = new Vector2(720f, 300f);
        private static readonly Vector2 TitlePosition = new Vector2(64f, -34f);
        private static readonly Color Red = new Color(0.86f, 0.045f, 0.075f, 1f);
        private static readonly Color Cyan = new Color(0.17f, 0.84f, 0.91f, 1f);
        private static readonly Color Panel = new Color(0.025f, 0.03f, 0.04f, 0.96f);
        private static readonly Color PanelSoft = new Color(0.075f, 0.08f, 0.09f, 0.96f);

        private static TMP_FontAsset Font
        {
            get
            {
                KoreanUiFontSettings settings = KoreanUiFontSettings.Load();
                if (settings == null || settings.TextMeshProFont == null)
                {
                    throw new InvalidOperationException("Main menu requires KoreanUiFontSettings.");
                }

                return settings.TextMeshProFont;
            }
        }

        [MenuItem("Tools/Main Menu/Build Main Scene")]
        public static void BuildMainScene()
        {
            ConfigureSpriteImporter(BackgroundPath, false);
            ConfigureSpriteImporter(LogoPath, true);

            Sprite backgroundSprite = AssetDatabase.LoadAssetAtPath<Sprite>(BackgroundPath);
            Sprite logoSprite = AssetDatabase.LoadAssetAtPath<Sprite>(LogoPath);
            Require(backgroundSprite != null, $"Missing background Sprite: {BackgroundPath}");
            Require(logoSprite != null, $"Missing title Sprite: {LogoPath}");

            Scene scene = EditorSceneManager.OpenScene(MainScenePath, OpenSceneMode.Single);
            Canvas canvas = FindRequiredComponent<Canvas>(scene, "Canvas");
            RectTransform canvasTransform = canvas.GetComponent<RectTransform>();
            ConfigureCanvas(canvas);
            ConfigureArtwork(canvasTransform, backgroundSprite, logoSprite);

            DestroyDirectChild(canvasTransform, "PlayButton");
            DestroyDirectChild(canvasTransform, "TutorialKeyHint");
            DestroyDirectChild(canvasTransform, "MenuRoot");
            DestroyDirectChild(canvasTransform, "OptionPanel");
            DestroyDirectChild(canvasTransform, "CreditsPanel");

            MainMenuController controller = GetOrAddComponent<MainMenuController>(canvas.gameObject);
            CanvasGroup menuGroup;
            Button[] mainButtons;
            TextMeshProUGUI shortcut;
            BuildMainMenu(canvasTransform, controller, out menuGroup, out mainButtons, out shortcut);

            GameObject optionPanel;
            MainMenuOptionsController optionsController;
            BuildOptions(canvasTransform, controller, out optionPanel, out optionsController);
            GameObject creditsPanel = BuildCredits(canvasTransform, controller);

            controller.Configure(
                menuGroup,
                optionPanel,
                creditsPanel,
                mainButtons[0],
                mainButtons[1],
                mainButtons[2],
                shortcut,
                optionsController);
            EditorUtility.SetDirty(controller);

            ConfigureMainButtonEvents(controller, mainButtons);
            ConfigureNavigation(mainButtons);
            ConfigureEventSystem(scene, mainButtons[0].gameObject);
            optionPanel.SetActive(false);
            creditsPanel.SetActive(false);
            GameBuildSceneCatalog.Apply();

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene))
            {
                throw new InvalidOperationException($"Could not save {MainScenePath}.");
            }
            AssetDatabase.SaveAssets();
        }

        public static void BuildAndValidateFromCommandLine()
        {
            SceneBuildCommand.Run(() =>
            {
                BuildMainScene();
                ValidateMainScene();
                Debug.Log("MainScene build and validation completed.");
            });
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

            Image background = FindRequiredComponent<Image>(scene, "BackgroundImage");
            Image title = FindRequiredComponent<Image>(scene, "TitleImage");
            Require(AssetDatabase.GetAssetPath(background.sprite) == BackgroundPath,
                "BackgroundImage must use mainMenuBackground.png.");
            Require(AssetDatabase.GetAssetPath(title.sprite) == LogoPath,
                "TitleImage must use titleLogoWide.png.");
            Require(title.preserveAspect && title.rectTransform.sizeDelta == TitleSize &&
                    title.rectTransform.anchoredPosition == TitlePosition,
                "TitleImage must use the wide upper-left layout.");
            Require(AssetDatabase.GetAssetPath(title.sprite) != "Assets/_Project/Image/titleLogo.png",
                "Legacy titleLogo.png must not be referenced by MainScene.");

            MainMenuController controller = RequireComponent<MainMenuController>(canvas.gameObject, "Canvas");
            Require(controller.PlaySceneName == PlayScenePath,
                $"Main menu play target must be '{PlayScenePath}'.");
            string[] names = { "StartButton", "OptionButton", "CreditsButton", "ExitButton" };
            string[] labels = { "START", "OPTION", "CREDITS", "EXIT" };
            string[] methods =
            {
                nameof(MainMenuController.Play), nameof(MainMenuController.OpenOptions),
                nameof(MainMenuController.OpenCredits), nameof(MainMenuController.ExitGame)
            };
            for (int i = 0; i < names.Length; i++)
            {
                Button button = FindRequiredComponent<Button>(scene, names[i]);
                TextMeshProUGUI label = button.transform.Find("Label").GetComponent<TextMeshProUGUI>();
                Require(label.text == labels[i], $"{names[i]} must keep its English label.");
                Require(button.onClick.GetPersistentEventCount() == 1 &&
                        button.onClick.GetPersistentTarget(0) == controller &&
                        button.onClick.GetPersistentMethodName(0) == methods[i],
                    $"{names[i]} callback is not configured.");
                Require(button.GetComponent<MainMenuButtonFeedback>() != null,
                    $"{names[i]} requires selection feedback.");
            }

            Require(FindRequiredComponent<TextMeshProUGUI>(scene, "Tagline").text ==
                    "STOP TIME. TAKE CONTROL.",
                "Main menu tagline is missing.");
            Require(FindRequiredComponent<TextMeshProUGUI>(scene, "StartShortcut").text.StartsWith("PRESS "),
                "Dynamic start shortcut label is missing.");

            GameObject optionPanel = FindRequiredObject(scene, "OptionPanel");
            GameObject creditsPanel = FindRequiredObject(scene, "CreditsPanel");
            Require(optionPanel.GetComponentInChildren<MainMenuOptionsController>(true) != null,
                "OptionPanel requires MainMenuOptionsController.");
            Require(FindRequiredObject(scene, "GraphicsPage") != null &&
                    FindRequiredObject(scene, "KeysPage") != null &&
                    FindRequiredObject(scene, "AudioPage") != null,
                "OptionPanel requires GRAPHICS, KEYS and AUDIO pages.");
            Require(creditsPanel.transform.Find("Dialog/CreditsBody") != null,
                "CreditsPanel requires verified English credits copy.");

            ValidateImporter(BackgroundPath, false);
            ValidateImporter(LogoPath, true);
            Require(EditorBuildSettings.scenes.Length > 0 &&
                    EditorBuildSettings.scenes[0].enabled &&
                    EditorBuildSettings.scenes[0].path == MainScenePath,
                "MainScene must remain the first enabled Build Settings scene.");
            Require(SceneUtility.GetBuildIndexByScenePath(PlayScenePath) >= 0,
                "The official reworked Tutorial must remain in Build Settings.");
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
            SerializedObject data = new SerializedObject(controller);
            data.FindProperty("playSceneName").stringValue = PlayScenePath;
            data.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ConfigureArtwork(
            RectTransform canvasTransform,
            Sprite backgroundSprite,
            Sprite logoSprite)
        {
            Scene scene = canvasTransform.gameObject.scene;
            Image background = FindRequiredComponent<Image>(scene, "BackgroundImage");
            RectTransform backgroundRect = background.rectTransform;
            backgroundRect.SetParent(canvasTransform, false);
            Stretch(backgroundRect);
            background.sprite = backgroundSprite;
            background.color = Color.white;
            background.preserveAspect = true;
            background.raycastTarget = false;
            AspectRatioFitter fitter = GetOrAddComponent<AspectRatioFitter>(background.gameObject);
            fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            fitter.aspectRatio = backgroundSprite.rect.width / backgroundSprite.rect.height;
            backgroundRect.SetAsFirstSibling();

            Image title = FindRequiredComponent<Image>(scene, "TitleImage");
            SetTopLeft(title.rectTransform, TitlePosition.x, -TitlePosition.y, TitleSize.x, TitleSize.y);
            title.rectTransform.SetParent(canvasTransform, false);
            title.sprite = logoSprite;
            title.color = Color.white;
            title.preserveAspect = true;
            title.raycastTarget = false;
            title.rectTransform.SetAsLastSibling();
        }

        private static void BuildMainMenu(
            RectTransform canvas,
            MainMenuController controller,
            out CanvasGroup menuGroup,
            out Button[] buttons,
            out TextMeshProUGUI shortcut)
        {
            GameObject root = CreateUiObject("MenuRoot", canvas, typeof(CanvasGroup));
            RectTransform rootRect = root.GetComponent<RectTransform>();
            SetTopLeft(rootRect, 72f, 328f, 650f, 620f);
            menuGroup = root.GetComponent<CanvasGroup>();

            TextMeshProUGUI tagline = CreateText(rootRect, "Tagline", "STOP TIME. TAKE CONTROL.",
                21f, FontStyles.Normal, new Color(0.82f, 0.82f, 0.84f), TextAlignmentOptions.Left);
            SetTopLeft(tagline.rectTransform, 0f, 0f, 600f, 42f);
            tagline.characterSpacing = 6f;

            string[] names = { "StartButton", "OptionButton", "CreditsButton", "ExitButton" };
            string[] labels = { "START", "OPTION", "CREDITS", "EXIT" };
            buttons = new Button[names.Length];
            for (int i = 0; i < buttons.Length; i++)
            {
                buttons[i] = CreateMainButton(rootRect, names[i], labels[i], 74f + i * 82f);
            }

            shortcut = CreateText(rootRect, "StartShortcut", "PRESS N TO START", 17f,
                FontStyles.Normal, new Color(0.72f, 0.72f, 0.74f), TextAlignmentOptions.Left);
            SetTopLeft(shortcut.rectTransform, 42f, 426f, 520f, 34f);
            shortcut.characterSpacing = 4f;
        }

        private static Button CreateMainButton(RectTransform parent, string name, string labelText, float y)
        {
            GameObject buttonObject = CreateUiObject(name, parent, typeof(Image), typeof(Button),
                typeof(MainMenuButtonFeedback));
            RectTransform rect = buttonObject.GetComponent<RectTransform>();
            SetTopLeft(rect, 0f, y, 560f, 66f);
            Image target = buttonObject.GetComponent<Image>();
            target.color = new Color(1f, 1f, 1f, 0.002f);

            GameObject highlightObject = CreateUiObject("Highlight", rect, typeof(Image), typeof(Outline));
            Image highlight = highlightObject.GetComponent<Image>();
            highlight.color = new Color(Red.r, Red.g, Red.b, 0.21f);
            highlight.raycastTarget = false;
            Outline outline = highlightObject.GetComponent<Outline>();
            outline.effectColor = new Color(Red.r, Red.g, Red.b, 0.8f);
            outline.effectDistance = new Vector2(1f, -1f);
            Stretch(highlight.rectTransform);

            TextMeshProUGUI pointer = CreateText(rect, "Pointer", "▶", 24f, FontStyles.Bold,
                Red, TextAlignmentOptions.Center);
            SetTopLeft(pointer.rectTransform, 4f, 7f, 34f, 52f);
            TextMeshProUGUI label = CreateText(rect, "Label", labelText, 30f, FontStyles.Bold,
                Color.white, TextAlignmentOptions.Left);
            SetTopLeft(label.rectTransform, 44f, 6f, 480f, 54f);
            label.characterSpacing = 8f;

            Button button = buttonObject.GetComponent<Button>();
            button.targetGraphic = target;
            button.transition = Selectable.Transition.None;
            button.GetComponent<MainMenuButtonFeedback>().Configure(label, pointer, highlight);
            return button;
        }

        private static void ConfigureMainButtonEvents(MainMenuController controller, Button[] buttons)
        {
            for (int i = 0; i < buttons.Length; i++)
            {
                buttons[i].onClick = new Button.ButtonClickedEvent();
            }
            UnityEventTools.AddPersistentListener(buttons[0].onClick, controller.Play);
            UnityEventTools.AddPersistentListener(buttons[1].onClick, controller.OpenOptions);
            UnityEventTools.AddPersistentListener(buttons[2].onClick, controller.OpenCredits);
            UnityEventTools.AddPersistentListener(buttons[3].onClick, controller.ExitGame);
        }

        private static void ConfigureNavigation(Button[] buttons)
        {
            for (int i = 0; i < buttons.Length; i++)
            {
                buttons[i].navigation = new Navigation
                {
                    mode = Navigation.Mode.Explicit,
                    selectOnUp = buttons[(i - 1 + buttons.Length) % buttons.Length],
                    selectOnDown = buttons[(i + 1) % buttons.Length]
                };
            }
        }

        private static void BuildOptions(
            RectTransform canvas,
            MainMenuController owner,
            out GameObject overlay,
            out MainMenuOptionsController options)
        {
            overlay = CreateUiObject("OptionPanel", canvas, typeof(Image));
            Stretch(overlay.GetComponent<RectTransform>());
            overlay.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.82f);

            GameObject dialog = CreateUiObject("Dialog", overlay.transform, typeof(Image),
                typeof(Outline), typeof(MainMenuOptionsController));
            SetCentered(dialog.GetComponent<RectTransform>(), 1240f, 800f);
            dialog.GetComponent<Image>().color = Panel;
            Outline outline = dialog.GetComponent<Outline>();
            outline.effectColor = new Color(Red.r, Red.g, Red.b, 0.72f);
            outline.effectDistance = new Vector2(2f, -2f);
            options = dialog.GetComponent<MainMenuOptionsController>();

            TextMeshProUGUI title = CreateText(dialog.transform, "OptionTitle", "OPTION", 36f,
                FontStyles.Bold, Color.white, TextAlignmentOptions.Left);
            SetTopLeft(title.rectTransform, 42f, 24f, 330f, 60f);
            title.characterSpacing = 8f;

            Button graphicsTab = CreateUtilityButton(dialog.transform, "GraphicsTab", "GRAPHICS",
                46f, 94f, 220f, 48f, options.ShowGraphics);
            CreateUtilityButton(dialog.transform, "KeysTab", "KEYS", 276f, 94f, 220f, 48f,
                options.ShowKeys);
            CreateUtilityButton(dialog.transform, "AudioTab", "AUDIO", 506f, 94f, 220f, 48f,
                options.ShowAudio);

            GameObject graphicsPage = CreatePage(dialog.transform, "GraphicsPage");
            GameObject keysPage = CreatePage(dialog.transform, "KeysPage");
            GameObject audioPage = CreatePage(dialog.transform, "AudioPage");

            TextMeshProUGUI resolutionValue;
            CreateCycleRow(graphicsPage.transform, "Resolution", "RESOLUTION", 30f,
                options.PreviousResolution, options.NextResolution, out resolutionValue);
            TextMeshProUGUI fullscreenValue;
            CreateToggleRow(graphicsPage.transform, "Fullscreen", "DISPLAY MODE", 116f,
                options.ToggleFullscreen, out fullscreenValue);
            TextMeshProUGUI qualityValue;
            CreateCycleRow(graphicsPage.transform, "Quality", "QUALITY", 202f,
                options.PreviousQuality, options.NextQuality, out qualityValue);
            TextMeshProUGUI vSyncValue;
            CreateToggleRow(graphicsPage.transform, "VSync", "VSYNC", 288f,
                options.ToggleVSync, out vSyncValue);

            MainMenuOptionsController.RebindEntry[] entries = BuildKeyRows(keysPage.transform, options);

            Slider masterSlider;
            TextMeshProUGUI masterValue;
            CreateAudioRow(audioPage.transform, "Master", "MASTER", 54f, out masterSlider, out masterValue);
            Slider bgmSlider;
            TextMeshProUGUI bgmValue;
            CreateAudioRow(audioPage.transform, "Bgm", "BGM", 156f, out bgmSlider, out bgmValue);
            Slider sfxSlider;
            TextMeshProUGUI sfxValue;
            CreateAudioRow(audioPage.transform, "Sfx", "SFX", 258f, out sfxSlider, out sfxValue);

            TextMeshProUGUI status = CreateText(dialog.transform, "Status", string.Empty, 15f,
                FontStyles.Normal, Cyan, TextAlignmentOptions.Left);
            SetTopLeft(status.rectTransform, 48f, 706f, 520f, 40f);
            CreateUtilityButton(dialog.transform, "ResetDefaults", "RESET DEFAULTS", 48f, 746f,
                250f, 42f, options.ResetDefaults);
            CreateUtilityButton(dialog.transform, "Cancel", "CANCEL", 824f, 746f,
                160f, 42f, options.Cancel);
            CreateUtilityButton(dialog.transform, "Apply", "APPLY", 1000f, 746f,
                184f, 42f, options.Apply, Red);

            options.Configure(owner, graphicsPage, keysPage, audioPage, resolutionValue,
                fullscreenValue, qualityValue, vSyncValue, masterSlider, bgmSlider, sfxSlider,
                masterValue, bgmValue, sfxValue, status, entries, graphicsTab);
            EditorUtility.SetDirty(options);
        }

        private static GameObject CreatePage(Transform dialog, string name)
        {
            GameObject page = CreateUiObject(name, dialog);
            SetTopLeft(page.GetComponent<RectTransform>(), 48f, 156f, 1144f, 530f);
            return page;
        }

        private static void CreateCycleRow(
            Transform parent, string name, string labelText, float y,
            UnityAction previous, UnityAction next, out TextMeshProUGUI value)
        {
            TextMeshProUGUI label = CreateText(parent, name + "Label", labelText, 22f,
                FontStyles.Bold, new Color(0.72f, 0.72f, 0.74f), TextAlignmentOptions.Left);
            SetTopLeft(label.rectTransform, 36f, y, 280f, 54f);
            CreateUtilityButton(parent, name + "Previous", "◀", 350f, y, 56f, 54f, previous);
            value = CreateText(parent, name + "Value", string.Empty, 21f, FontStyles.Bold,
                Color.white, TextAlignmentOptions.Center);
            SetTopLeft(value.rectTransform, 420f, y, 470f, 54f);
            CreateUtilityButton(parent, name + "Next", "▶", 904f, y, 56f, 54f, next);
        }

        private static void CreateToggleRow(
            Transform parent, string name, string labelText, float y,
            UnityAction toggle, out TextMeshProUGUI value)
        {
            TextMeshProUGUI label = CreateText(parent, name + "Label", labelText, 22f,
                FontStyles.Bold, new Color(0.72f, 0.72f, 0.74f), TextAlignmentOptions.Left);
            SetTopLeft(label.rectTransform, 36f, y, 280f, 54f);
            Button button = CreateUtilityButton(parent, name + "Toggle", string.Empty,
                350f, y, 610f, 54f, toggle);
            value = button.GetComponentInChildren<TextMeshProUGUI>();
            value.gameObject.name = name + "Value";
        }

        private static MainMenuOptionsController.RebindEntry[] BuildKeyRows(
            Transform parent, MainMenuOptionsController options)
        {
            string[] labels =
            {
                "MOVE UP", "MOVE DOWN", "MOVE LEFT", "MOVE RIGHT", "FIRE", "THROW",
                "DASH", "DEADLINE", "INTERACT", "RESTART", "NEXT STAGE"
            };
            string[] actions =
            {
                "Move", "Move", "Move", "Move", "Fire", "Throw", "Dash", "Deadline",
                "Interact", "Restart", "NextStage"
            };
            string[] bindingNames =
            {
                "up", "down", "left", "right", null, null, null, null, null, null, null
            };
            MainMenuOptionsController.RebindEntry[] entries =
                new MainMenuOptionsController.RebindEntry[labels.Length];
            for (int i = 0; i < labels.Length; i++)
            {
                int column = i / 6;
                int row = i % 6;
                float x = 12f + column * 560f;
                float y = 4f + row * 79f;
                TextMeshProUGUI label = CreateText(parent, actions[i] + (bindingNames[i] ?? string.Empty) + "Label",
                    labels[i], 18f, FontStyles.Bold, new Color(0.72f, 0.72f, 0.74f),
                    TextAlignmentOptions.Left);
                SetTopLeft(label.rectTransform, x, y, 260f, 48f);
                Button button = CreateUtilityButton(parent, actions[i] + (bindingNames[i] ?? string.Empty) + "Binding",
                    "?", x + 270f, y, 250f, 48f, null);
                UnityEventTools.AddIntPersistentListener(button.onClick, options.BeginRebind, i);
                entries[i] = new MainMenuOptionsController.RebindEntry
                {
                    ActionName = actions[i],
                    BindingName = bindingNames[i],
                    AllowMouseButton = actions[i] == "Fire" || actions[i] == "Throw",
                    Button = button,
                    ValueLabel = button.GetComponentInChildren<TextMeshProUGUI>()
                };
            }
            return entries;
        }

        private static void CreateAudioRow(
            Transform parent, string name, string labelText, float y,
            out Slider slider, out TextMeshProUGUI value)
        {
            TextMeshProUGUI label = CreateText(parent, name + "Label", labelText, 22f,
                FontStyles.Bold, new Color(0.72f, 0.72f, 0.74f), TextAlignmentOptions.Left);
            SetTopLeft(label.rectTransform, 36f, y, 220f, 54f);
            GameObject sliderObject = CreateUiObject(name + "Slider", parent, typeof(Slider));
            SetTopLeft(sliderObject.GetComponent<RectTransform>(), 280f, y + 8f, 650f, 38f);
            slider = sliderObject.GetComponent<Slider>();
            slider.minValue = 0f;
            slider.maxValue = 1f;

            GameObject background = CreateUiObject("Background", sliderObject.transform, typeof(Image));
            Stretch(background.GetComponent<RectTransform>());
            background.GetComponent<Image>().color = new Color(0.18f, 0.19f, 0.21f, 1f);
            GameObject fillArea = CreateUiObject("Fill Area", sliderObject.transform);
            Stretch(fillArea.GetComponent<RectTransform>(), 5f);
            GameObject fill = CreateUiObject("Fill", fillArea.transform, typeof(Image));
            Stretch(fill.GetComponent<RectTransform>());
            fill.GetComponent<Image>().color = Red;
            GameObject handleArea = CreateUiObject("Handle Slide Area", sliderObject.transform);
            Stretch(handleArea.GetComponent<RectTransform>(), 8f);
            GameObject handle = CreateUiObject("Handle", handleArea.transform, typeof(Image));
            RectTransform handleRect = handle.GetComponent<RectTransform>();
            handleRect.sizeDelta = new Vector2(24f, 42f);
            handle.GetComponent<Image>().color = Color.white;
            slider.fillRect = fill.GetComponent<RectTransform>();
            slider.handleRect = handleRect;
            slider.targetGraphic = handle.GetComponent<Image>();

            value = CreateText(parent, name + "Value", "100%", 20f, FontStyles.Bold,
                Color.white, TextAlignmentOptions.Right);
            SetTopLeft(value.rectTransform, 950f, y, 130f, 54f);
        }

        private static GameObject BuildCredits(RectTransform canvas, MainMenuController owner)
        {
            GameObject overlay = CreateUiObject("CreditsPanel", canvas, typeof(Image));
            Stretch(overlay.GetComponent<RectTransform>());
            overlay.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.82f);
            GameObject dialog = CreateUiObject("Dialog", overlay.transform, typeof(Image), typeof(Outline));
            SetCentered(dialog.GetComponent<RectTransform>(), 1040f, 720f);
            dialog.GetComponent<Image>().color = Panel;
            dialog.GetComponent<Outline>().effectColor = new Color(Red.r, Red.g, Red.b, 0.72f);

            TextMeshProUGUI title = CreateText(dialog.transform, "CreditsTitle", "CREDITS", 38f,
                FontStyles.Bold, Color.white, TextAlignmentOptions.Center);
            SetTopLeft(title.rectTransform, 40f, 30f, 960f, 64f);
            title.characterSpacing = 9f;
            string bodyText =
                $"DELTA TIME\n\nUNITY {Application.unityVersion}\n\n" +
                "ART\nSYNTY STUDIOS - POLYGON GENERIC / POLYGON NIGHTCLUBS\n\n" +
                "FONT\nNOTO SANS KR - SIL OPEN FONT LICENSE 1.1\n\n" +
                "AUDIO\nOPENGAMEART - SAVAGE AMBUSH / FREE FIREARM SOUND LIBRARY / SWISHES SOUND PACK\n" +
                "KENNEY - IMPACT SOUNDS / RPG AUDIO (CC0)\n" +
                "PIXABAY - BLACK_KUMIZHI / CHRYSALYN / DRAGON-STUDIO\n\n" +
                "THANK YOU FOR PLAYING";
            TextMeshProUGUI body = CreateText(dialog.transform, "CreditsBody", bodyText, 20f,
                FontStyles.Normal, new Color(0.84f, 0.84f, 0.86f), TextAlignmentOptions.Center);
            SetTopLeft(body.rectTransform, 54f, 108f, 932f, 520f);
            body.enableWordWrapping = true;
            body.lineSpacing = 10f;
            CreateUtilityButton(dialog.transform, "CloseCredits", "CLOSE", 420f, 648f,
                200f, 48f, owner.CloseCredits, Red);
            return overlay;
        }

        private static Button CreateUtilityButton(
            Transform parent, string name, string text, float x, float y, float width, float height,
            UnityAction callback, Color? fill = null)
        {
            GameObject buttonObject = CreateUiObject(name, parent, typeof(Image), typeof(Button));
            SetTopLeft(buttonObject.GetComponent<RectTransform>(), x, y, width, height);
            Image image = buttonObject.GetComponent<Image>();
            image.color = fill ?? PanelSoft;
            Button button = buttonObject.GetComponent<Button>();
            button.targetGraphic = image;
            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1f, 0.42f, 0.42f, 1f);
            colors.selectedColor = colors.highlightedColor;
            colors.pressedColor = new Color(0.75f, 0.18f, 0.18f, 1f);
            colors.colorMultiplier = 1f;
            button.colors = colors;
            TextMeshProUGUI label = CreateText(buttonObject.transform, "Label", text, 17f,
                FontStyles.Bold, Color.white, TextAlignmentOptions.Center);
            Stretch(label.rectTransform, 4f);
            if (callback != null)
            {
                UnityEventTools.AddPersistentListener(button.onClick, callback);
            }
            return button;
        }

        private static TextMeshProUGUI CreateText(
            Transform parent, string name, string text, float size, FontStyles style,
            Color color, TextAlignmentOptions alignment)
        {
            GameObject textObject = CreateUiObject(name, parent, typeof(TextMeshProUGUI));
            TextMeshProUGUI label = textObject.GetComponent<TextMeshProUGUI>();
            label.font = Font;
            label.text = text;
            label.fontSize = size;
            label.fontStyle = style;
            label.color = color;
            label.alignment = alignment;
            label.raycastTarget = false;
            label.enableWordWrapping = false;
            label.overflowMode = TextOverflowModes.Ellipsis;
            return label;
        }

        private static GameObject CreateUiObject(string name, Transform parent, params Type[] components)
        {
            List<Type> types = new List<Type> { typeof(RectTransform), typeof(CanvasRenderer) };
            for (int i = 0; i < components.Length; i++)
            {
                if (!types.Contains(components[i])) types.Add(components[i]);
            }
            GameObject result = new GameObject(name, types.ToArray());
            result.transform.SetParent(parent, false);
            return result;
        }

        private static void ConfigureEventSystem(Scene scene, GameObject first)
        {
            EventSystem eventSystem = FindRequiredComponent<EventSystem>(scene, "EventSystem");
            eventSystem.firstSelectedGameObject = first;
            EditorUtility.SetDirty(eventSystem);
        }

        private static void ConfigureSpriteImporter(string path, bool alpha)
        {
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            Require(importer != null, $"Missing texture importer: {path}");
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.sRGBTexture = true;
            importer.alphaIsTransparency = alpha;
            importer.mipmapEnabled = false;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Bilinear;
            importer.maxTextureSize = 2048;
            importer.SaveAndReimport();
        }

        private static void ValidateImporter(string path, bool alpha)
        {
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            Require(importer != null && importer.textureType == TextureImporterType.Sprite &&
                    importer.sRGBTexture && importer.alphaIsTransparency == alpha &&
                    !importer.mipmapEnabled && importer.wrapMode == TextureWrapMode.Clamp &&
                    importer.filterMode == FilterMode.Bilinear && importer.maxTextureSize == 2048,
                $"Sprite importer settings are invalid for {path}.");
        }

        private static void ValidateResponsiveLayout()
        {
            Vector2[] displays =
            {
                new Vector2(1920f, 1080f), new Vector2(1280f, 720f),
                new Vector2(2560f, 1080f), new Vector2(1024f, 768f)
            };
            foreach (Vector2 display in displays)
            {
                float scale = Mathf.Sqrt(
                    display.x / ReferenceResolution.x * display.y / ReferenceResolution.y);
                Vector2 canvasSize = display / scale;
                Require(TitlePosition.x + TitleSize.x <= canvasSize.x &&
                        -TitlePosition.y + TitleSize.y <= canvasSize.y,
                    $"Wide logo does not fit {display.x}x{display.y}.");
                Require(canvasSize.x >= 1240f && canvasSize.y >= 800f,
                    $"Option dialog does not fit {display.x}x{display.y}.");
            }
        }

        private static void SetTopLeft(RectTransform rect, float x, float y, float width, float height)
        {
            rect.anchorMin = Vector2.up;
            rect.anchorMax = Vector2.up;
            rect.pivot = Vector2.up;
            rect.anchoredPosition = new Vector2(x, -y);
            rect.sizeDelta = new Vector2(width, height);
            rect.localScale = Vector3.one;
        }

        private static void SetCentered(RectTransform rect, float width, float height)
        {
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(width, height);
        }

        private static void Stretch(RectTransform rect, float inset = 0f)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(inset, inset);
            rect.offsetMax = new Vector2(-inset, -inset);
        }

        private static void DestroyDirectChild(RectTransform parent, string name)
        {
            Transform child = parent.Find(name);
            if (child != null) UnityEngine.Object.DestroyImmediate(child.gameObject);
        }

        private static GameObject FindRequiredObject(Scene scene, string name)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                Transform[] transforms = root.GetComponentsInChildren<Transform>(true);
                for (int i = 0; i < transforms.Length; i++)
                {
                    if (transforms[i].name == name) return transforms[i].gameObject;
                }
            }
            throw new InvalidOperationException($"Could not find '{name}' in {scene.path}.");
        }

        private static T FindRequiredComponent<T>(Scene scene, string objectName) where T : Component
        {
            GameObject target = FindRequiredObject(scene, objectName);
            T component = target.GetComponent<T>();
            if (component == null)
            {
                throw new InvalidOperationException($"'{objectName}' is missing {typeof(T).Name}.");
            }
            return component;
        }

        private static T GetOrAddComponent<T>(GameObject target) where T : Component
        {
            T component = target.GetComponent<T>();
            return component != null ? component : target.AddComponent<T>();
        }

        private static T RequireComponent<T>(GameObject target, string name) where T : Component
        {
            T component = target.GetComponent<T>();
            if (component == null) throw new InvalidOperationException($"'{name}' is missing {typeof(T).Name}.");
            return component;
        }

        private static void Require(bool condition, string message)
        {
            SceneValidation.Require(condition, message);
        }
    }
}
