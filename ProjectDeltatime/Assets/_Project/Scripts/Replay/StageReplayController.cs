using System;
using System.Collections.Generic;
using Deltatime.InputSystem;
using Deltatime.Level;
using Deltatime.TimeSystem;
using Deltatime.UI;
using UnityEngine;
using UnityEngine.Rendering;

namespace Deltatime.Replay
{
    [DefaultExecutionOrder(10000)]
    public sealed class StageReplayController : MonoBehaviour
    {
        private const string BaseColorProperty = "_BaseColor";
        private const string ColorProperty = "_Color";

        [SerializeField] private WorldTimeController worldTime;
        [SerializeField] private Camera gameplayCamera;
        [SerializeField, Min(1f)] private float captureRate = 20f;
        [SerializeField, Min(0f)] private float endHoldDuration = 0.65f;
        [SerializeField] private bool loop = true;

        private readonly List<CameraSample> cameraSamples =
            new List<CameraSample>(2048);
        private readonly Dictionary<int, VisualTrack> tracksByInstanceId =
            new Dictionary<int, VisualTrack>();
        private readonly List<VisualTrack> tracks = new List<VisualTrack>();
        private readonly Dictionary<int, LightTrack> lightTracksByInstanceId =
            new Dictionary<int, LightTrack>();
        private readonly List<LightTrack> lightTracks = new List<LightTrack>();
        private readonly HashSet<int> visibleRendererIds = new HashSet<int>();
        private readonly List<MonoBehaviour> disabledBehaviours =
            new List<MonoBehaviour>();

        private Transform replayRoot;
        private bool replayRequested;
        private float firstRecordedTime;
        private float lastRecordedTime;
        private float playbackTime;
        private float holdRemaining;
        private float captureAccumulator;

        public bool IsReplaying { get; private set; }
        public float CaptureRate => captureRate;
        public int CapturedFrameCount => cameraSamples.Count;
        public int TrackedLightCount => lightTracks.Count;
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
            cameraSamples.Count < 2
                ? 0f
                : Mathf.Max(0f, lastRecordedTime - firstRecordedTime);
        public float PlaybackElapsed =>
            IsReplaying ? Mathf.Max(0f, playbackTime - firstRecordedTime) : 0f;

        private void Awake()
        {
            EnsureReplayRoot();

            if (worldTime == null || gameplayCamera == null)
            {
                Debug.LogError(
                    $"{nameof(StageReplayController)} requires world time and a gameplay camera.",
                    this);
                enabled = false;
            }
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
            replayRoot.gameObject.SetActive(false);
        }

        private void Start()
        {
            if (enabled)
            {
                CaptureFrame();
            }
        }

        private void LateUpdate()
        {
            if (IsReplaying)
            {
                AdvanceReplay(UnityEngine.Time.unscaledDeltaTime);
                return;
            }

            float captureInterval = 1f / Mathf.Max(1f, captureRate);
            captureAccumulator += UnityEngine.Time.unscaledDeltaTime;
            bool captureDue = captureAccumulator >= captureInterval;

            if (captureDue)
            {
                captureAccumulator %= captureInterval;
                CaptureFrame();
            }

            if (replayRequested)
            {
                if (!captureDue)
                {
                    CaptureFrame();
                }

                BeginReplay();
            }
        }

        public void Configure(
            WorldTimeController timeSource,
            Camera targetCamera)
        {
            worldTime = timeSource;
            gameplayCamera = targetCamera;
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

        private void OnValidate()
        {
            captureRate = Mathf.Max(1f, captureRate);
            endHoldDuration = Mathf.Max(0f, endHoldDuration);
        }

        private void CaptureFrame()
        {
            float timestamp = worldTime.WorldElapsedTime;
            if (cameraSamples.Count > 0)
            {
                timestamp = Mathf.Max(
                    timestamp,
                    cameraSamples[cameraSamples.Count - 1].Time + 0.000001f);
            }

            if (cameraSamples.Count == 0)
            {
                firstRecordedTime = timestamp;
            }

            lastRecordedTime = timestamp;
            cameraSamples.Add(new CameraSample(
                timestamp,
                gameplayCamera.transform.position,
                gameplayCamera.transform.rotation,
                gameplayCamera.backgroundColor,
                gameplayCamera.fieldOfView));

            visibleRendererIds.Clear();
            Renderer[] renderers = FindObjectsByType<Renderer>(
                FindObjectsInactive.Exclude,
                FindObjectsSortMode.None);

            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer source = renderers[i];
                if (source == null ||
                    source.transform.IsChildOf(replayRoot) ||
                    !CanRecord(source))
                {
                    continue;
                }

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

                track.Capture(timestamp);
            }

            for (int i = 0; i < tracks.Count; i++)
            {
                VisualTrack track = tracks[i];
                if (!visibleRendererIds.Contains(track.InstanceId))
                {
                    track.CaptureHidden(timestamp);
                }
            }

            for (int i = 0; i < lightTracks.Count; i++)
            {
                lightTracks[i].Capture(timestamp);
            }
        }

        private void BeginReplay()
        {
            replayRequested = false;
            if (cameraSamples.Count == 0 || tracks.Count == 0)
            {
                Debug.LogWarning("Replay could not start because no frames were captured.", this);
                return;
            }

            DisableLiveSimulation();

            for (int i = 0; i < tracks.Count; i++)
            {
                tracks[i].HideSource();
            }

            for (int i = 0; i < lightTracks.Count; i++)
            {
                lightTracks[i].HideSource();
            }

            replayRoot.gameObject.SetActive(true);
            IsReplaying = true;
            playbackTime = firstRecordedTime;
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
                ApplyReplay(firstRecordedTime);
                return;
            }

            if (holdRemaining > 0f)
            {
                holdRemaining = Mathf.Max(0f, holdRemaining - realDeltaTime);
                if (holdRemaining > 0f || !loop)
                {
                    return;
                }

                playbackTime = firstRecordedTime;
            }
            else
            {
                playbackTime += realDeltaTime;
            }

            if (playbackTime >= lastRecordedTime)
            {
                playbackTime = lastRecordedTime;
                ApplyReplay(playbackTime);
                holdRemaining = endHoldDuration;
                return;
            }

            ApplyReplay(playbackTime);
        }

        private void ApplyReplay(float timestamp)
        {
            ApplyCamera(timestamp);
            for (int i = 0; i < tracks.Count; i++)
            {
                tracks[i].Apply(timestamp);
            }

            for (int i = 0; i < lightTracks.Count; i++)
            {
                lightTracks[i].Apply(timestamp);
            }
        }

        private void ApplyCamera(float timestamp)
        {
            if (cameraSamples.Count == 0 || gameplayCamera == null)
            {
                return;
            }

            int nextIndex = FindNextCameraSample(timestamp);
            int previousIndex = Mathf.Max(0, nextIndex - 1);
            CameraSample previous = cameraSamples[previousIndex];

            if (nextIndex >= cameraSamples.Count)
            {
                SetCamera(previous);
                return;
            }

            CameraSample next = cameraSamples[nextIndex];
            float duration = next.Time - previous.Time;
            float blend = duration <= 0.000001f
                ? 0f
                : Mathf.Clamp01((timestamp - previous.Time) / duration);

            gameplayCamera.transform.SetPositionAndRotation(
                Vector3.Lerp(previous.Position, next.Position, blend),
                Quaternion.Slerp(previous.Rotation, next.Rotation, blend));
            gameplayCamera.backgroundColor =
                Color.Lerp(previous.BackgroundColor, next.BackgroundColor, blend);
            gameplayCamera.fieldOfView =
                Mathf.Lerp(previous.FieldOfView, next.FieldOfView, blend);
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

        private VisualTrack CreateTrack(Renderer source)
        {
            Renderer proxy = CreateProxyRenderer(source);
            if (proxy == null)
            {
                return null;
            }

            proxy.transform.SetParent(replayRoot, false);
            proxy.gameObject.name = $"Replay - {source.gameObject.name}";
            return new VisualTrack(source, proxy);
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
                   source is SkinnedMeshRenderer ||
                   source is LineRenderer;
        }

        private static Renderer CreateProxyRenderer(Renderer source)
        {
            if (source is LineRenderer sourceLine)
            {
                return CreateLineProxy(sourceLine);
            }

            Mesh mesh = null;
            if (source is SkinnedMeshRenderer skinned)
            {
                mesh = new Mesh { name = $"Replay Mesh - {source.name}" };
                skinned.BakeMesh(mesh);
            }
            else
            {
                MeshFilter sourceFilter = source.GetComponent<MeshFilter>();
                if (sourceFilter != null && sourceFilter.sharedMesh != null)
                {
                    mesh = Instantiate(sourceFilter.sharedMesh);
                    mesh.name = $"Replay Mesh - {sourceFilter.sharedMesh.name}";
                }
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

        private void OnDestroy()
        {
            for (int i = 0; i < tracks.Count; i++)
            {
                tracks[i].Dispose();
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
                new List<LightSample>(512);

            public bool IsProxyActive =>
                proxy != null &&
                proxy.enabled &&
                proxy.gameObject.activeInHierarchy;
            public bool IsSourceEnabled =>
                source != null &&
                source.enabled &&
                source.gameObject.activeInHierarchy;

            public LightTrack(Light sourceLight, Light proxyLight)
            {
                source = sourceLight;
                proxy = proxyLight;
            }

            public void Capture(float timestamp)
            {
                if (!IsSourceEnabled)
                {
                    CaptureHidden(timestamp);
                    return;
                }

                Vector3 position = source.transform.position;
                Quaternion rotation = source.transform.rotation;
                Color color = source.color;
                float intensity = source.intensity;
                float range = source.range;
                float spotAngle = source.spotAngle;
                float innerSpotAngle = source.innerSpotAngle;

                if (samples.Count > 0 &&
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

            private void CaptureHidden(float timestamp)
            {
                if (samples.Count == 0 || !samples[samples.Count - 1].Visible)
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
            private readonly List<VisualSample> samples =
                new List<VisualSample>(512);
            private readonly List<Material> sourceMaterials =
                new List<Material>(4);
            private readonly List<Material> proxyMaterials =
                new List<Material>(4);
            private Color[] materialColorBuffer = Array.Empty<Color>();
            private Vector3[] linePositionBuffer;

            public int InstanceId { get; }

            public VisualTrack(Renderer sourceRenderer, Renderer proxyRenderer)
            {
                source = sourceRenderer;
                proxy = proxyRenderer;
                sourceLine = sourceRenderer as LineRenderer;
                proxyLine = proxyRenderer as LineRenderer;
                InstanceId = sourceRenderer.GetInstanceID();
                proxy.enabled = false;
            }

            public void Capture(float timestamp)
            {
                bool visible = source != null &&
                               source.enabled &&
                               source.gameObject.activeInHierarchy;
                if (!visible)
                {
                    CaptureHidden(timestamp);
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
                if (samples.Count > 0 &&
                    samples[samples.Count - 1].Matches(
                        true,
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

                samples.Add(new VisualSample(
                    timestamp,
                    true,
                    position,
                    rotation,
                    scale,
                    (Color[])materialColorBuffer.Clone(),
                    linePositionBuffer == null
                        ? null
                        : (Vector3[])linePositionBuffer.Clone(),
                    startColor,
                    endColor));
            }

            public void CaptureHidden(float timestamp)
            {
                if (samples.Count == 0 || !samples[samples.Count - 1].Visible)
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

            public void Apply(float timestamp)
            {
                if (samples.Count == 0 || timestamp < samples[0].Time)
                {
                    proxy.enabled = false;
                    return;
                }

                int nextIndex = FindNextSample(timestamp);
                int previousIndex = Mathf.Max(0, nextIndex - 1);
                VisualSample previous = samples[previousIndex];

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

        private sealed class VisualSample
        {
            public float Time { get; }
            public bool Visible { get; }
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
                    Vector3.zero,
                    Quaternion.identity,
                    Vector3.one,
                    null,
                    null,
                    Color.white,
                    Color.white);
            }

            public bool Matches(
                bool visible,
                Vector3 position,
                Quaternion rotation,
                Vector3 scale,
                Color[] materialColors,
                Vector3[] linePositions,
                Color startColor,
                Color endColor)
            {
                if (Visible != visible ||
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

            private static bool ColorsMatch(Color[] left, Color[] right)
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

            private static bool PositionsMatch(Vector3[] left, Vector3[] right)
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
