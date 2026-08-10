using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;

namespace Deltatime.UI
{
    /// <summary>
    /// Presents hover and press feedback for the text-only Play action.
    /// </summary>
    public sealed class MainMenuButtonFeedback : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler,
        IPointerDownHandler, IPointerUpHandler
    {
        [SerializeField] private TextMeshProUGUI label;
        [SerializeField, Min(1f)] private float hoverScale = 1.08f;
        [SerializeField] private Color pressedColor = new Color(224f / 255f, 28f / 255f, 28f / 255f, 1f);

        private bool isPointerOver;
        private bool isPointerDown;

        public TextMeshProUGUI Label => label;
        public float HoverScale => hoverScale;
        public Color PressedColor => pressedColor;

        public void Configure(TextMeshProUGUI targetLabel, float targetHoverScale, Color targetPressedColor)
        {
            label = targetLabel;
            hoverScale = Mathf.Max(1f, targetHoverScale);
            pressedColor = targetPressedColor;
            ApplyVisualState();
        }

        private void OnEnable()
        {
            ApplyVisualState();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            isPointerOver = true;
            ApplyVisualState();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            isPointerOver = false;
            isPointerDown = false;
            ApplyVisualState();
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            isPointerDown = true;
            ApplyVisualState();
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            isPointerDown = false;
            ApplyVisualState();
        }

        private void ApplyVisualState()
        {
            if (label == null)
            {
                return;
            }

            label.rectTransform.localScale = isPointerOver
                ? Vector3.one * hoverScale
                : Vector3.one;
            label.color = isPointerDown && isPointerOver ? pressedColor : Color.white;
        }
    }
}
