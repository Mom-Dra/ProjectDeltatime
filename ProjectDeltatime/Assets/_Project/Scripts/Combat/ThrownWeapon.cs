using Deltatime.Core;
using Deltatime.Replay;
using Deltatime.TimeSystem;
using Deltatime.Utilities;
using Deltatime.Visuals;
using UnityEngine;

namespace Deltatime.Combat
{
    [RequireComponent(typeof(LineRenderer))]
    public sealed class ThrownWeapon : MonoBehaviour
    {
        [SerializeField, Min(0.1f)] private float speed = 7f;
        [SerializeField, Min(0.01f)] private float collisionRadius = 0.25f;
        [SerializeField, Min(0.1f)] private float maximumTravelDistance = 4f;
        [SerializeField, Min(0.01f)] private float stunDuration = 2f;
        [SerializeField, Min(0f)] private float maximumTrailLength = 1.2f;
        [SerializeField, Min(1f)] private float slowTimeTrailMultiplier = 2f;
        [SerializeField] private Color trailColor = new Color(1f, 0.8f, 0.15f, 1f);

        private readonly RaycastHit[] castHits = new RaycastHit[24];
        private WorldTimeController worldTime;
        private WeaponPickup pickupPrefab;
        private WeaponDefinition definition;
        private LineRenderer trail;
        private Renderer bodyRenderer;
        private WeaponFlightVisualPresenter flightVisual;
        private CombatFaction faction;
        private GameObject source;
        private Vector3 direction;
        private Vector3 trailStart;
        private float travelledDistance;
        private int ammunition;
        private bool initialized;
        private bool resolved;

        public float Speed => speed;
        public float MaximumTravelDistance => maximumTravelDistance;
        public float StunDuration => stunDuration;
        public float TravelledDistance => travelledDistance;

        private void Awake()
        {
            trail = GetComponent<LineRenderer>();
            bodyRenderer = GetComponentInChildren<Renderer>();
            flightVisual = GetComponent<WeaponFlightVisualPresenter>();
            if (flightVisual == null)
            {
                flightVisual = gameObject.AddComponent<WeaponFlightVisualPresenter>();
            }
        }

        private void Update()
        {
            if (!initialized || resolved)
            {
                return;
            }

            float deltaTime = worldTime.WorldDeltaTime;
            float remainingDistance = Mathf.Max(
                0f,
                maximumTravelDistance - travelledDistance);
            if (remainingDistance <= 0.0001f)
            {
                Settle(transform.position);
                return;
            }

            float travelDistance = Mathf.Min(
                speed * deltaTime,
                remainingDistance);
            Vector3 origin = transform.position;

            if (travelDistance > 0f &&
                TryFindImpact(origin, travelDistance, out RaycastHit impact, out IDamageable target))
            {
                transform.position = origin + (direction * impact.distance);
                travelledDistance += impact.distance;
                if (target is IStunnable stunnable)
                {
                    stunnable.ReceiveStun(new StunHit(
                        stunDuration,
                        impact.point,
                        direction,
                        source));
                }

                Settle(impact.point);
                return;
            }

            transform.position = origin + (direction * travelDistance);
            travelledDistance += travelDistance;
            transform.Rotate(0f, 900f * deltaTime, 0f, Space.World);
            UpdateTrail();

            if (travelledDistance >= maximumTravelDistance - 0.0001f)
            {
                Settle(transform.position);
            }
        }

        public void Initialize(
            WorldTimeController timeSource,
            WeaponPickup dropPrefab,
            WeaponDefinition weaponDefinition,
            int remainingAmmunition,
            CombatFaction ownerFaction,
            GameObject owner,
            Vector3 travelDirection)
        {
            worldTime = timeSource;
            pickupPrefab = dropPrefab;
            definition = weaponDefinition;
            ammunition = remainingAmmunition;
            faction = ownerFaction;
            source = owner;
            direction = travelDirection.sqrMagnitude > 0.0001f
                ? travelDirection.normalized
                : Vector3.forward;
            trailStart = transform.position;
            initialized = worldTime != null && pickupPrefab != null && definition != null;

            if (definition != null)
            {
                flightVisual.Apply(definition, bodyRenderer);
                transform.localScale = flightVisual.HasCustomModel
                    ? Vector3.one
                    : definition.WorldVisualScale;
                if (!flightVisual.HasCustomModel && bodyRenderer != null)
                {
                    bodyRenderer.material.color = definition.VisualColor;
                }
            }

            trail.startColor = trailColor;
            trail.endColor = new Color(trailColor.r, trailColor.g, trailColor.b, 0.1f);
            UpdateTrail();

            if (!initialized)
            {
                Debug.LogError($"{nameof(ThrownWeapon)} was spawned without required data.", this);
                Destroy(gameObject);
                return;
            }

            ReplayVisualRegistry.Active?.RegisterRendererHierarchy(
                transform);
        }

        public void ConfigurePrototype(
            float throwSpeed,
            float maxTravelDistance,
            float stunWorldDuration)
        {
            speed = Mathf.Max(0.1f, throwSpeed);
            maximumTravelDistance = Mathf.Max(0.1f, maxTravelDistance);
            stunDuration = Mathf.Max(0.01f, stunWorldDuration);
        }

        private bool TryFindImpact(
            Vector3 origin,
            float distance,
            out RaycastHit nearestHit,
            out IDamageable nearestDamageable)
        {
            int count = Physics.SphereCastNonAlloc(
                origin,
                collisionRadius,
                direction,
                castHits,
                distance,
                ~0,
                QueryTriggerInteraction.Ignore);

            nearestHit = default;
            nearestDamageable = null;
            float nearestDistance = float.PositiveInfinity;

            for (int i = 0; i < count; i++)
            {
                Collider collider = castHits[i].collider;
                if (collider == null ||
                    collider.isTrigger ||
                    CombatQuery.BelongsToSource(collider, source))
                {
                    continue;
                }

                IDamageable damageable = null;
                if (CombatQuery.TryGetDamageable(collider, out IDamageable candidate))
                {
                    if (!candidate.IsAlive || candidate.Faction == faction)
                    {
                        continue;
                    }

                    damageable = candidate;
                }

                if (castHits[i].distance < nearestDistance)
                {
                    nearestDistance = castHits[i].distance;
                    nearestHit = castHits[i];
                    nearestDamageable = damageable;
                }
            }

            return nearestDistance < float.PositiveInfinity;
        }

        private void Settle(Vector3 position)
        {
            if (resolved)
            {
                return;
            }

            resolved = true;
            Vector3 pickupPosition = position;
            pickupPosition.y = 0.18f;
            WeaponPickup pickup = Instantiate(
                pickupPrefab,
                pickupPosition,
                Quaternion.identity);
            pickup.Initialize(definition, ammunition);
            HitFlash.Create(position, trailColor);
            Destroy(gameObject);
        }

        private void UpdateTrail()
        {
            trail.positionCount = 2;
            Vector3 head = transform.position;
            float trailLength = maximumTrailLength;
            if (worldTime != null)
            {
                float slowAmount = 1f - Mathf.Clamp01(worldTime.CurrentTimeScale);
                trailLength *= Mathf.Lerp(1f, slowTimeTrailMultiplier, slowAmount);
            }

            Vector3 tailEnd = Vector3.MoveTowards(head, trailStart, trailLength);
            trail.SetPosition(0, head);
            trail.SetPosition(1, tailEnd);
        }
    }
}
