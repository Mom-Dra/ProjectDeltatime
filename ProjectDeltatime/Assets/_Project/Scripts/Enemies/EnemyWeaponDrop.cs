using Deltatime.Combat;
using UnityEngine;

namespace Deltatime.Enemies
{
    public sealed class EnemyWeaponDrop : MonoBehaviour
    {
        [SerializeField] private WeaponPickup pickupPrefab;
        [SerializeField] private WeaponDefinition definition;
        [SerializeField, Min(0)] private int ammunitionOnDrop = 4;

        private bool hasDropped;

        public void Drop()
        {
            if (hasDropped || pickupPrefab == null || definition == null)
            {
                return;
            }

            hasDropped = true;
            Vector3 dropPosition = transform.position;
            dropPosition.y = 0.18f;
            WeaponPickup pickup = Instantiate(
                pickupPrefab,
                dropPosition,
                Quaternion.identity);
            pickup.Initialize(definition, ammunitionOnDrop);
        }

        public void Configure(
            WeaponPickup dropPrefab,
            WeaponDefinition weaponDefinition,
            int remainingAmmunition)
        {
            pickupPrefab = dropPrefab;
            definition = weaponDefinition;
            ammunitionOnDrop = remainingAmmunition;
        }
    }
}
