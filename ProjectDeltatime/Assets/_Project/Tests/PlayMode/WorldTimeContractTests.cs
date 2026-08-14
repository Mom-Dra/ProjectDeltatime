using System.Collections;
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
    }
}
