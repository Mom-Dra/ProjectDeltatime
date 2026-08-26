using System.Collections;
using Deltatime.Combat;
using Deltatime.Core;
using Deltatime.Enemies;
using Deltatime.InputSystem;
using Deltatime.Level;
using Deltatime.Player;
using Deltatime.Replay;
using Deltatime.TimeSystem;
using Deltatime.Utilities;
using Deltatime.Visuals;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Deltatime.Tests.PlayMode
{
    public sealed class CombatFeedbackTests
    {
        [UnityTest]
        public IEnumerator PlayerShot_PlaysOneMuzzleAndImpulseWithoutMovingPlayer()
        {
            float originalTimeScale = Time.timeScale;
            FeedbackRig rig = FeedbackRig.Create();
            WeaponDefinition shotgun = CreateWeapon(
                "Test Shotgun",
                0.11f,
                0.32f,
                0.15f,
                0.045f,
                0.28f,
                4);
            GameObject muzzle = new GameObject("Test Muzzle");
            Vector3 playerPosition = rig.Player.transform.position;

            try
            {
                Time.timeScale = 1f;
                int muzzleCount = Object.FindObjectsByType<MuzzleFlash>(
                    FindObjectsSortMode.None).Length;

                CombatFeedbackController.ReportWeaponFired(
                    shotgun,
                    CombatFaction.Player,
                    muzzle.transform);
                for (int i = 0; i < shotgun.ProjectileCount; i++)
                {
                    CombatFeedbackController.ReportImpact(
                        shotgun,
                        CombatFaction.Player,
                        CombatFaction.Enemy,
                        Vector3.forward + Vector3.right * i * 0.1f,
                        Vector3.forward,
                        true);
                }

                Assert.That(
                    Object.FindObjectsByType<MuzzleFlash>(
                        FindObjectsSortMode.None).Length,
                    Is.EqualTo(muzzleCount + 1));
                Assert.That(rig.CameraController.ImpulsePlayCount, Is.EqualTo(1));
                Assert.That(
                    rig.WorldTime.HardFreezeRemaining,
                    Is.EqualTo(0.045f).Within(0.0001f));
                Assert.That(rig.Player.transform.position, Is.EqualTo(playerPosition));
                Assert.That(Time.timeScale, Is.EqualTo(1f));
            }
            finally
            {
                Time.timeScale = originalTimeScale;
                Object.Destroy(muzzle);
                Object.Destroy(shotgun);
                rig.Destroy();
            }

            yield return null;
        }

        [UnityTest]
        public IEnumerator MeleeMissHasNoImpact_EnemyHitHasOneFlashAndDeathReaction()
        {
            float originalTimeScale = Time.timeScale;
            FeedbackRig rig = FeedbackRig.Create();
            GameObject source = new GameObject("Melee Source");
            GameObject enemyObject = new GameObject("Melee Target");
            enemyObject.SetActive(false);
            BoxCollider enemyCollider = enemyObject.AddComponent<BoxCollider>();
            EnemyHealth enemy = enemyObject.AddComponent<EnemyHealth>();
            enemy.Configure(null, null, null, enemyCollider, null);
            enemyObject.transform.position = Vector3.forward * 0.8f;
            enemyObject.SetActive(true);
            Physics.SyncTransforms();

            try
            {
                Time.timeScale = 1f;
                int flashCount = Object.FindObjectsByType<HitFlash>(
                    FindObjectsSortMode.None).Length;

                Assert.That(
                    MeleeAttackResolver.TryHitNearest(
                        source,
                        CombatFaction.Player,
                        Vector3.back,
                        1.45f,
                        35f,
                        1),
                    Is.False);
                Assert.That(rig.CameraController.ImpulsePlayCount, Is.Zero);
                Assert.That(rig.WorldTime.IsHardFrozen, Is.False);
                Assert.That(
                    Object.FindObjectsByType<HitFlash>(
                        FindObjectsSortMode.None).Length,
                    Is.EqualTo(flashCount));

                Assert.That(
                    MeleeAttackResolver.TryHitNearest(
                        source,
                        CombatFaction.Player,
                        Vector3.forward,
                        1.45f,
                        35f,
                        1),
                    Is.True);
                Assert.That(enemy.IsAlive, Is.False);
                Assert.That(enemyCollider.enabled, Is.False);
                Assert.That(enemy.IsPresentingDeath, Is.True);
                Assert.That(enemyObject, Is.Not.Null);
                Assert.That(rig.CameraController.ImpulsePlayCount, Is.EqualTo(1));
                Assert.That(
                    Object.FindObjectsByType<HitFlash>(
                        FindObjectsSortMode.None).Length,
                    Is.EqualTo(flashCount + 1));
                Assert.That(rig.WorldTime.HardFreezeRemaining, Is.GreaterThan(0f));
                Assert.That(Time.timeScale, Is.EqualTo(1f));

                yield return new WaitForSecondsRealtime(0.18f);
                Assert.That(enemyObject, Is.Not.Null);
                Assert.That(enemyObject.transform.position.z, Is.GreaterThan(0.8f));
                yield return new WaitForSecondsRealtime(0.2f);
                Assert.That(enemyObject == null, Is.True);
            }
            finally
            {
                Time.timeScale = originalTimeScale;
                Object.Destroy(source);
                if (enemyObject != null)
                {
                    Object.Destroy(enemyObject);
                }

                rig.Destroy();
            }

            yield return null;
        }

        [UnityTest]
        public IEnumerator PlayerDamage_FeedbackOnlyForActualDamage()
        {
            float originalTimeScale = Time.timeScale;
            FeedbackRig rig = FeedbackRig.Create();
            GameObject attacker = new GameObject("Damage Source");
            DamageHit hit = new DamageHit(
                1,
                rig.Player.transform.position,
                Vector3.forward,
                attacker);

            try
            {
                Time.timeScale = 1f;
                rig.PlayerHealth.SetDashInvulnerable(true);
                rig.PlayerHealth.ReceiveHit(hit);
                Assert.That(rig.PlayerHealth.CurrentHealth, Is.EqualTo(3));
                Assert.That(rig.Feedback.PlayerDamageFeedbackCount, Is.Zero);
                Assert.That(rig.Feedback.IsDamageFlashActive, Is.False);
                Assert.That(rig.CameraController.ImpulsePlayCount, Is.Zero);
                Assert.That(rig.WorldTime.IsHardFrozen, Is.False);

                rig.PlayerHealth.SetDashInvulnerable(false);
                rig.PlayerHealth.ReceiveHit(hit);
                Assert.That(rig.PlayerHealth.CurrentHealth, Is.EqualTo(2));
                Assert.That(rig.Feedback.PlayerDamageFeedbackCount, Is.EqualTo(1));
                Assert.That(rig.Feedback.IsDamageFlashActive, Is.True);
                Assert.That(rig.CameraController.ImpulsePlayCount, Is.EqualTo(1));
                Assert.That(
                    rig.WorldTime.HardFreezeRemaining,
                    Is.EqualTo(0.04f).Within(0.0001f));
                Assert.That(Time.timeScale, Is.EqualTo(1f));
            }
            finally
            {
                Time.timeScale = originalTimeScale;
                Object.Destroy(attacker);
                rig.Destroy();
            }

            yield return null;
        }

        [UnityTest]
        public IEnumerator LastKill_ClearsImmediatelyAndDelaysReplayForReaction()
        {
            float originalTimeScale = Time.timeScale;
            FeedbackRig rig = FeedbackRig.Create();
            GameObject deadlineObject = new GameObject("Inactive Deadline");
            deadlineObject.SetActive(false);
            DeadlineController deadline =
                deadlineObject.AddComponent<DeadlineController>();
            GameObject replayVisual = GameObject.CreatePrimitive(
                PrimitiveType.Cube);
            replayVisual.name = "Delayed Replay Visual";

            GameObject replayObject = new GameObject("Delayed Replay");
            replayObject.SetActive(false);
            StageReplayController replay =
                replayObject.AddComponent<StageReplayController>();
            replay.Configure(
                rig.WorldTime,
                rig.CameraRoot.GetComponent<Camera>(),
                deadline);
            replay.enabled = true;
            replayObject.SetActive(true);

            GameObject inactiveDependencies =
                new GameObject("Inactive Stage Dependencies");
            inactiveDependencies.SetActive(false);
            PlayerInputReader input =
                inactiveDependencies.AddComponent<PlayerInputReader>();
            input.Configure(rig.TimeRoot.GetComponent<WorldTimeActivity>());
            PlayerCombat combat =
                inactiveDependencies.AddComponent<PlayerCombat>();

            GameObject stageObject = new GameObject("Delayed Stage");
            stageObject.SetActive(false);
            StageController stage = stageObject.AddComponent<StageController>();
            stage.Configure(input, rig.PlayerHealth, combat, replay);
            stageObject.SetActive(true);

            GameObject enemyObject = new GameObject("Last Enemy");
            enemyObject.SetActive(false);
            EnemyHealth enemy = enemyObject.AddComponent<EnemyHealth>();
            stage.RegisterEnemy(enemy);

            try
            {
                Time.timeScale = 1f;
                yield return new WaitForSecondsRealtime(0.1f);

                stage.NotifyEnemyDied(enemy, 0.32f);
                Assert.That(stage.CurrentState, Is.EqualTo(
                    StageController.StageState.Cleared));
                Assert.That(stage.RemainingEnemyCount, Is.Zero);
                Assert.That(replay.IsReplaying, Is.False);
                Assert.That(replay.TimelineEvents.Count, Is.EqualTo(2));
                Assert.That(
                    replay.TimelineEvents[0].Kind,
                    Is.EqualTo(
                        StageReplayController.ReplayTimelineEventKind.Kill));
                Assert.That(
                    replay.TimelineEvents[1].Kind,
                    Is.EqualTo(
                        StageReplayController.ReplayTimelineEventKind.Clear));

                yield return new WaitForSecondsRealtime(0.18f);
                Assert.That(stage.CurrentState, Is.EqualTo(
                    StageController.StageState.Cleared));
                Assert.That(replay.IsReplaying, Is.False);

                yield return new WaitForSecondsRealtime(0.18f);
                Assert.That(stage.CurrentState, Is.EqualTo(
                    StageController.StageState.Replaying));
                Assert.That(replay.IsReplaying, Is.True);
                Assert.That(Time.timeScale, Is.EqualTo(1f));
            }
            finally
            {
                Time.timeScale = originalTimeScale;
                Object.Destroy(enemyObject);
                Object.Destroy(stageObject);
                Object.Destroy(inactiveDependencies);
                Object.Destroy(replayObject);
                Object.Destroy(deadlineObject);
                Object.Destroy(replayVisual);
                rig.Destroy();
            }

            yield return null;
        }

        private static WeaponDefinition CreateWeapon(
            string name,
            float positionImpulse,
            float rotationImpulse,
            float impulseDuration,
            float hitStopDuration,
            float muzzleSize,
            int projectileCount)
        {
            WeaponDefinition definition =
                ScriptableObject.CreateInstance<WeaponDefinition>();
            definition.ConfigureFirearmPrototype(
                name,
                6,
                0.75f,
                16f,
                1,
                0.075f,
                1,
                WeaponFireMode.SemiAutomatic,
                projectileCount,
                18f,
                0f,
                307,
                0f,
                14f);
            definition.ConfigureCombatFeedback(
                positionImpulse,
                rotationImpulse,
                impulseDuration,
                hitStopDuration,
                muzzleSize);
            return definition;
        }

        private sealed class FeedbackRig
        {
            public GameObject TimeRoot { get; private set; }
            public GameObject Player { get; private set; }
            public GameObject CameraRoot { get; private set; }
            public WorldTimeController WorldTime { get; private set; }
            public PlayerHealth PlayerHealth { get; private set; }
            public TopDownCameraController CameraController { get; private set; }
            public CombatFeedbackController Feedback { get; private set; }

            public static FeedbackRig Create()
            {
                FeedbackRig rig = new FeedbackRig();

                rig.TimeRoot = new GameObject("Combat Feedback Time");
                rig.TimeRoot.SetActive(false);
                WorldTimeActivity activity =
                    rig.TimeRoot.AddComponent<WorldTimeActivity>();
                rig.WorldTime =
                    rig.TimeRoot.AddComponent<WorldTimeController>();
                rig.WorldTime.Configure(activity);
                rig.TimeRoot.SetActive(true);

                rig.Player = new GameObject("Combat Feedback Player");
                rig.Player.SetActive(false);
                rig.PlayerHealth = rig.Player.AddComponent<PlayerHealth>();
                rig.PlayerHealth.Configure(null);
                rig.Player.SetActive(true);

                rig.CameraRoot = new GameObject("Combat Feedback Camera");
                rig.CameraRoot.SetActive(false);
                rig.CameraRoot.AddComponent<Camera>();
                rig.CameraRoot.AddComponent<AudioListener>();
                rig.CameraController =
                    rig.CameraRoot.AddComponent<TopDownCameraController>();
                rig.Feedback =
                    rig.CameraRoot.AddComponent<CombatFeedbackController>();
                rig.Feedback.Configure(
                    rig.WorldTime,
                    rig.CameraController,
                    rig.PlayerHealth);
                rig.CameraRoot.SetActive(true);
                return rig;
            }

            public void Destroy()
            {
                Object.Destroy(CameraRoot);
                Object.Destroy(Player);
                Object.Destroy(TimeRoot);
            }
        }
    }
}
