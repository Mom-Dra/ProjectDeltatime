using Deltatime.InputSystem;
using Deltatime.TimeSystem;
using UnityEngine;

namespace Deltatime.Player
{
    [DefaultExecutionOrder(-340)]
    [RequireComponent(typeof(Rigidbody))]
    public sealed class PlayerDash : MonoBehaviour
    {
        [SerializeField] private PlayerInputReader input;
        [SerializeField] private PlayerHealth health;
        [SerializeField] private WorldTimeActivity worldTimeActivity;

        [Header("Dash")]
        [SerializeField, Min(0.1f)] private float dashDistance = 3.5f;
        [SerializeField, Min(0.1f)] private float dashSpeed = 22f;
        [SerializeField, Min(0.01f)] private float dashDuration = 0.16f;
        [SerializeField, Min(0f)] private float dashCooldown = 0.8f;
        [SerializeField, Range(0f, 1f)] private float activityStrength = 1f;
        [SerializeField, Min(0.01f)] private float activityDuration = 0.22f;

        private Rigidbody body;
        private Vector3 dashDirection;
        private float dashTimeRemaining;
        private float dashDistanceRemaining;
        private float cooldownRemaining;

        public bool IsDashing { get; private set; }
        public float CooldownRemaining => cooldownRemaining;
        public float CooldownDuration => dashCooldown;
        public float CooldownNormalized =>
            dashCooldown <= 0f ? 0f : Mathf.Clamp01(cooldownRemaining / dashCooldown);

        private void Awake()
        {
            body = GetComponent<Rigidbody>();
            ValidateConfiguration();
        }

        private void Update()
        {
            cooldownRemaining = Mathf.Max(0f, cooldownRemaining - UnityEngine.Time.unscaledDeltaTime);

            if (input.DashPressed && CanStartDash())
            {
                StartDash();
            }
        }

        private void FixedUpdate()
        {
            if (!IsDashing)
            {
                return;
            }

            float realDeltaTime = UnityEngine.Time.fixedUnscaledDeltaTime;
            float requestedDistance = Mathf.Min(
                dashSpeed * realDeltaTime,
                dashDistanceRemaining);
            float safeDistance = GetSafeDashDistance(requestedDistance);

            body.MovePosition(body.position + (dashDirection * safeDistance));
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
            WorldTimeActivity activity)
        {
            input = inputReader;
            health = playerHealth;
            worldTimeActivity = activity;
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
            IsDashing = true;
            health.SetDashInvulnerable(true);
            worldTimeActivity.Pulse(activityStrength, activityDuration);
        }

        private void EndDash()
        {
            IsDashing = false;
            dashTimeRemaining = 0f;
            dashDistanceRemaining = 0f;

            if (health != null)
            {
                health.SetDashInvulnerable(false);
            }
        }

        private float GetSafeDashDistance(float requestedDistance)
        {
            RaycastHit[] hits = body.SweepTestAll(
                dashDirection,
                requestedDistance + 0.03f,
                QueryTriggerInteraction.Ignore);
            float safeDistance = requestedDistance;

            for (int i = 0; i < hits.Length; i++)
            {
                if (hits[i].collider == null || hits[i].collider.isTrigger)
                {
                    continue;
                }

                safeDistance = Mathf.Min(
                    safeDistance,
                    Mathf.Max(0f, hits[i].distance - 0.03f));
            }

            return safeDistance;
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
            if (input == null || health == null || worldTimeActivity == null)
            {
                Debug.LogError(
                    $"{nameof(PlayerDash)} requires input, health, and world-time activity references.",
                    this);
                enabled = false;
            }
        }
    }
}
