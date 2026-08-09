using Deltatime.Combat;
using Deltatime.Enemies;
using Deltatime.InputSystem;
using Deltatime.Player;
using Deltatime.TimeSystem;
using UnityEngine;

namespace Deltatime.Visuals
{
    [DefaultExecutionOrder(100)]
    [DisallowMultipleComponent]
    public sealed class CharacterAnimationController : MonoBehaviour
    {
        private static readonly int MoveXHash = Animator.StringToHash("MoveX");
        private static readonly int MoveYHash = Animator.StringToHash("MoveY");
        private static readonly int RollHash = Animator.StringToHash("Roll");
        private static readonly int AttackAHash = Animator.StringToHash("AttackA");
        private static readonly int AttackBHash = Animator.StringToHash("AttackB");

        [SerializeField] private Animator animator;
        [SerializeField] private Transform visualRoot;
        [SerializeField] private CharacterAnimationLibrary library;
        [SerializeField] private WeaponController weapon;
        [SerializeField] private WorldTimeController worldTime;

        [Header("Player Sources")]
        [SerializeField] private PlayerInputReader playerInput;
        [SerializeField] private PlayerMovement playerMovement;
        [SerializeField] private PlayerDash playerDash;
        [SerializeField] private PlayerCombat playerCombat;

        [Header("Enemy Sources")]
        [SerializeField] private EnemyMotor enemyMotor;
        [SerializeField] private EnemyCombatant enemyCombatant;

        [Header("Blend")]
        [SerializeField, Min(0f)] private float directionDamping = 0.08f;

        [Header("Dash Visual")]
        [SerializeField, Min(0.01f)] private float rollVisualDuration = 0.5f;

        private CharacterAnimationStyle currentStyle;
        private RuntimeAnimatorController currentController;
        private bool previousDashState;
        private bool useAlternateAttack;
        private Quaternion visualRootRestRotation;
        private bool hasVisualRootRestRotation;
        private Vector3 rollDirection;
        private float rollVisualTimeRemaining;

        public Animator Animator => animator;
        public CharacterAnimationStyle CurrentStyle => currentStyle;
        public bool IsEnemy => enemyMotor != null;

        private void Awake()
        {
            CacheSources();
            ConfigureAnimator();
            RefreshEquipmentStyle(true);
        }

        private void OnEnable()
        {
            Subscribe();
        }

        private void Update()
        {
            if (animator == null || library == null)
            {
                return;
            }

            UpdatePlaybackSpeed();
            UpdateMovement();
            UpdateRoll();
        }

        private void LateUpdate()
        {
            UpdateDashVisualFacing();
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        public void Configure(
            Animator targetAnimator,
            CharacterAnimationLibrary animationLibrary)
        {
            animator = targetAnimator;
            library = animationLibrary;
            rollVisualDuration = Mathf.Max(0.5f, rollVisualDuration);
            CacheSources();
            ConfigureAnimator();
            RefreshEquipmentStyle(true);
        }

        private void CacheSources()
        {
            if (animator == null)
            {
                animator = GetComponentInChildren<Animator>(true);
            }

            if (visualRoot == null && animator != null)
            {
                visualRoot = animator.transform;
            }

            if (visualRoot != null && !hasVisualRootRestRotation)
            {
                visualRootRestRotation = visualRoot.localRotation;
                hasVisualRootRestRotation = true;
            }

            if (weapon == null)
            {
                weapon = GetComponent<WeaponController>();
            }

            if (worldTime == null)
            {
                worldTime = FindFirstObjectByType<WorldTimeController>();
            }

            if (playerInput == null)
            {
                playerInput = GetComponent<PlayerInputReader>();
            }

            if (playerMovement == null)
            {
                playerMovement = GetComponent<PlayerMovement>();
            }

            if (playerDash == null)
            {
                playerDash = GetComponent<PlayerDash>();
            }

            if (playerCombat == null)
            {
                playerCombat = GetComponent<PlayerCombat>();
            }

            if (enemyMotor == null)
            {
                enemyMotor = GetComponent<EnemyMotor>();
            }

            if (enemyCombatant == null)
            {
                enemyCombatant = GetComponent<EnemyCombatant>();
            }
        }

        private void ConfigureAnimator()
        {
            if (animator == null)
            {
                return;
            }

            animator.enabled = true;
            animator.applyRootMotion = false;
            animator.updateMode = AnimatorUpdateMode.UnscaledTime;
            animator.cullingMode = AnimatorCullingMode.CullUpdateTransforms;
        }

        private void Subscribe()
        {
            if (weapon != null)
            {
                weapon.EquipmentChanged -= HandleEquipmentChanged;
                weapon.EquipmentChanged += HandleEquipmentChanged;
                weapon.UsePerformed -= HandleAttackPerformed;
                weapon.UsePerformed += HandleAttackPerformed;
            }

        }

        private void Unsubscribe()
        {
            if (weapon != null)
            {
                weapon.EquipmentChanged -= HandleEquipmentChanged;
                weapon.UsePerformed -= HandleAttackPerformed;
            }

        }

        private void HandleEquipmentChanged()
        {
            RefreshEquipmentStyle(false);
        }

        private void RefreshEquipmentStyle(bool force)
        {
            if (animator == null || library == null)
            {
                return;
            }

            CharacterAnimationStyle nextStyle = library.ResolveStyle(
                weapon == null ? null : weapon.Definition);
            RuntimeAnimatorController nextController =
                library.GetController(nextStyle);
            if (!force &&
                currentStyle == nextStyle &&
                currentController == nextController)
            {
                return;
            }

            currentStyle = nextStyle;
            currentController = nextController;
            animator.runtimeAnimatorController = nextController;
            if (nextController != null && animator.isActiveAndEnabled)
            {
                animator.Rebind();
                animator.Update(0f);
            }
        }

        private void UpdatePlaybackSpeed()
        {
            if (worldTime == null)
            {
                animator.speed = 1f;
                return;
            }

            animator.speed = IsEnemy
                ? Mathf.Max(0f, worldTime.CurrentTimeScale)
                : worldTime.IsHardFrozen ? 0f : 1f;
        }

        private void UpdateMovement()
        {
            Vector3 worldDirection = Vector3.zero;
            float magnitude = 0f;

            if (enemyMotor != null && enemyMotor.IsMoving)
            {
                worldDirection = enemyMotor.MovementDirection;
                magnitude = 1f;
            }
            else if (playerInput != null &&
                     playerMovement != null &&
                     playerMovement.IsPhysicallyMoving &&
                     (playerDash == null || !playerDash.IsDashing))
            {
                Vector2 inputDirection = playerInput.Move;
                worldDirection = new Vector3(
                    inputDirection.x,
                    0f,
                    inputDirection.y);
                magnitude = Mathf.Clamp01(inputDirection.magnitude);
            }

            Vector3 localDirection = worldDirection.sqrMagnitude <= 0.000001f
                ? Vector3.zero
                : transform.InverseTransformDirection(
                    worldDirection.normalized);
            float deltaTime = UnityEngine.Time.unscaledDeltaTime;
            animator.SetFloat(
                MoveXHash,
                localDirection.x * magnitude,
                directionDamping,
                deltaTime);
            animator.SetFloat(
                MoveYHash,
                localDirection.z * magnitude,
                directionDamping,
                deltaTime);
        }

        private void UpdateRoll()
        {
            bool isDashing = playerDash != null && playerDash.IsDashing;
            if (isDashing && !previousDashState)
            {
                rollDirection = playerDash.DashDirection;
                rollDirection.y = 0f;
                rollVisualTimeRemaining = rollVisualDuration;
                animator.ResetTrigger(AttackAHash);
                animator.ResetTrigger(AttackBHash);
                animator.SetTrigger(RollHash);
            }

            if (rollVisualTimeRemaining > 0f)
            {
                rollVisualTimeRemaining = Mathf.Max(
                    0f,
                    rollVisualTimeRemaining - UnityEngine.Time.unscaledDeltaTime);
            }

            previousDashState = isDashing;
        }

        private void UpdateDashVisualFacing()
        {
            if (visualRoot == null || !hasVisualRootRestRotation)
            {
                return;
            }

            if (rollVisualTimeRemaining > 0f &&
                rollDirection.sqrMagnitude > 0.000001f)
            {
                visualRoot.rotation = Quaternion.LookRotation(
                    rollDirection.normalized,
                    Vector3.up);
                return;
            }

            visualRoot.localRotation = visualRootRestRotation;
        }

        public bool TryPlayMeleeAttackAnimation()
        {
            if (animator == null ||
                library == null ||
                !library.SupportsAttack(currentStyle) ||
                animator.runtimeAnimatorController == null ||
                animator.layerCount < 2)
            {
                return false;
            }

            int trigger = useAlternateAttack ? AttackBHash : AttackAHash;
            useAlternateAttack = !useAlternateAttack;
            animator.SetTrigger(trigger);
            return true;
        }

        private void HandleAttackPerformed()
        {
            TryPlayMeleeAttackAnimation();
        }

        private void OnValidate()
        {
            directionDamping = Mathf.Max(0f, directionDamping);
            rollVisualDuration = Mathf.Max(0.01f, rollVisualDuration);
        }
    }
}
