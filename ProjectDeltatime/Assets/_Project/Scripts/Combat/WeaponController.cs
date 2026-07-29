using Deltatime.Core;
using Deltatime.TimeSystem;
using UnityEngine;

namespace Deltatime.Combat
{
    public sealed class WeaponController : MonoBehaviour
    {
        [SerializeField] private WeaponDefinition startingDefinition;
        [SerializeField] private Transform muzzle;
        [SerializeField] private Renderer heldWeaponRenderer;
        [SerializeField] private Projectile projectilePrefab;
        [SerializeField] private WeaponPickup pickupPrefab;
        [SerializeField] private ThrownWeapon thrownWeaponPrefab;

        private float nextFireTime;

        public WeaponDefinition Definition { get; private set; }
        public int Ammunition { get; private set; }
        public bool HasWeapon => Definition != null;
        public Transform Muzzle => muzzle;

        private void Awake()
        {
            ValidateConfiguration();
            if (startingDefinition != null && Definition == null)
            {
                Equip(startingDefinition, startingDefinition.AmmunitionCapacity);
            }
            else
            {
                RefreshVisual();
            }
        }

        public bool TryFire(
            CombatFaction faction,
            Vector3 direction,
            float clock,
            WorldTimeController worldTime)
        {
            if (Definition == null ||
                Ammunition <= 0 ||
                clock < nextFireTime ||
                projectilePrefab == null ||
                muzzle == null ||
                worldTime == null)
            {
                return false;
            }

            Ammunition--;
            nextFireTime = clock + Definition.FireInterval;

            Projectile projectile = Instantiate(
                projectilePrefab,
                muzzle.position,
                Quaternion.identity);
            projectile.Initialize(
                worldTime,
                faction,
                gameObject,
                direction,
                Definition.ProjectileSpeed,
                Definition.Damage,
                Definition.ProjectileRadius);
            return true;
        }

        public bool Throw(
            CombatFaction faction,
            Vector3 direction,
            WorldTimeController worldTime)
        {
            if (Definition == null ||
                thrownWeaponPrefab == null ||
                pickupPrefab == null ||
                muzzle == null ||
                worldTime == null)
            {
                return false;
            }

            WeaponDefinition thrownDefinition = Definition;
            int thrownAmmunition = Ammunition;
            Clear();

            ThrownWeapon thrown = Instantiate(
                thrownWeaponPrefab,
                muzzle.position,
                Quaternion.identity);
            thrown.Initialize(
                worldTime,
                pickupPrefab,
                thrownDefinition,
                thrownAmmunition,
                faction,
                gameObject,
                direction);
            return true;
        }

        public void Equip(WeaponDefinition definition, int ammunition)
        {
            Definition = definition;
            Ammunition = definition == null
                ? 0
                : Mathf.Clamp(ammunition, 0, definition.AmmunitionCapacity);
            nextFireTime = 0f;
            RefreshVisual();
        }

        public void Clear()
        {
            Definition = null;
            Ammunition = 0;
            nextFireTime = 0f;
            RefreshVisual();
        }

        public void Configure(
            Transform muzzleTransform,
            Renderer weaponRenderer,
            Projectile projectileTemplate,
            WeaponPickup pickupTemplate,
            ThrownWeapon thrownTemplate,
            WeaponDefinition initialDefinition)
        {
            muzzle = muzzleTransform;
            heldWeaponRenderer = weaponRenderer;
            projectilePrefab = projectileTemplate;
            pickupPrefab = pickupTemplate;
            thrownWeaponPrefab = thrownTemplate;
            startingDefinition = initialDefinition;
        }

        private void RefreshVisual()
        {
            if (heldWeaponRenderer != null)
            {
                heldWeaponRenderer.enabled = Definition != null;
            }
        }

        private void ValidateConfiguration()
        {
            if (muzzle == null || projectilePrefab == null)
            {
                Debug.LogError(
                    $"{nameof(WeaponController)} requires a muzzle and projectile prefab.",
                    this);
                enabled = false;
            }
        }
    }
}
