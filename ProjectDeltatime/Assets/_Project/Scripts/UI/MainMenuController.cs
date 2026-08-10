using Deltatime.Audio;
using Deltatime.InputSystem;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Deltatime.UI
{
    /// <summary>
    /// Handles the single action exposed by the title screen.
    /// </summary>
    public sealed class MainMenuController : MonoBehaviour
    {
        [SerializeField] private string playSceneName = "Tutorial";

        private PlayerControls controls;
        private bool isLoadingPlayScene;

        public string PlaySceneName => playSceneName;

        private void Awake()
        {
            controls = new PlayerControls();
        }

        private void OnEnable()
        {
            controls?.Gameplay.Enable();
        }

        private void Update()
        {
            if (!isLoadingPlayScene &&
                controls != null &&
                controls.Gameplay.NextStage.WasPressedThisFrame())
            {
                Play();
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

        public void Play()
        {
            if (isLoadingPlayScene)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(playSceneName))
            {
                Debug.LogError("Main menu Play scene is not configured.", this);
                return;
            }

            if (!Application.CanStreamedLevelBeLoaded(playSceneName))
            {
                Debug.LogError($"Main menu could not load '{playSceneName}'. Add it to Build Settings.", this);
                return;
            }

            isLoadingPlayScene = true;
            SoundManager.Instance?.PlayUiClick();
            SceneManager.LoadScene(playSceneName);
        }
    }
}
