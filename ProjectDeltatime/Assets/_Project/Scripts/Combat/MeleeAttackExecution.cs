using System.Collections.Generic;
using Deltatime.Core;
using Deltatime.Visuals;
using UnityEngine;

namespace Deltatime.Combat
{
    /// <summary>
    /// Stores a close-range attack until its Animator state reaches the impact
    /// frame. Actors without the generated attack Animator still resolve the
    /// hit immediately, so gameplay remains valid in non-character scenes.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class MeleeAttackExecution : MonoBehaviour
    {
        private const float AnimationFallbackDelay = 0.65f;

        private readonly Queue<AttackRequest> queuedAttacks =
            new Queue<AttackRequest>();

        private CharacterAnimationController animationController;
        private AttackRequest currentAttack;
        private bool hasCurrentAttack;
        private bool awaitingAnimatorImpact;
        private float fallbackRemaining;

        public bool HasPendingAttack => hasCurrentAttack ||
                                        queuedAttacks.Count > 0;

        private void Awake()
        {
            animationController =
                GetComponent<CharacterAnimationController>();
        }

        private void Update()
        {
            if (!awaitingAnimatorImpact)
            {
                return;
            }

            fallbackRemaining -= Time.unscaledDeltaTime;
            if (fallbackRemaining <= 0f)
            {
                TryApplyImpact();
            }
        }

        public bool BeginAttack(
            GameObject source,
            CombatFaction faction,
            Vector3 direction,
            float range,
            float halfAngle,
            int damage,
            MeleeImpactKind impactKind = MeleeImpactKind.Punch)
        {
            direction.y = 0f;
            if (source == null ||
                direction.sqrMagnitude <= 0.000001f ||
                range <= 0f ||
                damage <= 0)
            {
                return false;
            }

            AttackRequest request = new AttackRequest(
                source,
                faction,
                direction.normalized,
                range,
                halfAngle,
                damage,
                impactKind);
            if (hasCurrentAttack)
            {
                queuedAttacks.Enqueue(request);
                return true;
            }

            StartAttack(request);
            return true;
        }

        public bool TryApplyImpact()
        {
            if (!hasCurrentAttack)
            {
                return false;
            }

            AttackRequest resolvedAttack = currentAttack;
            hasCurrentAttack = false;
            awaitingAnimatorImpact = false;
            fallbackRemaining = 0f;

            MeleeAttackResolver.TryHitNearest(
                resolvedAttack.Source,
                resolvedAttack.Faction,
                resolvedAttack.Direction,
                resolvedAttack.Range,
                resolvedAttack.HalfAngle,
                resolvedAttack.Damage,
                resolvedAttack.ImpactKind);

            if (queuedAttacks.Count > 0)
            {
                StartAttack(queuedAttacks.Dequeue());
            }

            return true;
        }

        public void CancelPendingAttacks()
        {
            hasCurrentAttack = false;
            awaitingAnimatorImpact = false;
            fallbackRemaining = 0f;
            queuedAttacks.Clear();
        }

        private void StartAttack(AttackRequest request)
        {
            currentAttack = request;
            hasCurrentAttack = true;
            if (animationController == null)
            {
                animationController =
                    GetComponent<CharacterAnimationController>();
            }

            awaitingAnimatorImpact =
                animationController != null &&
                animationController.TryPlayMeleeAttackAnimation();
            if (!awaitingAnimatorImpact)
            {
                TryApplyImpact();
                return;
            }

            // This only resolves an improperly configured controller. Normal
            // attacks are applied by MeleeAttackImpactBehaviour at its exact
            // normalized animation time.
            fallbackRemaining = AnimationFallbackDelay;
        }

        private readonly struct AttackRequest
        {
            public AttackRequest(
                GameObject source,
                CombatFaction faction,
                Vector3 direction,
                float range,
                float halfAngle,
                int damage,
                MeleeImpactKind impactKind)
            {
                Source = source;
                Faction = faction;
                Direction = direction;
                Range = range;
                HalfAngle = halfAngle;
                Damage = damage;
                ImpactKind = impactKind;
            }

            public GameObject Source { get; }
            public CombatFaction Faction { get; }
            public Vector3 Direction { get; }
            public float Range { get; }
            public float HalfAngle { get; }
            public int Damage { get; }
            public MeleeImpactKind ImpactKind { get; }
        }
    }
}
