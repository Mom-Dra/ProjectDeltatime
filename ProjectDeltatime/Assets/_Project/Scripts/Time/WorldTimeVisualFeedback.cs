using UnityEngine;

namespace Deltatime.TimeSystem
{
    public sealed class WorldTimeVisualFeedback : MonoBehaviour
    {
        [SerializeField] private WorldTimeController worldTime;
        [SerializeField] private Camera gameplayCamera;
        [SerializeField] private Color nearlyStoppedColor =
            new Color(0.012f, 0.014f, 0.017f, 1f);
        [SerializeField] private Color activeColor =
            new Color(0.004f, 0.008f, 0.014f, 1f);
        [SerializeField, Min(0.01f)] private float colorBlendSpeed = 7f;

        private void Awake()
        {
            if (worldTime == null || gameplayCamera == null)
            {
                Debug.LogError(
                    $"{nameof(WorldTimeVisualFeedback)} requires world time and a camera.",
                    this);
                enabled = false;
            }
        }

        private void Update()
        {
            Color target = Color.Lerp(
                nearlyStoppedColor,
                activeColor,
                Mathf.Clamp01(worldTime.CurrentTimeScale));
            float blend = 1f - Mathf.Exp(-colorBlendSpeed * UnityEngine.Time.unscaledDeltaTime);
            gameplayCamera.backgroundColor = Color.Lerp(
                gameplayCamera.backgroundColor,
                target,
                blend);
        }

        private void OnGUI()
        {
            float slowAmount = 1f - Mathf.Clamp01(worldTime.CurrentTimeScale);
            if (slowAmount <= 0.001f)
            {
                return;
            }

            int previousDepth = GUI.depth;
            Color previousColor = GUI.color;
            GUI.depth = 1000;
            GUI.color = new Color(0.02f, 0.025f, 0.035f, slowAmount * 0.22f);
            GUI.DrawTexture(
                new Rect(0f, 0f, Screen.width, Screen.height),
                Texture2D.whiteTexture);
            GUI.color = previousColor;
            GUI.depth = previousDepth;
        }

        public void Configure(WorldTimeController timeSource, Camera targetCamera)
        {
            worldTime = timeSource;
            gameplayCamera = targetCamera;
        }
    }
}
