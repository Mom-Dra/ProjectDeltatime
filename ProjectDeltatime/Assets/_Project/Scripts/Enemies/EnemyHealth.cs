using Deltatime.Core;
using Deltatime.Level;
using Deltatime.Utilities;
using Deltatime.Visuals;
using UnityEngine;

namespace Deltatime.Enemies
{
    public sealed class EnemyHealth : MonoBehaviour, IDamageable, IStunnable
    {
        [SerializeField] private EnemyBehavior behavior;
        [SerializeField] private EnemyWeaponDrop weaponDrop;
        [SerializeField] private StageController stage;
        [SerializeField] private Collider bodyCollider;
        [SerializeField] private Renderer bodyRenderer;
        [SerializeField] private CharacterVisualController characterVisual;
        [SerializeField] private bool damageEnabled = true;
        [SerializeField] private Color hitColor = new Color(1f, 0.25f, 0.2f, 1f);
        [SerializeField] private Color stunColor = new Color(1f, 0.78f, 0.15f, 1f);

        private Material bodyMaterial;
        private Color normalColor;
        private bool normalColorCaptured;
        private bool showingStunColor;

        public CombatFaction Faction => CombatFaction.Enemy;
        public bool IsAlive { get; private set; } = true;
        public bool IsStunned =>
            IsAlive &&
            behavior != null &&
            behavior.IsStunned;
        public bool DamageEnabled => damageEnabled;

        private void Awake()
        {
            EnsureBodyMaterial();
        }

        private void OnEnable()
        {
            if (stage != null && IsAlive)
            {
                stage.RegisterEnemy(this);
            }
        }

        private void Update()
        {
            if (showingStunColor && !IsStunned)
            {
                showingStunColor = false;
                RestoreBodyColor();
            }
        }

        public void ReceiveHit(DamageHit hit)
        {
            if (!IsAlive || !damageEnabled)
            {
                return;
            }

            IsAlive = false;
            showingStunColor = false;
            if (behavior != null)
            {
                behavior.SetDead();
            }

            if (bodyCollider != null)
            {
                bodyCollider.enabled = false;
            }

            if (bodyRenderer != null)
            {
                SetBodyColor(hitColor);
            }

            if (weaponDrop != null)
            {
                weaponDrop.Drop();
            }

            if (stage != null)
            {
                stage.NotifyEnemyDied(this);
            }

            HitFlash.Create(hit.Point, hitColor);
            Destroy(gameObject);
        }

        public void ReceiveStun(StunHit hit)
        {
            if (!IsAlive || behavior == null || hit.Duration <= 0f)
            {
                return;
            }

            behavior.ApplyStun(hit.Duration);
            if (!IsStunned)
            {
                return;
            }

            if (weaponDrop != null)
            {
                weaponDrop.Drop();
            }

            behavior.Disarm();
            showingStunColor = true;
            SetBodyColor(stunColor);
            HitFlash.Create(hit.Point, stunColor);
        }

        public void Configure(
            EnemyBehavior enemyBehavior,
            EnemyWeaponDrop drop,
            StageController stageController,
            Collider collider,
            Renderer renderer)
        {
            behavior = enemyBehavior;
            weaponDrop = drop;
            stage = stageController;
            bodyCollider = collider;
            bodyRenderer = renderer;
        }

        public void ConfigureVisual(CharacterVisualController visualController)
        {
            characterVisual = visualController;
        }

        public void SetDamageEnabled(bool value)
        {
            damageEnabled = value;
        }

        private void SetBodyColor(Color color)
        {
            characterVisual?.SetTint(color);
            EnsureBodyMaterial();
            if (bodyMaterial == null)
            {
                return;
            }

            bodyMaterial.color = color;
        }

        private void RestoreBodyColor()
        {
            characterVisual?.RestoreTint();
            EnsureBodyMaterial();
            if (bodyMaterial != null)
            {
                bodyMaterial.color = normalColor;
            }
        }

        private void EnsureBodyMaterial()
        {
            if (bodyRenderer == null)
            {
                return;
            }

            if (bodyMaterial == null)
            {
                bodyMaterial = bodyRenderer.material;
            }

            if (!normalColorCaptured)
            {
                normalColor = bodyMaterial.color;
                normalColorCaptured = true;
            }
        }

        private void OnDisable()
        {
            if (stage != null && IsAlive)
            {
                stage.UnregisterEnemy(this);
            }
        }
    }
}
