using System;
using Deltatime.Core;
using UnityEngine;

namespace Deltatime.Player
{
    public sealed class PlayerHealth : MonoBehaviour, IDamageable
    {
        [SerializeField] private Renderer bodyRenderer;
        [SerializeField] private Color aliveColor = new Color(0.1f, 0.95f, 1f, 1f);
        [SerializeField] private Color deadColor = new Color(0.35f, 0.4f, 0.45f, 1f);

        private bool dashInvulnerable;

        public event Action Died;

        public CombatFaction Faction => CombatFaction.Player;
        public bool IsAlive { get; private set; } = true;
        public bool IsInvulnerable => dashInvulnerable;

        private void Awake()
        {
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

            IsAlive = false;
            if (bodyRenderer != null)
            {
                bodyRenderer.material.color = deadColor;
            }

            Died?.Invoke();
        }

        public void SetDashInvulnerable(bool value)
        {
            dashInvulnerable = value;
        }

        public void Configure(Renderer renderer)
        {
            bodyRenderer = renderer;
            if (bodyRenderer != null)
            {
                bodyRenderer.sharedMaterial.color = aliveColor;
            }
        }
    }
}
