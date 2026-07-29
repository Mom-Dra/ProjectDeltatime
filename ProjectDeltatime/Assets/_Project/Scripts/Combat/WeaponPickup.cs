using UnityEngine;

namespace Deltatime.Combat
{
    public sealed class WeaponPickup : MonoBehaviour
    {
        [SerializeField] private WeaponDefinition definition;
        [SerializeField, Min(0)] private int ammunition = 1;
        [SerializeField] private Renderer bodyRenderer;
        [SerializeField] private Color pickupColor = new Color(1f, 0.8f, 0.15f, 1f);

        public WeaponDefinition Definition => definition;
        public int Ammunition => ammunition;

        private void Awake()
        {
            if (bodyRenderer == null)
            {
                bodyRenderer = GetComponentInChildren<Renderer>();
            }

            ApplyVisual();
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
            if (collector == null || definition == null)
            {
                return false;
            }

            WeaponDefinition previousDefinition = collector.Definition;
            int previousAmmunition = collector.Ammunition;

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
                bodyRenderer.material.color = pickupColor;
            }
        }
    }
}
