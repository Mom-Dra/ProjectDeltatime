using UnityEngine;

namespace Deltatime.Core
{
    public static class CombatQuery
    {
        public static bool TryGetDamageable(Collider collider, out IDamageable damageable)
        {
            damageable = null;
            if (collider == null)
            {
                return false;
            }

            MonoBehaviour[] behaviours = collider.GetComponentsInParent<MonoBehaviour>();
            for (int i = 0; i < behaviours.Length; i++)
            {
                if (behaviours[i] is IDamageable candidate)
                {
                    damageable = candidate;
                    return true;
                }
            }

            return false;
        }

        public static bool BelongsToSource(Collider collider, GameObject source)
        {
            if (collider == null || source == null)
            {
                return false;
            }

            Transform colliderTransform = collider.transform;
            Transform sourceTransform = source.transform;
            return colliderTransform == sourceTransform ||
                   colliderTransform.IsChildOf(sourceTransform) ||
                   sourceTransform.IsChildOf(colliderTransform);
        }
    }
}
