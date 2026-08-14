using UnityEngine;

namespace Deltatime.Enemies
{
    internal static class EnemyMovementMath
    {
        internal static Vector3 PlanarOffset(Vector3 from, Vector3 to)
        {
            Vector3 offset = to - from;
            offset.y = 0f;
            return offset;
        }

        internal static bool HasReached(
            Vector3 planarOffset,
            float stoppingDistance)
        {
            return planarOffset.magnitude <= Mathf.Max(0f, stoppingDistance);
        }

        internal static float RequestedDistance(
            float speed,
            float deltaTime,
            float remainingDistance,
            float stoppingDistance)
        {
            return Mathf.Min(
                speed * deltaTime,
                Mathf.Max(
                    0f,
                    remainingDistance - Mathf.Max(0f, stoppingDistance)));
        }

        internal static bool DestinationChanged(
            Vector3 destination,
            Vector3 previousDestination)
        {
            return (destination - previousDestination).sqrMagnitude > 0.16f;
        }
    }
}
