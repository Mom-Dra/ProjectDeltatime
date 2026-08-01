using UnityEngine;

namespace Deltatime.Combat
{
    public enum WeaponKind
    {
        Firearm,
        Melee
    }

    [CreateAssetMenu(
        fileName = "WeaponDefinition",
        menuName = "Deltatime/Weapon Definition")]
    public sealed class WeaponDefinition : ScriptableObject
    {
        [SerializeField] private WeaponKind kind = WeaponKind.Firearm;
        [SerializeField] private string displayName = "Pistol";
        [SerializeField, Min(0)] private int ammunitionCapacity = 8;
        [SerializeField, Min(0.01f)] private float fireInterval = 0.24f;
        [SerializeField, Min(0.1f)] private float projectileSpeed = 17f;
        [SerializeField, Min(1)] private int damage = 1;
        [SerializeField, Min(0.01f)] private float projectileRadius = 0.08f;

        [Header("Enemy Firearm Use")]
        [SerializeField, Min(1)] private int enemyBurstShotCount = 1;

        [Header("Melee")]
        [SerializeField, Min(0.1f)] private float meleeRange = 1.45f;
        [SerializeField, Range(1f, 90f)] private float meleeHalfAngle = 35f;

        [Header("Prototype Visuals")]
        [SerializeField] private Vector3 heldVisualScale =
            new Vector3(0.18f, 0.16f, 0.78f);
        [SerializeField] private Vector3 worldVisualScale =
            new Vector3(0.82f, 0.16f, 0.26f);
        [SerializeField] private Color visualColor =
            new Color(0.5f, 0.55f, 0.62f, 1f);

        public WeaponKind Kind => kind;
        public string DisplayName => displayName;
        public int AmmunitionCapacity => ammunitionCapacity;
        public float FireInterval => fireInterval;
        public float UseInterval => fireInterval;
        public float ProjectileSpeed => projectileSpeed;
        public int Damage => damage;
        public float ProjectileRadius => projectileRadius;
        public int EnemyBurstShotCount => enemyBurstShotCount;
        public float MeleeRange => meleeRange;
        public float MeleeHalfAngle => meleeHalfAngle;
        public Vector3 HeldVisualScale => heldVisualScale;
        public Vector3 WorldVisualScale => worldVisualScale;
        public Color VisualColor => visualColor;
        public bool IsFirearm => kind == WeaponKind.Firearm;
        public bool IsMelee => kind == WeaponKind.Melee;

        public void ConfigureFirearmPrototype(
            string weaponName,
            int capacity,
            float interval,
            float speed,
            int projectileDamage,
            float radius,
            int burstCount)
        {
            kind = WeaponKind.Firearm;
            displayName = weaponName;
            ammunitionCapacity = Mathf.Max(1, capacity);
            fireInterval = Mathf.Max(0.01f, interval);
            projectileSpeed = Mathf.Max(0.1f, speed);
            damage = Mathf.Max(1, projectileDamage);
            projectileRadius = Mathf.Max(0.01f, radius);
            enemyBurstShotCount = Mathf.Max(1, burstCount);
            heldVisualScale = new Vector3(0.18f, 0.16f, 0.78f);
            worldVisualScale = new Vector3(0.82f, 0.16f, 0.26f);
            visualColor = new Color(0.5f, 0.55f, 0.62f, 1f);
        }

        public void ConfigureMeleePrototype(
            string weaponName,
            float interval,
            int meleeDamage,
            float range,
            float halfAngle)
        {
            kind = WeaponKind.Melee;
            displayName = weaponName;
            ammunitionCapacity = 0;
            fireInterval = Mathf.Max(0.01f, interval);
            damage = Mathf.Max(1, meleeDamage);
            enemyBurstShotCount = 1;
            meleeRange = Mathf.Max(0.1f, range);
            meleeHalfAngle = Mathf.Clamp(halfAngle, 1f, 90f);
            heldVisualScale = new Vector3(0.14f, 0.14f, 1.05f);
            worldVisualScale = new Vector3(0.24f, 0.16f, 1.05f);
            visualColor = new Color(0.9f, 0.42f, 0.08f, 1f);
        }
    }
}
