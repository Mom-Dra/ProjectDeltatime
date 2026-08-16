using Deltatime.Combat;
using UnityEngine;

namespace Deltatime.Player
{
    internal enum PlayerWeaponInteractionPrompt
    {
        None,
        PickUp,
        Swap,
        Catch
    }

    internal static class PlayerAttackDecision
    {
        internal static bool ShouldUseWeapon(
            bool firePressed,
            bool fireHeld,
            WeaponDefinition definition)
        {
            return firePressed ||
                   (fireHeld && definition != null && definition.IsAutomatic);
        }
    }

    internal static class PlayerWeaponInteractionSelector
    {
        internal static InterceptableWeapon FindNearestAirborne(
            Collider[] results,
            int count,
            Vector3 origin)
        {
            InterceptableWeapon nearest = null;
            float nearestDistanceSquared = float.PositiveInfinity;
            for (int i = 0; i < count; i++)
            {
                Collider result = results[i];
                InterceptableWeapon candidate = result == null
                    ? null
                    : result.GetComponentInParent<InterceptableWeapon>();
                if (candidate == null || !candidate.IsCatchable)
                {
                    continue;
                }

                float distanceSquared =
                    (candidate.transform.position - origin).sqrMagnitude;
                if (distanceSquared < nearestDistanceSquared)
                {
                    nearestDistanceSquared = distanceSquared;
                    nearest = candidate;
                }
            }

            return nearest;
        }

        internal static WeaponPickup FindNearestGround(
            Collider[] results,
            int count,
            Vector3 origin)
        {
            WeaponPickup nearest = null;
            float nearestDistanceSquared = float.PositiveInfinity;
            for (int i = 0; i < count; i++)
            {
                Collider result = results[i];
                WeaponPickup candidate = result == null
                    ? null
                    : result.GetComponentInParent<WeaponPickup>();
                if (candidate == null || candidate.Definition == null)
                {
                    continue;
                }

                float distanceSquared =
                    (candidate.transform.position - origin).sqrMagnitude;
                if (distanceSquared < nearestDistanceSquared)
                {
                    nearestDistanceSquared = distanceSquared;
                    nearest = candidate;
                }
            }

            return nearest;
        }
    }

    internal static class PlayerWeaponInteractionPromptPolicy
    {
        internal static PlayerWeaponInteractionPrompt Resolve(
            bool hasAirborneWeapon,
            bool hasGroundWeapon,
            bool hasEquippedWeapon)
        {
            if (hasAirborneWeapon)
            {
                return PlayerWeaponInteractionPrompt.Catch;
            }

            if (!hasGroundWeapon)
            {
                return PlayerWeaponInteractionPrompt.None;
            }

            return hasEquippedWeapon
                ? PlayerWeaponInteractionPrompt.Swap
                : PlayerWeaponInteractionPrompt.PickUp;
        }
    }
}
