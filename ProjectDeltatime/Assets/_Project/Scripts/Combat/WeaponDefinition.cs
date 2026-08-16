using Deltatime.Visuals;
using UnityEngine;

namespace Deltatime.Combat
{
    public enum WeaponKind
    {
        Firearm,
        Melee
    }

    public enum WeaponFireMode
    {
        SemiAutomatic,
        Automatic
    }

    [CreateAssetMenu(
        fileName = "WeaponDefinition",
        menuName = "Deltatime/Weapon Definition")]
    public sealed class WeaponDefinition : ScriptableObject
    {
        [SerializeField] private WeaponKind kind = WeaponKind.Firearm;
        [SerializeField] private string displayName = "권총";
        [SerializeField] private CharacterAnimationStyle animationStyle =
            CharacterAnimationStyle.Pistol;
        [SerializeField, Min(0)] private int ammunitionCapacity = 8;
        [SerializeField, Min(0.01f)] private float fireInterval = 0.24f;
        [SerializeField, Min(0.1f)] private float projectileSpeed = 17f;
        [SerializeField, Min(1)] private int damage = 1;
        [SerializeField, Min(0.01f)] private float projectileRadius = 0.08f;
        [SerializeField] private WeaponFireMode fireMode =
            WeaponFireMode.SemiAutomatic;
        [SerializeField, Min(1)] private int projectileCount = 1;
        [SerializeField, Range(0f, 180f)] private float spreadAngle;
        [SerializeField, Range(0f, 45f)] private float spreadJitterAngle;
        [SerializeField] private int spreadSeed = 1;

        [Header("Player Recoil")]
        [SerializeField, Min(0f)] private float playerRecoilDistance;

        [Header("Projectile Range")]
        [SerializeField, Min(0f)] private float maximumProjectileDistance;

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

        [Header("HUD")]
        [SerializeField] private Sprite hudIcon;

        [Header("Model Visuals")]
        [SerializeField] private GameObject heldVisualPrefab;
        [SerializeField] private GameObject worldVisualPrefab;
        [SerializeField] private Vector3 heldModelLocalPosition;
        [SerializeField] private Vector3 heldModelLocalEulerAngles;
        [SerializeField] private Vector3 heldModelLocalScale = Vector3.one;
        [SerializeField] private Vector3 worldModelLocalPosition;
        [SerializeField] private Vector3 worldModelLocalEulerAngles;
        [SerializeField] private Vector3 worldModelLocalScale = Vector3.one;

        [Header("Held Muzzle")]
        [SerializeField] private Vector3 heldMuzzleLocalPosition;
        [SerializeField] private Vector3 heldMuzzleLocalEulerAngles;
        [SerializeField] private bool hasHeldMuzzleCalibration;

        public WeaponKind Kind => kind;
        public string DisplayName => displayName;
        public CharacterAnimationStyle AnimationStyle => animationStyle;
        public int AmmunitionCapacity => ammunitionCapacity;
        public float FireInterval => fireInterval;
        public float UseInterval => fireInterval;
        public float ProjectileSpeed => projectileSpeed;
        public int Damage => damage;
        public float ProjectileRadius => projectileRadius;
        public WeaponFireMode FireMode => fireMode;
        public int ProjectileCount => projectileCount;
        public float SpreadAngle => spreadAngle;
        public float SpreadJitterAngle => spreadJitterAngle;
        public int SpreadSeed => spreadSeed;
        public float PlayerRecoilDistance => playerRecoilDistance;
        public float MaximumProjectileDistance => maximumProjectileDistance;
        public int EnemyBurstShotCount => enemyBurstShotCount;
        public float MeleeRange => meleeRange;
        public float MeleeHalfAngle => meleeHalfAngle;
        public Vector3 HeldVisualScale => heldVisualScale;
        public Vector3 WorldVisualScale => worldVisualScale;
        public Color VisualColor => visualColor;
        public Sprite HudIcon => hudIcon;
        public GameObject HeldVisualPrefab => heldVisualPrefab;
        public GameObject WorldVisualPrefab => worldVisualPrefab;
        public Vector3 HeldModelLocalPosition => heldModelLocalPosition;
        public Vector3 HeldModelLocalEulerAngles => heldModelLocalEulerAngles;
        public Vector3 HeldModelLocalScale => heldModelLocalScale;
        public Vector3 WorldModelLocalPosition => worldModelLocalPosition;
        public Vector3 WorldModelLocalEulerAngles => worldModelLocalEulerAngles;
        public Vector3 WorldModelLocalScale => worldModelLocalScale;
        public Vector3 HeldMuzzleLocalPosition => heldMuzzleLocalPosition;
        public Vector3 HeldMuzzleLocalEulerAngles => heldMuzzleLocalEulerAngles;
        public bool HasHeldMuzzleCalibration => hasHeldMuzzleCalibration;
        public bool HasCustomHeldVisual => heldVisualPrefab != null;
        public bool HasCustomWorldVisual => worldVisualPrefab != null;
        public bool IsFirearm => kind == WeaponKind.Firearm;
        public bool IsMelee => kind == WeaponKind.Melee;
        public bool IsAutomatic =>
            IsFirearm && fireMode == WeaponFireMode.Automatic;

        public void SetDisplayName(string weaponName)
        {
            if (string.IsNullOrWhiteSpace(weaponName))
            {
                throw new System.ArgumentException(
                    "Weapon display name cannot be empty.",
                    nameof(weaponName));
            }

            displayName = weaponName;
        }

        public void ConfigureFirearmPrototype(
            string weaponName,
            int capacity,
            float interval,
            float speed,
            int projectileDamage,
            float radius,
            int burstCount,
            WeaponFireMode mode,
            int pellets,
            float totalSpreadAngle,
            float maximumSpreadJitterAngle,
            int deterministicSpreadSeed,
            float recoilDistance,
            float projectileMaximumDistance,
            CharacterAnimationStyle firearmAnimationStyle =
                CharacterAnimationStyle.Rifle)
        {
            kind = WeaponKind.Firearm;
            displayName = weaponName;
            animationStyle = firearmAnimationStyle;
            ammunitionCapacity = Mathf.Max(1, capacity);
            fireInterval = Mathf.Max(0.01f, interval);
            projectileSpeed = Mathf.Max(0.1f, speed);
            damage = Mathf.Max(1, projectileDamage);
            projectileRadius = Mathf.Max(0.01f, radius);
            fireMode = mode;
            projectileCount = Mathf.Max(1, pellets);
            spreadAngle = Mathf.Clamp(totalSpreadAngle, 0f, 180f);
            spreadJitterAngle = Mathf.Clamp(
                maximumSpreadJitterAngle,
                0f,
                45f);
            spreadSeed = deterministicSpreadSeed;
            playerRecoilDistance = Mathf.Max(0f, recoilDistance);
            maximumProjectileDistance = Mathf.Max(
                0f,
                projectileMaximumDistance);
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
            animationStyle = CharacterAnimationStyle.Melee;
            ammunitionCapacity = 0;
            fireInterval = Mathf.Max(0.01f, interval);
            damage = Mathf.Max(1, meleeDamage);
            fireMode = WeaponFireMode.SemiAutomatic;
            projectileCount = 1;
            spreadAngle = 0f;
            spreadJitterAngle = 0f;
            spreadSeed = 0;
            playerRecoilDistance = 0f;
            maximumProjectileDistance = 0f;
            enemyBurstShotCount = 1;
            meleeRange = Mathf.Max(0.1f, range);
            meleeHalfAngle = Mathf.Clamp(halfAngle, 1f, 90f);
            heldVisualScale = new Vector3(0.14f, 0.14f, 1.05f);
            worldVisualScale = new Vector3(0.24f, 0.16f, 1.05f);
            visualColor = new Color(0.9f, 0.42f, 0.08f, 1f);
        }

        public void ConfigureModelVisuals(
            GameObject heldPrefab,
            GameObject worldPrefab,
            Vector3 heldPosition,
            Vector3 heldEulerAngles,
            Vector3 heldScale,
            Vector3 worldPosition,
            Vector3 worldEulerAngles,
            Vector3 worldScale,
            Vector3 muzzlePosition,
            Vector3 muzzleEulerAngles)
        {
            heldVisualPrefab = heldPrefab;
            worldVisualPrefab = worldPrefab;
            heldModelLocalPosition = heldPosition;
            heldModelLocalEulerAngles = heldEulerAngles;
            heldModelLocalScale = SanitizeScale(heldScale);
            worldModelLocalPosition = worldPosition;
            worldModelLocalEulerAngles = worldEulerAngles;
            worldModelLocalScale = SanitizeScale(worldScale);
            ConfigureHeldMuzzle(muzzlePosition, muzzleEulerAngles);
        }

        public void ConfigureModelVisualPrefabs(
            GameObject heldPrefab,
            GameObject worldPrefab)
        {
            heldVisualPrefab = heldPrefab;
            worldVisualPrefab = worldPrefab;
        }

        public void ConfigureHeldMuzzle(
            Vector3 localPosition,
            Vector3 localEulerAngles)
        {
            heldMuzzleLocalPosition = localPosition;
            heldMuzzleLocalEulerAngles = localEulerAngles;
            hasHeldMuzzleCalibration = true;
        }

        public void MarkModelVisualsCalibrated()
        {
            hasHeldMuzzleCalibration = true;
        }

        private static Vector3 SanitizeScale(Vector3 value)
        {
            return new Vector3(
                Mathf.Max(0.001f, value.x),
                Mathf.Max(0.001f, value.y),
                Mathf.Max(0.001f, value.z));
        }
    }
}
