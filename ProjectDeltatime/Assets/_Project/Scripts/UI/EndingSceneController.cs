using Deltatime.InputSystem;
using Deltatime.Level;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Deltatime.UI
{
    /// <summary>
    /// Handles the keyboard-only return from the completion scene to the title.
    /// </summary>
    public sealed class EndingSceneController : MonoBehaviour
    {
        private PlayerControls controls;
        private bool isLoadingMainScene;
        [SerializeField] private TextMeshProUGUI returnInstruction;

        public void Configure(TextMeshProUGUI instruction)
        {
            returnInstruction = instruction;
        }

        private void Awake()
        {
            controls = PlayerControlsFactory.Create();
            if (returnInstruction != null)
            {
                returnInstruction.text =
                    $"Press {InputBindingDisplay.Get("NextStage")} to return to Main Menu";
            }
        }

        private void OnEnable()
        {
            controls?.Gameplay.Enable();
        }

        private void Update()
        {
            if (!isLoadingMainScene &&
                controls != null &&
                controls.Gameplay.NextStage.WasPressedThisFrame())
            {
                ReturnToMainMenu();
            }
        }

        private void OnDisable()
        {
            controls?.Gameplay.Disable();
        }

        private void OnDestroy()
        {
            controls?.Dispose();
            controls = null;
        }

        public void ReturnToMainMenu()
        {
            if (isLoadingMainScene)
            {
                return;
            }

            if (!Application.CanStreamedLevelBeLoaded(
                    StageSceneFlow.MainSceneName))
            {
                Debug.LogError(
                    $"Ending scene could not load '{StageSceneFlow.MainSceneName}'. " +
                    "Add it to Build Settings.",
                    this);
                return;
            }

            isLoadingMainScene = true;
            SceneManager.LoadScene(StageSceneFlow.MainSceneName);
        }
    }
}
