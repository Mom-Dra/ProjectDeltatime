using Deltatime.Combat;
using Deltatime.Enemies;
using Deltatime.InputSystem;
using Deltatime.Player;
using Deltatime.TimeSystem;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Deltatime.Tutorial
{
    [DefaultExecutionOrder(-320)]
    public sealed class TutorialDirector : MonoBehaviour
    {
        public enum TutorialStep
        {
            TimeMovement,
            AimAndDash,
            Melee,
            Pistol,
            ThrowAndRecover,
            DeadlineApproach,
            Deadline,
            Complete
        }

        [Header("Player and Time")]
        [SerializeField] private PlayerInputReader input;
        [SerializeField] private PlayerMovement movement;
        [SerializeField] private PlayerAim aim;
        [SerializeField] private PlayerDash dash;
        [SerializeField] private PlayerCombat combat;
        [SerializeField] private WeaponController playerWeapon;
        [SerializeField] private DeadlineController deadline;
        [SerializeField] private WorldTimeController worldTime;
        [SerializeField] private Rigidbody playerBody;
        [SerializeField] private PlayerHealth playerHealth;

        [Header("Progression")]
        [SerializeField] private TutorialGate timeGate;
        [SerializeField] private TutorialGate dashGate;
        [SerializeField] private TutorialGate meleeGate;
        [SerializeField] private TutorialGate pistolGate;
        [SerializeField] private TutorialGate arenaEntranceGate;
        [SerializeField] private TutorialGate arenaExitGate;
        [SerializeField] private Transform deadlineResetPoint;

        [Header("Lessons")]
        [SerializeField] private TutorialTargetDummy meleeTarget;
        [SerializeField] private TutorialTargetDummy pistolTarget;
        [SerializeField] private TutorialWeaponDispenser meleeDispenser;
        [SerializeField] private TutorialWeaponDispenser pistolDispenser;
        [SerializeField] private TutorialWeaponDispenser deadlinePistolDispenser;
        [SerializeField] private WeaponDefinition pistolDefinition;

        [Header("Throw Enemy")]
        [SerializeField] private EnemyHealth throwEnemyHealth;
        [SerializeField] private EnemyCombatant throwEnemyBehavior;
        [SerializeField] private WeaponController throwEnemyWeapon;
        [SerializeField] private EnemyWeaponDrop throwEnemyDrop;

        [Header("Deadline Arena")]
        [SerializeField] private EnemyCombatant[] deadlineEnemies;
        [SerializeField, Min(0.1f)] private float movementProofDuration = 0.7f;
        [SerializeField, Min(0.1f)] private float idleProofDuration = 0.7f;
        [SerializeField, Min(15f)] private float aimProofDegrees = 100f;
        [SerializeField, Min(0.1f)] private float completionDelay = 2f;
        [SerializeField] private bool loadStageOneOnComplete = true;

        private Vector3[] deadlineEnemyPositions;
        private Quaternion[] deadlineEnemyRotations;
        private float movingProof;
        private float idleProof;
        private float accumulatedAimDegrees;
        private float previousAimAngle;
        private float completionRemaining;
        private bool hasPreviousAimAngle;
        private bool observedDash;
        private readonly TutorialProgression progression = new();
        private readonly TutorialThrowRecoveryScenario throwRecovery = new();
        private readonly TutorialDeadlineScenario deadlineScenario = new();
        private bool observedThrowOutcome => throwRecovery.OutcomeObserved;
        private static bool restoreDeadlineCheckpointAfterReload;

        public TutorialStep CurrentStep => progression.CurrentStep;
        public int CurrentStepIndex =>
            Mathf.Min((int)CurrentStep + 1, TotalStepCount);
        public int TotalStepCount => TutorialProgression.TotalStepCount;
        public bool Completed => CurrentStep == TutorialStep.Complete;
        public bool DeadlineSucceeded => deadlineScenario.Succeeded;
        public bool PlayerDead => playerHealth != null && !playerHealth.IsAlive;
        internal PlayerHealth PlayerHealth => playerHealth;

        public string StepTitle
        {
            get
            {
                if (PlayerDead)
                {
                    return "다시 시도";
                }

                switch (CurrentStep)
                {
                    case TutorialStep.TimeMovement:
                        return "1. 움직임과 월드 시간";
                    case TutorialStep.AimAndDash:
                        return "2. 조준과 대시";
                    case TutorialStep.Melee:
                        return "3. 근접 공격";
                    case TutorialStep.Pistol:
                        return "4. 권총 사격";
                    case TutorialStep.ThrowAndRecover:
                        return "5. 무기 투척과 무장 해제";
                    case TutorialStep.DeadlineApproach:
                        return "6. DEADLINE 준비";
                    case TutorialStep.Deadline:
                        return "7. DEADLINE 포위전";
                    default:
                        return "튜토리얼 완료";
                }
            }
        }

        public string Instruction
        {
            get
            {
                if (PlayerDead)
                {
                    return CurrentStep == TutorialStep.Deadline
                        ? $"DEADLINE에서 쓰러졌습니다. {InputBindingDisplay.Get("Restart")}을 누르면 DEADLINE부터 다시 시작합니다."
                        : $"쓰러졌습니다. {InputBindingDisplay.Get("Restart")}을 눌러 튜토리얼을 다시 시작하세요.";
                }

                switch (CurrentStep)
                {
                    case TutorialStep.TimeMovement:
                        return TutorialGuidancePolicy.NeedsMovementProof(
                            movingProof,
                            movementProofDuration)
                            ? $"{InputBindingDisplay.GetMovement()}로 이동하세요. 행동하면 월드 시간이 빨라집니다."
                            : "이제 멈추세요. 정지하면 월드가 0.02배로 느려집니다.";
                    case TutorialStep.AimAndDash:
                        return TutorialGuidancePolicy.NeedsAimProof(
                            accumulatedAimDegrees,
                            aimProofDegrees)
                            ? "마우스를 움직여 조준 방향을 크게 돌리세요."
                            : $"{InputBindingDisplay.GetMovement()} 방향키를 누른 채 {InputBindingDisplay.Get("Dash")}로 표식 구간을 대시하세요.";
                    case TutorialStep.Melee:
                        return $"{InputBindingDisplay.Get("Interact")}로 근접 무기를 줍고, 표적을 향해 {InputBindingDisplay.Get("Fire")}로 공격하세요.";
                    case TutorialStep.Pistol:
                        return $"{InputBindingDisplay.Get("Interact")}로 권총을 교체하고, 마우스로 조준한 뒤 {InputBindingDisplay.Get("Fire")}로 사격하세요.";
                    case TutorialStep.ThrowAndRecover:
                        return !throwRecovery.OutcomeObserved
                            ? $"{InputBindingDisplay.Get("Fire")} 사격이 아니라 {InputBindingDisplay.Get("Throw")}로 권총을 던져 적을 기절시키고 무장을 해제하세요."
                            : $"성공! 공중의 적 무기를 {InputBindingDisplay.Get("Interact")}로 잡으면 바로 DEADLINE으로 진행합니다.";
                    case TutorialStep.DeadlineApproach:
                        return "권총을 장비했습니다. 북쪽 포위전 중앙으로 이동하세요.";
                    case TutorialStep.Deadline:
                        if (deadlineScenario.Succeeded)
                        {
                            return "DEADLINE 실행 성공. 열린 북쪽 출구로 탈출하세요.";
                        }

                        if (!deadlineScenario.ActivationObserved)
                        {
                            return $"{InputBindingDisplay.Get("Deadline")}: DEADLINE — 월드를 완전히 정지시키세요.";
                        }

                        if (!deadlineScenario.TwoCausesObserved)
                        {
                            return $"마우스로 앞의 두 적을 조준하고 {InputBindingDisplay.Get("Fire")} 두 번으로 원인 2개를 배치하세요.";
                        }

                        return $"{InputBindingDisplay.GetMovement()}로 이동하여 원인과 월드를 실행하고 북쪽으로 탈출하세요.";
                    default:
                        return "튜토리얼 완료 — 잠시 후 스테이지 1로 이동합니다.";
                }
            }
        }

        public string ProgressText
        {
            get
            {
                if (PlayerDead)
                {
                    return CurrentStep == TutorialStep.Deadline
                        ? $"{InputBindingDisplay.Get("Restart")}: DEADLINE부터 재시작"
                        : $"{InputBindingDisplay.Get("Restart")}: 튜토리얼 재시작";
                }

                switch (CurrentStep)
                {
                    case TutorialStep.TimeMovement:
                        return $"이동 {movingProof:0.0}/{movementProofDuration:0.0}s  |  정지 {idleProof:0.0}/{idleProofDuration:0.0}s";
                    case TutorialStep.AimAndDash:
                        return $"조준 회전 {Mathf.Min(accumulatedAimDegrees, aimProofDegrees):0}/{aimProofDegrees:0}°  |  대시 {(observedDash ? "완료" : "대기")}";
                    case TutorialStep.Melee:
                        return $"근접 적중 {meleeTarget?.AcceptedHitCount ?? 0}/1";
                    case TutorialStep.Pistol:
                    {
                        string pistolSupply = pistolDispenser == null
                            ? "권총 지급기 없음"
                            : pistolDispenser.HasExpectedLoadout
                                ? "권총 장비 완료"
                                : pistolDispenser.HasSpawnedPickup
                                    ? "권총 생성됨"
                                    : "권총 보급 중";
                        return $"권총 적중 {pistolTarget?.AcceptedHitCount ?? 0}/1  |  {pistolSupply}";
                    }
                    case TutorialStep.ThrowAndRecover:
                        return $"기절·드롭 {(observedThrowOutcome ? "완료" : "대기")}  |  공중 무기 {(observedThrowOutcome && playerWeapon.HasWeapon ? "확보" : "필요")}";
                    case TutorialStep.Deadline:
                        return deadline == null
                            ? "DEADLINE 연결 없음"
                            : $"원인 {deadline.StagedActionCount}/{deadline.MaxStagedActions}  |  충전 {deadline.ChargesRemaining}/{deadline.MaxCharges}";
                    default:
                        return string.Empty;
                }
            }
        }

        private void Awake()
        {
            if (!ValidateConfiguration(out string error))
            {
                Debug.LogError(error, this);
                enabled = false;
                return;
            }

            bool restoreDeadlineCheckpoint =
                ConsumeDeadlineCheckpointReloadRequest();
            deadline.enabled = false;
            dash.enabled = false;
            throwEnemyBehavior.enabled = false;
            throwEnemyHealth.SetDamageEnabled(false);
            CaptureDeadlineEnemyPoses();
            SetDeadlineEnemiesEnabled(false);
            SetInitialLessonState();

            if (restoreDeadlineCheckpoint)
            {
                RestoreDeadlineCheckpoint();
            }
        }

        private void OnEnable()
        {
            if (meleeTarget != null)
            {
                meleeTarget.Accepted += HandleTargetAccepted;
            }

            if (pistolTarget != null)
            {
                pistolTarget.Accepted += HandleTargetAccepted;
            }

            if (throwEnemyDrop != null)
            {
                throwEnemyDrop.WeaponDropped += HandleThrowEnemyDropped;
            }

            if (deadline != null)
            {
                deadline.Released += HandleDeadlineReleased;
            }
        }

        private void Update()
        {
            if (input.RestartPressed)
            {
                if (PlayerDead && CurrentStep == TutorialStep.Deadline)
                {
                    restoreDeadlineCheckpointAfterReload = true;
                }

                ReloadActiveScene();
                return;
            }

            if (PlayerDead)
            {
                return;
            }

            if (Completed)
            {
                UpdateCompletion();
                return;
            }

            switch (CurrentStep)
            {
                case TutorialStep.TimeMovement:
                    UpdateTimeMovement();
                    break;
                case TutorialStep.AimAndDash:
                    UpdateAimAndDash();
                    break;
                case TutorialStep.ThrowAndRecover:
                    UpdateThrowAndRecover();
                    break;
                case TutorialStep.Deadline:
                    UpdateDeadline();
                    break;
            }
        }

        public void NotifyTrigger(TutorialTrigger.TriggerKind kind)
        {
            switch (kind)
            {
                case TutorialTrigger.TriggerKind.DashExit:
                    if (CurrentStep == TutorialStep.AimAndDash &&
                        accumulatedAimDegrees >= aimProofDegrees &&
                        observedDash)
                    {
                        AdvanceTo(TutorialStep.Melee);
                    }
                    break;

                case TutorialTrigger.TriggerKind.DeadlineEntry:
                    if (CurrentStep == TutorialStep.DeadlineApproach)
                    {
                        BeginDeadlineArena();
                    }
                    break;

                case TutorialTrigger.TriggerKind.TutorialExit:
                    if (CurrentStep == TutorialStep.Deadline &&
                        deadlineScenario.Succeeded)
                    {
                        CompleteTutorial();
                    }
                    break;
            }
        }

        public void Configure(
            PlayerInputReader inputReader,
            PlayerMovement playerMovement,
            PlayerAim playerAim,
            PlayerDash playerDash,
            PlayerCombat playerCombat,
            WeaponController playerWeaponController,
            DeadlineController deadlineController,
            WorldTimeController timeSource,
            Rigidbody playerRigidbody,
            PlayerHealth health,
            TutorialGate movementGate,
            TutorialGate aimDashGate,
            TutorialGate meleeLessonGate,
            TutorialGate pistolLessonGate,
            TutorialGate entranceGate,
            TutorialGate exitGate,
            Transform arenaResetPoint,
            TutorialTargetDummy meleeDummy,
            TutorialTargetDummy pistolDummy,
            TutorialWeaponDispenser meleeWeaponDispenser,
            TutorialWeaponDispenser pistolWeaponDispenser,
            TutorialWeaponDispenser arenaPistolDispenser,
            WeaponDefinition pistol,
            EnemyHealth throwHealth,
            EnemyCombatant throwBehavior,
            WeaponController throwWeapon,
            EnemyWeaponDrop throwDrop,
            EnemyCombatant[] arenaEnemies)
        {
            input = inputReader;
            movement = playerMovement;
            aim = playerAim;
            dash = playerDash;
            combat = playerCombat;
            playerWeapon = playerWeaponController;
            deadline = deadlineController;
            worldTime = timeSource;
            playerBody = playerRigidbody;
            playerHealth = health;
            timeGate = movementGate;
            dashGate = aimDashGate;
            meleeGate = meleeLessonGate;
            pistolGate = pistolLessonGate;
            arenaEntranceGate = entranceGate;
            arenaExitGate = exitGate;
            deadlineResetPoint = arenaResetPoint;
            meleeTarget = meleeDummy;
            pistolTarget = pistolDummy;
            meleeDispenser = meleeWeaponDispenser;
            pistolDispenser = pistolWeaponDispenser;
            deadlinePistolDispenser = arenaPistolDispenser;
            pistolDefinition = pistol;
            throwEnemyHealth = throwHealth;
            throwEnemyBehavior = throwBehavior;
            throwEnemyWeapon = throwWeapon;
            throwEnemyDrop = throwDrop;
            deadlineEnemies = arenaEnemies;
        }

        public bool ValidateConfiguration(out string error)
        {
            if (input == null || movement == null || aim == null ||
                dash == null || combat == null || playerWeapon == null ||
                deadline == null || worldTime == null || playerBody == null ||
                playerHealth == null ||
                timeGate == null || dashGate == null || meleeGate == null ||
                pistolGate == null || arenaEntranceGate == null ||
                arenaExitGate == null || deadlineResetPoint == null ||
                meleeTarget == null || pistolTarget == null ||
                meleeDispenser == null || pistolDispenser == null ||
                deadlinePistolDispenser == null || pistolDefinition == null ||
                throwEnemyHealth == null || throwEnemyBehavior == null ||
                throwEnemyWeapon == null || throwEnemyDrop == null ||
                deadlineEnemies == null || deadlineEnemies.Length != 4)
            {
                error = $"{nameof(TutorialDirector)} is missing required references or does not have four DEADLINE enemies.";
                return false;
            }

            for (int i = 0; i < deadlineEnemies.Length; i++)
            {
                if (deadlineEnemies[i] == null)
                {
                    error = $"{nameof(TutorialDirector)} DEADLINE enemy {i} is missing.";
                    return false;
                }
            }

            error = string.Empty;
            return true;
        }

#if UNITY_EDITOR
        public void EnterThrowRecoveryForValidation()
        {
            if (CurrentStep == TutorialStep.Complete)
            {
                return;
            }

            progression.MoveTo(TutorialStep.ThrowAndRecover);
            throwRecovery.Reset();
            pistolDispenser.SetAvailable(true);
            deadlinePistolDispenser.SetAvailable(false);
            arenaEntranceGate.SetOpen(false, true);
        }

        public void EvaluateThrowRecoveryForValidation()
        {
            if (CurrentStep == TutorialStep.ThrowAndRecover)
            {
                UpdateThrowAndRecover();
            }
        }

        public void EnterDeadlineForValidation()
        {
            if (CurrentStep == TutorialStep.Complete)
            {
                return;
            }

            progression.MoveTo(TutorialStep.DeadlineApproach);
            deadlinePistolDispenser.SetAvailable(true);
            BeginDeadlineArena();
        }

        public void RestoreDeadlineCheckpointForValidation()
        {
            if (CurrentStep != TutorialStep.Complete)
            {
                RestoreDeadlineCheckpoint();
            }
        }
#endif

        private void SetInitialLessonState()
        {
            timeGate.SetOpen(false, true);
            dashGate.SetOpen(false, true);
            meleeGate.SetOpen(false, true);
            pistolGate.SetOpen(false, true);
            arenaEntranceGate.SetOpen(false, true);
            arenaExitGate.SetOpen(false, true);
            meleeDispenser.SetAvailable(false);
            pistolDispenser.SetAvailable(false);
            deadlinePistolDispenser.SetAvailable(false);
        }

        private void UpdateTimeMovement()
        {
            if (movement.IsPhysicallyMoving &&
                worldTime.CurrentTimeScale >= 0.35f)
            {
                movingProof = Mathf.Min(
                    movementProofDuration,
                    movingProof + UnityEngine.Time.unscaledDeltaTime);
            }

            if (movingProof < movementProofDuration)
            {
                idleProof = 0f;
                return;
            }

            if (!movement.IsPhysicallyMoving &&
                input.Move.sqrMagnitude <= 0.0025f &&
                worldTime.CurrentTimeScale <= 0.12f)
            {
                idleProof = Mathf.Min(
                    idleProofDuration,
                    idleProof + UnityEngine.Time.unscaledDeltaTime);
            }
            else
            {
                idleProof = 0f;
            }

            if (idleProof >= idleProofDuration)
            {
                AdvanceTo(TutorialStep.AimAndDash);
            }
        }

        private void UpdateAimAndDash()
        {
            float currentAngle = aim.AimAngleDegrees;
            if (hasPreviousAimAngle)
            {
                accumulatedAimDegrees += Mathf.Abs(
                    Mathf.DeltaAngle(previousAimAngle, currentAngle));
            }

            previousAimAngle = currentAngle;
            hasPreviousAimAngle = true;
            if (accumulatedAimDegrees >= aimProofDegrees && dash.IsDashing)
            {
                observedDash = true;
            }
        }

        private void UpdateThrowAndRecover()
        {
            if (throwRecovery.TryObserveOutcome(
                    throwEnemyHealth.IsAlive,
                    throwEnemyHealth.IsStunned,
                    throwEnemyBehavior.IsDisarmed,
                    throwEnemyWeapon.HasWeapon))
            {
                pistolDispenser.SetAvailable(false);
                deadlinePistolDispenser.SetAvailable(true);
                arenaEntranceGate.SetOpen(true);
            }

            if (throwRecovery.OutcomeObserved && playerWeapon.HasWeapon)
            {
                throwEnemyHealth.gameObject.SetActive(false);
                deadlinePistolDispenser.SetAvailable(false);
                AdvanceTo(TutorialStep.DeadlineApproach);
            }
        }

        private void UpdateDeadline()
        {
            if (deadlineScenario.Observe(
                    deadline.IsActive,
                    deadline.StagedActionCount,
                    deadline.MaxStagedActions))
            {
                SetDeadlineEnemiesEnabled(true);
            }
        }

        private void AdvanceTo(TutorialStep nextStep)
        {
            progression.MoveTo(nextStep);
            switch (nextStep)
            {
                case TutorialStep.AimAndDash:
                    timeGate.SetOpen(true);
                    dash.enabled = true;
                    break;
                case TutorialStep.Melee:
                    dashGate.SetOpen(true);
                    meleeDispenser.SetAvailable(true);
                    break;
                case TutorialStep.Pistol:
                    meleeGate.SetOpen(true);
                    meleeDispenser.SetAvailable(false);
                    pistolDispenser.SetAvailable(true);
                    break;
                case TutorialStep.ThrowAndRecover:
                    pistolGate.SetOpen(true);
                    pistolDispenser.SetAvailable(true);
                    break;
                case TutorialStep.DeadlineApproach:
                    arenaEntranceGate.SetOpen(true);
                    break;
            }
        }

        private void HandleTargetAccepted(TutorialTargetDummy target)
        {
            if (CurrentStep == TutorialStep.Melee && target == meleeTarget)
            {
                AdvanceTo(TutorialStep.Pistol);
            }
            else if (CurrentStep == TutorialStep.Pistol && target == pistolTarget)
            {
                AdvanceTo(TutorialStep.ThrowAndRecover);
            }
        }

        private void HandleThrowEnemyDropped()
        {
            if (CurrentStep == TutorialStep.ThrowAndRecover)
            {
                throwRecovery.ObserveDrop();
            }
        }

        private void BeginDeadlineArena()
        {
            progression.MoveTo(TutorialStep.Deadline);
            arenaEntranceGate.SetOpen(false);
            arenaExitGate.SetOpen(false, true);
            deadlineScenario.Begin();
            SetDeadlineEnemiesEnabled(false);
            deadline.enabled = true;
            deadline.RefillCharges();
        }

        private void HandleDeadlineReleased()
        {
            if (CurrentStep != TutorialStep.Deadline ||
                !deadline.ReleasedThisFrame)
            {
                return;
            }

            if (deadlineScenario.TrySucceed())
            {
                arenaExitGate.SetOpen(true);
                return;
            }

            ResetDeadlineAttempt();
        }

        private void ResetDeadlineAttempt()
        {
            SetDeadlineEnemiesEnabled(false);
            ResetDeadlineEnemyPoses();
            deadlineScenario.ResetAttempt();
            deadline.RefillCharges();

            MovePlayerToDeadlineResetPoint();
            deadlinePistolDispenser.SetAvailable(true);
        }

        private void RestoreDeadlineCheckpoint()
        {
            ResetDeadlineEnemyPoses();
            playerWeapon.Equip(
                pistolDefinition,
                pistolDefinition.AmmunitionCapacity);
            MovePlayerToDeadlineResetPoint();
            BeginDeadlineArena();
        }

        private void MovePlayerToDeadlineResetPoint()
        {
            playerBody.linearVelocity = Vector3.zero;
            playerBody.angularVelocity = Vector3.zero;
            playerBody.position = deadlineResetPoint.position;
            playerBody.rotation = deadlineResetPoint.rotation;
            playerBody.transform.SetPositionAndRotation(
                deadlineResetPoint.position,
                deadlineResetPoint.rotation);
            Physics.SyncTransforms();
        }

        private static bool ConsumeDeadlineCheckpointReloadRequest()
        {
            bool shouldRestore = restoreDeadlineCheckpointAfterReload;
            restoreDeadlineCheckpointAfterReload = false;
            return shouldRestore;
        }

        private static void ReloadActiveScene()
        {
            Scene activeScene = SceneManager.GetActiveScene();
            if (activeScene.buildIndex >= 0)
            {
                SceneManager.LoadScene(activeScene.buildIndex);
            }
            else
            {
                SceneManager.LoadScene(activeScene.name);
            }
        }

        private void CompleteTutorial()
        {
            progression.MoveTo(TutorialStep.Complete);
            completionRemaining = completionDelay;
            SetDeadlineEnemiesEnabled(false);
            deadline.enabled = false;
            combat.SetCombatEnabled(false);
        }

        private void UpdateCompletion()
        {
            if (!loadStageOneOnComplete)
            {
                return;
            }

            completionRemaining = Mathf.Max(
                0f,
                completionRemaining - UnityEngine.Time.unscaledDeltaTime);
            if (completionRemaining <= 0f)
            {
                SceneManager.LoadScene("Stage1");
            }
        }

        private void CaptureDeadlineEnemyPoses()
        {
            deadlineEnemyPositions = new Vector3[deadlineEnemies.Length];
            deadlineEnemyRotations = new Quaternion[deadlineEnemies.Length];
            for (int i = 0; i < deadlineEnemies.Length; i++)
            {
                deadlineEnemyPositions[i] = deadlineEnemies[i].transform.position;
                deadlineEnemyRotations[i] = deadlineEnemies[i].transform.rotation;
            }
        }

        private void ResetDeadlineEnemyPoses()
        {
            if (deadlineEnemyPositions == null ||
                deadlineEnemyPositions.Length != deadlineEnemies.Length)
            {
                return;
            }

            for (int i = 0; i < deadlineEnemies.Length; i++)
            {
                EnemyCombatant enemy = deadlineEnemies[i];
                if (enemy == null)
                {
                    continue;
                }

                Rigidbody body = enemy.GetComponent<Rigidbody>();
                if (body != null)
                {
                    if (!body.isKinematic)
                    {
                        body.linearVelocity = Vector3.zero;
                        body.angularVelocity = Vector3.zero;
                    }

                    body.position = deadlineEnemyPositions[i];
                    body.rotation = deadlineEnemyRotations[i];
                }
                else
                {
                    enemy.transform.SetPositionAndRotation(
                        deadlineEnemyPositions[i],
                        deadlineEnemyRotations[i]);
                }
            }
        }

        private void SetDeadlineEnemiesEnabled(bool value)
        {
            for (int i = 0; i < deadlineEnemies.Length; i++)
            {
                if (deadlineEnemies[i] != null &&
                    !deadlineEnemies[i].IsDead)
                {
                    deadlineEnemies[i].enabled = value;
                }
            }
        }

        private void OnDisable()
        {
            if (meleeTarget != null)
            {
                meleeTarget.Accepted -= HandleTargetAccepted;
            }

            if (pistolTarget != null)
            {
                pistolTarget.Accepted -= HandleTargetAccepted;
            }

            if (throwEnemyDrop != null)
            {
                throwEnemyDrop.WeaponDropped -= HandleThrowEnemyDropped;
            }

            if (deadline != null)
            {
                deadline.Released -= HandleDeadlineReleased;
            }
        }
    }
}
