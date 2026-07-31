using Deltatime.Core;
using Deltatime.TimeSystem;
using Deltatime.Vision;
using UnityEngine;

namespace Deltatime.Enemies
{
    [RequireComponent(typeof(EnemyMotor))]
    [RequireComponent(typeof(EnemyPerception))]
    public sealed class EnemyChaser : EnemyBehavior
    {
        public enum ChaserState
        {
            Detecting,
            Chasing,
            AttackWindup,
            Attacking,
            Cooldown,
            Stunned,
            Disarmed,
            Dead
        }

        [SerializeField] private WorldTimeController worldTime;
        [SerializeField] private EnemyPerception perception;
        [SerializeField] private EnemyMotor motor;
        [SerializeField] private LineRenderer warningLine;
        [SerializeField] private VisionCone playerVision;
        [SerializeField] private Renderer bodyRenderer;
        [SerializeField] private Renderer weaponRenderer;

        [Header("Melee Attack")]
        [SerializeField, Min(0.1f)] private float attackRange = 1.45f;
        [SerializeField, Min(0.1f)] private float attackCancelRange = 1.9f;
        [SerializeField, Min(0.01f)] private float windupDuration = 0.42f;
        [SerializeField, Min(0.01f)] private float cooldownDuration = 0.72f;
        [SerializeField, Min(1)] private int damage = 1;
        [SerializeField, Range(0f, 1f)]
        private float windupMoveSpeedMultiplier = 0.35f;
        [SerializeField, Range(0f, 90f)]
        private float facingToleranceDegrees = 25f;

        private float stateTimeRemaining;

        public ChaserState CurrentState { get; private set; } =
            ChaserState.Detecting;

        private void Awake()
        {
            if (worldTime == null ||
                perception == null ||
                motor == null ||
                warningLine == null ||
                playerVision == null ||
                bodyRenderer == null ||
                weaponRenderer == null)
            {
                Debug.LogError(
                    $"{nameof(EnemyChaser)} is missing required references.",
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
                SetWarningVisible(false);
                return;
            }

            float deltaTime = worldTime.WorldDeltaTime;
            if (!AdvanceStatus(deltaTime))
            {
                CurrentState = IsStunned
                    ? ChaserState.Stunned
                    : IsDisarmed
                        ? ChaserState.Disarmed
                        : ChaserState.Dead;
                motor.Stop();
                SetWarningVisible(false);
                return;
            }

            if (!perception.HasLivingTarget)
            {
                motor.Stop();
                SetWarningVisible(false);
                return;
            }

            switch (CurrentState)
            {
                case ChaserState.Detecting:
                    motor.Stop();
                    if (perception.CanSeeTarget())
                    {
                        TransitionTo(ChaserState.Chasing, 0f);
                    }
                    break;

                case ChaserState.Chasing:
                    UpdateChase(deltaTime);
                    break;

                case ChaserState.AttackWindup:
                    UpdateAttackWindup(deltaTime);
                    break;

                case ChaserState.Attacking:
                    PerformAttack();
                    TransitionTo(
                        ChaserState.Cooldown,
                        cooldownDuration);
                    break;

                case ChaserState.Cooldown:
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
            LineRenderer telegraphLine,
            VisionCone vision,
            Renderer enemyBodyRenderer,
            Renderer heldWeaponRenderer)
        {
            worldTime = timeSource;
            perception = enemyPerception;
            motor = enemyMotor;
            warningLine = telegraphLine;
            playerVision = vision;
            bodyRenderer = enemyBodyRenderer;
            weaponRenderer = heldWeaponRenderer;
        }

        protected override void OnStunned()
        {
            TransitionTo(ChaserState.Stunned, 0f);
            motor?.ClearPath();
        }

        protected override void OnStunRecovered()
        {
            TransitionTo(
                IsDisarmed
                    ? ChaserState.Disarmed
                    : ChaserState.Detecting,
                0f);
        }

        protected override void OnDisarmed()
        {
            if (weaponRenderer != null)
            {
                weaponRenderer.enabled = false;
            }

            motor?.ClearPath();
            if (!IsStunned)
            {
                TransitionTo(ChaserState.Disarmed, 0f);
            }
        }

        protected override void OnDead()
        {
            motor?.ClearPath();
            TransitionTo(ChaserState.Dead, 0f);
        }

        private void UpdateChase(float deltaTime)
        {
            bool canSeeTarget = perception.CanSeeTarget();
            Vector3 destination;
            if (canSeeTarget)
            {
                if (perception.PlanarDistanceToTarget <= attackRange)
                {
                    TransitionTo(
                        ChaserState.AttackWindup,
                        windupDuration);
                    return;
                }

                destination = perception.Target.position;
            }
            else if (perception.HasLastKnownTargetPosition)
            {
                destination = perception.LastKnownTargetPosition;
            }
            else
            {
                TransitionTo(ChaserState.Detecting, 0f);
                return;
            }

            bool arrived = motor.MoveTowards(
                destination,
                canSeeTarget ? attackRange * 0.8f : 0.15f,
                deltaTime);
            if (arrived && !canSeeTarget)
            {
                TransitionTo(ChaserState.Detecting, 0f);
            }
        }

        private void UpdateAttackWindup(float deltaTime)
        {
            bool canSeeTarget = perception.CanSeeTarget();
            if (!canSeeTarget ||
                perception.PlanarDistanceToTarget > attackCancelRange)
            {
                TransitionTo(ChaserState.Chasing, 0f);
                return;
            }

            motor.MoveTowards(
                perception.Target.position,
                attackRange * 0.55f,
                deltaTime,
                windupMoveSpeedMultiplier,
                false);
            motor.RotateTowards(
                perception.Target.position,
                deltaTime);
            SetWarningVisible(true);
            UpdateWarningLine();

            stateTimeRemaining -= deltaTime;
            if (stateTimeRemaining <= 0f)
            {
                TransitionTo(ChaserState.Attacking, 0f);
            }
        }

        private void PerformAttack()
        {
            if (perception.TargetHealth == null ||
                !perception.TargetHealth.IsAlive ||
                perception.PlanarDistanceToTarget > attackRange ||
                !IsFacingTarget())
            {
                return;
            }

            Vector3 direction = perception.PlanarDirectionToTarget;
            perception.TargetHealth.ReceiveHit(new DamageHit(
                damage,
                perception.Target.position,
                direction,
                gameObject));
        }

        private void UpdateCooldown(float deltaTime)
        {
            motor.Stop();
            if (perception.Target != null)
            {
                motor.RotateTowards(
                    perception.Target.position,
                    deltaTime);
            }

            stateTimeRemaining -= deltaTime;
            if (stateTimeRemaining <= 0f)
            {
                TransitionTo(ChaserState.Chasing, 0f);
            }
        }

        private bool IsFacingTarget()
        {
            Vector3 direction = perception.PlanarDirectionToTarget;
            return direction.sqrMagnitude > 0.000001f &&
                   Vector3.Angle(transform.forward, direction) <=
                   facingToleranceDegrees;
        }

        private void TransitionTo(
            ChaserState nextState,
            float stateDuration)
        {
            CurrentState = nextState;
            stateTimeRemaining = Mathf.Max(0f, stateDuration);
            SetWarningVisible(false);
            if (nextState != ChaserState.Chasing)
            {
                motor?.Stop();
            }
        }

        private void UpdateVisionVisibility()
        {
            if (playerVision == null ||
                bodyRenderer == null ||
                weaponRenderer == null)
            {
                return;
            }

            bool visible =
                playerVision.ContainsWorldPoint(bodyRenderer.bounds.center);
            bodyRenderer.enabled = visible;
            weaponRenderer.enabled = visible && !IsDisarmed;
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
            warningLine.SetPosition(
                0,
                transform.position + (Vector3.up * 0.45f));
            warningLine.SetPosition(
                1,
                perception.Target.position);
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
            Gizmos.color = new Color(1f, 0.55f, 0.05f, 0.4f);
            Gizmos.DrawWireSphere(transform.position, attackRange);
        }
    }
}
