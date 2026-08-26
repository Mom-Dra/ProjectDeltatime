using UnityEngine;

namespace Deltatime.Replay
{
    /// <summary>
    /// Marks a static hierarchy that should remain live during replay instead
    /// of creating one recorded proxy track per renderer. A nearer
    /// <see cref="ReplayIncluded"/> can opt a dynamic subtree back in.
    /// </summary>
    public sealed class ReplayExcluded : MonoBehaviour
    {
    }
}
