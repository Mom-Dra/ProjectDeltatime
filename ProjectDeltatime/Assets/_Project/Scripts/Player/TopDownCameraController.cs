using Deltatime.InputSystem;
using UnityEngine;

namespace Deltatime.Player
{
    [RequireComponent(typeof(Camera))]
    public sealed class TopDownCameraController : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private PlayerAim aim;
        [SerializeField] private PlayerInputReader input;
        [SerializeField] private Vector3 cameraOffset = new Vector3(0f, 13.5f, -12.5f);
        [SerializeField, Min(0f)] private float aimLeadDistance = 2.25f;
        [SerializeField, Min(0.01f)] private float followSharpness = 8f;
        [SerializeField, Min(0.01f)] private float rotationSharpness = 12f;
        [SerializeField, Min(0f)] private float lookHeight = 0.55f;

        private bool initialized;

        private void Awake()
        {
            ValidateConfiguration();
        }

        private void Start()
        {
            if (!enabled)
            {
                return;
            }

            SnapToTarget();
        }

        private void LateUpdate()
        {
            if (!initialized || target == null)
            {
                return;
            }

            Vector3 aimLead = aim == null
                ? Vector3.zero
                : aim.AimDirection * aimLeadDistance;
            Vector3 focus = target.position + aimLead;
            Vector3 desiredPosition = focus + cameraOffset;
            float positionBlend =
                1f - Mathf.Exp(-followSharpness * UnityEngine.Time.unscaledDeltaTime);
            transform.position = Vector3.Lerp(
                transform.position,
                desiredPosition,
                positionBlend);

            Vector3 lookPoint = focus + (Vector3.up * lookHeight);
            Quaternion desiredRotation = Quaternion.LookRotation(
                lookPoint - transform.position,
                Vector3.up);
            float rotationBlend =
                1f - Mathf.Exp(-rotationSharpness * UnityEngine.Time.unscaledDeltaTime);
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                desiredRotation,
                rotationBlend);
        }

        public void Configure(
            Transform followTarget,
            PlayerAim playerAim,
            PlayerInputReader inputReader)
        {
            target = followTarget;
            aim = playerAim;
            input = inputReader;
            initialized = target != null;
        }

        public void SnapToTarget()
        {
            if (target == null)
            {
                return;
            }

            Vector3 focus = target.position;
            transform.position = focus + cameraOffset;
            transform.LookAt(focus + (Vector3.up * lookHeight), Vector3.up);
            initialized = true;
        }

        private void ValidateConfiguration()
        {
            if (target == null && !initialized)
            {
                return;
            }

            if (target == null || aim == null || input == null)
            {
                Debug.LogError(
                    $"{nameof(TopDownCameraController)} requires a target, aim, and input.",
                    this);
                enabled = false;
            }
        }
    }
}
