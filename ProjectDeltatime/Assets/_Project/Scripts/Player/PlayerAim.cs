using Deltatime.InputSystem;
using Deltatime.TimeSystem;
using UnityEngine;

namespace Deltatime.Player
{
    [DefaultExecutionOrder(-360)]
    [RequireComponent(typeof(Rigidbody))]
    public sealed class PlayerAim : MonoBehaviour
    {
        private const float BodyForwardDebugRayLength = 1.5f;

        [SerializeField] private PlayerInputReader input;
        [SerializeField] private WorldTimeActivity worldTimeActivity;
        [SerializeField] private Camera gameplayCamera;
        [SerializeField, Min(1f)] private float angularSpeedForFullActivity = 360f;

        private Rigidbody body;
        private float previousAngle;
        private bool hasPreviousAngle;

        public Vector3 AimDirection { get; private set; } = Vector3.forward;
        public Vector3 AimPoint { get; private set; }
        public float AimAngleDegrees { get; private set; }

        private void Awake()
        {
            body = GetComponent<Rigidbody>();
            AimPoint = body.position + AimDirection;
            ValidateConfiguration();
        }

        private void Update()
        {
            Ray pointerRay = gameplayCamera.ScreenPointToRay(input.PointerScreenPosition);
            if (TryResolveAimPoint(pointerRay, out Vector3 worldPoint))
            {
                AimPoint = worldPoint;
                Vector3 delta = worldPoint - body.position;
                delta.y = 0f;
                if (delta.sqrMagnitude > 0.0001f)
                {
                    AimDirection = delta.normalized;
                    AimAngleDegrees =
                        Mathf.Atan2(AimDirection.x, AimDirection.z) * Mathf.Rad2Deg;
                    body.MoveRotation(Quaternion.Euler(0f, AimAngleDegrees, 0f));
                }
            }

            float turnActivity = 0f;
            if (hasPreviousAngle && UnityEngine.Time.unscaledDeltaTime > 0.00001f)
            {
                float degreesPerSecond =
                    Mathf.Abs(Mathf.DeltaAngle(previousAngle, AimAngleDegrees)) /
                    UnityEngine.Time.unscaledDeltaTime;
                turnActivity = degreesPerSecond / angularSpeedForFullActivity;
            }

            previousAngle = AimAngleDegrees;
            hasPreviousAngle = true;
            worldTimeActivity.SetAimTurn(turnActivity);
            Debug.DrawRay(
                transform.position + (Vector3.up * 0.08f),
                transform.forward * BodyForwardDebugRayLength,
                Color.green);
        }

        public Vector3 GetPlanarDirectionFrom(Vector3 origin)
        {
            Vector3 delta = AimPoint - origin;
            delta.y = 0f;
            return delta.sqrMagnitude > 0.0001f
                ? delta.normalized
                : AimDirection;
        }

        public void Configure(
            PlayerInputReader inputReader,
            WorldTimeActivity activity,
            Camera targetCamera)
        {
            input = inputReader;
            worldTimeActivity = activity;
            gameplayCamera = targetCamera;
        }

        private bool TryResolveAimPoint(Ray pointerRay, out Vector3 point)
        {
            // Aim must follow the cursor's position on the actor's traversal plane,
            // not the first physics collider between the camera and that plane. In
            // particular, Stage 5 hides foreground geometry visually while keeping
            // its collision and vision blockers active.
            Plane aimPlane = new Plane(Vector3.up, body.position);
            if (!aimPlane.Raycast(pointerRay, out float hitDistance))
            {
                point = default;
                return false;
            }

            point = pointerRay.GetPoint(hitDistance);
            return true;
        }

        private void ValidateConfiguration()
        {
            if (input == null || worldTimeActivity == null || gameplayCamera == null)
            {
                Debug.LogError(
                    $"{nameof(PlayerAim)} requires input, world-time activity, and camera references.",
                    this);
                enabled = false;
            }
        }
    }
}
