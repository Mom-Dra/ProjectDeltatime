using System;
using Deltatime.Core;
using Deltatime.InputSystem;
using Deltatime.TimeSystem;
using UnityEngine;

namespace Deltatime.Player
{
    [DefaultExecutionOrder(-350)]
    public sealed class DeadlineController : MonoBehaviour
    {
        [SerializeField] private PlayerInputReader input;
        [SerializeField] private PlayerMovement movement;
        [SerializeField] private PlayerHealth health;
        [SerializeField] private PlayerCombat combat;
        [SerializeField] private WorldTimeController worldTime;

        [SerializeField, Min(0f)] private float rearmWorldDuration = 0.35f;

        [Header("Charges")]
        [SerializeField, Min(1)] private int maximumCharges = 2;

        [Header("Simultaneous Release")]
        [SerializeField, Min(1)] private int maximumStagedActions = 2;

        private float nextReadyWorldTime;
        private float rejectedFeedbackRemaining;
        private int hardFreezeToken;
        private int chargesRemaining;
        private int stagedActionCount;
        private const float ReleaseMovementThreshold = 0.05f;

        public event Action Released;

        public bool IsActive { get; private set; }
        public bool ReleasedThisFrame { get; private set; }
        public bool HasCharges => chargesRemaining > 0;
        public bool IsReady =>
            !IsActive &&
            HasCharges &&
            worldTime != null &&
            worldTime.WorldElapsedTime >= nextReadyWorldTime;
        public bool CanStageAction =>
            IsActive && stagedActionCount < maximumStagedActions;
        public bool RejectedActionFeedback =>
            rejectedFeedbackRemaining > 0f;
        public float CooldownRemaining =>
            worldTime == null
                ? 0f
                : Mathf.Max(0f, nextReadyWorldTime - worldTime.WorldElapsedTime);
        public int ChargesRemaining => chargesRemaining;
        public int MaxCharges => maximumCharges;
        public int StagedActionCount => stagedActionCount;
        public int MaxStagedActions => maximumStagedActions;

        private void Awake()
        {
            maximumCharges = Mathf.Max(1, maximumCharges);
            chargesRemaining = maximumCharges;
            ValidateConfiguration();
        }

        private void Update()
        {
            ReleasedThisFrame = false;
            rejectedFeedbackRemaining = Mathf.Max(
                0f,
                rejectedFeedbackRemaining - UnityEngine.Time.unscaledDeltaTime);

            if (health == null ||
                !health.IsAlive ||
                health.IsInvulnerable ||
                movement == null ||
                combat == null ||
                !combat.CombatEnabled)
            {
                AbortDeadline();
                return;
            }

            if (IsActive)
            {
                if (IsMovementInputActive())
                {
                    ReleaseDeadline();
                }

                return;
            }

            if (!input.DeadlinePressed ||
                !IsReady ||
                worldTime.IsHardFrozen)
            {
                return;
            }

            ActivateDeadline();
        }

        public bool RegisterStagedAction()
        {
            if (!CanStageAction)
            {
                NotifyActionRejected();
                return false;
            }

            stagedActionCount++;
            return true;
        }

        public void NotifyActionRejected()
        {
            rejectedFeedbackRemaining = Mathf.Max(
                rejectedFeedbackRemaining,
                0.18f);
        }

        public void Configure(
            PlayerInputReader inputReader,
            PlayerMovement playerMovement,
            PlayerHealth playerHealth,
            PlayerCombat playerCombat,
            WorldTimeController timeSource,
            int deadlineMaximumCharges)
        {
            input = inputReader;
            movement = playerMovement;
            health = playerHealth;
            combat = playerCombat;
            worldTime = timeSource;
            SetMaximumCharges(deadlineMaximumCharges);
        }

        public void SetMaximumCharges(int deadlineMaximumCharges)
        {
            maximumCharges = Mathf.Max(1, deadlineMaximumCharges);
        }

        private void ActivateDeadline()
        {
            IsActive = true;
            stagedActionCount = 0;
            rejectedFeedbackRemaining = 0f;
            hardFreezeToken = worldTime.AcquireHardFreeze(
                allowMinimumTimeScaleDuringAim: true);
            chargesRemaining = Mathf.Max(0, chargesRemaining - 1);
        }

        private void ReleaseDeadline()
        {
            if (!IsActive)
            {
                return;
            }

            if (hardFreezeToken != 0)
            {
                worldTime.ReleaseHardFreeze(hardFreezeToken);
                hardFreezeToken = 0;
            }

            IsActive = false;
            ReleasedThisFrame = true;
            nextReadyWorldTime =
                worldTime.WorldElapsedTime + rearmWorldDuration;
            Released?.Invoke();
        }

        private void AbortDeadline()
        {
            bool wasActive = IsActive;
            if (hardFreezeToken != 0 && worldTime != null)
            {
                worldTime.ReleaseHardFreeze(hardFreezeToken);
                hardFreezeToken = 0;
            }

            IsActive = false;
            stagedActionCount = 0;
            if (wasActive)
            {
                Released?.Invoke();
            }
        }

        private bool IsMovementInputActive()
        {
            return input != null &&
                   input.Move.sqrMagnitude >
                   ReleaseMovementThreshold * ReleaseMovementThreshold;
        }

        private void OnDisable()
        {
            AbortDeadline();
        }

        private void OnValidate()
        {
            maximumCharges = Mathf.Max(1, maximumCharges);
        }

        private void ValidateConfiguration()
        {
            if (input == null ||
                movement == null ||
                health == null ||
                combat == null ||
                worldTime == null)
            {
                Debug.LogError(
                    $"{nameof(DeadlineController)} is missing required references.",
                    this);
                enabled = false;
            }
        }
    }
}
