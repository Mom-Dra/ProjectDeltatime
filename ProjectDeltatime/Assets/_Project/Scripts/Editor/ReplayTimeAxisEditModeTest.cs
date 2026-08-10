using System;
using Deltatime.Replay;
using UnityEditor;
using UnityEngine;

namespace Deltatime.EditorTools
{
    public static class ReplayTimeAxisEditModeTest
    {
        public static void RunFromCommandLine()
        {
            try
            {
                ValidateNormalAndSlowRecordingsHaveEqualReplayLength();
                ValidateVariableScaleEventOrder();
                ValidateHardFreezeCompression();
                ValidateRecordingBudgetPolicy();
                if (!Mathf.Approximately(Time.timeScale, 1f))
                {
                    throw new InvalidOperationException(
                        "Replay time-axis test changed global Time.timeScale.");
                }

                Debug.Log("Replay time-axis edit-mode test passed.");
                EditorApplication.Exit(0);
            }
            catch (Exception exception)
            {
                Debug.LogError(
                    $"Replay time-axis edit-mode test failed: {exception}");
                EditorApplication.Exit(1);
            }
        }

        private static void
            ValidateNormalAndSlowRecordingsHaveEqualReplayLength()
        {
            ReplayRecordingClock normal = default;
            for (int i = 0; i < 120; i++)
            {
                normal.Advance(0.05f, 0.05f);
            }

            ReplayRecordingClock strongSlow = default;
            for (int i = 0; i < 600; i++)
            {
                strongSlow.Advance(0.05f, 0.01f);
            }

            RequireApproximately(
                normal.ReplayElapsedTime,
                6f,
                "Normal-speed replay duration");
            RequireApproximately(
                strongSlow.ReplayElapsedTime,
                normal.ReplayElapsedTime,
                "Strong-slow replay duration");
            RequireApproximately(
                normal.SourceElapsedTime,
                6f,
                "Normal source duration");
            RequireApproximately(
                strongSlow.SourceElapsedTime,
                30f,
                "Strong-slow source duration");
        }

        private static void ValidateVariableScaleEventOrder()
        {
            ReplayRecordingClock clock = default;
            float previousSource = 0f;
            float previousReplay = 0f;
            float[] worldDeltas =
            {
                0.05f, // movement
                0.005f, // attack under strong slow
                0f, // hard-freeze staging
                0.02f, // hit resolution
                0.05f // aftermath movement
            };

            for (int i = 0; i < worldDeltas.Length; i++)
            {
                clock.Advance(0.05f, worldDeltas[i]);
                if (clock.SourceElapsedTime <= previousSource ||
                    clock.ReplayElapsedTime < previousReplay)
                {
                    throw new InvalidOperationException(
                        $"Replay event order regressed at event {i}.");
                }

                previousSource = clock.SourceElapsedTime;
                previousReplay = clock.ReplayElapsedTime;
            }

            RequireApproximately(
                clock.ReplayElapsedTime,
                0.125f,
                "Variable-scale normalized duration");
        }

        private static void ValidateHardFreezeCompression()
        {
            ReplayRecordingClock clock = default;
            clock.Advance(0.5f, 0.5f);
            float beforeFreeze = clock.ReplayElapsedTime;
            for (int i = 0; i < 200; i++)
            {
                clock.Advance(0.05f, 0f);
            }

            RequireApproximately(
                clock.ReplayElapsedTime,
                beforeFreeze,
                "Hard-freeze replay duration");
            RequireApproximately(
                clock.SourceElapsedTime,
                10.5f,
                "Hard-freeze source duration");
        }

        private static void ValidateRecordingBudgetPolicy()
        {
            ReplayRecordingLimitReason none = ReplayRecordingBudget.Evaluate(
                59f,
                1024L,
                60f,
                2048L);
            ReplayRecordingLimitReason duration =
                ReplayRecordingBudget.Evaluate(
                    60f,
                    1024L,
                    60f,
                    2048L);
            ReplayRecordingLimitReason memory =
                ReplayRecordingBudget.Evaluate(
                    59f,
                    2048L,
                    60f,
                    2048L);
            if (none != ReplayRecordingLimitReason.None ||
                duration != ReplayRecordingLimitReason.SourceDuration ||
                memory != ReplayRecordingLimitReason.MemoryBudget)
            {
                throw new InvalidOperationException(
                    "Replay recording budget policy did not stop explicitly at its limits.");
            }
        }

        private static void RequireApproximately(
            float actual,
            float expected,
            string label)
        {
            if (Mathf.Abs(actual - expected) > 0.001f)
            {
                throw new InvalidOperationException(
                    $"{label} was {actual:0.0000}, expected " +
                    $"{expected:0.0000}.");
            }
        }
    }
}
