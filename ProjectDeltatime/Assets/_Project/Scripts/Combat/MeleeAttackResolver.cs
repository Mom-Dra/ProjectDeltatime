using Deltatime.Audio;
using Deltatime.Core;
using UnityEngine;

namespace Deltatime.Combat
{
    public enum MeleeImpactKind
    {
        Punch,
        Bat
    }

    public static class MeleeAttackResolver
    {
        private static readonly Collider[] CandidateColliders =
            new Collider[32];
        private static readonly RaycastHit[] SightHits =
            new RaycastHit[32];

        public static bool TryHitNearest(
            GameObject source,
            CombatFaction sourceFaction,
            Vector3 direction,
            float range,
            float halfAngleDegrees,
            int damage,
            MeleeImpactKind impactKind = MeleeImpactKind.Punch)
        {
            if (source == null || range <= 0f || damage <= 0)
            {
                return false;
            }

            Vector3 attackDirection = direction;
            attackDirection.y = 0f;
            if (attackDirection.sqrMagnitude <= 0.000001f)
            {
                return false;
            }

            attackDirection.Normalize();
            Vector3 origin =
                source.transform.position +
                (Vector3.up * 0.2f);
            int count = Physics.OverlapSphereNonAlloc(
                origin,
                range,
                CandidateColliders,
                ~0,
                QueryTriggerInteraction.Ignore);

            IDamageable nearestDamageable = null;
            Collider nearestCollider = null;
            float nearestDistanceSquared = float.PositiveInfinity;

            for (int i = 0; i < count; i++)
            {
                Collider candidateCollider = CandidateColliders[i];
                if (candidateCollider == null ||
                    CombatQuery.BelongsToSource(candidateCollider, source) ||
                    !CombatQuery.TryGetDamageable(
                        candidateCollider,
                        out IDamageable candidate) ||
                    !candidate.IsAlive ||
                    candidate.Faction == sourceFaction)
                {
                    continue;
                }

                Vector3 targetPoint = candidateCollider.bounds.center;
                Vector3 offset = targetPoint - origin;
                offset.y = 0f;
                float distanceSquared = offset.sqrMagnitude;
                if (distanceSquared > range * range ||
                    distanceSquared <= 0.000001f ||
                    Vector3.Angle(attackDirection, offset) >
                    Mathf.Clamp(halfAngleDegrees, 1f, 90f) ||
                    distanceSquared >= nearestDistanceSquared ||
                    !HasLineOfSight(
                        source,
                        sourceFaction,
                        candidate,
                        origin,
                        targetPoint))
                {
                    continue;
                }

                nearestDistanceSquared = distanceSquared;
                nearestDamageable = candidate;
                nearestCollider = candidateCollider;
            }

            if (nearestDamageable == null || nearestCollider == null)
            {
                return false;
            }

            Vector3 hitPoint = nearestCollider.bounds.ClosestPoint(origin);
            Vector3 hitDirection = hitPoint - origin;
            hitDirection.y = 0f;
            if (hitDirection.sqrMagnitude <= 0.000001f)
            {
                hitDirection = attackDirection;
            }

            nearestDamageable.ReceiveHit(new DamageHit(
                damage,
                hitPoint,
                hitDirection.normalized,
                source));
            SoundManager.Instance?.PlayMeleeImpact(impactKind, hitPoint);
            return true;
        }

        private static bool HasLineOfSight(
            GameObject source,
            CombatFaction sourceFaction,
            IDamageable target,
            Vector3 origin,
            Vector3 targetPoint)
        {
            Component targetComponent = target as Component;
            if (targetComponent == null)
            {
                return false;
            }

            Vector3 offset = targetPoint - origin;
            float distance = offset.magnitude;
            if (distance <= 0.001f)
            {
                return true;
            }

            int count = Physics.RaycastNonAlloc(
                origin,
                offset / distance,
                SightHits,
                distance + 0.05f,
                ~0,
                QueryTriggerInteraction.Ignore);
            float nearestDistance = float.PositiveInfinity;
            Collider nearestCollider = null;

            for (int i = 0; i < count; i++)
            {
                Collider collider = SightHits[i].collider;
                if (collider == null ||
                    collider.isTrigger ||
                    CombatQuery.BelongsToSource(collider, source))
                {
                    continue;
                }

                if (CombatQuery.TryGetDamageable(
                        collider,
                        out IDamageable damageable) &&
                    damageable.Faction == sourceFaction)
                {
                    continue;
                }

                if (SightHits[i].distance < nearestDistance)
                {
                    nearestDistance = SightHits[i].distance;
                    nearestCollider = collider;
                }
            }

            return nearestCollider != null &&
                   CombatQuery.BelongsToSource(
                       nearestCollider,
                       targetComponent.gameObject);
        }
    }
}
