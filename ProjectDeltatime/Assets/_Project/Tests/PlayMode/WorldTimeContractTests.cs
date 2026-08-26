using System.Collections;
using Deltatime.Player;
using Deltatime.Replay;
using Deltatime.TimeSystem;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Deltatime.Tests.PlayMode
{
    public sealed class WorldTimeContractTests
    {
        [UnityTest]
        public IEnumerator HardFreeze_DoesNotChangeGlobalTimeScale()
        {
            float originalTimeScale = Time.timeScale;
            GameObject root = new GameObject("World Time Contract Test");
            root.SetActive(false);
            WorldTimeActivity activity = root.AddComponent<WorldTimeActivity>();
            WorldTimeController controller = root.AddComponent<WorldTimeController>();
            controller.Configure(activity);
            root.SetActive(true);

            try
            {
                Time.timeScale = 1f;
                yield return null;

                int token = controller.AcquireHardFreeze();
                yield return null;

                Assert.That(Time.timeScale, Is.EqualTo(1f));
                Assert.That(controller.WorldDeltaTime, Is.Zero);
                Assert.That(controller.ReleaseHardFreeze(token), Is.True);
            }
            finally
            {
                Time.timeScale = originalTimeScale;
                Object.Destroy(root);
            }
        }

        [UnityTest]
        public IEnumerator AmbientAnchor_FollowsWorldTimeAndStopsForHardFreeze()
        {
            float originalTimeScale = Time.timeScale;
            GameObject timeRoot = new GameObject("Ambient World Time Test");
            timeRoot.SetActive(false);
            WorldTimeActivity activity = timeRoot.AddComponent<WorldTimeActivity>();
            WorldTimeController controller =
                timeRoot.AddComponent<WorldTimeController>();
            controller.Configure(activity);

            GameObject anchorRoot = new GameObject("Ambient Anchor Test");
            anchorRoot.SetActive(false);
            GameObject rotorObject = new GameObject("Rotor");
            rotorObject.transform.SetParent(anchorRoot.transform, false);
            AudioSource source = anchorRoot.AddComponent<AudioSource>();
            anchorRoot.AddComponent<AudioListener>();
            source.clip = AudioClip.Create(
                "Ambient Anchor Test Loop",
                4410,
                1,
                44100,
                false);
            source.loop = true;
            AudioLowPassFilter filter =
                anchorRoot.AddComponent<AudioLowPassFilter>();
            WorldTimeAmbientAnchor anchor =
                anchorRoot.AddComponent<WorldTimeAmbientAnchor>();
            anchor.Configure(controller);
            anchor.ConfigurePresentationForTests(
                rotorObject.transform,
                source,
                filter);

            try
            {
                Time.timeScale = 1f;
                activity.SetMovement(1f);
                timeRoot.SetActive(true);
                anchorRoot.SetActive(true);
                yield return new WaitForSecondsRealtime(0.05f);

                Quaternion beforeRotation = rotorObject.transform.localRotation;
                yield return new WaitForSecondsRealtime(0.05f);
                Assert.That(
                    Quaternion.Angle(
                        beforeRotation,
                        rotorObject.transform.localRotation),
                    Is.GreaterThan(0.01f));
                Assert.That(anchor.CurrentOutputVolume, Is.GreaterThan(0f));

                int token = controller.AcquireHardFreeze();
                yield return new WaitForSecondsRealtime(0.18f);

                Quaternion frozenRotation = rotorObject.transform.localRotation;
                yield return null;
                Assert.That(
                    Quaternion.Angle(
                        frozenRotation,
                        rotorObject.transform.localRotation),
                    Is.LessThan(0.001f));
                Assert.That(anchor.CurrentOutputVolume, Is.Zero.Within(0.001f));
                Assert.That(
                    anchor.CurrentCutoffFrequency,
                    Is.EqualTo(500f).Within(1f));
                Assert.That(Time.timeScale, Is.EqualTo(1f));

                Assert.That(controller.ReleaseHardFreeze(token), Is.True);
                yield return new WaitForSecondsRealtime(0.18f);
                Assert.That(anchor.CurrentOutputVolume, Is.GreaterThan(0f));

                anchor.enabled = false;
                Assert.That(anchor.IsLoopPlaying, Is.False);
            }
            finally
            {
                Time.timeScale = originalTimeScale;
                Object.Destroy(anchorRoot);
                Object.Destroy(timeRoot);
            }
        }

        [UnityTest]
        public IEnumerator AmbientAnchor_RotorIsRecordedBelowExcludedHousing()
        {
            float originalTimeScale = Time.timeScale;
            GameObject replayObject = new GameObject("Ambient Replay Test");
            replayObject.SetActive(false);
            WorldTimeActivity activity =
                replayObject.AddComponent<WorldTimeActivity>();
            WorldTimeController controller =
                replayObject.AddComponent<WorldTimeController>();
            controller.Configure(activity);
            Camera replayCamera = replayObject.AddComponent<Camera>();
            replayObject.AddComponent<AudioListener>();
            DeadlineController deadline =
                replayObject.AddComponent<DeadlineController>();
            StageReplayController replay =
                replayObject.AddComponent<StageReplayController>();
            replay.Configure(controller, replayCamera, deadline);
            replay.ConfigureRendererDiscovery(new Transform[0], 0f);

            GameObject anchorRoot = new GameObject("Excluded Fan Housing");
            anchorRoot.SetActive(false);
            anchorRoot.AddComponent<ReplayExcluded>();
            GameObject rotorObject = GameObject.CreatePrimitive(
                PrimitiveType.Cube);
            rotorObject.name = "Ambient Replay Rotor";
            rotorObject.transform.SetParent(anchorRoot.transform, false);
            rotorObject.AddComponent<ReplayIncluded>();
            Renderer rotorRenderer = rotorObject.GetComponent<Renderer>();
            AudioSource source = anchorRoot.AddComponent<AudioSource>();
            AudioClip clip = AudioClip.Create(
                "Ambient Replay Test Loop",
                4410,
                1,
                44100,
                false);
            source.clip = clip;
            source.loop = true;
            AudioLowPassFilter filter =
                anchorRoot.AddComponent<AudioLowPassFilter>();
            WorldTimeAmbientAnchor anchor =
                anchorRoot.AddComponent<WorldTimeAmbientAnchor>();
            anchor.Configure(controller);
            anchor.ConfigurePresentationForTests(
                rotorObject.transform,
                source,
                filter);

            try
            {
                Time.timeScale = 1f;
                activity.SetMovement(1f);
                anchorRoot.SetActive(true);
                LogAssert.Expect(
                    LogType.Error,
                    "DeadlineController is missing required references.");
                replayObject.SetActive(true);
                yield return new WaitForSecondsRealtime(0.28f);

                Assert.That(replay.TrackedVisualCount, Is.EqualTo(1));
                Assert.That(replay.TrackedExcludedVisualCount, Is.Zero);
                Transform proxyTransform = replayObject.transform.Find(
                    "Replay Visuals/Replay - Ambient Replay Rotor");
                Assert.That(proxyTransform, Is.Not.Null);

                Assert.That(replay.RequestReplay(), Is.True);
                yield return null;

                Assert.That(replay.IsReplaying, Is.True);
                Assert.That(anchor.enabled, Is.False);
                Assert.That(anchor.IsLoopPlaying, Is.False);
                Assert.That(rotorRenderer.enabled, Is.False);
                Renderer proxyRenderer =
                    proxyTransform.GetComponent<Renderer>();
                Assert.That(proxyRenderer.enabled, Is.True);

                Quaternion beforeReplayRotation =
                    proxyTransform.rotation;
                yield return new WaitForSecondsRealtime(0.1f);
                Assert.That(
                    Quaternion.Angle(
                        beforeReplayRotation,
                        proxyTransform.rotation),
                    Is.GreaterThan(1f));
            }
            finally
            {
                Time.timeScale = originalTimeScale;
                Object.Destroy(anchorRoot);
                Object.Destroy(replayObject);
                Object.Destroy(clip);
            }

            yield return null;
        }
    }
}
