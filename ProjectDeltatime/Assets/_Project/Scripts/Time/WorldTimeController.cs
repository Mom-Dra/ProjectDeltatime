using UnityEngine;

namespace Deltatime.TimeSystem
{
    [DefaultExecutionOrder(-300)]
    public sealed class WorldTimeController : MonoBehaviour
    {
        [Header("Source")]
        [SerializeField] private WorldTimeActivity activity;

        [Header("Scale")]
        [SerializeField, Range(0f, 1f)] private float minimumTimeScale = 0.02f;
        [SerializeField, Range(0.01f, 2f)] private float maximumTimeScale = 1f;
        [SerializeField, Min(0.01f)] private float interpolationSpeed = 8f;

        [Header("Activity Weights")]
        [SerializeField, Min(0f)] private float movementWeight = 1f;
        [SerializeField, Min(0f)] private float aimTurnWeight = 1f;
        [SerializeField, Min(0f)] private float pulseWeight = 1f;

        public float CurrentTimeScale { get; private set; }
        public float TargetTimeScale { get; private set; }
        public float WorldDeltaTime { get; private set; }
        public float WorldElapsedTime { get; private set; }
        public float RealDeltaTime { get; private set; }

        private void Awake()
        {
            ValidateConfiguration();
            CurrentTimeScale = minimumTimeScale;
            TargetTimeScale = minimumTimeScale;
        }

        private void Update()
        {
            RealDeltaTime = UnityEngine.Time.unscaledDeltaTime;

            float activityAmount = 0f;
            if (activity != null)
            {
                activityAmount =
                    (activity.Movement * movementWeight) +
                    (activity.AimTurn * aimTurnWeight) +
                    (activity.PulseStrength * pulseWeight);
            }

            TargetTimeScale = Mathf.Lerp(
                minimumTimeScale,
                maximumTimeScale,
                Mathf.Clamp01(activityAmount));

            float blend = 1f - Mathf.Exp(-interpolationSpeed * RealDeltaTime);
            CurrentTimeScale = Mathf.Lerp(CurrentTimeScale, TargetTimeScale, blend);
            WorldDeltaTime = RealDeltaTime * CurrentTimeScale;
            WorldElapsedTime += WorldDeltaTime;

            if (activity != null)
            {
                activity.AdvanceRealTime(RealDeltaTime);
            }
        }

        public void Configure(WorldTimeActivity source)
        {
            activity = source;
        }

        private void OnValidate()
        {
            minimumTimeScale = Mathf.Max(0f, minimumTimeScale);
            maximumTimeScale = Mathf.Max(minimumTimeScale, maximumTimeScale);
            interpolationSpeed = Mathf.Max(0.01f, interpolationSpeed);
        }

        private void ValidateConfiguration()
        {
            if (activity == null)
            {
                Debug.LogError($"{nameof(WorldTimeController)} requires a {nameof(WorldTimeActivity)} reference.", this);
                enabled = false;
            }
        }
    }
}
