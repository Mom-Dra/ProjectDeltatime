using System;
using System.Collections.Generic;
using Deltatime.Enemies;
using Deltatime.InputSystem;
using Deltatime.Level;
using Deltatime.Player;
using Deltatime.TimeSystem;
using Deltatime.UI;
using Deltatime.Vision;
using Deltatime.Visuals;
using UnityEngine;
using UnityEngine.Rendering;

namespace Deltatime.Replay
{
    [DefaultExecutionOrder(10000)]
    public sealed class StageReplayController : MonoBehaviour
    {
        public static StageReplayController ActiveRecorder { get; private set; }

        public enum ReplayPlaybackPhase
        {
            Normal,
            Deadline,
            DeadlineAftermath
        }

        private const string BaseColorProperty = "_BaseColor";
        private const string ColorProperty = "_Color";

        [SerializeField] private WorldTimeController worldTime;
        [SerializeField] private Camera gameplayCamera;
        [SerializeField] private DeadlineController deadline;
        [SerializeField, Min(1f)] private float captureRate = 20f;
        [SerializeField, Min(0f)] private float endHoldDuration = 0.65f;
        [SerializeField] private bool loop = true;

        [Header("Recording Budget")]
        [Tooltip("Maximum wall-clock recording duration. Recording stops with an " +
                 "explicit warning; captured data remains replayable.")]
        [SerializeField, Min(1f)] private float maximumSourceRecordingDuration =
            300f;
        [Tooltip("Estimated replay payload budget in MiB. The final sample is " +
                 "kept and recording stops explicitly when the budget is reached.")]
        [SerializeField, Min(1)] private int memoryBudgetMegabytes = 64;
        [SerializeField, Min(0.25f)] private float animationCheckpointInterval =
            2f;

        [Header("Renderer Discovery")]
        [Tooltip("Optional dynamic gameplay roots captured every replay sample. " +
                 "When empty, replay uses an initial cache plus explicit runtime registration.")]
        [SerializeField] private Transform[] rendererDiscoveryRoots =
            Array.Empty<Transform>();
        [Tooltip("Full-scene fallback interval for transient renderers outside the " +
                 "configured dynamic roots. Zero disables repeated scans after the " +
                 "initial cache; spawned replay visuals should register explicitly.")]
        [SerializeField, Min(0f)] private float fallbackRendererDiscoveryInterval =
            1f;

        [Header("Deadline Replay Camera")]
        // Compatibility: existing scenes serialize these legacy pacing values.
        // They remain readable so scene/prefab data and editor tooling
        // do not break, but normalized replay timing intentionally ignores
        // them. Camera lock/recovery settings below are still active.
        [SerializeField, Range(0.05f, 1f)]
        private float deadlineCinematicPlaybackRate = 0.5f;
        [SerializeField, Min(0f)]
        private float minimumDeadlineCinematicDuration = 0.8f;
        [SerializeField, Min(0f)]
        private float maximumDeadlineCinematicDuration = 2f;
        [SerializeField, Min(0f)]
        private float deadlineAftermathWorldDuration = 0.75f;
        [SerializeField, Range(0.05f, 1f)]
        private float deadlineAftermathPlaybackRate = 0.5f;
        [SerializeField, Min(0f)]
        private float deadlineCameraRecoveryDuration = 0.2f;

        [Header("Omniscient Replay View")]
        [SerializeField] private Color omniscientAmbientSkyColor =
            new Color(0.30f, 0.34f, 0.40f, 1f);
        [SerializeField] private Color omniscientAmbientEquatorColor =
            new Color(0.22f, 0.25f, 0.30f, 1f);
        [SerializeField] private Color omniscientAmbientGroundColor =
            new Color(0.12f, 0.14f, 0.17f, 1f);
        [SerializeField, Min(0f)] private float omniscientAmbientIntensity = 1f;
        [SerializeField, Min(0f)] private float omniscientReflectionIntensity =
            0.35f;
        [SerializeField] private Color omniscientBackgroundColor =
            new Color(0.025f, 0.04f, 0.065f, 1f);
        [SerializeField] private Color omniscientFillLightColor =
            new Color(0.78f, 0.86f, 1f, 1f);
        [SerializeField, Min(0f)] private float omniscientFillLightIntensity =
            0.65f;
        [SerializeField] private Vector3 omniscientFillLightRotation =
            new Vector3(50f, -30f, 0f);

        private readonly List<CameraSample> cameraSamples =
            new List<CameraSample>(2048);
        private readonly List<ReplayTimingSample> timingSamples =
            new List<ReplayTimingSample>(2048);
        private readonly List<ReplaySegment> replaySegments =
            new List<ReplaySegment>(128);
        private readonly Dictionary<int, VisualTrack> tracksByInstanceId =
            new Dictionary<int, VisualTrack>();
        private readonly List<VisualTrack> tracks = new List<VisualTrack>();
        private readonly Dictionary<int, ReplayAnimationTrack>
            animationTracksByInstanceId =
                new Dictionary<int, ReplayAnimationTrack>();
        private readonly List<ReplayAnimationTrack> animationTracks =
            new List<ReplayAnimationTrack>();
        private readonly HashSet<int> animationRendererIds = new HashSet<int>();
        private readonly Dictionary<int, LightTrack> lightTracksByInstanceId =
            new Dictionary<int, LightTrack>();
        private readonly List<LightTrack> lightTracks = new List<LightTrack>();
        private readonly HashSet<int> visibleRendererIds = new HashSet<int>();
        private readonly HashSet<int> explicitlyRegisteredRendererIds =
            new HashSet<int>();
        private readonly List<Renderer> rendererCandidates = new List<Renderer>();
        private readonly HashSet<int> rendererCandidateIds = new HashSet<int>();
        private readonly List<Renderer> fallbackRendererCandidates =
            new List<Renderer>();
        private readonly List<Renderer> immediateRegistrationBuffer =
            new List<Renderer>(8);
        private readonly List<Renderer> dynamicRendererBuffer =
            new List<Renderer>(64);
        private readonly List<MonoBehaviour> disabledBehaviours =
            new List<MonoBehaviour>();

        private Transform replayRoot;
        private Light omniscientFillLight;
        private ReplayLightingSnapshot replayLightingSnapshot;
        private ReplayRecordingClock recordingClock;
        private bool hasReplayLightingSnapshot;
        private bool replayRequested;
        private float firstPresentationTime;
        private float lastPresentationTime;
        private float playbackTime;
        private float holdRemaining;
        private float captureAccumulator;
        private bool hasCapturedDeadlineState;
        private bool lastCapturedDeadlineState;
        private float nextFallbackRendererDiscoveryTime;
        private bool fallbackRendererDiscoveryInitialized;
        private bool recordingLimitReached;
        private ReplayRecordingLimitReason recordingLimitReason;

        public bool IsReplaying { get; private set; }
        public bool IsOmniscientViewEnabled { get; private set; }
        public ReplayPlaybackPhase CurrentPlaybackPhase { get; private set; }
        public bool IsReplayCameraLocked { get; private set; }
        public float CurrentCameraRecoveryBlend { get; private set; }
        public int DeadlineCinematicSegmentCount { get; private set; }
        public float ShortestDeadlineCinematicDuration { get; private set; }
        public float LongestDeadlineCinematicDuration { get; private set; }
        public float LongestDeadlineAftermathDuration { get; private set; }
        public float CaptureRate => captureRate;
        public int CapturedFrameCount => cameraSamples.Count;
        public bool UsesOptimizedRendererDiscovery =>
            rendererDiscoveryRoots != null && rendererDiscoveryRoots.Length > 0;
        public int RendererDiscoveryRootCount
        {
            get
            {
                if (rendererDiscoveryRoots == null)
                {
                    return 0;
                }

                int count = 0;
                for (int i = 0; i < rendererDiscoveryRoots.Length; i++)
                {
                    if (rendererDiscoveryRoots[i] != null)
                    {
                        count++;
                    }
                }

                return count;
            }
        }
        public float FallbackRendererDiscoveryInterval =>
            fallbackRendererDiscoveryInterval;

        public bool HasRendererDiscoveryRoot(Transform root)
        {
            if (root == null || rendererDiscoveryRoots == null)
            {
                return false;
            }

            for (int i = 0; i < rendererDiscoveryRoots.Length; i++)
            {
                if (rendererDiscoveryRoots[i] == root)
                {
                    return true;
                }
            }

            return false;
        }
        public int TrackedReplayVisionConeCount
        {
            get
            {
                int count = 0;
                for (int i = 0; i < tracks.Count; i++)
                {
                    if (tracks[i].IsVisionCone)
                    {
                        count++;
                    }
                }

                return count;
            }
        }
        public bool IsReplayVisionConeVisible
        {
            get
            {
                for (int i = 0; i < tracks.Count; i++)
                {
                    if (tracks[i].IsVisionCone && tracks[i].IsProxyActive)
                    {
                        return true;
                    }
                }

                return false;
            }
        }
        public int ActiveOmniscientEnemyVisualCount
        {
            get
            {
                int count = 0;
                for (int i = 0; i < tracks.Count; i++)
                {
                    if (tracks[i].SupportsOmniscientVisibility &&
                        tracks[i].IsProxyActive)
                    {
                        count++;
                    }
                }

                return count;
            }
        }
        public bool IsOmniscientFillLightActive =>
            omniscientFillLight != null &&
            omniscientFillLight.enabled &&
            omniscientFillLight.gameObject.activeInHierarchy;
        public int TrackedLightCount => lightTracks.Count;
        public int TrackedVisualCount => tracks.Count;
        public int TrackedAnimatedVisualCount => animationTracks.Count;
        // Compatibility diagnostic retained for existing tooling. Bone-pose
        // recording was removed; this value is now guaranteed to remain zero.
        public int RecordedAnimatedPoseCount => 0;
        public bool HasRecordedAnimatedMotion
        {
            get
            {
                for (int i = 0; i < animationTracks.Count; i++)
                {
                    if (animationTracks[i].HasAdvancedState ||
                        animationTracks[i].EventCount > 0)
                    {
                        return true;
                    }
                }

                return false;
            }
        }
        public int ActiveAnimatedReplayVisualCount
        {
            get
            {
                int count = 0;
                for (int i = 0; i < animationTracks.Count; i++)
                {
                    if (animationTracks[i].IsProxyActive)
                    {
                        count++;
                    }
                }

                return count;
            }
        }
        public int TrackedExcludedVisualCount
        {
            get
            {
                int count = 0;
                for (int i = 0; i < tracks.Count; i++)
                {
                    if (tracks[i].IsReplayExcluded)
                    {
                        count++;
                    }
                }

                return count;
            }
        }
        public int ActiveReplayLightCount
        {
            get
            {
                int count = 0;
                for (int i = 0; i < lightTracks.Count; i++)
                {
                    if (lightTracks[i].IsProxyActive)
                    {
                        count++;
                    }
                }

                return count;
            }
        }
        public bool AreTrackedSourceLightsDisabled
        {
            get
            {
                for (int i = 0; i < lightTracks.Count; i++)
                {
                    if (lightTracks[i].IsSourceEnabled)
                    {
                        return false;
                    }
                }

                return true;
            }
        }
        public float RecordedDuration =>
            replaySegments.Count == 0
                ? 0f
                : Mathf.Max(0f, lastPresentationTime - firstPresentationTime);
        public float SourceRecordedDuration => timingSamples.Count < 2
            ? 0f
            : Mathf.Max(
                0f,
                timingSamples[timingSamples.Count - 1].SourceTimestamp -
                timingSamples[0].SourceTimestamp);
        public float PlaybackElapsed =>
            IsReplaying
                ? Mathf.Max(0f, playbackTime - firstPresentationTime)
                : 0f;
        public float CurrentSourceTimestamp { get; private set; }
        public bool IsRecording => enabled && !IsReplaying &&
                                   !recordingLimitReached;
        public bool RecordingLimitReached => recordingLimitReached;
        public ReplayRecordingLimitReason RecordingLimitReason =>
            recordingLimitReason;
        public float MaximumSourceRecordingDuration =>
            maximumSourceRecordingDuration;
        public long MemoryBudgetBytes => memoryBudgetMegabytes * 1024L * 1024L;
        public int RecordedAnimationEventCount
        {
            get
            {
                int count = 0;
                for (int i = 0; i < animationTracks.Count; i++)
                {
                    count += animationTracks[i].EventCount;
                }

                return count;
            }
        }
        public int RecordedAnimationCheckpointCount
        {
            get
            {
                int count = 0;
                for (int i = 0; i < animationTracks.Count; i++)
                {
                    count += animationTracks[i].CheckpointCount;
                }

                return count;
            }
        }
        public int RecordedAnimationControllerChangeCount
        {
            get
            {
                int count = 0;
                for (int i = 0; i < animationTracks.Count; i++)
                {
                    count += animationTracks[i].ControllerChangeCount;
                }

                return count;
            }
        }
        public Animator FirstReplayProxyAnimator => animationTracks.Count == 0
            ? null
            : animationTracks[0].ProxyAnimator;
        public bool HasAdvancedReplayProxyAnimator
        {
            get
            {
                for (int i = 0; i < animationTracks.Count; i++)
                {
                    if (animationTracks[i].HasAdvancedState)
                    {
                        return true;
                    }
                }

                return false;
            }
        }
        public bool HasReplayProxyStateTransition
        {
            get
            {
                for (int i = 0; i < animationTracks.Count; i++)
                {
                    if (animationTracks[i].HasObservedTransition ||
                        animationTracks[i].HasObservedStateChange)
                    {
                        return true;
                    }
                }

                return false;
            }
        }
        public bool AreReplayProxyGameplayComponentsDisabled
        {
            get
            {
                for (int i = 0; i < animationTracks.Count; i++)
                {
                    if (animationTracks[i].HasEnabledGameplayComponents)
                    {
                        return false;
                    }
                }

                return true;
            }
        }

        /// <summary>
        /// Configures an opt-in hybrid renderer discovery path.  Gameplay roots are
        /// inspected at the normal replay sample rate, while transient objects outside
        /// them are found by a lower-frequency fallback scan. Empty roots use an
        /// initial cache; a zero fallback interval then relies on explicit registration.
        /// </summary>
        public void ConfigureRendererDiscovery(
            Transform[] dynamicRoots,
            float fallbackInterval)
        {
            rendererDiscoveryRoots = dynamicRoots ?? Array.Empty<Transform>();
            fallbackRendererDiscoveryInterval = Mathf.Max(0f, fallbackInterval);
            ResetRendererDiscoveryCache();
        }

        public void ConfigureRecordingBudget(
            float maximumSourceDuration,
            int budgetMegabytes)
        {
            if (cameraSamples.Count > 0 || timingSamples.Count > 0)
            {
                Debug.LogWarning(
                    "Replay recording budget can only be changed before capture starts.",
                    this);
                return;
            }

            maximumSourceRecordingDuration = Mathf.Max(
                1f,
                maximumSourceDuration);
            memoryBudgetMegabytes = Mathf.Max(1, budgetMegabytes);
        }

        public ReplayMemoryStatistics GetMemoryStatistics()
        {
            long estimatedBytes = cameraSamples.Count * 52L +
                                  timingSamples.Count * 20L +
                                  replaySegments.Count * 96L;
            int animationEvents = 0;
            int animationCheckpoints = 0;
            int animationTransforms = 0;
            for (int i = 0; i < animationTracks.Count; i++)
            {
                ReplayAnimationTrack track = animationTracks[i];
                estimatedBytes += track.EstimatedBytes;
                animationEvents += track.EventCount;
                animationCheckpoints += track.CheckpointCount;
                animationTransforms += track.TransformSampleCount;
            }

            int visualSamples = 0;
            for (int i = 0; i < tracks.Count; i++)
            {
                estimatedBytes += tracks[i].EstimatedBytes;
                visualSamples += tracks[i].SampleCount;
            }

            for (int i = 0; i < lightTracks.Count; i++)
            {
                estimatedBytes += lightTracks[i].EstimatedBytes;
            }

            return new ReplayMemoryStatistics(
                estimatedBytes,
                animationTracks.Count,
                animationEvents,
                animationCheckpoints,
                animationTransforms,
                visualSamples,
                cameraSamples.Count,
                timingSamples.Count,
                recordingClock.SourceElapsedTime,
                recordingClock.ReplayElapsedTime,
                recordingLimitReached,
                recordingLimitReason);
        }

        public bool HasRecordedAnimationTrigger(int parameterHash)
        {
            for (int i = 0; i < animationTracks.Count; i++)
            {
                if (animationTracks[i].HasTriggerEvent(parameterHash))
                {
                    return true;
                }
            }

            return false;
        }

        public bool RegisterAnimationSource(
            CharacterAnimationController source)
        {
            if (!enabled || IsReplaying || recordingLimitReached ||
                source == null || source.Animator == null ||
                source.VisualRoot == null)
            {
                return false;
            }

            int instanceId = source.GetInstanceID();
            if (animationTracksByInstanceId.ContainsKey(instanceId))
            {
                return false;
            }

            EnsureReplayRoot();
            ReplayAnimationTrack track = new ReplayAnimationTrack(
                source,
                replayRoot,
                animationCheckpointInterval);
            if (!track.IsValid)
            {
                track.Dispose();
                Debug.LogWarning(
                    $"Replay skipped animated visual '{source.name}' because its " +
                    "Animator/visual hierarchy could not be cloned safely.",
                    source);
                return false;
            }

            animationTracksByInstanceId.Add(instanceId, track);
            animationTracks.Add(track);
            Renderer[] renderers =
                source.VisualRoot.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] is SkinnedMeshRenderer)
                {
                    animationRendererIds.Add(renderers[i].GetInstanceID());
                }
            }

            track.Capture(
                recordingClock.SourceElapsedTime,
                recordingClock.ReplayElapsedTime,
                true);
            return true;
        }

        public void RecordAnimatorController(
            CharacterAnimationController source,
            RuntimeAnimatorController controller)
        {
            if (!TryGetAnimationTrack(source, out ReplayAnimationTrack track))
            {
                return;
            }

            track.RecordController(
                controller,
                recordingClock.SourceElapsedTime,
                recordingClock.ReplayElapsedTime);
        }

        public void RecordAnimatorTrigger(
            CharacterAnimationController source,
            int parameterHash,
            bool set)
        {
            if (!TryGetAnimationTrack(source, out ReplayAnimationTrack track))
            {
                return;
            }

            track.RecordTrigger(
                parameterHash,
                set,
                recordingClock.SourceElapsedTime,
                recordingClock.ReplayElapsedTime);
        }

        public void RecordAnimatorActive(
            CharacterAnimationController source,
            bool active)
        {
            if (!TryGetAnimationTrack(source, out ReplayAnimationTrack track))
            {
                return;
            }

            track.RecordActive(
                active,
                recordingClock.SourceElapsedTime,
                recordingClock.ReplayElapsedTime);
        }

        private bool TryGetAnimationTrack(
            CharacterAnimationController source,
            out ReplayAnimationTrack track)
        {
            track = null;
            if (source == null || IsReplaying || recordingLimitReached)
            {
                return false;
            }

            int instanceId = source.GetInstanceID();
            if (animationTracksByInstanceId.TryGetValue(instanceId, out track))
            {
                return true;
            }

            RegisterAnimationSource(source);
            return animationTracksByInstanceId.TryGetValue(instanceId, out track);
        }

        private void Awake()
        {
            EnsureReplayRoot();

            if (worldTime == null || gameplayCamera == null || deadline == null)
            {
                Debug.LogError(
                    $"{nameof(StageReplayController)} requires world time, a gameplay camera, and Deadline.",
                    this);
                enabled = false;
                return;
            }

            ActiveRecorder = this;
        }

        private void EnsureReplayRoot()
        {
            if (replayRoot != null)
            {
                return;
            }

            GameObject root = new GameObject("Replay Visuals");
            replayRoot = root.transform;
            replayRoot.SetParent(transform, false);
            EnsureOmniscientFillLight();
            replayRoot.gameObject.SetActive(false);
        }

        private void EnsureOmniscientFillLight()
        {
            if (omniscientFillLight != null)
            {
                return;
            }

            GameObject lightObject = new GameObject("Replay Omniscient Fill Light");
            lightObject.transform.SetParent(replayRoot, false);
            omniscientFillLight = lightObject.AddComponent<Light>();
            omniscientFillLight.type = LightType.Directional;
            omniscientFillLight.shadows = LightShadows.None;
            omniscientFillLight.renderMode = LightRenderMode.Auto;
            omniscientFillLight.cullingMask = ~0;
            omniscientFillLight.enabled = false;
            ApplyOmniscientFillLightSettings();
        }

        private void Start()
        {
            if (enabled)
            {
                DiscoverInitialAnimationSources();
                CaptureFrame(true);
            }
        }

        private void DiscoverInitialAnimationSources()
        {
            CharacterAnimationController[] controllers =
                FindObjectsByType<CharacterAnimationController>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None);
            for (int i = 0; i < controllers.Length; i++)
            {
                RegisterAnimationSource(controllers[i]);
            }
        }

        private void LateUpdate()
        {
            float realDeltaTime = UnityEngine.Time.unscaledDeltaTime;
            if (IsReplaying)
            {
                AdvanceReplay(realDeltaTime);
                return;
            }

            recordingClock.Advance(realDeltaTime, worldTime.WorldDeltaTime);
            if (recordingLimitReached)
            {
                if (replayRequested)
                {
                    BeginReplay();
                }

                return;
            }

            // Replay display time is a third clock: source real time records
            // when a pose happened, world time records simulation progress,
            // and this zero-based clock records how long the same progress
            // takes at normal (1x) world speed.  Capturing the accumulated
            // delta avoids reconstructing variable slow motion from a guessed
            // or hard-coded multiplier during playback.
            float captureInterval = 1f / Mathf.Max(1f, captureRate);
            captureAccumulator += realDeltaTime;
            bool captureDue = captureAccumulator >= captureInterval;
            bool deadlineStateChanged = HasDeadlineStateChanged();

            if (captureDue)
            {
                captureAccumulator %= captureInterval;
            }

            if (captureDue || deadlineStateChanged)
            {
                CaptureFrame(deadlineStateChanged);
                CheckRecordingBudget();
            }

            if (!recordingLimitReached &&
                recordingClock.SourceElapsedTime >=
                maximumSourceRecordingDuration)
            {
                if (!captureDue && !deadlineStateChanged)
                {
                    CaptureFrame(false);
                }

                StopRecording(ReplayRecordingLimitReason.SourceDuration);
            }

            if (replayRequested)
            {
                if (!recordingLimitReached &&
                    !captureDue && !deadlineStateChanged)
                {
                    CaptureFrame(false);
                }

                BeginReplay();
            }
        }

        private void CheckRecordingBudget()
        {
            if (recordingLimitReached)
            {
                return;
            }

            ReplayMemoryStatistics statistics = GetMemoryStatistics();
            ReplayRecordingLimitReason reason = ReplayRecordingBudget.Evaluate(
                statistics.SourceDuration,
                statistics.EstimatedBytes,
                maximumSourceRecordingDuration,
                MemoryBudgetBytes);
            if (reason != ReplayRecordingLimitReason.None)
            {
                StopRecording(reason);
            }
        }

        private void StopRecording(ReplayRecordingLimitReason reason)
        {
            if (recordingLimitReached)
            {
                return;
            }

            recordingLimitReached = true;
            recordingLimitReason = reason;
            ReplayMemoryStatistics statistics = GetMemoryStatistics();
            Debug.LogWarning(
                $"Replay recording stopped explicitly ({reason}). Captured data " +
                $"remains available: source={statistics.SourceDuration:0.0}s, " +
                $"replay={statistics.ReplayDuration:0.0}s, estimated=" +
                $"{statistics.EstimatedBytes / (1024f * 1024f):0.00} MiB. " +
                "The recording was not silently trimmed.",
                this);
        }

        public void Configure(
            WorldTimeController timeSource,
            Camera targetCamera,
            DeadlineController deadlineController)
        {
            worldTime = timeSource;
            gameplayCamera = targetCamera;
            deadline = deadlineController;
        }

        public bool RequestReplay()
        {
            if (!enabled || IsReplaying || replayRequested)
            {
                return false;
            }

            replayRequested = true;
            return true;
        }

        public bool SetOmniscientView(bool enabledState)
        {
            if (!IsReplaying || IsOmniscientViewEnabled == enabledState)
            {
                return false;
            }

            IsOmniscientViewEnabled = enabledState;
            if (enabledState)
            {
                SaveReplayLightingState();
                ApplyOmniscientLighting();
            }
            else
            {
                RestoreReplayLightingState();
            }

            ApplyReplay(playbackTime);
            return true;
        }

        public bool RegisterLight(Light source)
        {
            if (source == null)
            {
                return false;
            }

            int instanceId = source.GetInstanceID();
            if (lightTracksByInstanceId.ContainsKey(instanceId))
            {
                return false;
            }

            EnsureReplayRoot();
            LightTrack track = new LightTrack(
                source,
                CreateProxyLight(source, replayRoot));
            lightTracksByInstanceId.Add(instanceId, track);
            lightTracks.Add(track);
            return true;
        }

        /// <summary>
        /// Immediately registers a newly spawned short-lived renderer. This is
        /// the allocation-free event path for VFX that may exist for less than
        /// one renderer-discovery interval. Periodic discovery remains the
        /// compatibility path for ordinary gameplay objects.
        /// </summary>
        public bool RegisterRenderer(Renderer source)
        {
            if (!enabled || IsReplaying || !IsTrackableRenderer(source))
            {
                return false;
            }

            int instanceId = source.GetInstanceID();
            if (tracksByInstanceId.ContainsKey(instanceId))
            {
                return false;
            }

            VisualTrack track = CreateTrack(source);
            if (track == null)
            {
                return false;
            }

            tracksByInstanceId.Add(instanceId, track);
            tracks.Add(track);
            explicitlyRegisteredRendererIds.Add(instanceId);
            track.Capture(recordingClock.SourceElapsedTime, true);
            return true;
        }

        /// <summary>
        /// Registers every replayable renderer under a newly spawned gameplay
        /// object. The reusable result buffer keeps this event-driven path free
        /// of managed allocations and avoids relying on the periodic discovery
        /// scan for very short projectile or thrown-weapon lifetimes.
        /// </summary>
        public int RegisterRendererHierarchy(Transform sourceRoot)
        {
            if (!enabled || IsReplaying || sourceRoot == null)
            {
                return 0;
            }

            immediateRegistrationBuffer.Clear();
            sourceRoot.GetComponentsInChildren(
                true,
                immediateRegistrationBuffer);

            int registeredCount = 0;
            for (int i = 0; i < immediateRegistrationBuffer.Count; i++)
            {
                if (RegisterRenderer(immediateRegistrationBuffer[i]))
                {
                    registeredCount++;
                }
            }

            immediateRegistrationBuffer.Clear();
            return registeredCount;
        }

        private void OnValidate()
        {
            captureRate = Mathf.Max(1f, captureRate);
            endHoldDuration = Mathf.Max(0f, endHoldDuration);
            maximumSourceRecordingDuration = Mathf.Max(
                1f,
                maximumSourceRecordingDuration);
            memoryBudgetMegabytes = Mathf.Max(1, memoryBudgetMegabytes);
            animationCheckpointInterval = Mathf.Max(
                0.25f,
                animationCheckpointInterval);
            deadlineCinematicPlaybackRate = Mathf.Clamp(
                deadlineCinematicPlaybackRate,
                0.05f,
                1f);
            minimumDeadlineCinematicDuration = Mathf.Max(
                0f,
                minimumDeadlineCinematicDuration);
            maximumDeadlineCinematicDuration = Mathf.Max(
                minimumDeadlineCinematicDuration,
                maximumDeadlineCinematicDuration);
            deadlineAftermathWorldDuration = Mathf.Max(
                0f,
                deadlineAftermathWorldDuration);
            deadlineAftermathPlaybackRate = Mathf.Clamp(
                deadlineAftermathPlaybackRate,
                0.05f,
                1f);
            deadlineCameraRecoveryDuration = Mathf.Max(
                0f,
                deadlineCameraRecoveryDuration);
            omniscientAmbientIntensity =
                Mathf.Max(0f, omniscientAmbientIntensity);
            omniscientReflectionIntensity =
                Mathf.Max(0f, omniscientReflectionIntensity);
            omniscientFillLightIntensity =
                Mathf.Max(0f, omniscientFillLightIntensity);

            if (omniscientFillLight != null)
            {
                ApplyOmniscientFillLightSettings();
            }
        }

        private bool HasDeadlineStateChanged()
        {
            bool deadlineActive = deadline != null && deadline.IsActive;
            return !hasCapturedDeadlineState ||
                   deadlineActive != lastCapturedDeadlineState;
        }

        private void CaptureFrame(bool forceKeyframe)
        {
            float timestamp = recordingClock.SourceElapsedTime;
            bool deadlineActive = deadline != null && deadline.IsActive;
            hasCapturedDeadlineState = true;
            lastCapturedDeadlineState = deadlineActive;

            cameraSamples.Add(new CameraSample(
                timestamp,
                gameplayCamera.transform.position,
                gameplayCamera.transform.rotation,
                gameplayCamera.backgroundColor,
                gameplayCamera.fieldOfView));
            timingSamples.Add(new ReplayTimingSample(
                timestamp,
                worldTime.WorldElapsedTime,
                recordingClock.ReplayElapsedTime,
                deadlineActive));

            for (int i = 0; i < animationTracks.Count; i++)
            {
                animationTracks[i].Capture(
                    timestamp,
                    recordingClock.ReplayElapsedTime,
                    forceKeyframe);
            }

            CollectRendererCandidates();
            visibleRendererIds.Clear();

            for (int i = 0; i < rendererCandidates.Count; i++)
            {
                Renderer source = rendererCandidates[i];

                int instanceId = source.GetInstanceID();
                visibleRendererIds.Add(instanceId);

                if (!tracksByInstanceId.TryGetValue(instanceId, out VisualTrack track))
                {
                    track = CreateTrack(source);
                    if (track == null)
                    {
                        continue;
                    }

                    tracksByInstanceId.Add(instanceId, track);
                    tracks.Add(track);
                }

                track.Capture(timestamp, forceKeyframe);
            }

            for (int i = 0; i < tracks.Count; i++)
            {
                VisualTrack track = tracks[i];
                if (!visibleRendererIds.Contains(track.InstanceId))
                {
                    if (explicitlyRegisteredRendererIds.Contains(
                            track.InstanceId))
                    {
                        track.Capture(timestamp, forceKeyframe);
                    }
                    else
                    {
                        track.CaptureHidden(timestamp, forceKeyframe);
                    }
                }
            }

            for (int i = 0; i < lightTracks.Count; i++)
            {
                lightTracks[i].Capture(timestamp, forceKeyframe);
            }
        }

        private void CollectRendererCandidates()
        {
            rendererCandidates.Clear();
            rendererCandidateIds.Clear();

            if (!UsesOptimizedRendererDiscovery)
            {
                if (!fallbackRendererDiscoveryInitialized ||
                    (fallbackRendererDiscoveryInterval > 0f &&
                     recordingClock.SourceElapsedTime >=
                     nextFallbackRendererDiscoveryTime))
                {
                    RefreshFallbackRendererCandidates();
                }

                for (int i = fallbackRendererCandidates.Count - 1;
                     i >= 0;
                     i--)
                {
                    Renderer candidate = fallbackRendererCandidates[i];
                    if (candidate == null)
                    {
                        fallbackRendererCandidates.RemoveAt(i);
                        continue;
                    }

                    AddRendererCandidate(candidate);
                }

                return;
            }

            CollectDynamicRootRenderers();
            if (!fallbackRendererDiscoveryInitialized ||
                (fallbackRendererDiscoveryInterval > 0f &&
                 recordingClock.SourceElapsedTime >=
                 nextFallbackRendererDiscoveryTime))
            {
                RefreshFallbackRendererCandidates();
            }

            for (int i = fallbackRendererCandidates.Count - 1; i >= 0; i--)
            {
                Renderer candidate = fallbackRendererCandidates[i];
                if (candidate == null)
                {
                    fallbackRendererCandidates.RemoveAt(i);
                    continue;
                }

                AddRendererCandidate(candidate);
            }
        }

        private void CollectDynamicRootRenderers()
        {
            for (int i = 0; i < rendererDiscoveryRoots.Length; i++)
            {
                Transform root = rendererDiscoveryRoots[i];
                if (root == null || !root.gameObject.activeInHierarchy)
                {
                    continue;
                }

                dynamicRendererBuffer.Clear();
                root.GetComponentsInChildren(true, dynamicRendererBuffer);
                for (int j = 0; j < dynamicRendererBuffer.Count; j++)
                {
                    AddRendererCandidate(dynamicRendererBuffer[j]);
                }
            }

            dynamicRendererBuffer.Clear();
        }

        private void RefreshFallbackRendererCandidates()
        {
            fallbackRendererCandidates.Clear();
            Renderer[] renderers = FindObjectsByType<Renderer>(
                FindObjectsInactive.Exclude,
                FindObjectsSortMode.None);
            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer candidate = renderers[i];
                if (IsUnderRendererDiscoveryRoot(candidate) ||
                    !IsTrackableRenderer(candidate))
                {
                    continue;
                }

                fallbackRendererCandidates.Add(candidate);
            }

            fallbackRendererDiscoveryInitialized = true;
            nextFallbackRendererDiscoveryTime =
                recordingClock.SourceElapsedTime +
                fallbackRendererDiscoveryInterval;
        }

        private bool IsUnderRendererDiscoveryRoot(Renderer candidate)
        {
            if (candidate == null || rendererDiscoveryRoots == null)
            {
                return false;
            }

            Transform sourceTransform = candidate.transform;
            for (int i = 0; i < rendererDiscoveryRoots.Length; i++)
            {
                Transform root = rendererDiscoveryRoots[i];
                if (root != null && sourceTransform.IsChildOf(root))
                {
                    return true;
                }
            }

            return false;
        }

        private void AddRendererCandidate(Renderer candidate)
        {
            if (!IsTrackableRenderer(candidate))
            {
                return;
            }

            int instanceId = candidate.GetInstanceID();
            if (rendererCandidateIds.Add(instanceId))
            {
                rendererCandidates.Add(candidate);
            }
        }

        private bool IsTrackableRenderer(Renderer candidate)
        {
            return candidate != null &&
                   !candidate.transform.IsChildOf(replayRoot) &&
                   !animationRendererIds.Contains(candidate.GetInstanceID()) &&
                   candidate.GetComponentInParent<ReplayExcluded>() == null &&
                   CanRecord(candidate);
        }

        private void ResetRendererDiscoveryCache()
        {
            fallbackRendererCandidates.Clear();
            rendererCandidates.Clear();
            rendererCandidateIds.Clear();
            fallbackRendererDiscoveryInitialized = false;
            nextFallbackRendererDiscoveryTime = 0f;
        }

        private void BuildPresentationTimeline()
        {
            replaySegments.Clear();
            firstPresentationTime = 0f;
            lastPresentationTime = 0f;
            CurrentPlaybackPhase = ReplayPlaybackPhase.Normal;
            IsReplayCameraLocked = false;
            CurrentCameraRecoveryBlend = 1f;
            DeadlineCinematicSegmentCount = 0;
            ShortestDeadlineCinematicDuration = 0f;
            LongestDeadlineCinematicDuration = 0f;
            LongestDeadlineAftermathDuration = 0f;

            if (timingSamples.Count < 2 ||
                cameraSamples.Count != timingSamples.Count)
            {
                return;
            }

            bool deadlineOpen = false;
            float deadlineDuration = 0f;
            float aftermathEndReplayTime = -1f;
            float aftermathDuration = 0f;
            float aftermathPresentationStart = -1f;
            CameraSample deadlineCameraAnchor = default;

            // One segment per captured interval is intentional.  A single
            // segment for an entire run only preserves the average time scale;
            // these piecewise mappings preserve the local ordering and timing
            // when WorldTimeController changes scale between samples.
            for (int i = 0; i < timingSamples.Count - 1; i++)
            {
                ReplayTimingSample current = timingSamples[i];
                ReplayTimingSample next = timingSamples[i + 1];

                if (current.DeadlineActive && !deadlineOpen)
                {
                    CompleteAftermathMetrics(aftermathDuration);
                    aftermathDuration = 0f;
                    aftermathEndReplayTime = -1f;
                    aftermathPresentationStart = -1f;
                    deadlineOpen = true;
                    deadlineDuration = 0f;
                    deadlineCameraAnchor = cameraSamples[i];
                    DeadlineCinematicSegmentCount++;
                }
                else if (!current.DeadlineActive && deadlineOpen)
                {
                    CompleteDeadlineMetrics(deadlineDuration);
                    deadlineOpen = false;
                    aftermathDuration = 0f;
                    aftermathEndReplayTime =
                        current.ReplayTimestamp +
                        deadlineAftermathWorldDuration;
                    // Recovery spans the whole aftermath. Starting it from
                    // every 20 Hz segment boundary makes the camera jump back
                    // toward the Deadline anchor and produces visible shake.
                    aftermathPresentationStart = lastPresentationTime;
                }

                float replayDuration = Mathf.Max(
                    0f,
                    next.ReplayTimestamp - current.ReplayTimestamp);
                if (replayDuration <= 0.000001f)
                {
                    // A true hard freeze has no normal-speed simulation
                    // duration.  Its source samples remain ordered, and the
                    // next positive interval jumps across them atomically.
                    continue;
                }

                if (current.DeadlineActive)
                {
                    AppendReplaySegment(
                        current.SourceTimestamp,
                        next.SourceTimestamp,
                        replayDuration,
                        ReplayPlaybackPhase.Deadline,
                        deadlineCameraAnchor,
                        0f,
                        0f);
                    deadlineDuration += replayDuration;
                    continue;
                }

                float replayIntervalEnd = next.ReplayTimestamp;
                if (aftermathEndReplayTime > current.ReplayTimestamp)
                {
                    float aftermathReplayEnd = Mathf.Min(
                        replayIntervalEnd,
                        aftermathEndReplayTime);
                    float aftermathPart = Mathf.Max(
                        0f,
                        aftermathReplayEnd - current.ReplayTimestamp);
                    if (aftermathPart > 0.000001f)
                    {
                        float sourceSplit = Mathf.Lerp(
                            current.SourceTimestamp,
                            next.SourceTimestamp,
                            aftermathPart / replayDuration);
                        AppendReplaySegment(
                            current.SourceTimestamp,
                            sourceSplit,
                            aftermathPart,
                            ReplayPlaybackPhase.DeadlineAftermath,
                            deadlineCameraAnchor,
                            aftermathPresentationStart,
                            deadlineCameraRecoveryDuration);
                        aftermathDuration += aftermathPart;

                        float normalPart = replayDuration - aftermathPart;
                        if (normalPart > 0.000001f)
                        {
                            AppendReplaySegment(
                                sourceSplit,
                                next.SourceTimestamp,
                                normalPart,
                                ReplayPlaybackPhase.Normal,
                                default,
                                0f,
                                0f);
                            CompleteAftermathMetrics(aftermathDuration);
                            aftermathDuration = 0f;
                            aftermathEndReplayTime = -1f;
                            aftermathPresentationStart = -1f;
                        }

                        continue;
                    }
                }

                CompleteAftermathMetrics(aftermathDuration);
                aftermathDuration = 0f;
                aftermathEndReplayTime = -1f;
                aftermathPresentationStart = -1f;
                AppendReplaySegment(
                    current.SourceTimestamp,
                    next.SourceTimestamp,
                    replayDuration,
                    ReplayPlaybackPhase.Normal,
                    default,
                    0f,
                    0f);
            }

            if (deadlineOpen)
            {
                CompleteDeadlineMetrics(deadlineDuration);
            }

            CompleteAftermathMetrics(aftermathDuration);
        }

        private void CompleteDeadlineMetrics(float duration)
        {
            if (DeadlineCinematicSegmentCount == 1)
            {
                ShortestDeadlineCinematicDuration = duration;
            }
            else
            {
                ShortestDeadlineCinematicDuration = Mathf.Min(
                    ShortestDeadlineCinematicDuration,
                    duration);
            }

            LongestDeadlineCinematicDuration = Mathf.Max(
                LongestDeadlineCinematicDuration,
                duration);
        }

        private void CompleteAftermathMetrics(float duration)
        {
            LongestDeadlineAftermathDuration = Mathf.Max(
                LongestDeadlineAftermathDuration,
                duration);
        }

        private void AppendReplaySegment(
            float sourceStart,
            float sourceEnd,
            float presentationDuration,
            ReplayPlaybackPhase phase,
            CameraSample cameraAnchor,
            float cameraRecoveryStart,
            float cameraRecoveryDuration)
        {
            if (presentationDuration <= 0.000001f)
            {
                return;
            }

            float presentationStart = lastPresentationTime;
            lastPresentationTime += presentationDuration;
            replaySegments.Add(new ReplaySegment(
                presentationStart,
                lastPresentationTime,
                sourceStart,
                sourceEnd,
                phase,
                cameraAnchor,
                cameraRecoveryStart,
                cameraRecoveryDuration));

        }

        private ReplayPosition ResolveReplayPosition(float presentationTimestamp)
        {
            if (replaySegments.Count == 0)
            {
                return new ReplayPosition(
                    new ReplaySegment(
                        0f,
                        0f,
                        0f,
                        0f,
                        ReplayPlaybackPhase.Normal,
                        default,
                        0f,
                        0f),
                    0f);
            }

            int low = 0;
            int high = replaySegments.Count - 1;
            while (low < high)
            {
                int middle = low + ((high - low) / 2);
                if (replaySegments[middle].PresentationEnd <=
                    presentationTimestamp)
                {
                    low = middle + 1;
                }
                else
                {
                    high = middle;
                }
            }

            ReplaySegment segment = replaySegments[low];
            if (presentationTimestamp >= lastPresentationTime)
            {
                segment = replaySegments[replaySegments.Count - 1];
            }

            return new ReplayPosition(
                segment,
                segment.GetSourceTimestamp(presentationTimestamp));
        }

        private void BeginReplay()
        {
            replayRequested = false;
            BuildPresentationTimeline();
            if (cameraSamples.Count == 0 ||
                timingSamples.Count < 2 ||
                replaySegments.Count == 0 ||
                (tracks.Count == 0 && animationTracks.Count == 0))
            {
                Debug.LogWarning("Replay could not start because no frames were captured.", this);
                return;
            }

            SaveReplayLightingState();
            replayRoot.gameObject.SetActive(true);
            IsReplaying = true;
            float replayTimeOrigin = timingSamples[0].ReplayTimestamp;
            for (int i = 0; i < animationTracks.Count; i++)
            {
                animationTracks[i].PrepareForReplay(replayTimeOrigin);
            }

            DisableLiveSimulation();

            for (int i = 0; i < tracks.Count; i++)
            {
                tracks[i].HideSource();
            }

            for (int i = 0; i < animationTracks.Count; i++)
            {
                animationTracks[i].HideSource();
            }

            for (int i = 0; i < lightTracks.Count; i++)
            {
                lightTracks[i].HideSource();
            }

            IsOmniscientViewEnabled = false;
            omniscientFillLight.enabled = false;
            playbackTime = firstPresentationTime;
            CurrentSourceTimestamp = replaySegments[0].SourceStart;
            holdRemaining = 0f;
            ApplyReplay(playbackTime);
        }

        private void DisableLiveSimulation()
        {
            MonoBehaviour[] behaviours = FindObjectsByType<MonoBehaviour>(
                FindObjectsInactive.Exclude,
                FindObjectsSortMode.None);

            for (int i = 0; i < behaviours.Length; i++)
            {
                MonoBehaviour behaviour = behaviours[i];
                if (behaviour == null ||
                    !behaviour.enabled ||
                    behaviour == this ||
                    behaviour is StageController ||
                    behaviour is PlayerInputReader ||
                    behaviour is GameHud)
                {
                    continue;
                }

                behaviour.enabled = false;
                disabledBehaviours.Add(behaviour);
            }
        }

        private void AdvanceReplay(float realDeltaTime)
        {
            if (RecordedDuration <= 0f)
            {
                ApplyReplay(firstPresentationTime);
                return;
            }

            if (holdRemaining > 0f)
            {
                holdRemaining = Mathf.Max(0f, holdRemaining - realDeltaTime);
                if (holdRemaining > 0f || !loop)
                {
                    return;
                }

                playbackTime = firstPresentationTime;
            }
            else
            {
                playbackTime += realDeltaTime;
            }

            if (playbackTime >= lastPresentationTime)
            {
                playbackTime = lastPresentationTime;
                ApplyReplay(playbackTime);
                holdRemaining = endHoldDuration;
                return;
            }

            ApplyReplay(playbackTime);
        }

        private void ApplyReplay(float presentationTimestamp)
        {
            ReplayPosition replayPosition = ResolveReplayPosition(
                presentationTimestamp);
            CurrentSourceTimestamp = replayPosition.SourceTimestamp;
            CurrentPlaybackPhase = replayPosition.Phase;
            ApplyCamera(replayPosition);
            for (int i = 0; i < tracks.Count; i++)
            {
                tracks[i].Apply(
                    replayPosition.SourceTimestamp,
                    IsOmniscientViewEnabled);
            }

            for (int i = 0; i < animationTracks.Count; i++)
            {
                animationTracks[i].Apply(
                    presentationTimestamp,
                    replayPosition.SourceTimestamp);
            }

            if (IsOmniscientViewEnabled)
            {
                for (int i = 0; i < lightTracks.Count; i++)
                {
                    lightTracks[i].HideProxy();
                }

                ApplyOmniscientLighting();
            }
            else
            {
                for (int i = 0; i < lightTracks.Count; i++)
                {
                    lightTracks[i].Apply(replayPosition.SourceTimestamp);
                }
            }
        }

        private void ApplyCamera(ReplayPosition replayPosition)
        {
            if (cameraSamples.Count == 0 || gameplayCamera == null)
            {
                return;
            }

            ReplaySegment segment = replayPosition.Segment;
            if (segment.Phase == ReplayPlaybackPhase.Deadline)
            {
                IsReplayCameraLocked = true;
                CurrentCameraRecoveryBlend = 0f;
                SetCamera(segment.CameraAnchor);
                return;
            }

            CameraSample recorded = EvaluateCamera(
                replayPosition.SourceTimestamp);
            IsReplayCameraLocked = false;
            if (segment.Phase == ReplayPlaybackPhase.DeadlineAftermath &&
                segment.CameraRecoveryDuration > 0f)
            {
                float recoveryBlend = Mathf.Clamp01(
                    (playbackTime - segment.CameraRecoveryStart) /
                    segment.CameraRecoveryDuration);
                CurrentCameraRecoveryBlend = recoveryBlend;
                if (recoveryBlend < 1f)
                {
                    SetCamera(CameraSample.Interpolate(
                        segment.CameraAnchor,
                        recorded,
                        recoveryBlend));
                    return;
                }
            }

            CurrentCameraRecoveryBlend = 1f;
            SetCamera(recorded);
        }

        private CameraSample EvaluateCamera(float timestamp)
        {
            int nextIndex = FindNextCameraSample(timestamp);
            int previousIndex = Mathf.Max(0, nextIndex - 1);
            CameraSample previous = cameraSamples[previousIndex];

            if (nextIndex >= cameraSamples.Count)
            {
                return previous;
            }

            CameraSample next = cameraSamples[nextIndex];
            float duration = next.Time - previous.Time;
            float blend = duration <= 0.000001f
                ? 0f
                : Mathf.Clamp01((timestamp - previous.Time) / duration);
            return CameraSample.Interpolate(previous, next, blend);
        }

        private int FindNextCameraSample(float timestamp)
        {
            int low = 0;
            int high = cameraSamples.Count;

            while (low < high)
            {
                int middle = low + ((high - low) / 2);
                if (cameraSamples[middle].Time <= timestamp)
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

        private void SetCamera(CameraSample sample)
        {
            gameplayCamera.transform.SetPositionAndRotation(
                sample.Position,
                sample.Rotation);
            gameplayCamera.backgroundColor = sample.BackgroundColor;
            gameplayCamera.fieldOfView = sample.FieldOfView;
        }

        private void SaveReplayLightingState()
        {
            if (hasReplayLightingSnapshot)
            {
                return;
            }

            replayLightingSnapshot =
                ReplayLightingSnapshot.Capture(gameplayCamera);
            hasReplayLightingSnapshot = true;
        }

        private void ApplyOmniscientLighting()
        {
            EnsureOmniscientFillLight();
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = omniscientAmbientSkyColor;
            RenderSettings.ambientEquatorColor = omniscientAmbientEquatorColor;
            RenderSettings.ambientGroundColor = omniscientAmbientGroundColor;
            RenderSettings.ambientIntensity = omniscientAmbientIntensity;
            RenderSettings.reflectionIntensity =
                omniscientReflectionIntensity;
            RenderSettings.fog = false;

            ApplyOmniscientFillLightSettings();
            omniscientFillLight.enabled = true;
            if (gameplayCamera != null)
            {
                gameplayCamera.backgroundColor = omniscientBackgroundColor;
            }
        }

        private void ApplyOmniscientFillLightSettings()
        {
            if (omniscientFillLight == null)
            {
                return;
            }

            omniscientFillLight.color = omniscientFillLightColor;
            omniscientFillLight.intensity = omniscientFillLightIntensity;
            omniscientFillLight.transform.rotation =
                Quaternion.Euler(omniscientFillLightRotation);
        }

        private void RestoreReplayLightingState()
        {
            if (hasReplayLightingSnapshot)
            {
                replayLightingSnapshot.Restore(gameplayCamera);
                hasReplayLightingSnapshot = false;
            }

            if (omniscientFillLight != null)
            {
                omniscientFillLight.enabled = false;
            }
        }

        private VisualTrack CreateTrack(Renderer source)
        {
            Renderer proxy = CreateProxyRenderer(source);
            if (proxy == null)
            {
                return null;
            }

            proxy.transform.SetParent(replayRoot, false);
            proxy.gameObject.name = $"Replay - {source.gameObject.name}";
            VisionCone visionCone = source.GetComponent<VisionCone>();
            EnemyCombatant enemy =
                source.GetComponentInParent<EnemyCombatant>(true);
            return new VisualTrack(
                source,
                proxy,
                visionCone,
                enemy);
        }

        private static Light CreateProxyLight(
            Light source,
            Transform parent)
        {
            GameObject proxyObject =
                new GameObject($"Replay Light - {source.gameObject.name}");
            proxyObject.transform.SetParent(parent, false);

            Light proxy = proxyObject.AddComponent<Light>();
            proxy.type = source.type;
            proxy.color = source.color;
            proxy.intensity = source.intensity;
            proxy.range = source.range;
            proxy.spotAngle = source.spotAngle;
            proxy.innerSpotAngle = source.innerSpotAngle;
            proxy.shadows = source.shadows;
            proxy.shadowStrength = source.shadowStrength;
            proxy.shadowBias = source.shadowBias;
            proxy.shadowNormalBias = source.shadowNormalBias;
            proxy.shadowNearPlane = source.shadowNearPlane;
            proxy.renderMode = source.renderMode;
            proxy.cullingMask = source.cullingMask;
            proxy.renderingLayerMask = source.renderingLayerMask;
            proxy.cookie = source.cookie;
            proxy.cookieSize = source.cookieSize;
            proxy.bounceIntensity = source.bounceIntensity;
            proxy.useColorTemperature = source.useColorTemperature;
            proxy.colorTemperature = source.colorTemperature;
            proxy.enabled = false;
            return proxy;
        }

        private static bool CanRecord(Renderer source)
        {
            return source is MeshRenderer ||
                   source is LineRenderer;
        }

        private static Renderer CreateProxyRenderer(Renderer source)
        {
            if (source is LineRenderer sourceLine)
            {
                return CreateLineProxy(sourceLine);
            }

            Mesh mesh = null;
            MeshFilter sourceFilter = source.GetComponent<MeshFilter>();
            if (sourceFilter != null && sourceFilter.sharedMesh != null)
            {
                mesh = Instantiate(sourceFilter.sharedMesh);
                mesh.name = $"Replay Mesh - {sourceFilter.sharedMesh.name}";
            }

            if (mesh == null)
            {
                return null;
            }

            GameObject proxyObject = new GameObject();
            MeshFilter proxyFilter = proxyObject.AddComponent<MeshFilter>();
            MeshRenderer proxyRenderer = proxyObject.AddComponent<MeshRenderer>();
            proxyFilter.sharedMesh = mesh;
            CopyRendererSettings(source, proxyRenderer);
            proxyRenderer.sharedMaterials = CloneMaterials(source.sharedMaterials);
            return proxyRenderer;
        }

        private static LineRenderer CreateLineProxy(LineRenderer source)
        {
            GameObject proxyObject = new GameObject();
            LineRenderer proxy = proxyObject.AddComponent<LineRenderer>();

            CopyRendererSettings(source, proxy);
            proxy.sharedMaterials = CloneMaterials(source.sharedMaterials);
            proxy.useWorldSpace = source.useWorldSpace;
            proxy.loop = source.loop;
            proxy.widthCurve = new AnimationCurve(source.widthCurve.keys);
            proxy.widthMultiplier = source.widthMultiplier;
            proxy.textureMode = source.textureMode;
            proxy.alignment = source.alignment;
            proxy.numCapVertices = source.numCapVertices;
            proxy.numCornerVertices = source.numCornerVertices;
            proxy.generateLightingData = source.generateLightingData;
            return proxy;
        }

        private static void CopyRendererSettings(Renderer source, Renderer target)
        {
            target.shadowCastingMode = source.shadowCastingMode;
            target.receiveShadows = source.receiveShadows;
            target.lightProbeUsage = LightProbeUsage.Off;
            target.reflectionProbeUsage = ReflectionProbeUsage.Off;
            target.sortingLayerID = source.sortingLayerID;
            target.sortingOrder = source.sortingOrder;
            target.renderingLayerMask = source.renderingLayerMask;
        }

        private static Material[] CloneMaterials(Material[] sourceMaterials)
        {
            Material[] clones = new Material[sourceMaterials.Length];
            for (int i = 0; i < sourceMaterials.Length; i++)
            {
                Material source = sourceMaterials[i];
                clones[i] = source == null ? null : new Material(source);
            }

            return clones;
        }

        private void OnDisable()
        {
            RestoreReplayLightingState();
            IsOmniscientViewEnabled = false;
            CurrentPlaybackPhase = ReplayPlaybackPhase.Normal;
            IsReplayCameraLocked = false;
            CurrentCameraRecoveryBlend = 1f;
            CurrentSourceTimestamp = 0f;
        }

        private void OnDestroy()
        {
            RestoreReplayLightingState();
            if (ActiveRecorder == this)
            {
                ActiveRecorder = null;
            }

            for (int i = 0; i < tracks.Count; i++)
            {
                tracks[i].Dispose();
            }

            for (int i = 0; i < animationTracks.Count; i++)
            {
                animationTracks[i].Dispose();
            }
        }

        private readonly struct ReplayLightingSnapshot
        {
            private readonly AmbientMode ambientMode;
            private readonly Color ambientSkyColor;
            private readonly Color ambientEquatorColor;
            private readonly Color ambientGroundColor;
            private readonly float ambientIntensity;
            private readonly float reflectionIntensity;
            private readonly bool fog;
            private readonly FogMode fogMode;
            private readonly Color fogColor;
            private readonly float fogDensity;
            private readonly float fogStartDistance;
            private readonly float fogEndDistance;
            private readonly bool hasCamera;
            private readonly Color cameraBackgroundColor;

            private ReplayLightingSnapshot(Camera camera)
            {
                ambientMode = RenderSettings.ambientMode;
                ambientSkyColor = RenderSettings.ambientSkyColor;
                ambientEquatorColor = RenderSettings.ambientEquatorColor;
                ambientGroundColor = RenderSettings.ambientGroundColor;
                ambientIntensity = RenderSettings.ambientIntensity;
                reflectionIntensity = RenderSettings.reflectionIntensity;
                fog = RenderSettings.fog;
                fogMode = RenderSettings.fogMode;
                fogColor = RenderSettings.fogColor;
                fogDensity = RenderSettings.fogDensity;
                fogStartDistance = RenderSettings.fogStartDistance;
                fogEndDistance = RenderSettings.fogEndDistance;
                hasCamera = camera != null;
                cameraBackgroundColor =
                    hasCamera ? camera.backgroundColor : Color.black;
            }

            public static ReplayLightingSnapshot Capture(Camera camera)
            {
                return new ReplayLightingSnapshot(camera);
            }

            public void Restore(Camera camera)
            {
                RenderSettings.ambientMode = ambientMode;
                RenderSettings.ambientSkyColor = ambientSkyColor;
                RenderSettings.ambientEquatorColor = ambientEquatorColor;
                RenderSettings.ambientGroundColor = ambientGroundColor;
                RenderSettings.ambientIntensity = ambientIntensity;
                RenderSettings.reflectionIntensity = reflectionIntensity;
                RenderSettings.fog = fog;
                RenderSettings.fogMode = fogMode;
                RenderSettings.fogColor = fogColor;
                RenderSettings.fogDensity = fogDensity;
                RenderSettings.fogStartDistance = fogStartDistance;
                RenderSettings.fogEndDistance = fogEndDistance;

                if (hasCamera && camera != null)
                {
                    camera.backgroundColor = cameraBackgroundColor;
                }
            }
        }

        private readonly struct CameraSample
        {
            public readonly float Time;
            public readonly Vector3 Position;
            public readonly Quaternion Rotation;
            public readonly Color BackgroundColor;
            public readonly float FieldOfView;

            public CameraSample(
                float time,
                Vector3 position,
                Quaternion rotation,
                Color backgroundColor,
                float fieldOfView)
            {
                Time = time;
                Position = position;
                Rotation = rotation;
                BackgroundColor = backgroundColor;
                FieldOfView = fieldOfView;
            }

            public static CameraSample Interpolate(
                CameraSample previous,
                CameraSample next,
                float blend)
            {
                return new CameraSample(
                    Mathf.Lerp(previous.Time, next.Time, blend),
                    Vector3.Lerp(previous.Position, next.Position, blend),
                    Quaternion.Slerp(previous.Rotation, next.Rotation, blend),
                    Color.Lerp(
                        previous.BackgroundColor,
                        next.BackgroundColor,
                        blend),
                    Mathf.Lerp(previous.FieldOfView, next.FieldOfView, blend));
            }
        }

        private readonly struct ReplayTimingSample
        {
            public readonly float SourceTimestamp;
            public readonly float WorldTimestamp;
            public readonly float ReplayTimestamp;
            public readonly bool DeadlineActive;

            public ReplayTimingSample(
                float sourceTimestamp,
                float worldTimestamp,
                float replayTimestamp,
                bool deadlineActive)
            {
                SourceTimestamp = sourceTimestamp;
                WorldTimestamp = worldTimestamp;
                ReplayTimestamp = replayTimestamp;
                DeadlineActive = deadlineActive;
            }
        }

        private readonly struct ReplaySegment
        {
            public readonly float PresentationStart;
            public readonly float PresentationEnd;
            public readonly float SourceStart;
            public readonly float SourceEnd;
            public readonly ReplayPlaybackPhase Phase;
            public readonly CameraSample CameraAnchor;
            public readonly float CameraRecoveryStart;
            public readonly float CameraRecoveryDuration;

            public ReplaySegment(
                float presentationStart,
                float presentationEnd,
                float sourceStart,
                float sourceEnd,
                ReplayPlaybackPhase phase,
                CameraSample cameraAnchor,
                float cameraRecoveryStart,
                float cameraRecoveryDuration)
            {
                PresentationStart = presentationStart;
                PresentationEnd = presentationEnd;
                SourceStart = sourceStart;
                SourceEnd = sourceEnd;
                Phase = phase;
                CameraAnchor = cameraAnchor;
                CameraRecoveryStart = cameraRecoveryStart;
                CameraRecoveryDuration = cameraRecoveryDuration;
            }

            public float GetSourceTimestamp(float presentationTimestamp)
            {
                float duration = PresentationEnd - PresentationStart;
                float blend = duration <= 0.000001f
                    ? 0f
                    : Mathf.Clamp01(
                        (presentationTimestamp - PresentationStart) / duration);
                return Mathf.Lerp(SourceStart, SourceEnd, blend);
            }
        }

        private readonly struct ReplayPosition
        {
            public readonly ReplaySegment Segment;
            public readonly float SourceTimestamp;
            public ReplayPlaybackPhase Phase => Segment.Phase;

            public ReplayPosition(
                ReplaySegment segment,
                float sourceTimestamp)
            {
                Segment = segment;
                SourceTimestamp = sourceTimestamp;
            }
        }

        private readonly struct LightSample
        {
            public readonly float Time;
            public readonly bool Visible;
            public readonly Vector3 Position;
            public readonly Quaternion Rotation;
            public readonly Color Color;
            public readonly float Intensity;
            public readonly float Range;
            public readonly float SpotAngle;
            public readonly float InnerSpotAngle;

            public LightSample(
                float time,
                bool visible,
                Vector3 position,
                Quaternion rotation,
                Color color,
                float intensity,
                float range,
                float spotAngle,
                float innerSpotAngle)
            {
                Time = time;
                Visible = visible;
                Position = position;
                Rotation = rotation;
                Color = color;
                Intensity = intensity;
                Range = range;
                SpotAngle = spotAngle;
                InnerSpotAngle = innerSpotAngle;
            }

            public static LightSample Hidden(float time)
            {
                return new LightSample(
                    time,
                    false,
                    Vector3.zero,
                    Quaternion.identity,
                    Color.black,
                    0f,
                    0f,
                    0f,
                    0f);
            }

            public bool Matches(
                Vector3 position,
                Quaternion rotation,
                Color color,
                float intensity,
                float range,
                float spotAngle,
                float innerSpotAngle)
            {
                return Visible &&
                       (Position - position).sqrMagnitude <= 0.00000001f &&
                       Quaternion.Dot(Rotation, rotation) >= 0.999999f &&
                       Approximately(Color, color) &&
                       Mathf.Abs(Intensity - intensity) <= 0.0001f &&
                       Mathf.Abs(Range - range) <= 0.0001f &&
                       Mathf.Abs(SpotAngle - spotAngle) <= 0.0001f &&
                       Mathf.Abs(InnerSpotAngle - innerSpotAngle) <= 0.0001f;
            }

            private static bool Approximately(Color left, Color right)
            {
                float difference =
                    Mathf.Abs(left.r - right.r) +
                    Mathf.Abs(left.g - right.g) +
                    Mathf.Abs(left.b - right.b) +
                    Mathf.Abs(left.a - right.a);
                return difference <= 0.0001f;
            }
        }

        private sealed class LightTrack
        {
            private readonly Light source;
            private readonly Light proxy;
            private readonly List<LightSample> samples =
                new List<LightSample>(64);

            public bool IsProxyActive =>
                proxy != null &&
                proxy.enabled &&
                proxy.gameObject.activeInHierarchy;
            public bool IsSourceEnabled =>
                source != null &&
                source.enabled &&
                source.gameObject.activeInHierarchy;
            public long EstimatedBytes => samples.Count * 64L;

            public LightTrack(Light sourceLight, Light proxyLight)
            {
                source = sourceLight;
                proxy = proxyLight;
            }

            public void Capture(float timestamp, bool forceKeyframe)
            {
                if (!IsSourceEnabled)
                {
                    CaptureHidden(timestamp, forceKeyframe);
                    return;
                }

                Vector3 position = source.transform.position;
                Quaternion rotation = source.transform.rotation;
                Color color = source.color;
                float intensity = source.intensity;
                float range = source.range;
                float spotAngle = source.spotAngle;
                float innerSpotAngle = source.innerSpotAngle;

                if (!forceKeyframe &&
                    samples.Count > 0 &&
                    samples[samples.Count - 1].Matches(
                        position,
                        rotation,
                        color,
                        intensity,
                        range,
                        spotAngle,
                        innerSpotAngle))
                {
                    return;
                }

                samples.Add(new LightSample(
                    timestamp,
                    true,
                    position,
                    rotation,
                    color,
                    intensity,
                    range,
                    spotAngle,
                    innerSpotAngle));
            }

            public void HideSource()
            {
                if (source != null)
                {
                    source.enabled = false;
                }
            }

            public void HideProxy()
            {
                if (proxy != null)
                {
                    proxy.enabled = false;
                }
            }

            public void Apply(float timestamp)
            {
                if (proxy == null ||
                    samples.Count == 0 ||
                    timestamp < samples[0].Time)
                {
                    if (proxy != null)
                    {
                        proxy.enabled = false;
                    }

                    return;
                }

                int nextIndex = FindNextSample(timestamp);
                int previousIndex = Mathf.Max(0, nextIndex - 1);
                LightSample previous = samples[previousIndex];

                if (!previous.Visible)
                {
                    proxy.enabled = false;
                    return;
                }

                if (nextIndex >= samples.Count || !samples[nextIndex].Visible)
                {
                    ApplySample(previous);
                    return;
                }

                LightSample next = samples[nextIndex];
                float duration = next.Time - previous.Time;
                float blend = duration <= 0.000001f
                    ? 0f
                    : Mathf.Clamp01((timestamp - previous.Time) / duration);
                ApplyInterpolated(previous, next, blend);
            }

            private void CaptureHidden(float timestamp, bool forceKeyframe)
            {
                if (samples.Count == 0 ||
                    (!forceKeyframe && !samples[samples.Count - 1].Visible))
                {
                    return;
                }

                samples.Add(LightSample.Hidden(timestamp));
            }

            private int FindNextSample(float timestamp)
            {
                int low = 0;
                int high = samples.Count;

                while (low < high)
                {
                    int middle = low + ((high - low) / 2);
                    if (samples[middle].Time <= timestamp)
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

            private void ApplySample(LightSample sample)
            {
                proxy.enabled = true;
                proxy.transform.SetPositionAndRotation(
                    sample.Position,
                    sample.Rotation);
                proxy.color = sample.Color;
                proxy.intensity = sample.Intensity;
                proxy.range = sample.Range;
                proxy.spotAngle = sample.SpotAngle;
                proxy.innerSpotAngle = sample.InnerSpotAngle;
            }

            private void ApplyInterpolated(
                LightSample previous,
                LightSample next,
                float blend)
            {
                proxy.enabled = true;
                proxy.transform.SetPositionAndRotation(
                    Vector3.Lerp(previous.Position, next.Position, blend),
                    Quaternion.Slerp(previous.Rotation, next.Rotation, blend));
                proxy.color = Color.Lerp(previous.Color, next.Color, blend);
                proxy.intensity =
                    Mathf.Lerp(previous.Intensity, next.Intensity, blend);
                proxy.range = Mathf.Lerp(previous.Range, next.Range, blend);
                proxy.spotAngle =
                    Mathf.Lerp(previous.SpotAngle, next.SpotAngle, blend);
                proxy.innerSpotAngle = Mathf.Lerp(
                    previous.InnerSpotAngle,
                    next.InnerSpotAngle,
                    blend);
            }
        }

        private sealed class VisualTrack : IDisposable
        {
            private readonly Renderer source;
            private readonly Renderer proxy;
            private readonly LineRenderer sourceLine;
            private readonly LineRenderer proxyLine;
            private readonly Mesh proxyMesh;
            private readonly VisionCone visionCone;
            private readonly bool isVisionCone;
            private readonly EnemyCombatant replayVisibilityOwner;
            private readonly bool supportsOmniscientVisibility;
            private readonly List<VisualSample> samples =
                new List<VisualSample>(128);
            private readonly List<Material> sourceMaterials =
                new List<Material>(4);
            private readonly List<Material> proxyMaterials =
                new List<Material>(4);
            private Color[] materialColorBuffer = Array.Empty<Color>();
            private Vector3[] linePositionBuffer;

            public int InstanceId { get; }
            public bool IsVisionCone => isVisionCone;
            public int SampleCount => samples.Count;
            public long EstimatedBytes
            {
                get
                {
                    long bytes = 64L * samples.Count;
                    for (int i = 0; i < samples.Count; i++)
                    {
                        bytes += samples[i].MaterialColors == null
                            ? 0L
                            : samples[i].MaterialColors.Length * 16L;
                        bytes += samples[i].LinePositions == null
                            ? 0L
                            : samples[i].LinePositions.Length * 12L;
                    }

                    return bytes;
                }
            }
            public bool SupportsOmniscientVisibility =>
                supportsOmniscientVisibility;
            public bool IsReplayExcluded =>
                source != null &&
                source.GetComponentInParent<ReplayExcluded>() != null;
            public bool IsProxyActive =>
                proxy != null &&
                proxy.enabled &&
                proxy.gameObject.activeInHierarchy;
            public VisualTrack(
                Renderer sourceRenderer,
                Renderer proxyRenderer,
                VisionCone sourceVisionCone,
                EnemyCombatant enemy)
            {
                source = sourceRenderer;
                proxy = proxyRenderer;
                sourceLine = sourceRenderer as LineRenderer;
                proxyLine = proxyRenderer as LineRenderer;
                visionCone = sourceVisionCone;
                isVisionCone = sourceVisionCone != null;
                MeshFilter proxyFilter = isVisionCone
                    ? proxyRenderer.GetComponent<MeshFilter>()
                    : null;
                proxyMesh = proxyFilter != null
                    ? proxyFilter.sharedMesh
                    : null;
                if (proxyMesh != null)
                {
                    proxyMesh.MarkDynamic();
                }
                replayVisibilityOwner = enemy;
                supportsOmniscientVisibility =
                    enemy != null &&
                    enemy.TryGetReplayVisibility(sourceRenderer, out _);
                InstanceId = sourceRenderer.GetInstanceID();
                proxy.enabled = false;
            }

            public void Capture(float timestamp, bool forceKeyframe)
            {
                bool active = source != null &&
                              source.gameObject.activeInHierarchy;
                bool visible = active && source.enabled;
                bool omniscientVisible = visible;
                if (supportsOmniscientVisibility)
                {
                    omniscientVisible =
                        active &&
                        replayVisibilityOwner != null &&
                        replayVisibilityOwner.TryGetReplayVisibility(
                            source,
                            out bool logicalVisibility) &&
                        logicalVisibility;
                }

                if (!visible && !omniscientVisible)
                {
                    CaptureHidden(timestamp, forceKeyframe);
                    return;
                }

                ReadMaterialColors();

                Color startColor = Color.white;
                Color endColor = Color.white;
                if (sourceLine != null)
                {
                    if (linePositionBuffer == null ||
                        linePositionBuffer.Length != sourceLine.positionCount)
                    {
                        linePositionBuffer =
                            new Vector3[sourceLine.positionCount];
                    }

                    sourceLine.GetPositions(linePositionBuffer);
                    startColor = sourceLine.startColor;
                    endColor = sourceLine.endColor;
                }

                Vector3 position = source.transform.position;
                Quaternion rotation = source.transform.rotation;
                Vector3 scale = source.transform.lossyScale;
                if (!forceKeyframe &&
                    samples.Count > 0 &&
                    samples[samples.Count - 1].Matches(
                        visible,
                        omniscientVisible,
                        position,
                        rotation,
                        scale,
                        materialColorBuffer,
                         linePositionBuffer,
                         startColor,
                         endColor))
                {
                    return;
                }

                Color[] recordedMaterialColors =
                    samples.Count > 0 &&
                    VisualSample.ColorsMatch(
                        samples[samples.Count - 1].MaterialColors,
                        materialColorBuffer)
                        ? samples[samples.Count - 1].MaterialColors
                        : (Color[])materialColorBuffer.Clone();
                Vector3[] recordedLinePositions = linePositionBuffer == null
                    ? null
                    : samples.Count > 0 &&
                      VisualSample.PositionsMatch(
                          samples[samples.Count - 1].LinePositions,
                          linePositionBuffer)
                        ? samples[samples.Count - 1].LinePositions
                        : (Vector3[])linePositionBuffer.Clone();
                samples.Add(new VisualSample(
                    timestamp,
                    visible,
                    omniscientVisible,
                    position,
                    rotation,
                    scale,
                    recordedMaterialColors,
                    recordedLinePositions,
                    startColor,
                    endColor));
            }

            public void CaptureHidden(float timestamp, bool forceKeyframe)
            {
                if (samples.Count == 0 ||
                    (!forceKeyframe &&
                     !samples[samples.Count - 1].HasAnyVisibility))
                {
                    return;
                }

                samples.Add(VisualSample.Hidden(timestamp));
            }

            public void HideSource()
            {
                if (source != null)
                {
                    source.enabled = false;
                }
            }

            public void Apply(float timestamp, bool omniscientView)
            {
                if ((isVisionCone && omniscientView) ||
                    samples.Count == 0 ||
                    timestamp < samples[0].Time)
                {
                    proxy.enabled = false;
                    return;
                }

                int nextIndex = FindNextSample(timestamp);
                int previousIndex = Mathf.Max(0, nextIndex - 1);
                VisualSample previous = samples[previousIndex];

                if (!previous.IsVisible(omniscientView))
                {
                    proxy.enabled = false;
                    return;
                }

                if (nextIndex >= samples.Count ||
                    !samples[nextIndex].IsVisible(omniscientView))
                {
                    ApplySample(previous);
                    return;
                }

                VisualSample next = samples[nextIndex];
                float duration = next.Time - previous.Time;
                float blend = duration <= 0.000001f
                    ? 0f
                    : Mathf.Clamp01((timestamp - previous.Time) / duration);
                ApplyInterpolated(previous, next, blend);
            }

            private int FindNextSample(float timestamp)
            {
                int low = 0;
                int high = samples.Count;

                while (low < high)
                {
                    int middle = low + ((high - low) / 2);
                    if (samples[middle].Time <= timestamp)
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

            private void ApplySample(VisualSample sample)
            {
                proxy.enabled = true;
                proxy.transform.SetPositionAndRotation(sample.Position, sample.Rotation);
                proxy.transform.localScale = sample.Scale;
                ApplyMaterialColors(sample.MaterialColors);
                ApplyLine(sample.LinePositions, sample.StartColor, sample.EndColor);
                RebuildReplayVisionCone(sample.Position, sample.Rotation);
            }

            private void ApplyInterpolated(
                VisualSample previous,
                VisualSample next,
                float blend)
            {
                proxy.enabled = true;
                proxy.transform.SetPositionAndRotation(
                    Vector3.Lerp(previous.Position, next.Position, blend),
                    Quaternion.Slerp(previous.Rotation, next.Rotation, blend));
                proxy.transform.localScale =
                    Vector3.Lerp(previous.Scale, next.Scale, blend);
                ApplyInterpolatedMaterialColors(
                    previous.MaterialColors,
                    next.MaterialColors,
                    blend);
                ApplyInterpolatedLine(previous, next, blend);
                RebuildReplayVisionCone(
                    Vector3.Lerp(previous.Position, next.Position, blend),
                    Quaternion.Slerp(previous.Rotation, next.Rotation, blend));
            }

            private void RebuildReplayVisionCone(
                Vector3 worldPosition,
                Quaternion worldRotation)
            {
                if (!isVisionCone || visionCone == null || proxyMesh == null)
                {
                    return;
                }

                visionCone.RebuildReplayMesh(
                    proxyMesh,
                    worldPosition,
                    worldRotation);
            }

            private void ApplyLine(
                Vector3[] positions,
                Color startColor,
                Color endColor)
            {
                if (proxyLine == null || positions == null)
                {
                    return;
                }

                proxyLine.positionCount = positions.Length;
                proxyLine.SetPositions(positions);
                proxyLine.startColor = startColor;
                proxyLine.endColor = endColor;
            }

            private void ApplyInterpolatedLine(
                VisualSample previous,
                VisualSample next,
                float blend)
            {
                Vector3[] previousPositions = previous.LinePositions;
                Vector3[] nextPositions = next.LinePositions;
                if (proxyLine == null || previousPositions == null)
                {
                    return;
                }

                if (nextPositions == null ||
                    previousPositions.Length != nextPositions.Length)
                {
                    ApplyLine(
                        previousPositions,
                        previous.StartColor,
                        previous.EndColor);
                    return;
                }

                proxyLine.positionCount = previousPositions.Length;
                for (int i = 0; i < previousPositions.Length; i++)
                {
                    proxyLine.SetPosition(
                        i,
                        Vector3.Lerp(
                            previousPositions[i],
                            nextPositions[i],
                            blend));
                }

                proxyLine.startColor =
                    Color.Lerp(previous.StartColor, next.StartColor, blend);
                proxyLine.endColor =
                    Color.Lerp(previous.EndColor, next.EndColor, blend);
            }

            public void Dispose()
            {
                if (proxy == null)
                {
                    return;
                }

                proxy.GetSharedMaterials(proxyMaterials);
                for (int i = 0; i < proxyMaterials.Count; i++)
                {
                    if (proxyMaterials[i] != null)
                    {
                        Destroy(proxyMaterials[i]);
                    }
                }

                MeshFilter filter = proxy.GetComponent<MeshFilter>();
                if (filter != null && filter.sharedMesh != null)
                {
                    Destroy(filter.sharedMesh);
                }

            }

            private void ReadMaterialColors()
            {
                source.GetSharedMaterials(sourceMaterials);
                if (materialColorBuffer.Length != sourceMaterials.Count)
                {
                    materialColorBuffer = new Color[sourceMaterials.Count];
                }

                for (int i = 0; i < sourceMaterials.Count; i++)
                {
                    materialColorBuffer[i] =
                        ReadMaterialColor(sourceMaterials[i]);
                }
            }

            private static Color ReadMaterialColor(Material material)
            {
                if (material == null)
                {
                    return Color.white;
                }

                if (material.HasProperty(BaseColorProperty))
                {
                    return material.GetColor(BaseColorProperty);
                }

                return material.HasProperty(ColorProperty)
                    ? material.GetColor(ColorProperty)
                    : Color.white;
            }

            private void ApplyMaterialColors(Color[] colors)
            {
                if (colors == null)
                {
                    return;
                }

                proxy.GetSharedMaterials(proxyMaterials);
                int count = Mathf.Min(proxyMaterials.Count, colors.Length);
                for (int i = 0; i < count; i++)
                {
                    SetMaterialColor(proxyMaterials[i], colors[i]);
                }
            }

            private void ApplyInterpolatedMaterialColors(
                Color[] previous,
                Color[] next,
                float blend)
            {
                if (previous == null || next == null)
                {
                    ApplyMaterialColors(previous);
                    return;
                }

                proxy.GetSharedMaterials(proxyMaterials);
                int count = Mathf.Min(
                    proxyMaterials.Count,
                    Mathf.Min(previous.Length, next.Length));
                for (int i = 0; i < count; i++)
                {
                    SetMaterialColor(
                        proxyMaterials[i],
                        Color.Lerp(previous[i], next[i], blend));
                }
            }

            private static void SetMaterialColor(
                Material material,
                Color color)
            {
                if (material == null)
                {
                    return;
                }

                if (material.HasProperty(BaseColorProperty))
                {
                    material.SetColor(BaseColorProperty, color);
                }

                if (material.HasProperty(ColorProperty))
                {
                    material.SetColor(ColorProperty, color);
                }
            }
        }

        private readonly struct VisualSample
        {
            public float Time { get; }
            public bool Visible { get; }
            public bool OmniscientVisible { get; }
            public bool HasAnyVisibility => Visible || OmniscientVisible;
            public Vector3 Position { get; }
            public Quaternion Rotation { get; }
            public Vector3 Scale { get; }
            public Color[] MaterialColors { get; }
            public Vector3[] LinePositions { get; }
            public Color StartColor { get; }
            public Color EndColor { get; }
            public VisualSample(
                float time,
                bool visible,
                bool omniscientVisible,
                Vector3 position,
                Quaternion rotation,
                Vector3 scale,
                Color[] materialColors,
                Vector3[] linePositions,
                Color startColor,
                Color endColor)
            {
                Time = time;
                Visible = visible;
                OmniscientVisible = omniscientVisible;
                Position = position;
                Rotation = rotation;
                Scale = scale;
                MaterialColors = materialColors;
                LinePositions = linePositions;
                StartColor = startColor;
                EndColor = endColor;
            }

            public static VisualSample Hidden(float time)
            {
                return new VisualSample(
                    time,
                    false,
                    false,
                    Vector3.zero,
                    Quaternion.identity,
                    Vector3.one,
                    null,
                    null,
                    Color.white,
                    Color.white);
            }

            public bool IsVisible(bool omniscientView)
            {
                return omniscientView ? OmniscientVisible : Visible;
            }

            public bool Matches(
                bool visible,
                bool omniscientVisible,
                Vector3 position,
                Quaternion rotation,
                Vector3 scale,
                Color[] materialColors,
                Vector3[] linePositions,
                Color startColor,
                Color endColor)
            {
                if (Visible != visible ||
                    OmniscientVisible != omniscientVisible ||
                    !Approximately(Position, position) ||
                    Quaternion.Dot(Rotation, rotation) < 0.999999f ||
                    !Approximately(Scale, scale) ||
                    !ColorsMatch(MaterialColors, materialColors) ||
                    !PositionsMatch(LinePositions, linePositions))
                {
                    return false;
                }

                return Approximately(StartColor, startColor) &&
                       Approximately(EndColor, endColor);
            }

            public static bool ColorsMatch(Color[] left, Color[] right)
            {
                if (left == null || right == null)
                {
                    return left == right;
                }

                if (left.Length != right.Length)
                {
                    return false;
                }

                for (int i = 0; i < left.Length; i++)
                {
                    if (!Approximately(left[i], right[i]))
                    {
                        return false;
                    }
                }

                return true;
            }

            public static bool PositionsMatch(Vector3[] left, Vector3[] right)
            {
                if (left == null || right == null)
                {
                    return left == right;
                }

                if (left.Length != right.Length)
                {
                    return false;
                }

                for (int i = 0; i < left.Length; i++)
                {
                    if (!Approximately(left[i], right[i]))
                    {
                        return false;
                    }
                }

                return true;
            }

            private static bool Approximately(Vector3 left, Vector3 right)
            {
                return (left - right).sqrMagnitude <= 0.00000001f;
            }

            private static bool Approximately(Color left, Color right)
            {
                float difference =
                    Mathf.Abs(left.r - right.r) +
                    Mathf.Abs(left.g - right.g) +
                    Mathf.Abs(left.b - right.b) +
                    Mathf.Abs(left.a - right.a);
                return difference <= 0.0001f;
            }
        }
    }
}
