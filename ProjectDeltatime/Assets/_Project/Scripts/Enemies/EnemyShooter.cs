using Deltatime.Combat;
using Deltatime.Core;
using Deltatime.TimeSystem;
using Deltatime.Vision;
using UnityEngine;

namespace Deltatime.Enemies
{
    [RequireComponent(typeof(EnemyMotor))]
    [RequireComponent(typeof(EnemyPerception))]
    public sealed class EnemyShooter : EnemyBehavior
    {
        public enum ShooterState
        {
            Detecting,
            Aiming,
            BurstFiring,
            Cooldown,
            Stunned,
            Disarmed,
            Dead
        }

        public enum ShooterMovementMode
        {
            Stopped,
            Pursuing,
            Holding,
            Retreating
        }

        [SerializeField] private WorldTimeController worldTime;
        [SerializeField] private EnemyPerception perception;
        [SerializeField] private EnemyMotor motor;
        [SerializeField] private WeaponController weapon;
        [SerializeField] private LineRenderer warningLine;
        [SerializeField] private VisionCone playerVision;
        [SerializeField] private Renderer bodyRenderer;
        [SerializeField] private Renderer weaponRenderer;

        [Header("Engagement")]
        [SerializeField, Min(0.1f)] private float preferredMinimumRange = 6f;
        [SerializeField, Min(0.1f)] private float preferredMaximumRange = 9f;
        [SerializeField, Min(0.1f)] private float retreatStepDistance = 3f;
        [SerializeField, Range(0f, 1f)]
        private float retreatMoveSpeedMultiplier = 0.7f;

        [Header("Attack")]
        [SerializeField, Min(0.01f)] private float aimDuration = 0.65f;
        [SerializeField, Min(1)] private int burstShotCount = 4;
        [SerializeField, Min(0.01f)] private float cooldownDuration = 1.15f;
        [SerializeField, Range(0f, 45f)]
        private float facingToleranceDegrees = 6f;

        private float stateTimeRemaining;
        private int burstShotsRemaining;

        public ShooterState CurrentState { get; private set; } =
            ShooterState.Detecting;
        public ShooterMovementMode CurrentMovementMode { get; private set; } =
            ShooterMovementMode.Stopped;

        private void Awake()
        {
            if (worldTime == null ||
                perception == null ||
                motor == null ||
                weapon == null ||
                warningLine == null ||
                playerVision == null ||
                bodyRenderer == null ||
                weaponRenderer == null)
            {
                Debug.LogError(
                    $"{nameof(EnemyShooter)} is missing required references.",
                    this);
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
            if (IsDead)
            {
                StopMovement();
                SetWarningVisible(false);
                return;
            }

            float deltaTime = worldTime.WorldDeltaTime;
            if (!AdvanceStatus(deltaTime))
            {
                CurrentState = IsStunned
                    ? ShooterState.Stunned
                    : IsDisarmed
                        ? ShooterState.Disarmed
                        : ShooterState.Dead;
                StopMovement();
                SetWarningVisible(false);
                return;
            }

            if (!perception.HasLivingTarget)
            {
                TransitionTo(ShooterState.Detecting, 0f);
                StopMovement();
                return;
            }

            bool canSeeTarget = perception.CanSeeTarget();
            UpdateMovement(canSeeTarget, deltaTime);

            if (!canSeeTarget ||
                perception.PlanarDistanceToTarget >
                preferredMaximumRange)
            {
                if (CurrentState != ShooterState.Detecting)
                {
                    TransitionTo(ShooterState.Detecting, 0f);
                }

                return;
            }

            switch (CurrentState)
            {
                case ShooterState.Detecting:
                    TransitionTo(ShooterState.Aiming, aimDuration);
                    break;

                case ShooterState.Aiming:
                    UpdateAim(deltaTime);
                    break;

                case ShooterState.BurstFiring:
                    UpdateBurst();
                    break;

                case ShooterState.Cooldown:
                    UpdateCooldown(deltaTime);
                    break;
            }
        }

        private void LateUpdate()
        {
            UpdateVisionVisibility();
        }

        public void Configure(
            WorldTimeController timeSource,
            EnemyPerception enemyPerception,
            EnemyMotor enemyMotor,
            WeaponController weaponController,
            LineRenderer telegraphLine,
            VisionCone vision,
            Renderer enemyBodyRenderer,
            Renderer heldWeaponRenderer)
        {
            worldTime = timeSource;
            perception = enemyPerception;
            motor = enemyMotor;
            weapon = weaponController;
            warningLine = telegraphLine;
            playerVision = vision;
            bodyRenderer = enemyBodyRenderer;
            weaponRenderer = heldWeaponRenderer;
        }

        protected override void OnStunned()
        {
            TransitionTo(ShooterState.Stunned, 0f);
            motor?.ClearPath();
            CurrentMovementMode = ShooterMovementMode.Stopped;
        }

        protected override void OnStunRecovered()
        {
            TransitionTo(
                IsDisarmed
                    ? ShooterState.Disarmed
                    : ShooterState.Detecting,
                0f);
            StopMovement();
        }

        protected override void OnDisarmed()
        {
            if (weapon != null)
            {
                weapon.Clear();
            }

            motor?.ClearPath();
            CurrentMovementMode = ShooterMovementMode.Stopped;
            if (!IsStunned)
            {
                TransitionTo(ShooterState.Disarmed, 0f);
            }
        }

        protected override void OnDead()
        {
            motor?.ClearPath();
            CurrentMovementMode = ShooterMovementMode.Stopped;
            TransitionTo(ShooterState.Dead, 0f);
        }

        private void UpdateMovement(
            bool canSeeTarget,
            float deltaTime)
        {
            if (canSeeTarget)
            {
                float distance = perception.PlanarDistanceToTarget;
                if (distance > preferredMaximumRange)
                {
                    CurrentMovementMode =
                        ShooterMovementMode.Pursuing;
                    motor.MoveTowards(
                        perception.Target.position,
                        preferredMaximumRange * 0.9f,
                        deltaTime);
                    return;
                }

                if (distance < preferredMinimumRange)
                {
                    CurrentMovementMode =
                        ShooterMovementMode.Retreating;
                    Vector3 away =
                        -perception.PlanarDirectionToTarget;
                    Vector3 retreatDestination =
                        transform.position +
                        (away * retreatStepDistance);
                    motor.MoveTowards(
                        retreatDestination,
                        0.15f,
                        deltaTime,
                        retreatMoveSpeedMultiplier,
                        false);
                    motor.RotateTowards(
                        perception.Target.position,
                        deltaTime);
                    return;
                }

                CurrentMovementMode =
                    ShooterMovementMode.Holding;
                motor.Stop();
                motor.RotateTowards(
                    perception.Target.position,
                    deltaTime);
                return;
            }

            if (perception.HasLastKnownTargetPosition)
            {
                CurrentMovementMode =
                    ShooterMovementMode.Pursuing;
                bool arrived = motor.MoveTowards(
                    perception.LastKnownTargetPosition,
                    0.2f,
                    deltaTime);
                if (arrived)
                {
                    StopMovement();
                }

                return;
            }

            StopMovement();
        }

        private void UpdateAim(float deltaTime)
        {
            bool isFacingTarget = IsFacingTarget();
            SetWarningVisible(isFacingTarget);
            if (!isFacingTarget)
            {
                return;
            }

            UpdateWarningLine();
            stateTimeRemaining -= deltaTime;
            if (stateTimeRemaining <= 0f)
            {
                burstShotsRemaining = Mathf.Max(1, burstShotCount);
                TransitionTo(ShooterState.BurstFiring, 0f);
            }
        }

        private void UpdateBurst()
        {
            bool isFacingTarget = IsFacingTarget();
            SetWarningVisible(isFacingTarget);
            if (!isFacingTarget)
            {
                return;
            }

            UpdateWarningLine();
            Vector3 direction =
                perception.Target.position -
                weapon.Muzzle.position;
            direction.y = 0f;
            if (direction.sqrMagnitude <= 0.000001f)
            {
                return;
            }

            if (weapon.TryFire(
                    CombatFaction.Enemy,
                    direction.normalized,
                    worldTime.WorldElapsedTime,
                    worldTime))
            {
                burstShotsRemaining--;
            }

            if (burstShotsRemaining <= 0 || weapon.Ammunition <= 0)
            {
                TransitionTo(
                    ShooterState.Cooldown,
                    cooldownDuration);
            }
        }

        private void UpdateCooldown(float deltaTime)
        {
            stateTimeRemaining -= deltaTime;
            if (stateTimeRemaining <= 0f)
            {
                TransitionTo(ShooterState.Detecting, 0f);
            }
        }

        private bool IsFacingTarget()
        {
            Vector3 direction = perception.PlanarDirectionToTarget;
            return direction.sqrMagnitude > 0.000001f &&
                   Vector3.Angle(transform.forward, direction) <=
                   facingToleranceDegrees;
        }

        private void StopMovement()
        {
            CurrentMovementMode = ShooterMovementMode.Stopped;
            motor?.Stop();
        }

        private void TransitionTo(
            ShooterState nextState,
            float stateDuration)
        {
            CurrentState = nextState;
            stateTimeRemaining = Mathf.Max(0f, stateDuration);
            SetWarningVisible(false);
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

            bool visible =
                playerVision.ContainsWorldPoint(bodyRenderer.bounds.center);
            bodyRenderer.enabled = visible;
            weaponRenderer.enabled = visible && weapon.HasWeapon;
            if (!visible)
            {
                SetWarningVisible(false);
            }
        }

        private void UpdateWarningLine()
        {
            if (warningLine == null ||
                !warningLine.enabled ||
                perception.Target == null)
            {
                return;
            }

            warningLine.positionCount = 2;
            warningLine.SetPosition(0, weapon.Muzzle.position);
            warningLine.SetPosition(1, perception.Target.position);
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
            Gizmos.color = new Color(1f, 0.7f, 0.1f, 0.3f);
            Gizmos.DrawWireSphere(
                transform.position,
                preferredMinimumRange);
            Gizmos.color = new Color(1f, 0.2f, 0.2f, 0.25f);
            Gizmos.DrawWireSphere(
                transform.position,
                preferredMaximumRange);
        }
    }
}
