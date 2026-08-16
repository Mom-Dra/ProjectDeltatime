using Deltatime.UI;
using NUnit.Framework;
using UnityEngine;

namespace Deltatime.Tests.EditMode
{
    public sealed class HudPresentationTests
    {
        [TestCase("Stage1", "STAGE 1")]
        [TestCase("Stage2", "STAGE 2")]
        [TestCase("Stage5", "STAGE 3")]
        [TestCase("Stage6", "STAGE 4")]
        [TestCase("Tutorial", "TUTORIAL")]
        public void StageLabel_UsesProgressionOrder(
            string sceneName,
            string expected)
        {
            Assert.That(
                HudDisplayFormatter.FormatStageLabel(sceneName),
                Is.EqualTo(expected));
        }

        [Test]
        public void HealthSlots_RepresentFullPartialAndEmptyHealth()
        {
            Assert.That(
                HudDisplayFormatter.IsHealthSlotFilled(3, 3, 0),
                Is.True);
            Assert.That(
                HudDisplayFormatter.IsHealthSlotFilled(3, 3, 2),
                Is.True);
            Assert.That(
                HudDisplayFormatter.IsHealthSlotFilled(2, 3, 1),
                Is.True);
            Assert.That(
                HudDisplayFormatter.IsHealthSlotFilled(2, 3, 2),
                Is.False);
            Assert.That(
                HudDisplayFormatter.IsHealthSlotFilled(0, 3, 0),
                Is.False);
        }

        [TestCase(2, "2")]
        [TestCase(1, "1")]
        [TestCase(0, "0")]
        [TestCase(-1, "0")]
        public void DeadlineCount_ClampsToNonNegative(
            int charges,
            string expected)
        {
            Assert.That(
                HudDisplayFormatter.FormatChargeCount(charges),
                Is.EqualTo(expected));
        }

        [TestCase(true, true, 16, "16")]
        [TestCase(true, true, 0, "0")]
        [TestCase(true, false, 0, "—")]
        [TestCase(false, false, 0, "—")]
        public void Ammunition_UsesCurrentCountOrDash(
            bool hasWeapon,
            bool isFirearm,
            int ammunition,
            string expected)
        {
            Assert.That(
                HudDisplayFormatter.FormatAmmunition(
                    hasWeapon,
                    isFirearm,
                    ammunition),
                Is.EqualTo(expected));
        }

        [TestCase(0f, false, "SCALE 0.00x")]
        [TestCase(0.02f, false, "SCALE 0.02x")]
        [TestCase(1f, false, "SCALE 1.00x")]
        [TestCase(0f, true, "REPLAY 1.00x")]
        public void TimeScale_FormatsLiveAndReplayStates(
            float scale,
            bool replay,
            string expected)
        {
            Assert.That(
                HudDisplayFormatter.FormatTimeScale(scale, replay),
                Is.EqualTo(expected));
        }

        [TestCase(1920f, 1080f)]
        [TestCase(1280f, 720f)]
        public void Layout_StaysInsideScreenAndAvoidsPersistentOverlap(
            float width,
            float height)
        {
            CyberHudLayout layout = CyberHudLayout.Calculate(
                new Rect(0f, 0f, width, height));

            Assert.That(layout.Contains(layout.StageLabel), Is.True);
            Assert.That(layout.Contains(layout.VitalsPanel), Is.True);
            Assert.That(layout.Contains(layout.WeaponPanel), Is.True);
            Assert.That(layout.Contains(layout.TimePanel), Is.True);
            Assert.That(layout.Contains(layout.ControlsPanel), Is.True);
            Assert.That(layout.Contains(layout.TutorialPanel), Is.True);
            Assert.That(layout.Contains(layout.TopMessagePanel), Is.True);
            Assert.That(layout.Contains(layout.CenterMessagePanel), Is.True);
            Assert.That(
                layout.VitalsPanel.Overlaps(layout.WeaponPanel),
                Is.False);
            Assert.That(
                layout.ControlsPanel.Overlaps(layout.WeaponPanel),
                Is.False);
            Assert.That(
                layout.ControlsPanel.Overlaps(layout.TimePanel),
                Is.False);
            Assert.That(
                layout.TutorialPanel.Overlaps(layout.WeaponPanel),
                Is.False);
            Assert.That(
                layout.TutorialPanel.Overlaps(layout.TimePanel),
                Is.False);
            Assert.That(
                layout.TopMessagePanel.Overlaps(layout.StageLabel),
                Is.False);
            Assert.That(
                layout.TopMessagePanel.Overlaps(layout.VitalsPanel),
                Is.False);
            Assert.That(
                layout.TopMessagePanel.Overlaps(layout.TimePanel),
                Is.False);
            Assert.That(
                layout.CenterMessagePanel.Overlaps(layout.VitalsPanel),
                Is.False);
            Assert.That(
                layout.CenterMessagePanel.Overlaps(layout.TimePanel),
                Is.False);
        }

        [Test]
        public void Layout_HonorsInsetSafeArea()
        {
            Rect safeArea = new Rect(48f, 32f, 1824f, 1016f);
            CyberHudLayout layout = CyberHudLayout.Calculate(safeArea);

            Assert.That(layout.Contains(layout.StageLabel), Is.True);
            Assert.That(layout.Contains(layout.VitalsPanel), Is.True);
            Assert.That(layout.Contains(layout.WeaponPanel), Is.True);
            Assert.That(layout.Contains(layout.TimePanel), Is.True);
            Assert.That(layout.Contains(layout.TutorialPanel), Is.True);
            Assert.That(layout.Contains(layout.TopMessagePanel), Is.True);
            Assert.That(layout.Contains(layout.CenterMessagePanel), Is.True);
        }

        [TestCase(1920f, 1080f)]
        [TestCase(1280f, 720f)]
        public void InteractionPrompt_UsesSafeAreaAndAvoidsItsAnchor(
            float width,
            float height)
        {
            CyberHudLayout layout = CyberHudLayout.Calculate(
                new Rect(0f, 0f, width, height));

            Rect gamePrompt = layout.GetWeaponInteractionPromptPanel(false);
            Rect tutorialPrompt = layout.GetWeaponInteractionPromptPanel(true);

            Assert.That(layout.Contains(gamePrompt), Is.True);
            Assert.That(layout.Contains(tutorialPrompt), Is.True);
            Assert.That(gamePrompt.Overlaps(layout.ControlsPanel), Is.False);
            Assert.That(tutorialPrompt.Overlaps(layout.TutorialPanel), Is.False);
            Assert.That(gamePrompt.yMax, Is.LessThanOrEqualTo(
                layout.ControlsPanel.yMin));
            Assert.That(tutorialPrompt.yMax, Is.LessThanOrEqualTo(
                layout.TutorialPanel.yMin));
        }
    }
}
