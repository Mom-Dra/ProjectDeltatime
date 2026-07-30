using System.Collections.Generic;
using Deltatime.Core;
using Deltatime.TimeSystem;
using Deltatime.Utilities;
using UnityEngine;

namespace Deltatime.Combat
{
    [RequireComponent(typeof(LineRenderer))]
    public sealed class Projectile : MonoBehaviour
    {
        private static readonly List<Projectile> activeProjectiles =
            new List<Projectile>(64);

        [SerializeField, Min(0.1f)] private float maximumWorldLifetime = 4f;
        [SerializeField, Min(0f)] private float maximumTrailLength = 0.8f;
        [SerializeField, Min(1f)] private float slowTimeTrailMultiplier = 2f;
        [SerializeField] private Color playerColor = new Color(0.2f, 1f, 1f, 1f);
        [SerializeField] private Color enemyColor = new Color(1f, 0.2f, 0.2f, 1f);

        private readonly RaycastHit[] castHits = new RaycastHit[24];
        private WorldTimeController worldTime;
        private LineRenderer trail;
        private CombatFaction faction;
        private GameObject source;
        private Vector3 direction;
        private Vector3 trailStart;
        private float speed;
        private float radius;
        private float worldLifetime;
        private int damage;
        private bool initialized;
        private bool resolved;
        private bool deadlineClaimed;
        private Color normalStartColor;
        private Color normalEndColor;
        private float normalStartWidth;
        private float normalEndWidth;

        public static IReadOnlyList<Projectile> ActiveProjectiles =>
            activeProjectiles;
        public CombatFaction Faction => faction;
        public Vector3 Direction => direction;
        public float Speed => speed;
        public float Radius => radius;
        public bool IsActive => initialized && !resolved;
        public bool CanTriggerDeadline => IsActive && !deadlineClaimed;

        private void Awake()
        {
            trail = GetComponent<LineRenderer>();
            normalStartWidth = trail.startWidth;
            normalEndWidth = trail.endWidth;
        }

        private void OnEnable()
        {
            if (!activeProjectiles.Contains(this))
            {
                activeProjectiles.Add(this);
            }
        }

        private void Update()
        {
            if (!initialized || resolved)
            {
                return;
            }

            float deltaTime = worldTime.WorldDeltaTime;
            float travelDistance = speed * deltaTime;
            Vector3 origin = transform.position;

            if (travelDistance > 0f && TryFindImpact(origin, travelDistance, out RaycastHit impact, out IDamageable target))
            {
                transform.position = origin + (direction * impact.distance);
                UpdateTrail();
                ResolveImpact(impact.point, target);
                return;
            }

            transform.position = origin + (direction * travelDistance);
            worldLifetime += deltaTime;
            UpdateTrail();

            if (worldLifetime >= maximumWorldLifetime)
            {
                resolved = true;
                Destroy(gameObject);
            }
        }

        public void Initialize(
            WorldTimeController timeSource,
            CombatFaction ownerFaction,
            GameObject owner,
            Vector3 travelDirection,
            float travelSpeed,
            int hitDamage,
            float castRadius)
        {
            worldTime = timeSource;
            faction = ownerFaction;
            source = owner;
            direction = travelDirection.sqrMagnitude > 0.0001f
                ? travelDirection.normalized
                : Vector3.forward;
            speed = travelSpeed;
            damage = hitDamage;
            radius = castRadius;
            trailStart = transform.position;
            initialized = worldTime != null;

            Color color = faction == CombatFaction.Player ? playerColor : enemyColor;
            normalStartColor = color;
            normalEndColor = new Color(color.r, color.g, color.b, 0.2f);
            trail.startColor = normalStartColor;
            trail.endColor = normalEndColor;
            UpdateTrail();

            if (!initialized)
            {
                Debug.LogError($"{nameof(Projectile)} was spawned without world time.", this);
                Destroy(gameObject);
            }
        }

        public bool TryPredictImpact(
            GameObject target,
            float maximumWorldSeconds,
            out float impactWorldTime)
        {
            impactWorldTime = float.PositiveInfinity;
            if (!IsActive ||
                target == null ||
                speed <= 0f ||
                maximumWorldSeconds <= 0f)
            {
                return false;
            }

            float predictionDistance = speed * maximumWorldSeconds;
            if (!TryFindImpact(
                    transform.position,
                    predictionDistance,
                    out RaycastHit impact,
                    out IDamageable damageable) ||
                damageable == null ||
                !CombatQuery.BelongsToSource(impact.collider, target))
            {
                return false;
            }

            impactWorldTime = impact.distance / speed;
            return true;
        }

        public bool TryClaimDeadline()
        {
            if (!IsActive || deadlineClaimed)
            {
                return false;
            }

            deadlineClaimed = true;
            return true;
        }

        public void SetDeadlineHighlighted(bool value)
        {
            if (trail == null)
            {
                return;
            }

            if (value)
            {
                trail.startColor = new Color(1f, 0.14f, 0.08f, 1f);
                trail.endColor = new Color(1f, 0.02f, 0.01f, 0.55f);
                trail.startWidth = normalStartWidth * 1.8f;
                trail.endWidth = normalEndWidth * 1.8f;
                return;
            }

            trail.startColor = normalStartColor;
            trail.endColor = normalEndColor;
            trail.startWidth = normalStartWidth;
            trail.endWidth = normalEndWidth;
        }

        private bool TryFindImpact(
            Vector3 origin,
            float distance,
            out RaycastHit nearestHit,
            out IDamageable nearestDamageable)
        {
            int count = Physics.SphereCastNonAlloc(
                origin,
                radius,
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

        private void ResolveImpact(Vector3 point, IDamageable target)
        {
            if (resolved)
            {
                return;
            }

            resolved = true;
            if (target != null)
            {
                target.ReceiveHit(new DamageHit(damage, point, direction, source));
            }

            Color flashColor = faction == CombatFaction.Player ? playerColor : enemyColor;
            HitFlash.Create(point, flashColor);
            Destroy(gameObject);
        }

        private void UpdateTrail()
        {
            if (trail == null)
            {
                return;
            }

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

        private void OnDisable()
        {
            activeProjectiles.Remove(this);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetRegistry()
        {
            activeProjectiles.Clear();
        }
    }
}
