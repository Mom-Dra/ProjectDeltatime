using Deltatime.Combat;
using UnityEngine;

namespace Deltatime.Enemies
{
    internal sealed class EnemyCombatStateController
    {
        internal EnemyCombatant.CombatState CurrentState { get; private set; } =
            EnemyCombatant.CombatState.Detecting;

        internal EnemyCombatant.MovementMode CurrentMovementMode
        {
            get;
            private set;
        } = EnemyCombatant.MovementMode.Stopped;

        private float stateTimeRemaining;

        internal void TransitionTo(
            EnemyCombatant.CombatState nextState,
            float duration)
        {
            CurrentState = nextState;
            stateTimeRemaining = Mathf.Max(0f, duration);
        }

        internal void SetState(EnemyCombatant.CombatState state)
        {
            CurrentState = state;
        }

        internal void SetMovementMode(
            EnemyCombatant.MovementMode movementMode)
        {
            CurrentMovementMode = movementMode;
        }

        internal bool AdvanceStateTimer(float deltaTime)
        {
            stateTimeRemaining -= deltaTime;
            return stateTimeRemaining <= 0f;
        }
    }

    internal enum FirearmRangeDecision
    {
        Pursue,
        Retreat,
        Hold
    }

    internal static class EnemyFirearmRangePolicy
    {
        internal static FirearmRangeDecision Decide(
            float distance,
            float preferredMinimumRange,
            float preferredMaximumRange)
        {
            if (distance > preferredMaximumRange)
            {
                return FirearmRangeDecision.Pursue;
            }

            return distance < preferredMinimumRange
                ? FirearmRangeDecision.Retreat
                : FirearmRangeDecision.Hold;
        }
    }

    internal static class EnemyWeaponSearchPolicy
    {
        internal static WeaponPickup Select(
            WeaponPickup firearm,
            float firearmPathLength,
            WeaponPickup melee,
            float meleePathLength,
            float firearmPathTolerance)
        {
            if (melee != null &&
                (firearm == null ||
                 firearmPathLength >=
                 meleePathLength + firearmPathTolerance))
            {
                return melee;
            }

            return firearm;
        }
    }

    internal sealed class EnemyWarningPresenter
    {
        private LineRenderer line;

        internal void Bind(LineRenderer warningLine)
        {
            line = warningLine;
        }

        internal bool IsVisible => line != null && line.enabled;

        internal void SetVisible(bool visible)
        {
            if (line != null)
            {
                line.enabled = visible;
            }
        }

        internal void Update(Vector3 origin, Vector3 target)
        {
            if (!IsVisible)
            {
                return;
            }

            line.positionCount = 2;
            line.SetPosition(0, origin);
            line.SetPosition(1, target);
        }
    }
}
