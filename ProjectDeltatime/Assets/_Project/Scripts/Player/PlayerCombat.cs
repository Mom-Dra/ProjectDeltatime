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

        [Header("Interaction")]
        [SerializeField, Min(0.1f)] private float pickupRadius = 1.25f;
        [SerializeField] private LayerMask pickupLayers = ~0;

        [Header("Activity Pulses")]
        [SerializeField, Range(0f, 1f)] private float fireActivity = 0.9f;
        [SerializeField, Min(0.01f)] private float fireActivityDuration = 0.16f;
        [SerializeField, Range(0f, 1f)] private float throwActivity = 1f;
        [SerializeField, Min(0.01f)] private float throwActivityDuration = 0.22f;

        private readonly Collider[] pickupResults = new Collider[16];

        public bool CombatEnabled { get; private set; } = true;
        public WeaponController Weapon => weapon;

        private void Awake()
        {
            if (input == null ||
                aim == null ||
                health == null ||
                weapon == null ||
                worldTime == null ||
                worldTimeActivity == null)
            {
                Debug.LogError($"{nameof(PlayerCombat)} is missing required references.", this);
                enabled = false;
            }
        }

        private void Update()
        {
            if (!health.IsAlive)
            {
                return;
            }

            if (input.InteractPressed)
            {
                TryCollectNearestWeapon();
            }

            if (!CombatEnabled)
            {
                return;
            }

            if (input.FirePressed &&
                weapon.TryFire(
                    CombatFaction.Player,
                    aim.AimDirection,
                    UnityEngine.Time.unscaledTime,
                    worldTime))
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
        }

        public void Configure(
            PlayerInputReader inputReader,
            PlayerAim playerAim,
            PlayerHealth playerHealth,
            WeaponController weaponController,
            WorldTimeController timeSource,
            WorldTimeActivity activity)
        {
            input = inputReader;
            aim = playerAim;
            health = playerHealth;
            weapon = weaponController;
            worldTime = timeSource;
            worldTimeActivity = activity;
        }

        private void TryCollectNearestWeapon()
        {
            int count = Physics.OverlapSphereNonAlloc(
                transform.position,
                pickupRadius,
                pickupResults,
                pickupLayers,
                QueryTriggerInteraction.Collide);

            WeaponPickup nearest = null;
            float nearestDistanceSquared = float.PositiveInfinity;

            for (int i = 0; i < count; i++)
            {
                WeaponPickup pickup = pickupResults[i].GetComponent<WeaponPickup>();
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
                nearest.TryTake(weapon);
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0.8f, 0.15f, 0.6f);
            Gizmos.DrawWireSphere(transform.position, pickupRadius);
        }
    }
}
