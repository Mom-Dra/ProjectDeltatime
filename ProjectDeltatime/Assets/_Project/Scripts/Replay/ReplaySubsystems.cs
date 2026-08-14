using System;
using System.Collections.Generic;
using Deltatime.Visuals;
using UnityEngine;

namespace Deltatime.Replay
{
    internal interface IReplayCaptureSink
    {
        bool RegisterRenderer(Renderer source);
        int RegisterRendererHierarchy(Transform sourceRoot);
        bool RegisterAnimationSource(CharacterAnimationController source);
        void RecordAnimatorController(
            CharacterAnimationController source,
            RuntimeAnimatorController controller);
        void RecordAnimatorTrigger(
            CharacterAnimationController source,
            int parameterHash,
            bool set);
        void RecordAnimatorActive(
            CharacterAnimationController source,
            bool active);
    }

    internal static class ReplayVisualRegistry
    {
        public static IReplayCaptureSink Active { get; private set; }

        public static void SetActive(IReplayCaptureSink sink)
        {
            Active = sink;
        }

        public static void Clear(IReplayCaptureSink sink)
        {
            if (ReferenceEquals(Active, sink))
            {
                Active = null;
            }
        }
    }

    internal sealed class ReplayCaptureSession
    {
        private ReplayRecordingClock clock;
        private float captureAccumulator;

        public float SourceElapsedTime => clock.SourceElapsedTime;
        public float ReplayElapsedTime => clock.ReplayElapsedTime;

        public void Advance(float realDeltaTime, float worldDeltaTime)
        {
            clock.Advance(realDeltaTime, worldDeltaTime);
        }

        public bool ConsumeCaptureDue(float realDeltaTime, float captureRate)
        {
            float captureInterval = 1f / Mathf.Max(1f, captureRate);
            captureAccumulator += Mathf.Max(0f, realDeltaTime);
            if (captureAccumulator < captureInterval)
            {
                return false;
            }

            captureAccumulator %= captureInterval;
            return true;
        }

        public ReplayRecordingLimitReason EvaluateBudget(
            long estimatedBytes,
            float maximumSourceDuration,
            long memoryBudgetBytes)
        {
            return ReplayRecordingBudget.Evaluate(
                SourceElapsedTime,
                estimatedBytes,
                maximumSourceDuration,
                memoryBudgetBytes);
        }

        public void Reset()
        {
            clock.Reset();
            captureAccumulator = 0f;
        }
    }

    internal static class ReplayTimeline
    {
        public static int FindSegmentIndex<T>(
            IReadOnlyList<T> segments,
            float presentationTimestamp,
            Func<T, float> getPresentationEnd)
        {
            if (segments == null || segments.Count == 0)
            {
                return -1;
            }

            int low = 0;
            int high = segments.Count - 1;
            while (low < high)
            {
                int middle = low + ((high - low) / 2);
                if (getPresentationEnd(segments[middle]) <=
                    presentationTimestamp)
                {
                    low = middle + 1;
                }
                else
                {
                    high = middle;
                }
            }

            return low;
        }
    }

    internal readonly struct ReplayPlaybackStep
    {
        public ReplayPlaybackStep(bool shouldApply, float presentationTime)
        {
            ShouldApply = shouldApply;
            PresentationTime = presentationTime;
        }

        public bool ShouldApply { get; }
        public float PresentationTime { get; }
    }

    internal sealed class ReplayPlaybackSession
    {
        public float CurrentTime { get; private set; }
        public float HoldRemaining { get; private set; }

        public void Reset(float firstPresentationTime)
        {
            CurrentTime = firstPresentationTime;
            HoldRemaining = 0f;
        }

        public ReplayPlaybackStep Advance(
            float realDeltaTime,
            float firstPresentationTime,
            float lastPresentationTime,
            float endHoldDuration,
            bool loop)
        {
            if (HoldRemaining > 0f)
            {
                HoldRemaining = Mathf.Max(
                    0f,
                    HoldRemaining - Mathf.Max(0f, realDeltaTime));
                if (HoldRemaining > 0f || !loop)
                {
                    return new ReplayPlaybackStep(false, CurrentTime);
                }

                CurrentTime = firstPresentationTime;
            }
            else
            {
                CurrentTime += Mathf.Max(0f, realDeltaTime);
            }

            if (CurrentTime >= lastPresentationTime)
            {
                CurrentTime = lastPresentationTime;
                HoldRemaining = Mathf.Max(0f, endHoldDuration);
            }

            return new ReplayPlaybackStep(true, CurrentTime);
        }
    }

    internal static class ReplayAnimationRecorder
    {
        public static void RecordController(
            ReplayAnimationTrack track,
            RuntimeAnimatorController controller,
            float sourceTime,
            float replayTime)
        {
            track?.RecordController(controller, sourceTime, replayTime);
        }

        public static void RecordTrigger(
            ReplayAnimationTrack track,
            int parameterHash,
            bool set,
            float sourceTime,
            float replayTime)
        {
            track?.RecordTrigger(parameterHash, set, sourceTime, replayTime);
        }

        public static void RecordActive(
            ReplayAnimationTrack track,
            bool active,
            float sourceTime,
            float replayTime)
        {
            track?.RecordActive(active, sourceTime, replayTime);
        }
    }

    internal static class ReplayAnimationPlayer
    {
        public static void Prepare(
            IReadOnlyList<ReplayAnimationTrack> tracks,
            float replayTimeOrigin)
        {
            for (int index = 0; index < tracks.Count; index++)
            {
                tracks[index].PrepareForReplay(replayTimeOrigin);
            }
        }

        public static void HideSources(IReadOnlyList<ReplayAnimationTrack> tracks)
        {
            for (int index = 0; index < tracks.Count; index++)
            {
                tracks[index].HideSource();
            }
        }

        public static void Apply(
            IReadOnlyList<ReplayAnimationTrack> tracks,
            float presentationTime,
            float sourceTime)
        {
            for (int index = 0; index < tracks.Count; index++)
            {
                tracks[index].Apply(presentationTime, sourceTime);
            }
        }

        public static void Dispose(IReadOnlyList<ReplayAnimationTrack> tracks)
        {
            for (int index = 0; index < tracks.Count; index++)
            {
                tracks[index].Dispose();
            }
        }
    }
}
