using Deltatime.Combat;
using Deltatime.Core;
using Deltatime.Player;
using Deltatime.TimeSystem;
using Deltatime.Utilities;
using UnityEngine;

namespace Deltatime.Visuals
{
    [DisallowMultipleComponent]
    public sealed class CombatFeedbackController : MonoBehaviour
    {
        private const float PunchPositionImpulse = 0.05f;
        private const float PunchRotationImpulse = 0.15f;
        private const float PunchImpulseDuration = 0.09f;
        private const float PunchHitStopDuration = 0.03f;
        private const float BatPositionImpulse = 0.08f;
        private const float BatRotationImpulse = 0.24f;
        private const float BatImpulseDuration = 0.12f;
        private const float BatHitStopDuration = 0.05f;
        private const float PlayerHitPositionImpulse = 0.07f;
        private const float PlayerHitRotationImpulse = 0.2f;
        private const float PlayerHitImpulseDuration = 0.12f;
        private const float PlayerHitStopDuration = 0.04f;
        private const float DamageFlashDuration = 0.18f;
        private const int DamageTextureSize = 64;

        private static readonly Color PlayerImpactColor =
            new Color(0.2f, 1f, 1f, 1f);
        private static readonly Color EnemyImpactColor =
            new Color(1f, 0.2f, 0.2f, 1f);

        [SerializeField] private WorldTimeController worldTime;
        [SerializeField] private TopDownCameraController cameraController;
        [SerializeField] private PlayerHealth playerHealth;

        private Texture2D damageTexture;
        private float damageFlashRemaining;
        private float damageFlashPeakAlpha;

        public static CombatFeedbackController Active { get; private set; }
        public bool IsDamageFlashActive => damageFlashRemaining > 0f;
        public int WeaponFeedbackCount { get; private set; }
        public int ImpactFeedbackCount { get; private set; }
        public int PlayerDamageFeedbackCount { get; private set; }

        private void Awake()
        {
            ResolveReferences();
            EnsureDamageTexture();
        }

        private void OnEnable()
        {
            Active = this;
            ResolveReferences();
            BindPlayerHealth();
        }

        private void Update()
        {
            damageFlashRemaining = Mathf.Max(
                0f,
                damageFlashRemaining - UnityEngine.Time.unscaledDeltaTime);
        }

        private void OnGUI()
        {
            if (!Application.isPlaying ||
                damageFlashRemaining <= 0f ||
                damageTexture == null)
            {
                return;
            }

            float progress = Mathf.Clamp01(
                damageFlashRemaining / DamageFlashDuration);
            int previousDepth = GUI.depth;
            Color previousColor = GUI.color;
            GUI.depth = -900;
            GUI.color = new Color(1f, 1f, 1f, damageFlashPeakAlpha * progress);
            GUI.DrawTexture(
                new Rect(0f, 0f, Screen.width, Screen.height),
                damageTexture,
                ScaleMode.StretchToFill,
                true);
            GUI.color = previousColor;
            GUI.depth = previousDepth;
        }

        public void Configure(
            WorldTimeController timeSource,
            TopDownCameraController targetCamera,
            PlayerHealth health)
        {
            UnbindPlayerHealth();
            worldTime = timeSource;
            cameraController = targetCamera;
            playerHealth = health;
            EnsureDamageTexture();
            if (isActiveAndEnabled)
            {
                Active = this;
                BindPlayerHealth();
            }
        }

        public static void ReportWeaponFired(
            WeaponDefinition definition,
            CombatFaction sourceFaction,
            Transform muzzle)
        {
            if (definition == null || !definition.IsFirearm || muzzle == null)
            {
                return;
            }

            if (definition.MuzzleFlashSize > 0f)
            {
                MuzzleFlash.Create(
                    muzzle,
                    definition.VisualColor,
                    definition.MuzzleFlashSize);
            }

            CombatFeedbackController active = Active;
            if (active == null)
            {
                return;
            }

            active.WeaponFeedbackCount++;
            if (sourceFaction == CombatFaction.Player)
            {
                active.PlayDefinitionImpulse(definition);
            }
        }

        public static void ReportImpact(
            WeaponDefinition definition,
            CombatFaction sourceFaction,
            CombatFaction targetFaction,
            Vector3 point,
            Vector3 direction,
            bool hitDamageable,
            MeleeImpactKind? meleeImpactKind = null)
        {
            if (!hitDamageable && targetFaction != CombatFaction.Neutral)
            {
                return;
            }

            Color color = sourceFaction == CombatFaction.Player
                ? PlayerImpactColor
                : EnemyImpactColor;
            HitFlash.Create(point, color, direction);

            CombatFeedbackController active = Active;
            if (active == null)
            {
                return;
            }

            active.ImpactFeedbackCount++;
            if (!hitDamageable ||
                sourceFaction != CombatFaction.Player ||
                targetFaction != CombatFaction.Enemy)
            {
                return;
            }

            float hitStopDuration = definition == null
                ? ResolveFallbackHitStop(meleeImpactKind)
                : definition.ImpactHitStopDuration;
            active.worldTime?.RequestHardFreeze(hitStopDuration);

            if (meleeImpactKind.HasValue)
            {
                if (definition != null)
                {
                    active.PlayDefinitionImpulse(definition);
                }
                else
                {
                    active.PlayFallbackMeleeImpulse(meleeImpactKind.Value);
                }
            }
        }

        private void HandlePlayerDamaged(DamageHit hit, bool lethal)
        {
            PlayerDamageFeedbackCount++;
            damageFlashRemaining = DamageFlashDuration;
            damageFlashPeakAlpha = lethal ? 0.42f : 0.3f;
            cameraController?.AddImpulse(
                PlayerHitPositionImpulse,
                PlayerHitRotationImpulse,
                PlayerHitImpulseDuration);
            worldTime?.RequestHardFreeze(PlayerHitStopDuration);
        }

        private void PlayDefinitionImpulse(WeaponDefinition definition)
        {
            if (definition == null)
            {
                return;
            }

            cameraController?.AddImpulse(
                definition.CameraImpulsePosition,
                definition.CameraImpulseRotation,
                definition.CameraImpulseDuration);
        }

        private void PlayFallbackMeleeImpulse(MeleeImpactKind impactKind)
        {
            if (impactKind == MeleeImpactKind.Bat)
            {
                cameraController?.AddImpulse(
                    BatPositionImpulse,
                    BatRotationImpulse,
                    BatImpulseDuration);
                return;
            }

            cameraController?.AddImpulse(
                PunchPositionImpulse,
                PunchRotationImpulse,
                PunchImpulseDuration);
        }

        private static float ResolveFallbackHitStop(
            MeleeImpactKind? impactKind)
        {
            return impactKind == MeleeImpactKind.Bat
                ? BatHitStopDuration
                : PunchHitStopDuration;
        }

        private void ResolveReferences()
        {
            if (cameraController == null)
            {
                cameraController = GetComponent<TopDownCameraController>();
            }

            if (worldTime == null)
            {
                worldTime = FindFirstObjectByType<WorldTimeController>();
            }

            if (playerHealth == null)
            {
                playerHealth = FindFirstObjectByType<PlayerHealth>();
            }
        }

        private void BindPlayerHealth()
        {
            if (playerHealth == null)
            {
                return;
            }

            playerHealth.Damaged -= HandlePlayerDamaged;
            playerHealth.Damaged += HandlePlayerDamaged;
        }

        private void UnbindPlayerHealth()
        {
            if (playerHealth != null)
            {
                playerHealth.Damaged -= HandlePlayerDamaged;
            }
        }

        private void EnsureDamageTexture()
        {
            if (!Application.isPlaying || damageTexture != null)
            {
                return;
            }

            damageTexture = new Texture2D(
                DamageTextureSize,
                DamageTextureSize,
                TextureFormat.RGBA32,
                false)
            {
                name = "Player Damage Vignette",
                hideFlags = HideFlags.DontSave,
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear
            };
            Color[] pixels = new Color[DamageTextureSize * DamageTextureSize];
            for (int y = 0; y < DamageTextureSize; y++)
            {
                float normalizedY = Mathf.Abs(
                    (y / (DamageTextureSize - 1f) - 0.5f) * 2f);
                for (int x = 0; x < DamageTextureSize; x++)
                {
                    float normalizedX = Mathf.Abs(
                        (x / (DamageTextureSize - 1f) - 0.5f) * 2f);
                    float edge = Mathf.Max(normalizedX, normalizedY);
                    float alpha = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(
                        0.32f,
                        1f,
                        edge));
                    pixels[y * DamageTextureSize + x] =
                        new Color(0.95f, 0.035f, 0.02f, alpha);
                }
            }

            damageTexture.SetPixels(pixels);
            damageTexture.Apply(false, true);
        }

        private void OnDisable()
        {
            UnbindPlayerHealth();
            damageFlashRemaining = 0f;
            if (Active == this)
            {
                Active = null;
            }
        }

        private void OnDestroy()
        {
            if (damageTexture != null)
            {
                Destroy(damageTexture);
            }
        }
    }
}
