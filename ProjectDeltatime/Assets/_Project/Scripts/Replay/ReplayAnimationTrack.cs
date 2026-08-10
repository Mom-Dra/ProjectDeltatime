using System;
using System.Collections.Generic;
using Deltatime.Visuals;
using UnityEngine;

namespace Deltatime.Replay
{
    /// <summary>
    /// Low-volume replay data for one animated visual hierarchy. The hierarchy,
    /// Avatar and controllers are retained once; recording stores only actor
    /// transforms, parameter/controller/trigger events and sparse state checkpoints.
    /// No skinned-mesh bone transform is sampled.
    /// </summary>
    internal sealed class ReplayAnimationTrack : IDisposable
    {
        private const float ValueEpsilon = 0.0001f;
        private const long TransformSampleBytes = 48L;
        private const long AnimatorEventBytes = 40L;
        private const long LayerCheckpointBytes = 52L;
        private const long AppearanceSampleBaseBytes = 24L;

        private readonly CharacterAnimationController source;
        private readonly Animator sourceAnimator;
        private readonly Transform sourceVisualRoot;
        private readonly GameObject proxyRootObject;
        private readonly Transform proxyRoot;
        private readonly Animator proxyAnimator;
        private readonly Renderer[] sourceRenderers = Array.Empty<Renderer>();
        private readonly Renderer[] proxyRenderers = Array.Empty<Renderer>();
        private readonly AppearanceBinding[] appearanceBindings =
            Array.Empty<AppearanceBinding>();
        private readonly List<AnimationTransformSample> transformSamples =
            new List<AnimationTransformSample>(128);
        private readonly List<AnimatorReplayEvent> events =
            new List<AnimatorReplayEvent>(32);
        private readonly List<AnimationCheckpoint> checkpoints =
            new List<AnimationCheckpoint>(8);
        private readonly List<ParameterBinding> parameters =
            new List<ParameterBinding>(8);
        private Behaviour[] proxyBehaviours = Array.Empty<Behaviour>();
        private Collider[] proxyColliders = Array.Empty<Collider>();
        private Rigidbody[] proxyRigidbodies = Array.Empty<Rigidbody>();

        private RuntimeAnimatorController initialController;
        private RuntimeAnimatorController lastRecordedController;
        private bool initialActive;
        private bool hasRecordedActive;
        private bool lastRecordedActive;
        private bool playbackActorActive;
        private bool transformSampleActive;
        private float checkpointInterval;
        private float nextCheckpointReplayTime;
        private float replayTimeOrigin;
        private float trackPresentationStart;
        private float lastAppliedPresentationTime = -1f;
        private int eventCursor;
        private int checkpointCursor;
        private bool prepared;
        private bool disposed;

        public int InstanceId { get; }
        public int EventCount => events.Count;
        public int ControllerChangeCount
        {
            get
            {
                int count = 0;
                for (int i = 0; i < events.Count; i++)
                {
                    if (events[i].Kind == AnimatorReplayEventKind.Controller)
                    {
                        count++;
                    }
                }

                return count;
            }
        }
        public int CheckpointCount => checkpoints.Count;
        public int TransformSampleCount => transformSamples.Count;
        public Animator ProxyAnimator => proxyAnimator;
        public bool HasAdvancedState { get; private set; }
        public bool HasObservedTransition { get; private set; }
        public bool HasObservedStateChange { get; private set; }
        public bool HasEnabledGameplayComponents
        {
            get
            {
                for (int i = 0; i < proxyBehaviours.Length; i++)
                {
                    Behaviour behaviour = proxyBehaviours[i];
                    if (behaviour != null && behaviour != proxyAnimator &&
                        behaviour.enabled)
                    {
                        return true;
                    }
                }

                for (int i = 0; i < proxyColliders.Length; i++)
                {
                    if (proxyColliders[i] != null &&
                        proxyColliders[i].enabled)
                    {
                        return true;
                    }
                }

                for (int i = 0; i < proxyRigidbodies.Length; i++)
                {
                    if (proxyRigidbodies[i] != null &&
                        proxyRigidbodies[i].detectCollisions)
                    {
                        return true;
                    }
                }

                return false;
            }
        }
        public bool IsProxyActive
        {
            get
            {
                if (proxyRootObject == null ||
                    !proxyRootObject.activeInHierarchy)
                {
                    return false;
                }

                for (int i = 0; i < proxyRenderers.Length; i++)
                {
                    if (proxyRenderers[i] != null && proxyRenderers[i].enabled)
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        public long EstimatedBytes
        {
            get
            {
                long bytes = transformSamples.Count * TransformSampleBytes +
                             events.Count * AnimatorEventBytes;
                for (int i = 0; i < checkpoints.Count; i++)
                {
                    AnimationCheckpoint checkpoint = checkpoints[i];
                    bytes += 24L;
                    bytes += checkpoint.RecordedLayers.Length *
                             LayerCheckpointBytes;
                    if (checkpoint.PresentationLayers != null)
                    {
                        bytes += checkpoint.PresentationLayers.Length *
                                 LayerCheckpointBytes;
                    }

                    if (checkpoint.PresentationParameters != null)
                    {
                        bytes += checkpoint.PresentationParameters.Length * 16L;
                    }
                }

                for (int i = 0; i < appearanceBindings.Length; i++)
                {
                    bytes += appearanceBindings[i].EstimatedBytes;
                }

                return bytes;
            }
        }

        public ReplayAnimationTrack(
            CharacterAnimationController sourceController,
            Transform replayParent,
            float stateCheckpointInterval)
        {
            source = sourceController;
            sourceAnimator = sourceController == null
                ? null
                : sourceController.Animator;
            sourceVisualRoot = sourceController == null
                ? null
                : sourceController.VisualRoot;
            checkpointInterval = Mathf.Max(0.25f, stateCheckpointInterval);

            if (source == null || sourceAnimator == null ||
                sourceVisualRoot == null || replayParent == null)
            {
                return;
            }

            InstanceId = source.GetInstanceID();
            initialController = sourceAnimator.runtimeAnimatorController;
            lastRecordedController = initialController;
            initialActive = sourceVisualRoot.gameObject.activeInHierarchy;
            lastRecordedActive = initialActive;
            playbackActorActive = initialActive;
            hasRecordedActive = true;

            // Instantiate invokes Awake immediately for active MonoBehaviours.
            // The current character visual root is deliberately script-free;
            // reject future roots that violate that contract instead of briefly
            // executing gameplay code before it can be disabled.
            MonoBehaviour[] sourceVisualBehaviours =
                sourceVisualRoot.GetComponentsInChildren<MonoBehaviour>(true);
            if (sourceVisualBehaviours.Length > 0)
            {
                return;
            }

            proxyRootObject = UnityEngine.Object.Instantiate(
                sourceVisualRoot.gameObject,
                replayParent);
            proxyRootObject.name = $"Replay Animator - {source.gameObject.name}";
            proxyRootObject.SetActive(false);
            proxyRoot = proxyRootObject.transform;

            Transform[] sourceTransforms =
                sourceVisualRoot.GetComponentsInChildren<Transform>(true);
            Transform[] proxyTransforms =
                proxyRoot.GetComponentsInChildren<Transform>(true);
            int animatorTransformIndex = Array.IndexOf(
                sourceTransforms,
                sourceAnimator.transform);
            if (animatorTransformIndex >= 0 &&
                animatorTransformIndex < proxyTransforms.Length)
            {
                proxyAnimator = proxyTransforms[animatorTransformIndex]
                    .GetComponent<Animator>();
            }

            sourceRenderers =
                sourceVisualRoot.GetComponentsInChildren<Renderer>(true);
            proxyRenderers =
                proxyRoot.GetComponentsInChildren<Renderer>(true);
            int rendererCount = Mathf.Min(
                sourceRenderers.Length,
                proxyRenderers.Length);
            appearanceBindings = new AppearanceBinding[rendererCount];
            for (int i = 0; i < rendererCount; i++)
            {
                appearanceBindings[i] = new AppearanceBinding(
                    sourceRenderers[i],
                    proxyRenderers[i]);
            }

            DisableProxyGameplayComponents();
            if (proxyAnimator != null)
            {
                ReplayAnimatorProxyRegistry.Register(proxyAnimator);
                proxyAnimator.enabled = true;
                proxyAnimator.applyRootMotion = false;
                proxyAnimator.updateMode = AnimatorUpdateMode.UnscaledTime;
                proxyAnimator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                // Automatic Animator evaluation is held at zero. Replay time is
                // advanced explicitly with Animator.Update(unscaled replay delta),
                // so world slow and Unity's scaled clock cannot affect the proxy.
                proxyAnimator.speed = 0f;
            }

            RefreshParameterBindings();
        }

        // A destroyed gameplay source must not invalidate its already captured
        // replay actor. This is what keeps death/removal-tail animation visible.
        public bool IsValid => proxyRootObject != null && proxyAnimator != null;

        private bool CanCapture => IsValid && source != null &&
                                   sourceAnimator != null &&
                                   sourceVisualRoot != null;

        public void Capture(
            float sourceTime,
            float replayTime,
            bool forceCheckpoint)
        {
            if (!CanCapture || prepared)
            {
                return;
            }

            bool active = sourceVisualRoot.gameObject.activeInHierarchy;
            CaptureActive(sourceTime, replayTime, active);
            CaptureTransform(sourceTime, active);
            CaptureParameters(sourceTime, replayTime);
            for (int i = 0; i < appearanceBindings.Length; i++)
            {
                appearanceBindings[i].Capture(sourceTime);
            }

            if (forceCheckpoint || checkpoints.Count == 0 ||
                replayTime + ValueEpsilon >= nextCheckpointReplayTime)
            {
                CaptureCheckpoint(sourceTime, replayTime);
                nextCheckpointReplayTime = replayTime + checkpointInterval;
            }
        }

        public void RecordController(
            RuntimeAnimatorController controller,
            float sourceTime,
            float replayTime)
        {
            if (!IsValid || prepared || controller == lastRecordedController)
            {
                return;
            }

            events.Add(AnimatorReplayEvent.ControllerChanged(
                sourceTime,
                replayTime,
                controller));
            lastRecordedController = controller;
            RefreshParameterBindings();
        }

        public void RecordTrigger(
            int parameterHash,
            bool set,
            float sourceTime,
            float replayTime)
        {
            if (!IsValid || prepared)
            {
                return;
            }

            events.Add(AnimatorReplayEvent.Trigger(
                sourceTime,
                replayTime,
                parameterHash,
                set));
        }

        public void RecordActive(
            bool active,
            float sourceTime,
            float replayTime)
        {
            if (!IsValid || prepared)
            {
                return;
            }

            CaptureActive(sourceTime, replayTime, active);
        }

        public void PrepareForReplay(float presentationReplayTimeOrigin)
        {
            if (!IsValid || prepared)
            {
                return;
            }

            replayTimeOrigin = presentationReplayTimeOrigin;
            trackPresentationStart = events.Count == 0
                ? 0f
                : Mathf.Max(0f, events[0].ReplayTime - replayTimeOrigin);
            if (checkpoints.Count > 0)
            {
                trackPresentationStart = Mathf.Min(
                    trackPresentationStart,
                    Mathf.Max(
                        0f,
                        checkpoints[0].ReplayTime - replayTimeOrigin));
            }

            prepared = true;
            proxyRootObject.SetActive(true);
            ResetPlayback(trackPresentationStart);
        }

        public void HideSource()
        {
            for (int i = 0; i < sourceRenderers.Length; i++)
            {
                if (sourceRenderers[i] != null)
                {
                    sourceRenderers[i].enabled = false;
                }
            }
        }

        public void Apply(float presentationTime, float sourceTime)
        {
            if (!prepared || !IsValid)
            {
                return;
            }

            ApplyTransform(sourceTime);
            for (int i = 0; i < appearanceBindings.Length; i++)
            {
                appearanceBindings[i].Apply(sourceTime);
            }

            float target = Mathf.Max(trackPresentationStart, presentationTime);
            if (lastAppliedPresentationTime < 0f ||
                target + ValueEpsilon < lastAppliedPresentationTime)
            {
                ResetPlayback(target);
            }

            AdvanceAnimator(target);
        }

        public bool HasTriggerEvent(int parameterHash)
        {
            for (int i = 0; i < events.Count; i++)
            {
                if (events[i].Kind == AnimatorReplayEventKind.SetTrigger &&
                    events[i].ParameterHash == parameterHash)
                {
                    return true;
                }
            }

            return false;
        }

        private void CaptureActive(
            float sourceTime,
            float replayTime,
            bool active)
        {
            if (hasRecordedActive && active == lastRecordedActive)
            {
                return;
            }

            events.Add(AnimatorReplayEvent.ActiveChanged(
                sourceTime,
                replayTime,
                active));
            hasRecordedActive = true;
            lastRecordedActive = active;
        }

        private void CaptureTransform(float sourceTime, bool active)
        {
            Vector3 position = sourceVisualRoot.position;
            Quaternion rotation = sourceVisualRoot.rotation;
            Vector3 scale = sourceVisualRoot.lossyScale;
            if (transformSamples.Count > 0 &&
                transformSamples[transformSamples.Count - 1].Matches(
                    active,
                    position,
                    rotation,
                    scale))
            {
                return;
            }

            transformSamples.Add(new AnimationTransformSample(
                sourceTime,
                active,
                position,
                rotation,
                scale));
        }

        private void CaptureParameters(float sourceTime, float replayTime)
        {
            for (int i = 0; i < parameters.Count; i++)
            {
                ParameterBinding parameter = parameters[i];
                switch (parameter.Type)
                {
                    case AnimatorControllerParameterType.Float:
                    {
                        float value = sourceAnimator.GetFloat(parameter.Hash);
                        if (!parameter.Initialized ||
                            Mathf.Abs(value - parameter.FloatValue) >
                            ValueEpsilon)
                        {
                            events.Add(AnimatorReplayEvent.FloatParameter(
                                sourceTime,
                                replayTime,
                                parameter.Hash,
                                value));
                            parameter.FloatValue = value;
                            parameter.Initialized = true;
                        }

                        break;
                    }
                    case AnimatorControllerParameterType.Bool:
                    {
                        bool value = sourceAnimator.GetBool(parameter.Hash);
                        if (!parameter.Initialized || value != parameter.BoolValue)
                        {
                            events.Add(AnimatorReplayEvent.BoolParameter(
                                sourceTime,
                                replayTime,
                                parameter.Hash,
                                value));
                            parameter.BoolValue = value;
                            parameter.Initialized = true;
                        }

                        break;
                    }
                    case AnimatorControllerParameterType.Int:
                    {
                        int value = sourceAnimator.GetInteger(parameter.Hash);
                        if (!parameter.Initialized || value != parameter.IntValue)
                        {
                            events.Add(AnimatorReplayEvent.IntParameter(
                                sourceTime,
                                replayTime,
                                parameter.Hash,
                                value));
                            parameter.IntValue = value;
                            parameter.Initialized = true;
                        }

                        break;
                    }
                }
            }
        }

        private void RefreshParameterBindings()
        {
            parameters.Clear();
            if (sourceAnimator == null ||
                sourceAnimator.runtimeAnimatorController == null)
            {
                return;
            }

            // Animator.parameters allocates an array. It is intentionally read
            // only at actor registration/controller changes, never per frame.
            AnimatorControllerParameter[] animatorParameters =
                sourceAnimator.parameters;
            for (int i = 0; i < animatorParameters.Length; i++)
            {
                AnimatorControllerParameter parameter = animatorParameters[i];
                if (parameter.type != AnimatorControllerParameterType.Trigger)
                {
                    parameters.Add(new ParameterBinding(
                        parameter.nameHash,
                        parameter.type));
                }
            }
        }

        private void CaptureCheckpoint(float sourceTime, float replayTime)
        {
            checkpoints.Add(new AnimationCheckpoint(
                sourceTime,
                replayTime,
                CaptureLayers(sourceAnimator)));
        }

        private static AnimatorLayerState[] CaptureLayers(Animator animator)
        {
            if (animator == null || animator.runtimeAnimatorController == null)
            {
                return Array.Empty<AnimatorLayerState>();
            }

            AnimatorLayerState[] layers =
                new AnimatorLayerState[animator.layerCount];
            for (int i = 0; i < layers.Length; i++)
            {
                AnimatorStateInfo current =
                    animator.GetCurrentAnimatorStateInfo(i);
                bool inTransition = animator.IsInTransition(i);
                AnimatorStateInfo next = inTransition
                    ? animator.GetNextAnimatorStateInfo(i)
                    : default;
                AnimatorTransitionInfo transition = inTransition
                    ? animator.GetAnimatorTransitionInfo(i)
                    : default;
                layers[i] = new AnimatorLayerState(
                    current.fullPathHash,
                    current.normalizedTime,
                    animator.GetLayerWeight(i),
                    inTransition,
                    next.fullPathHash,
                    next.normalizedTime,
                    next.length,
                    transition.duration,
                    transition.normalizedTime,
                    transition.durationUnit == DurationUnit.Fixed);
            }

            return layers;
        }

        private ParameterSnapshot[] CaptureProxyParameters()
        {
            if (proxyAnimator == null ||
                proxyAnimator.runtimeAnimatorController == null)
            {
                return Array.Empty<ParameterSnapshot>();
            }

            AnimatorControllerParameter[] animatorParameters =
                proxyAnimator.parameters;
            int supportedCount = 0;
            for (int i = 0; i < animatorParameters.Length; i++)
            {
                if (animatorParameters[i].type !=
                    AnimatorControllerParameterType.Trigger)
                {
                    supportedCount++;
                }
            }

            ParameterSnapshot[] snapshots =
                new ParameterSnapshot[supportedCount];
            int index = 0;
            for (int i = 0; i < animatorParameters.Length; i++)
            {
                AnimatorControllerParameter parameter = animatorParameters[i];
                switch (parameter.type)
                {
                    case AnimatorControllerParameterType.Float:
                        snapshots[index++] = ParameterSnapshot.Float(
                            parameter.nameHash,
                            proxyAnimator.GetFloat(parameter.nameHash));
                        break;
                    case AnimatorControllerParameterType.Bool:
                        snapshots[index++] = ParameterSnapshot.Bool(
                            parameter.nameHash,
                            proxyAnimator.GetBool(parameter.nameHash));
                        break;
                    case AnimatorControllerParameterType.Int:
                        snapshots[index++] = ParameterSnapshot.Int(
                            parameter.nameHash,
                            proxyAnimator.GetInteger(parameter.nameHash));
                        break;
                }
            }

            return snapshots;
        }

        private void ResetPlayback(float targetPresentationTime)
        {
            proxyAnimator.speed = 0f;
            proxyAnimator.runtimeAnimatorController = initialController;
            if (initialController != null)
            {
                proxyAnimator.Rebind();
                proxyAnimator.Update(0f);
            }

            lastAppliedPresentationTime = trackPresentationStart;
            eventCursor = 0;
            checkpointCursor = 0;
            playbackActorActive = initialActive;

            AnimationCheckpoint restore = FindPresentationCheckpoint(
                targetPresentationTime);
            if (restore != null)
            {
                RestorePresentationCheckpoint(restore);
                lastAppliedPresentationTime =
                    Mathf.Max(
                        trackPresentationStart,
                        restore.ReplayTime - replayTimeOrigin);
                eventCursor = FindFirstEventAfter(lastAppliedPresentationTime);
                checkpointCursor = FindFirstCheckpointAfter(
                    lastAppliedPresentationTime);
                playbackActorActive = FindActiveAt(lastAppliedPresentationTime);
            }
            else if (checkpoints.Count > 0)
            {
                RestoreLayers(proxyAnimator, checkpoints[0].RecordedLayers);
            }

            RefreshProxyVisibility();
            AdvanceAnimator(targetPresentationTime);
        }

        private bool FindActiveAt(float presentationTime)
        {
            bool active = initialActive;
            for (int i = 0; i < events.Count; i++)
            {
                AnimatorReplayEvent replayEvent = events[i];
                if (replayEvent.ReplayTime - replayTimeOrigin >
                    presentationTime + ValueEpsilon)
                {
                    break;
                }

                if (replayEvent.Kind == AnimatorReplayEventKind.Active)
                {
                    active = replayEvent.BoolValue;
                }
            }

            return active;
        }

        private AnimationCheckpoint FindPresentationCheckpoint(float target)
        {
            for (int i = checkpoints.Count - 1; i >= 0; i--)
            {
                AnimationCheckpoint checkpoint = checkpoints[i];
                float presentation = checkpoint.ReplayTime - replayTimeOrigin;
                if (checkpoint.PresentationLayers != null &&
                    presentation <= target + ValueEpsilon)
                {
                    return checkpoint;
                }
            }

            return null;
        }

        private int FindFirstEventAfter(float presentationTime)
        {
            int index = 0;
            while (index < events.Count &&
                   events[index].ReplayTime - replayTimeOrigin <=
                   presentationTime + ValueEpsilon)
            {
                index++;
            }

            return index;
        }

        private int FindFirstCheckpointAfter(float presentationTime)
        {
            int index = 0;
            while (index < checkpoints.Count &&
                   checkpoints[index].ReplayTime - replayTimeOrigin <=
                   presentationTime + ValueEpsilon)
            {
                index++;
            }

            return index;
        }

        private void AdvanceAnimator(float targetPresentationTime)
        {
            if (targetPresentationTime <= lastAppliedPresentationTime +
                ValueEpsilon)
            {
                return;
            }

            while (lastAppliedPresentationTime + ValueEpsilon <
                   targetPresentationTime)
            {
                float nextEventTime = eventCursor < events.Count
                    ? events[eventCursor].ReplayTime - replayTimeOrigin
                    : float.PositiveInfinity;
                float nextCheckpointTime = checkpointCursor < checkpoints.Count
                    ? checkpoints[checkpointCursor].ReplayTime - replayTimeOrigin
                    : float.PositiveInfinity;
                float stepTarget = Mathf.Min(
                    targetPresentationTime,
                    Mathf.Min(nextEventTime, nextCheckpointTime));

                if (stepTarget > lastAppliedPresentationTime + ValueEpsilon)
                {
                    StepAnimator(stepTarget - lastAppliedPresentationTime);
                    lastAppliedPresentationTime = stepTarget;
                }

                bool progressed = false;
                while (eventCursor < events.Count &&
                       events[eventCursor].ReplayTime - replayTimeOrigin <=
                       lastAppliedPresentationTime + ValueEpsilon)
                {
                    ApplyEvent(events[eventCursor]);
                    eventCursor++;
                    progressed = true;
                }

                while (checkpointCursor < checkpoints.Count &&
                       checkpoints[checkpointCursor].ReplayTime -
                       replayTimeOrigin <=
                       lastAppliedPresentationTime + ValueEpsilon)
                {
                    AnimationCheckpoint checkpoint =
                        checkpoints[checkpointCursor];
                    if (checkpoint.PresentationLayers == null)
                    {
                        proxyAnimator.Update(0f);
                        checkpoint.PresentationController =
                            proxyAnimator.runtimeAnimatorController;
                        checkpoint.PresentationLayers =
                            CaptureLayers(proxyAnimator);
                        checkpoint.PresentationParameters =
                            CaptureProxyParameters();
                    }

                    checkpointCursor++;
                    progressed = true;
                }

                if (!progressed &&
                    stepTarget <= lastAppliedPresentationTime + ValueEpsilon)
                {
                    StepAnimator(
                        targetPresentationTime - lastAppliedPresentationTime);
                    lastAppliedPresentationTime = targetPresentationTime;
                }
            }
        }

        private void StepAnimator(float unscaledReplayDelta)
        {
            if (unscaledReplayDelta <= 0f ||
                proxyAnimator.runtimeAnimatorController == null)
            {
                return;
            }

            proxyAnimator.speed = 1f;
            proxyAnimator.Update(unscaledReplayDelta);
            proxyAnimator.speed = 0f;
            HasAdvancedState = true;
            for (int i = 0; i < proxyAnimator.layerCount; i++)
            {
                HasObservedTransition |= proxyAnimator.IsInTransition(i);
                if (checkpoints.Count > 0 &&
                    i < checkpoints[0].RecordedLayers.Length)
                {
                    HasObservedStateChange |=
                        proxyAnimator.GetCurrentAnimatorStateInfo(i)
                            .fullPathHash !=
                        checkpoints[0].RecordedLayers[i]
                            .CurrentFullPathHash;
                }
            }
        }

        private void ApplyEvent(AnimatorReplayEvent replayEvent)
        {
            switch (replayEvent.Kind)
            {
                case AnimatorReplayEventKind.Controller:
                    proxyAnimator.runtimeAnimatorController =
                        replayEvent.Controller;
                    if (replayEvent.Controller != null)
                    {
                        proxyAnimator.Rebind();
                        proxyAnimator.Update(0f);
                    }

                    break;
                case AnimatorReplayEventKind.Float:
                    proxyAnimator.SetFloat(
                        replayEvent.ParameterHash,
                        replayEvent.FloatValue);
                    break;
                case AnimatorReplayEventKind.Bool:
                    proxyAnimator.SetBool(
                        replayEvent.ParameterHash,
                        replayEvent.BoolValue);
                    break;
                case AnimatorReplayEventKind.Int:
                    proxyAnimator.SetInteger(
                        replayEvent.ParameterHash,
                        replayEvent.IntValue);
                    break;
                case AnimatorReplayEventKind.SetTrigger:
                    proxyAnimator.SetTrigger(replayEvent.ParameterHash);
                    break;
                case AnimatorReplayEventKind.ResetTrigger:
                    proxyAnimator.ResetTrigger(replayEvent.ParameterHash);
                    break;
                case AnimatorReplayEventKind.Active:
                    // Keep evaluating the Animator while hidden so reactivation
                    // does not restart its state machine or lose replay time.
                    playbackActorActive = replayEvent.BoolValue;
                    RefreshProxyVisibility();
                    break;
            }

            proxyAnimator.Update(0f);
        }

        private static void RestoreLayers(
            Animator animator,
            AnimatorLayerState[] layers)
        {
            if (animator == null || layers == null ||
                animator.runtimeAnimatorController == null)
            {
                return;
            }

            int count = Mathf.Min(animator.layerCount, layers.Length);
            for (int i = 0; i < count; i++)
            {
                AnimatorLayerState layer = layers[i];
                animator.SetLayerWeight(i, layer.Weight);
                if (layer.CurrentFullPathHash != 0)
                {
                    animator.Play(
                        layer.CurrentFullPathHash,
                        i,
                        layer.CurrentNormalizedTime);
                }
            }

            animator.Update(0f);
            for (int i = 0; i < count; i++)
            {
                AnimatorLayerState layer = layers[i];
                if (!layer.InTransition || layer.NextFullPathHash == 0)
                {
                    continue;
                }

                if (layer.TransitionUsesFixedDuration)
                {
                    animator.CrossFadeInFixedTime(
                        layer.NextFullPathHash,
                        Mathf.Max(0f, layer.TransitionDuration),
                        i,
                        Mathf.Max(
                            0f,
                            layer.NextNormalizedTime * layer.NextLength),
                        Mathf.Clamp01(layer.TransitionNormalizedTime));
                }
                else
                {
                    animator.CrossFade(
                        layer.NextFullPathHash,
                        Mathf.Max(0f, layer.TransitionDuration),
                        i,
                        layer.NextNormalizedTime,
                        Mathf.Clamp01(layer.TransitionNormalizedTime));
                }
            }

            animator.Update(0f);
        }

        private void RestorePresentationCheckpoint(
            AnimationCheckpoint checkpoint)
        {
            proxyAnimator.runtimeAnimatorController =
                checkpoint.PresentationController;
            if (checkpoint.PresentationController != null)
            {
                proxyAnimator.Rebind();
                proxyAnimator.Update(0f);
            }

            ParameterSnapshot[] snapshots =
                checkpoint.PresentationParameters;
            if (snapshots != null)
            {
                for (int i = 0; i < snapshots.Length; i++)
                {
                    snapshots[i].Apply(proxyAnimator);
                }
            }

            RestoreLayers(proxyAnimator, checkpoint.PresentationLayers);
            proxyAnimator.speed = 0f;
        }

        private void ApplyTransform(float sourceTime)
        {
            if (transformSamples.Count == 0 ||
                sourceTime < transformSamples[0].Time)
            {
                transformSampleActive = false;
                RefreshProxyVisibility();
                return;
            }

            int nextIndex = FindNextTransformSample(sourceTime);
            int previousIndex = Mathf.Max(0, nextIndex - 1);
            AnimationTransformSample previous =
                transformSamples[previousIndex];
            if (!previous.Active)
            {
                transformSampleActive = false;
                RefreshProxyVisibility();
                return;
            }

            transformSampleActive = true;
            RefreshProxyVisibility();

            if (nextIndex >= transformSamples.Count ||
                !transformSamples[nextIndex].Active)
            {
                ApplyTransformSample(previous);
                return;
            }

            AnimationTransformSample next = transformSamples[nextIndex];
            float duration = next.Time - previous.Time;
            float blend = duration <= ValueEpsilon
                ? 0f
                : Mathf.Clamp01((sourceTime - previous.Time) / duration);
            proxyRoot.SetPositionAndRotation(
                Vector3.Lerp(previous.Position, next.Position, blend),
                Quaternion.Slerp(previous.Rotation, next.Rotation, blend));
            proxyRoot.localScale = GetLocalScale(
                proxyRoot,
                Vector3.Lerp(previous.Scale, next.Scale, blend));
        }

        private int FindNextTransformSample(float sourceTime)
        {
            int low = 0;
            int high = transformSamples.Count;
            while (low < high)
            {
                int middle = low + ((high - low) / 2);
                if (transformSamples[middle].Time <= sourceTime)
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

        private void ApplyTransformSample(AnimationTransformSample sample)
        {
            proxyRoot.SetPositionAndRotation(sample.Position, sample.Rotation);
            proxyRoot.localScale = GetLocalScale(proxyRoot, sample.Scale);
        }

        private void RefreshProxyVisibility()
        {
            bool active = playbackActorActive && transformSampleActive;
            for (int i = 0; i < appearanceBindings.Length; i++)
            {
                appearanceBindings[i].SetActorActive(active);
            }
        }

        private void DisableProxyGameplayComponents()
        {
            proxyBehaviours =
                proxyRoot.GetComponentsInChildren<Behaviour>(true);
            for (int i = 0; i < proxyBehaviours.Length; i++)
            {
                Behaviour behaviour = proxyBehaviours[i];
                if (behaviour != null && behaviour != proxyAnimator)
                {
                    behaviour.enabled = false;
                }
            }

            proxyColliders =
                proxyRoot.GetComponentsInChildren<Collider>(true);
            for (int i = 0; i < proxyColliders.Length; i++)
            {
                proxyColliders[i].enabled = false;
            }

            proxyRigidbodies =
                proxyRoot.GetComponentsInChildren<Rigidbody>(true);
            for (int i = 0; i < proxyRigidbodies.Length; i++)
            {
                proxyRigidbodies[i].detectCollisions = false;
                proxyRigidbodies[i].isKinematic = true;
            }
        }

        private static Vector3 GetLocalScale(
            Transform target,
            Vector3 worldScale)
        {
            Transform parent = target.parent;
            Vector3 parentScale = parent == null
                ? Vector3.one
                : parent.lossyScale;
            return new Vector3(
                SafeDivide(worldScale.x, parentScale.x),
                SafeDivide(worldScale.y, parentScale.y),
                SafeDivide(worldScale.z, parentScale.z));
        }

        private static float SafeDivide(float value, float divisor)
        {
            return Mathf.Abs(divisor) <= 0.000001f
                ? value
                : value / divisor;
        }

        public void Dispose()
        {
            if (disposed)
            {
                return;
            }

            disposed = true;
            ReplayAnimatorProxyRegistry.Unregister(proxyAnimator);
            for (int i = 0; i < appearanceBindings.Length; i++)
            {
                appearanceBindings[i].Dispose();
            }

            if (proxyRootObject != null)
            {
                UnityEngine.Object.Destroy(proxyRootObject);
            }
        }

        private sealed class ParameterBinding
        {
            public int Hash { get; }
            public AnimatorControllerParameterType Type { get; }
            public bool Initialized;
            public float FloatValue;
            public bool BoolValue;
            public int IntValue;

            public ParameterBinding(
                int hash,
                AnimatorControllerParameterType type)
            {
                Hash = hash;
                Type = type;
            }
        }

        private sealed class AppearanceBinding : IDisposable
        {
            private readonly Renderer source;
            private readonly Renderer proxy;
            private readonly bool managedByAnimatorTrack;
            private readonly List<Material> sourceMaterials =
                new List<Material>(4);
            private readonly List<Material> proxyMaterials =
                new List<Material>(4);
            private readonly List<AppearanceSample> samples =
                new List<AppearanceSample>(16);
            private Color[] colorBuffer = Array.Empty<Color>();
            private bool actorActive = true;
            private bool hasAppliedSample;
            private bool lastSampleEnabled;

            public long EstimatedBytes
            {
                get
                {
                    long bytes = 0L;
                    for (int i = 0; i < samples.Count; i++)
                    {
                        bytes += AppearanceSampleBaseBytes +
                                 (samples[i].Colors == null
                                     ? 0L
                                     : samples[i].Colors.Length * 16L);
                    }

                    return bytes;
                }
            }

            public AppearanceBinding(Renderer sourceRenderer, Renderer proxyRenderer)
            {
                source = sourceRenderer;
                proxy = proxyRenderer;
                if (source == null || proxy == null)
                {
                    return;
                }

                managedByAnimatorTrack = source is SkinnedMeshRenderer;
                proxy.enabled = false;
                if (!managedByAnimatorTrack)
                {
                    // Rigid attachments (including held weapons) retain the
                    // existing low-volume Transform track so equipment swaps can
                    // spawn/despawn independently of the cloned skeleton.
                    return;
                }

                source.GetSharedMaterials(sourceMaterials);
                Material[] clones = new Material[sourceMaterials.Count];
                for (int i = 0; i < sourceMaterials.Count; i++)
                {
                    clones[i] = sourceMaterials[i] == null
                        ? null
                        : new Material(sourceMaterials[i]);
                }

                proxy.sharedMaterials = clones;
            }

            public void Capture(float sourceTime)
            {
                if (!managedByAnimatorTrack || source == null || proxy == null)
                {
                    return;
                }

                source.GetSharedMaterials(sourceMaterials);
                if (colorBuffer.Length != sourceMaterials.Count)
                {
                    colorBuffer = new Color[sourceMaterials.Count];
                }

                for (int i = 0; i < sourceMaterials.Count; i++)
                {
                    colorBuffer[i] = ReadMaterialColor(sourceMaterials[i]);
                }

                bool enabled = source.gameObject.activeInHierarchy &&
                               source.enabled;
                if (samples.Count > 0 &&
                    samples[samples.Count - 1].Matches(enabled, colorBuffer))
                {
                    return;
                }

                Color[] colors = samples.Count > 0 &&
                                 AppearanceSample.ColorsMatch(
                                     samples[samples.Count - 1].Colors,
                                     colorBuffer)
                    ? samples[samples.Count - 1].Colors
                    : (Color[])colorBuffer.Clone();
                samples.Add(new AppearanceSample(sourceTime, enabled, colors));
            }

            public void Apply(float sourceTime)
            {
                if (!managedByAnimatorTrack || proxy == null ||
                    samples.Count == 0 ||
                    sourceTime < samples[0].Time)
                {
                    if (proxy != null)
                    {
                        proxy.enabled = false;
                    }

                    return;
                }

                int low = 0;
                int high = samples.Count;
                while (low < high)
                {
                    int middle = low + ((high - low) / 2);
                    if (samples[middle].Time <= sourceTime)
                    {
                        low = middle + 1;
                    }
                    else
                    {
                        high = middle;
                    }
                }

                AppearanceSample sample = samples[Mathf.Max(0, low - 1)];
                hasAppliedSample = true;
                lastSampleEnabled = sample.Enabled;
                proxy.enabled = actorActive && lastSampleEnabled;
                ApplyColors(sample.Colors);
            }

            public void SetActorActive(bool active)
            {
                if (!managedByAnimatorTrack)
                {
                    return;
                }

                actorActive = active;
                if (proxy != null && hasAppliedSample)
                {
                    proxy.enabled = actorActive && lastSampleEnabled;
                }
                else if (!active && proxy != null)
                {
                    proxy.enabled = false;
                }
            }

            private void ApplyColors(Color[] colors)
            {
                if (colors == null)
                {
                    return;
                }

                proxy.GetSharedMaterials(proxyMaterials);
                int count = Mathf.Min(proxyMaterials.Count, colors.Length);
                for (int i = 0; i < count; i++)
                {
                    Material material = proxyMaterials[i];
                    if (material == null)
                    {
                        continue;
                    }

                    if (material.HasProperty("_BaseColor"))
                    {
                        material.SetColor("_BaseColor", colors[i]);
                    }

                    if (material.HasProperty("_Color"))
                    {
                        material.SetColor("_Color", colors[i]);
                    }
                }
            }

            private static Color ReadMaterialColor(Material material)
            {
                if (material == null)
                {
                    return Color.white;
                }

                if (material.HasProperty("_BaseColor"))
                {
                    return material.GetColor("_BaseColor");
                }

                return material.HasProperty("_Color")
                    ? material.GetColor("_Color")
                    : Color.white;
            }

            public void Dispose()
            {
                if (!managedByAnimatorTrack || proxy == null)
                {
                    return;
                }

                proxy.GetSharedMaterials(proxyMaterials);
                for (int i = 0; i < proxyMaterials.Count; i++)
                {
                    if (proxyMaterials[i] != null)
                    {
                        UnityEngine.Object.Destroy(proxyMaterials[i]);
                    }
                }
            }
        }

        private sealed class AnimationCheckpoint
        {
            public float SourceTime { get; }
            public float ReplayTime { get; }
            public AnimatorLayerState[] RecordedLayers { get; }
            public RuntimeAnimatorController PresentationController;
            public AnimatorLayerState[] PresentationLayers;
            public ParameterSnapshot[] PresentationParameters;

            public AnimationCheckpoint(
                float sourceTime,
                float replayTime,
                AnimatorLayerState[] recordedLayers)
            {
                SourceTime = sourceTime;
                ReplayTime = replayTime;
                RecordedLayers = recordedLayers ??
                                 Array.Empty<AnimatorLayerState>();
            }
        }

        private readonly struct AnimationTransformSample
        {
            public float Time { get; }
            public bool Active { get; }
            public Vector3 Position { get; }
            public Quaternion Rotation { get; }
            public Vector3 Scale { get; }

            public AnimationTransformSample(
                float time,
                bool active,
                Vector3 position,
                Quaternion rotation,
                Vector3 scale)
            {
                Time = time;
                Active = active;
                Position = position;
                Rotation = rotation;
                Scale = scale;
            }

            public bool Matches(
                bool active,
                Vector3 position,
                Quaternion rotation,
                Vector3 scale)
            {
                return Active == active &&
                       (Position - position).sqrMagnitude <= 0.00000001f &&
                       Quaternion.Dot(Rotation, rotation) >= 0.999999f &&
                       (Scale - scale).sqrMagnitude <= 0.00000001f;
            }
        }

        private readonly struct AppearanceSample
        {
            public float Time { get; }
            public bool Enabled { get; }
            public Color[] Colors { get; }

            public AppearanceSample(float time, bool enabled, Color[] colors)
            {
                Time = time;
                Enabled = enabled;
                Colors = colors;
            }

            public bool Matches(bool enabled, Color[] colors)
            {
                return Enabled == enabled && ColorsMatch(Colors, colors);
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
                    Color difference = left[i] - right[i];
                    if (Mathf.Abs(difference.r) + Mathf.Abs(difference.g) +
                        Mathf.Abs(difference.b) + Mathf.Abs(difference.a) >
                        0.0001f)
                    {
                        return false;
                    }
                }

                return true;
            }
        }

        private readonly struct AnimatorLayerState
        {
            public int CurrentFullPathHash { get; }
            public float CurrentNormalizedTime { get; }
            public float Weight { get; }
            public bool InTransition { get; }
            public int NextFullPathHash { get; }
            public float NextNormalizedTime { get; }
            public float NextLength { get; }
            public float TransitionDuration { get; }
            public float TransitionNormalizedTime { get; }
            public bool TransitionUsesFixedDuration { get; }

            public AnimatorLayerState(
                int currentFullPathHash,
                float currentNormalizedTime,
                float weight,
                bool inTransition,
                int nextFullPathHash,
                float nextNormalizedTime,
                float nextLength,
                float transitionDuration,
                float transitionNormalizedTime,
                bool transitionUsesFixedDuration)
            {
                CurrentFullPathHash = currentFullPathHash;
                CurrentNormalizedTime = currentNormalizedTime;
                Weight = weight;
                InTransition = inTransition;
                NextFullPathHash = nextFullPathHash;
                NextNormalizedTime = nextNormalizedTime;
                NextLength = nextLength;
                TransitionDuration = transitionDuration;
                TransitionNormalizedTime = transitionNormalizedTime;
                TransitionUsesFixedDuration = transitionUsesFixedDuration;
            }
        }

        private readonly struct ParameterSnapshot
        {
            private readonly int hash;
            private readonly AnimatorControllerParameterType type;
            private readonly float floatValue;
            private readonly int intValue;
            private readonly bool boolValue;

            private ParameterSnapshot(
                int parameterHash,
                AnimatorControllerParameterType parameterType,
                float capturedFloat,
                int capturedInt,
                bool capturedBool)
            {
                hash = parameterHash;
                type = parameterType;
                floatValue = capturedFloat;
                intValue = capturedInt;
                boolValue = capturedBool;
            }

            public static ParameterSnapshot Float(int hash, float value)
            {
                return new ParameterSnapshot(
                    hash,
                    AnimatorControllerParameterType.Float,
                    value,
                    0,
                    false);
            }

            public static ParameterSnapshot Int(int hash, int value)
            {
                return new ParameterSnapshot(
                    hash,
                    AnimatorControllerParameterType.Int,
                    0f,
                    value,
                    false);
            }

            public static ParameterSnapshot Bool(int hash, bool value)
            {
                return new ParameterSnapshot(
                    hash,
                    AnimatorControllerParameterType.Bool,
                    0f,
                    0,
                    value);
            }

            public void Apply(Animator animator)
            {
                switch (type)
                {
                    case AnimatorControllerParameterType.Float:
                        animator.SetFloat(hash, floatValue);
                        break;
                    case AnimatorControllerParameterType.Int:
                        animator.SetInteger(hash, intValue);
                        break;
                    case AnimatorControllerParameterType.Bool:
                        animator.SetBool(hash, boolValue);
                        break;
                }
            }
        }

        private enum AnimatorReplayEventKind : byte
        {
            Float,
            Bool,
            Int,
            SetTrigger,
            ResetTrigger,
            Controller,
            Active
        }

        private readonly struct AnimatorReplayEvent
        {
            public float SourceTime { get; }
            public float ReplayTime { get; }
            public AnimatorReplayEventKind Kind { get; }
            public int ParameterHash { get; }
            public float FloatValue { get; }
            public int IntValue { get; }
            public bool BoolValue { get; }
            public RuntimeAnimatorController Controller { get; }

            private AnimatorReplayEvent(
                float sourceTime,
                float replayTime,
                AnimatorReplayEventKind kind,
                int parameterHash,
                float floatValue,
                int intValue,
                bool boolValue,
                RuntimeAnimatorController controller)
            {
                SourceTime = sourceTime;
                ReplayTime = replayTime;
                Kind = kind;
                ParameterHash = parameterHash;
                FloatValue = floatValue;
                IntValue = intValue;
                BoolValue = boolValue;
                Controller = controller;
            }

            public static AnimatorReplayEvent FloatParameter(
                float sourceTime,
                float replayTime,
                int hash,
                float value)
            {
                return new AnimatorReplayEvent(
                    sourceTime,
                    replayTime,
                    AnimatorReplayEventKind.Float,
                    hash,
                    value,
                    0,
                    false,
                    null);
            }

            public static AnimatorReplayEvent BoolParameter(
                float sourceTime,
                float replayTime,
                int hash,
                bool value)
            {
                return new AnimatorReplayEvent(
                    sourceTime,
                    replayTime,
                    AnimatorReplayEventKind.Bool,
                    hash,
                    0f,
                    0,
                    value,
                    null);
            }

            public static AnimatorReplayEvent IntParameter(
                float sourceTime,
                float replayTime,
                int hash,
                int value)
            {
                return new AnimatorReplayEvent(
                    sourceTime,
                    replayTime,
                    AnimatorReplayEventKind.Int,
                    hash,
                    0f,
                    value,
                    false,
                    null);
            }

            public static AnimatorReplayEvent Trigger(
                float sourceTime,
                float replayTime,
                int hash,
                bool set)
            {
                return new AnimatorReplayEvent(
                    sourceTime,
                    replayTime,
                    set
                        ? AnimatorReplayEventKind.SetTrigger
                        : AnimatorReplayEventKind.ResetTrigger,
                    hash,
                    0f,
                    0,
                    false,
                    null);
            }

            public static AnimatorReplayEvent ControllerChanged(
                float sourceTime,
                float replayTime,
                RuntimeAnimatorController controller)
            {
                return new AnimatorReplayEvent(
                    sourceTime,
                    replayTime,
                    AnimatorReplayEventKind.Controller,
                    0,
                    0f,
                    0,
                    false,
                    controller);
            }

            public static AnimatorReplayEvent ActiveChanged(
                float sourceTime,
                float replayTime,
                bool active)
            {
                return new AnimatorReplayEvent(
                    sourceTime,
                    replayTime,
                    AnimatorReplayEventKind.Active,
                    0,
                    0f,
                    0,
                    active,
                    null);
            }
        }
    }
}
