using Deltatime.TimeSystem;
using UnityEngine;

namespace Deltatime.Tutorial
{
    public sealed class TutorialTimeProbe : MonoBehaviour
    {
        [SerializeField] private WorldTimeController worldTime;
        [SerializeField, Min(1f)] private float degreesPerWorldSecond = 180f;

        public float AccumulatedDegrees { get; private set; }

        private void Update()
        {
            if (worldTime == null)
            {
                return;
            }

            float degrees = degreesPerWorldSecond * worldTime.WorldDeltaTime;
            AccumulatedDegrees += Mathf.Abs(degrees);
            transform.Rotate(0f, degrees, 0f, Space.Self);
        }

        public void Configure(
            WorldTimeController timeSource,
            float rotationDegreesPerWorldSecond = 180f)
        {
            worldTime = timeSource;
            degreesPerWorldSecond = Mathf.Max(
                1f,
                rotationDegreesPerWorldSecond);
        }
    }
}
