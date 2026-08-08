using System;
using Deltatime.Combat;
using Deltatime.Core;
using UnityEngine;

namespace Deltatime.Tutorial
{
    [RequireComponent(typeof(Collider))]
    public sealed class TutorialTargetDummy : MonoBehaviour, IDamageable
    {
        public enum AcceptedAttack
        {
            Melee,
            Firearm
        }

        [SerializeField] private AcceptedAttack acceptedAttack;
        [SerializeField] private Renderer bodyRenderer;
        [SerializeField] private Color idleColor = new Color(0.18f, 0.65f, 0.78f, 1f);
        [SerializeField] private Color acceptedColor = new Color(0.2f, 1f, 0.35f, 1f);
        [SerializeField] private Color rejectedColor = new Color(1f, 0.25f, 0.15f, 1f);
        [SerializeField, Min(0.01f)] private float feedbackDuration = 0.28f;

        private Material bodyMaterial;
        private float feedbackRemaining;

        public event Action<TutorialTargetDummy> Accepted;

        public CombatFaction Faction => CombatFaction.Enemy;
        public bool IsAlive => true;
        public AcceptedAttack RequiredAttack => acceptedAttack;
        public int AcceptedHitCount { get; private set; }
        public int RejectedHitCount { get; private set; }

        private void Awake()
        {
            if (bodyRenderer == null)
            {
                bodyRenderer = GetComponentInChildren<Renderer>();
            }

            if (bodyRenderer != null)
            {
                bodyMaterial = bodyRenderer.material;
                bodyMaterial.color = idleColor;
            }
        }

        private void Update()
        {
            if (feedbackRemaining <= 0f)
            {
                return;
            }

            feedbackRemaining = Mathf.Max(
                0f,
                feedbackRemaining - UnityEngine.Time.unscaledDeltaTime);
            if (feedbackRemaining <= 0f && bodyMaterial != null)
            {
                bodyMaterial.color = idleColor;
            }
        }

        public void ReceiveHit(DamageHit hit)
        {
            WeaponController sourceWeapon = hit.Source == null
                ? null
                : hit.Source.GetComponentInParent<WeaponController>();
            WeaponDefinition definition = sourceWeapon == null
                ? null
                : sourceWeapon.Definition;
            bool accepted = acceptedAttack == AcceptedAttack.Melee
                ? definition != null && definition.IsMelee
                : definition != null && definition.IsFirearm;

            feedbackRemaining = feedbackDuration;
            if (!accepted)
            {
                RejectedHitCount++;
                if (bodyMaterial != null)
                {
                    bodyMaterial.color = rejectedColor;
                }

                return;
            }

            AcceptedHitCount++;
            if (bodyMaterial != null)
            {
                bodyMaterial.color = acceptedColor;
            }

            Accepted?.Invoke(this);
        }

        public void Configure(
            AcceptedAttack requiredAttack,
            Renderer renderer)
        {
            acceptedAttack = requiredAttack;
            bodyRenderer = renderer;
        }
    }
}
