using Deltatime.Combat;
using Deltatime.TimeSystem;
using UnityEngine;

namespace Deltatime.Enemies
{
    public sealed class EnemyWeaponDrop : MonoBehaviour
    {
        [SerializeField] private WeaponPickup pickupPrefab;
        [SerializeField] private InterceptableWeapon interceptablePrefab;
        [SerializeField] private WeaponDefinition definition;
        [SerializeField] private WorldTimeController worldTime;
        [SerializeField, Min(0)] private int ammunitionOnDrop = 4;

        [Header("Drop Direction")]
        [SerializeField, Min(0f)] private float minimumMovementDistance = 0.001f;
        [SerializeField, Min(0f)] private float movementDirectionFreshness = 0.2f;

        private bool hasDropped;
        private bool hasPreviousPosition;
        private Vector3 previousPosition;
        private Vector3 lastMovementDirection;
        private float lastMovementWorldTime = float.NegativeInfinity;

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

        public void Drop()
        {
            if (hasDropped ||
                pickupPrefab == null ||
                interceptablePrefab == null ||
                definition == null ||
                worldTime == null)
            {
                return;
            }

            hasDropped = true;
            Vector3 direction = ResolveDropDirection();
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
                ammunitionOnDrop,
                direction);
        }

        public void Configure(
            WeaponPickup dropPrefab,
            InterceptableWeapon airbornePrefab,
            WeaponDefinition weaponDefinition,
            WorldTimeController timeSource,
            int remainingAmmunition)
        {
            pickupPrefab = dropPrefab;
            interceptablePrefab = airbornePrefab;
            definition = weaponDefinition;
            worldTime = timeSource;
            ammunitionOnDrop = remainingAmmunition;
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
