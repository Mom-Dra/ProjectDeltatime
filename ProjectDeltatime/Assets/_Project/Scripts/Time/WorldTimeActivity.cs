using UnityEngine;

namespace Deltatime.TimeSystem
{
    public sealed class WorldTimeActivity : MonoBehaviour
    {
        private float movement;
        private float aimTurn;
        private float pulseStrength;
        private float pulseRemaining;

        public float Movement => movement;
        public float AimTurn => aimTurn;
        public float PulseStrength => pulseRemaining > 0f ? pulseStrength : 0f;

        public void SetMovement(float normalizedAmount)
        {
            movement = Mathf.Clamp01(normalizedAmount);
        }

        public void SetAimTurn(float normalizedAmount)
        {
            aimTurn = Mathf.Clamp01(normalizedAmount);
        }

        public void Pulse(float strength, float realDuration)
        {
            if (strength >= pulseStrength || pulseRemaining <= 0f)
            {
                pulseStrength = Mathf.Clamp01(strength);
            }

            pulseRemaining = Mathf.Max(pulseRemaining, realDuration);
        }

        public void AdvanceRealTime(float realDeltaTime)
        {
            if (pulseRemaining <= 0f)
            {
                pulseStrength = 0f;
                return;
            }

            pulseRemaining = Mathf.Max(0f, pulseRemaining - realDeltaTime);
        }
    }
}
