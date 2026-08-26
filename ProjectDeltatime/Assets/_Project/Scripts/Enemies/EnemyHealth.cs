using Deltatime.Core;
using Deltatime.Level;
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
        [SerializeField, Min(0f)] private float deathPresentationDuration = 0.32f;
        [SerializeField, Min(0f)] private float deathDisplacement = 0.22f;
        [SerializeField, Range(0f, 45f)] private float deathLeanAngle = 12f;

        private Material bodyMaterial;
        private Color normalColor;
        private bool normalColorCaptured;
        private bool showingStunColor;
        private bool presentingDeath;
        private float deathPresentationElapsed;
        private Vector3 deathStartPosition;
        private Vector3 deathEndPosition;
        private Quaternion deathStartRotation;
        private Quaternion deathEndRotation;

        public bool IsPresentingDeath => presentingDeath;

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
            if (presentingDeath)
            {
                UpdateDeathPresentation();
                return;
            }

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
                stage.NotifyEnemyDied(this, deathPresentationDuration);
            }

            BeginDeathPresentation(hit.Direction);
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

        private void BeginDeathPresentation(Vector3 attackDirection)
        {
            Vector3 direction = Vector3.ProjectOnPlane(
                attackDirection,
                Vector3.up);
            if (direction.sqrMagnitude <= 0.000001f)
            {
                direction = Vector3.ProjectOnPlane(transform.forward, Vector3.up);
            }

            direction = direction.sqrMagnitude > 0.000001f
                ? direction.normalized
                : Vector3.forward;
            deathStartPosition = transform.position;
            deathEndPosition = deathStartPosition + direction * deathDisplacement;
            deathStartRotation = transform.rotation;
            Vector3 leanAxis = Vector3.Cross(Vector3.up, direction);
            deathEndRotation = Quaternion.AngleAxis(
                deathLeanAngle,
                leanAxis) * deathStartRotation;
            deathPresentationElapsed = 0f;
            presentingDeath = true;

            if (deathPresentationDuration <= 0f)
            {
                Destroy(gameObject);
            }
        }

        private void UpdateDeathPresentation()
        {
            deathPresentationElapsed += UnityEngine.Time.unscaledDeltaTime;
            float progress = deathPresentationDuration <= 0f
                ? 1f
                : Mathf.Clamp01(
                    deathPresentationElapsed / deathPresentationDuration);
            float eased = 1f - Mathf.Pow(1f - progress, 3f);
            transform.SetPositionAndRotation(
                Vector3.LerpUnclamped(
                    deathStartPosition,
                    deathEndPosition,
                    eased),
                Quaternion.SlerpUnclamped(
                    deathStartRotation,
                    deathEndRotation,
                    eased));

            if (progress >= 1f)
            {
                Destroy(gameObject);
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
