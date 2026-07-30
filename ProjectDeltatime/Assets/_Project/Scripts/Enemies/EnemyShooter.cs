using Deltatime.Combat;
using Deltatime.Core;
using Deltatime.Player;
using Deltatime.TimeSystem;
using Deltatime.Vision;
using UnityEngine;

namespace Deltatime.Enemies
{
    [RequireComponent(typeof(Rigidbody))]
    public sealed class EnemyShooter : MonoBehaviour
    {
        public enum ShooterState
        {
            Detecting,
            Aiming,
            Firing,
            Cooldown,
            Stunned,
            Disarmed,
            Dead
        }

        [SerializeField] private WorldTimeController worldTime;
        [SerializeField] private Transform target;
        [SerializeField] private PlayerHealth targetHealth;
        [SerializeField] private WeaponController weapon;
        [SerializeField] private LineRenderer warningLine;
        [SerializeField] private VisionCone playerVision;
        [SerializeField] private Renderer bodyRenderer;
        [SerializeField] private Renderer weaponRenderer;

        [Header("Behavior")]
        [SerializeField, Min(0.1f)] private float detectionRange = 18f;
        [SerializeField, Min(0.01f)] private float aimDuration = 0.9f;
        [SerializeField, Min(0.01f)] private float cooldownDuration = 1.1f;
        [SerializeField, Min(1f)] private float rotationSpeed = 220f;
        [SerializeField, Range(0f, 45f)] private float facingToleranceDegrees = 5f;

        private readonly RaycastHit[] sightHits = new RaycastHit[24];
        private Rigidbody body;
        private float stateTimeRemaining;
        private bool isDisarmed;

        public ShooterState CurrentState { get; private set; } = ShooterState.Detecting;
        public bool IsDisarmed => isDisarmed;
        public float StunTimeRemaining =>
            CurrentState == ShooterState.Stunned
                ? Mathf.Max(0f, stateTimeRemaining)
                : 0f;

        private void Awake()
        {
            body = GetComponent<Rigidbody>();
            if (worldTime == null ||
                target == null ||
                targetHealth == null ||
                weapon == null ||
                warningLine == null ||
                playerVision == null ||
                bodyRenderer == null ||
                weaponRenderer == null)
            {
                Debug.LogError($"{nameof(EnemyShooter)} is missing required references.", this);
                enabled = false;
            }

            SetWarningVisible(false);
        }

        private void Start()
        {
            UpdateVisionVisibility();
        }

        private void Update()
        {
            if (CurrentState == ShooterState.Dead || targetHealth == null || !targetHealth.IsAlive)
            {
                SetWarningVisible(false);
                return;
            }

            float deltaTime = worldTime.WorldDeltaTime;

            switch (CurrentState)
            {
                case ShooterState.Detecting:
                    if (CanSeeTarget())
                    {
                        TransitionTo(ShooterState.Aiming, aimDuration);
                    }
                    break;

                case ShooterState.Aiming:
                    if (!CanSeeTarget())
                    {
                        TransitionTo(ShooterState.Detecting, 0f);
                        break;
                    }

                    RotateTowardsTarget(deltaTime);
                    bool isFacingTarget = IsFacingTarget();
                    SetWarningVisible(isFacingTarget);
                    if (!isFacingTarget)
                    {
                        break;
                    }

                    UpdateWarningLine();
                    stateTimeRemaining -= deltaTime;
                    if (stateTimeRemaining <= 0f)
                    {
                        TransitionTo(ShooterState.Firing, 0f);
                    }
                    break;

                case ShooterState.Firing:
                    Fire();
                    TransitionTo(ShooterState.Cooldown, cooldownDuration);
                    break;

                case ShooterState.Cooldown:
                    RotateTowardsTarget(deltaTime);
                    stateTimeRemaining -= deltaTime;
                    if (stateTimeRemaining <= 0f)
                    {
                        TransitionTo(ShooterState.Detecting, 0f);
                    }
                    break;

                case ShooterState.Stunned:
                    stateTimeRemaining -= deltaTime;
                    if (stateTimeRemaining <= 0f)
                    {
                        TransitionTo(
                            isDisarmed || weapon == null || !weapon.HasWeapon
                                ? ShooterState.Disarmed
                                : ShooterState.Detecting,
                            0f);
                    }
                    break;

                case ShooterState.Disarmed:
                    break;
            }
        }

        private void LateUpdate()
        {
            UpdateVisionVisibility();
        }

        public void SetDead()
        {
            TransitionTo(ShooterState.Dead, 0f);
            enabled = false;
        }

        public void ApplyStun(float worldDuration)
        {
            if (CurrentState == ShooterState.Dead || worldDuration <= 0f)
            {
                return;
            }

            if (CurrentState == ShooterState.Stunned)
            {
                stateTimeRemaining = Mathf.Max(
                    stateTimeRemaining,
                    worldDuration);
                SetWarningVisible(false);
                return;
            }

            TransitionTo(ShooterState.Stunned, worldDuration);
        }

        public void Disarm()
        {
            if (CurrentState == ShooterState.Dead)
            {
                return;
            }

            isDisarmed = true;
            if (weapon != null)
            {
                weapon.Clear();
            }

            if (CurrentState != ShooterState.Stunned)
            {
                TransitionTo(ShooterState.Disarmed, 0f);
            }
        }

        public void Configure(
            WorldTimeController timeSource,
            Transform playerTarget,
            PlayerHealth playerHealth,
            WeaponController weaponController,
            LineRenderer telegraphLine,
            VisionCone vision,
            Renderer enemyBodyRenderer,
            Renderer heldWeaponRenderer)
        {
            worldTime = timeSource;
            target = playerTarget;
            targetHealth = playerHealth;
            weapon = weaponController;
            warningLine = telegraphLine;
            playerVision = vision;
            bodyRenderer = enemyBodyRenderer;
            weaponRenderer = heldWeaponRenderer;
        }

        private void UpdateVisionVisibility()
        {
            if (playerVision == null ||
                bodyRenderer == null ||
                weaponRenderer == null ||
                weapon == null)
            {
                return;
            }

            bool visible = playerVision.ContainsWorldPoint(bodyRenderer.bounds.center);
            bodyRenderer.enabled = visible;
            weaponRenderer.enabled = visible && weapon.HasWeapon;
        }

        private void TransitionTo(ShooterState nextState, float stateDuration)
        {
            CurrentState = nextState;
            stateTimeRemaining = stateDuration;
            SetWarningVisible(false);
        }

        private void RotateTowardsTarget(float deltaTime)
        {
            Vector3 direction = target.position - body.position;
            direction.y = 0f;
            if (direction.sqrMagnitude <= 0.000001f)
            {
                return;
            }

            Quaternion targetRotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
            body.MoveRotation(Quaternion.RotateTowards(
                body.rotation,
                targetRotation,
                rotationSpeed * deltaTime));
        }

        private bool IsFacingTarget()
        {
            Vector3 direction = target.position - body.position;
            direction.y = 0f;
            if (direction.sqrMagnitude <= 0.000001f)
            {
                return false;
            }

            float angleError = Vector3.Angle(transform.forward, direction);
            return angleError <= facingToleranceDegrees;
        }

        private void Fire()
        {
            Vector3 direction = target.position - weapon.Muzzle.position;
            direction.y = 0f;
            direction.Normalize();
            weapon.TryFire(CombatFaction.Enemy, direction, worldTime.WorldElapsedTime, worldTime);
        }

        private bool CanSeeTarget()
        {
            Vector3 origin = weapon.Muzzle.position;
            Vector3 offset = target.position + (Vector3.up * 0.2f) - origin;
            float distance = offset.magnitude;

            if (distance > detectionRange || distance <= 0.001f)
            {
                return false;
            }

            int count = Physics.RaycastNonAlloc(
                origin,
                offset / distance,
                sightHits,
                distance,
                ~0,
                QueryTriggerInteraction.Ignore);

            float nearestDistance = float.PositiveInfinity;
            Collider nearestCollider = null;

            for (int i = 0; i < count; i++)
            {
                Collider collider = sightHits[i].collider;
                if (collider == null || collider.isTrigger || CombatQuery.BelongsToSource(collider, gameObject))
                {
                    continue;
                }

                if (CombatQuery.TryGetDamageable(collider, out IDamageable damageable) && damageable.Faction == CombatFaction.Enemy)
                {
                    continue;
                }

                if (sightHits[i].distance < nearestDistance)
                {
                    nearestDistance = sightHits[i].distance;
                    nearestCollider = collider;
                }
            }

            return nearestCollider != null && CombatQuery.BelongsToSource(nearestCollider, target.gameObject);
        }

        private void UpdateWarningLine()
        {
            if (warningLine == null || !warningLine.enabled)
            {
                return;
            }

            warningLine.positionCount = 2;
            warningLine.SetPosition(0, weapon.Muzzle.position);
            warningLine.SetPosition(1, target.position);
        }

        private void SetWarningVisible(bool visible)
        {
            if (warningLine != null)
            {
                warningLine.enabled = visible;
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0.2f, 0.2f, 0.35f);
            Gizmos.DrawWireSphere(transform.position, detectionRange);
        }
    }
}
