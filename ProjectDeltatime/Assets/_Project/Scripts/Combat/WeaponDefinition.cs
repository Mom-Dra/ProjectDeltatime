using UnityEngine;

namespace Deltatime.Combat
{
    [CreateAssetMenu(
        fileName = "WeaponDefinition",
        menuName = "Deltatime/Weapon Definition")]
    public sealed class WeaponDefinition : ScriptableObject
    {
        [SerializeField] private string displayName = "Pistol";
        [SerializeField, Min(1)] private int ammunitionCapacity = 8;
        [SerializeField, Min(0.01f)] private float fireInterval = 0.24f;
        [SerializeField, Min(0.1f)] private float projectileSpeed = 17f;
        [SerializeField, Min(1)] private int damage = 1;
        [SerializeField, Min(0.01f)] private float projectileRadius = 0.08f;

        public string DisplayName => displayName;
        public int AmmunitionCapacity => ammunitionCapacity;
        public float FireInterval => fireInterval;
        public float ProjectileSpeed => projectileSpeed;
        public int Damage => damage;
        public float ProjectileRadius => projectileRadius;

        public void ConfigurePrototype(
            string weaponName,
            int capacity,
            float interval,
            float speed,
            int projectileDamage,
            float radius)
        {
            displayName = weaponName;
            ammunitionCapacity = Mathf.Max(1, capacity);
            fireInterval = Mathf.Max(0.01f, interval);
            projectileSpeed = Mathf.Max(0.1f, speed);
            damage = Mathf.Max(1, projectileDamage);
            projectileRadius = Mathf.Max(0.01f, radius);
        }
    }
}
