using System;
using System.Text;
using Deltatime.Combat;
using Deltatime.Core;
using Deltatime.Enemies;
using Deltatime.Level;
using Deltatime.Player;
using Deltatime.Replay;
using Deltatime.TimeSystem;
using Deltatime.UI;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Deltatime.EditorTools
{
    [InitializeOnLoad]
    public static class PrototypePlayModeSmokeTest
    {
        private const string ScenePath = "Assets/_Project/Scenes/Stage2.unity";
        private const string ThrownWeaponPrefabPath =
            "Assets/_Project/Prefabs/ThrownWeapon.prefab";
        private const string WeaponPickupPrefabPath =
            "Assets/_Project/Prefabs/WeaponPickup.prefab";
        private static readonly Vector3 RangeProbeStart =
            new Vector3(8.5f, 5f, 0f);
        private static readonly Vector3 RangeProbeLanding =
            new Vector3(8.5f, 0.18f, 6f);
        private const string RunningKey = "Deltatime.Smoke.Running";
        private const string FailedKey = "Deltatime.Smoke.Failed";
        private const string FailureTextKey = "Deltatime.Smoke.FailureText";
        private const string PhaseKey = "Deltatime.Smoke.Phase";

        private static double playStartedAt;
        private static bool checksRan;
        private static bool movementChecksRan;
        private static bool stunChecksRan;
        private static bool replayChecksRan;
        private static bool deadlineLongProbeStarted;
        private static bool deadlineLongProbeReleased;
        private static bool deadlineShortProbeStarted;
        private static bool deadlineShortProbeReleased;
        private static bool stageClearTriggered;
        private static bool deadlineReplayFinalValidationRan;
        private static bool replayTimelineOrderValidated;
        private static bool replayAnimatedProxyObserved;
        private static bool replayVisionObservedDuringDeadline;
        private static bool replayVisionObservedAfterLoop;
        private static float previousReplayElapsed;
        private static float previousReplaySourceTimestamp;
        private static float deadlineLongWorldStart;
        private static readonly CommandLineSmokeRunner Runner =
            new CommandLineSmokeRunner(
                RunningKey,
                Tick,
                HandlePlayModeStateChanged,
                HandleLog);
        private static readonly System.Collections.Generic.Dictionary<int, Vector3>
            EnemyMovementStarts =
                new System.Collections.Generic.Dictionary<int, Vector3>();
        private static readonly System.Collections.Generic.Dictionary<int, float>
            EnemyMovementDistances =
                new System.Collections.Generic.Dictionary<int, float>();

        static PrototypePlayModeSmokeTest()
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
            SessionState.SetString(FailureTextKey, string.Empty);
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
                checksRan = false;
                movementChecksRan = false;
                stunChecksRan = false;
                replayChecksRan = false;
                ResetDeadlineReplayProbe();
                replayVisionObservedDuringDeadline = false;
                replayVisionObservedAfterLoop = false;
                SessionState.SetString(PhaseKey, "playing");
            }
            else if (state == PlayModeStateChange.ExitingPlayMode)
            {
                SessionState.SetString(PhaseKey, "stopping");
            }
            else if (state == PlayModeStateChange.EnteredEditMode &&
                     SessionState.GetString(PhaseKey, string.Empty) == "stopping")
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

            string phase = SessionState.GetString(PhaseKey, string.Empty);
            if (EditorApplication.isPlaying)
            {
                if (phase != "playing")
                {
                    playStartedAt = EditorApplication.timeSinceStartup;
                    checksRan = false;
                    movementChecksRan = false;
                    stunChecksRan = false;
                    replayChecksRan = false;
                    ResetDeadlineReplayProbe();
                    replayVisionObservedDuringDeadline = false;
                    replayVisionObservedAfterLoop = false;
                    SessionState.SetString(PhaseKey, "playing");
                }

                double elapsed = EditorApplication.timeSinceStartup - playStartedAt;
                if (checksRan && !movementChecksRan)
                {
                    SustainMovementProbe();
                }

                if (!checksRan && elapsed >= 0.5d)
                {
                    checksRan = true;
                    ValidateRuntimeState();
                    ValidateWarningLineTracking();
                    BeginMovementValidation();
                }

                if (!movementChecksRan && elapsed >= 1.1d)
                {
                    movementChecksRan = true;
                    ValidateEnemyMovement();
                    BeginStunValidation();
                }

                if (!stunChecksRan && elapsed >= 3.6d)
                {
                    stunChecksRan = true;
                    ValidateStunRecovery();
                    BeginDeadlineReplayProbe(false);
                }

                if ((deadlineLongProbeStarted && !deadlineLongProbeReleased) ||
                    (deadlineShortProbeStarted && !deadlineShortProbeReleased))
                {
                    SustainDeadlineReplayProbe();
                }

                if (deadlineLongProbeStarted &&
                    !deadlineLongProbeReleased &&
                    elapsed >= 4.65d)
                {
                    ReleaseDeadlineReplayProbe(true);
                }

                if (deadlineLongProbeReleased &&
                    !deadlineShortProbeStarted &&
                    elapsed >= 5.2d)
                {
                    BeginDeadlineReplayProbe(true);
                }

                if (deadlineShortProbeStarted &&
                    !deadlineShortProbeReleased &&
                    elapsed >= 5.45d)
                {
                    ReleaseDeadlineReplayProbe(false);
                }

                if (deadlineShortProbeReleased &&
                    !stageClearTriggered &&
                    elapsed >= 6.35d)
                {
                    stageClearTriggered = true;
                    ClearStage();
                }

                if (!replayChecksRan && elapsed >= 6.7d)
                {
                    replayChecksRan = true;
                    ValidateReplayState();
                }

                if (replayChecksRan)
                {
                    ValidateReplayPresentationOrder();
                }

                if (!deadlineReplayFinalValidationRan && elapsed >= 15.5d)
                {
                    deadlineReplayFinalValidationRan = true;
                    Require(
                        replayTimelineOrderValidated,
                        "Replay source events did not advance monotonically on the normalized timeline.");
                    Require(
                        replayAnimatedProxyObserved,
                        "Replay never displayed an animated skinned-mesh proxy.");
                    Require(
                        replayVisionObservedDuringDeadline,
                        "Replay did not preserve dark vision during a Deadline segment.");
                    Require(
                        replayVisionObservedAfterLoop,
                        "Replay did not preserve dark vision after looping.");
                }

                if (elapsed >= 16d)
                {
                    SessionState.SetString(PhaseKey, "stopping");
                    EditorApplication.isPlaying = false;
                }
            }
            else if (phase == "stopping")
            {
                Finish();
            }
        }

        private static void ResetDeadlineReplayProbe()
        {
            deadlineLongProbeStarted = false;
            deadlineLongProbeReleased = false;
            deadlineShortProbeStarted = false;
            deadlineShortProbeReleased = false;
            stageClearTriggered = false;
            deadlineReplayFinalValidationRan = false;
            replayTimelineOrderValidated = false;
            replayAnimatedProxyObserved = false;
            previousReplayElapsed = -1f;
            previousReplaySourceTimestamp = -1f;
            deadlineLongWorldStart = 0f;
        }

        private static void BeginDeadlineReplayProbe(bool shortProbe)
        {
            DeadlineController deadline =
                UnityEngine.Object.FindObjectOfType<DeadlineController>();
            WorldTimeController worldTime =
                UnityEngine.Object.FindObjectOfType<WorldTimeController>();
            WorldTimeActivity activity =
                UnityEngine.Object.FindObjectOfType<WorldTimeActivity>();
            PlayerAim aim = UnityEngine.Object.FindObjectOfType<PlayerAim>();
            Camera camera = Camera.main;
            System.Reflection.MethodInfo activateMethod =
                typeof(DeadlineController).GetMethod(
                    "ActivateDeadline",
                    System.Reflection.BindingFlags.Instance |
                    System.Reflection.BindingFlags.NonPublic);

            Require(
                deadline != null &&
                worldTime != null &&
                activity != null &&
                aim != null &&
                camera != null &&
                activateMethod != null,
                "Deadline replay probe dependencies are missing.");
            if (deadline == null ||
                worldTime == null ||
                activity == null ||
                aim == null ||
                camera == null ||
                activateMethod == null)
            {
                return;
            }

            if (!shortProbe)
            {
                EnemyHealth[] enemies =
                    UnityEngine.Object.FindObjectsOfType<EnemyHealth>();
                for (int i = 0; i < enemies.Length; i++)
                {
                    enemies[i].ReceiveStun(new StunHit(
                        10f,
                        enemies[i].transform.position,
                        Vector3.forward,
                        null));
                }

                deadlineLongWorldStart = worldTime.WorldElapsedTime;
                deadlineLongProbeStarted = true;
            }
            else
            {
                deadlineShortProbeStarted = true;
            }

            aim.enabled = false;
            activity.SetAimTurn(1f);
            camera.transform.position += shortProbe
                ? new Vector3(-0.45f, 0.12f, 0.25f)
                : new Vector3(0.7f, 0.2f, -0.35f);

            try
            {
                activateMethod.Invoke(deadline, null);
            }
            catch (Exception exception)
            {
                RecordFailure(
                    $"Deadline replay probe activation threw: {exception}");
                return;
            }

            Require(
                deadline.IsActive,
                "Deadline replay probe did not activate Deadline.");
        }

        private static void SustainDeadlineReplayProbe()
        {
            WorldTimeActivity activity =
                UnityEngine.Object.FindObjectOfType<WorldTimeActivity>();
            Camera camera = Camera.main;
            activity?.SetAimTurn(1f);
            if (camera != null)
            {
                camera.transform.position += new Vector3(0.012f, 0f, 0.006f);
                camera.transform.rotation =
                    Quaternion.AngleAxis(0.7f, Vector3.up) *
                    camera.transform.rotation;
            }
        }

        private static void ReleaseDeadlineReplayProbe(bool longProbe)
        {
            DeadlineController deadline =
                UnityEngine.Object.FindObjectOfType<DeadlineController>();
            WorldTimeController worldTime =
                UnityEngine.Object.FindObjectOfType<WorldTimeController>();
            WorldTimeActivity activity =
                UnityEngine.Object.FindObjectOfType<WorldTimeActivity>();
            PlayerAim aim = UnityEngine.Object.FindObjectOfType<PlayerAim>();
            System.Reflection.MethodInfo releaseMethod =
                typeof(DeadlineController).GetMethod(
                    "ReleaseDeadline",
                    System.Reflection.BindingFlags.Instance |
                    System.Reflection.BindingFlags.NonPublic);

            Require(
                deadline != null &&
                worldTime != null &&
                activity != null &&
                aim != null &&
                releaseMethod != null,
                "Deadline replay probe release dependencies are missing.");
            if (deadline == null ||
                worldTime == null ||
                activity == null ||
                aim == null ||
                releaseMethod == null)
            {
                return;
            }

            try
            {
                releaseMethod.Invoke(deadline, null);
            }
            catch (Exception exception)
            {
                RecordFailure(
                    $"Deadline replay probe release threw: {exception}");
                return;
            }

            aim.enabled = true;
            activity.SetAimTurn(0f);
            activity.Pulse(1f, 1.2f);
            Require(
                !deadline.IsActive,
                "Deadline replay probe did not release Deadline.");

            if (longProbe)
            {
                float deadlineWorldDuration =
                    worldTime.WorldElapsedTime - deadlineLongWorldStart;
                Require(
                    deadlineWorldDuration >= 0.01f &&
                    deadlineWorldDuration <= 0.06f,
                    $"One-second Deadline probe advanced world time by {deadlineWorldDuration:0.000}s instead of about 0.02s.");
                deadlineLongProbeReleased = true;
            }
            else
            {
                deadlineShortProbeReleased = true;
            }
        }

        private static void ValidateReplayPresentationOrder()
        {
            StageReplayController replay =
                UnityEngine.Object.FindObjectOfType<StageReplayController>();
            if (replay == null || !replay.IsReplaying)
            {
                return;
            }

            replayAnimatedProxyObserved |=
                replay.ActiveAnimatedReplayVisualCount > 0;
            Require(
                replay.ActiveReplayLightCount == 2 &&
                replay.IsReplayVisionConeVisible,
                $"Replay dark vision was interrupted during " +
                $"{replay.CurrentPlaybackPhase}: " +
                $"lights={replay.ActiveReplayLightCount}, " +
                $"cone={replay.IsReplayVisionConeVisible}.");
            replayVisionObservedDuringDeadline |=
                replay.CurrentPlaybackPhase !=
                StageReplayController.ReplayPlaybackPhase.Normal;

            float elapsed = replay.PlaybackElapsed;
            float sourceTimestamp = replay.CurrentSourceTimestamp;
            bool loopRestarted =
                previousReplayElapsed >= 0f &&
                elapsed + 0.001f < previousReplayElapsed;
            if (loopRestarted)
            {
                previousReplaySourceTimestamp = -1f;
                replayVisionObservedAfterLoop |=
                    replay.ActiveReplayLightCount == 2 &&
                    replay.IsReplayVisionConeVisible;
            }

            if (previousReplaySourceTimestamp >= 0f)
            {
                Require(
                    sourceTimestamp + 0.001f >=
                    previousReplaySourceTimestamp,
                    "Replay source event order moved backwards inside a loop.");
                replayTimelineOrderValidated |=
                    sourceTimestamp >
                    previousReplaySourceTimestamp + 0.001f;
            }

            previousReplayElapsed = elapsed;
            previousReplaySourceTimestamp = sourceTimestamp;
        }

        private static void BeginMovementValidation()
        {
            WorldTimeActivity activity =
                UnityEngine.Object.FindObjectOfType<WorldTimeActivity>();
            EnemyMotor[] motors =
                UnityEngine.Object.FindObjectsOfType<EnemyMotor>();

            Require(
                activity != null,
                "WorldTimeActivity is missing before movement validation.");
            Require(
                motors.Length == 3,
                $"Movement validation expected 3 motors, found {motors.Length}.");

            EnemyMovementStarts.Clear();
            EnemyMovementDistances.Clear();
            for (int i = 0; i < motors.Length; i++)
            {
                EnemyMovementStarts[motors[i].GetInstanceID()] =
                    motors[i].transform.position;
                EnemyMovementDistances[motors[i].GetInstanceID()] =
                    motors[i].TotalDistanceMoved;
            }

            activity?.Pulse(1f, 0.45f);
        }

        private static void SustainMovementProbe()
        {
            WorldTimeActivity activity =
                UnityEngine.Object.FindObjectOfType<WorldTimeActivity>();
            activity?.Pulse(1f, 0.2f);
        }

        private static void ValidateEnemyMovement()
        {
            EnemyMotor[] motors =
                UnityEngine.Object.FindObjectsOfType<EnemyMotor>();
            EnemyChaser chaser =
                UnityEngine.Object.FindObjectOfType<EnemyChaser>();
            int movedEnemyCount = 0;
            StringBuilder movementDetails = new StringBuilder();

            for (int i = 0; i < motors.Length; i++)
            {
                EnemyMotor motor = motors[i];
                if (!EnemyMovementStarts.TryGetValue(
                        motor.GetInstanceID(),
                        out Vector3 start))
                {
                    continue;
                }

                Vector3 displacement =
                    motor.transform.position - start;
                displacement.y = 0f;
                EnemyMovementDistances.TryGetValue(
                    motor.GetInstanceID(),
                    out float startDistance);
                float traveledDistance =
                    motor.TotalDistanceMoved - startDistance;
                if (movementDetails.Length > 0)
                {
                    movementDetails.Append("; ");
                }

                EnemyShooter shooter =
                    motor.GetComponent<EnemyShooter>();
                EnemyChaser motorChaser =
                    motor.GetComponent<EnemyChaser>();
                movementDetails.Append(
                    $"{motor.name}: displacement={displacement.magnitude:0.000}, " +
                    $"traveled={traveledDistance:0.000}, " +
                    $"moving={motor.IsMoving}, path={motor.HasNavigationPath}, " +
                    $"state={(shooter != null ? shooter.CurrentState.ToString() : motorChaser?.CurrentState.ToString())}");
                if (traveledDistance > 0.05f &&
                    displacement.magnitude > 0.01f)
                {
                    movedEnemyCount++;
                }
            }

            Require(
                movedEnemyCount >= 2,
                $"Only {movedEnemyCount} enemies moved during the movement probe. " +
                movementDetails);
            Require(
                chaser != null &&
                chaser.CurrentState !=
                EnemyCombatant.CombatState.Detecting,
                "The melee chaser did not begin following the player.");
            Require(
                motors.Length == 3 &&
                System.Array.Exists(
                    motors,
                    motor => motor.HasNavigationPath),
                "No enemy acquired a NavMesh path while moving.");
        }

        private static void ValidateWarningLineTracking()
        {
            EnemyShooter[] shooters =
                UnityEngine.Object.FindObjectsOfType<EnemyShooter>();
            EnemyShooter shooter = System.Array.Find(
                shooters,
                candidate =>
                {
                    WeaponController weapon = candidate == null
                        ? null
                        : candidate.GetComponent<WeaponController>();
                    return weapon != null &&
                           weapon.Muzzle != null &&
                           candidate.GetComponent<LineRenderer>() != null &&
                           candidate.GetComponent<EnemyPerception>() != null;
                });
            Require(
                shooter != null,
                "Warning-line validation could not find a configured ranged enemy.");
            if (shooter == null)
            {
                return;
            }

            WeaponController weapon = shooter.GetComponent<WeaponController>();
            EnemyPerception perception =
                shooter.GetComponent<EnemyPerception>();
            LineRenderer warningLine = shooter.GetComponent<LineRenderer>();
            System.Reflection.MethodInfo refreshMethod =
                typeof(EnemyCombatant).GetMethod(
                    "RefreshWarningLine",
                    System.Reflection.BindingFlags.Instance |
                    System.Reflection.BindingFlags.NonPublic);
            Require(
                refreshMethod != null &&
                weapon != null &&
                weapon.Muzzle != null &&
                perception != null &&
                perception.Target != null &&
                warningLine != null,
                "Warning-line tracking dependencies are missing.");
            if (refreshMethod == null ||
                weapon == null ||
                weapon.Muzzle == null ||
                perception == null ||
                perception.Target == null ||
                warningLine == null)
            {
                return;
            }

            Vector3 originalPosition = shooter.transform.position;
            bool wasVisible = warningLine.enabled;
            try
            {
                shooter.transform.position = originalPosition +
                                             new Vector3(0.75f, 0f, 0.5f);
                warningLine.enabled = true;
                refreshMethod.Invoke(shooter, null);

                Require(
                    warningLine.positionCount == 2,
                    "Warning line did not retain two endpoints after refresh.");
                Require(
                    (warningLine.GetPosition(0) - weapon.Muzzle.position)
                    .sqrMagnitude <= 0.000001f,
                    "Warning-line origin did not follow the moved enemy muzzle.");
                Require(
                    (warningLine.GetPosition(1) - perception.Target.position)
                    .sqrMagnitude <= 0.000001f,
                    "Warning-line endpoint did not follow the current target position.");
            }
            finally
            {
                shooter.transform.position = originalPosition;
                warningLine.enabled = wasVisible;
                if (wasVisible)
                {
                    refreshMethod.Invoke(shooter, null);
                }
            }
        }

        private static void BeginStunValidation()
        {
            StageController stage =
                UnityEngine.Object.FindObjectOfType<StageController>();
            WorldTimeController worldTime =
                UnityEngine.Object.FindObjectOfType<WorldTimeController>();
            WorldTimeActivity activity =
                UnityEngine.Object.FindObjectOfType<WorldTimeActivity>();
            PlayerCombat playerCombat =
                UnityEngine.Object.FindObjectOfType<PlayerCombat>();
            EnemyHealth[] enemies =
                UnityEngine.Object.FindObjectsOfType<EnemyHealth>();
            int airborneCountBefore =
                UnityEngine.Object.FindObjectsOfType<InterceptableWeapon>().Length;

            Require(stage != null, "StageController is missing before stun validation.");
            Require(worldTime != null, "WorldTimeController is missing before stun validation.");
            Require(activity != null, "WorldTimeActivity is missing before stun validation.");
            Require(playerCombat != null, "PlayerCombat is missing before stun validation.");
            Require(enemies.Length == 3, "Stun validation requires all three enemies.");

            if (stage == null ||
                worldTime == null ||
                activity == null ||
                playerCombat == null ||
                enemies.Length == 0)
            {
                return;
            }

            activity.Pulse(1f, 3f);
            SpawnThrownRangeProbe(worldTime, playerCombat);
            for (int i = 0; i < enemies.Length; i++)
            {
                EnemyHealth enemy = enemies[i];
                enemy.ReceiveStun(new StunHit(
                    2f,
                    enemy.transform.position,
                    Vector3.forward,
                    null));

                EnemyBehavior behavior =
                    enemy.GetComponent<EnemyBehavior>();
                EnemyShooter shooter =
                    enemy.GetComponent<EnemyShooter>();
                EnemyChaser chaser =
                    enemy.GetComponent<EnemyChaser>();
                WeaponController weapon =
                    enemy.GetComponent<WeaponController>();
                Require(enemy.IsAlive, "A stun killed an enemy.");
                Require(enemy.IsStunned, "An enemy did not enter the stunned state.");
                Require(
                    behavior != null && behavior.IsStunned,
                    "Enemy behavior remained active while stunned.");
                if (shooter != null)
                {
                    Require(
                        shooter.CurrentState ==
                        EnemyCombatant.CombatState.Stunned,
                        "Enemy shooting behavior remained active while stunned.");
                }
                else
                {
                    Require(
                        chaser != null &&
                        chaser.CurrentState ==
                        EnemyCombatant.CombatState.Stunned,
                        "Enemy chasing behavior remained active while stunned.");
                }

                Require(
                    weapon != null && !weapon.HasWeapon,
                    "A stunned enemy retained its held weapon.");
            }

            int airborneCountAfter =
                UnityEngine.Object.FindObjectsOfType<InterceptableWeapon>().Length;
            Require(
                airborneCountAfter == airborneCountBefore + enemies.Length,
                "Stunning enemies did not create exactly one weapon drop each.");
            Require(
                stage.CurrentState == StageController.StageState.Active &&
                stage.RemainingEnemyCount == enemies.Length,
                "Stunning enemies changed stage-clear progress.");

            enemies[0].ReceiveStun(new StunHit(
                2f,
                enemies[0].transform.position,
                Vector3.forward,
                null));
            Require(
                UnityEngine.Object.FindObjectsOfType<InterceptableWeapon>().Length ==
                airborneCountAfter,
                "Repeated stun created a duplicate weapon drop.");

            EnemyBehavior firstBehavior =
                enemies[0].GetComponent<EnemyBehavior>();
            Require(
                firstBehavior != null &&
                firstBehavior.StunTimeRemaining > 1.99f,
                "Repeated stun did not refresh the stun duration.");
        }

        private static void SpawnThrownRangeProbe(
            WorldTimeController worldTime,
            PlayerCombat playerCombat)
        {
            GameObject thrownPrefabObject =
                AssetDatabase.LoadAssetAtPath<GameObject>(
                    ThrownWeaponPrefabPath);
            GameObject pickupPrefabObject =
                AssetDatabase.LoadAssetAtPath<GameObject>(
                    WeaponPickupPrefabPath);
            ThrownWeapon thrownPrefab = thrownPrefabObject == null
                ? null
                : thrownPrefabObject.GetComponent<ThrownWeapon>();
            WeaponPickup pickupPrefab = pickupPrefabObject == null
                ? null
                : pickupPrefabObject.GetComponent<WeaponPickup>();
            WeaponDefinition definition =
                playerCombat.Weapon == null
                    ? null
                    : playerCombat.Weapon.Definition;

            Require(
                thrownPrefab != null &&
                pickupPrefab != null &&
                definition != null,
                "Thrown range probe dependencies are missing.");
            if (thrownPrefab == null ||
                pickupPrefab == null ||
                definition == null)
            {
                return;
            }

            ThrownWeapon probe = UnityEngine.Object.Instantiate(
                thrownPrefab,
                RangeProbeStart,
                Quaternion.identity);
            probe.name = "Thrown Range Probe";
            probe.Initialize(
                worldTime,
                pickupPrefab,
                definition,
                0,
                CombatFaction.Player,
                null,
                Vector3.forward);
        }

        private static void ValidateStunRecovery()
        {
            StageController stage =
                UnityEngine.Object.FindObjectOfType<StageController>();
            EnemyHealth[] enemies =
                UnityEngine.Object.FindObjectsOfType<EnemyHealth>();

            Require(stage != null, "StageController is missing after stun validation.");
            Require(enemies.Length == 3, "An enemy disappeared while stunned.");
            for (int i = 0; i < enemies.Length; i++)
            {
                EnemyShooter shooter = enemies[i].GetComponent<EnemyShooter>();
                EnemyChaser chaser = enemies[i].GetComponent<EnemyChaser>();
                EnemyBehavior behavior =
                    enemies[i].GetComponent<EnemyBehavior>();
                Require(enemies[i].IsAlive, "A stunned enemy did not remain alive.");
                Require(!enemies[i].IsStunned, "An enemy did not recover from stun.");
                Require(
                    behavior != null && !behavior.IsDead,
                    "A recovered enemy did not return to combat decisions.");
                if (shooter != null)
                {
                    Require(
                        shooter.CurrentState ==
                        EnemyCombatant.CombatState.Detecting ||
                        shooter.CurrentState ==
                        EnemyCombatant.CombatState.Pursuing ||
                        shooter.CurrentState ==
                        EnemyCombatant.CombatState.SeekingWeapon ||
                        shooter.CurrentState ==
                        EnemyCombatant.CombatState.AttackWindup,
                        "A recovered ranged enemy did not resume decisions.");
                }
                else
                {
                    Require(
                        chaser != null &&
                        chaser.CurrentState !=
                        EnemyCombatant.CombatState.Stunned &&
                        chaser.CurrentState !=
                        EnemyCombatant.CombatState.Dead,
                        "A recovered chasing enemy did not resume decisions.");
                }
            }

            ThrownWeapon[] thrownWeapons =
                UnityEngine.Object.FindObjectsOfType<ThrownWeapon>();
            for (int i = 0; i < thrownWeapons.Length; i++)
            {
                Require(
                    thrownWeapons[i].name != "Thrown Range Probe",
                    "Thrown weapon did not settle at its maximum range.");
            }

            WeaponPickup[] pickups =
                UnityEngine.Object.FindObjectsOfType<WeaponPickup>();
            float nearestRangeProbeDistance = float.PositiveInfinity;
            for (int i = 0; i < pickups.Length; i++)
            {
                nearestRangeProbeDistance = Mathf.Min(
                    nearestRangeProbeDistance,
                    Vector3.Distance(
                        pickups[i].transform.position,
                        RangeProbeLanding));
            }

            Require(
                nearestRangeProbeDistance <= 0.01f,
                "Thrown weapon did not settle exactly six units from its start.");

            if (stage != null)
            {
                Require(
                    stage.CurrentState == StageController.StageState.Active &&
                    stage.RemainingEnemyCount == enemies.Length,
                    "Stun recovery changed stage-clear progress.");
            }
        }

        private static void ClearStage()
        {
            PlayerHealth player =
                UnityEngine.Object.FindObjectOfType<PlayerHealth>();
            if (player != null)
            {
                player.transform.position += new Vector3(1.25f, 0f, 0.75f);
                player.transform.rotation = Quaternion.Euler(0f, 35f, 0f);
            }

            EnemyHealth[] enemies =
                UnityEngine.Object.FindObjectsOfType<EnemyHealth>();
            for (int i = 0; i < enemies.Length; i++)
            {
                EnemyHealth enemy = enemies[i];
                enemy.ReceiveHit(new DamageHit(
                    1,
                    enemy.transform.position,
                    Vector3.forward,
                    null));
            }
        }

        private static void ValidateReplayState()
        {
            StageController stage =
                UnityEngine.Object.FindObjectOfType<StageController>();
            StageReplayController replay =
                UnityEngine.Object.FindObjectOfType<StageReplayController>();
            TopDownCameraController cameraRig =
                UnityEngine.Object.FindObjectOfType<TopDownCameraController>();

            Require(
                stage != null &&
                stage.CurrentState == StageController.StageState.Replaying,
                "Clearing all enemies did not put the stage into replay state.");
            Require(
                replay != null && replay.IsReplaying,
                "Clearing all enemies did not start replay playback.");
            Require(
                replay != null && replay.RecordedDuration > 0f,
                "The replay did not retain a playable recording.");
            Require(
                replay != null &&
                replay.SourceRecordedDuration - replay.RecordedDuration >=
                0.8f,
                "Strong world-time slow motion was not removed from the replay duration.");
            Require(
                replay != null && replay.DeadlineCinematicSegmentCount == 2,
                "Replay did not retain both Deadline event windows.");
            Require(
                replay != null &&
                replay.ShortestDeadlineCinematicDuration >= 0f &&
                replay.ShortestDeadlineCinematicDuration <= 0.03f,
                $"Short Deadline normalized duration was {replay?.ShortestDeadlineCinematicDuration:0.000}s instead of at most 0.03s.");
            Require(
                replay != null &&
                replay.LongestDeadlineCinematicDuration >= 0.01f &&
                replay.LongestDeadlineCinematicDuration <= 0.08f,
                $"Long Deadline normalized duration was {replay?.LongestDeadlineCinematicDuration:0.000}s instead of about 0.02s.");
            Require(
                replay != null &&
                replay.LongestDeadlineAftermathDuration >= 0.70f &&
                replay.LongestDeadlineAftermathDuration <= 0.76f,
                $"Deadline aftermath duration was {replay?.LongestDeadlineAftermathDuration:0.000}s instead of 0.75 normal-speed seconds.");
            Require(
                replay != null &&
                replay.TrackedAnimatedVisualCount >= 1 &&
                replay.RecordedAnimatedPoseCount >
                replay.TrackedAnimatedVisualCount &&
                replay.HasRecordedAnimatedMotion,
                $"Replay did not record changing character bone poses: " +
                $"tracked={replay?.TrackedAnimatedVisualCount}, " +
                $"poses={replay?.RecordedAnimatedPoseCount}, " +
                $"motion={replay?.HasRecordedAnimatedMotion}.");
            Require(
                cameraRig != null && !cameraRig.enabled,
                "Live camera simulation remained enabled during replay.");
            Require(
                replay != null && replay.AreTrackedSourceLightsDisabled,
                "Original dark-vision lights remained enabled during replay.");
            Require(
                replay != null &&
                replay.ActiveReplayLightCount == 2 &&
                replay.IsReplayVisionConeVisible &&
                replay.ActiveAnimatedReplayVisualCount >= 1,
                $"Replay did not start with persistent dark vision: " +
                $"lights={replay?.ActiveReplayLightCount}, " +
                $"cone={replay?.IsReplayVisionConeVisible}, " +
                $"animated={replay?.ActiveAnimatedReplayVisualCount}.");
            Require(
                IsSceneLightEnabled("Directional Key Light") &&
                IsSceneLightEnabled("Blue Bay Light") &&
                IsSceneLightEnabled("Red Alert Light"),
                "Replay disabled a static scene light.");
            Require(
                Mathf.Approximately(UnityEngine.Time.timeScale, 1f),
                "Replay changed global Time.timeScale.");
        }

        private static void ValidateRuntimeState()
        {
            StageController stage = UnityEngine.Object.FindObjectOfType<StageController>();
            WorldTimeController worldTime =
                UnityEngine.Object.FindObjectOfType<WorldTimeController>();
            PlayerHealth player = UnityEngine.Object.FindObjectOfType<PlayerHealth>();
            WeaponController weapon =
                player == null
                    ? null
                    : player.GetComponent<WeaponController>();
            GameHud hud = UnityEngine.Object.FindObjectOfType<GameHud>();
            StageReplayController replay =
                UnityEngine.Object.FindObjectOfType<StageReplayController>();
            EnemyShooter[] shooters =
                UnityEngine.Object.FindObjectsOfType<EnemyShooter>();
            EnemyChaser[] chasers =
                UnityEngine.Object.FindObjectsOfType<EnemyChaser>();
            EnemyMotor[] motors =
                UnityEngine.Object.FindObjectsOfType<EnemyMotor>();
            EnemyPerception[] perceptions =
                UnityEngine.Object.FindObjectsOfType<EnemyPerception>();
            NavMeshSurface navigationSurface =
                UnityEngine.Object.FindObjectOfType<NavMeshSurface>();
            TopDownCameraController cameraRig =
                UnityEngine.Object.FindObjectOfType<TopDownCameraController>();
            Camera gameplayCamera = Camera.main;
            Rigidbody2D[] legacyBodies =
                UnityEngine.Object.FindObjectsOfType<Rigidbody2D>();

            Require(stage != null, "StageController is missing at runtime.");
            Require(worldTime != null, "WorldTimeController is missing at runtime.");
            Require(player != null && player.IsAlive, "The player did not initialize alive.");
            Require(weapon != null && weapon.HasWeapon, "The player did not initialize with a weapon.");
            Require(hud != null && hud.enabled, "GameHud did not initialize.");
            Require(replay != null && replay.enabled, "Stage replay did not initialize.");
            Require(
                replay != null && Mathf.Approximately(replay.CaptureRate, 20f),
                "Stage replay capture rate is not configured to 20 Hz.");
            Require(
                replay != null && replay.TrackedLightCount == 2,
                "Stage replay did not register both dark-vision lights.");
            Require(
                replay != null && replay.TrackedReplayVisionConeCount == 1,
                "Stage replay did not register the replay vision-cone track.");
            Require(
                shooters.Length == 2,
                $"Expected 2 ranged enemies, found {shooters.Length}.");
            Require(
                chasers.Length == 1,
                $"Expected 1 chasing enemy, found {chasers.Length}.");
            Require(
                motors.Length == 3 && perceptions.Length == 3,
                "Enemy movement or perception components are missing.");
            Require(
                navigationSurface != null &&
                navigationSurface.navMeshData != null,
                "The stage has no baked NavMesh data.");
            Require(
                gameplayCamera != null && !gameplayCamera.orthographic,
                "The gameplay camera is not a perspective camera.");
            Require(cameraRig != null && cameraRig.enabled, "The 3D camera rig did not initialize.");
            Require(legacyBodies.Length == 0, "Legacy 2D rigidbodies remain in the 3D scene.");
            ValidatePlayerWallCollision();

            if (stage != null)
            {
                Require(
                    stage.RemainingEnemyCount == 3,
                    $"Stage registered {stage.RemainingEnemyCount} enemies instead of 3.");
            }

            if (worldTime != null)
            {
                Require(
                    worldTime.CurrentTimeScale >= 0.019f &&
                    worldTime.CurrentTimeScale < 0.2f,
                    $"Idle world scale was {worldTime.CurrentTimeScale:0.000}.");
            }

            Require(
                Mathf.Approximately(UnityEngine.Time.timeScale, 1f),
                "Global Time.timeScale was modified.");

            GameObject thrownWeaponPrefab =
                AssetDatabase.LoadAssetAtPath<GameObject>(
                    ThrownWeaponPrefabPath);
            ThrownWeapon thrownWeapon = thrownWeaponPrefab == null
                ? null
                : thrownWeaponPrefab.GetComponent<ThrownWeapon>();
            Require(
                thrownWeapon != null &&
                Mathf.Approximately(thrownWeapon.Speed, 7f) &&
                Mathf.Approximately(thrownWeapon.MaximumTravelDistance, 6f) &&
                Mathf.Approximately(thrownWeapon.StunDuration, 2f),
                "Thrown weapon speed, range, or stun duration is misconfigured.");

            if (replay != null)
            {
                Require(
                    replay.CapturedFrameCount > 0,
                    "Stage replay did not capture any frames.");
            }
        }

        private static void ValidatePlayerWallCollision()
        {
            PlayerDash dash =
                UnityEngine.Object.FindObjectOfType<PlayerDash>();
            GameObject northWall = GameObject.Find("North Wall");
            Rigidbody body = dash == null
                ? null
                : dash.GetComponent<Rigidbody>();
            CapsuleCollider capsule = dash == null
                ? null
                : dash.GetComponent<CapsuleCollider>();
            Collider wallCollider = northWall == null
                ? null
                : northWall.GetComponent<Collider>();
            System.Reflection.FieldInfo directionField =
                typeof(PlayerDash).GetField(
                    "dashDirection",
                    System.Reflection.BindingFlags.Instance |
                    System.Reflection.BindingFlags.NonPublic);
            System.Reflection.MethodInfo safeDistanceMethod =
                typeof(PlayerDash).GetMethod(
                    "GetSafeDashDistance",
                    System.Reflection.BindingFlags.Instance |
                    System.Reflection.BindingFlags.NonPublic);

            Require(
                dash != null &&
                body != null &&
                capsule != null &&
                wallCollider != null &&
                directionField != null &&
                safeDistanceMethod != null,
                "Player wall-collision regression dependencies are missing.");
            if (dash == null ||
                body == null ||
                capsule == null ||
                wallCollider == null ||
                directionField == null ||
                safeDistanceMethod == null)
            {
                return;
            }

            Vector3 originalPosition = body.position;
            Quaternion originalRotation = body.rotation;
            Vector3 originalLinearVelocity = body.linearVelocity;
            Vector3 originalAngularVelocity = body.angularVelocity;
            object originalDirection = directionField.GetValue(dash);

            try
            {
                directionField.SetValue(dash, Vector3.forward);
                Physics.SyncTransforms();

                float openDistance = (float)safeDistanceMethod.Invoke(
                    dash,
                    new object[] { 0.5f });
                Require(
                    openDistance >= 0.499f,
                    $"Open dash path was shortened to {openDistance:0.000} units.");

                Vector3 scale = capsule.transform.lossyScale;
                float horizontalRadius =
                    capsule.radius *
                    Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.z));
                float wallInnerEdge = wallCollider.bounds.min.z;

                body.position = new Vector3(
                    0f,
                    originalPosition.y,
                    wallInnerEdge - horizontalRadius + 0.01f);
                body.rotation = Quaternion.identity;
                body.linearVelocity = Vector3.zero;
                body.angularVelocity = Vector3.zero;
                Physics.SyncTransforms();

                float safeDistance = (float)safeDistanceMethod.Invoke(
                    dash,
                    new object[] { 0.5f });
                Require(
                    safeDistance <= 0.001f,
                    $"Dash wall contact allowed {safeDistance:0.000} units of travel.");
            }
            catch (Exception exception)
            {
                RecordFailure(
                    $"Player wall-collision regression threw: {exception}");
            }
            finally
            {
                directionField.SetValue(dash, originalDirection);
                body.position = originalPosition;
                body.rotation = originalRotation;
                body.linearVelocity = originalLinearVelocity;
                body.angularVelocity = originalAngularVelocity;
                Physics.SyncTransforms();
            }
        }

        private static bool IsSceneLightEnabled(string objectName)
        {
            GameObject lightObject = GameObject.Find(objectName);
            if (lightObject == null)
            {
                return false;
            }

            Light light = lightObject.GetComponent<Light>();
            return light != null && light.enabled;
        }

        private static void Require(bool condition, string message)
        {
            if (!condition)
            {
                RecordFailure(message);
            }
        }

        private static void HandleLog(string condition, string stackTrace, LogType type)
        {
            if (type == LogType.Error ||
                type == LogType.Exception ||
                type == LogType.Assert)
            {
                RecordFailure($"{type}: {condition}\n{stackTrace}");
            }
        }

        private static void RecordFailure(string message)
        {
            SessionState.SetBool(FailedKey, true);
            string existing = SessionState.GetString(FailureTextKey, string.Empty);
            StringBuilder builder = new StringBuilder(existing);
            if (builder.Length > 0)
            {
                builder.AppendLine();
            }

            builder.Append(message);
            SessionState.SetString(FailureTextKey, builder.ToString());
        }

        private static void Finish()
        {
            bool failed = SessionState.GetBool(FailedKey, false);
            string failureText = SessionState.GetString(FailureTextKey, string.Empty);

            SessionState.EraseBool(RunningKey);
            SessionState.EraseBool(FailedKey);
            SessionState.EraseString(FailureTextKey);
            SessionState.EraseString(PhaseKey);
            DetachCallbacks();

            if (failed)
            {
                Debug.LogError($"Prototype play-mode smoke test failed:\n{failureText}");
                EditorApplication.Exit(1);
            }
            else
            {
                Debug.Log("Prototype play-mode smoke test passed.");
                EditorApplication.Exit(0);
            }
        }
    }
}
