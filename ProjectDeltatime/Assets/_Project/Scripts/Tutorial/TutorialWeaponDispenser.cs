using Deltatime.Combat;
using UnityEngine;

namespace Deltatime.Tutorial
{
    public sealed class TutorialWeaponDispenser : MonoBehaviour
    {
        [SerializeField] private WeaponController playerWeapon;
        [SerializeField] private WeaponPickup pickupPrefab;
        [SerializeField] private WeaponDefinition definition;
        [SerializeField] private Transform spawnAnchor;
        [SerializeField, Min(0)] private int ammunition;
        [SerializeField, Min(0.05f)] private float respawnDelay = 0.5f;

        private WeaponPickup spawnedPickup;
        private float respawnRemaining;

        public bool IsAvailable { get; private set; }
        public WeaponDefinition Definition => definition;
        public bool HasSpawnedPickup => spawnedPickup != null &&
                                        spawnedPickup.gameObject.activeInHierarchy;
        public WeaponPickup SpawnedPickup => spawnedPickup;
        public bool HasExpectedLoadout =>
            playerWeapon != null &&
            playerWeapon.Definition == definition &&
            (definition == null ||
             definition.IsMelee ||
             playerWeapon.Ammunition >= Mathf.Min(2, ammunition));

        private void Update()
        {
            RefreshPickup();
        }

        public void SetAvailable(bool value)
        {
            IsAvailable = value;
            respawnRemaining = 0f;

            if (!value)
            {
                if (spawnedPickup != null &&
                    spawnedPickup.Definition == definition)
                {
                    Destroy(spawnedPickup.gameObject);
                    spawnedPickup = null;
                }

                return;
            }

            RefreshPickup();
        }

        private void RefreshPickup()
        {
            if (!IsAvailable ||
                playerWeapon == null ||
                pickupPrefab == null ||
                definition == null ||
                spawnAnchor == null)
            {
                return;
            }

            if (spawnedPickup != null &&
                spawnedPickup.Definition != definition)
            {
                spawnedPickup = null;
            }

            if (HasExpectedLoadout || spawnedPickup != null)
            {
                respawnRemaining = respawnDelay;
                return;
            }

            respawnRemaining = Mathf.Max(
                0f,
                respawnRemaining - UnityEngine.Time.unscaledDeltaTime);
            if (respawnRemaining > 0f)
            {
                return;
            }

            spawnedPickup = Instantiate(
                pickupPrefab,
                spawnAnchor.position,
                spawnAnchor.rotation);
            spawnedPickup.name = $"Tutorial {definition.DisplayName} Pickup";
            spawnedPickup.Initialize(definition, ammunition);
            respawnRemaining = respawnDelay;
        }

        public void Configure(
            WeaponController collector,
            WeaponPickup template,
            WeaponDefinition weaponDefinition,
            Transform anchor,
            int suppliedAmmunition)
        {
            playerWeapon = collector;
            pickupPrefab = template;
            definition = weaponDefinition;
            spawnAnchor = anchor;
            ammunition = weaponDefinition == null || weaponDefinition.IsMelee
                ? 0
                : Mathf.Clamp(
                    suppliedAmmunition,
                    0,
                    weaponDefinition.AmmunitionCapacity);
        }
    }
}
