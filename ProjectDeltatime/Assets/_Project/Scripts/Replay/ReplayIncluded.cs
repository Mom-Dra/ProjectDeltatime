using UnityEngine;

namespace Deltatime.Replay
{
    /// <summary>
    /// Opts a dynamic subtree back into renderer recording when it lives below
    /// a static <see cref="ReplayExcluded"/> hierarchy.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ReplayIncluded : MonoBehaviour
    {
    }

    internal static class ReplayHierarchyPolicy
    {
        internal static bool IsExcluded(Component source)
        {
            if (source == null)
            {
                return false;
            }

            Transform current = source.transform;
            while (current != null)
            {
                // Inclusion wins when both markers are on the same Transform.
                if (current.TryGetComponent(out ReplayIncluded _))
                {
                    return false;
                }

                if (current.TryGetComponent(out ReplayExcluded _))
                {
                    return true;
                }

                current = current.parent;
            }

            return false;
        }
    }
}
