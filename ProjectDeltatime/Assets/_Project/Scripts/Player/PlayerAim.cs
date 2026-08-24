using Deltatime.Core;
using Deltatime.InputSystem;
using Deltatime.TimeSystem;
using UnityEngine;
using UnityEngine.Rendering;

namespace Deltatime.Player
{
    [DefaultExecutionOrder(-360)]
    [RequireComponent(typeof(Rigidbody))]
    public sealed class PlayerAim : MonoBehaviour
    {
        private const float BodyForwardDebugRayLength = 1.5f;
        private const int FireAimHitCapacity = 64;
        private const float DirectionEpsilon = 0.0001f;
        private const float HorizontalSurfaceNormalThreshold = 0.7f;

        [SerializeField] private PlayerInputReader input;
        [SerializeField] private WorldTimeActivity worldTimeActivity;
        [SerializeField] private Camera gameplayCamera;
        [SerializeField, Min(1f)] private float angularSpeedForFullActivity = 360f;

        private Rigidbody body;
        private readonly RaycastHit[] fireAimHits =
            new RaycastHit[FireAimHitCapacity];
        private bool fireAimUsesOriginHeight;
        private float previousAngle;
        private bool hasPreviousAngle;

        public Vector3 AimDirection { get; private set; } = Vector3.forward;
        public Vector3 AimPoint { get; private set; }
        /// <summary>
        /// The closest visible firearm target beneath the cursor. Damageable
        /// targets and walls retain their actual 3D contact point; horizontal
        /// ground and ceiling contacts retain their horizontal intent and use
        /// the firearm origin height when their direction is calculated.
        /// </summary>
        public Vector3 FireAimPoint { get; private set; }
        public float AimAngleDegrees { get; private set; }

        private void Awake()
        {
            body = GetComponent<Rigidbody>();
            AimPoint = body.position + AimDirection;
            FireAimPoint = AimPoint;
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

            UpdateFireAimPoint(pointerRay);

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
            return delta.sqrMagnitude > DirectionEpsilon
                ? delta.normalized
                : AimDirection;
        }

        /// <summary>
        /// Returns the full 3D direction from a firearm muzzle to the cursor's
        /// visible physical point. This intentionally preserves vertical
        /// displacement for shots between platforms.
        /// </summary>
        public Vector3 GetFireDirectionFrom(Vector3 origin)
        {
            Vector3 targetPoint = FireAimPoint;
            if (fireAimUsesOriginHeight)
            {
                targetPoint.y = origin.y;
            }

            Vector3 delta = targetPoint - origin;
            return delta.sqrMagnitude > DirectionEpsilon
                ? delta.normalized
                : GetPlanarDirectionFrom(origin);
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

        internal Vector3 ResolveFireAimPoint(Ray pointerRay)
        {
            return ResolveFireAimPoint(pointerRay, out _);
        }

        internal void UpdateFireAimPoint(Ray pointerRay)
        {
            FireAimPoint = ResolveFireAimPoint(
                pointerRay,
                out fireAimUsesOriginHeight);
        }

        private Vector3 ResolveFireAimPoint(
            Ray pointerRay,
            out bool usesOriginHeight)
        {
            if (TryResolveFireAimPoint(
                    pointerRay,
                    out Vector3 point,
                    out usesOriginHeight))
            {
                return point;
            }

            usesOriginHeight = true;
            return TryResolveAimPoint(pointerRay, out point)
                ? point
                : AimPoint;
        }

        internal bool TryResolveFireAimPoint(Ray pointerRay, out Vector3 point)
        {
            return TryResolveFireAimPoint(pointerRay, out point, out _);
        }

        private bool TryResolveFireAimPoint(
            Ray pointerRay,
            out Vector3 point,
            out bool usesOriginHeight)
        {
            int count = Physics.RaycastNonAlloc(
                pointerRay,
                fireAimHits,
                gameplayCamera == null ? Mathf.Infinity : gameplayCamera.farClipPlane,
                Physics.DefaultRaycastLayers,
                QueryTriggerInteraction.Ignore);

            point = default;
            usesOriginHeight = false;
            float nearestDistance = float.PositiveInfinity;
            for (int i = 0; i < count; i++)
            {
                RaycastHit hit = fireAimHits[i];
                Collider collider = hit.collider;
                if (!IsFireAimCandidate(collider) || hit.distance >= nearestDistance)
                {
                    continue;
                }

                nearestDistance = hit.distance;
                point = GetFireAimPoint(hit, out usesOriginHeight);
            }

            return nearestDistance < float.PositiveInfinity;
        }

        private bool IsFireAimCandidate(Collider collider)
        {
            return collider != null &&
                   collider.enabled &&
                   !collider.isTrigger &&
                   !CombatQuery.BelongsToSource(collider, gameObject) &&
                   !IsHiddenCutawayCollider(collider);
        }

        private Vector3 GetFireAimPoint(
            RaycastHit hit,
            out bool usesOriginHeight)
        {
            if (CombatQuery.TryGetDamageable(hit.collider, out _) ||
                Mathf.Abs(hit.normal.y) < HorizontalSurfaceNormalThreshold)
            {
                usesOriginHeight = false;
                return hit.point;
            }

            // Clicking a horizontal ground or ceiling surface should retain
            // the cursor's horizontal intent without pitching the muzzle into
            // the floor immediately in front of the player. The target Y is
            // resolved from the firing origin in GetFireDirectionFrom.
            usesOriginHeight = true;
            return new Vector3(hit.point.x, body.position.y, hit.point.z);
        }

        private static bool IsHiddenCutawayCollider(Collider collider)
        {
            for (Transform current = collider.transform;
                 current != null;
                 current = current.parent)
            {
                Renderer renderer = current.GetComponent<Renderer>();
                if (renderer != null &&
                    renderer.shadowCastingMode == ShadowCastingMode.ShadowsOnly)
                {
                    return true;
                }
            }

            return false;
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
