using System;
using System.Collections;
using Deltatime.Combat;
using Deltatime.Core;
using Deltatime.Player;
using Deltatime.TimeSystem;
using Deltatime.Visuals;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Deltatime.EditorTools
{
    [InitializeOnLoad]
    public static class Stage1CharacterAnimationPlayModeSmokeTest
    {
        private const string ScenePath =
            "Assets/_Project/Scenes/Stage1.unity";
        private const string PistolPath = "Assets/_Project/Pistol.asset";
        private const string RiflePath =
            "Assets/_Project/AutomaticRifle.asset";
        private const string ShotgunPath = "Assets/_Project/Shotgun.asset";
        private const string MeleePath = "Assets/_Project/MeleeWeapon.asset";
        private const string PickupPrefabPath =
            "Assets/_Project/Prefabs/WeaponPickup.prefab";
        private const string ThrownWeaponPrefabPath =
            "Assets/_Project/Prefabs/ThrownWeapon.prefab";
        private const string InterceptableWeaponPrefabPath =
            "Assets/_Project/Prefabs/InterceptableWeapon.prefab";
        private const string AimPivotName = "Weapon Aim Pivot";
        private const string RunningKey =
            "Deltatime.Stage1CharacterAnimationSmoke.Running";
        private const string FailedKey =
            "Deltatime.Stage1CharacterAnimationSmoke.Failed";
        private const string FailureKey =
            "Deltatime.Stage1CharacterAnimationSmoke.Failure";
        private const string PhaseKey =
            "Deltatime.Stage1CharacterAnimationSmoke.Phase";

        private static readonly CommandLineSmokeRunner Runner =
            new CommandLineSmokeRunner(
                RunningKey,
                Tick,
                HandlePlayModeStateChanged,
                HandleLog);
        private static bool validationRan;
        private static double playStartedAt;
        private static double meleeAttackStartedAt;
        private static TimingTarget timingTarget;
        private static GameObject timingTargetObject;
        private static WeaponController timingWeapon;
        private static CharacterAnimationController timingAnimation;
        private static WeaponDefinition timingOriginalDefinition;
        private static int timingOriginalAmmunition;
        private static bool preImpactCheckCompleted;
        private static bool weaponVisualValidationPending;
        private static GameObject weaponVisualValidationRunnerObject;

        static Stage1CharacterAnimationPlayModeSmokeTest()
        {
            if (SessionState.GetBool(RunningKey, false))
            {
                AttachCallbacks();
            }
        }

        public static void RunFromCommandLine()
        {
            SessionState.SetBool(RunningKey, true);
            SessionState.SetBool(FailedKey, false);
            SessionState.SetString(FailureKey, string.Empty);
            SessionState.SetString(PhaseKey, "entering");
            Runner.OpenSceneAndEnterPlayMode(ScenePath);
        }

        private static void AttachCallbacks()
        {
            Runner.Attach();
        }

        private static void DetachCallbacks()
        {
            Runner.Detach();
        }

        private static void HandlePlayModeStateChanged(PlayModeStateChange state)
        {
            if (!SessionState.GetBool(RunningKey, false))
            {
                return;
            }

            if (state == PlayModeStateChange.EnteredPlayMode)
            {
                playStartedAt = EditorApplication.timeSinceStartup;
                validationRan = false;
                meleeAttackStartedAt = 0d;
                timingTarget = null;
                timingTargetObject = null;
                timingWeapon = null;
                timingAnimation = null;
                preImpactCheckCompleted = false;
                weaponVisualValidationPending = false;
                weaponVisualValidationRunnerObject = null;
                SessionState.SetString(PhaseKey, "playing");
            }
            else if (state == PlayModeStateChange.ExitingPlayMode)
            {
                SessionState.SetString(PhaseKey, "stopping");
            }
            else if (state == PlayModeStateChange.EnteredEditMode &&
                     SessionState.GetString(PhaseKey, string.Empty) ==
                         "stopping")
            {
                Finish();
            }
        }

        private static void Tick()
        {
            if (!SessionState.GetBool(RunningKey, false))
            {
                DetachCallbacks();
                return;
            }

            if (!EditorApplication.isPlaying)
            {
                return;
            }

            double elapsed = EditorApplication.timeSinceStartup - playStartedAt;
            if (!validationRan && elapsed >= 0.8d)
            {
                validationRan = true;
                try
                {
                    ValidateRuntimeState();
                }
                catch (Exception exception)
                {
                    RecordFailure(exception.ToString());
                }

                if (timingTarget == null && !weaponVisualValidationPending)
                {
                    EditorApplication.isPlaying = false;
                }
            }
            else if (validationRan &&
                     timingTarget != null &&
                     !preImpactCheckCompleted &&
                     elapsed >= meleeAttackStartedAt + 0.18d)
            {
                try
                {
                    Require(timingTarget.HitCount == 0,
                        "Melee damage was applied before the animation impact frame.");
                    preImpactCheckCompleted = true;
                }
                catch (Exception exception)
                {
                    RecordFailure(exception.ToString());
                    EditorApplication.isPlaying = false;
                }
            }
            else if (validationRan &&
                     timingTarget != null &&
                     elapsed >= meleeAttackStartedAt + 0.85d)
            {
                try
                {
                    Require(timingTarget.HitCount == 1,
                        "Melee damage did not resolve at the animation impact frame.");
                    timingWeapon.Equip(
                        timingOriginalDefinition,
                        timingOriginalAmmunition);
                    Require(
                        timingAnimation != null &&
                        timingAnimation.CurrentStyle ==
                            CharacterAnimationStyle.Pistol,
                        "Stage1 player did not restore the starting pistol profile.");
                }
                catch (Exception exception)
                {
                    RecordFailure(exception.ToString());
                }
                finally
                {
                    if (timingTargetObject != null)
                    {
                        UnityEngine.Object.Destroy(timingTargetObject);
                    }

                    timingTargetObject = null;
                    timingTarget = null;
                    EditorApplication.isPlaying = false;
                }
            }
            else if (elapsed >= 15d)
            {
                RecordFailure(
                    "Stage1 character animation play-mode smoke test timed out.");
                EditorApplication.isPlaying = false;
            }
        }

        private static void ValidateRuntimeState()
        {
            Require(
                SceneManager.GetActiveScene().path == ScenePath,
                $"Unexpected scene: {SceneManager.GetActiveScene().path}");

            CharacterAnimationController[] controllers =
                UnityEngine.Object.FindObjectsByType<CharacterAnimationController>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None);
            Require(
                controllers.Length == 4,
                $"Stage1 initialized {controllers.Length} animated actors instead of 4.");

            CharacterAnimationLibrary library =
                AssetDatabase.LoadAssetAtPath<CharacterAnimationLibrary>(
                    CharacterAnimationEditorSetup.LibraryPath);
            WorldTimeController worldTime =
                UnityEngine.Object.FindFirstObjectByType<WorldTimeController>();
            Require(library != null, "Character animation library is missing.");
            Require(worldTime != null, "Stage1 world time is missing.");

            for (int i = 0; i < controllers.Length; i++)
            {
                CharacterAnimationController controller = controllers[i];
                Animator animator = controller.Animator;
                Require(
                    animator != null &&
                    animator.enabled &&
                    animator.isInitialized &&
                    animator.avatar != null &&
                    animator.avatar.isHuman &&
                    animator.avatar.isValid &&
                    !animator.applyRootMotion &&
                    animator.updateMode == AnimatorUpdateMode.UnscaledTime &&
                    animator.runtimeAnimatorController != null &&
                    animator.runtimeAnimatorController.animationClips.Length >= 7 &&
                    animator.layerCount == 2 &&
                    animator.GetLayerName(1) == "Upper Body Attack" &&
                    Mathf.Approximately(animator.GetLayerWeight(1), 1f) &&
                    controller.GetComponent<MeleeAttackExecution>() != null &&
                    controller.GetComponent<WeaponVisualPresenter>() != null,
                    $"Stage1 Animator is not runtime-ready on {controller.name}.");
                Require(
                    HasParameter(animator, "MoveX") &&
                    HasParameter(animator, "MoveY") &&
                    HasParameter(animator, "Roll") &&
                    HasParameter(animator, "AttackA") &&
                    HasParameter(animator, "AttackB"),
                    $"Stage1 Animator parameters are incomplete on {controller.name}.");
                if (controller.IsEnemy)
                {
                    Require(
                        Mathf.Approximately(
                            animator.speed,
                            worldTime.CurrentTimeScale),
                        $"Enemy animation time scale is incorrect on {controller.name}.");
                }
            }

            GameObject player = GameObject.Find("Player");
            CharacterAnimationController playerAnimation =
                player == null
                    ? null
                    : player.GetComponent<CharacterAnimationController>();
            WeaponController playerWeapon =
                player == null ? null : player.GetComponent<WeaponController>();
            Require(
                playerAnimation != null && playerWeapon != null,
                "Stage1 player animation or weapon controller is missing.");

            WeaponDefinition pistol = LoadWeapon(PistolPath);
            WeaponDefinition rifle = LoadWeapon(RiflePath);
            WeaponDefinition shotgun = LoadWeapon(ShotgunPath);
            WeaponDefinition melee = LoadWeapon(MeleePath);
            WeaponDefinition originalDefinition = playerWeapon.Definition;
            int originalAmmunition = playerWeapon.Ammunition;

            RequireProfile(
                playerWeapon,
                playerAnimation,
                library,
                null,
                CharacterAnimationStyle.Unarmed);
            RequireProfile(
                playerWeapon,
                playerAnimation,
                library,
                pistol,
                CharacterAnimationStyle.Pistol);
            RequireProfile(
                playerWeapon,
                playerAnimation,
                library,
                rifle,
                CharacterAnimationStyle.Rifle);
            RequireProfile(
                playerWeapon,
                playerAnimation,
                library,
                shotgun,
                CharacterAnimationStyle.Rifle);
            RequireProfile(
                playerWeapon,
                playerAnimation,
                library,
                melee,
                CharacterAnimationStyle.Melee);
            BeginWeaponModelVisualValidation(
                player,
                playerAnimation,
                playerWeapon,
                worldTime,
                pistol,
                rifle,
                shotgun,
                melee,
                originalDefinition,
                originalAmmunition);
        }

        private static WeaponDefinition LoadWeapon(string path)
        {
            WeaponDefinition definition =
                AssetDatabase.LoadAssetAtPath<WeaponDefinition>(path);
            Require(definition != null, $"Weapon definition is missing: {path}");
            return definition;
        }

        private static void RequireProfile(
            WeaponController weapon,
            CharacterAnimationController animation,
            CharacterAnimationLibrary library,
            WeaponDefinition definition,
            CharacterAnimationStyle expectedStyle)
        {
            if (definition == null)
            {
                weapon.Clear();
            }
            else
            {
                weapon.Equip(definition, definition.AmmunitionCapacity);
            }

            Require(
                animation.CurrentStyle == expectedStyle &&
                animation.Animator.runtimeAnimatorController ==
                    library.GetController(expectedStyle),
                $"Equipment profile did not switch to {expectedStyle}.");
        }

        private static void BeginWeaponModelVisualValidation(
            GameObject player,
            CharacterAnimationController playerAnimation,
            WeaponController playerWeapon,
            WorldTimeController worldTime,
            WeaponDefinition pistol,
            WeaponDefinition rifle,
            WeaponDefinition shotgun,
            WeaponDefinition melee,
            WeaponDefinition originalDefinition,
            int originalAmmunition)
        {
            weaponVisualValidationPending = true;
            weaponVisualValidationRunnerObject = new GameObject(
                "Stage1 Weapon Visual Validation");
            WeaponVisualValidationRunner runner =
                weaponVisualValidationRunnerObject.AddComponent<
                    WeaponVisualValidationRunner>();
            runner.Begin(ValidateWeaponModelVisualsAfterFrame(
                player,
                playerAnimation,
                playerWeapon,
                worldTime,
                pistol,
                rifle,
                shotgun,
                melee,
                originalDefinition,
                originalAmmunition));
        }

        private static IEnumerator ValidateWeaponModelVisualsAfterFrame(
            GameObject player,
            CharacterAnimationController playerAnimation,
            WeaponController playerWeapon,
            WorldTimeController worldTime,
            WeaponDefinition pistol,
            WeaponDefinition rifle,
            WeaponDefinition shotgun,
            WeaponDefinition melee,
            WeaponDefinition originalDefinition,
            int originalAmmunition)
        {
            try
            {
                GameObject pickupPrefab =
                    AssetDatabase.LoadAssetAtPath<GameObject>(PickupPrefabPath);
                Require(pickupPrefab != null,
                    "Weapon pickup prefab is missing for model validation.");
                PlayerAim playerAim = player.GetComponent<PlayerAim>();
                Require(playerAim != null,
                    "Player aim is missing for firearm visual validation.");

                WeaponDefinition[] definitions =
                {
                    pistol,
                    rifle,
                    shotgun,
                    melee
                };
                for (int i = 0; i < definitions.Length; i++)
                {
                    WeaponDefinition definition = definitions[i];
                    Require(
                        definition.HasCustomHeldVisual &&
                        definition.HasCustomWorldVisual,
                        $"{definition.DisplayName} is missing its model visual prefabs.");
                    playerWeapon.Equip(
                        definition,
                        definition.AmmunitionCapacity);

                    // Resume only after the next player-loop frame has run the
                    // Animator and WeaponVisualPresenter.LateUpdate.
                    yield return null;

                    ValidateHeldWeaponModel(
                        playerAnimation,
                        playerWeapon,
                        definition);
                    if (definition.IsFirearm)
                    {
                        ValidatePlayerFirearmAimAlignment(
                            playerAim,
                            playerWeapon,
                            definition);
                    }
                    else
                    {
                        ValidateMeleeAimPivot(
                            playerAnimation,
                            playerWeapon,
                            definition);
                    }
                    ValidateGroundWeaponModel(pickupPrefab, definition);
                    ValidateFlyingWeaponVisuals(
                        player,
                        worldTime,
                        pickupPrefab,
                        definition);
                }

                playerWeapon.Equip(melee, melee.AmmunitionCapacity);
                yield return null;
                timingAnimation = playerAnimation;
                BeginMeleeTimingValidation(player, playerWeapon, melee);
                timingOriginalDefinition = originalDefinition;
                timingOriginalAmmunition = originalAmmunition;
            }
            finally
            {
                weaponVisualValidationPending = false;
                if (weaponVisualValidationRunnerObject != null)
                {
                    UnityEngine.Object.Destroy(
                        weaponVisualValidationRunnerObject);
                    weaponVisualValidationRunnerObject = null;
                }
            }
        }

        private static void ValidateHeldWeaponModel(
            CharacterAnimationController playerAnimation,
            WeaponController playerWeapon,
            WeaponDefinition definition)
        {
            Transform hand = playerAnimation.Animator.GetBoneTransform(
                HumanBodyBones.RightHand);
            Transform muzzle = playerWeapon.Muzzle;
            Transform aimPivot = FindAncestor(muzzle, AimPivotName);
            Transform heldModel = aimPivot == null
                ? null
                : aimPivot.Find("Held Weapon Model");
            bool isAttached =
                hand != null &&
                aimPivot != null &&
                aimPivot.parent == hand &&
                heldModel != null &&
                playerWeapon.CustomHeldVisualActive &&
                muzzle != null &&
                muzzle.name == "Weapon Muzzle" &&
                muzzle.IsChildOf(aimPivot);
            if (!isAttached)
            {
                Debug.Log(
                    $"Held hierarchy diagnostic for {definition.DisplayName}: " +
                    $"hand={hand}, pivot={aimPivot}, " +
                    $"model={heldModel}, " +
                    $"customActive={playerWeapon.CustomHeldVisualActive}, " +
                    $"muzzle={muzzle}, muzzleParent={muzzle?.parent}.");
            }

            Require(
                isAttached,
                $"{definition.DisplayName} model or muzzle was not attached to the player's right hand.");
        }

        private static Transform FindAncestor(Transform transform, string name)
        {
            for (Transform current = transform;
                 current != null;
                 current = current.parent)
            {
                if (current.name == name)
                {
                    return current;
                }
            }

            return null;
        }

        private static void ValidatePlayerFirearmAimAlignment(
            PlayerAim playerAim,
            WeaponController playerWeapon,
            WeaponDefinition definition)
        {
            Transform muzzle = playerWeapon.Muzzle;
            Vector3 muzzleForward = Vector3.ProjectOnPlane(
                muzzle.forward,
                Vector3.up);
            Vector3 targetDirection = playerAim.GetPlanarDirectionFrom(
                muzzle.position);
            Require(
                muzzleForward.sqrMagnitude > 0.000001f &&
                targetDirection.sqrMagnitude > 0.000001f &&
                Vector3.Angle(
                    muzzleForward.normalized,
                    targetDirection.normalized) <= 0.25f,
                $"{definition.DisplayName} visual muzzle is not aligned with player aim.");
        }

        private static void ValidateMeleeAimPivot(
            CharacterAnimationController playerAnimation,
            WeaponController playerWeapon,
            WeaponDefinition definition)
        {
            Transform hand = playerAnimation.Animator.GetBoneTransform(
                HumanBodyBones.RightHand);
            Transform aimPivot = FindAncestor(
                playerWeapon.Muzzle,
                AimPivotName);
            Require(
                hand != null &&
                aimPivot != null &&
                aimPivot.parent == hand &&
                Quaternion.Angle(
                    aimPivot.localRotation,
                    Quaternion.identity) <= 0.01f,
                $"{definition.DisplayName} must not receive firearm aim-pivot rotation.");
        }

        private static void ValidateGroundWeaponModel(
            GameObject pickupPrefab,
            WeaponDefinition definition)
        {
            GameObject pickupObject = UnityEngine.Object.Instantiate(pickupPrefab);
            try
            {
                WeaponPickup pickup = pickupObject.GetComponent<WeaponPickup>();
                Require(pickup != null,
                    "Weapon pickup prefab is missing its component.");
                pickup.Initialize(definition, definition.AmmunitionCapacity);
                Require(
                    pickup.transform.Find("Weapon Model Visual") != null,
                    $"{definition.DisplayName} model was not created for the ground pickup.");
            }
            finally
            {
                UnityEngine.Object.Destroy(pickupObject);
            }
        }

        private static void ValidateFlyingWeaponVisuals(
            GameObject player,
            WorldTimeController worldTime,
            GameObject pickupPrefabObject,
            WeaponDefinition definition)
        {
            ThrownWeapon thrownPrefab = LoadPrefabComponent<ThrownWeapon>(
                ThrownWeaponPrefabPath);
            InterceptableWeapon interceptablePrefab =
                LoadPrefabComponent<InterceptableWeapon>(
                    InterceptableWeaponPrefabPath);
            WeaponPickup pickupPrefab =
                pickupPrefabObject.GetComponent<WeaponPickup>();
            Require(pickupPrefab != null,
                "Weapon pickup prefab is missing its component for flight validation.");

            GameObject thrownObject = UnityEngine.Object.Instantiate(
                thrownPrefab.gameObject,
                player.transform.position + (Vector3.up * 4f),
                Quaternion.identity);
            GameObject interceptableObject = UnityEngine.Object.Instantiate(
                interceptablePrefab.gameObject,
                player.transform.position + (Vector3.up * 6f),
                Quaternion.identity);
            try
            {
                ThrownWeapon thrown = thrownObject.GetComponent<ThrownWeapon>();
                thrown.Initialize(
                    worldTime,
                    pickupPrefab,
                    definition,
                    definition.AmmunitionCapacity,
                    CombatFaction.Player,
                    player,
                    Vector3.forward);
                RequireFlightModel(
                    thrownObject,
                    thrownObject.GetComponent<Renderer>(),
                    $"Player-thrown {definition.DisplayName}");

                InterceptableWeapon interceptable =
                    interceptableObject.GetComponent<InterceptableWeapon>();
                interceptable.Initialize(
                    worldTime,
                    pickupPrefab,
                    definition,
                    definition.AmmunitionCapacity,
                    Vector3.forward);
                Transform body = interceptableObject.transform.Find("Body");
                RequireFlightModel(
                    interceptableObject,
                    body == null ? null : body.GetComponent<Renderer>(),
                    $"Disarmed {definition.DisplayName}");
            }
            finally
            {
                UnityEngine.Object.Destroy(thrownObject);
                UnityEngine.Object.Destroy(interceptableObject);
            }
        }

        private static T LoadPrefabComponent<T>(string path) where T : Component
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            T component = prefab == null ? null : prefab.GetComponent<T>();
            Require(component != null, $"Weapon flight prefab is missing: {path}");
            return component;
        }

        private static void RequireFlightModel(
            GameObject flightObject,
            Renderer fallbackRenderer,
            string description)
        {
            WeaponFlightVisualPresenter presenter =
                flightObject.GetComponent<WeaponFlightVisualPresenter>();
            Require(
                presenter != null &&
                presenter.HasCustomModel &&
                flightObject.transform.Find("Flying Weapon Model") != null,
                $"{description} did not create the weapon flight model.");
            Require(
                fallbackRenderer != null && !fallbackRenderer.enabled,
                $"{description} still displays its prototype cube.");
        }

        private static void BeginMeleeTimingValidation(
            GameObject player,
            WeaponController playerWeapon,
            WeaponDefinition melee)
        {
            MeleeAttackExecution execution =
                player.GetComponent<MeleeAttackExecution>();
            Require(execution != null,
                "Player melee execution component is missing.");

            timingTargetObject = new GameObject("Melee Timing Target");
            timingTargetObject.transform.position =
                player.transform.position +
                (player.transform.forward * 0.85f);
            SphereCollider collider =
                timingTargetObject.AddComponent<SphereCollider>();
            collider.radius = 0.25f;
            timingTarget = timingTargetObject.AddComponent<TimingTarget>();
            timingWeapon = playerWeapon;
            Require(
                playerWeapon.TryMeleeAttack(
                    CombatFaction.Player,
                    player,
                    player.transform.forward,
                    Time.unscaledTime,
                    execution),
                "Player could not begin the melee timing validation attack.");
            Require(timingTarget.HitCount == 0,
                "Melee damage was applied immediately instead of at impact.");
            meleeAttackStartedAt = EditorApplication.timeSinceStartup -
                                   playStartedAt;
        }

        private static bool HasParameter(Animator animator, string name)
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

        private static void Require(bool condition, string message)
        {
            if (!condition)
            {
                throw new InvalidOperationException(message);
            }
        }

        private sealed class WeaponVisualValidationRunner : MonoBehaviour
        {
            public void Begin(IEnumerator routine)
            {
                StartCoroutine(Run(routine));
            }

            private static IEnumerator Run(IEnumerator routine)
            {
                while (true)
                {
                    object current;
                    try
                    {
                        if (!routine.MoveNext())
                        {
                            yield break;
                        }

                        current = routine.Current;
                    }
                    catch (Exception exception)
                    {
                        RecordFailure(exception.ToString());
                        EditorApplication.isPlaying = false;
                        yield break;
                    }

                    yield return current;
                }
            }
        }

        private sealed class TimingTarget : MonoBehaviour, IDamageable
        {
            public int HitCount { get; private set; }
            public CombatFaction Faction => CombatFaction.Enemy;
            public bool IsAlive => true;

            public void ReceiveHit(DamageHit hit)
            {
                HitCount++;
            }
        }

        private static void HandleLog(
            string condition,
            string stackTrace,
            LogType type)
        {
            if (!SessionState.GetBool(RunningKey, false) ||
                (type != LogType.Error &&
                 type != LogType.Exception &&
                 type != LogType.Assert))
            {
                return;
            }

            RecordFailure(condition + Environment.NewLine + stackTrace);
        }

        private static void RecordFailure(string failure)
        {
            if (SessionState.GetBool(FailedKey, false))
            {
                return;
            }

            SessionState.SetBool(FailedKey, true);
            SessionState.SetString(FailureKey, failure);
        }

        private static void Finish()
        {
            bool failed = SessionState.GetBool(FailedKey, false);
            string failure = SessionState.GetString(FailureKey, string.Empty);
            SessionState.SetBool(RunningKey, false);
            SessionState.SetString(PhaseKey, string.Empty);
            DetachCallbacks();

            if (failed)
            {
                Debug.LogError(
                    "Stage1 character animation play-mode smoke test failed:\n" +
                    failure);
                EditorApplication.Exit(1);
                return;
            }

            Debug.Log(
                "Stage1 character animation play-mode smoke test passed.");
            EditorApplication.Exit(0);
        }
    }
}
