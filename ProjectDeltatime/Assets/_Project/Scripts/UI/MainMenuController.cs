using Deltatime.Audio;
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

        public string PlaySceneName => playSceneName;

        public void Play()
        {
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

            SoundManager.Instance?.PlayUiClick();
            SceneManager.LoadScene(playSceneName);
        }
    }
}
