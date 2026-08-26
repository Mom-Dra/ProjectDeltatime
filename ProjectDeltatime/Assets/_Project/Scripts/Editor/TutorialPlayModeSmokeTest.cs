using System;
using Deltatime.Combat;
using Deltatime.Core;
using Deltatime.Enemies;
using Deltatime.Player;
using Deltatime.TimeSystem;
using Deltatime.Tutorial;
using Deltatime.Vision;
using Deltatime.Visuals;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace Deltatime.EditorTools
{
    [InitializeOnLoad]
    public static class TutorialPlayModeSmokeTest
    {
        private const string ScenePath = "Assets/_Project/Scenes/Tutorial.unity";
        private const string ReworkScenePath =
            "Assets/_Project/Scenes/TutorialRework/Tutorial.unity";
        private const string RunningKey = "Deltatime.TutorialSmoke.Running";
        private const string FailedKey = "Deltatime.TutorialSmoke.Failed";
        private const string FailureKey = "Deltatime.TutorialSmoke.Failure";
        private const string ScenePathKey = "Deltatime.TutorialSmoke.ScenePath";
        private const string GateVisualRootName = "Training Shutter Visuals";
        private const string GateBarNamePrefix = "Gate Bar ";
        private const int GateBarCount = 17;
        private const float GateBarWidth = 0.24f;
        private const float GateBarHeight = 2.45f;
        private const float GateBarDepth = 0.18f;

        private static double phaseStartedAt;
        private static int phase;
        private static float fastScale;
        private static float fastProbeDelta;
        private static float phaseProbeStart;
        private static Keyboard testKeyboard;
        private static bool addedKeyboard;
        private static bool gateLayoutValidated;
        private static bool characterVisualsValidated;
        private static Vector3 movementProbeStart;
        private static readonly CommandLineSmokeRunner Runner =
            new CommandLineSmokeRunner(
                RunningKey,
                Tick,
                HandlePlayModeChanged,
                HandleLog);

        static TutorialPlayModeSmokeTest()
        {
            if (SessionState.GetBool(RunningKey, false))
            {
                Attach();
            }
        }

        public static void RunFromCommandLine()
        {
            StartSmoke(ScenePath, false);
        }

        public static void RunReworkFromCommandLine()
        {
            StartSmoke(ReworkScenePath, true);
        }

        private static void StartSmoke(string scenePath, bool reworked)
        {
            if (SessionState.GetBool(RunningKey, false))
            {
                return;
            }

            if (reworked)
            {
                TutorialSceneBuilder.ValidateSavedTutorialRework();
            }
            else
            {
                TutorialSceneBuilder.ValidateSavedTutorial();
            }

            SessionState.SetString(ScenePathKey, scenePath);
            SessionState.SetBool(RunningKey, true);
            SessionState.SetBool(FailedKey, false);
            SessionState.SetString(FailureKey, string.Empty);
            phase = 0;
            phaseStartedAt = EditorApplication.timeSinceStartup;
            gateLayoutValidated = false;
            characterVisualsValidated = false;
            Runner.OpenSceneAndEnterPlayMode(scenePath);
        }

        private static void Attach()
        {
            Runner.Attach();
        }

        private static void HandlePlayModeChanged(PlayModeStateChange state)
        {
            if (!SessionState.GetBool(RunningKey, false))
            {
                return;
            }

            if (state == PlayModeStateChange.EnteredPlayMode)
            {
                phase = 0;
                phaseStartedAt = EditorApplication.timeSinceStartup;
                phaseProbeStart = 0f;
            }
            else if (state == PlayModeStateChange.EnteredEditMode)
            {
                Finish();
            }
        }

        private static void Tick()
        {
            if (!SessionState.GetBool(RunningKey, false) ||
                !EditorApplication.isPlaying)
            {
                return;
            }

            try
            {
                TutorialDirector director =
                    UnityEngine.Object.FindFirstObjectByType<TutorialDirector>();
                TutorialTimeProbe probe =
                    UnityEngine.Object.FindFirstObjectByType<TutorialTimeProbe>();
                WorldTimeController worldTime =
                    UnityEngine.Object.FindFirstObjectByType<WorldTimeController>();
                WorldTimeActivity activity =
                    UnityEngine.Object.FindFirstObjectByType<WorldTimeActivity>();
                Deltatime.InputSystem.PlayerInputReader input =
                    UnityEngine.Object.FindFirstObjectByType<
                        Deltatime.InputSystem.PlayerInputReader>();
                PlayerHealth player =
                    UnityEngine.Object.FindFirstObjectByType<PlayerHealth>();
                TutorialHud hud =
                    UnityEngine.Object.FindFirstObjectByType<TutorialHud>();

                Require(director != null && probe != null &&
                        worldTime != null && activity != null &&
                        input != null && player != null && hud != null,
                    "Tutorial runtime initialization is incomplete.");
                Require(hud.enabled && hud.HasRequiredVisualAssets,
                    "Tutorial cyber HUD did not initialize with its visual assets.");
                Require(Mathf.Approximately(UnityEngine.Time.timeScale, 1f),
                    "Tutorial changed global Time.timeScale.");
                if (!gateLayoutValidated)
                {
                    ValidateRuntimeGateLayout();
                    gateLayoutValidated = true;
                }
                if (!characterVisualsValidated)
                {
                    ValidateRuntimeCharacterVisuals(player);
                    characterVisualsValidated = true;
                }
                ValidateDeadlineBinding();
                ValidateTutorialVision(player);

                double elapsed = EditorApplication.timeSinceStartup - phaseStartedAt;
                switch (phase)
                {
                    case 0:
                        input.enabled = false;
                        activity.SetMovement(1f);
                        if (phaseProbeStart <= 0f)
                        {
                            phaseProbeStart = probe.AccumulatedDegrees;
                        }

                        if (elapsed >= 0.9)
                        {
                            fastScale = worldTime.CurrentTimeScale;
                            fastProbeDelta =
                                probe.AccumulatedDegrees - phaseProbeStart;
                            Require(fastScale >= 0.75f,
                                $"Movement activity only reached {fastScale:0.00}x.");
                            Require(fastProbeDelta >= 80f,
                                "WorldDeltaTime tutorial probe did not advance quickly.");
                            BeginPhase(1, probe.AccumulatedDegrees);
                        }
                        break;

                    case 1:
                        activity.SetMovement(0f);
                        if (elapsed >= 0.9)
                        {
                            float idleScale = worldTime.CurrentTimeScale;
                            float idleProbeDelta =
                                probe.AccumulatedDegrees - phaseProbeStart;
                            Require(idleScale <= 0.12f,
                                $"Idle world time remained at {idleScale:0.00}x.");
                            Require(fastProbeDelta > idleProbeDelta * 4f,
                                "Tutorial probe did not visibly distinguish fast and idle time.");
                            ValidatePistolDispenser(player);
                            ValidateTargetsAndThrow(director, player);
                            input.enabled = true;
                            director.EnterDeadlineForValidation();
                            QueueKeyPress(Key.Q);
                            PumpGameplayInput(input, player);
                            DeadlineController queuedDeadline =
                                player.GetComponent<DeadlineController>();
                            PlayerCombat queuedCombat =
                                player.GetComponent<PlayerCombat>();
                            Require(input.DeadlinePressed,
                                "Validation input did not reach PlayerInputReader.");
                            Require(queuedDeadline != null && queuedDeadline.IsActive,
                                $"Q reached PlayerInputReader but DEADLINE stayed inactive; " +
                                $"ready={queuedDeadline?.IsReady}, " +
                                $"alive={player.IsAlive}, invulnerable={player.IsInvulnerable}, " +
                                $"combat={queuedCombat?.CombatEnabled}, " +
                                $"hardFrozen={worldTime.IsHardFrozen}.");
                            BeginPhase(2, probe.AccumulatedDegrees);
                        }
                        break;

                    case 2:
                        if (elapsed >= 0.25)
                        {
                            DeadlineController deadline =
                                player.GetComponent<DeadlineController>();
                            Require(deadline != null && deadline.IsActive,
                                "Q did not activate DEADLINE in Tutorial.");
                            Require(deadline.RegisterStagedAction(),
                                "DEADLINE rejected its first staged cause.");
                            Require(deadline.RegisterStagedAction(),
                                "DEADLINE rejected its second staged cause.");
                            Require(!deadline.RegisterStagedAction(),
                                "DEADLINE accepted more than two staged causes.");
                            QueueKeyRelease(Key.Q);
                            BeginPhase(3, probe.AccumulatedDegrees);
                        }
                        break;

                    case 3:
                        if (elapsed >= 0.2)
                        {
                            movementProbeStart = player.transform.position;
                            QueueKeyPress(Key.W);
                            PumpGameplayInput(input, player);
                            input.enabled = false;
                            input.SetValidationInputState(Vector2.up, false);
                            BeginPhase(4, probe.AccumulatedDegrees);
                        }
                        break;

                    case 4:
                        if (elapsed >= 0.35)
                        {
                            CharacterAnimationController playerAnimation =
                                player.GetComponent<CharacterAnimationController>();
                            float moveBlendMagnitude = playerAnimation == null
                                ? 0f
                                : new Vector2(
                                    playerAnimation.Animator.GetFloat("MoveX"),
                                    playerAnimation.Animator.GetFloat("MoveY"))
                                .magnitude;
                            PlayerMovement playerMovement =
                                player.GetComponent<PlayerMovement>();
                            float displacement = Vector3.Distance(
                                movementProbeStart,
                                player.transform.position);
                            Require(moveBlendMagnitude > 0.1f,
                                "Tutorial movement input did not drive the character locomotion blend. " +
                                $"blend={moveBlendMagnitude:0.###}, displacement={displacement:0.###}, " +
                                $"input={input.Move}, physicallyMoving={playerMovement?.IsPhysicallyMoving}.");
                            input.SetValidationInputState(Vector2.zero, false);
                            input.enabled = true;
                            QueueKeyRelease(Key.W);
                            PumpGameplayInput(input, player);
                            DeadlineController deadline =
                                player.GetComponent<DeadlineController>();
                            Require(deadline != null && !deadline.IsActive,
                                "Movement did not release DEADLINE.");
                            Require(director.DeadlineSucceeded,
                                "Tutorial did not accept two causes plus movement release.");
                            Require(Mathf.Approximately(UnityEngine.Time.timeScale, 1f),
                                "Tutorial smoke changed global Time.timeScale.");
                            ValidateDeadlineCheckpointRestore(director, player);
                            Debug.Log(
                                "Tutorial PlayMode smoke passed: world-time probe, typed targets, " +
                                "throw stun/disarm/drop and airborne catch advance, unrestricted tutorial vision, " +
                                "animated Synty actors, locomotion blend, equipment animation profiles, " +
                                "Q activation, two-cause limit, movement release, and DEADLINE checkpoint restore.");
                            EditorApplication.ExitPlaymode();
                        }
                        break;
                }
            }
            catch (Exception exception)
            {
                RecordFailure(exception.ToString());
                EditorApplication.ExitPlaymode();
            }
        }

        private static void ValidateTargetsAndThrow(
            TutorialDirector director,
            PlayerHealth player)
        {
            WeaponController playerWeapon = player.GetComponent<WeaponController>();
            WeaponDefinition melee = AssetDatabase.LoadAssetAtPath<WeaponDefinition>(
                "Assets/_Project/MeleeWeapon.asset");
            WeaponDefinition pistol = AssetDatabase.LoadAssetAtPath<WeaponDefinition>(
                "Assets/_Project/Pistol.asset");
            TutorialTargetDummy[] targets =
                UnityEngine.Object.FindObjectsByType<TutorialTargetDummy>(
                    FindObjectsSortMode.None);
            TutorialTargetDummy meleeTarget = Array.Find(
                targets,
                target => target.RequiredAttack ==
                          TutorialTargetDummy.AcceptedAttack.Melee);
            TutorialTargetDummy pistolTarget = Array.Find(
                targets,
                target => target.RequiredAttack ==
                          TutorialTargetDummy.AcceptedAttack.Firearm);
            Require(playerWeapon != null && melee != null && pistol != null &&
                    meleeTarget != null && pistolTarget != null,
                "Tutorial target validation setup is incomplete.");

            playerWeapon.Equip(melee, 0);
            CharacterAnimationController playerAnimation =
                player.GetComponent<CharacterAnimationController>();
            CharacterAnimationLibrary animationLibrary =
                AssetDatabase.LoadAssetAtPath<CharacterAnimationLibrary>(
                    CharacterAnimationEditorSetup.LibraryPath);
            Require(playerAnimation != null && animationLibrary != null &&
                    playerAnimation.CurrentStyle == CharacterAnimationStyle.Melee &&
                    playerAnimation.Animator.runtimeAnimatorController ==
                        animationLibrary.GetController(CharacterAnimationStyle.Melee) &&
                    playerAnimation.TryPlayMeleeAttackAnimation(),
                "Tutorial melee equipment did not select or trigger the melee animation profile.");
            DamageHit syntheticHit = new DamageHit(
                1,
                meleeTarget.transform.position,
                Vector3.forward,
                player.gameObject);
            pistolTarget.ReceiveHit(syntheticHit);
            Require(pistolTarget.AcceptedHitCount == 0 &&
                    pistolTarget.RejectedHitCount == 1,
                "Firearm target accepted a melee hit.");
            meleeTarget.ReceiveHit(syntheticHit);
            Require(meleeTarget.AcceptedHitCount == 1,
                "Melee target rejected a melee weapon hit.");

            playerWeapon.Equip(pistol, pistol.AmmunitionCapacity);
            Require(playerAnimation.CurrentStyle == CharacterAnimationStyle.Pistol &&
                    playerAnimation.Animator.runtimeAnimatorController ==
                        animationLibrary.GetController(CharacterAnimationStyle.Pistol),
                "Tutorial pistol equipment did not select the pistol animation profile.");
            pistolTarget.ReceiveHit(new DamageHit(
                1,
                pistolTarget.transform.position,
                Vector3.forward,
                player.gameObject));
            Require(pistolTarget.AcceptedHitCount == 1,
                "Firearm target rejected a pistol hit.");

            GameObject throwEnemyObject = GameObject.Find(
                "Throw Lesson Armed Enemy");
            EnemyHealth throwHealth = throwEnemyObject == null
                ? null
                : throwEnemyObject.GetComponent<EnemyHealth>();
            EnemyCombatant throwBehavior = throwEnemyObject == null
                ? null
                : throwEnemyObject.GetComponent<EnemyCombatant>();
            WeaponController throwWeapon = throwEnemyObject == null
                ? null
                : throwEnemyObject.GetComponent<WeaponController>();
            Require(throwHealth != null && throwBehavior != null &&
                    throwWeapon != null && throwWeapon.HasWeapon,
                "Throw lesson enemy did not initialize armed.");
            director.EnterThrowRecoveryForValidation();
            throwHealth.ReceiveHit(new DamageHit(
                3,
                throwHealth.transform.position,
                Vector3.forward,
                player.gameObject));
            Require(throwHealth.IsAlive && throwWeapon.HasWeapon &&
                    !throwHealth.DamageEnabled,
                "Throw lesson enemy can be killed before it is disarmed.");
            throwHealth.ReceiveStun(new StunHit(
                2f,
                throwHealth.transform.position,
                Vector3.forward,
                player.gameObject));
            Require(throwHealth.IsAlive && throwHealth.IsStunned &&
                    throwBehavior.IsDisarmed && !throwWeapon.HasWeapon,
                "Throw lesson stun did not preserve life and disarm the enemy.");
            InterceptableWeapon airborneWeapon =
                UnityEngine.Object.FindFirstObjectByType<InterceptableWeapon>();
            Require(airborneWeapon != null,
                "Throw lesson enemy did not create an airborne weapon drop.");
            playerWeapon.Clear();
            director.EvaluateThrowRecoveryForValidation();
            TutorialGate entranceGate = GameObject.Find("Gate 5 - Arena Entrance")
                ?.GetComponent<TutorialGate>();
            Require(entranceGate != null && entranceGate.IsOpen,
                "Disarming the throw lesson enemy did not open the next gate.");
            Require(director.CurrentStep == TutorialDirector.TutorialStep.ThrowAndRecover,
                "Tutorial advanced before the player recovered the airborne weapon.");
            Require(airborneWeapon.TryCatch(playerWeapon) && playerWeapon.HasWeapon,
                "Player could not catch the throw lesson airborne weapon.");
            director.EvaluateThrowRecoveryForValidation();
            Require(director.CurrentStep == TutorialDirector.TutorialStep.DeadlineApproach &&
                    !throwEnemyObject.activeSelf,
                "Catching the disarmed enemy weapon did not advance to DEADLINE.");
        }

        private static void ValidateTutorialVision(PlayerHealth player)
        {
            VisionCone vision =
                UnityEngine.Object.FindFirstObjectByType<VisionCone>();
            MeshRenderer overlay = vision == null
                ? null
                : vision.GetComponent<MeshRenderer>();
            Vector3 distantPoint = player.transform.position +
                (player.transform.right * 40f);
            Require(vision != null && vision.HasUnlimitedVision &&
                    overlay != null && !overlay.enabled &&
                    vision.ContainsWorldPoint(distantPoint),
                "Tutorial VisionCone did not provide unrestricted visibility.");
        }

        private static void ValidateRuntimeCharacterVisuals(PlayerHealth player)
        {
            CharacterAnimationController[] controllers =
                UnityEngine.Object.FindObjectsByType<CharacterAnimationController>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None);
            Require(controllers.Length == 6,
                $"Tutorial initialized {controllers.Length} animated actors instead of 6.");
            for (int i = 0; i < controllers.Length; i++)
            {
                CharacterAnimationController controller = controllers[i];
                Animator animator = controller.Animator;
                CharacterVisualController visual =
                    controller.GetComponent<CharacterVisualController>();
                Require(animator != null && animator.enabled &&
                        animator.isInitialized && animator.avatar != null &&
                        animator.avatar.isHuman &&
                        animator.runtimeAnimatorController != null &&
                        animator.runtimeAnimatorController.animationClips.Length >= 7 &&
                        animator.updateMode == AnimatorUpdateMode.UnscaledTime &&
                        !animator.applyRootMotion &&
                        HasAnimatorParameter(animator, "MoveX") &&
                        HasAnimatorParameter(animator, "MoveY") &&
                        HasAnimatorParameter(animator, "Roll") &&
                        HasAnimatorParameter(animator, "AttackA") &&
                        HasAnimatorParameter(animator, "AttackB") &&
                        visual != null && visual.VisualRoot != null &&
                        visual.VisualRoot.GetComponentsInChildren<
                            SkinnedMeshRenderer>(true).Length > 0,
                    $"Tutorial animated Synty actor is not runtime-ready on {controller.name}.");
            }

            CharacterAnimationController playerAnimation =
                player.GetComponent<CharacterAnimationController>();
            Require(playerAnimation != null &&
                    playerAnimation.CurrentStyle == CharacterAnimationStyle.Unarmed,
                "Tutorial player did not initialize with the unarmed animation profile.");
        }

        private static bool HasAnimatorParameter(Animator animator, string name)
        {
            AnimatorControllerParameter[] parameters = animator.parameters;
            for (int i = 0; i < parameters.Length; i++)
            {
                if (parameters[i].name == name)
                {
                    return true;
                }
            }

            return false;
        }

        private static void ValidatePistolDispenser(PlayerHealth player)
        {
            TutorialWeaponDispenser[] dispensers =
                UnityEngine.Object.FindObjectsByType<TutorialWeaponDispenser>(
                    FindObjectsSortMode.None);
            TutorialWeaponDispenser pistolDispenser = Array.Find(
                dispensers,
                dispenser => dispenser.Definition != null &&
                             dispenser.Definition.IsFirearm &&
                             dispenser.transform.position.z < 20f);
            Require(pistolDispenser != null,
                "Tutorial pistol dispenser is missing.");
            Require(player.GetComponent<WeaponController>()?.Definition == null,
                "Tutorial pistol dispenser test requires the player to start unarmed.");

            pistolDispenser.SetAvailable(true);
            WeaponPickup spawned = pistolDispenser.SpawnedPickup;
            Require(pistolDispenser.HasSpawnedPickup &&
                    spawned != null &&
                    spawned.Definition == pistolDispenser.Definition,
                "Tutorial pistol dispenser did not spawn its firearm immediately.");
            pistolDispenser.SetAvailable(false);
        }

        private static void ValidateDeadlineCheckpointRestore(
            TutorialDirector director,
            PlayerHealth player)
        {
            WeaponController playerWeapon = player.GetComponent<WeaponController>();
            DeadlineController deadline = player.GetComponent<DeadlineController>();
            Transform resetPoint = GameObject.Find("Deadline Reset Point")?.transform;
            TutorialGate exitGate = GameObject.Find("Gate 6 - Arena Exit")
                ?.GetComponent<TutorialGate>();
            Require(playerWeapon != null && deadline != null && resetPoint != null &&
                    exitGate != null,
                "DEADLINE checkpoint validation setup is incomplete.");

            playerWeapon.Clear();
            director.RestoreDeadlineCheckpointForValidation();
            float resetDistance = Vector3.Distance(
                player.transform.position,
                resetPoint.position);
            bool checkpointRestored =
                director.CurrentStep == TutorialDirector.TutorialStep.Deadline &&
                !director.DeadlineSucceeded && !deadline.IsActive &&
                deadline.ChargesRemaining == deadline.MaxCharges &&
                playerWeapon.Definition != null &&
                playerWeapon.Definition.IsFirearm &&
                playerWeapon.Ammunition ==
                    playerWeapon.Definition.AmmunitionCapacity &&
                resetDistance <= 0.01f &&
                !exitGate.IsOpen;
            Require(checkpointRestored,
                "DEADLINE checkpoint restore did not reset the arena state and full Pistol loadout. " +
                $"step={director.CurrentStep}, succeeded={director.DeadlineSucceeded}, " +
                $"active={deadline.IsActive}, charges={deadline.ChargesRemaining}/{deadline.MaxCharges}, " +
                $"weapon={playerWeapon.Definition?.name ?? "none"}, " +
                $"ammo={playerWeapon.Ammunition}/{playerWeapon.Definition?.AmmunitionCapacity ?? 0}, " +
                $"resetDistance={resetDistance:0.###}, exitOpen={exitGate.IsOpen}.");
        }

        private static void ValidateRuntimeGateLayout()
        {
            ValidateGatePosition("Gate 1 - Time", -25f);
            ValidateGatePosition("Gate 2 - Dash", -13f);
            ValidateGatePosition("Gate 3 - Melee", -1f);
            ValidateGatePosition("Gate 4 - Pistol", 13f);
            ValidateGatePosition("Gate 5 - Arena Entrance", 34f);
            ValidateGatePosition("Gate 6 - Arena Exit", 57f);
            ValidateRuntimeGateVisuals();
            TutorialGate exitGate = GameObject.Find("Gate 6 - Arena Exit")
                ?.GetComponent<TutorialGate>();
            Require(exitGate != null,
                "Tutorial exit gate is missing.");
            exitGate.SetOpen(true, true);
            Require(!exitGate.IsVisible,
                "An opened Tutorial gate remained visible.");
            exitGate.SetOpen(false, true);
            Require(exitGate.IsVisible,
                "A closed Tutorial gate was not visible.");
        }

        private static void ValidateGatePosition(string name, float expectedZ)
        {
            GameObject gateObject = GameObject.Find(name);
            TutorialGate gate = gateObject == null
                ? null
                : gateObject.GetComponent<TutorialGate>();
            Require(gate != null &&
                    Mathf.Abs(gateObject.transform.position.z - expectedZ) <= 0.01f,
                $"Tutorial gate {name} moved from z={expectedZ:0.##} during initialization.");
        }

        private static void ValidateRuntimeGateVisuals()
        {
            string[] gateNames =
            {
                "Gate 1 - Time",
                "Gate 2 - Dash",
                "Gate 3 - Melee",
                "Gate 4 - Pistol",
                "Gate 5 - Arena Entrance",
                "Gate 6 - Arena Exit"
            };

            for (int gateIndex = 0; gateIndex < gateNames.Length; gateIndex++)
            {
                GameObject gateObject = GameObject.Find(gateNames[gateIndex]);
                Transform visualRoot = gateObject == null
                    ? null
                    : gateObject.transform.Find(GateVisualRootName);
                Require(visualRoot != null &&
                        visualRoot.childCount == GateBarCount + 2 &&
                        visualRoot.Find("Upper Shutter Rail") != null &&
                        visualRoot.Find("Lower Shutter Rail") != null &&
                        visualRoot.Find("Shutter Slat 01") == null &&
                        visualRoot.Find("Status Strip 01") == null,
                    $"Tutorial gate {gateNames[gateIndex]} does not use the expected bar visual layout.");

                for (int barIndex = 0; barIndex < GateBarCount; barIndex++)
                {
                    Transform bar = visualRoot.Find(
                        $"{GateBarNamePrefix}{barIndex + 1:00}");
                    Require(bar != null && bar.GetComponent<Renderer>() != null &&
                            bar.GetComponent<Collider>() == null &&
                            Mathf.Abs(bar.localScale.x - GateBarWidth) <= 0.001f &&
                            Mathf.Abs(bar.localScale.y - GateBarHeight) <= 0.001f &&
                            Mathf.Abs(bar.localScale.z - GateBarDepth) <= 0.001f,
                        $"Tutorial gate {gateNames[gateIndex]} has an invalid bar {barIndex + 1:00}.");
                }
            }
        }

        private static void QueueKeyPress(Key key)
        {
            EnsureKeyboard();
            UnityEngine.InputSystem.LowLevel.KeyboardState state =
                new UnityEngine.InputSystem.LowLevel.KeyboardState(key);
            UnityEngine.InputSystem.InputSystem.QueueStateEvent(
                testKeyboard, state);
            UnityEngine.InputSystem.InputSystem.Update();
        }

        private static void QueueKeyRelease(Key key)
        {
            EnsureKeyboard();
            UnityEngine.InputSystem.LowLevel.KeyboardState state =
                new UnityEngine.InputSystem.LowLevel.KeyboardState();
            UnityEngine.InputSystem.InputSystem.QueueStateEvent(
                testKeyboard, state);
            UnityEngine.InputSystem.InputSystem.Update();
        }

        private static void PumpGameplayInput(
            Deltatime.InputSystem.PlayerInputReader input,
            PlayerHealth player)
        {
            Vector2 move = new Vector2(
                (testKeyboard.dKey.isPressed ? 1f : 0f) -
                (testKeyboard.aKey.isPressed ? 1f : 0f),
                (testKeyboard.wKey.isPressed ? 1f : 0f) -
                (testKeyboard.sKey.isPressed ? 1f : 0f));
            input.SetValidationInputState(
                move,
                testKeyboard.qKey.isPressed);
            DeadlineController deadline =
                player.GetComponent<DeadlineController>();
            Require(deadline != null,
                "Tutorial player is missing DeadlineController.");
            System.Reflection.MethodInfo update =
                typeof(DeadlineController).GetMethod(
                    "Update",
                    System.Reflection.BindingFlags.Instance |
                    System.Reflection.BindingFlags.NonPublic);
            Require(update != null,
                "DeadlineController.Update could not be resolved.");
            update.Invoke(deadline, null);
        }

        private static void ValidateDeadlineBinding()
        {
            UnityEngine.InputSystem.InputActionAsset asset =
                AssetDatabase.LoadAssetAtPath<
                    UnityEngine.InputSystem.InputActionAsset>(
                    "Assets/_Project/Input/PlayerControls.inputactions");
            UnityEngine.InputSystem.InputAction action =
                asset == null ? null : asset.FindAction("Gameplay/Deadline");
            bool hasQBinding = false;
            if (action != null)
            {
                foreach (UnityEngine.InputSystem.InputBinding binding in
                         action.bindings)
                {
                    if (string.Equals(
                            binding.path,
                            "<Keyboard>/q",
                            StringComparison.OrdinalIgnoreCase))
                    {
                        hasQBinding = true;
                        break;
                    }
                }
            }

            Require(hasQBinding,
                "Gameplay/Deadline is not bound to <Keyboard>/q.");
        }

        private static void EnsureKeyboard()
        {
            if (testKeyboard != null)
            {
                return;
            }

            testKeyboard = Keyboard.current;
            if (testKeyboard == null)
            {
                testKeyboard = UnityEngine.InputSystem.InputSystem
                    .AddDevice<Keyboard>();
                addedKeyboard = true;
            }
        }

        private static void BeginPhase(int nextPhase, float probeDegrees)
        {
            phase = nextPhase;
            phaseStartedAt = EditorApplication.timeSinceStartup;
            phaseProbeStart = probeDegrees;
        }

        private static void HandleLog(
            string condition,
            string stackTrace,
            LogType type)
        {
            if (type == LogType.Error || type == LogType.Exception ||
                type == LogType.Assert)
            {
                RecordFailure(condition + "\n" + stackTrace);
            }
        }

        private static void RecordFailure(string message)
        {
            SessionState.SetBool(FailedKey, true);
            if (string.IsNullOrEmpty(SessionState.GetString(FailureKey, string.Empty)))
            {
                SessionState.SetString(FailureKey, message);
            }
        }

        private static void Finish()
        {
            Runner.Detach();
            if (addedKeyboard && testKeyboard != null)
            {
                UnityEngine.InputSystem.InputSystem.RemoveDevice(testKeyboard);
            }

            testKeyboard = null;
            addedKeyboard = false;
            bool failed = SessionState.GetBool(FailedKey, false);
            string failure = SessionState.GetString(FailureKey, string.Empty);
            SessionState.SetBool(RunningKey, false);
            SessionState.EraseBool(FailedKey);
            SessionState.EraseString(FailureKey);
            SessionState.EraseString(ScenePathKey);

            if (Application.isBatchMode)
            {
                if (failed)
                {
                    Debug.LogError("Tutorial smoke failed: " + failure);
                    EditorApplication.Exit(1);
                }
                else
                {
                    EditorApplication.Exit(0);
                }
            }
            else if (failed)
            {
                Debug.LogError("Tutorial smoke failed: " + failure);
            }
        }

        private static void Require(bool condition, string message)
        {
            if (!condition)
            {
                throw new InvalidOperationException(message);
            }
        }
    }
}
