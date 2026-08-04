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
        [SerializeField] private LayerMask aimCollisionMask = ~0;
        [SerializeField, Min(1f)] private float angularSpeedForFullActivity = 360f;
        [SerializeField, Min(0.1f)] private float directionLineLength = 1.2f;

        private readonly RaycastHit[] aimHits = new RaycastHit[32];
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
            UpdateDirectionLine();
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
            Camera targetCamera,
            LineRenderer debugLine,
            LayerMask collisionMask)
        {
            input = inputReader;
            worldTimeActivity = activity;
            gameplayCamera = targetCamera;
            directionLine = debugLine;
            aimCollisionMask = collisionMask;
        }

        private bool TryResolveAimPoint(Ray pointerRay, out Vector3 point)
        {
            int hitCount = Physics.RaycastNonAlloc(
                pointerRay,
                aimHits,
                gameplayCamera.farClipPlane,
                aimCollisionMask,
                QueryTriggerInteraction.Ignore);
            float nearestDistance = float.PositiveInfinity;
            point = default;

            for (int i = 0; i < hitCount; i++)
            {
                RaycastHit hit = aimHits[i];
                Collider collider = hit.collider;
                if (collider == null ||
                    collider.isTrigger ||
                    collider.transform == transform ||
                    collider.transform.IsChildOf(transform) ||
                    hit.distance >= nearestDistance)
                {
                    continue;
                }

                nearestDistance = hit.distance;
                point = hit.point;
            }

            if (nearestDistance < float.PositiveInfinity)
            {
                return true;
            }

            Plane groundPlane = new Plane(Vector3.up, Vector3.zero);
            if (!groundPlane.Raycast(pointerRay, out float hitDistance))
            {
                return false;
            }

            point = pointerRay.GetPoint(hitDistance);
            return true;
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
