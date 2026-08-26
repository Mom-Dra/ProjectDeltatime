using System;
using Deltatime.Audio;
using Deltatime.Core;
using Deltatime.TimeSystem;
using Deltatime.Visuals;
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
                float interval,
                MeleeImpactKind impactKind = MeleeImpactKind.Bat)
            {
                Direction = direction;
                Range = range;
                HalfAngle = halfAngle;
                Damage = damage;
                Interval = interval;
                ImpactKind = impactKind;
            }

            public Vector3 Direction { get; }
            public float Range { get; }
            public float HalfAngle { get; }
            public int Damage { get; }
            public float Interval { get; }
            public MeleeImpactKind ImpactKind { get; }
        }

        [SerializeField] private WeaponDefinition startingDefinition;
        [SerializeField] private Transform muzzle;
        [SerializeField] private Renderer heldWeaponRenderer;
        [SerializeField] private Projectile projectilePrefab;
        [SerializeField] private WeaponPickup pickupPrefab;
        [SerializeField] private ThrownWeapon thrownWeaponPrefab;

        private float nextUseTime;
        private bool hasStagedFire;
        private bool customHeldVisualActive;
        private Transform customHeldMuzzle;
        private int shotSequence;

        public event Action EquipmentChanged;
        public event Action UsePerformed;

        public WeaponDefinition Definition { get; private set; }
        public int Ammunition { get; private set; }
        public bool HasWeapon => Definition != null;
        public bool HasUsableWeapon =>
            Definition != null &&
            (Definition.IsMelee || Ammunition > 0);
        public bool CustomHeldVisualActive => customHeldVisualActive;
        public WeaponDefinition StartingDefinition => startingDefinition;
        public Transform Muzzle => customHeldMuzzle != null
            ? customHeldMuzzle
            : muzzle;

        private void Awake()
        {
            if (GetComponent<WeaponVisualPresenter>() == null)
            {
                gameObject.AddComponent<WeaponVisualPresenter>();
            }

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
            return TryFire(
                faction,
                direction,
                clock,
                worldTime,
                out _);
        }

        public bool TryFire(
            CombatFaction faction,
            Vector3 direction,
            float clock,
            WorldTimeController worldTime,
            out bool fireAttempted)
        {
            fireAttempted = false;
            if (Definition == null ||
                !Definition.IsFirearm ||
                clock < nextUseTime ||
                projectilePrefab == null ||
                Muzzle == null ||
                worldTime == null)
            {
                return false;
            }

            fireAttempted = true;
            if (Ammunition <= 0)
            {
                nextUseTime = clock + Definition.UseInterval;
                return false;
            }

            Ammunition--;
            nextUseTime = clock + Definition.UseInterval;
            SpawnProjectile(
                faction,
                direction,
                worldTime,
                ConsumeShotSequence());
            CombatFeedbackController.ReportWeaponFired(
                Definition,
                faction,
                Muzzle);
            SoundManager.Instance?.PlayWeaponFire(
                Definition,
                Muzzle.position,
                faction);
            UsePerformed?.Invoke();
            return true;
        }

        public bool TryMeleeAttack(
            CombatFaction faction,
            GameObject source,
            Vector3 direction,
            float clock,
            MeleeAttackExecution attackExecution = null)
        {
            if (Definition == null ||
                !Definition.IsMelee ||
                source == null ||
                clock < nextUseTime)
            {
                return false;
            }

            if (attackExecution != null)
            {
                if (!attackExecution.BeginAttack(
                        source,
                        faction,
                        direction,
                        Definition.MeleeRange,
                        Definition.MeleeHalfAngle,
                        Definition.Damage,
                        MeleeImpactKind.Bat))
                {
                    return false;
                }

                nextUseTime = clock + Definition.UseInterval;
                return true;
            }

            nextUseTime = clock + Definition.UseInterval;
            SoundManager.Instance?.PlayMeleeSwing(
                source.transform.position,
                faction);
            MeleeAttackResolver.TryHitNearest(
                source,
                faction,
                direction,
                Definition.MeleeRange,
                Definition.MeleeHalfAngle,
                Definition.Damage,
                MeleeImpactKind.Bat);
            UsePerformed?.Invoke();
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
                Muzzle == null ||
                worldTime == null)
            {
                return false;
            }

            Ammunition--;
            hasStagedFire = true;
            SpawnProjectile(
                faction,
                direction,
                worldTime,
                ConsumeShotSequence());
            CombatFeedbackController.ReportWeaponFired(
                Definition,
                faction,
                Muzzle);
            SoundManager.Instance?.PlayWeaponFire(
                Definition,
                Muzzle.position,
                faction);
            UsePerformed?.Invoke();
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
            float clock,
            MeleeAttackExecution attackExecution = null)
        {
            if (source == null || stagedAttack.Damage <= 0)
            {
                return;
            }

            if (attackExecution != null)
            {
                if (attackExecution.BeginAttack(
                        source,
                        faction,
                        stagedAttack.Direction,
                        stagedAttack.Range,
                        stagedAttack.HalfAngle,
                        stagedAttack.Damage,
                        stagedAttack.ImpactKind))
                {
                    nextUseTime = Mathf.Max(
                        nextUseTime,
                        clock + stagedAttack.Interval);
                }

                return;
            }

            MeleeAttackResolver.TryHitNearest(
                source,
                faction,
                stagedAttack.Direction,
                stagedAttack.Range,
                stagedAttack.HalfAngle,
                stagedAttack.Damage,
                stagedAttack.ImpactKind);
            UsePerformed?.Invoke();
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
            SoundManager.Instance?.PlayWeaponThrow(muzzle.position, faction);
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

        public void SetCustomHeldVisualActive(bool value)
        {
            if (customHeldVisualActive == value)
            {
                return;
            }

            customHeldVisualActive = value;
            RefreshVisual();
        }

        public void SetCustomHeldMuzzle(Transform value)
        {
            customHeldMuzzle = value;
        }

        private void RefreshVisual()
        {
            if (heldWeaponRenderer != null)
            {
                heldWeaponRenderer.enabled = Definition != null &&
                                             !customHeldVisualActive;
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
            WorldTimeController worldTime,
            int currentShotSequence)
        {
            int projectileCount = Mathf.Max(1, Definition.ProjectileCount);
            float coneAngle = projectileCount > 1
                ? Definition.SpreadAngle
                : 0f;

            for (int i = 0; i < projectileCount; i++)
            {
                Projectile projectile = Instantiate(
                    projectilePrefab,
                    Muzzle.position,
                    Quaternion.identity);
                projectile.Initialize(
                    worldTime,
                    faction,
                    gameObject,
                    WeaponSpreadPattern.GetProjectileDirection(
                        direction,
                        i,
                        projectileCount,
                        coneAngle,
                        Definition.SpreadJitterAngle,
                        Definition.SpreadSeed,
                        currentShotSequence),
                    Definition.ProjectileSpeed,
                    Definition.Damage,
                    Definition.ProjectileRadius,
                    Definition.MaximumProjectileDistance,
                    Definition);
            }
        }

        private int ConsumeShotSequence()
        {
            int currentShotSequence = shotSequence;
            shotSequence++;
            return currentShotSequence;
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
