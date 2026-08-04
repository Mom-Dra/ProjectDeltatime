using UnityEngine;

namespace Deltatime.Replay
{
    /// <summary>
    /// Marks a static hierarchy that should remain live during replay instead
    /// of creating one recorded proxy track per renderer.
    /// </summary>
    public sealed class ReplayExcluded : MonoBehaviour
    {
    }
}
