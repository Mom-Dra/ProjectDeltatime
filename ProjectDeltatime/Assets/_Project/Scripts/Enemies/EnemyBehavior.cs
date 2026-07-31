using UnityEngine;

namespace Deltatime.Enemies
{
    public abstract class EnemyBehavior : MonoBehaviour
    {
        public enum BehaviorStatus
        {
            Active,
            Stunned,
            Disarmed,
            Dead
        }

        private float stunTimeRemaining;
        private bool isDisarmed;

        public BehaviorStatus Status { get; private set; } =
            BehaviorStatus.Active;
        public bool IsStunned => Status == BehaviorStatus.Stunned;
        public bool IsDisarmed => isDisarmed;
        public bool IsDead => Status == BehaviorStatus.Dead;
        public float StunTimeRemaining =>
            IsStunned
                ? Mathf.Max(0f, stunTimeRemaining)
                : 0f;

        public void SetDead()
        {
            if (IsDead)
            {
                return;
            }

            Status = BehaviorStatus.Dead;
            stunTimeRemaining = 0f;
            OnDead();
            enabled = false;
        }

        public void ApplyStun(float worldDuration)
        {
            if (IsDead || worldDuration <= 0f)
            {
                return;
            }

            bool enteredStun = !IsStunned;
            Status = BehaviorStatus.Stunned;
            stunTimeRemaining = Mathf.Max(
                stunTimeRemaining,
                worldDuration);

            if (enteredStun)
            {
                OnStunned();
            }
        }

        public void Disarm()
        {
            if (IsDead)
            {
                return;
            }

            if (!isDisarmed)
            {
                isDisarmed = true;
                OnDisarmed();
            }

            if (!IsStunned)
            {
                Status = BehaviorStatus.Disarmed;
            }
        }

        protected bool AdvanceStatus(float worldDeltaTime)
        {
            if (Status == BehaviorStatus.Stunned)
            {
                stunTimeRemaining = Mathf.Max(
                    0f,
                    stunTimeRemaining - Mathf.Max(0f, worldDeltaTime));
                if (stunTimeRemaining <= 0f)
                {
                    Status = isDisarmed
                        ? BehaviorStatus.Disarmed
                        : BehaviorStatus.Active;
                    OnStunRecovered();
                }
            }

            return Status == BehaviorStatus.Active;
        }

        protected virtual void OnStunned()
        {
        }

        protected virtual void OnStunRecovered()
        {
        }

        protected virtual void OnDisarmed()
        {
        }

        protected virtual void OnDead()
        {
        }
    }
}
