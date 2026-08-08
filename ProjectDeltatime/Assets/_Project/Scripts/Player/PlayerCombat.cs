using Deltatime.Combat;
using Deltatime.Core;
using Deltatime.InputSystem;
using Deltatime.TimeSystem;
using UnityEngine;

namespace Deltatime.Player
{
    [DefaultExecutionOrder(-330)]
    public sealed class PlayerCombat : MonoBehaviour
    {
        [SerializeField] private PlayerInputReader input;
        [SerializeField] private PlayerAim aim;
        [SerializeField] private PlayerMovement movement;
        [SerializeField] private PlayerHealth health;
        [SerializeField] private WeaponController weapon;
        [SerializeField] private WorldTimeController worldTime;
        [SerializeField] private WorldTimeActivity worldTimeActivity;
        [SerializeField] private DeadlineController deadline;

        [Header("Interaction")]
        [SerializeField, Min(0.1f)] private float pickupRadius = 1.25f;
        [SerializeField, Min(0.1f)] private float airborneCatchRadius = 1.15f;
        [SerializeField, Min(0.01f)] private float catchInputBuffer = 0.18f;
        [SerializeField, Min(0.01f)] private float catchFreezeDuration = 0.2f;
        [SerializeField] private LayerMask pickupLayers = ~0;

        [Header("Unarmed Punch")]
        [SerializeField, Min(0.1f)] private float punchRange = 1.2f;
        [SerializeField, Range(1f, 90f)] private float punchHalfAngle = 35f;
        [SerializeField, Min(1)] private int punchDamage = 1;
        [SerializeField, Min(0.01f)] private float punchInterval = 0.6f;

        [Header("Activity Pulses")]
        [SerializeField, Range(0f, 1f)] private float fireActivity = 0.9f;
        [SerializeField, Min(0.01f)] private float fireActivityDuration = 0.16f;
        [SerializeField, Range(0f, 1f)] private float throwActivity = 1f;
        [SerializeField, Min(0.01f)] private float throwActivityDuration = 0.22f;

        private readonly Collider[] interactionResults = new Collider[24];
        private readonly WeaponController.StagedMeleeAttack[]
            stagedMeleeAttacks =
                new WeaponController.StagedMeleeAttack[2];
        private float catchBufferRemaining;
        private float nextPunchTime;
        private Vector3 stagedRecoilDisplacement;
        private int stagedMeleeAttackCount;
        private int stagedUnarmedPunchCount;

        public bool CombatEnabled { get; private set; } = true;
        public WeaponController Weapon => weapon;

        private void Awake()
        {
            if (movement == null)
            {
                movement = GetComponent<PlayerMovement>();
            }

            if (input == null ||
                aim == null ||
                movement == null ||
                health == null ||
                weapon == null ||
                worldTime == null ||
                worldTimeActivity == null ||
                deadline == null)
            {
                Debug.LogError($"{nameof(PlayerCombat)} is missing required references.", this);
                enabled = false;
            }
        }

        private void OnEnable()
        {
            if (deadline != null)
            {
                deadline.Released += HandleDeadlineReleased;
            }
        }

        private void Update()
        {
            if (!health.IsAlive)
            {
                catchBufferRemaining = 0f;
                ClearStagedMeleeAttacks();
                return;
            }

            if (!CombatEnabled)
            {
                catchBufferRemaining = 0f;
                ClearStagedMeleeAttacks();
                return;
            }

            if (deadline.ReleasedThisFrame)
            {
                catchBufferRemaining = 0f;
                return;
            }

            if (deadline.IsActive)
            {
                catchBufferRemaining = 0f;
                UpdateDeadlineActions();
                return;
            }

            if (!worldTime.IsHardFrozen)
            {
                UpdateWeaponInteraction();
            }
            else
            {
                catchBufferRemaining = 0f;
            }

            bool shouldUseWeapon = input.FirePressed ||
                (input.FireHeld &&
                 weapon.Definition != null &&
                 weapon.Definition.IsAutomatic);
            if (shouldUseWeapon)
            {
                bool weaponUseSucceeded = TryUseEquippedWeapon(
                    out bool firearmAttempted);
                if (weaponUseSucceeded &&
                    weapon.Definition != null &&
                    weapon.Definition.IsFirearm)
                {
                    movement.QueueRecoil(
                        -GetWeaponOriginAimDirection(),
                        weapon.Definition.PlayerRecoilDistance);
                }
                if (weaponUseSucceeded || firearmAttempted)
                {
                    worldTimeActivity.Pulse(
                        fireActivity,
                        fireActivityDuration);
                }
            }

            if (input.ThrowPressed &&
                weapon.Throw(
                    CombatFaction.Player,
                    GetWeaponOriginAimDirection(),
                    worldTime))
            {
                worldTimeActivity.Pulse(throwActivity, throwActivityDuration);
            }
        }

        public void SetCombatEnabled(bool value)
        {
            CombatEnabled = value;
            if (!value)
            {
                catchBufferRemaining = 0f;
                ClearStagedMeleeAttacks();
            }
        }

        public void Configure(
            PlayerInputReader inputReader,
            PlayerAim playerAim,
            PlayerHealth playerHealth,
            WeaponController weaponController,
            WorldTimeController timeSource,
            WorldTimeActivity activity,
            DeadlineController deadlineController)
        {
            input = inputReader;
            aim = playerAim;
            health = playerHealth;
            weapon = weaponController;
            worldTime = timeSource;
            worldTimeActivity = activity;
            deadline = deadlineController;
        }

        private void UpdateDeadlineActions()
        {
            if (input.FirePressed)
            {
                if (!deadline.CanStageAction)
                {
                    deadline.NotifyActionRejected();
                }
                else if (TryStageEquippedAttack())
                {
                    deadline.RegisterStagedAction();
                }
            }

            if (input.ThrowPressed)
            {
                if (!deadline.CanStageAction)
                {
                    deadline.NotifyActionRejected();
                }
                else if (weapon.Throw(
                             CombatFaction.Player,
                             GetWeaponOriginAimDirection(),
                             worldTime))
                {
                    deadline.RegisterStagedAction();
                }
            }
        }

        private void HandleDeadlineReleased()
        {
            if (weapon == null || deadline == null)
            {
                ClearStagedMeleeAttacks();
                return;
            }

            if (!deadline.ReleasedThisFrame)
            {
                weapon.CancelStagedUse();
                ClearStagedMeleeAttacks();
                return;
            }

            float clock = UnityEngine.Time.unscaledTime;
            weapon.CommitStagedFireCooldown(clock);
            if (stagedRecoilDisplacement.sqrMagnitude > 0.000001f)
            {
                movement.QueueRecoil(
                    stagedRecoilDisplacement,
                    stagedRecoilDisplacement.magnitude);
            }

            for (int i = 0; i < stagedMeleeAttackCount; i++)
            {
                weapon.CommitStagedMeleeAttack(
                    CombatFaction.Player,
                    gameObject,
                    stagedMeleeAttacks[i],
                    clock);
            }

            if (stagedUnarmedPunchCount > 0)
            {
                nextPunchTime = Mathf.Max(
                    nextPunchTime,
                    clock + punchInterval);
            }

            if (stagedMeleeAttackCount > 0)
            {
                worldTimeActivity.Pulse(
                    fireActivity,
                    fireActivityDuration);
            }

            ClearStagedMeleeAttacks();
        }

        private bool TryUseEquippedWeapon(out bool firearmAttempted)
        {
            firearmAttempted = false;
            if (weapon.Definition == null)
            {
                return TryUseUnarmedPunch();
            }

            float clock = UnityEngine.Time.unscaledTime;
            return weapon.Definition.IsMelee
                ? weapon.TryMeleeAttack(
                    CombatFaction.Player,
                    gameObject,
                    aim.AimDirection,
                    clock)
                : weapon.TryFire(
                    CombatFaction.Player,
                    GetWeaponOriginAimDirection(),
                    clock,
                    worldTime,
                    out firearmAttempted);
        }

        private bool TryStageEquippedAttack()
        {
            if (weapon.Definition == null)
            {
                return TryStageUnarmedPunch();
            }

            if (weapon.Definition.IsFirearm)
            {
                Vector3 fireDirection = GetWeaponOriginAimDirection();
                bool stagedFire = weapon.TryStageFire(
                    CombatFaction.Player,
                    fireDirection,
                    worldTime);
                if (stagedFire)
                {
                    stagedRecoilDisplacement +=
                        -fireDirection.normalized *
                        weapon.Definition.PlayerRecoilDistance;
                }

                return stagedFire;
            }

            if (stagedMeleeAttackCount >= stagedMeleeAttacks.Length ||
                !weapon.TryStageMeleeAttack(
                    aim.AimDirection,
                    out WeaponController.StagedMeleeAttack stagedAttack))
            {
                return false;
            }

            stagedMeleeAttacks[stagedMeleeAttackCount] = stagedAttack;
            stagedMeleeAttackCount++;
            return true;
        }

        private Vector3 GetWeaponOriginAimDirection()
        {
            if (weapon == null || weapon.Muzzle == null)
            {
                return aim.AimDirection;
            }

            return aim.GetPlanarDirectionFrom(weapon.Muzzle.position);
        }

        private void ClearStagedMeleeAttacks()
        {
            stagedMeleeAttackCount = 0;
            stagedUnarmedPunchCount = 0;
            stagedRecoilDisplacement = Vector3.zero;
        }

        private bool TryUseUnarmedPunch()
        {
            float clock = UnityEngine.Time.unscaledTime;
            Vector3 direction = aim.AimDirection;
            direction.y = 0f;
            if (clock < nextPunchTime ||
                direction.sqrMagnitude <= 0.000001f)
            {
                return false;
            }

            nextPunchTime = clock + punchInterval;
            MeleeAttackResolver.TryHitNearest(
                gameObject,
                CombatFaction.Player,
                direction,
                punchRange,
                punchHalfAngle,
                punchDamage);
            return true;
        }

        private bool TryStageUnarmedPunch()
        {
            if (stagedMeleeAttackCount >= stagedMeleeAttacks.Length)
            {
                return false;
            }

            Vector3 direction = aim.AimDirection;
            direction.y = 0f;
            if (direction.sqrMagnitude <= 0.000001f)
            {
                return false;
            }

            stagedMeleeAttacks[stagedMeleeAttackCount] =
                new WeaponController.StagedMeleeAttack(
                    direction.normalized,
                    punchRange,
                    punchHalfAngle,
                    punchDamage,
                    punchInterval);
            stagedMeleeAttackCount++;
            stagedUnarmedPunchCount++;
            return true;
        }

        private void UpdateWeaponInteraction()
        {
            if (input.InteractPressed)
            {
                if (TryCatchNearestAirborneWeapon() ||
                    TryCollectNearestGroundWeapon())
                {
                    catchBufferRemaining = 0f;
                    return;
                }

                catchBufferRemaining = catchInputBuffer;
                return;
            }

            if (catchBufferRemaining <= 0f)
            {
                return;
            }

            if (TryCatchNearestAirborneWeapon())
            {
                catchBufferRemaining = 0f;
                return;
            }

            catchBufferRemaining = Mathf.Max(
                0f,
                catchBufferRemaining - UnityEngine.Time.unscaledDeltaTime);
        }

        private bool TryCatchNearestAirborneWeapon()
        {
            int count = Physics.OverlapSphereNonAlloc(
                transform.position,
                airborneCatchRadius,
                interactionResults,
                pickupLayers,
                QueryTriggerInteraction.Collide);

            InterceptableWeapon nearest = null;
            float nearestDistanceSquared = float.PositiveInfinity;

            for (int i = 0; i < count; i++)
            {
                InterceptableWeapon candidate =
                    interactionResults[i].GetComponentInParent<InterceptableWeapon>();
                if (candidate == null || !candidate.IsCatchable)
                {
                    continue;
                }

                float distanceSquared =
                    (candidate.transform.position - transform.position).sqrMagnitude;
                if (distanceSquared < nearestDistanceSquared)
                {
                    nearestDistanceSquared = distanceSquared;
                    nearest = candidate;
                }
            }

            if (nearest == null || !nearest.TryCatch(weapon))
            {
                return false;
            }

            worldTime.RequestHardFreeze(catchFreezeDuration);
            return true;
        }

        private bool TryCollectNearestGroundWeapon()
        {
            int count = Physics.OverlapSphereNonAlloc(
                transform.position,
                pickupRadius,
                interactionResults,
                pickupLayers,
                QueryTriggerInteraction.Collide);

            WeaponPickup nearest = null;
            float nearestDistanceSquared = float.PositiveInfinity;

            for (int i = 0; i < count; i++)
            {
                WeaponPickup pickup =
                    interactionResults[i].GetComponentInParent<WeaponPickup>();
                if (pickup == null || pickup.Definition == null)
                {
                    continue;
                }

                float distanceSquared =
                    (pickup.transform.position - transform.position).sqrMagnitude;
                if (distanceSquared < nearestDistanceSquared)
                {
                    nearestDistanceSquared = distanceSquared;
                    nearest = pickup;
                }
            }

            if (nearest != null)
            {
                return nearest.TryTake(weapon);
            }

            return false;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0.8f, 0.15f, 0.6f);
            Gizmos.DrawWireSphere(transform.position, pickupRadius);
            Gizmos.color = new Color(0.2f, 1f, 1f, 0.6f);
            Gizmos.DrawWireSphere(transform.position, airborneCatchRadius);
        }

        private void OnDisable()
        {
            ClearStagedMeleeAttacks();
            if (deadline != null)
            {
                deadline.Released -= HandleDeadlineReleased;
            }
        }
    }
}
