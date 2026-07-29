using Deltatime.InputSystem;
using Deltatime.TimeSystem;
using UnityEngine;

namespace Deltatime.Player
{
    [DefaultExecutionOrder(-360)]
    [RequireComponent(typeof(Rigidbody))]
    public sealed class PlayerAim : MonoBehaviour
    {
        [SerializeField] private PlayerInputReader input;
        [SerializeField] private WorldTimeActivity worldTimeActivity;
        [SerializeField] private Camera gameplayCamera;
        [SerializeField] private LineRenderer directionLine;
        [SerializeField, Min(1f)] private float angularSpeedForFullActivity = 360f;
        [SerializeField, Min(0.1f)] private float directionLineLength = 1.2f;

        private Rigidbody body;
        private float previousAngle;
        private bool hasPreviousAngle;

        public Vector3 AimDirection { get; private set; } = Vector3.forward;
        public float AimAngleDegrees { get; private set; }

        private void Awake()
        {
            body = GetComponent<Rigidbody>();
            ValidateConfiguration();
        }

        private void Update()
        {
            Ray pointerRay = gameplayCamera.ScreenPointToRay(input.PointerScreenPosition);
            Plane groundPlane = new Plane(Vector3.up, Vector3.zero);
            if (groundPlane.Raycast(pointerRay, out float hitDistance))
            {
                Vector3 worldPoint = pointerRay.GetPoint(hitDistance);
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
            UpdateDirectionLine();
        }

        public void Configure(
            PlayerInputReader inputReader,
            WorldTimeActivity activity,
            Camera targetCamera,
            LineRenderer debugLine)
        {
            input = inputReader;
            worldTimeActivity = activity;
            gameplayCamera = targetCamera;
            directionLine = debugLine;
        }

        private void UpdateDirectionLine()
        {
            if (directionLine == null)
            {
                return;
            }

            directionLine.positionCount = 2;
            Vector3 origin = transform.position + (Vector3.up * 0.08f);
            directionLine.SetPosition(0, origin);
            directionLine.SetPosition(1, origin + (AimDirection * directionLineLength));
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
