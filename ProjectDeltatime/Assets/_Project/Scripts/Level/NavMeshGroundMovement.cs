using UnityEngine;
using UnityEngine.AI;

namespace Deltatime.Level
{
    /// <summary>
    /// Optional actor-local movement projector for stages whose baked NavMesh
    /// contains playable elevation changes. Stages without this component keep
    /// their existing planar Rigidbody movement.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody))]
    public sealed class NavMeshGroundMovement : MonoBehaviour
    {
        [SerializeField, Min(0.05f)] private float sampleDistance = 1.25f;
        [SerializeField, Min(0.02f)] private float maximumSegmentLength = 0.12f;
        [SerializeField] private int areaMask = NavMesh.AllAreas;

        // NavMesh points describe the walkable surface, while this component
        // moves Rigidbody roots. Capture their initial vertical relationship
        // once so a capsule root is not moved down into the floor.
        private bool hasGroundOffset;
        private float groundOffset;

        /// <summary>
        /// Projects a small planar displacement onto the same reachable NavMesh
        /// region, preserving the baked ground height for stairs and platforms.
        /// </summary>
        public bool TryProjectDisplacement(
            Vector3 start,
            Vector3 planarDisplacement,
            out Vector3 projectedPosition)
        {
            planarDisplacement.y = 0f;
            projectedPosition = start;
            float totalDistance = planarDisplacement.magnitude;
            if (totalDistance <= 0.00001f)
            {
                return true;
            }

            if (!NavMesh.SamplePosition(
                    start,
                    out NavMeshHit currentHit,
                    sampleDistance,
                    areaMask))
            {
                return false;
            }

            int segmentCount = Mathf.Max(
                1,
                Mathf.CeilToInt(totalDistance / maximumSegmentLength));
            Vector3 segment = planarDisplacement / segmentCount;
            Vector3 current = currentHit.position;
            bool moved = false;

            for (int i = 0; i < segmentCount; i++)
            {
                Vector3 requested = current + segment;
                if (!NavMesh.SamplePosition(
                        requested,
                        out NavMeshHit nextHit,
                        sampleDistance,
                        areaMask))
                {
                    break;
                }

                if (NavMesh.Raycast(
                        current,
                        nextHit.position,
                        out _,
                        areaMask) &&
                    !TryResolveElevationStep(
                        current,
                        segment,
                        ref nextHit))
                {
                    break;
                }

                Vector3 planarError = nextHit.position - requested;
                planarError.y = 0f;
                if (planarError.magnitude > sampleDistance)
                {
                    break;
                }

                current = nextHit.position;
                moved = true;
            }

            if (!moved)
            {
                return false;
            }

            projectedPosition = current;
            return true;
        }

        private bool TryResolveElevationStep(
            Vector3 current,
            Vector3 requestedSegment,
            ref NavMeshHit nextHit)
        {
            NavMeshPath path = new NavMeshPath();
            if (!NavMesh.CalculatePath(
                    current,
                    nextHit.position,
                    areaMask,
                    path) ||
                path.status != NavMeshPathStatus.PathComplete ||
                path.corners.Length < 2)
            {
                return false;
            }

            Vector3 candidate = path.corners[1];
            Vector3 candidatePlanar = candidate - current;
            candidatePlanar.y = 0f;
            Vector3 requestedPlanar = requestedSegment;
            requestedPlanar.y = 0f;
            if (candidatePlanar.sqrMagnitude <= 0.000001f ||
                Vector3.Dot(
                    candidatePlanar.normalized,
                    requestedPlanar.normalized) < 0.5f ||
                candidatePlanar.magnitude > maximumSegmentLength * 2.5f)
            {
                return false;
            }

            nextHit.position = candidate;
            return true;
        }

        public bool TryMove(
            Rigidbody body,
            Vector3 planarDisplacement,
            out float movedPlanarDistance)
        {
            if (!TryProjectRigidbodyDisplacement(
                    body,
                    planarDisplacement,
                    out Vector3 projected,
                    out movedPlanarDistance))
            {
                return false;
            }

            body.MovePosition(projected);
            return true;
        }

        /// <summary>
        /// Projects an actor-root displacement onto the NavMesh while keeping
        /// the root's initial vertical distance above the walkable surface.
        /// </summary>
        public bool TryProjectRigidbodyDisplacement(
            Rigidbody body,
            Vector3 planarDisplacement,
            out Vector3 projectedPosition,
            out float movedPlanarDistance)
        {
            projectedPosition = body == null ? Vector3.zero : body.position;
            movedPlanarDistance = 0f;
            planarDisplacement.y = 0f;
            if (body == null || planarDisplacement.sqrMagnitude <= 0.0000000001f ||
                !TryGetGroundOffset(body.position, out float verticalOffset) ||
                !TryProjectDisplacement(
                    body.position,
                    planarDisplacement,
                    out Vector3 projectedSurfacePosition))
            {
                return false;
            }

            projectedPosition = projectedSurfacePosition;
            projectedPosition.y += verticalOffset;
            Vector3 planarDelta = projectedPosition - body.position;
            planarDelta.y = 0f;
            movedPlanarDistance = planarDelta.magnitude;
            return movedPlanarDistance > 0.00001f;
        }

        private bool TryGetGroundOffset(
            Vector3 bodyPosition,
            out float verticalOffset)
        {
            if (hasGroundOffset)
            {
                verticalOffset = groundOffset;
                return true;
            }

            if (!NavMesh.SamplePosition(
                    bodyPosition,
                    out NavMeshHit groundHit,
                    sampleDistance,
                    areaMask))
            {
                verticalOffset = 0f;
                return false;
            }

            groundOffset = bodyPosition.y - groundHit.position.y;
            hasGroundOffset = true;
            verticalOffset = groundOffset;
            return true;
        }

        public void Configure(float configuredSampleDistance, float configuredSegmentLength)
        {
            sampleDistance = Mathf.Max(0.05f, configuredSampleDistance);
            maximumSegmentLength = Mathf.Max(0.02f, configuredSegmentLength);
            areaMask = NavMesh.AllAreas;
        }

        private void OnEnable()
        {
            hasGroundOffset = false;
            groundOffset = 0f;
        }

        private void OnDisable()
        {
            hasGroundOffset = false;
            groundOffset = 0f;
        }

        private void OnValidate()
        {
            sampleDistance = Mathf.Max(0.05f, sampleDistance);
            maximumSegmentLength = Mathf.Max(0.02f, maximumSegmentLength);
        }
    }
}
