using System.Collections.Generic;
using Deltatime.Combat;
using UnityEngine;

namespace Deltatime.Visuals
{
    /// <summary>
    /// Attached to generated upper-body attack states. It invokes the pending
    /// melee hit once the motion reaches its authored strike point.
    /// </summary>
    public sealed class MeleeAttackImpactBehaviour : StateMachineBehaviour
    {
        [Range(0.05f, 0.95f)]
        [SerializeField] private float impactNormalizedTime = 0.48f;

        private readonly HashSet<int> impactedAnimators = new HashSet<int>();

        public float ImpactNormalizedTime
        {
            get => impactNormalizedTime;
            set => impactNormalizedTime = Mathf.Clamp(value, 0.05f, 0.95f);
        }

        public override void OnStateEnter(
            Animator animator,
            AnimatorStateInfo stateInfo,
            int layerIndex)
        {
            impactedAnimators.Remove(animator.GetInstanceID());
        }

        public override void OnStateUpdate(
            Animator animator,
            AnimatorStateInfo stateInfo,
            int layerIndex)
        {
            int animatorId = animator.GetInstanceID();
            if (stateInfo.normalizedTime < impactNormalizedTime ||
                impactedAnimators.Contains(animatorId))
            {
                return;
            }

            impactedAnimators.Add(animatorId);
            animator.GetComponentInParent<MeleeAttackExecution>()
                ?.TryApplyImpact();
        }

        public override void OnStateExit(
            Animator animator,
            AnimatorStateInfo stateInfo,
            int layerIndex)
        {
            impactedAnimators.Remove(animator.GetInstanceID());
        }
    }
}
