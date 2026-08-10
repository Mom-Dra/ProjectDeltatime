using System.Collections.Generic;
using UnityEngine;

namespace Deltatime.Replay
{
    /// <summary>
    /// Identifies Animators that are presentation-only replay proxies.
    /// StateMachineBehaviours use this boundary to suppress gameplay callbacks.
    /// </summary>
    public static class ReplayAnimatorProxyRegistry
    {
        private static readonly HashSet<int> AnimatorIds = new HashSet<int>();

        [RuntimeInitializeOnLoadMethod(
            RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetRegistry()
        {
            AnimatorIds.Clear();
        }

        public static bool IsProxy(Animator animator)
        {
            return animator != null && AnimatorIds.Contains(animator.GetInstanceID());
        }

        internal static void Register(Animator animator)
        {
            if (animator != null)
            {
                AnimatorIds.Add(animator.GetInstanceID());
            }
        }

        internal static void Unregister(Animator animator)
        {
            if (animator != null)
            {
                AnimatorIds.Remove(animator.GetInstanceID());
            }
        }
    }
}
