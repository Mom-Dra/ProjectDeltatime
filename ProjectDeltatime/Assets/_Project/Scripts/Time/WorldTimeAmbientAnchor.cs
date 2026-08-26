using Deltatime.Audio;
using UnityEngine;

namespace Deltatime.TimeSystem
{
    [DisallowMultipleComponent]
    public sealed class WorldTimeAmbientAnchor : MonoBehaviour
    {
        [Header("World Time")]
        [SerializeField] private WorldTimeController worldTime;
        [SerializeField] private Transform rotatingPart;
        [SerializeField] private Vector3 localRotationAxis = Vector3.up;
        [SerializeField, Min(0f)] private float rotationDegreesPerWorldSecond =
            240f;

        [Header("Ambient Loop")]
        [SerializeField] private AudioSource loopSource;
        [SerializeField] private AudioLowPassFilter lowPassFilter;
        [SerializeField, Min(0f)] private float baseVolume = 0.22f;
        [SerializeField, Min(0.01f)] private float responseDuration = 0.15f;

        private float audibleTimeScale;

        public float CurrentPitch { get; private set; }
        public float CurrentCutoffFrequency { get; private set; }
        public float CurrentOutputVolume { get; private set; }
        public bool IsLoopPlaying =>
            loopSource != null && loopSource.isPlaying;

        private void Awake()
        {
            if (!ValidateConfiguration())
            {
                enabled = false;
                return;
            }

            audibleTimeScale = TargetAudibleTimeScale();
            ApplyAudioState();
        }

        private void OnEnable()
        {
            if (loopSource == null || loopSource.clip == null)
            {
                return;
            }

            loopSource.loop = true;
            loopSource.volume = 0f;
            SetDeterministicPlaybackOffset();
            loopSource.Play();
        }

        private void Update()
        {
            if (worldTime == null)
            {
                return;
            }

            RotateWithWorldTime();

            audibleTimeScale = WorldTimeAmbientState.AdvanceAudibleTimeScale(
                audibleTimeScale,
                TargetAudibleTimeScale(),
                UnityEngine.Time.unscaledDeltaTime,
                responseDuration);
            ApplyAudioState();
        }

        public void Configure(WorldTimeController timeSource)
        {
            worldTime = timeSource;
        }

        internal void ConfigurePresentationForTests(
            Transform targetRotatingPart,
            AudioSource targetLoopSource,
            AudioLowPassFilter targetLowPassFilter)
        {
            rotatingPart = targetRotatingPart;
            loopSource = targetLoopSource;
            lowPassFilter = targetLowPassFilter;
        }

        private void RotateWithWorldTime()
        {
            if (rotatingPart == null || worldTime.WorldDeltaTime <= 0f)
            {
                return;
            }

            rotatingPart.Rotate(
                localRotationAxis,
                rotationDegreesPerWorldSecond * worldTime.WorldDeltaTime,
                Space.Self);
        }

        private float TargetAudibleTimeScale()
        {
            return worldTime == null || worldTime.IsHardFrozen
                ? 0f
                : Mathf.Clamp01(worldTime.CurrentTimeScale);
        }

        private void ApplyAudioState()
        {
            WorldTimeAmbientAudioState state =
                WorldTimeAmbientState.Evaluate(audibleTimeScale);
            CurrentPitch = state.Pitch;
            CurrentCutoffFrequency = state.CutoffFrequency;

            float userVolume = 1f;
            SoundManager soundManager = SoundManager.Instance;
            if (soundManager != null)
            {
                userVolume = soundManager.UserMasterVolume *
                             soundManager.UserSfxVolume;
            }

            CurrentOutputVolume = baseVolume * state.VolumeFactor * userVolume;
            if (loopSource != null)
            {
                loopSource.pitch = CurrentPitch;
                loopSource.volume = CurrentOutputVolume;
            }

            if (lowPassFilter != null)
            {
                lowPassFilter.cutoffFrequency = CurrentCutoffFrequency;
            }
        }

        private void SetDeterministicPlaybackOffset()
        {
            AudioClip clip = loopSource.clip;
            if (clip == null || clip.length <= 0.01f)
            {
                return;
            }

            Vector3 position = transform.position;
            float normalizedOffset = Mathf.Repeat(
                Mathf.Abs(position.x * 0.137f + position.z * 0.193f),
                1f);
            loopSource.time = normalizedOffset * clip.length;
        }

        private bool ValidateConfiguration()
        {
            if (worldTime != null && rotatingPart != null &&
                loopSource != null && lowPassFilter != null &&
                loopSource.clip != null)
            {
                return true;
            }

            Debug.LogError(
                $"{nameof(WorldTimeAmbientAnchor)} is missing world time, " +
                "rotating part, loop source, low-pass filter, or audio clip.",
                this);
            return false;
        }

        private void OnDisable()
        {
            if (loopSource != null)
            {
                loopSource.Stop();
                loopSource.volume = 0f;
            }

            CurrentOutputVolume = 0f;
        }

        private void OnValidate()
        {
            if (localRotationAxis.sqrMagnitude <= 0.0001f)
            {
                localRotationAxis = Vector3.up;
            }
            else
            {
                localRotationAxis.Normalize();
            }

            rotationDegreesPerWorldSecond = Mathf.Max(
                0f,
                rotationDegreesPerWorldSecond);
            baseVolume = Mathf.Max(0f, baseVolume);
            responseDuration = Mathf.Max(0.01f, responseDuration);
        }
    }

    internal readonly struct WorldTimeAmbientAudioState
    {
        internal WorldTimeAmbientAudioState(
            float pitch,
            float cutoffFrequency,
            float volumeFactor)
        {
            Pitch = pitch;
            CutoffFrequency = cutoffFrequency;
            VolumeFactor = volumeFactor;
        }

        internal float Pitch { get; }
        internal float CutoffFrequency { get; }
        internal float VolumeFactor { get; }
    }

    internal static class WorldTimeAmbientState
    {
        internal static WorldTimeAmbientAudioState Evaluate(float timeScale)
        {
            float scale = Mathf.Clamp01(timeScale);
            float perceptualScale = Mathf.Sqrt(scale);
            return new WorldTimeAmbientAudioState(
                Mathf.Lerp(0.45f, 1f, perceptualScale),
                Mathf.Lerp(500f, 16000f, scale),
                perceptualScale);
        }

        internal static float AdvanceAudibleTimeScale(
            float current,
            float target,
            float unscaledDeltaTime,
            float responseDuration)
        {
            return Mathf.MoveTowards(
                Mathf.Clamp01(current),
                Mathf.Clamp01(target),
                Mathf.Max(0f, unscaledDeltaTime) /
                Mathf.Max(0.01f, responseDuration));
        }
    }
}
