using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Deltatime.UI
{
    public sealed class MainMenuButtonFeedback : MonoBehaviour,
        IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler,
        IPointerUpHandler, ISelectHandler, IDeselectHandler
    {
        [SerializeField] private TextMeshProUGUI label;
        [SerializeField] private TextMeshProUGUI pointer;
        [SerializeField] private Image highlight;
        [SerializeField] private Color idleColor = new Color(0.56f, 0.56f, 0.58f, 1f);
        [SerializeField] private Color activeColor = Color.white;
        [SerializeField] private Color pressedColor = new Color(0.88f, 0.11f, 0.11f, 1f);

        private bool pointerOver;
        private bool pointerDown;
        private bool selected;

        public TextMeshProUGUI Label => label;

        public void Configure(
            TextMeshProUGUI targetLabel,
            TextMeshProUGUI targetPointer,
            Image targetHighlight)
        {
            label = targetLabel;
            pointer = targetPointer;
            highlight = targetHighlight;
            ApplyVisualState();
        }

        private void OnEnable() => ApplyVisualState();
        public void OnPointerEnter(PointerEventData eventData) { pointerOver = true; ApplyVisualState(); }
        public void OnPointerExit(PointerEventData eventData) { pointerOver = false; pointerDown = false; ApplyVisualState(); }
        public void OnPointerDown(PointerEventData eventData) { pointerDown = true; ApplyVisualState(); }
        public void OnPointerUp(PointerEventData eventData) { pointerDown = false; ApplyVisualState(); }
        public void OnSelect(BaseEventData eventData) { selected = true; ApplyVisualState(); }
        public void OnDeselect(BaseEventData eventData) { selected = false; ApplyVisualState(); }

        private void ApplyVisualState()
        {
            bool active = selected || pointerOver;
            if (label != null)
            {
                label.color = pointerDown && active ? pressedColor : active ? activeColor : idleColor;
            }

            pointer?.gameObject.SetActive(active);
            highlight?.gameObject.SetActive(active);
        }
    }
}
