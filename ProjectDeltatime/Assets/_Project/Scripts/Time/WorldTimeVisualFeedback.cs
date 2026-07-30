using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace Deltatime.TimeSystem
{
    [ExecuteAlways]
    public sealed class WorldTimeVisualFeedback : MonoBehaviour
    {
        [SerializeField] private WorldTimeController worldTime;
        [SerializeField] private Camera gameplayCamera;
        [SerializeField] private Light directionalKeyLight;

        [Header("Environment Lighting")]
        [SerializeField] private Color ambientSkyColor =
            new Color(0.012f, 0.016f, 0.022f, 1f);
        [SerializeField] private Color ambientEquatorColor =
            new Color(0.006f, 0.008f, 0.012f, 1f);
        [SerializeField] private Color ambientGroundColor =
            new Color(0.002f, 0.003f, 0.005f, 1f);
        [SerializeField, Min(0f)] private float ambientIntensity = 0.35f;
        [SerializeField, Min(0f)] private float reflectionIntensity = 0.08f;
        [SerializeField, Min(0f)] private float directionalLightIntensity = 0.06f;
        [SerializeField] private Color fogColor =
            new Color(0.004f, 0.007f, 0.012f, 1f);
        [SerializeField, Min(0f)] private float fogStartDistance = 19f;
        [SerializeField, Min(0f)] private float fogEndDistance = 42f;

        [Header("Map Fill Lights")]
        [SerializeField] private Color mapFillLightColor =
            new Color(0.48f, 0.58f, 0.72f, 1f);
        [SerializeField, Min(0f)] private float mapFillLightIntensity;
        [SerializeField, Min(0.1f)] private float mapFillLightRange = 7.5f;
        [SerializeField, Range(1f, 179f)] private float mapFillLightSpotAngle = 90f;
        [SerializeField] private Vector3[] mapFillLightPositions =
        {
            new Vector3(-6f, 3.6f, -4.5f),
            new Vector3(0f, 3.6f, -4.5f),
            new Vector3(6f, 3.6f, -4.5f),
            new Vector3(-6f, 3.6f, 4.5f),
            new Vector3(0f, 3.6f, 4.5f),
            new Vector3(6f, 3.6f, 4.5f)
        };

        [Header("World Time Feedback")]
        [SerializeField] private Color nearlyStoppedColor =
            new Color(0.012f, 0.014f, 0.017f, 1f);
        [SerializeField] private Color activeColor =
            new Color(0.004f, 0.008f, 0.014f, 1f);
        [SerializeField, Min(0.01f)] private float colorBlendSpeed = 7f;

        private const string MapFillRootName = "Runtime Map Fill Lights";
        private GameObject mapFillRoot;
        private Light[] mapFillLights;

        private void Awake()
        {
            ResolveDirectionalKeyLight();
            ApplyEnvironmentLighting();
            EnsureMapFillLights();

            if (!Application.isPlaying)
            {
                return;
            }

            if (worldTime == null || gameplayCamera == null)
            {
                Debug.LogError(
                    $"{nameof(WorldTimeVisualFeedback)} requires world time and a camera.",
                    this);
                enabled = false;
            }
        }

        private void OnEnable()
        {
            if (!Application.isPlaying)
            {
                ResolveDirectionalKeyLight();
                ApplyEnvironmentLighting();
                EnsureMapFillLights();
            }
        }

        private void Update()
        {
            if (!Application.isPlaying)
            {
                return;
            }

            Color target = Color.Lerp(
                nearlyStoppedColor,
                activeColor,
                Mathf.Clamp01(worldTime.CurrentTimeScale));
            float blend = 1f - Mathf.Exp(-colorBlendSpeed * UnityEngine.Time.unscaledDeltaTime);
            gameplayCamera.backgroundColor = Color.Lerp(
                gameplayCamera.backgroundColor,
                target,
                blend);
        }

        private void OnGUI()
        {
            if (!Application.isPlaying)
            {
                return;
            }

            float slowAmount = 1f - Mathf.Clamp01(worldTime.CurrentTimeScale);
            if (slowAmount <= 0.001f)
            {
                return;
            }

            int previousDepth = GUI.depth;
            Color previousColor = GUI.color;
            GUI.depth = 1000;
            GUI.color = new Color(0.02f, 0.025f, 0.035f, slowAmount * 0.22f);
            GUI.DrawTexture(
                new Rect(0f, 0f, Screen.width, Screen.height),
                Texture2D.whiteTexture);
            GUI.color = previousColor;
            GUI.depth = previousDepth;
        }

        public void Configure(WorldTimeController timeSource, Camera targetCamera)
        {
            Configure(timeSource, targetCamera, null);
        }

        public void Configure(
            WorldTimeController timeSource,
            Camera targetCamera,
            Light keyLight)
        {
            worldTime = timeSource;
            gameplayCamera = targetCamera;
            directionalKeyLight = keyLight;
            ResolveDirectionalKeyLight();
            ApplyEnvironmentLighting();
            EnsureMapFillLights();
        }

        private void ResolveDirectionalKeyLight()
        {
            if (directionalKeyLight != null)
            {
                return;
            }

            GameObject keyLightObject = GameObject.Find("Directional Key Light");
            if (keyLightObject != null)
            {
                directionalKeyLight = keyLightObject.GetComponent<Light>();
            }
        }

        private void ApplyEnvironmentLighting()
        {
            if (!Application.isPlaying &&
                gameObject.scene != SceneManager.GetActiveScene())
            {
                return;
            }

            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = ambientSkyColor;
            RenderSettings.ambientEquatorColor = ambientEquatorColor;
            RenderSettings.ambientGroundColor = ambientGroundColor;
            RenderSettings.ambientIntensity = ambientIntensity;
            RenderSettings.reflectionIntensity = reflectionIntensity;
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = fogColor;
            RenderSettings.fogStartDistance = fogStartDistance;
            RenderSettings.fogEndDistance = fogEndDistance;

            if (directionalKeyLight != null)
            {
                directionalKeyLight.intensity = directionalLightIntensity;
                directionalKeyLight.shadows = LightShadows.None;
            }
        }

        private void EnsureMapFillLights()
        {
            int requestedCount = mapFillLightPositions != null
                ? mapFillLightPositions.Length
                : 0;

            if (mapFillRoot == null)
            {
                GameObject existingRoot = GameObject.Find(MapFillRootName);
                if (existingRoot != null &&
                    existingRoot.scene == gameObject.scene)
                {
                    mapFillRoot = existingRoot;
                }
            }

            if (mapFillRoot != null)
            {
                mapFillLights = mapFillRoot.GetComponentsInChildren<Light>(true);
                if (mapFillLights.Length != requestedCount)
                {
                    DestroyMapFillLights();
                }
            }

            if (mapFillRoot == null && requestedCount > 0)
            {
                mapFillRoot = new GameObject(MapFillRootName)
                {
                    hideFlags = HideFlags.DontSave
                };

                if (mapFillRoot.scene != gameObject.scene &&
                    gameObject.scene.IsValid())
                {
                    SceneManager.MoveGameObjectToScene(
                        mapFillRoot,
                        gameObject.scene);
                }

                mapFillLights = new Light[requestedCount];
                for (int i = 0; i < requestedCount; i++)
                {
                    GameObject lightObject =
                        new GameObject($"Map Fill Light {i + 1}")
                        {
                            hideFlags = HideFlags.DontSave
                        };
                    lightObject.transform.SetParent(mapFillRoot.transform, false);
                    mapFillLights[i] = lightObject.AddComponent<Light>();
                }
            }

            UpdateMapFillLights();
        }

        private void UpdateMapFillLights()
        {
            if (mapFillLights == null ||
                mapFillLightPositions == null ||
                mapFillLights.Length != mapFillLightPositions.Length)
            {
                return;
            }

            Quaternion downwardRotation = Quaternion.Euler(90f, 0f, 0f);
            for (int i = 0; i < mapFillLights.Length; i++)
            {
                Light fillLight = mapFillLights[i];
                fillLight.transform.SetPositionAndRotation(
                    mapFillLightPositions[i],
                    downwardRotation);
                fillLight.type = LightType.Spot;
                fillLight.color = mapFillLightColor;
                fillLight.intensity = mapFillLightIntensity;
                fillLight.range = mapFillLightRange;
                fillLight.spotAngle = mapFillLightSpotAngle;
                fillLight.innerSpotAngle = mapFillLightSpotAngle * 0.72f;
                fillLight.shadows = LightShadows.None;
                fillLight.renderMode = LightRenderMode.ForcePixel;
            }
        }

        private void DestroyMapFillLights()
        {
            if (mapFillRoot == null)
            {
                mapFillLights = null;
                return;
            }

            if (Application.isPlaying)
            {
                Destroy(mapFillRoot);
            }
            else
            {
                DestroyImmediate(mapFillRoot);
            }

            mapFillRoot = null;
            mapFillLights = null;
        }

        private void OnValidate()
        {
            ambientIntensity = Mathf.Max(0f, ambientIntensity);
            reflectionIntensity = Mathf.Max(0f, reflectionIntensity);
            directionalLightIntensity = Mathf.Max(0f, directionalLightIntensity);
            fogStartDistance = Mathf.Max(0f, fogStartDistance);
            fogEndDistance = Mathf.Max(
                fogStartDistance + 0.01f,
                fogEndDistance);
            mapFillLightIntensity = Mathf.Max(0f, mapFillLightIntensity);
            mapFillLightRange = Mathf.Max(0.1f, mapFillLightRange);
            mapFillLightSpotAngle = Mathf.Clamp(
                mapFillLightSpotAngle,
                1f,
                179f);

            if (isActiveAndEnabled)
            {
                ResolveDirectionalKeyLight();
                ApplyEnvironmentLighting();
                EnsureMapFillLights();
            }
        }

        private void OnDisable()
        {
            DestroyMapFillLights();
        }
    }
}
