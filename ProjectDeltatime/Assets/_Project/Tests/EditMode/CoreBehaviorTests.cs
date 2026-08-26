using System.Collections.Generic;
using Deltatime.Combat;
using Deltatime.Enemies;
using Deltatime.Level;
using Deltatime.Player;
using Deltatime.Replay;
using Deltatime.TimeSystem;
using Deltatime.Tutorial;
using NUnit.Framework;
using UnityEngine;

namespace Deltatime.Tests.EditMode
{
    public sealed class CoreBehaviorTests
    {
        [Test]
        public void ReplayRecordingClock_AdvancesIndependentTimeAxes()
        {
            ReplayRecordingClock clock = new ReplayRecordingClock();

            clock.Advance(1f, 0.25f);
            clock.Advance(0.5f, 0.1f);

            Assert.That(clock.SourceElapsedTime, Is.EqualTo(1.5f).Within(0.0001f));
            Assert.That(clock.ReplayElapsedTime, Is.EqualTo(0.35f).Within(0.0001f));
        }

        [Test]
        public void ReplayRecordingClock_IgnoresNegativeDeltaAndResets()
        {
            ReplayRecordingClock clock = new ReplayRecordingClock();
            clock.Advance(-1f, -1f);

            Assert.That(clock.SourceElapsedTime, Is.Zero);
            Assert.That(clock.ReplayElapsedTime, Is.Zero);

            clock.Advance(1f, 1f);
            clock.Reset();
            Assert.That(clock.SourceElapsedTime, Is.Zero);
            Assert.That(clock.ReplayElapsedTime, Is.Zero);
        }

        [Test]
        public void ReplayHierarchyPolicy_UsesNearestMarkerAndIncludesOnTie()
        {
            GameObject root = new GameObject("Replay Excluded Root");
            root.AddComponent<ReplayExcluded>();
            GameObject excludedChild = new GameObject("Excluded Child");
            excludedChild.transform.SetParent(root.transform, false);

            GameObject includedBranch = new GameObject("Included Branch");
            includedBranch.transform.SetParent(root.transform, false);
            includedBranch.AddComponent<ReplayIncluded>();
            GameObject includedChild = new GameObject("Included Child");
            includedChild.transform.SetParent(includedBranch.transform, false);

            GameObject innerExcluded = new GameObject("Inner Excluded");
            innerExcluded.transform.SetParent(includedBranch.transform, false);
            innerExcluded.AddComponent<ReplayExcluded>();
            GameObject innerExcludedChild =
                new GameObject("Inner Excluded Child");
            innerExcludedChild.transform.SetParent(
                innerExcluded.transform,
                false);

            GameObject tiedMarkers = new GameObject("Tied Markers");
            tiedMarkers.transform.SetParent(innerExcluded.transform, false);
            tiedMarkers.AddComponent<ReplayExcluded>();
            tiedMarkers.AddComponent<ReplayIncluded>();

            try
            {
                Assert.That(
                    ReplayHierarchyPolicy.IsExcluded(
                        excludedChild.transform),
                    Is.True);
                Assert.That(
                    ReplayHierarchyPolicy.IsExcluded(
                        includedChild.transform),
                    Is.False);
                Assert.That(
                    ReplayHierarchyPolicy.IsExcluded(
                        innerExcludedChild.transform),
                    Is.True);
                Assert.That(
                    ReplayHierarchyPolicy.IsExcluded(tiedMarkers.transform),
                    Is.False);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void ReplayRecordingBudget_PreservesDurationPrecedence()
        {
            Assert.That(
                ReplayRecordingBudget.Evaluate(300f, 64, 300f, 64),
                Is.EqualTo(ReplayRecordingLimitReason.SourceDuration));
            Assert.That(
                ReplayRecordingBudget.Evaluate(10f, 64, 300f, 64),
                Is.EqualTo(ReplayRecordingLimitReason.MemoryBudget));
            Assert.That(
                ReplayRecordingBudget.Evaluate(10f, 63, 300f, 64),
                Is.EqualTo(ReplayRecordingLimitReason.None));
        }

        [Test]
        public void ReplayTimeline_SelectsFirstSegmentEndingAfterTimestamp()
        {
            List<float> segmentEnds = new List<float> { 1f, 2f, 3f };

            Assert.That(
                ReplayTimeline.FindSegmentIndex(
                    segmentEnds,
                    1f,
                    value => value),
                Is.EqualTo(1));
            Assert.That(
                ReplayTimeline.FindSegmentIndex(
                    segmentEnds,
                    2.5f,
                    value => value),
                Is.EqualTo(2));
        }

        [Test]
        public void ReplayPlaybackSession_PreservesHoldAndLoopBehavior()
        {
            ReplayPlaybackSession session = new ReplayPlaybackSession();
            session.Reset(4f);

            ReplayPlaybackStep endStep = session.Advance(
                3f,
                4f,
                6f,
                0.5f,
                true);
            ReplayPlaybackStep holdStep = session.Advance(
                0.25f,
                4f,
                6f,
                0.5f,
                true);
            ReplayPlaybackStep loopStep = session.Advance(
                0.25f,
                4f,
                6f,
                0.5f,
                true);

            Assert.That(endStep.PresentationTime, Is.EqualTo(6f));
            Assert.That(holdStep.ShouldApply, Is.False);
            Assert.That(loopStep.ShouldApply, Is.True);
            Assert.That(loopStep.PresentationTime, Is.EqualTo(4f));
        }

        [Test]
        public void WeaponSpreadPattern_IsDeterministicNormalizedAndBounded()
        {
            Vector3 first = WeaponSpreadPattern.GetProjectileDirection(
                Vector3.forward,
                3,
                8,
                10f,
                0f,
                27,
                4);
            Vector3 second = WeaponSpreadPattern.GetProjectileDirection(
                Vector3.forward,
                3,
                8,
                10f,
                0f,
                27,
                4);

            Assert.That(second, Is.EqualTo(first));
            Assert.That(first.magnitude, Is.EqualTo(1f).Within(0.0001f));
            Assert.That(Vector3.Angle(Vector3.forward, first), Is.LessThanOrEqualTo(5.001f));
        }

        [Test]
        public void EnemyCombatState_TransitionsAndExpiresWithoutComponentState()
        {
            EnemyCombatStateController state =
                new EnemyCombatStateController();

            state.TransitionTo(EnemyCombatant.CombatState.Aiming, 0.5f);
            state.SetMovementMode(EnemyCombatant.MovementMode.Holding);

            Assert.That(
                state.CurrentState,
                Is.EqualTo(EnemyCombatant.CombatState.Aiming));
            Assert.That(
                state.CurrentMovementMode,
                Is.EqualTo(EnemyCombatant.MovementMode.Holding));
            Assert.That(state.AdvanceStateTimer(0.25f), Is.False);
            Assert.That(state.AdvanceStateTimer(0.25f), Is.True);
        }

        [TestCase(10f, (int)FirearmRangeDecision.Pursue)]
        [TestCase(5f, (int)FirearmRangeDecision.Retreat)]
        [TestCase(7f, (int)FirearmRangeDecision.Hold)]
        public void EnemyFirearmRangePolicy_PreservesRangeDecisions(
            float distance,
            int expected)
        {
            Assert.That(
                (int)EnemyFirearmRangePolicy.Decide(distance, 6f, 9f),
                Is.EqualTo(expected));
        }

        [Test]
        public void TutorialScenarios_PreserveStepAndDeadlineGates()
        {
            TutorialProgression progression = new TutorialProgression();
            TutorialDeadlineScenario deadline =
                new TutorialDeadlineScenario();

            progression.MoveTo(TutorialDirector.TutorialStep.Deadline);
            deadline.Begin();

            Assert.That(
                progression.CurrentStep,
                Is.EqualTo(TutorialDirector.TutorialStep.Deadline));
            Assert.That(deadline.Observe(true, 1, 2), Is.True);
            Assert.That(deadline.TrySucceed(), Is.False);
            Assert.That(deadline.Observe(true, 2, 2), Is.False);
            Assert.That(deadline.TrySucceed(), Is.True);
        }

        [Test]
        public void WorldTimeTokens_RequireMatchingRelease()
        {
            GameObject root = new GameObject("World Time Token Test");
            root.SetActive(false);
            WorldTimeActivity activity = root.AddComponent<WorldTimeActivity>();
            WorldTimeController controller =
                root.AddComponent<WorldTimeController>();
            controller.Configure(activity);
            root.SetActive(true);

            try
            {
                int token = controller.AcquireHardFreeze();

                Assert.That(controller.IsHardFrozen, Is.True);
                Assert.That(controller.ReleaseHardFreeze(token + 1), Is.False);
                Assert.That(controller.ReleaseHardFreeze(token), Is.True);
                Assert.That(controller.IsHardFrozen, Is.False);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void WorldTimeAmbientState_OrdersFullHalfAndIdleAudio()
        {
            WorldTimeAmbientAudioState full =
                WorldTimeAmbientState.Evaluate(1f);
            WorldTimeAmbientAudioState half =
                WorldTimeAmbientState.Evaluate(0.5f);
            WorldTimeAmbientAudioState idle =
                WorldTimeAmbientState.Evaluate(0.02f);

            Assert.That(full.Pitch, Is.EqualTo(1f).Within(0.0001f));
            Assert.That(full.CutoffFrequency, Is.EqualTo(16000f).Within(0.01f));
            Assert.That(full.VolumeFactor, Is.EqualTo(1f).Within(0.0001f));
            Assert.That(half.Pitch, Is.LessThan(full.Pitch));
            Assert.That(idle.Pitch, Is.LessThan(half.Pitch));
            Assert.That(half.CutoffFrequency, Is.LessThan(full.CutoffFrequency));
            Assert.That(idle.CutoffFrequency, Is.LessThan(half.CutoffFrequency));
            Assert.That(half.VolumeFactor, Is.LessThan(full.VolumeFactor));
            Assert.That(idle.VolumeFactor, Is.LessThan(half.VolumeFactor));
        }

        [Test]
        public void WorldTimeAmbientState_ReachesSilenceWithinResponseDuration()
        {
            float audibleScale =
                WorldTimeAmbientState.AdvanceAudibleTimeScale(
                    1f,
                    0f,
                    0.15f,
                    0.15f);
            WorldTimeAmbientAudioState frozen =
                WorldTimeAmbientState.Evaluate(audibleScale);

            Assert.That(audibleScale, Is.Zero);
            Assert.That(frozen.Pitch, Is.EqualTo(0.45f).Within(0.0001f));
            Assert.That(frozen.CutoffFrequency, Is.EqualTo(500f).Within(0.01f));
            Assert.That(frozen.VolumeFactor, Is.Zero);
        }

        [Test]
        public void PlayerAttackDecision_RequiresAutomaticWeaponForHeldFire()
        {
            Assert.That(
                PlayerAttackDecision.ShouldUseWeapon(false, true, null),
                Is.False);
            Assert.That(
                PlayerAttackDecision.ShouldUseWeapon(true, false, null),
                Is.True);
        }

        [TestCase(false, false, false, (int)PlayerWeaponInteractionPrompt.None)]
        [TestCase(false, true, false, (int)PlayerWeaponInteractionPrompt.PickUp)]
        [TestCase(false, true, true, (int)PlayerWeaponInteractionPrompt.Swap)]
        [TestCase(true, false, false, (int)PlayerWeaponInteractionPrompt.Catch)]
        [TestCase(true, true, true, (int)PlayerWeaponInteractionPrompt.Catch)]
        public void PlayerWeaponInteractionPrompt_UsesActionPriority(
            bool hasAirborneWeapon,
            bool hasGroundWeapon,
            bool hasEquippedWeapon,
            int expected)
        {
            Assert.That(
                PlayerWeaponInteractionPromptPolicy.Resolve(
                    hasAirborneWeapon,
                    hasGroundWeapon,
                    hasEquippedWeapon),
                Is.EqualTo((PlayerWeaponInteractionPrompt)expected));
        }

        [TestCase("Stage1", "Stage2")]
        [TestCase("Stage2", "StageBattingCage")]
        [TestCase("StageBattingCage", "Stage5")]
        [TestCase("Stage5", "EndingScene")]
        public void StageSceneFlow_PreservesActiveRoute(
            string current,
            string expected)
        {
            bool found = StageSceneFlow.TryGetNextDestination(
                current,
                out string destination);

            Assert.That(found, Is.True);
            Assert.That(destination, Is.EqualTo(expected));
        }

        [Test]
        public void StageSceneFlow_RejectsDormantStage()
        {
            Assert.That(
                StageSceneFlow.TryGetNextDestination("Stage6", out string destination),
                Is.False);
            Assert.That(destination, Is.Empty);
        }
    }
}
