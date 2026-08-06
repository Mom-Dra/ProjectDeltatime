using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Deltatime.Performance
{
    /// <summary>
    /// Stage 6-only runtime rendering budget. The copied Synty rooftop remains
    /// intact in the saved scene; this component limits the costly shadow work
    /// only while Neon Overlook is being played and restores global quality state
    /// when the scene unloads.
    /// </summary>
    public sealed class Stage6PerformanceController : MonoBehaviour
    {
        [Header("Stage 6 References")]
        [SerializeField] private Transform environmentRoot;
        [SerializeField] private Transform player;

        [Header("Shadow Budget")]
        [SerializeField, Min(0f)] private float shadowDistance = 40f;
        [SerializeField, Range(1, 4)] private int maximumShadowCascades = 2;
        [SerializeField] private ShadowResolution maximumShadowResolution =
            ShadowResolution.Medium;
        [SerializeField, Range(0, 8)]
        private int maximumShadowedEnvironmentPointLights = 2;
        [SerializeField, Min(0.05f)]
        private float environmentShadowSelectionInterval = 0.25f;

        private readonly List<RendererShadowState> backgroundRenderers =
            new List<RendererShadowState>();
        private readonly List<LightShadowState> environmentPointLights =
            new List<LightShadowState>();

        private QualitySettingsSnapshot qualitySnapshot;
        private bool qualitySnapshotCaptured;
        private bool runtimeBudgetApplied;
        private float selectionElapsed;

        public Transform EnvironmentRoot => environmentRoot;
        public Transform Player => player;
        public float ShadowDistance => shadowDistance;
        public int MaximumShadowCascades => maximumShadowCascades;
        public ShadowResolution MaximumShadowResolution => maximumShadowResolution;
        public int MaximumShadowedEnvironmentPointLights =>
            maximumShadowedEnvironmentPointLights;
        public float EnvironmentShadowSelectionInterval =>
            environmentShadowSelectionInterval;
        public bool IsRuntimePerformanceBudgetApplied => runtimeBudgetApplied;
        public int EnvironmentPointLightCount => environmentPointLights.Count;
        public int ActiveEnvironmentShadowedPointLightCount
        {
            get
            {
                int count = 0;
                for (int i = 0; i < environmentPointLights.Count; i++)
                {
                    Light light = environmentPointLights[i].Light;
                    if (light != null && light.isActiveAndEnabled &&
                        light.shadows != LightShadows.None)
                    {
                        count++;
                    }
                }

                return count;
            }
        }

        /// <summary>
        /// Builder-facing setup kept explicit so re-running the Stage 6 builder
        /// never adds duplicate controllers or relies on reflection.
        /// </summary>
        public void Configure(
            Transform stageEnvironmentRoot,
            Transform playerTransform,
            float configuredShadowDistance,
            int configuredMaximumCascades,
            ShadowResolution configuredMaximumResolution,
            int configuredPointLightBudget,
            float configuredSelectionInterval)
        {
            environmentRoot = stageEnvironmentRoot;
            player = playerTransform;
            shadowDistance = Mathf.Max(0f, configuredShadowDistance);
            maximumShadowCascades = Mathf.Clamp(configuredMaximumCascades, 1, 4);
            maximumShadowResolution = configuredMaximumResolution;
            maximumShadowedEnvironmentPointLights =
                Mathf.Clamp(configuredPointLightBudget, 0, 8);
            environmentShadowSelectionInterval =
                Mathf.Max(0.05f, configuredSelectionInterval);
        }

        private void OnEnable()
        {
            if (!Application.isPlaying)
            {
                return;
            }

            CacheRuntimeTargets();
            CaptureAndApplyQualitySettings();
            ApplyBackgroundShadowPolicy();
            RebalanceEnvironmentPointLightShadows();
            runtimeBudgetApplied = true;
        }

        private void Update()
        {
            if (!runtimeBudgetApplied)
            {
                return;
            }

            selectionElapsed += Time.unscaledDeltaTime;
            if (selectionElapsed < environmentShadowSelectionInterval)
            {
                return;
            }

            selectionElapsed %= environmentShadowSelectionInterval;
            RebalanceEnvironmentPointLightShadows();
        }

        private void OnDisable()
        {
            RestoreRuntimeState();
        }

        private void OnDestroy()
        {
            RestoreRuntimeState();
        }

        private void CacheRuntimeTargets()
        {
            backgroundRenderers.Clear();
            environmentPointLights.Clear();
            selectionElapsed = 0f;

            Transform backgroundCity = FindDescendant(environmentRoot, "BackgroundCity");
            if (backgroundCity != null)
            {
                Renderer[] renderers =
                    backgroundCity.GetComponentsInChildren<Renderer>(true);
                for (int i = 0; i < renderers.Length; i++)
                {
                    Renderer renderer = renderers[i];
                    if (renderer != null)
                    {
                        backgroundRenderers.Add(new RendererShadowState(renderer));
                    }
                }
            }

            if (environmentRoot == null)
            {
                return;
            }

            Light[] lights = environmentRoot.GetComponentsInChildren<Light>(true);
            for (int i = 0; i < lights.Length; i++)
            {
                Light light = lights[i];
                if (light != null && light.type == LightType.Point &&
                    light.shadows != LightShadows.None)
                {
                    environmentPointLights.Add(new LightShadowState(light));
                }
            }
        }

        private void CaptureAndApplyQualitySettings()
        {
            qualitySnapshot = new QualitySettingsSnapshot(
                QualitySettings.shadowDistance,
                QualitySettings.shadowCascades,
                QualitySettings.shadowResolution);
            qualitySnapshotCaptured = true;

            QualitySettings.shadowDistance = shadowDistance;
            QualitySettings.shadowCascades = Mathf.Min(
                QualitySettings.shadowCascades,
                maximumShadowCascades);
            if ((int)QualitySettings.shadowResolution >
                (int)maximumShadowResolution)
            {
                QualitySettings.shadowResolution = maximumShadowResolution;
            }
        }

        private void ApplyBackgroundShadowPolicy()
        {
            for (int i = 0; i < backgroundRenderers.Count; i++)
            {
                Renderer renderer = backgroundRenderers[i].Renderer;
                if (renderer == null)
                {
                    continue;
                }

                renderer.shadowCastingMode = ShadowCastingMode.Off;
                renderer.receiveShadows = false;
            }
        }

        private void RebalanceEnvironmentPointLightShadows()
        {
            int closest = -1;
            int secondClosest = -1;
            float closestDistance = float.PositiveInfinity;
            float secondClosestDistance = float.PositiveInfinity;
            Vector3 playerPosition = player == null
                ? Vector3.zero
                : player.position;

            for (int i = 0; i < environmentPointLights.Count; i++)
            {
                Light light = environmentPointLights[i].Light;
                if (light == null || !light.isActiveAndEnabled)
                {
                    continue;
                }

                float distance = (light.transform.position - playerPosition).sqrMagnitude;
                if (distance < closestDistance)
                {
                    secondClosest = closest;
                    secondClosestDistance = closestDistance;
                    closest = i;
                    closestDistance = distance;
                }
                else if (distance < secondClosestDistance)
                {
                    secondClosest = i;
                    secondClosestDistance = distance;
                }
            }

            for (int i = 0; i < environmentPointLights.Count; i++)
            {
                LightShadowState state = environmentPointLights[i];
                Light light = state.Light;
                if (light == null)
                {
                    continue;
                }

                bool keepShadow = maximumShadowedEnvironmentPointLights > 0 &&
                    i == closest;
                if (maximumShadowedEnvironmentPointLights > 1 && i == secondClosest)
                {
                    keepShadow = true;
                }

                light.shadows = keepShadow ? state.OriginalShadows : LightShadows.None;
            }
        }

        private void RestoreRuntimeState()
        {
            if (!runtimeBudgetApplied && !qualitySnapshotCaptured)
            {
                return;
            }

            for (int i = 0; i < backgroundRenderers.Count; i++)
            {
                backgroundRenderers[i].Restore();
            }

            for (int i = 0; i < environmentPointLights.Count; i++)
            {
                environmentPointLights[i].Restore();
            }

            if (qualitySnapshotCaptured)
            {
                qualitySnapshot.Restore();
            }

            runtimeBudgetApplied = false;
            qualitySnapshotCaptured = false;
        }

        private static Transform FindDescendant(Transform root, string name)
        {
            if (root == null)
            {
                return null;
            }

            Transform[] transforms = root.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < transforms.Length; i++)
            {
                if (transforms[i].name == name)
                {
                    return transforms[i];
                }
            }

            return null;
        }

        private readonly struct RendererShadowState
        {
            public readonly Renderer Renderer;
            private readonly ShadowCastingMode shadowCastingMode;
            private readonly bool receiveShadows;

            public RendererShadowState(Renderer renderer)
            {
                Renderer = renderer;
                shadowCastingMode = renderer.shadowCastingMode;
                receiveShadows = renderer.receiveShadows;
            }

            public void Restore()
            {
                if (Renderer == null)
                {
                    return;
                }

                Renderer.shadowCastingMode = shadowCastingMode;
                Renderer.receiveShadows = receiveShadows;
            }
        }

        private readonly struct LightShadowState
        {
            public readonly Light Light;
            public readonly LightShadows OriginalShadows;

            public LightShadowState(Light light)
            {
                Light = light;
                OriginalShadows = light.shadows;
            }

            public void Restore()
            {
                if (Light != null)
                {
                    Light.shadows = OriginalShadows;
                }
            }
        }

        private readonly struct QualitySettingsSnapshot
        {
            private readonly float shadowDistance;
            private readonly int shadowCascades;
            private readonly ShadowResolution shadowResolution;

            public QualitySettingsSnapshot(
                float originalShadowDistance,
                int originalShadowCascades,
                ShadowResolution originalShadowResolution)
            {
                shadowDistance = originalShadowDistance;
                shadowCascades = originalShadowCascades;
                shadowResolution = originalShadowResolution;
            }

            public void Restore()
            {
                QualitySettings.shadowDistance = shadowDistance;
                QualitySettings.shadowCascades = shadowCascades;
                QualitySettings.shadowResolution = shadowResolution;
            }
        }
    }
}
