using Deltatime.Player;
using UnityEngine;

namespace Deltatime.TimeSystem
{
    [DefaultExecutionOrder(-200)]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Camera))]
    public sealed class DeadlineVisualFeedback : MonoBehaviour
    {
        public enum VisualPhase
        {
            Inactive,
            Entering,
            Active,
            Releasing
        }

        private const string ShaderResourcePath =
            "Shaders/DeadlineScreenEffect";
        private const float EnterDuration = 0.14f;
        private const float ReleaseDuration = 0.24f;
        private const int ActionNodeCapacity = 2;
        private const int ActionNodeTextureSize = 32;
        private const float ActionNodeSize = 20f;
        private const float ActionNodeGap = 8f;

        private static readonly int BlendId = Shader.PropertyToID("_Blend");
        private static readonly int SaturationId =
            Shader.PropertyToID("_Saturation");
        private static readonly int TintColorId =
            Shader.PropertyToID("_TintColor");
        private static readonly int TintStrengthId =
            Shader.PropertyToID("_TintStrength");
        private static readonly int VignetteStrengthId =
            Shader.PropertyToID("_VignetteStrength");
        private static readonly int GrainStrengthId =
            Shader.PropertyToID("_GrainStrength");
        private static readonly int EffectCenterId =
            Shader.PropertyToID("_EffectCenter");
        private static readonly int AimCenterId =
            Shader.PropertyToID("_AimCenter");
        private static readonly int ScreenAspectId =
            Shader.PropertyToID("_ScreenAspect");
        private static readonly int RingRadiusId =
            Shader.PropertyToID("_RingRadius");
        private static readonly int RingStrengthId =
            Shader.PropertyToID("_RingStrength");
        private static readonly int FlashStrengthId =
            Shader.PropertyToID("_FlashStrength");
        private static readonly int UnscaledTimeId =
            Shader.PropertyToID("_UnscaledTime");

        private readonly Color tintColor =
            new Color(0.66f, 0.92f, 1f, 1f);
        private readonly Color actionNodeColor =
            new Color(0.2f, 0.95f, 1f, 1f);
        private readonly Color rejectedActionColor =
            new Color(1f, 0.52f, 0.15f, 1f);

        private Camera gameplayCamera;
        private DeadlineController deadline;
        private PlayerAim playerAim;
        private Material effectMaterial;
        private Texture2D actionNodeFillTexture;
        private Texture2D actionNodeRingTexture;
        private float phaseElapsed;
        private float releaseStartBlend;
        private bool subscribed;
        private static bool shaderResourceErrorLogged;

        public VisualPhase CurrentPhase { get; private set; }
        public float EffectBlend { get; private set; }
        public int DisplayedActionCount =>
            deadline == null ||
            !deadline.IsActive ||
            CurrentPhase == VisualPhase.Releasing
            ? 0
            : Mathf.Clamp(
                deadline.StagedActionCount,
                0,
                ActionNodeCapacity);
        public bool IsShaderReady =>
            effectMaterial != null &&
            effectMaterial.shader != null &&
            effectMaterial.shader.isSupported;
        public bool IsVisualActive =>
            CurrentPhase != VisualPhase.Inactive ||
            EffectBlend > 0.001f;

        private void Awake()
        {
            gameplayCamera = GetComponent<Camera>();
            EnsureResources();
            ResetVisualState();
        }

        private void OnEnable()
        {
            Subscribe();
            if (deadline != null && deadline.IsActive)
            {
                BeginEnter();
            }
        }

        private void Update()
        {
            float deltaTime = UnityEngine.Time.unscaledDeltaTime;
            switch (CurrentPhase)
            {
                case VisualPhase.Entering:
                    phaseElapsed += deltaTime;
                    EffectBlend = Smooth01(phaseElapsed / EnterDuration);
                    if (phaseElapsed >= EnterDuration)
                    {
                        CurrentPhase = VisualPhase.Active;
                        phaseElapsed = 0f;
                        EffectBlend = 1f;
                    }
                    break;

                case VisualPhase.Active:
                    EffectBlend = 1f;
                    if (deadline == null || !deadline.IsActive)
                    {
                        ResetVisualState();
                    }
                    break;

                case VisualPhase.Releasing:
                    phaseElapsed += deltaTime;
                    EffectBlend = releaseStartBlend *
                        (1f - Smooth01(phaseElapsed / ReleaseDuration));
                    if (phaseElapsed >= ReleaseDuration)
                    {
                        ResetVisualState();
                    }
                    break;
            }
        }

        private void OnRenderImage(RenderTexture source, RenderTexture destination)
        {
            if (!IsShaderReady || !IsVisualActive)
            {
                Graphics.Blit(source, destination);
                return;
            }

            float progress = GetPhaseProgress();
            float ringRadius = 0f;
            float ringStrength = 0f;
            float flashStrength = 0f;
            if (CurrentPhase == VisualPhase.Entering)
            {
                ringRadius = Mathf.Lerp(1.08f, 0.12f, progress);
                ringStrength = Mathf.Sin(progress * Mathf.PI);
                flashStrength = Mathf.Pow(1f - progress, 3f) * 0.34f;
            }
            else if (CurrentPhase == VisualPhase.Releasing)
            {
                ringRadius = Mathf.Lerp(0.08f, 1.16f, progress);
                ringStrength = Mathf.Sin(progress * Mathf.PI);
                flashStrength = Mathf.Sin(progress * Mathf.PI) * 0.08f;
            }

            Vector2 effectCenter = GetPlayerViewportPosition();
            effectMaterial.SetFloat(BlendId, EffectBlend);
            effectMaterial.SetFloat(SaturationId, 0.55f);
            effectMaterial.SetColor(TintColorId, tintColor);
            effectMaterial.SetFloat(TintStrengthId, 0.18f);
            effectMaterial.SetFloat(VignetteStrengthId, 0.32f);
            effectMaterial.SetFloat(GrainStrengthId, 0.014f);
            effectMaterial.SetVector(EffectCenterId, effectCenter);
            effectMaterial.SetVector(AimCenterId, GetAimViewportPosition(effectCenter));
            effectMaterial.SetFloat(
                ScreenAspectId,
                source.height > 0
                    ? (float)source.width / source.height
                    : 1f);
            effectMaterial.SetFloat(RingRadiusId, ringRadius);
            effectMaterial.SetFloat(RingStrengthId, ringStrength);
            effectMaterial.SetFloat(FlashStrengthId, flashStrength);
            effectMaterial.SetFloat(
                UnscaledTimeId,
                UnityEngine.Time.unscaledTime);

            Graphics.Blit(source, destination, effectMaterial);
        }

        private void OnGUI()
        {
            if (!Application.isPlaying ||
                !IsShaderReady ||
                deadline == null ||
                !deadline.IsActive ||
                CurrentPhase == VisualPhase.Releasing)
            {
                return;
            }

            EnsureNodeTextures();
            if (actionNodeFillTexture == null || actionNodeRingTexture == null)
            {
                return;
            }

            Vector3 anchor = gameplayCamera.WorldToScreenPoint(
                deadline.transform.position + Vector3.up * 2f);
            if (anchor.z <= 0f)
            {
                return;
            }

            float totalWidth =
                ActionNodeCapacity * ActionNodeSize +
                (ActionNodeCapacity - 1) * ActionNodeGap;
            float startX = Mathf.Clamp(
                anchor.x - totalWidth * 0.5f,
                8f,
                Mathf.Max(8f, Screen.width - totalWidth - 8f));
            float y = Mathf.Clamp(
                Screen.height - anchor.y - ActionNodeSize * 0.5f,
                8f,
                Mathf.Max(8f, Screen.height - ActionNodeSize - 8f));

            bool rejected = deadline.RejectedActionFeedback;
            float rejectionPulse = 0.72f +
                Mathf.Sin(UnityEngine.Time.unscaledTime * 36f) * 0.28f;
            Color nodeColor = rejected
                ? new Color(
                    rejectedActionColor.r,
                    rejectedActionColor.g,
                    rejectedActionColor.b,
                    rejectionPulse)
                : actionNodeColor;

            int previousDepth = GUI.depth;
            Color previousColor = GUI.color;
            GUI.depth = 500;
            for (int i = 0; i < ActionNodeCapacity; i++)
            {
                Rect nodeRect = new Rect(
                    startX + i * (ActionNodeSize + ActionNodeGap),
                    y,
                    ActionNodeSize,
                    ActionNodeSize);
                GUI.color = new Color(
                    nodeColor.r,
                    nodeColor.g,
                    nodeColor.b,
                    nodeColor.a * 0.72f);
                GUI.DrawTexture(nodeRect, actionNodeRingTexture);

                if (i < DisplayedActionCount)
                {
                    Rect fillRect = new Rect(
                        nodeRect.x + 3f,
                        nodeRect.y + 3f,
                        nodeRect.width - 6f,
                        nodeRect.height - 6f);
                    GUI.color = nodeColor;
                    GUI.DrawTexture(fillRect, actionNodeFillTexture);
                }
            }

            GUI.color = previousColor;
            GUI.depth = previousDepth;
        }

        public void Configure(DeadlineController deadlineController)
        {
            if (deadline == deadlineController)
            {
                Subscribe();
                return;
            }

            Unsubscribe();
            deadline = deadlineController;
            playerAim = deadline != null
                ? deadline.GetComponent<PlayerAim>()
                : null;
            Subscribe();
            ResetVisualState();
            if (isActiveAndEnabled && deadline != null && deadline.IsActive)
            {
                BeginEnter();
            }
        }

        private void HandleDeadlineActivated()
        {
            BeginEnter();
        }

        private void HandleDeadlineReleased()
        {
            if (deadline != null && deadline.ReleasedThisFrame)
            {
                BeginRelease();
                return;
            }

            ResetVisualState();
        }

        private void BeginEnter()
        {
            CurrentPhase = VisualPhase.Entering;
            phaseElapsed = 0f;
            EffectBlend = 0f;
        }

        private void BeginRelease()
        {
            CurrentPhase = VisualPhase.Releasing;
            phaseElapsed = 0f;
            EffectBlend = Mathf.Clamp01(EffectBlend);
            releaseStartBlend = EffectBlend;
        }

        private void ResetVisualState()
        {
            CurrentPhase = VisualPhase.Inactive;
            phaseElapsed = 0f;
            EffectBlend = 0f;
            releaseStartBlend = 0f;
        }

        private float GetPhaseProgress()
        {
            if (CurrentPhase == VisualPhase.Entering)
            {
                return Mathf.Clamp01(phaseElapsed / EnterDuration);
            }

            if (CurrentPhase == VisualPhase.Releasing)
            {
                return Mathf.Clamp01(phaseElapsed / ReleaseDuration);
            }

            return CurrentPhase == VisualPhase.Active ? 1f : 0f;
        }

        private Vector2 GetPlayerViewportPosition()
        {
            if (gameplayCamera == null || deadline == null)
            {
                return new Vector2(0.5f, 0.5f);
            }

            Vector3 viewportPosition = gameplayCamera.WorldToViewportPoint(
                deadline.transform.position + Vector3.up * 0.8f);
            if (viewportPosition.z <= 0f)
            {
                return new Vector2(0.5f, 0.5f);
            }

            return new Vector2(
                Mathf.Clamp01(viewportPosition.x),
                Mathf.Clamp01(viewportPosition.y));
        }

        private Vector2 GetAimViewportPosition(Vector2 fallback)
        {
            if (gameplayCamera == null || playerAim == null)
            {
                return fallback;
            }

            Vector3 viewportPosition = gameplayCamera.WorldToViewportPoint(
                playerAim.AimPoint);
            if (viewportPosition.z <= 0f)
            {
                return fallback;
            }

            return new Vector2(
                Mathf.Clamp01(viewportPosition.x),
                Mathf.Clamp01(viewportPosition.y));
        }

        private void EnsureResources()
        {
            if (effectMaterial != null)
            {
                return;
            }

            Shader shader = Resources.Load<Shader>(ShaderResourcePath);
            if (shader == null || !shader.isSupported)
            {
                if (!shaderResourceErrorLogged)
                {
                    string reason = shader == null
                        ? "is missing"
                        : "is not supported";
                    Debug.LogError(
                        $"DEADLINE screen-effect shader Resources/{ShaderResourcePath} {reason}.",
                        this);
                    shaderResourceErrorLogged = true;
                }

                return;
            }

            effectMaterial = new Material(shader)
            {
                name = "Runtime DEADLINE Screen Effect",
                hideFlags = HideFlags.HideAndDontSave
            };
        }

        private void EnsureNodeTextures()
        {
            if (actionNodeFillTexture == null)
            {
                actionNodeFillTexture = CreateNodeTexture(false);
            }

            if (actionNodeRingTexture == null)
            {
                actionNodeRingTexture = CreateNodeTexture(true);
            }
        }

        private static Texture2D CreateNodeTexture(bool ringOnly)
        {
            Texture2D texture = new Texture2D(
                ActionNodeTextureSize,
                ActionNodeTextureSize,
                TextureFormat.RGBA32,
                false,
                true)
            {
                name = ringOnly
                    ? "Runtime DEADLINE Action Node Ring"
                    : "Runtime DEADLINE Action Node Fill",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.HideAndDontSave
            };

            Color32[] pixels = new Color32[
                ActionNodeTextureSize * ActionNodeTextureSize];
            for (int y = 0; y < ActionNodeTextureSize; y++)
            {
                for (int x = 0; x < ActionNodeTextureSize; x++)
                {
                    Vector2 position = new Vector2(
                        (x + 0.5f) / ActionNodeTextureSize - 0.5f,
                        (y + 0.5f) / ActionNodeTextureSize - 0.5f);
                    float distance = position.magnitude;
                    float outer = 1f - Mathf.SmoothStep(0.43f, 0.5f, distance);
                    float alpha = outer;
                    if (ringOnly)
                    {
                        float inner = 1f - Mathf.SmoothStep(
                            0.30f,
                            0.37f,
                            distance);
                        alpha *= 1f - inner;
                    }

                    pixels[y * ActionNodeTextureSize + x] = new Color32(
                        255,
                        255,
                        255,
                        (byte)Mathf.RoundToInt(Mathf.Clamp01(alpha) * 255f));
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            return texture;
        }

        private void Subscribe()
        {
            if (subscribed || deadline == null || !isActiveAndEnabled)
            {
                return;
            }

            deadline.Activated += HandleDeadlineActivated;
            deadline.Released += HandleDeadlineReleased;
            subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!subscribed || deadline == null)
            {
                subscribed = false;
                return;
            }

            deadline.Activated -= HandleDeadlineActivated;
            deadline.Released -= HandleDeadlineReleased;
            subscribed = false;
        }

        private static float Smooth01(float value)
        {
            value = Mathf.Clamp01(value);
            return value * value * (3f - 2f * value);
        }

        private void OnDisable()
        {
            Unsubscribe();
            ResetVisualState();
        }

        private void OnDestroy()
        {
            Unsubscribe();
            DestroyRuntimeObject(effectMaterial);
            DestroyRuntimeObject(actionNodeFillTexture);
            DestroyRuntimeObject(actionNodeRingTexture);
            effectMaterial = null;
            actionNodeFillTexture = null;
            actionNodeRingTexture = null;
        }

        private static void DestroyRuntimeObject(Object target)
        {
            if (target == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Destroy(target);
            }
            else
            {
                DestroyImmediate(target);
            }
        }
    }
}
