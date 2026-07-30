using System;
using System.Collections.Generic;
using Deltatime.Combat;
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
        [SerializeField] private PlayerHealth health;
        [SerializeField] private PlayerCombat combat;
        [SerializeField] private WorldTimeController worldTime;

        [Header("Trigger")]
        [SerializeField, Min(0.1f)] private float dangerRadius = 1.5f;
        [SerializeField, Min(0.01f)] private float maximumImpactWorldTime = 0.15f;
        [SerializeField, Range(0f, 1f)] private float movementThreshold = 0.05f;
        [SerializeField, Min(0f)] private float rearmWorldDuration = 0.35f;

        [Header("Simultaneous Release")]
        [SerializeField, Min(1)] private int maximumStagedActions = 2;

        private Projectile currentThreat;
        private float impactWorldTime = float.PositiveInfinity;
        private float nextReadyWorldTime;
        private float rejectedFeedbackRemaining;
        private int hardFreezeToken;
        private int stagedActionCount;
        private bool wasMoving;

        public event Action Released;

        public bool IsActive { get; private set; }
        public bool ReleasedThisFrame { get; private set; }
        public bool HasThreat =>
            currentThreat != null && currentThreat.IsActive;
        public bool IsReady =>
            !IsActive &&
            worldTime != null &&
            worldTime.WorldElapsedTime >= nextReadyWorldTime;
        public bool CanStageAction =>
            IsActive && stagedActionCount < maximumStagedActions;
        public bool RejectedActionFeedback =>
            rejectedFeedbackRemaining > 0f;
        public float ImpactTime => impactWorldTime;
        public float CooldownRemaining =>
            worldTime == null
                ? 0f
                : Mathf.Max(0f, nextReadyWorldTime - worldTime.WorldElapsedTime);
        public int StagedActionCount => stagedActionCount;
        public int MaxStagedActions => maximumStagedActions;

        private void Awake()
        {
            ValidateConfiguration();
        }

        private void Start()
        {
            wasMoving = IsMovementInputActive();
        }

        private void Update()
        {
            ReleasedThisFrame = false;
            rejectedFeedbackRemaining = Mathf.Max(
                0f,
                rejectedFeedbackRemaining - UnityEngine.Time.unscaledDeltaTime);

            bool isMoving = IsMovementInputActive();
            if (health == null ||
                !health.IsAlive ||
                health.IsInvulnerable ||
                combat == null ||
                !combat.CombatEnabled)
            {
                AbortDeadline();
                wasMoving = isMoving;
                return;
            }

            if (IsActive)
            {
                if (isMoving)
                {
                    ReleaseDeadline();
                }

                wasMoving = isMoving;
                return;
            }

            if (worldTime.IsHardFrozen || !IsReady)
            {
                ClearThreat();
                wasMoving = isMoving;
                return;
            }

            FindImminentThreat();
            bool stoppedThisFrame = wasMoving && !isMoving;
            if (stoppedThisFrame &&
                currentThreat != null &&
                currentThreat.TryClaimDeadline())
            {
                ActivateDeadline();
            }

            wasMoving = isMoving;
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
            PlayerHealth playerHealth,
            PlayerCombat playerCombat,
            WorldTimeController timeSource)
        {
            input = inputReader;
            health = playerHealth;
            combat = playerCombat;
            worldTime = timeSource;
        }

        private void FindImminentThreat()
        {
            Projectile nearestThreat = null;
            float nearestImpactTime = float.PositiveInfinity;
            float dangerRadiusSquared = dangerRadius * dangerRadius;
            IReadOnlyList<Projectile> projectiles =
                Projectile.ActiveProjectiles;

            for (int i = 0; i < projectiles.Count; i++)
            {
                Projectile projectile = projectiles[i];
                if (projectile == null ||
                    !projectile.CanTriggerDeadline ||
                    projectile.Faction != CombatFaction.Enemy)
                {
                    continue;
                }

                Vector3 offset = projectile.transform.position - transform.position;
                offset.y = 0f;
                if (offset.sqrMagnitude > dangerRadiusSquared ||
                    !projectile.TryPredictImpact(
                        gameObject,
                        maximumImpactWorldTime,
                        out float candidateImpactTime) ||
                    candidateImpactTime >= nearestImpactTime)
                {
                    continue;
                }

                nearestThreat = projectile;
                nearestImpactTime = candidateImpactTime;
            }

            SetThreat(nearestThreat, nearestImpactTime);
        }

        private void SetThreat(
            Projectile threat,
            float threatImpactWorldTime)
        {
            if (currentThreat != threat && currentThreat != null)
            {
                currentThreat.SetDeadlineHighlighted(false);
            }

            currentThreat = threat;
            impactWorldTime = threat == null
                ? float.PositiveInfinity
                : threatImpactWorldTime;

            if (currentThreat != null)
            {
                currentThreat.SetDeadlineHighlighted(true);
            }
        }

        private void ActivateDeadline()
        {
            IsActive = true;
            stagedActionCount = 0;
            rejectedFeedbackRemaining = 0f;
            hardFreezeToken = worldTime.AcquireHardFreeze();
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
            ClearThreat();
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
            ClearThreat();

            if (wasActive)
            {
                Released?.Invoke();
            }
        }

        private void ClearThreat()
        {
            if (currentThreat != null)
            {
                currentThreat.SetDeadlineHighlighted(false);
            }

            currentThreat = null;
            impactWorldTime = float.PositiveInfinity;
        }

        private bool IsMovementInputActive()
        {
            return input != null &&
                   input.Move.sqrMagnitude >
                   movementThreshold * movementThreshold;
        }

        private void OnDisable()
        {
            AbortDeadline();
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0.12f, 0.06f, 0.65f);
            Gizmos.DrawWireSphere(transform.position, dangerRadius);
        }

        private void ValidateConfiguration()
        {
            if (input == null ||
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
