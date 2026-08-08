using UnityEngine;

namespace Deltatime.Tutorial
{
    public sealed class TutorialGate : MonoBehaviour
    {
        [SerializeField] private Collider blocker;
        [SerializeField] private Renderer gateRenderer;
        [SerializeField, Min(0.1f)] private float openHeight = 3.2f;
        [SerializeField, Min(0.1f)] private float movementSpeed = 8f;
        [SerializeField] private Color closedColor = new Color(0.9f, 0.12f, 0.08f, 1f);
        [SerializeField] private Color openColor = new Color(0.12f, 0.95f, 0.35f, 1f);

        private Vector3 closedLocalPosition;
        private Material gateMaterial;
        private bool hasClosedLocalPosition;

        public bool IsOpen { get; private set; }
        public bool IsVisible => gateRenderer != null && gateRenderer.enabled;

        private void Awake()
        {
            EnsureClosedLocalPosition();
            if (blocker == null)
            {
                blocker = GetComponent<Collider>();
            }

            if (gateRenderer == null)
            {
                gateRenderer = GetComponentInChildren<Renderer>();
            }

            if (gateRenderer != null)
            {
                gateMaterial = gateRenderer.material;
            }

            ApplyState(true);
        }

        private void Update()
        {
            Vector3 target = closedLocalPosition +
                             (IsOpen ? Vector3.up * openHeight : Vector3.zero);
            transform.localPosition = Vector3.MoveTowards(
                transform.localPosition,
                target,
                movementSpeed * UnityEngine.Time.unscaledDeltaTime);

            if (gateRenderer != null)
            {
                gateRenderer.enabled = !IsOpen ||
                    (transform.localPosition - target).sqrMagnitude > 0.0001f;
            }
        }

        public void SetOpen(bool value, bool instant = false)
        {
            EnsureClosedLocalPosition();
            IsOpen = value;
            ApplyState(instant);
        }

        public void Configure(Collider gateBlocker, Renderer renderer)
        {
            blocker = gateBlocker;
            gateRenderer = renderer;
        }

        private void ApplyState(bool instant)
        {
            if (blocker != null)
            {
                blocker.enabled = !IsOpen;
            }

            if (gateMaterial != null)
            {
                gateMaterial.color = IsOpen ? openColor : closedColor;
            }

            if (gateRenderer != null)
            {
                gateRenderer.enabled = !IsOpen || !instant;
            }

            if (instant)
            {
                transform.localPosition = closedLocalPosition +
                    (IsOpen ? Vector3.up * openHeight : Vector3.zero);
            }
        }

        private void EnsureClosedLocalPosition()
        {
            if (hasClosedLocalPosition)
            {
                return;
            }

            closedLocalPosition = transform.localPosition;
            hasClosedLocalPosition = true;
        }
    }
}
