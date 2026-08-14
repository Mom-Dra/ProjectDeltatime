namespace Deltatime.Tutorial
{
    internal sealed class TutorialProgression
    {
        internal const int TotalStepCount = 7;

        internal TutorialDirector.TutorialStep CurrentStep
        {
            get;
            private set;
        } = TutorialDirector.TutorialStep.TimeMovement;

        internal void MoveTo(TutorialDirector.TutorialStep step)
        {
            CurrentStep = step;
        }
    }

    internal sealed class TutorialThrowRecoveryScenario
    {
        internal bool DropObserved { get; private set; }
        internal bool OutcomeObserved { get; private set; }

        internal void Reset()
        {
            DropObserved = false;
            OutcomeObserved = false;
        }

        internal void ObserveDrop()
        {
            DropObserved = true;
        }

        internal bool TryObserveOutcome(
            bool enemyAlive,
            bool enemyStunned,
            bool enemyDisarmed,
            bool enemyHasWeapon)
        {
            if (OutcomeObserved ||
                !DropObserved ||
                !enemyAlive ||
                !enemyStunned ||
                !enemyDisarmed ||
                enemyHasWeapon)
            {
                return false;
            }

            OutcomeObserved = true;
            return true;
        }
    }

    internal sealed class TutorialDeadlineScenario
    {
        internal bool ActivationObserved { get; private set; }
        internal bool TwoCausesObserved { get; private set; }
        internal bool AiActive { get; private set; }
        internal bool Succeeded { get; private set; }

        internal void Begin()
        {
            ActivationObserved = false;
            TwoCausesObserved = false;
            AiActive = false;
            Succeeded = false;
        }

        internal bool Observe(
            bool deadlineActive,
            int stagedActionCount,
            int maximumStagedActions)
        {
            if (!deadlineActive)
            {
                return false;
            }

            ActivationObserved = true;
            bool shouldActivateAi = !AiActive;
            AiActive = true;
            if (stagedActionCount >= maximumStagedActions)
            {
                TwoCausesObserved = true;
            }

            return shouldActivateAi;
        }

        internal bool TrySucceed()
        {
            if (!ActivationObserved || !TwoCausesObserved)
            {
                return false;
            }

            Succeeded = true;
            return true;
        }

        internal void ResetAttempt()
        {
            AiActive = false;
            ActivationObserved = false;
            TwoCausesObserved = false;
        }
    }

    internal static class TutorialGuidancePolicy
    {
        internal static bool NeedsMovementProof(
            float accumulatedDuration,
            float requiredDuration)
        {
            return accumulatedDuration < requiredDuration;
        }

        internal static bool NeedsAimProof(
            float accumulatedDegrees,
            float requiredDegrees)
        {
            return accumulatedDegrees < requiredDegrees;
        }
    }
}
