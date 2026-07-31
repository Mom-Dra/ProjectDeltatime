using Deltatime.Core;
using Deltatime.Player;
using UnityEngine;

namespace Deltatime.Enemies
{
    [DisallowMultipleComponent]
    public sealed class EnemyPerception : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private PlayerHealth targetHealth;
        [SerializeField] private Transform sightOrigin;
        [SerializeField, Min(0.1f)] private float detectionRange = 18f;

        private readonly RaycastHit[] sightHits = new RaycastHit[24];

        public Transform Target => target;
        public PlayerHealth TargetHealth => targetHealth;
        public bool HasLivingTarget =>
            target != null &&
            targetHealth != null &&
            targetHealth.IsAlive;
        public bool HasLastKnownTargetPosition { get; private set; }
        public Vector3 LastKnownTargetPosition { get; private set; }
        public float DetectionRange => detectionRange;

        public float PlanarDistanceToTarget
        {
            get
            {
                if (target == null)
                {
                    return float.PositiveInfinity;
                }

                Vector3 offset = target.position - transform.position;
                offset.y = 0f;
                return offset.magnitude;
            }
        }

        public Vector3 PlanarDirectionToTarget
        {
            get
            {
                if (target == null)
                {
                    return Vector3.zero;
                }

                Vector3 direction = target.position - transform.position;
                direction.y = 0f;
                return direction.sqrMagnitude <= 0.000001f
                    ? Vector3.zero
                    : direction.normalized;
            }
        }

        private void Awake()
        {
            if (target == null ||
                targetHealth == null ||
                sightOrigin == null)
            {
                Debug.LogError(
                    $"{nameof(EnemyPerception)} is missing required references.",
                    this);
                enabled = false;
            }
        }

        public void Configure(
            Transform playerTarget,
            PlayerHealth playerHealth,
            Transform origin,
            float range = 18f)
        {
            target = playerTarget;
            targetHealth = playerHealth;
            sightOrigin = origin;
            detectionRange = Mathf.Max(0.1f, range);
        }

        public bool CanSeeTarget()
        {
            if (!HasLivingTarget || sightOrigin == null)
            {
                return false;
            }

            Vector3 origin = sightOrigin.position;
            Vector3 offset =
                target.position +
                (Vector3.up * 0.2f) -
                origin;
            float distance = offset.magnitude;

            if (distance > detectionRange || distance <= 0.001f)
            {
                return false;
            }

            int count = Physics.RaycastNonAlloc(
                origin,
                offset / distance,
                sightHits,
                distance,
                ~0,
                QueryTriggerInteraction.Ignore);

            float nearestDistance = float.PositiveInfinity;
            Collider nearestCollider = null;

            for (int i = 0; i < count; i++)
            {
                Collider collider = sightHits[i].collider;
                if (collider == null ||
                    collider.isTrigger ||
                    CombatQuery.BelongsToSource(collider, gameObject))
                {
                    continue;
                }

                if (CombatQuery.TryGetDamageable(
                        collider,
                        out IDamageable damageable) &&
                    damageable.Faction == CombatFaction.Enemy)
                {
                    continue;
                }

                if (sightHits[i].distance < nearestDistance)
                {
                    nearestDistance = sightHits[i].distance;
                    nearestCollider = collider;
                }
            }

            bool visible =
                nearestCollider != null &&
                CombatQuery.BelongsToSource(
                    nearestCollider,
                    target.gameObject);
            if (visible)
            {
                LastKnownTargetPosition = target.position;
                HasLastKnownTargetPosition = true;
            }

            return visible;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0.2f, 0.2f, 0.25f);
            Gizmos.DrawWireSphere(transform.position, detectionRange);
        }
    }
}
