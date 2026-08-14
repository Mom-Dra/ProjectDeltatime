using Deltatime.Core;
using Deltatime.Level;
using Deltatime.TimeSystem;
using UnityEngine;
using UnityEngine.AI;

namespace Deltatime.Enemies
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(CapsuleCollider))]
    public sealed class EnemyMotor : MonoBehaviour
    {
        [SerializeField] private WorldTimeController worldTime;

        [Header("Movement")]
        [SerializeField, Min(0f)] private float moveSpeed = 3.4f;
        [SerializeField, Min(1f)] private float rotationSpeed = 220f;
        [SerializeField, Min(0.01f)] private float repathInterval = 0.15f;
        [SerializeField, Min(0.01f)] private float cornerReachDistance = 0.18f;
        [SerializeField, Min(0f)] private float collisionPadding = 0.03f;

        [Header("Local Separation")]
        [SerializeField, Min(0f)] private float separationRadius = 0.9f;
        [SerializeField, Range(0f, 2f)] private float separationWeight = 0.7f;

        private readonly RaycastHit[] movementHits = new RaycastHit[24];
        private readonly Collider[] separationHits = new Collider[16];
        private NavMeshPath path;
        private NavMeshPath queryPath;

        private Rigidbody body;
        private CapsuleCollider capsule;
        private NavMeshGroundMovement groundMovement;
        private Vector3[] pathCorners = System.Array.Empty<Vector3>();
        private Vector3 lastPathDestination;
        private float nextRepathWorldTime;
        private int cornerIndex;
        private bool hasPath;
        private bool warnedMissingNavMesh;

        public bool IsMoving { get; private set; }
        public Vector3 MovementDirection { get; private set; }
        public bool HasNavigationPath => hasPath;
        public float MoveSpeed => moveSpeed;
        public float TotalDistanceMoved { get; private set; }

        private void Awake()
        {
            body = GetComponent<Rigidbody>();
            capsule = GetComponent<CapsuleCollider>();
            groundMovement = GetComponent<NavMeshGroundMovement>();
            path = new NavMeshPath();
            queryPath = new NavMeshPath();
            if (worldTime == null)
            {
                Debug.LogError(
                    $"{nameof(EnemyMotor)} requires a world-time reference.",
                    this);
                enabled = false;
            }
        }

        public void Configure(
            WorldTimeController timeSource,
            float speed,
            float turnSpeed,
            float pathRefreshInterval = 0.15f)
        {
            worldTime = timeSource;
            moveSpeed = Mathf.Max(0f, speed);
            rotationSpeed = Mathf.Max(1f, turnSpeed);
            repathInterval = Mathf.Max(0.01f, pathRefreshInterval);
        }

        public bool MoveTowards(
            Vector3 destination,
            float stoppingDistance,
            float worldDeltaTime,
            float speedMultiplier = 1f,
            bool rotateToMovement = true)
        {
            IsMoving = false;
            MovementDirection = Vector3.zero;
            float effectiveMoveSpeed =
                moveSpeed * Mathf.Max(0f, speedMultiplier);
            if (body == null ||
                capsule == null ||
                worldDeltaTime <= 0f ||
                effectiveMoveSpeed <= 0f)
            {
                return false;
            }

            Vector3 destinationOffset = EnemyMovementMath.PlanarOffset(
                body.position,
                destination);
            if (EnemyMovementMath.HasReached(
                    destinationOffset,
                    stoppingDistance))
            {
                return true;
            }

            EnsurePath(destination);
            Vector3 steeringTarget = ResolveSteeringTarget(destination);
            Vector3 direction = EnemyMovementMath.PlanarOffset(
                body.position,
                steeringTarget);
            if (direction.sqrMagnitude <= 0.000001f)
            {
                return false;
            }

            direction = ApplySeparation(direction.normalized);
            if (rotateToMovement)
            {
                RotateTowards(
                    body.position + direction,
                    worldDeltaTime);
            }

            float requestedDistance = EnemyMovementMath.RequestedDistance(
                effectiveMoveSpeed,
                worldDeltaTime,
                destinationOffset.magnitude,
                stoppingDistance);
            float movedDistance = MoveWithCollision(
                direction,
                requestedDistance);
            IsMoving = movedDistance > 0.00001f;
            if (IsMoving)
            {
                MovementDirection = direction;
            }
            return destinationOffset.magnitude - movedDistance <=
                   Mathf.Max(0f, stoppingDistance);
        }

        public void RotateTowards(
            Vector3 worldPosition,
            float worldDeltaTime)
        {
            if (body == null || worldDeltaTime <= 0f)
            {
                return;
            }

            Vector3 direction = worldPosition - body.position;
            direction.y = 0f;
            if (direction.sqrMagnitude <= 0.000001f)
            {
                return;
            }

            Quaternion targetRotation =
                Quaternion.LookRotation(direction.normalized, Vector3.up);
            body.MoveRotation(Quaternion.RotateTowards(
                body.rotation,
                targetRotation,
                rotationSpeed * worldDeltaTime));
        }

        public void Stop()
        {
            IsMoving = false;
            MovementDirection = Vector3.zero;
        }

        public bool TryCalculatePathLength(
            Vector3 destination,
            out float pathLength)
        {
            pathLength = 0f;
            if (body == null ||
                !NavMesh.SamplePosition(
                    body.position,
                    out NavMeshHit startHit,
                    1.5f,
                    NavMesh.AllAreas) ||
                !NavMesh.SamplePosition(
                    destination,
                    out NavMeshHit destinationHit,
                    3f,
                    NavMesh.AllAreas))
            {
                return false;
            }

            if (queryPath == null)
            {
                queryPath = new NavMeshPath();
            }

            if (!NavMesh.CalculatePath(
                    startHit.position,
                    destinationHit.position,
                    NavMesh.AllAreas,
                    queryPath) ||
                queryPath.status != NavMeshPathStatus.PathComplete ||
                queryPath.corners.Length < 1)
            {
                return false;
            }

            Vector3 previous = startHit.position;
            for (int i = 0; i < queryPath.corners.Length; i++)
            {
                Vector3 corner = queryPath.corners[i];
                pathLength += Vector3.Distance(previous, corner);
                previous = corner;
            }

            return true;
        }

        public void ClearPath()
        {
            hasPath = false;
            pathCorners = System.Array.Empty<Vector3>();
            cornerIndex = 0;
            IsMoving = false;
        }

        private void EnsurePath(Vector3 destination)
        {
            bool destinationChanged = EnemyMovementMath.DestinationChanged(
                destination,
                lastPathDestination);
            if (hasPath &&
                !destinationChanged &&
                worldTime != null &&
                worldTime.WorldElapsedTime < nextRepathWorldTime)
            {
                return;
            }

            if (path == null)
            {
                path = new NavMeshPath();
            }

            lastPathDestination = destination;
            nextRepathWorldTime =
                (worldTime == null ? 0f : worldTime.WorldElapsedTime) +
                repathInterval;

            if (!NavMesh.SamplePosition(
                    body.position,
                    out NavMeshHit startHit,
                    1.5f,
                    NavMesh.AllAreas) ||
                !NavMesh.SamplePosition(
                    destination,
                    out NavMeshHit destinationHit,
                    3f,
                    NavMesh.AllAreas) ||
                !NavMesh.CalculatePath(
                    startHit.position,
                    destinationHit.position,
                    NavMesh.AllAreas,
                    path) ||
                path.status == NavMeshPathStatus.PathInvalid)
            {
                hasPath = false;
                pathCorners = System.Array.Empty<Vector3>();
                cornerIndex = 0;
                if (!warnedMissingNavMesh)
                {
                    warnedMissingNavMesh = true;
                    Debug.LogWarning(
                        $"{name} could not acquire a NavMesh path; " +
                        "using collision-safe direct steering.",
                        this);
                }

                return;
            }

            pathCorners = path.corners;
            cornerIndex = pathCorners.Length > 1 ? 1 : 0;
            hasPath = pathCorners.Length > 0;
        }

        private Vector3 ResolveSteeringTarget(Vector3 fallback)
        {
            if (!hasPath || pathCorners.Length == 0)
            {
                return fallback;
            }

            while (cornerIndex < pathCorners.Length - 1)
            {
                Vector3 offset =
                    pathCorners[cornerIndex] -
                    body.position;
                offset.y = 0f;
                if (offset.magnitude > cornerReachDistance)
                {
                    break;
                }

                cornerIndex++;
            }

            return pathCorners[Mathf.Clamp(
                cornerIndex,
                0,
                pathCorners.Length - 1)];
        }

        private Vector3 ApplySeparation(Vector3 desiredDirection)
        {
            if (separationRadius <= 0f || separationWeight <= 0f)
            {
                return desiredDirection;
            }

            int count = Physics.OverlapSphereNonAlloc(
                body.position,
                separationRadius,
                separationHits,
                ~0,
                QueryTriggerInteraction.Ignore);
            Vector3 separation = Vector3.zero;

            for (int i = 0; i < count; i++)
            {
                Collider candidate = separationHits[i];
                if (candidate == null ||
                    CombatQuery.BelongsToSource(candidate, gameObject))
                {
                    continue;
                }

                EnemyHealth enemy =
                    candidate.GetComponentInParent<EnemyHealth>();
                if (enemy == null || !enemy.IsAlive)
                {
                    continue;
                }

                Vector3 away =
                    body.position -
                    candidate.bounds.center;
                away.y = 0f;
                float distance = away.magnitude;
                if (distance <= 0.0001f)
                {
                    away = transform.right;
                    distance = 0.0001f;
                }

                separation +=
                    (away / distance) *
                    (1f - Mathf.Clamp01(distance / separationRadius));
            }

            Vector3 combined =
                desiredDirection +
                (separation * separationWeight);
            combined.y = 0f;
            return combined.sqrMagnitude <= 0.000001f
                ? desiredDirection
                : combined.normalized;
        }

        private float MoveWithCollision(
            Vector3 direction,
            float requestedDistance)
        {
            if (requestedDistance <= 0f)
            {
                return 0f;
            }

            GetWorldCapsule(
                out Vector3 pointA,
                out Vector3 pointB,
                out float radius);
            int count = Physics.CapsuleCastNonAlloc(
                pointA,
                pointB,
                Mathf.Max(0.01f, radius - collisionPadding),
                direction,
                movementHits,
                requestedDistance + collisionPadding,
                ~0,
                QueryTriggerInteraction.Ignore);

            float nearestBlockingDistance = float.PositiveInfinity;
            for (int i = 0; i < count; i++)
            {
                Collider candidate = movementHits[i].collider;
                if (candidate == null ||
                    CombatQuery.BelongsToSource(candidate, gameObject))
                {
                    continue;
                }

                if (candidate.bounds.max.y <=
                    capsule.bounds.min.y + 0.08f)
                {
                    continue;
                }

                if (CombatQuery.TryGetDamageable(
                        candidate,
                        out IDamageable damageable) &&
                    damageable.Faction == CombatFaction.Enemy)
                {
                    continue;
                }

                nearestBlockingDistance = Mathf.Min(
                    nearestBlockingDistance,
                    movementHits[i].distance);
            }

            float safeDistance = float.IsPositiveInfinity(
                nearestBlockingDistance)
                ? requestedDistance
                : Mathf.Clamp(
                    nearestBlockingDistance - collisionPadding,
                    0f,
                    requestedDistance);
            if (safeDistance <= 0f)
            {
                return 0f;
            }

            if (groundMovement != null)
            {
                if (!groundMovement.TryMove(
                        body,
                        direction * safeDistance,
                        out float projectedDistance))
                {
                    return 0f;
                }

                TotalDistanceMoved += projectedDistance;
                return projectedDistance;
            }

            body.MovePosition(body.position + (direction * safeDistance));
            TotalDistanceMoved += safeDistance;
            return safeDistance;
        }

        private void GetWorldCapsule(
            out Vector3 pointA,
            out Vector3 pointB,
            out float radius)
        {
            Vector3 scale = capsule.transform.lossyScale;
            Vector3 axis;
            float heightScale;
            float radiusScale;

            switch (capsule.direction)
            {
                case 0:
                    axis = capsule.transform.right;
                    heightScale = Mathf.Abs(scale.x);
                    radiusScale = Mathf.Max(
                        Mathf.Abs(scale.y),
                        Mathf.Abs(scale.z));
                    break;
                case 2:
                    axis = capsule.transform.forward;
                    heightScale = Mathf.Abs(scale.z);
                    radiusScale = Mathf.Max(
                        Mathf.Abs(scale.x),
                        Mathf.Abs(scale.y));
                    break;
                default:
                    axis = capsule.transform.up;
                    heightScale = Mathf.Abs(scale.y);
                    radiusScale = Mathf.Max(
                        Mathf.Abs(scale.x),
                        Mathf.Abs(scale.z));
                    break;
            }

            radius = capsule.radius * radiusScale;
            float height = Mathf.Max(
                capsule.height * heightScale,
                radius * 2f);
            Vector3 center =
                capsule.transform.TransformPoint(capsule.center);
            float halfSegment = Mathf.Max(
                0f,
                (height * 0.5f) - radius);
            pointA = center + (axis * halfSegment);
            pointB = center - (axis * halfSegment);
        }

        private void OnDisable()
        {
            ClearPath();
        }
    }
}
