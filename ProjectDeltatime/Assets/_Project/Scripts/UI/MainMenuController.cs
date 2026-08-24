using Deltatime.Audio;
using Deltatime.InputSystem;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Deltatime.UI
{
    /// <summary>Coordinates title actions, modals and the saved start shortcut.</summary>
    public sealed class MainMenuController : MonoBehaviour
    {
        [SerializeField] private string playSceneName = "Tutorial";
        [SerializeField] private CanvasGroup menuGroup;
        [SerializeField] private GameObject optionPanel;
        [SerializeField] private GameObject creditsPanel;
        [SerializeField] private Button startButton;
        [SerializeField] private Button optionButton;
        [SerializeField] private Button creditsButton;
        [SerializeField] private TextMeshProUGUI shortcutLabel;
        [SerializeField] private MainMenuOptionsController optionsController;

        private PlayerControls controls;
        private bool isLoadingPlayScene;

        public string PlaySceneName => playSceneName;
        public bool IsModalOpen =>
            optionPanel != null && optionPanel.activeSelf ||
            creditsPanel != null && creditsPanel.activeSelf;

        public void Configure(
            CanvasGroup targetMenuGroup,
            GameObject targetOptionPanel,
            GameObject targetCreditsPanel,
            Button targetStartButton,
            Button targetOptionButton,
            Button targetCreditsButton,
            TextMeshProUGUI targetShortcutLabel,
            MainMenuOptionsController targetOptionsController)
        {
            menuGroup = targetMenuGroup;
            optionPanel = targetOptionPanel;
            creditsPanel = targetCreditsPanel;
            startButton = targetStartButton;
            optionButton = targetOptionButton;
            creditsButton = targetCreditsButton;
            shortcutLabel = targetShortcutLabel;
            optionsController = targetOptionsController;
        }

        private void Awake()
        {
            ReloadControls();
            SetModalState(false);
            RefreshShortcutLabel();
        }

        private void Start() => Select(startButton);

        private void OnEnable() => controls?.Gameplay.Enable();

        private void Update()
        {
            if (!isLoadingPlayScene && !IsModalOpen && controls != null &&
                controls.Gameplay.NextStage.WasPressedThisFrame())
            {
                Play();
            }

            if (creditsPanel != null && creditsPanel.activeSelf &&
                UnityEngine.InputSystem.Keyboard.current?.escapeKey.wasPressedThisFrame == true)
            {
                CloseCredits();
            }
        }

        private void OnDisable() => controls?.Gameplay.Disable();

        private void OnDestroy()
        {
            controls?.Dispose();
            controls = null;
        }

        public void ReloadControls()
        {
            bool wasEnabled = controls != null && controls.Gameplay.enabled;
            controls?.Dispose();
            controls = PlayerControlsFactory.Create();
            if (wasEnabled || isActiveAndEnabled)
            {
                controls.Gameplay.Enable();
            }

            RefreshShortcutLabel();
        }

        public void Play()
        {
            if (isLoadingPlayScene || IsModalOpen)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(playSceneName) ||
                !Application.CanStreamedLevelBeLoaded(playSceneName))
            {
                Debug.LogError(
                    $"Main menu could not load '{playSceneName}'. Add it to Build Settings.",
                    this);
                return;
            }

            isLoadingPlayScene = true;
            SoundManager.Instance?.PlayUiClick();
            SceneManager.LoadScene(playSceneName);
        }

        public void OpenOptions()
        {
            if (IsModalOpen)
            {
                return;
            }

            SoundManager.Instance?.PlayUiClick();
            SetModalState(true);
            optionPanel.SetActive(true);
            optionsController?.Open();
        }

        public void CloseOptions(bool applied = false)
        {
            optionPanel?.SetActive(false);
            SetModalState(false);
            if (applied)
            {
                ReloadControls();
            }

            Select(optionButton);
        }

        public void OpenCredits()
        {
            if (IsModalOpen)
            {
                return;
            }

            SoundManager.Instance?.PlayUiClick();
            SetModalState(true);
            creditsPanel.SetActive(true);
        }

        public void CloseCredits()
        {
            creditsPanel?.SetActive(false);
            SetModalState(false);
            Select(creditsButton);
        }

        public void ExitGame()
        {
            SoundManager.Instance?.PlayUiClick();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private void SetModalState(bool open)
        {
            if (!open)
            {
                optionPanel?.SetActive(false);
                creditsPanel?.SetActive(false);
            }

            if (menuGroup != null)
            {
                menuGroup.interactable = !open;
                menuGroup.blocksRaycasts = !open;
            }
        }

        private void RefreshShortcutLabel()
        {
            if (shortcutLabel != null)
            {
                shortcutLabel.text = $"PRESS {InputBindingDisplay.Get("NextStage")} TO START";
            }
        }

        private static void Select(Selectable selectable)
        {
            if (selectable != null && EventSystem.current != null)
            {
                EventSystem.current.SetSelectedGameObject(selectable.gameObject);
            }
        }
    }
}
