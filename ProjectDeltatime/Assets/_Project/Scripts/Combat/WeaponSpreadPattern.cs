using UnityEngine;

namespace Deltatime.Combat
{
    /// <summary>
    /// Creates deterministic firearm directions without using Unity's global
    /// random state. Multi-pellet shots fill a circular cone cross-section.
    /// </summary>
    public static class WeaponSpreadPattern
    {
        private const uint PatternRotationChannelSalt = 0x68BC21EBu;
        private const uint RadialJitterChannelSalt = 0x165667B1u;
        private const uint AzimuthJitterChannelSalt = 0xA4F1D2C3u;
        private const float GoldenAngleDegrees = 137.507764f;

        public static Vector3 GetProjectileDirection(
            Vector3 baseDirection,
            int projectileIndex,
            int projectileCount,
            float coneAngle,
            float maximumSpreadJitterAngle,
            int spreadSeed,
            int currentShotSequence)
        {
            Vector3 normalizedDirection = baseDirection.sqrMagnitude > 0.0001f
                ? baseDirection.normalized
                : Vector3.forward;
            int clampedProjectileCount = Mathf.Max(1, projectileCount);
            int clampedProjectileIndex = Mathf.Clamp(
                projectileIndex,
                0,
                clampedProjectileCount - 1);
            float maximumConeAngle = Mathf.Max(0f, coneAngle * 0.5f);
            float radialAngle = 0f;
            if (clampedProjectileCount > 1 && maximumConeAngle > 0f)
            {
                // sqrt keeps pellet density even across the cone's circular cross-section.
                float normalizedRadius = Mathf.Sqrt(
                    (clampedProjectileIndex + 0.5f) /
                    clampedProjectileCount);
                radialAngle = maximumConeAngle * normalizedRadius;
            }

            float radialJitterSample = GetDeterministicSpreadSample(
                spreadSeed,
                currentShotSequence,
                clampedProjectileIndex,
                RadialJitterChannelSalt);
            if (maximumConeAngle > 0f)
            {
                radialAngle = Mathf.Clamp(
                    radialAngle +
                    ((radialJitterSample * 2f - 1f) *
                     maximumSpreadJitterAngle),
                    0f,
                    maximumConeAngle);
            }
            else if (maximumSpreadJitterAngle > 0f)
            {
                radialAngle = maximumSpreadJitterAngle *
                    Mathf.Sqrt(radialJitterSample);
            }

            float patternRotation = GetDeterministicSpreadSample(
                spreadSeed,
                currentShotSequence,
                0,
                PatternRotationChannelSalt) * 360f;
            float azimuthJitter = (GetDeterministicSpreadSample(
                spreadSeed,
                currentShotSequence,
                clampedProjectileIndex,
                AzimuthJitterChannelSalt) * 2f - 1f) * 12f;
            float azimuth = patternRotation +
                (clampedProjectileIndex * GoldenAngleDegrees) +
                azimuthJitter;

            return GetDirectionInCircularCone(
                normalizedDirection,
                radialAngle,
                azimuth);
        }

        private static Vector3 GetDirectionInCircularCone(
            Vector3 forward,
            float radialAngle,
            float azimuth)
        {
            Vector3 referenceUp = Mathf.Abs(Vector3.Dot(forward, Vector3.up))
                > 0.999f
                ? Vector3.forward
                : Vector3.up;
            Vector3 right = Vector3.Cross(referenceUp, forward).normalized;
            Vector3 up = Vector3.Cross(forward, right).normalized;
            float azimuthRadians = azimuth * Mathf.Deg2Rad;
            Vector3 radialDirection =
                (right * Mathf.Cos(azimuthRadians)) +
                (up * Mathf.Sin(azimuthRadians));
            float radialRadians = radialAngle * Mathf.Deg2Rad;
            return ((forward * Mathf.Cos(radialRadians)) +
                    (radialDirection * Mathf.Sin(radialRadians))).normalized;
        }

        private static float GetDeterministicSpreadSample(
            int spreadSeed,
            int currentShotSequence,
            int projectileIndex,
            uint channelSalt)
        {
            uint state = (uint)spreadSeed;
            state += (uint)currentShotSequence * 0x9E3779B9u;
            state += (uint)projectileIndex * 0x85EBCA6Bu;
            state += channelSalt;
            state ^= state >> 16;
            state *= 0x7FEB352Du;
            state ^= state >> 15;
            state *= 0x846CA68Bu;
            state ^= state >> 16;

            return (state & 0x00FFFFFFu) / 16777215f;
        }
    }
}
