using System;
using Deltatime.Combat;
using Deltatime.Core;
using Deltatime.Enemies;
using Deltatime.Level;
using Deltatime.Player;
using Deltatime.Replay;
using Deltatime.TimeSystem;
using Deltatime.Visuals;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

namespace Deltatime.EditorTools
{
    [InitializeOnLoad]
    public static class StageBattingCagePlayModeSmokeTest
    {
        private const string RunningKey =
            "Deltatime.StageBattingCageSmoke.Running";
        private const string FailedKey =
            "Deltatime.StageBattingCageSmoke.Failed";
        private const string FailureKey =
            "Deltatime.StageBattingCageSmoke.Failure";
        private const string PhaseKey =
            "Deltatime.StageBattingCageSmoke.Phase";
        private const string MeleePath =
            "Assets/_Project/MeleeWeapon.asset";
        private const string MeleePickupPath =
            "Assets/_Project/Prefabs/MeleeWeaponPickup.prefab";

        private static readonly CommandLineSmokeRunner Runner =
            new CommandLineSmokeRunner(
                RunningKey,
                Tick,
                HandlePlayModeStateChanged,
                HandleLog);
        private static double playStartedAt;

        static StageBattingCagePlayModeSmokeTest()
        {
            if (SessionState.GetBool(RunningKey, false))
            {
                Runner.Attach();
            }
        }

        public static void RunFromCommandLine()
        {
            SessionState.SetBool(RunningKey, true);
            SessionState.SetBool(FailedKey, false);
            SessionState.SetString(FailureKey, string.Empty);
            SessionState.SetString(PhaseKey, "entering");
            Runner.OpenSceneAndEnterPlayMode(
                StageBattingCageSceneBuilder.ScenePath);
        }

        private static void HandlePlayModeStateChanged(
            PlayModeStateChange state)
        {
            if (!SessionState.GetBool(RunningKey, false))
            {
                return;
            }

            if (state == PlayModeStateChange.EnteredPlayMode)
            {
                playStartedAt = EditorApplication.timeSinceStartup;
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
                Runner.Detach();
                return;
            }

            if (!EditorApplication.isPlaying)
            {
                return;
            }

            double elapsed = EditorApplication.timeSinceStartup -
                             playStartedAt;
            string phase = SessionState.GetString(
                PhaseKey,
                string.Empty);
            if (phase == "playing" && elapsed >= 0.8d)
            {
                try
                {
                    ValidateAndExerciseEncounter();
                    SessionState.SetString(PhaseKey, "waiting-replay");
                }
                catch (Exception exception)
                {
                    RecordFailure(exception.ToString());
                    EditorApplication.isPlaying = false;
                }
            }
            else if (phase == "waiting-replay" && elapsed >= 1.6d)
            {
                try
                {
                    ValidateReplayAndFlow();
                }
                catch (Exception exception)
                {
                    RecordFailure(exception.ToString());
                }

                EditorApplication.isPlaying = false;
            }
            else if (elapsed >= 15d)
            {
                RecordFailure(
                    "StageBattingCage play-mode smoke test timed out.");
                EditorApplication.isPlaying = false;
            }
        }

        private static void ValidateAndExerciseEncounter()
        {
            Scene scene = SceneManager.GetActiveScene();
            PlayerHealth player =
                UnityEngine.Object.FindFirstObjectByType<PlayerHealth>();
            DeadlineController deadline =
                UnityEngine.Object.FindFirstObjectByType<DeadlineController>();
            WorldTimeController worldTime =
                UnityEngine.Object.FindFirstObjectByType<WorldTimeController>();
            StageController stage =
                UnityEngine.Object.FindFirstObjectByType<StageController>();
            StageReplayController replay =
                UnityEngine.Object.FindFirstObjectByType<StageReplayController>();
            NavMeshSurface surface =
                UnityEngine.Object.FindFirstObjectByType<NavMeshSurface>();
            EnemyHealth[] enemies =
                UnityEngine.Object.FindObjectsByType<EnemyHealth>(
                    FindObjectsSortMode.None);
            EnemyChaser[] chasers =
                UnityEngine.Object.FindObjectsByType<EnemyChaser>(
                    FindObjectsSortMode.None);
            EnemyMotor[] motors =
                UnityEngine.Object.FindObjectsByType<EnemyMotor>(
                    FindObjectsSortMode.None);
            CharacterVisualController[] visuals =
                UnityEngine.Object.FindObjectsByType<CharacterVisualController>(
                    FindObjectsSortMode.None);
            WeaponPickup[] initialPickups =
                UnityEngine.Object.FindObjectsByType<WeaponPickup>(
                    FindObjectsSortMode.None);
            WeaponDefinition melee =
                AssetDatabase.LoadAssetAtPath<WeaponDefinition>(MeleePath);

            Require(
                scene.path == StageBattingCageSceneBuilder.ScenePath,
                "Unexpected scene: " + scene.path);
            Require(
                player != null && player.IsAlive &&
                player.MaximumHealth == 3 && player.CurrentHealth == 3,
                "StageBattingCage player did not initialize at 3 health.");
            Require(
                deadline != null && deadline.ChargesRemaining == 2,
                "StageBattingCage did not retain two DEADLINE charges.");
            Require(
                worldTime != null && worldTime.enabled,
                "StageBattingCage world time did not initialize.");
            Require(
                stage != null &&
                stage.CurrentState == StageController.StageState.Active &&
                stage.RemainingEnemyCount == 6,
                "StageBattingCage did not register six active enemies.");
            Require(
                replay != null && replay.IsRecording,
                "StageBattingCage replay did not start recording.");
            Require(
                surface != null && surface.navMeshData != null &&
                AssetDatabase.GetAssetPath(surface.navMeshData) ==
                StageBattingCageSceneBuilder.NavigationPath,
                "StageBattingCage runtime NavMesh is missing or incorrect.");
            Require(
                enemies.Length == 6 && chasers.Length == 6 &&
                motors.Length == 6 && visuals.Length == 7,
                "StageBattingCage runtime actor components are incomplete.");
            Require(
                initialPickups.Length == 0,
                "StageBattingCage spawned an initial ground weapon.");
            Require(
                melee != null && melee.Damage == 3,
                "StageBattingCage melee definition is missing or changed.");

            WeaponController playerWeapon =
                player.GetComponent<WeaponController>();
            Require(
                playerWeapon != null && playerWeapon.Definition == melee,
                "Player did not equip the starting bat.");
            for (int i = 0; i < enemies.Length; i++)
            {
                WeaponController weapon =
                    enemies[i].GetComponent<WeaponController>();
                Require(
                    weapon != null && weapon.Definition == melee,
                    enemies[i].name + " did not equip the starting bat.");
                RequireOnNavMesh(enemies[i].transform.position, enemies[i].name);
            }
            RequireOnNavMesh(player.transform.position, "player");

            ValidateInitialEngagementSplit();
            ValidateEnemyOneHit(player, chasers[0], melee);
            ValidateThrowAndReacquire(playerWeapon, worldTime, melee);
            ValidateEnemyRearm(chasers[1], melee);
            KillEveryEnemyWithPlayerBat(player, enemies, melee);

            InterceptableWeapon[] drops =
                UnityEngine.Object.FindObjectsByType<InterceptableWeapon>(
                    FindObjectsSortMode.None);
            Require(
                drops.Length >= 6,
                $"Enemy deaths produced {drops.Length} airborne bat drops; " +
                "expected at least 6.");
            Require(
                stage.RemainingEnemyCount == 0 &&
                stage.CurrentState == StageController.StageState.Cleared,
                "StageBattingCage did not clear after six bat kills.");
        }

        private static void ValidateInitialEngagementSplit()
        {
            int visible = 0;
            int blocked = 0;
            for (int i = 0; i < 6; i++)
            {
                GameObject enemy = GameObject.Find(
                    StageBattingCageEnemyName(i));
                EnemyPerception perception = enemy == null
                    ? null
                    : enemy.GetComponent<EnemyPerception>();
                Require(
                    perception != null,
                    "Batting-cage enemy perception is missing.");
                if (perception.CanSeeTarget())
                {
                    visible++;
                }
                else
                {
                    blocked++;
                }
            }

            Require(
                visible == 3 && blocked == 3,
                $"Initial engagement split is {visible} visible/{blocked} " +
                "blocked instead of 3/3.");
        }

        private static string StageBattingCageEnemyName(int index)
        {
            string[] names =
            {
                "Enemy Bat East",
                "Enemy Bat North East",
                "Enemy Bat North West",
                "Enemy Bat West",
                "Enemy Bat South West",
                "Enemy Bat South East"
            };
            return names[index];
        }

        private static void ValidateEnemyOneHit(
            PlayerHealth player,
            EnemyChaser attacker,
            WeaponDefinition melee)
        {
            GameObject dummy = new GameObject("Bat Damage Dummy Player");
            dummy.transform.position =
                attacker.transform.position + attacker.transform.forward;
            dummy.AddComponent<CapsuleCollider>();
            PlayerHealth dummyHealth = dummy.AddComponent<PlayerHealth>();
            Physics.SyncTransforms();

            bool hit = MeleeAttackResolver.TryHitNearest(
                attacker.gameObject,
                CombatFaction.Enemy,
                attacker.transform.forward,
                melee.MeleeRange,
                melee.MeleeHalfAngle,
                melee.Damage,
                MeleeImpactKind.Bat);
            Require(hit, "Enemy bat did not resolve against the damage dummy.");
            Require(
                !dummyHealth.IsAlive && dummyHealth.CurrentHealth == 0,
                "Enemy bat did not kill a 3-health player in one hit.");
            Require(
                player.IsAlive && player.CurrentHealth == 3,
                "Enemy one-hit probe damaged the real player.");
            UnityEngine.Object.Destroy(dummy);
        }

        private static void ValidateThrowAndReacquire(
            WeaponController playerWeapon,
            WorldTimeController worldTime,
            WeaponDefinition melee)
        {
            bool threw = playerWeapon.Throw(
                CombatFaction.Player,
                Vector3.forward,
                worldTime);
            Require(threw && playerWeapon.Definition == null,
                "Player could not throw the starting bat.");
            Require(
                UnityEngine.Object.FindFirstObjectByType<ThrownWeapon>() != null,
                "Bat throw did not create a thrown weapon.");

            WeaponPickup pickup = CreateMeleePickup(melee);
            Require(
                pickup.TryTake(playerWeapon),
                "Player could not reacquire a ground bat.");
            Require(
                playerWeapon.Definition == melee,
                "Player did not re-equip the reacquired bat.");
        }

        private static void ValidateEnemyRearm(
            EnemyChaser enemy,
            WeaponDefinition melee)
        {
            WeaponController weapon = enemy.GetComponent<WeaponController>();
            weapon.Clear();
            WeaponPickup pickup = CreateMeleePickup(melee);
            Require(
                pickup.TryTake(weapon, enemy),
                "Disarmed enemy could not take an available bat.");
            Require(
                weapon.Definition == melee,
                "Enemy rearm did not restore the bat.");
        }

        private static WeaponPickup CreateMeleePickup(
            WeaponDefinition melee)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                MeleePickupPath);
            Require(prefab != null, "Melee pickup prefab is missing.");
            GameObject instance = UnityEngine.Object.Instantiate(
                prefab,
                new Vector3(0f, 0.18f, -1f),
                Quaternion.identity);
            WeaponPickup pickup = instance.GetComponent<WeaponPickup>();
            Require(pickup != null, "Melee pickup prefab is invalid.");
            pickup.Initialize(melee, 0);
            return pickup;
        }

        private static void KillEveryEnemyWithPlayerBat(
            PlayerHealth player,
            EnemyHealth[] enemies,
            WeaponDefinition melee)
        {
            for (int i = 0; i < enemies.Length; i++)
            {
                if (!enemies[i].IsAlive)
                {
                    continue;
                }

                enemies[i].transform.position =
                    player.transform.position + Vector3.forward;
                Physics.SyncTransforms();
                bool hit = MeleeAttackResolver.TryHitNearest(
                    player.gameObject,
                    CombatFaction.Player,
                    Vector3.forward,
                    melee.MeleeRange,
                    melee.MeleeHalfAngle,
                    melee.Damage,
                    MeleeImpactKind.Bat);
                Require(hit, "Player bat missed " + enemies[i].name + ".");
                Require(
                    !enemies[i].IsAlive,
                    "Player bat did not one-hit " + enemies[i].name + ".");
            }
        }

        private static void ValidateReplayAndFlow()
        {
            StageController stage =
                UnityEngine.Object.FindFirstObjectByType<StageController>();
            StageReplayController replay =
                UnityEngine.Object.FindFirstObjectByType<StageReplayController>();
            Require(
                stage != null &&
                stage.CurrentState == StageController.StageState.Replaying,
                "StageBattingCage did not enter replay after clear.");
            Require(
                replay != null && replay.IsReplaying,
                "StageBattingCage replay controller is not replaying.");
            Require(
                StageSceneFlow.TryGetNextDestination(
                    "StageBattingCage",
                    out string destination) &&
                destination == "Stage5",
                "StageBattingCage does not advance to Stage5.");
            Require(
                Application.CanStreamedLevelBeLoaded("Stage5"),
                "Stage5 is not loadable after StageBattingCage.");
        }

        private static void RequireOnNavMesh(
            Vector3 position,
            string subject)
        {
            Require(
                NavigationSceneSetup.IsDirectlyAboveNavMesh(
                    position,
                    out _),
                subject +
                " has no batting-cage NavMesh directly below it.");
        }

        private static void Require(bool condition, string message)
        {
            if (!condition)
            {
                throw new InvalidOperationException(message);
            }
        }

        private static void HandleLog(
            string condition,
            string stackTrace,
            LogType type)
        {
            if (!SessionState.GetBool(RunningKey, false) ||
                (type != LogType.Error && type != LogType.Exception &&
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
            string failure = SessionState.GetString(
                FailureKey,
                string.Empty);
            SessionState.SetBool(RunningKey, false);
            SessionState.SetString(PhaseKey, string.Empty);
            Runner.Detach();

            if (failed)
            {
                Debug.LogError(
                    "StageBattingCage play-mode smoke test failed:\n" +
                    failure);
                EditorApplication.Exit(1);
                return;
            }

            Debug.Log(
                "StageBattingCage play-mode smoke test passed.");
            EditorApplication.Exit(0);
        }
    }
}
