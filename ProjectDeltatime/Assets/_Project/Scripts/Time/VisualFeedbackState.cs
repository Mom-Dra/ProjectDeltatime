using UnityEngine;

namespace Deltatime.TimeSystem
{
    internal static class WorldTimeVisualState
    {
        internal static Color TargetBackground(
            Color nearlyStopped,
            Color active,
            float timeScale)
        {
            return Color.Lerp(
                nearlyStopped,
                active,
                Mathf.Clamp01(timeScale));
        }

        internal static float ExponentialBlend(
            float blendSpeed,
            float unscaledDeltaTime)
        {
            return 1f - Mathf.Exp(-blendSpeed * unscaledDeltaTime);
        }

        internal static float SlowAmount(float timeScale)
        {
            return 1f - Mathf.Clamp01(timeScale);
        }
    }

    internal readonly struct DeadlineRingVisualState
    {
        internal DeadlineRingVisualState(
            float radius,
            float strength,
            float flashStrength)
        {
            Radius = radius;
            Strength = strength;
            FlashStrength = flashStrength;
        }

        internal float Radius { get; }
        internal float Strength { get; }
        internal float FlashStrength { get; }
    }

    internal static class DeadlineVisualState
    {
        internal static float PhaseProgress(
            DeadlineVisualFeedback.VisualPhase phase,
            float phaseElapsed,
            float enterDuration,
            float releaseDuration)
        {
            if (phase == DeadlineVisualFeedback.VisualPhase.Entering)
            {
                return Mathf.Clamp01(phaseElapsed / enterDuration);
            }

            if (phase == DeadlineVisualFeedback.VisualPhase.Releasing)
            {
                return Mathf.Clamp01(phaseElapsed / releaseDuration);
            }

            return phase == DeadlineVisualFeedback.VisualPhase.Active
                ? 1f
                : 0f;
        }

        internal static DeadlineRingVisualState EvaluateRing(
            DeadlineVisualFeedback.VisualPhase phase,
            float progress)
        {
            if (phase == DeadlineVisualFeedback.VisualPhase.Entering)
            {
                return new DeadlineRingVisualState(
                    Mathf.Lerp(1.08f, 0.12f, progress),
                    Mathf.Sin(progress * Mathf.PI),
                    Mathf.Pow(1f - progress, 3f) * 0.34f);
            }

            if (phase == DeadlineVisualFeedback.VisualPhase.Releasing)
            {
                float wave = Mathf.Sin(progress * Mathf.PI);
                return new DeadlineRingVisualState(
                    Mathf.Lerp(0.08f, 1.16f, progress),
                    wave,
                    wave * 0.08f);
            }

            return new DeadlineRingVisualState(0f, 0f, 0f);
        }
    }
}
