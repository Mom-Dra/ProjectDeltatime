namespace Deltatime.Replay
{
    public enum ReplayRecordingLimitReason
    {
        None,
        SourceDuration,
        MemoryBudget
    }

    /// <summary>
    /// Allocation-free development diagnostics for in-memory replay data.
    /// Byte counts estimate managed payload storage and exclude
    /// Unity object/native asset memory shared by the visual proxy.
    /// </summary>
    public readonly struct ReplayMemoryStatistics
    {
        public long EstimatedBytes { get; }
        public int TrackedActorCount { get; }
        public int AnimationEventCount { get; }
        public int AnimationCheckpointCount { get; }
        public int AnimationTransformSampleCount { get; }
        public int VisualTransformSampleCount { get; }
        public int BonePoseCount { get; }
        public int CameraSampleCount { get; }
        public int TimingSampleCount { get; }
        public float SourceDuration { get; }
        public float ReplayDuration { get; }
        public bool RecordingLimitReached { get; }
        public ReplayRecordingLimitReason LimitReason { get; }

        public ReplayMemoryStatistics(
            long estimatedBytes,
            int trackedActorCount,
            int animationEventCount,
            int animationCheckpointCount,
            int animationTransformSampleCount,
            int visualTransformSampleCount,
            int cameraSampleCount,
            int timingSampleCount,
            float sourceDuration,
            float replayDuration,
            bool recordingLimitReached,
            ReplayRecordingLimitReason limitReason)
        {
            EstimatedBytes = estimatedBytes;
            TrackedActorCount = trackedActorCount;
            AnimationEventCount = animationEventCount;
            AnimationCheckpointCount = animationCheckpointCount;
            AnimationTransformSampleCount = animationTransformSampleCount;
            VisualTransformSampleCount = visualTransformSampleCount;
            BonePoseCount = 0;
            CameraSampleCount = cameraSampleCount;
            TimingSampleCount = timingSampleCount;
            SourceDuration = sourceDuration;
            ReplayDuration = replayDuration;
            RecordingLimitReached = recordingLimitReached;
            LimitReason = limitReason;
        }
    }

    public static class ReplayRecordingBudget
    {
        public static ReplayRecordingLimitReason Evaluate(
            float sourceDuration,
            long estimatedBytes,
            float maximumSourceDuration,
            long memoryBudgetBytes)
        {
            if (sourceDuration >= maximumSourceDuration)
            {
                return ReplayRecordingLimitReason.SourceDuration;
            }

            return estimatedBytes >= memoryBudgetBytes
                ? ReplayRecordingLimitReason.MemoryBudget
                : ReplayRecordingLimitReason.None;
        }
    }
}
