using Deltatime.InputSystem;
using Deltatime.Level;
using Deltatime.TimeSystem;
using UnityEngine;

namespace Deltatime.Player
{
    [DefaultExecutionOrder(-340)]
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(CapsuleCollider))]
    public sealed class PlayerDash : MonoBehaviour
    {
        private const float CollisionSkin = 0.03f;
        private const float MinimumCastRadius = 0.001f;
        private const int HitBufferSize = 16;

        [SerializeField] private PlayerInputReader input;
        [SerializeField] private PlayerHealth health;
        [SerializeField] private WorldTimeActivity worldTimeActivity;
        [SerializeField] private WorldTimeController worldTime;

        [Header("Dash")]
        [SerializeField, Min(0.1f)] private float dashDistance = 3.5f;
        [SerializeField, Min(0.1f)] private float dashSpeed = 22f;
        [SerializeField, Min(0.01f)] private float dashDuration = 0.16f;
        [SerializeField, Min(0f)] private float dashCooldown = 0.8f;
        [SerializeField, Range(0f, 1f)] private float activityStrength = 1f;
        [SerializeField, Min(0.01f)] private float activityDuration = 0.22f;

        private Rigidbody body;
        private CapsuleCollider capsuleCollider;
        private NavMeshGroundMovement groundMovement;
        private readonly RaycastHit[] dashHits = new RaycastHit[HitBufferSize];
        private Vector3 dashDirection;
        private float dashTimeRemaining;
        private float dashDistanceRemaining;
        private float cooldownRemaining;

        public bool IsDashing { get; private set; }
        public Vector3 DashDirection => dashDirection;
        public float CooldownRemaining => cooldownRemaining;
        public float CooldownDuration => dashCooldown;
        public float CooldownNormalized =>
            dashCooldown <= 0f ? 0f : Mathf.Clamp01(cooldownRemaining / dashCooldown);

        private void Awake()
        {
            body = GetComponent<Rigidbody>();
            capsuleCollider = GetComponent<CapsuleCollider>();
            groundMovement = GetComponent<NavMeshGroundMovement>();
            ValidateConfiguration();
        }

        private void Update()
        {
            if (worldTime.IsHardFrozen)
            {
                return;
            }

            cooldownRemaining = Mathf.Max(0f, cooldownRemaining - UnityEngine.Time.unscaledDeltaTime);

            if (input.DashPressed && CanStartDash())
            {
                StartDash();
            }
        }

        private void FixedUpdate()
        {
            if (!IsDashing || worldTime.IsHardFrozen)
            {
                return;
            }

            float realDeltaTime = UnityEngine.Time.fixedUnscaledDeltaTime;
            float requestedDistance = Mathf.Min(
                dashSpeed * realDeltaTime,
                dashDistanceRemaining);
            float safeDistance = GetSafeDashDistance(requestedDistance);

            if (safeDistance > 0f)
            {
                if (groundMovement != null)
                {
                    if (!groundMovement.TryMove(
                            body,
                            dashDirection * safeDistance,
                            out safeDistance))
                    {
                        safeDistance = 0f;
                    }
                }
                else
                {
                    body.MovePosition(body.position + (dashDirection * safeDistance));
                }
            }

            dashDistanceRemaining -= safeDistance;
            dashTimeRemaining -= realDeltaTime;

            bool hitWall = safeDistance + 0.001f < requestedDistance;
            if (hitWall || dashDistanceRemaining <= 0f || dashTimeRemaining <= 0f)
            {
                EndDash();
            }
        }

        public void Configure(
            PlayerInputReader inputReader,
            PlayerHealth playerHealth,
            WorldTimeActivity activity,
            WorldTimeController timeSource)
        {
            input = inputReader;
            health = playerHealth;
            worldTimeActivity = activity;
            worldTime = timeSource;
        }

        private bool CanStartDash()
        {
            return !IsDashing &&
                   cooldownRemaining <= 0f &&
                   health != null &&
                   health.IsAlive &&
                   input.Move.sqrMagnitude > 0.01f;
        }

        private void StartDash()
        {
            dashDirection =
                new Vector3(input.Move.x, 0f, input.Move.y).normalized;
            dashTimeRemaining = dashDuration;
            dashDistanceRemaining = dashDistance;
            cooldownRemaining = dashCooldown;
            body.linearVelocity = Vector3.zero;
            IsDashing = true;
            health.SetDashInvulnerable(true);
            worldTimeActivity.Pulse(activityStrength, activityDuration);
        }

        private void EndDash()
        {
            IsDashing = false;
            dashTimeRemaining = 0f;
            dashDistanceRemaining = 0f;

            if (body != null)
            {
                body.linearVelocity = Vector3.zero;
            }

            if (health != null)
            {
                health.SetDashInvulnerable(false);
            }
        }

        private float GetSafeDashDistance(float requestedDistance)
        {
            if (requestedDistance <= 0f)
            {
                return 0f;
            }

            GetShrunkWorldCapsule(
                out Vector3 point1,
                out Vector3 point2,
                out float radius);
            int hitCount = Physics.CapsuleCastNonAlloc(
                point1,
                point2,
                radius,
                dashDirection,
                dashHits,
                requestedDistance + CollisionSkin,
                Physics.AllLayers,
                QueryTriggerInteraction.Ignore);
            float safeDistance = requestedDistance;

            for (int i = 0; i < hitCount; i++)
            {
                Collider hitCollider = dashHits[i].collider;
                if (hitCollider == null ||
                    hitCollider.isTrigger ||
                    hitCollider.attachedRigidbody == body ||
                    Physics.GetIgnoreLayerCollision(
                        gameObject.layer,
                        hitCollider.gameObject.layer))
                {
                    continue;
                }

                safeDistance = Mathf.Min(
                    safeDistance,
                    Mathf.Max(0f, dashHits[i].distance - CollisionSkin));
            }

            return safeDistance;
        }

        private void GetShrunkWorldCapsule(
            out Vector3 point1,
            out Vector3 point2,
            out float radius)
        {
            // The skin gap lets the cast detect a wall even when the full
            // player capsule starts at contact or with slight penetration.
            Transform capsuleTransform = capsuleCollider.transform;
            Vector3 scale = capsuleTransform.lossyScale;
            Vector3 axis;
            float axisScale;
            float radiusScale;

            switch (capsuleCollider.direction)
            {
                case 0:
                    axis = body.rotation * Vector3.right;
                    axisScale = Mathf.Abs(scale.x);
                    radiusScale = Mathf.Max(
                        Mathf.Abs(scale.y),
                        Mathf.Abs(scale.z));
                    break;
                case 2:
                    axis = body.rotation * Vector3.forward;
                    axisScale = Mathf.Abs(scale.z);
                    radiusScale = Mathf.Max(
                        Mathf.Abs(scale.x),
                        Mathf.Abs(scale.y));
                    break;
                default:
                    axis = body.rotation * Vector3.up;
                    axisScale = Mathf.Abs(scale.y);
                    radiusScale = Mathf.Max(
                        Mathf.Abs(scale.x),
                        Mathf.Abs(scale.z));
                    break;
            }

            float worldRadius = capsuleCollider.radius * radiusScale;
            float worldHeight = Mathf.Max(
                capsuleCollider.height * axisScale,
                worldRadius * 2f);
            float segmentHalfLength =
                Mathf.Max(0f, (worldHeight * 0.5f) - worldRadius);
            Vector3 scaledCenter = Vector3.Scale(
                capsuleCollider.center,
                scale);
            Vector3 worldCenter =
                body.position + (body.rotation * scaledCenter);

            axis.Normalize();
            point1 = worldCenter + (axis * segmentHalfLength);
            point2 = worldCenter - (axis * segmentHalfLength);
            radius = Mathf.Max(
                MinimumCastRadius,
                worldRadius - CollisionSkin);
        }

        private void OnDisable()
        {
            if (IsDashing)
            {
                EndDash();
            }
        }

        private void ValidateConfiguration()
        {
            if (input == null ||
                health == null ||
                worldTimeActivity == null ||
                worldTime == null)
            {
                Debug.LogError(
                    $"{nameof(PlayerDash)} requires input, health, activity, and world-time references.",
                    this);
                enabled = false;
            }
        }
    }
}
