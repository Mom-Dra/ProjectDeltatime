using Deltatime.Combat;
using Deltatime.Enemies;
using Deltatime.InputSystem;
using Deltatime.Player;
using Deltatime.Replay;
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
        public Transform VisualRoot => visualRoot;
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
            StageReplayController.ActiveRecorder?.RegisterAnimationSource(this);
            StageReplayController.ActiveRecorder?.RecordAnimatorActive(
                this,
                true);
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
            StageReplayController.ActiveRecorder?.RecordAnimatorActive(
                this,
                false);
            Unsubscribe();
        }

        public void Configure(
            Animator targetAnimator,
            CharacterAnimationLibrary animationLibrary)
        {
            Configure(
                targetAnimator,
                animationLibrary,
                targetAnimator == null ? null : targetAnimator.transform);
        }

        public void Configure(
            Animator targetAnimator,
            CharacterAnimationLibrary animationLibrary,
            Transform targetVisualRoot)
        {
            animator = targetAnimator;
            library = animationLibrary;
            visualRoot = targetVisualRoot == null
                ? (targetAnimator == null ? null : targetAnimator.transform)
                : targetVisualRoot;
            hasVisualRootRestRotation = false;
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
            SetRuntimeAnimatorController(nextController);
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
            SetFloatParameter(
                MoveXHash,
                localDirection.x * magnitude,
                directionDamping,
                deltaTime);
            SetFloatParameter(
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
                ResetTriggerParameter(AttackAHash);
                ResetTriggerParameter(AttackBHash);
                SetTriggerParameter(RollHash);
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
            SetTriggerParameter(trigger);
            return true;
        }

        /// <summary>
        /// Extensible Animator adapter used by gameplay and replay recording.
        /// Continuous parameters are sampled without duplicates by the replay
        /// track; triggers are emitted explicitly because Animator does not expose
        /// a reliable readable trigger value after controller evaluation.
        /// </summary>
        public void SetFloatParameter(
            int parameterHash,
            float value,
            float dampTime = 0f,
            float deltaTime = 0f)
        {
            if (animator == null)
            {
                return;
            }

            if (dampTime > 0f && deltaTime > 0f)
            {
                animator.SetFloat(
                    parameterHash,
                    value,
                    dampTime,
                    deltaTime);
            }
            else
            {
                animator.SetFloat(parameterHash, value);
            }
        }

        public void SetBoolParameter(int parameterHash, bool value)
        {
            if (animator != null)
            {
                animator.SetBool(parameterHash, value);
            }
        }

        public void SetIntegerParameter(int parameterHash, int value)
        {
            if (animator != null)
            {
                animator.SetInteger(parameterHash, value);
            }
        }

        public void SetTriggerParameter(int parameterHash)
        {
            if (animator == null)
            {
                return;
            }

            animator.SetTrigger(parameterHash);
            StageReplayController.ActiveRecorder?.RecordAnimatorTrigger(
                this,
                parameterHash,
                true);
        }

        public void ResetTriggerParameter(int parameterHash)
        {
            if (animator == null)
            {
                return;
            }

            animator.ResetTrigger(parameterHash);
            StageReplayController.ActiveRecorder?.RecordAnimatorTrigger(
                this,
                parameterHash,
                false);
        }

        private void SetRuntimeAnimatorController(
            RuntimeAnimatorController controller)
        {
            animator.runtimeAnimatorController = controller;
            StageReplayController.ActiveRecorder?.RecordAnimatorController(
                this,
                controller);
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
