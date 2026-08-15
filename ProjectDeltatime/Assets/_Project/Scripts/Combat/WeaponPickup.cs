using Deltatime.Enemies;
using Deltatime.Replay;
using Deltatime.Visuals;
using UnityEngine;

namespace Deltatime.Combat
{
    public sealed class WeaponPickup : MonoBehaviour
    {
        private const string CustomModelName = "Weapon Model Visual";

        [SerializeField] private WeaponDefinition definition;
        [SerializeField, Min(0)] private int ammunition = 1;
        [SerializeField] private Renderer bodyRenderer;
        [SerializeField] private Color pickupColor = new Color(1f, 0.8f, 0.15f, 1f);

        private EnemyCombatant reservationOwner;
        private GameObject customModel;
        private WeaponDefinition customModelDefinition;
        private WeaponPickupOutline pickupOutline;

        public WeaponDefinition Definition => definition;
        public int Ammunition => ammunition;
        public EnemyCombatant ReservationOwner => reservationOwner;

        private void Awake()
        {
            pickupOutline = GetComponent<WeaponPickupOutline>();
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
            bool hasCustomModel = definition != null &&
                                  definition.HasCustomWorldVisual;
            if (bodyRenderer != null)
            {
                bodyRenderer.enabled = !hasCustomModel;
                if (!hasCustomModel)
                {
                    bodyRenderer.material.color = definition == null
                        ? pickupColor
                        : definition.VisualColor;
                }
            }

            if (definition != null)
            {
                transform.localScale = hasCustomModel
                    ? Vector3.one
                    : definition.WorldVisualScale;
            }

            if (hasCustomModel)
            {
                EnsureCustomModel();
            }
            else
            {
                RemoveCustomModel();
            }

            Transform visualRoot = null;
            if (hasCustomModel && customModel != null)
            {
                visualRoot = customModel.transform;
            }
            else if (!hasCustomModel && bodyRenderer != null)
            {
                visualRoot = bodyRenderer.transform;
            }

            RefreshOutline(visualRoot);
        }

        private void RefreshOutline(Transform visualRoot)
        {
            if (pickupOutline == null)
            {
                pickupOutline = GetComponent<WeaponPickupOutline>();
            }

            if (definition == null)
            {
                pickupOutline?.Clear();
                return;
            }

            pickupOutline?.Refresh(visualRoot);
            ReplayVisualRegistry.Active?.RegisterRendererHierarchy(transform);
        }

        private void EnsureCustomModel()
        {
            if (customModel == null)
            {
                Transform existing = transform.Find(CustomModelName);
                if (existing != null)
                {
                    customModel = existing.gameObject;
                }
            }

            if (customModel != null && customModelDefinition == definition)
            {
                return;
            }

            RemoveCustomModel();
            customModel = Instantiate(
                definition.WorldVisualPrefab,
                transform,
                false);
            customModel.name = CustomModelName;
            Transform modelTransform = customModel.transform;
            modelTransform.localPosition = definition.WorldModelLocalPosition;
            modelTransform.localRotation = Quaternion.Euler(
                definition.WorldModelLocalEulerAngles);
            modelTransform.localScale = definition.WorldModelLocalScale;
            customModelDefinition = definition;
        }

        private void RemoveCustomModel()
        {
            if (customModel != null)
            {
                if (Application.isPlaying)
                {
                    Destroy(customModel);
                }
                else
                {
                    DestroyImmediate(customModel);
                }
            }

            customModel = null;
            customModelDefinition = null;
        }
    }
}
