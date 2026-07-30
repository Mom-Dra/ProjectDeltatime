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
        private static bool stunChecksRan;
        private static bool replayChecksRan;
        private static bool callbacksAttached;

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

            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            AttachCallbacks();
            EditorApplication.isPlaying = true;
        }

        private static void AttachCallbacks()
        {
            if (callbacksAttached)
            {
                return;
            }

            callbacksAttached = true;
            EditorApplication.update += Tick;
            EditorApplication.playModeStateChanged += HandlePlayModeStateChanged;
            Application.logMessageReceived += HandleLog;
        }

        private static void DetachCallbacks()
        {
            if (!callbacksAttached)
            {
                return;
            }

            callbacksAttached = false;
            EditorApplication.update -= Tick;
            EditorApplication.playModeStateChanged -= HandlePlayModeStateChanged;
            Application.logMessageReceived -= HandleLog;
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
                stunChecksRan = false;
                replayChecksRan = false;
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
                    stunChecksRan = false;
                    replayChecksRan = false;
                    SessionState.SetString(PhaseKey, "playing");
                }

                double elapsed = EditorApplication.timeSinceStartup - playStartedAt;
                if (!checksRan && elapsed >= 0.5d)
                {
                    checksRan = true;
                    ValidateRuntimeState();
                    BeginStunValidation();
                }

                if (!stunChecksRan && elapsed >= 3d)
                {
                    stunChecksRan = true;
                    ValidateStunRecovery();
                    ClearStage();
                }

                if (!replayChecksRan && elapsed >= 3.35d)
                {
                    replayChecksRan = true;
                    ValidateReplayState();
                }

                if (elapsed >= 3.9d)
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

                EnemyShooter shooter = enemy.GetComponent<EnemyShooter>();
                WeaponController weapon = enemy.GetComponent<WeaponController>();
                Require(enemy.IsAlive, "A stun killed an enemy.");
                Require(enemy.IsStunned, "An enemy did not enter the stunned state.");
                Require(
                    shooter != null &&
                    shooter.CurrentState == EnemyShooter.ShooterState.Stunned,
                    "Enemy shooting behavior remained active while stunned.");
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

            EnemyShooter firstShooter =
                enemies[0].GetComponent<EnemyShooter>();
            Require(
                firstShooter != null &&
                firstShooter.StunTimeRemaining > 1.99f,
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
                Require(enemies[i].IsAlive, "A stunned enemy did not remain alive.");
                Require(!enemies[i].IsStunned, "An enemy did not recover from stun.");
                Require(
                    shooter != null &&
                    shooter.CurrentState == EnemyShooter.ShooterState.Disarmed &&
                    shooter.IsDisarmed,
                    "A recovered enemy did not remain disarmed.");
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
                cameraRig != null && !cameraRig.enabled,
                "Live camera simulation remained enabled during replay.");
            Require(
                replay != null && replay.AreTrackedSourceLightsDisabled,
                "Original dark-vision lights remained enabled during replay.");
            Require(
                replay != null && replay.ActiveReplayLightCount == 2,
                "Replay did not activate both dark-vision proxy lights.");
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
                UnityEngine.Object.FindObjectOfType<WeaponController>();
            GameHud hud = UnityEngine.Object.FindObjectOfType<GameHud>();
            StageReplayController replay =
                UnityEngine.Object.FindObjectOfType<StageReplayController>();
            EnemyShooter[] enemies =
                UnityEngine.Object.FindObjectsOfType<EnemyShooter>();
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
            Require(enemies.Length == 3, $"Expected 3 enemies, found {enemies.Length}.");
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
