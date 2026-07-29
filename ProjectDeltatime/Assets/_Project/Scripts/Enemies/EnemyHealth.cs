using Deltatime.Core;
using Deltatime.Level;
using Deltatime.Utilities;
using UnityEngine;

namespace Deltatime.Enemies
{
    public sealed class EnemyHealth : MonoBehaviour, IDamageable
    {
        [SerializeField] private EnemyShooter shooter;
        [SerializeField] private EnemyWeaponDrop weaponDrop;
        [SerializeField] private StageController stage;
        [SerializeField] private Collider bodyCollider;
        [SerializeField] private Renderer bodyRenderer;
        [SerializeField] private Color hitColor = new Color(1f, 0.25f, 0.2f, 1f);

        public CombatFaction Faction => CombatFaction.Enemy;
        public bool IsAlive { get; private set; } = true;

        private void OnEnable()
        {
            if (stage != null && IsAlive)
            {
                stage.RegisterEnemy(this);
            }
        }

        public void ReceiveHit(DamageHit hit)
        {
            if (!IsAlive)
            {
                return;
            }

            IsAlive = false;
            if (shooter != null)
            {
                shooter.SetDead();
            }

            if (bodyCollider != null)
            {
                bodyCollider.enabled = false;
            }

            if (bodyRenderer != null)
            {
                bodyRenderer.material.color = hitColor;
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

        public void Configure(
            EnemyShooter enemyShooter,
            EnemyWeaponDrop drop,
            StageController stageController,
            Collider collider,
            Renderer renderer)
        {
            shooter = enemyShooter;
            weaponDrop = drop;
            stage = stageController;
            bodyCollider = collider;
            bodyRenderer = renderer;
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
