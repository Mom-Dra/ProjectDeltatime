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
        private int stagedMeleeAttackCount;

        public bool CombatEnabled { get; private set; } = true;
        public WeaponController Weapon => weapon;

        private void Awake()
        {
            if (input == null ||
                aim == null ||
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

            if (input.FirePressed && TryUseEquippedWeapon())
            {
                worldTimeActivity.Pulse(fireActivity, fireActivityDuration);
            }

            if (input.ThrowPressed &&
                weapon.Throw(CombatFaction.Player, aim.AimDirection, worldTime))
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
                             aim.AimDirection,
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
            for (int i = 0; i < stagedMeleeAttackCount; i++)
            {
                weapon.CommitStagedMeleeAttack(
                    CombatFaction.Player,
                    gameObject,
                    stagedMeleeAttacks[i],
                    clock);
            }

            if (stagedMeleeAttackCount > 0)
            {
                worldTimeActivity.Pulse(
                    fireActivity,
                    fireActivityDuration);
            }

            ClearStagedMeleeAttacks();
        }

        private bool TryUseEquippedWeapon()
        {
            if (weapon.Definition == null)
            {
                return false;
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
                    aim.AimDirection,
                    clock,
                    worldTime);
        }

        private bool TryStageEquippedAttack()
        {
            if (weapon.Definition == null)
            {
                return false;
            }

            if (weapon.Definition.IsFirearm)
            {
                return weapon.TryStageFire(
                    CombatFaction.Player,
                    aim.AimDirection,
                    worldTime);
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

        private void ClearStagedMeleeAttacks()
        {
            stagedMeleeAttackCount = 0;
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
