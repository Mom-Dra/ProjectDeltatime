using Deltatime.Enemies;
using UnityEngine;

namespace Deltatime.Combat
{
    public sealed class WeaponPickup : MonoBehaviour
    {
        [SerializeField] private WeaponDefinition definition;
        [SerializeField, Min(0)] private int ammunition = 1;
        [SerializeField] private Renderer bodyRenderer;
        [SerializeField] private Color pickupColor = new Color(1f, 0.8f, 0.15f, 1f);

        private EnemyCombatant reservationOwner;

        public WeaponDefinition Definition => definition;
        public int Ammunition => ammunition;
        public EnemyCombatant ReservationOwner => reservationOwner;

        private void Awake()
        {
            if (bodyRenderer == null)
            {
                bodyRenderer = GetComponentInChildren<Renderer>();
            }

            ApplyVisual();
        }

        public bool IsAvailableTo(EnemyCombatant requester)
        {
            return reservationOwner == null ||
                   reservationOwner == requester;
        }

        public bool TryReserve(EnemyCombatant requester)
        {
            if (requester == null || !IsAvailableTo(requester))
            {
                return false;
            }

            reservationOwner = requester;
            return true;
        }

        public void ReleaseReservation(EnemyCombatant requester)
        {
            if (reservationOwner == requester)
            {
                reservationOwner = null;
            }
        }

        public void Initialize(WeaponDefinition weaponDefinition, int remainingAmmunition)
        {
            definition = weaponDefinition;
            ammunition = definition == null
                ? 0
                : Mathf.Clamp(remainingAmmunition, 0, definition.AmmunitionCapacity);
            ApplyVisual();
        }

        public bool TryTake(WeaponController collector)
        {
            return TryTakeInternal(collector, null, true);
        }

        public bool TryTake(
            WeaponController collector,
            EnemyCombatant requester)
        {
            return TryTakeInternal(collector, requester, false);
        }

        private bool TryTakeInternal(
            WeaponController collector,
            EnemyCombatant requester,
            bool ignoreReservation)
        {
            if (collector == null ||
                definition == null ||
                (!ignoreReservation && !IsAvailableTo(requester)))
            {
                return false;
            }

            WeaponDefinition previousDefinition = collector.Definition;
            int previousAmmunition = collector.Ammunition;

            reservationOwner = null;
            collector.Equip(definition, ammunition);

            if (previousDefinition == null)
            {
                Destroy(gameObject);
            }
            else
            {
                Initialize(previousDefinition, previousAmmunition);
            }

            return true;
        }

        private void ApplyVisual()
        {
            if (bodyRenderer != null)
            {
                bodyRenderer.material.color = definition == null
                    ? pickupColor
                    : definition.VisualColor;
            }

            if (definition != null)
            {
                transform.localScale = definition.WorldVisualScale;
            }
        }
    }
}
