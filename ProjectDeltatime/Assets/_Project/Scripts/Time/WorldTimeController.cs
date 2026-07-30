using System.Collections.Generic;
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
        public bool IsHardFrozen =>
            hardFreezeRemaining > 0f || hardFreezeTokens.Count > 0;
        public float HardFreezeRemaining =>
            Mathf.Max(0f, hardFreezeRemaining);

        private readonly HashSet<int> hardFreezeTokens = new HashSet<int>();
        private float smoothedTimeScale;
        private float hardFreezeRemaining;
        private int nextHardFreezeToken = 1;

        private void Awake()
        {
            ValidateConfiguration();
            smoothedTimeScale = minimumTimeScale;
            CurrentTimeScale = smoothedTimeScale;
            TargetTimeScale = minimumTimeScale;
        }

        private void Update()
        {
            RealDeltaTime = UnityEngine.Time.unscaledDeltaTime;
            bool hardFrozen = IsHardFrozen;

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
            smoothedTimeScale = Mathf.Lerp(
                smoothedTimeScale,
                TargetTimeScale,
                blend);
            CurrentTimeScale = hardFrozen
                ? 0f
                : smoothedTimeScale;
            WorldDeltaTime = RealDeltaTime * CurrentTimeScale;
            WorldElapsedTime += WorldDeltaTime;

            if (activity != null)
            {
                activity.AdvanceRealTime(RealDeltaTime);
            }

            if (hardFreezeRemaining > 0f)
            {
                hardFreezeRemaining = Mathf.Max(
                    0f,
                    hardFreezeRemaining - RealDeltaTime);
            }
        }

        public void Configure(WorldTimeActivity source)
        {
            activity = source;
        }

        public void RequestHardFreeze(float realDuration)
        {
            if (realDuration <= 0f)
            {
                return;
            }

            hardFreezeRemaining = Mathf.Max(
                hardFreezeRemaining,
                realDuration);
            CurrentTimeScale = 0f;
            WorldDeltaTime = 0f;
        }

        public int AcquireHardFreeze()
        {
            int token = nextHardFreezeToken;
            nextHardFreezeToken =
                nextHardFreezeToken == int.MaxValue
                    ? 1
                    : nextHardFreezeToken + 1;

            while (token == 0 || hardFreezeTokens.Contains(token))
            {
                token = nextHardFreezeToken;
                nextHardFreezeToken =
                    nextHardFreezeToken == int.MaxValue
                        ? 1
                        : nextHardFreezeToken + 1;
            }

            hardFreezeTokens.Add(token);
            CurrentTimeScale = 0f;
            WorldDeltaTime = 0f;
            return token;
        }

        public bool ReleaseHardFreeze(int token)
        {
            if (token == 0)
            {
                return false;
            }

            return hardFreezeTokens.Remove(token);
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

        private void OnDisable()
        {
            hardFreezeTokens.Clear();
            hardFreezeRemaining = 0f;
        }
    }
}
