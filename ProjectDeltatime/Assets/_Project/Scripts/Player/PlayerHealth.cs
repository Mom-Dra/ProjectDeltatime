using System;
using Deltatime.Core;
using Deltatime.Visuals;
using UnityEngine;

namespace Deltatime.Player
{
    public sealed class PlayerHealth : MonoBehaviour, IDamageable
    {
        [SerializeField, Min(1)] private int maximumHealth = 3;
        [SerializeField] private Renderer bodyRenderer;
        [SerializeField] private CharacterVisualController characterVisual;
        [SerializeField] private Color aliveColor = new Color(0.1f, 0.95f, 1f, 1f);
        [SerializeField] private Color deadColor = new Color(0.35f, 0.4f, 0.45f, 1f);

        private bool dashInvulnerable;

        public event Action Died;
        public event Action<int, int> HealthChanged;
        public event Action<DamageHit, bool> Damaged;

        public CombatFaction Faction => CombatFaction.Player;
        public int MaximumHealth => maximumHealth;
        public int CurrentHealth { get; private set; }
        public bool IsAlive => CurrentHealth > 0;
        public bool IsInvulnerable => dashInvulnerable;

        private void Awake()
        {
            CurrentHealth = Mathf.Max(1, maximumHealth);
            if (bodyRenderer == null)
            {
                bodyRenderer = GetComponentInChildren<Renderer>();
            }
        }

        public void ReceiveHit(DamageHit hit)
        {
            if (!IsAlive || dashInvulnerable)
            {
                return;
            }

            int damage = Mathf.Max(0, hit.Damage);
            if (damage <= 0)
            {
                return;
            }

            CurrentHealth = Mathf.Max(0, CurrentHealth - damage);
            HealthChanged?.Invoke(CurrentHealth, MaximumHealth);
            Damaged?.Invoke(hit, !IsAlive);
            if (IsAlive)
            {
                return;
            }

            if (bodyRenderer != null)
            {
                bodyRenderer.material.color = deadColor;
            }
            characterVisual?.SetTint(deadColor);

            Died?.Invoke();
        }

        public void SetDashInvulnerable(bool value)
        {
            dashInvulnerable = value;
        }

        public void Configure(Renderer renderer)
        {
            bodyRenderer = renderer;
            maximumHealth = 3;
            CurrentHealth = maximumHealth;
            if (bodyRenderer != null)
            {
                bodyRenderer.sharedMaterial.color = aliveColor;
            }
        }

        public void ConfigureVisual(CharacterVisualController visualController)
        {
            characterVisual = visualController;
            characterVisual?.RestoreTint();
        }
    }
}
