using System;
using Deltatime.Combat;
using Deltatime.TimeSystem;
using UnityEngine;

namespace Deltatime.Enemies
{
    [RequireComponent(typeof(WeaponController))]
    public sealed class EnemyWeaponDrop : MonoBehaviour
    {
        [SerializeField] private WeaponPickup pickupPrefab;
        [SerializeField] private InterceptableWeapon interceptablePrefab;
        [SerializeField] private WeaponController equipment;
        [SerializeField] private WorldTimeController worldTime;

        [Header("Drop Direction")]
        [SerializeField, Min(0f)] private float minimumMovementDistance = 0.001f;
        [SerializeField, Min(0f)] private float movementDirectionFreshness = 0.2f;

        private bool hasPreviousPosition;
        private Vector3 previousPosition;
        private Vector3 lastMovementDirection;
        private float lastMovementWorldTime = float.NegativeInfinity;

        public event Action WeaponDropped;

        private void OnEnable()
        {
            previousPosition = transform.position;
            hasPreviousPosition = true;
        }

        private void LateUpdate()
        {
            Vector3 currentPosition = transform.position;
            if (!hasPreviousPosition)
            {
                previousPosition = currentPosition;
                hasPreviousPosition = true;
                return;
            }

            Vector3 displacement = currentPosition - previousPosition;
            displacement.y = 0f;
            previousPosition = currentPosition;

            float minimumDistanceSquared =
                minimumMovementDistance * minimumMovementDistance;
            if (displacement.sqrMagnitude <=
                Mathf.Max(0.00000001f, minimumDistanceSquared))
            {
                return;
            }

            lastMovementDirection = displacement.normalized;
            if (worldTime != null)
            {
                lastMovementWorldTime = worldTime.WorldElapsedTime;
            }
        }

        public bool Drop()
        {
            if (pickupPrefab == null ||
                interceptablePrefab == null ||
                equipment == null ||
                worldTime == null ||
                !TryRemoveCurrentWeapon(
                    out WeaponDefinition definition,
                    out int ammunition))
            {
                return false;
            }

            Vector3 spawnPosition =
                transform.position +
                (Vector3.up * 0.45f);
            InterceptableWeapon interceptable = Instantiate(
                interceptablePrefab,
                spawnPosition,
                Quaternion.identity);
            interceptable.Initialize(
                worldTime,
                pickupPrefab,
                definition,
                ammunition,
                ResolveDropDirection());
            WeaponDropped?.Invoke();
            return true;
        }

        public bool DropGround()
        {
            if (pickupPrefab == null ||
                equipment == null ||
                !TryRemoveCurrentWeapon(
                    out WeaponDefinition definition,
                    out int ammunition))
            {
                return false;
            }

            Vector3 pickupPosition = transform.position;
            pickupPosition.y = 0.18f;
            WeaponPickup pickup = Instantiate(
                pickupPrefab,
                pickupPosition,
                Quaternion.identity);
            pickup.Initialize(definition, ammunition);
            WeaponDropped?.Invoke();
            return true;
        }

        public void Configure(
            WeaponPickup dropPrefab,
            InterceptableWeapon airbornePrefab,
            WeaponController weaponController,
            WorldTimeController timeSource)
        {
            pickupPrefab = dropPrefab;
            interceptablePrefab = airbornePrefab;
            equipment = weaponController;
            worldTime = timeSource;
        }

        private bool TryRemoveCurrentWeapon(
            out WeaponDefinition definition,
            out int ammunition)
        {
            definition = equipment == null
                ? null
                : equipment.Definition;
            ammunition = equipment == null
                ? 0
                : equipment.Ammunition;
            if (definition == null)
            {
                return false;
            }

            equipment.Clear();
            return true;
        }

        private Vector3 ResolveDropDirection()
        {
            if (worldTime != null &&
                lastMovementDirection.sqrMagnitude > 0.0001f &&
                worldTime.WorldElapsedTime - lastMovementWorldTime <=
                movementDirectionFreshness)
            {
                return lastMovementDirection;
            }

            Vector3 direction = transform.forward;
            direction.y = 0f;
            if (direction.sqrMagnitude <= 0.0001f)
            {
                return Vector3.forward;
            }

            return direction.normalized;
        }
    }
}
