using System;
using Deltatime.Core;
using Deltatime.TimeSystem;
using UnityEngine;

namespace Deltatime.Combat
{
    public sealed class WeaponController : MonoBehaviour
    {
        public readonly struct StagedMeleeAttack
        {
            public StagedMeleeAttack(
                Vector3 direction,
                float range,
                float halfAngle,
                int damage,
                float interval)
            {
                Direction = direction;
                Range = range;
                HalfAngle = halfAngle;
                Damage = damage;
                Interval = interval;
            }

            public Vector3 Direction { get; }
            public float Range { get; }
            public float HalfAngle { get; }
            public int Damage { get; }
            public float Interval { get; }
        }

        [SerializeField] private WeaponDefinition startingDefinition;
        [SerializeField] private Transform muzzle;
        [SerializeField] private Renderer heldWeaponRenderer;
        [SerializeField] private Projectile projectilePrefab;
        [SerializeField] private WeaponPickup pickupPrefab;
        [SerializeField] private ThrownWeapon thrownWeaponPrefab;

        private float nextUseTime;
        private bool hasStagedFire;

        public event Action EquipmentChanged;

        public WeaponDefinition Definition { get; private set; }
        public int Ammunition { get; private set; }
        public bool HasWeapon => Definition != null;
        public bool HasUsableWeapon =>
            Definition != null &&
            (Definition.IsMelee || Ammunition > 0);
        public WeaponDefinition StartingDefinition => startingDefinition;
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
                !Definition.IsFirearm ||
                Ammunition <= 0 ||
                clock < nextUseTime ||
                projectilePrefab == null ||
                muzzle == null ||
                worldTime == null)
            {
                return false;
            }

            Ammunition--;
            nextUseTime = clock + Definition.UseInterval;
            SpawnProjectile(faction, direction, worldTime);
            return true;
        }

        public bool TryMeleeAttack(
            CombatFaction faction,
            GameObject source,
            Vector3 direction,
            float clock)
        {
            if (Definition == null ||
                !Definition.IsMelee ||
                source == null ||
                clock < nextUseTime)
            {
                return false;
            }

            nextUseTime = clock + Definition.UseInterval;
            MeleeAttackResolver.TryHitNearest(
                source,
                faction,
                direction,
                Definition.MeleeRange,
                Definition.MeleeHalfAngle,
                Definition.Damage);
            return true;
        }

        public bool TryStageFire(
            CombatFaction faction,
            Vector3 direction,
            WorldTimeController worldTime)
        {
            if (Definition == null ||
                !Definition.IsFirearm ||
                Ammunition <= 0 ||
                projectilePrefab == null ||
                muzzle == null ||
                worldTime == null)
            {
                return false;
            }

            Ammunition--;
            hasStagedFire = true;
            SpawnProjectile(faction, direction, worldTime);
            return true;
        }

        public bool TryStageMeleeAttack(
            Vector3 direction,
            out StagedMeleeAttack stagedAttack)
        {
            stagedAttack = default;
            if (Definition == null || !Definition.IsMelee)
            {
                return false;
            }

            Vector3 attackDirection = direction;
            attackDirection.y = 0f;
            if (attackDirection.sqrMagnitude <= 0.000001f)
            {
                return false;
            }

            stagedAttack = new StagedMeleeAttack(
                attackDirection.normalized,
                Definition.MeleeRange,
                Definition.MeleeHalfAngle,
                Definition.Damage,
                Definition.UseInterval);
            return true;
        }

        public void CommitStagedMeleeAttack(
            CombatFaction faction,
            GameObject source,
            StagedMeleeAttack stagedAttack,
            float clock)
        {
            if (source == null || stagedAttack.Damage <= 0)
            {
                return;
            }

            MeleeAttackResolver.TryHitNearest(
                source,
                faction,
                stagedAttack.Direction,
                stagedAttack.Range,
                stagedAttack.HalfAngle,
                stagedAttack.Damage);
            nextUseTime = Mathf.Max(
                nextUseTime,
                clock + stagedAttack.Interval);
        }

        public void CommitStagedFireCooldown(float clock)
        {
            if (!hasStagedFire)
            {
                return;
            }

            hasStagedFire = false;
            if (Definition != null)
            {
                nextUseTime = clock + Definition.UseInterval;
            }
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
            Ammunition = definition == null || definition.IsMelee
                ? 0
                : Mathf.Clamp(ammunition, 0, definition.AmmunitionCapacity);
            nextUseTime = 0f;
            hasStagedFire = false;
            RefreshVisual();
            EquipmentChanged?.Invoke();
        }

        public void Clear()
        {
            Definition = null;
            Ammunition = 0;
            nextUseTime = 0f;
            hasStagedFire = false;
            RefreshVisual();
            EquipmentChanged?.Invoke();
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
                if (Definition != null)
                {
                    heldWeaponRenderer.transform.localScale =
                        Definition.HeldVisualScale;
                    heldWeaponRenderer.material.color =
                        Definition.VisualColor;
                }
            }
        }

        public void CancelStagedUse()
        {
            hasStagedFire = false;
        }

        private void SpawnProjectile(
            CombatFaction faction,
            Vector3 direction,
            WorldTimeController worldTime)
        {
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
