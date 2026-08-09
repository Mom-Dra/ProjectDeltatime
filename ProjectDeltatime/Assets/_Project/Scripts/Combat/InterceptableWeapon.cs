using Deltatime.TimeSystem;
using Deltatime.Utilities;
using Deltatime.Visuals;
using UnityEngine;

namespace Deltatime.Combat
{
    public sealed class InterceptableWeapon : MonoBehaviour
    {
        private const float PickupHeight = 0.18f;
        private const float FlightEndHeight = 0.55f;

        [Header("Flight")]
        [SerializeField, Min(0.1f)] private float flightWorldDuration = 0.85f;
        [SerializeField, Min(0.1f)] private float horizontalDistance = 3f;
        [SerializeField, Min(0f)] private float arcHeight = 1.25f;
        [SerializeField, Min(0f)] private float rotationSpeed = 900f;
        [SerializeField, Min(0.01f)] private float collisionRadius = 0.25f;
        [SerializeField] private LayerMask collisionLayers;

        [Header("Prediction")]
        [SerializeField, Range(4, 32)] private int predictionPointCount = 16;

        [Header("Visuals")]
        [SerializeField] private LineRenderer trail;
        [SerializeField] private LineRenderer predictionLine;
        [SerializeField] private Renderer landingMarkerRenderer;
        [SerializeField] private Renderer bodyRenderer;
        [SerializeField, Min(0f)] private float maximumTrailLength = 1.2f;
        [SerializeField] private Color catchColor =
            new Color(1f, 0.72f, 0.08f, 1f);

        private WorldTimeController worldTime;
        private WeaponPickup pickupPrefab;
        private WeaponDefinition definition;
        private Vector3 startPosition;
        private Vector3 endPosition;
        private Vector3 trailStart;
        private float flightProgress;
        private int ammunition;
        private bool initialized;
        private bool resolved;
        private WeaponFlightVisualPresenter flightVisual;

        public WeaponDefinition Definition => definition;
        public int Ammunition => ammunition;
        public bool IsCatchable => initialized && !resolved;

        private void Awake()
        {
            if (trail == null ||
                predictionLine == null ||
                landingMarkerRenderer == null)
            {
                Debug.LogError(
                    $"{nameof(InterceptableWeapon)} is missing visual references.",
                    this);
                enabled = false;
                return;
            }

            predictionLine.enabled = false;
            landingMarkerRenderer.enabled = false;

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
            if (deltaTime > 0f)
            {
                AdvanceFlight(deltaTime);
            }

            if (!resolved)
            {
                UpdateTrail();
                UpdatePrediction();
            }
        }

        public void Initialize(
            WorldTimeController timeSource,
            WeaponPickup groundPickupPrefab,
            WeaponDefinition weaponDefinition,
            int remainingAmmunition,
            Vector3 travelDirection)
        {
            worldTime = timeSource;
            pickupPrefab = groundPickupPrefab;
            definition = weaponDefinition;
            ammunition = definition == null
                ? 0
                : Mathf.Clamp(
                    remainingAmmunition,
                    0,
                    definition.AmmunitionCapacity);

            Vector3 horizontalDirection = travelDirection;
            horizontalDirection.y = 0f;
            horizontalDirection = horizontalDirection.sqrMagnitude > 0.0001f
                ? horizontalDirection.normalized
                : Vector3.forward;

            startPosition = transform.position;
            endPosition =
                startPosition +
                (horizontalDirection * horizontalDistance);
            endPosition.y = FlightEndHeight;
            trailStart = startPosition;
            flightProgress = 0f;
            initialized =
                enabled &&
                worldTime != null &&
                pickupPrefab != null &&
                definition != null;

            ApplyWeaponVisual();

            UpdateTrail();
            UpdatePrediction();

            if (!initialized)
            {
                Debug.LogError(
                    $"{nameof(InterceptableWeapon)} was spawned without required data.",
                    this);
                Destroy(gameObject);
            }
        }

        public bool TryCatch(WeaponController collector)
        {
            if (!IsCatchable || collector == null)
            {
                return false;
            }

            resolved = true;

            WeaponDefinition previousDefinition = collector.Definition;
            int previousAmmunition = collector.Ammunition;
            collector.Equip(definition, ammunition);

            if (previousDefinition != null)
            {
                Vector3 dropPosition = collector.transform.position;
                dropPosition.y = PickupHeight;
                WeaponPickup previousPickup = Instantiate(
                    pickupPrefab,
                    dropPosition,
                    Quaternion.identity);
                previousPickup.Initialize(
                    previousDefinition,
                    previousAmmunition);
            }

            HidePrediction();
            HitFlash.Create(transform.position, catchColor);
            Destroy(gameObject);
            return true;
        }

        public void ConfigureVisuals(
            LineRenderer trailRenderer,
            LineRenderer trajectoryRenderer,
            Renderer landingRenderer,
            Renderer weaponBodyRenderer,
            LayerMask obstacleLayers)
        {
            trail = trailRenderer;
            predictionLine = trajectoryRenderer;
            landingMarkerRenderer = landingRenderer;
            bodyRenderer = weaponBodyRenderer;
            collisionLayers = obstacleLayers;
        }

        private void ApplyWeaponVisual()
        {
            if (definition == null)
            {
                return;
            }

            if (flightVisual == null)
            {
                flightVisual = GetComponent<WeaponFlightVisualPresenter>();
            }

            flightVisual.Apply(definition, bodyRenderer);
            transform.localScale = Vector3.one;
            if (!flightVisual.HasCustomModel && bodyRenderer != null)
            {
                bodyRenderer.transform.localScale = definition.WorldVisualScale;
                bodyRenderer.material.color = definition.VisualColor;
            }
        }

        private void AdvanceFlight(float deltaTime)
        {
            float duration = Mathf.Max(0.0001f, flightWorldDuration);
            float nextProgress = Mathf.Clamp01(
                flightProgress + (deltaTime / duration));
            Vector3 currentPosition = transform.position;
            Vector3 nextPosition = EvaluatePosition(nextProgress);

            if (TryFindObstacle(
                    currentPosition,
                    nextPosition,
                    out Vector3 safePosition))
            {
                Settle(safePosition);
                return;
            }

            transform.position = nextPosition;
            transform.Rotate(
                rotationSpeed * 0.65f * deltaTime,
                rotationSpeed * deltaTime,
                rotationSpeed * 0.4f * deltaTime,
                Space.World);
            flightProgress = nextProgress;

            if (flightProgress >= 1f)
            {
                Settle(endPosition);
            }
        }

        private Vector3 EvaluatePosition(float progress)
        {
            float t = Mathf.Clamp01(progress);
            Vector3 position = Vector3.Lerp(startPosition, endPosition, t);
            position.y += 4f * arcHeight * t * (1f - t);
            return position;
        }

        private bool TryFindObstacle(
            Vector3 origin,
            Vector3 destination,
            out Vector3 safePosition)
        {
            Vector3 offset = destination - origin;
            float distance = offset.magnitude;
            safePosition = destination;
            if (distance <= 0.0001f || collisionLayers.value == 0)
            {
                return false;
            }

            if (!Physics.SphereCast(
                    origin,
                    collisionRadius,
                    offset / distance,
                    out RaycastHit hit,
                    distance,
                    collisionLayers,
                    QueryTriggerInteraction.Ignore))
            {
                return false;
            }

            safePosition =
                origin +
                ((offset / distance) * Mathf.Max(0f, hit.distance - 0.02f));
            return true;
        }

        private void Settle(Vector3 position)
        {
            if (resolved)
            {
                return;
            }

            resolved = true;
            Vector3 pickupPosition = position;
            pickupPosition.y = PickupHeight;
            WeaponPickup pickup = Instantiate(
                pickupPrefab,
                pickupPosition,
                Quaternion.identity);
            pickup.Initialize(definition, ammunition);

            HidePrediction();
            HitFlash.Create(position, catchColor);
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
            Vector3 tailEnd = Vector3.MoveTowards(
                head,
                trailStart,
                maximumTrailLength);
            trail.SetPosition(0, head);
            trail.SetPosition(1, tailEnd);
        }

        private void UpdatePrediction()
        {
            if (predictionLine == null ||
                landingMarkerRenderer == null ||
                worldTime == null)
            {
                return;
            }

            predictionLine.enabled = true;
            landingMarkerRenderer.enabled = true;

            int requestedCount = Mathf.Max(4, predictionPointCount);
            predictionLine.positionCount = requestedCount;

            int actualCount = 1;
            Vector3 previous = transform.position;
            Vector3 landingPosition = endPosition;
            predictionLine.SetPosition(0, previous);

            for (int i = 1; i < requestedCount; i++)
            {
                float sampleBlend = i / (requestedCount - 1f);
                float sampleProgress = Mathf.Lerp(
                    flightProgress,
                    1f,
                    sampleBlend);
                Vector3 sample = EvaluatePosition(sampleProgress);

                if (TryFindObstacle(previous, sample, out Vector3 safePosition))
                {
                    predictionLine.SetPosition(actualCount, safePosition);
                    actualCount++;
                    landingPosition = safePosition;
                    break;
                }

                predictionLine.SetPosition(actualCount, sample);
                actualCount++;
                previous = sample;
                landingPosition = sample;
            }

            predictionLine.positionCount = actualCount;
            landingPosition.y = 0.03f;
            landingMarkerRenderer.transform.position = landingPosition;
            landingMarkerRenderer.transform.rotation = Quaternion.identity;
        }

        private void HidePrediction()
        {
            if (predictionLine != null)
            {
                predictionLine.enabled = false;
            }

            if (landingMarkerRenderer != null)
            {
                landingMarkerRenderer.enabled = false;
            }
        }
    }
}
