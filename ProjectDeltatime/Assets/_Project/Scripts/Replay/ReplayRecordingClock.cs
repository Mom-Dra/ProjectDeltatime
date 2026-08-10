using UnityEngine;

namespace Deltatime.Replay
{
    /// <summary>
    /// Separates capture ordering from normalized replay presentation time.
    /// Source time always follows unscaled real time. Replay time advances only
    /// by actual world-simulation progress, so variable slow motion requires no
    /// inferred or fixed playback multiplier.
    /// </summary>
    public struct ReplayRecordingClock
    {
        public float SourceElapsedTime { get; private set; }
        public float ReplayElapsedTime { get; private set; }

        public void Advance(float realDeltaTime, float worldDeltaTime)
        {
            SourceElapsedTime += Mathf.Max(0f, realDeltaTime);
            ReplayElapsedTime += Mathf.Max(0f, worldDeltaTime);
        }

        public void Reset()
        {
            SourceElapsedTime = 0f;
            ReplayElapsedTime = 0f;
        }
    }
}
