using System;
using Deltatime.Combat;
using Deltatime.Core;
using Deltatime.TimeSystem;
using Deltatime.Vision;
using Deltatime.Visuals;
using UnityEngine;

namespace Deltatime.Enemies
{
    [RequireComponent(typeof(EnemyMotor))]
    [RequireComponent(typeof(EnemyPerception))]
    [RequireComponent(typeof(WeaponController))]
    public abstract class EnemyCombatant : EnemyBehavior
    {
        private const string CombatIdentityRingName = "Combat Identity Ring";

        public enum CombatState
        {
            Detecting,
            Pursuing,
            SeekingWeapon,
            Aiming,
            BurstFiring,
            AttackWindup,
            Attacking,
            Cooldown,
            Stunned,
            Dead
        }

        public enum MovementMode
        {
            Stopped,
            Pursuing,
            Holding,
            Retreating,
            SeekingWeapon
        }

        private enum EquipmentMode
        {
            Unarmed,
            Firearm,
            Melee
        }

        [SerializeField] private WorldTimeController worldTime;
        [SerializeField] private EnemyPerception perception;
        [SerializeField] private EnemyMotor motor;
        [SerializeField] private WeaponController weapon;
        [SerializeField] private MeleeAttackExecution meleeAttackExecution;
        [SerializeField] private EnemyWeaponDrop weaponDrop;
        [SerializeField] private LineRenderer warningLine;
        [SerializeField] private VisionCone playerVision;
        [SerializeField] private Renderer bodyRenderer;
        [SerializeField] private Renderer weaponRenderer;
        [SerializeField] private CharacterVisualController characterVisual;

        [Header("Firearm Engagement")]
        [SerializeField, Min(0.1f)] private float preferredMinimumRange = 6f;
        [SerializeField, Min(0.1f)] private float preferredMaximumRange = 9f;
        [SerializeField, Min(0.1f)] private float retreatStepDistance = 3f;
        [SerializeField, Range(0f, 1f)]
        private float retreatMoveSpeedMultiplier = 0.7f;
        [SerializeField, Min(0.01f)] private float aimDuration = 0.65f;
        [SerializeField, Min(0.01f)] private float firearmCooldownDuration = 1.15f;
        [SerializeField, Range(0f, 45f)]
        private float firearmFacingToleranceDegrees = 6f;

        [Header("Armed Melee")]
        [SerializeField, Min(0.01f)] private float meleeWindupDuration = 0.42f;
        [SerializeField, Min(0.1f)] private float meleeCancelRange = 1.9f;
        [SerializeField, Range(0f, 1f)]
        private float windupMoveSpeedMultiplier = 0.35f;

        [Header("Unarmed Punch")]
        [SerializeField, Min(0.1f)] private float punchCommitDistance = 3f;
        [SerializeField, Min(0.1f)] private float punchRange = 1.2f;
        [SerializeField, Min(0.1f)] private float punchCancelRange = 1.65f;
        [SerializeField, Min(0.01f)] private float punchWindupDuration = 0.35f;
        [SerializeField, Min(0.01f)] private float punchCooldownDuration = 0.6f;
        [SerializeField, Min(1)] private int punchDamage = 1;
        [SerializeField, Range(1f, 90f)] private float punchHalfAngle = 35f;

        [Header("Weapon Search")]
        [SerializeField, Min(0.1f)] private float weaponSearchRadius = 8f;
        [SerializeField, Min(0.01f)] private float weaponSearchInterval = 0.25f;
        [SerializeField, Min(0.1f)] private float weaponPickupDistance = 1.1f;
        [SerializeField, Min(0f)] private float firearmPathTolerance = 2f;
        [SerializeField] private LayerMask weaponPickupLayers = ~0;

        private readonly Collider[] weaponSearchHits = new Collider[32];
        private float stateTimeRemaining;
        private int burstShotsRemaining;
        private float nextWeaponSearchWorldTime;
        private float pendingAttackRange;
        private float pendingAttackHalfAngle;
        private float pendingAttackCancelRange;
        private float pendingAttackCooldown;
        private int pendingAttackDamage;
        private MeleeImpactKind pendingAttackImpactKind;
        private EquipmentMode currentEquipmentMode;
        private bool equipmentModeInitialized;
        private WeaponPickup weaponTarget;
        private Renderer combatIdentityRingRenderer;

        public CombatState CurrentState { get; private set; } =
            CombatState.Detecting;
        public MovementMode CurrentMovementMode { get; private set; } =
            MovementMode.Stopped;
        public WeaponPickup CurrentWeaponTarget => weaponTarget;
        public event Action CloseAttackPerformed;

        protected virtual void Awake()
        {
            if (meleeAttackExecution == null)
            {
                meleeAttackExecution = GetComponent<MeleeAttackExecution>();
                if (meleeAttackExecution == null)
                {
                    meleeAttackExecution = gameObject.AddComponent<
                        MeleeAttackExecution>();
                }
            }

            if (worldTime == null ||
                perception == null ||
                motor == null ||
                weapon == null ||
                weaponDrop == null ||
                warningLine == null ||
                playerVision == null ||
                bodyRenderer == null ||
                weaponRenderer == null)
            {
                Debug.LogError(
                    $"{nameof(EnemyCombatant)} is missing required references.",
                    this);
                enabled = false;
                return;
            }

            Transform combatIdentityRing =
                transform.Find(CombatIdentityRingName);
            combatIdentityRingRenderer = combatIdentityRing == null
                ? null
                : combatIdentityRing.GetComponent<Renderer>();

            weapon.EquipmentChanged += HandleEquipmentChanged;
            if (!weapon.HasUsableWeapon)
            {
                Disarm();
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
                meleeAttackExecution?.CancelPendingAttacks();
                StopMovement();
                SetWarningVisible(false);
                return;
            }

            float deltaTime = worldTime.WorldDeltaTime;
            if (!AdvanceStatus(deltaTime))
            {
                CurrentState = IsStunned
                    ? CombatState.Stunned
                    : CombatState.Dead;
                StopMovement();
                SetWarningVisible(false);
                return;
            }

            if (!perception.HasLivingTarget)
            {
                TransitionTo(CombatState.Detecting, 0f);
                StopMovement();
                ReleaseWeaponTarget();
                return;
            }

            if (weapon.Definition != null &&
                weapon.Definition.IsFirearm &&
                weapon.Ammunition <= 0)
            {
                weaponDrop.DropGround();
            }

            EquipmentMode equipmentMode = ResolveEquipmentMode();
            if (!equipmentModeInitialized ||
                currentEquipmentMode != equipmentMode)
            {
                currentEquipmentMode = equipmentMode;
                equipmentModeInitialized = true;
                TransitionTo(CombatState.Detecting, 0f);
                if (equipmentMode != EquipmentMode.Unarmed)
                {
                    ReleaseWeaponTarget();
                }
            }

            bool canSeeTarget = perception.CanSeeTarget();
            switch (equipmentMode)
            {
                case EquipmentMode.Firearm:
                    UpdateFirearm(canSeeTarget, deltaTime);
                    break;

                case EquipmentMode.Melee:
                    UpdateCloseCombat(canSeeTarget, deltaTime, false);
                    break;

                default:
                    UpdateUnarmed(canSeeTarget, deltaTime);
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
            EnemyWeaponDrop dropController,
            LineRenderer telegraphLine,
            VisionCone vision,
            Renderer enemyBodyRenderer,
            Renderer heldWeaponRenderer,
            LayerMask pickupLayers)
        {
            worldTime = timeSource;
            perception = enemyPerception;
            motor = enemyMotor;
            weapon = weaponController;
            weaponDrop = dropController;
            warningLine = telegraphLine;
            playerVision = vision;
            bodyRenderer = enemyBodyRenderer;
            weaponRenderer = heldWeaponRenderer;
            weaponPickupLayers = pickupLayers;
        }

        public void ConfigureVisual(CharacterVisualController visualController)
        {
            characterVisual = visualController;
        }

        public bool TryGetReplayVisibility(
            Renderer targetRenderer,
            out bool visible)
        {
            if (targetRenderer == bodyRenderer)
            {
                visible = !IsDead;
                return true;
            }

            if (targetRenderer == weaponRenderer)
            {
                visible = !IsDead && weapon != null && weapon.HasWeapon;
                return true;
            }

            if (targetRenderer == combatIdentityRingRenderer)
            {
                visible = !IsDead;
                return true;
            }

            if (characterVisual != null &&
                characterVisual.ContainsRenderer(targetRenderer))
            {
                visible = !IsDead;
                return true;
            }

            visible = false;
            return false;
        }

        protected override void OnStunned()
        {
            ReleaseWeaponTarget();
            motor?.ClearPath();
            TransitionTo(CombatState.Stunned, 0f);
        }

        protected override void OnStunRecovered()
        {
            TransitionTo(CombatState.Detecting, 0f);
            StopMovement();
        }

        protected override void OnDisarmed()
        {
            equipmentModeInitialized = false;
            if (!IsStunned)
            {
                TransitionTo(CombatState.Detecting, 0f);
            }
        }

        protected override void OnRearmed()
        {
            equipmentModeInitialized = false;
            ReleaseWeaponTarget();
            if (!IsStunned)
            {
                TransitionTo(CombatState.Detecting, 0f);
            }
        }

        protected override void OnDead()
        {
            ReleaseWeaponTarget();
            motor?.ClearPath();
            TransitionTo(CombatState.Dead, 0f);
        }

        private EquipmentMode ResolveEquipmentMode()
        {
            if (weapon.Definition == null || !weapon.HasUsableWeapon)
            {
                return EquipmentMode.Unarmed;
            }

            return weapon.Definition.IsFirearm
                ? EquipmentMode.Firearm
                : EquipmentMode.Melee;
        }

        private void UpdateFirearm(
            bool canSeeTarget,
            float deltaTime)
        {
            UpdateFirearmMovement(canSeeTarget, deltaTime);

            if (!canSeeTarget ||
                perception.PlanarDistanceToTarget >
                preferredMaximumRange)
            {
                if (CurrentState != CombatState.Detecting &&
                    CurrentState != CombatState.Pursuing)
                {
                    TransitionTo(CombatState.Detecting, 0f);
                }

                return;
            }

            switch (CurrentState)
            {
                case CombatState.Aiming:
                    UpdateFirearmAim(deltaTime);
                    break;

                case CombatState.BurstFiring:
                    UpdateFirearmBurst();
                    break;

                case CombatState.Cooldown:
                    UpdateCooldown(deltaTime, false);
                    break;

                default:
                    TransitionTo(CombatState.Aiming, aimDuration);
                    break;
            }
        }

        private void UpdateFirearmMovement(
            bool canSeeTarget,
            float deltaTime)
        {
            if (canSeeTarget)
            {
                float distance = perception.PlanarDistanceToTarget;
                if (distance > preferredMaximumRange)
                {
                    CurrentMovementMode = MovementMode.Pursuing;
                    CurrentState = CombatState.Pursuing;
                    motor.MoveTowards(
                        perception.Target.position,
                        preferredMaximumRange * 0.9f,
                        deltaTime);
                    return;
                }

                if (distance < preferredMinimumRange)
                {
                    CurrentMovementMode = MovementMode.Retreating;
                    Vector3 away =
                        -perception.PlanarDirectionToTarget;
                    motor.MoveTowards(
                        transform.position +
                        (away * retreatStepDistance),
                        0.15f,
                        deltaTime,
                        retreatMoveSpeedMultiplier,
                        false);
                    motor.RotateTowards(
                        perception.Target.position,
                        deltaTime);
                    return;
                }

                CurrentMovementMode = MovementMode.Holding;
                motor.Stop();
                motor.RotateTowards(
                    perception.Target.position,
                    deltaTime);
                return;
            }

            MoveToLastKnownTarget(deltaTime);
        }

        private void UpdateFirearmAim(float deltaTime)
        {
            bool isFacingTarget = IsFacingTarget(
                firearmFacingToleranceDegrees);
            SetWarningVisible(isFacingTarget);
            if (!isFacingTarget)
            {
                return;
            }

            UpdateWarningLine(weapon.Muzzle.position);
            stateTimeRemaining -= deltaTime;
            if (stateTimeRemaining <= 0f)
            {
                burstShotsRemaining = Mathf.Max(
                    1,
                    weapon.Definition.EnemyBurstShotCount);
                TransitionTo(CombatState.BurstFiring, 0f);
            }
        }

        private void UpdateFirearmBurst()
        {
            bool isFacingTarget = IsFacingTarget(
                firearmFacingToleranceDegrees);
            SetWarningVisible(isFacingTarget);
            if (!isFacingTarget)
            {
                return;
            }

            UpdateWarningLine(weapon.Muzzle.position);
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
                    CombatState.Cooldown,
                    firearmCooldownDuration);
            }
        }

        private void UpdateUnarmed(
            bool canSeeTarget,
            float deltaTime)
        {
            if (canSeeTarget &&
                perception.PlanarDistanceToTarget <=
                punchCommitDistance)
            {
                ReleaseWeaponTarget();
                UpdateCloseCombat(true, deltaTime, true);
                return;
            }

            EnsureWeaponTarget();
            if (weaponTarget != null)
            {
                UpdateWeaponSeeking(deltaTime);
                return;
            }

            UpdateCloseCombat(canSeeTarget, deltaTime, true);
        }

        private void UpdateCloseCombat(
            bool canSeeTarget,
            float deltaTime,
            bool punch)
        {
            switch (CurrentState)
            {
                case CombatState.AttackWindup:
                    UpdateAttackWindup(canSeeTarget, deltaTime);
                    break;

                case CombatState.Attacking:
                    PerformCloseAttack();
                    TransitionTo(
                        CombatState.Cooldown,
                        pendingAttackCooldown);
                    break;

                case CombatState.Cooldown:
                    UpdateCooldown(deltaTime, true);
                    break;

                default:
                    UpdateCloseChase(canSeeTarget, deltaTime, punch);
                    break;
            }
        }

        private void UpdateCloseChase(
            bool canSeeTarget,
            float deltaTime,
            bool punch)
        {
            float attackRange = punch
                ? punchRange
                : weapon.Definition.MeleeRange;
            Vector3 destination;

            if (canSeeTarget)
            {
                if (perception.PlanarDistanceToTarget <= attackRange)
                {
                    BeginCloseAttack(punch);
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
                TransitionTo(CombatState.Detecting, 0f);
                StopMovement();
                return;
            }

            CurrentState = CombatState.Pursuing;
            CurrentMovementMode = MovementMode.Pursuing;
            bool arrived = motor.MoveTowards(
                destination,
                canSeeTarget ? attackRange * 0.8f : 0.15f,
                deltaTime);
            if (arrived && !canSeeTarget)
            {
                TransitionTo(CombatState.Detecting, 0f);
                StopMovement();
            }
        }

        private void BeginCloseAttack(bool punch)
        {
            pendingAttackRange = punch
                ? punchRange
                : weapon.Definition.MeleeRange;
            pendingAttackHalfAngle = punch
                ? punchHalfAngle
                : weapon.Definition.MeleeHalfAngle;
            pendingAttackCancelRange = punch
                ? punchCancelRange
                : meleeCancelRange;
            pendingAttackCooldown = punch
                ? punchCooldownDuration
                : weapon.Definition.UseInterval;
            pendingAttackDamage = punch
                ? punchDamage
                : weapon.Definition.Damage;
            pendingAttackImpactKind = punch
                ? MeleeImpactKind.Punch
                : MeleeImpactKind.Bat;
            TransitionTo(
                CombatState.AttackWindup,
                punch ? punchWindupDuration : meleeWindupDuration);
        }

        private void UpdateAttackWindup(
            bool canSeeTarget,
            float deltaTime)
        {
            if (!canSeeTarget ||
                perception.PlanarDistanceToTarget >
                pendingAttackCancelRange)
            {
                TransitionTo(CombatState.Detecting, 0f);
                return;
            }

            CurrentMovementMode = MovementMode.Pursuing;
            motor.MoveTowards(
                perception.Target.position,
                pendingAttackRange * 0.55f,
                deltaTime,
                windupMoveSpeedMultiplier,
                false);
            motor.RotateTowards(
                perception.Target.position,
                deltaTime);
            SetWarningVisible(true);
            UpdateWarningLine(
                transform.position +
                (Vector3.up * 0.45f));

            stateTimeRemaining -= deltaTime;
            if (stateTimeRemaining <= 0f)
            {
                TransitionTo(CombatState.Attacking, 0f);
            }
        }

        private void PerformCloseAttack()
        {
            if (!perception.HasLivingTarget ||
                perception.PlanarDistanceToTarget > pendingAttackRange)
            {
                return;
            }

            CloseAttackPerformed?.Invoke();
            if (meleeAttackExecution != null)
            {
                meleeAttackExecution.BeginAttack(
                    gameObject,
                    CombatFaction.Enemy,
                    transform.forward,
                    pendingAttackRange,
                    pendingAttackHalfAngle,
                    pendingAttackDamage,
                    pendingAttackImpactKind);
                return;
            }

            MeleeAttackResolver.TryHitNearest(
                gameObject,
                CombatFaction.Enemy,
                transform.forward,
                pendingAttackRange,
                pendingAttackHalfAngle,
                pendingAttackDamage,
                pendingAttackImpactKind);
        }

        private void UpdateCooldown(
            float deltaTime,
            bool stopMovement)
        {
            if (stopMovement)
            {
                StopMovement();
            }

            if (perception.Target != null)
            {
                motor.RotateTowards(
                    perception.Target.position,
                    deltaTime);
            }

            stateTimeRemaining -= deltaTime;
            if (stateTimeRemaining <= 0f)
            {
                TransitionTo(CombatState.Detecting, 0f);
            }
        }

        private void EnsureWeaponTarget()
        {
            if (IsWeaponTargetValid())
            {
                return;
            }

            ReleaseWeaponTarget();
            if (worldTime.WorldElapsedTime < nextWeaponSearchWorldTime)
            {
                return;
            }

            nextWeaponSearchWorldTime =
                worldTime.WorldElapsedTime + weaponSearchInterval;
            FindBestWeaponTarget();
        }

        private void FindBestWeaponTarget()
        {
            int count = Physics.OverlapSphereNonAlloc(
                transform.position,
                weaponSearchRadius,
                weaponSearchHits,
                weaponPickupLayers,
                QueryTriggerInteraction.Collide);
            WeaponPickup bestFirearm = null;
            WeaponPickup bestMelee = null;
            float bestFirearmPath = float.PositiveInfinity;
            float bestMeleePath = float.PositiveInfinity;

            for (int i = 0; i < count; i++)
            {
                WeaponPickup candidate = weaponSearchHits[i] == null
                    ? null
                    : weaponSearchHits[i]
                        .GetComponentInParent<WeaponPickup>();
                if (candidate == null ||
                    candidate.Definition == null ||
                    !candidate.IsAvailableTo(this) ||
                    (candidate.Definition.IsFirearm &&
                     candidate.Ammunition <= 0) ||
                    !motor.TryCalculatePathLength(
                        candidate.transform.position,
                        out float pathLength))
                {
                    continue;
                }

                if (candidate.Definition.IsFirearm)
                {
                    if (pathLength < bestFirearmPath)
                    {
                        bestFirearm = candidate;
                        bestFirearmPath = pathLength;
                    }
                }
                else if (pathLength < bestMeleePath)
                {
                    bestMelee = candidate;
                    bestMeleePath = pathLength;
                }
            }

            WeaponPickup selected = bestFirearm;
            if (bestMelee != null &&
                (bestFirearm == null ||
                 bestFirearmPath >=
                 bestMeleePath + firearmPathTolerance))
            {
                selected = bestMelee;
            }

            if (selected != null && selected.TryReserve(this))
            {
                weaponTarget = selected;
            }
        }

        private void UpdateWeaponSeeking(float deltaTime)
        {
            if (!IsWeaponTargetValid())
            {
                ReleaseWeaponTarget();
                return;
            }

            if (worldTime.WorldElapsedTime >= nextWeaponSearchWorldTime)
            {
                nextWeaponSearchWorldTime =
                    worldTime.WorldElapsedTime + weaponSearchInterval;
                if (!motor.TryCalculatePathLength(
                        weaponTarget.transform.position,
                        out _))
                {
                    ReleaseWeaponTarget();
                    StopMovement();
                    return;
                }
            }

            CurrentState = CombatState.SeekingWeapon;
            CurrentMovementMode = MovementMode.SeekingWeapon;
            SetWarningVisible(false);

            Vector3 offset = weaponTarget.transform.position -
                             transform.position;
            offset.y = 0f;
            if (offset.magnitude <= weaponPickupDistance)
            {
                WeaponPickup pickup = weaponTarget;
                if (pickup.TryTake(weapon, this))
                {
                    weaponTarget = null;
                    TransitionTo(CombatState.Detecting, 0f);
                    return;
                }


                if (!IsWeaponTargetValid())
                {
                    ReleaseWeaponTarget();
                    StopMovement();
                    return;
                }
            }

            motor.MoveTowards(
                weaponTarget.transform.position,
                weaponPickupDistance * 0.8f,
                deltaTime);
        }

        private bool IsWeaponTargetValid()
        {
            if (weaponTarget == null ||
                weaponTarget.Definition == null ||
                weaponTarget.ReservationOwner != this ||
                (weaponTarget.Definition.IsFirearm &&
                 weaponTarget.Ammunition <= 0))
            {
                return false;
            }

            Vector3 offset =
                weaponTarget.transform.position -
                transform.position;
            offset.y = 0f;
            return offset.sqrMagnitude <=
                   weaponSearchRadius * weaponSearchRadius;
        }

        private void ReleaseWeaponTarget()
        {
            if (weaponTarget != null)
            {
                weaponTarget.ReleaseReservation(this);
                weaponTarget = null;
            }
        }

        private void MoveToLastKnownTarget(float deltaTime)
        {
            if (!perception.HasLastKnownTargetPosition)
            {
                StopMovement();
                return;
            }

            CurrentState = CombatState.Pursuing;
            CurrentMovementMode = MovementMode.Pursuing;
            bool arrived = motor.MoveTowards(
                perception.LastKnownTargetPosition,
                0.2f,
                deltaTime);
            if (arrived)
            {
                StopMovement();
                CurrentState = CombatState.Detecting;
            }
        }

        private bool IsFacingTarget(float toleranceDegrees)
        {
            Vector3 direction = perception.PlanarDirectionToTarget;
            return direction.sqrMagnitude > 0.000001f &&
                   Vector3.Angle(transform.forward, direction) <=
                   toleranceDegrees;
        }

        private void HandleEquipmentChanged()
        {
            equipmentModeInitialized = false;
            if (weapon.HasUsableWeapon)
            {
                Rearm();
                ReleaseWeaponTarget();
            }
            else
            {
                Disarm();
            }
        }

        private void StopMovement()
        {
            CurrentMovementMode = MovementMode.Stopped;
            motor?.Stop();
        }

        private void TransitionTo(
            CombatState nextState,
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
            characterVisual?.SetVisible(visible && !IsDead);
            weaponRenderer.enabled = visible &&
                                     weapon.HasWeapon &&
                                     !weapon.CustomHeldVisualActive;
            if (!playerVision.HasUnlimitedVision &&
                combatIdentityRingRenderer != null)
            {
                combatIdentityRingRenderer.enabled = visible && !IsDead;
            }

            if (!visible)
            {
                SetWarningVisible(false);
            }
        }

        private void UpdateWarningLine(Vector3 origin)
        {
            if (warningLine == null ||
                !warningLine.enabled ||
                perception.Target == null)
            {
                return;
            }

            warningLine.positionCount = 2;
            warningLine.SetPosition(0, origin);
            warningLine.SetPosition(1, perception.Target.position);
        }

        private void SetWarningVisible(bool visible)
        {
            if (warningLine != null)
            {
                warningLine.enabled = visible;
            }
        }

        private void OnDestroy()
        {
            ReleaseWeaponTarget();
            if (weapon != null)
            {
                weapon.EquipmentChanged -= HandleEquipmentChanged;
            }
        }
    }
}
