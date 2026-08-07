using UnityEngine;
using UnityEngine.Rendering;

namespace Deltatime.Level
{
    /// <summary>
    /// Keeps Stage 5's front exterior and foreground furniture from obscuring
    /// the player without changing collision or vision behaviour.
    /// </summary>
    public sealed class Stage5SouthExteriorCutaway : MonoBehaviour
    {
        [SerializeField] private Transform player;
        [SerializeField] private Camera gameplayCamera;
        [SerializeField] private Renderer[] occluders;
        [SerializeField, Min(0)] private int southExteriorOccluderCount;
        [SerializeField] private float hideBelowZ;
        [SerializeField] private float restoreAboveZ;
        [SerializeField, Min(0f)] private float playerSightRadius = 0.45f;

        private ShadowCastingMode[] originalShadowCastingModes;
        private bool[] hiddenStates;
        private bool cutawayActive;

        public bool IsCutawayActive => cutawayActive;
        public Renderer[] Occluders => occluders;
        public int SouthExteriorOccluderCount => southExteriorOccluderCount;
        public float HideBelowZ => hideBelowZ;
        public float RestoreAboveZ => restoreAboveZ;

        public void Configure(
            Transform followTarget,
            Camera camera,
            Renderer[] targetRenderers,
            int structuralRendererCount,
            float hideThreshold,
            float restoreThreshold,
            float sightRadius)
        {
            RestoreOriginalRendererModes();
            player = followTarget;
            gameplayCamera = camera;
            occluders = targetRenderers;
            southExteriorOccluderCount = Mathf.Clamp(
                structuralRendererCount,
                0,
                occluders?.Length ?? 0);
            hideBelowZ = hideThreshold;
            restoreAboveZ = Mathf.Max(restoreThreshold, hideThreshold);
            playerSightRadius = Mathf.Max(0f, sightRadius);
            CacheOriginalRendererModes();
            ApplyCutaway(false, false);
        }

        private void Awake()
        {
            CacheOriginalRendererModes();
        }

        private void OnEnable()
        {
            if (originalShadowCastingModes == null)
            {
                CacheOriginalRendererModes();
            }
        }

        private void LateUpdate()
        {
            EvaluateNow();
        }

        public void EvaluateNow()
        {
            if (player == null || occluders == null || occluders.Length == 0)
            {
                return;
            }

            bool southExteriorHidden = cutawayActive
                ? player.position.z < restoreAboveZ
                : player.position.z <= hideBelowZ;
            ApplyCutaway(southExteriorHidden, true);
        }

        private void OnDisable()
        {
            RestoreOriginalRendererModes();
        }

        private void OnDestroy()
        {
            RestoreOriginalRendererModes();
        }

        private void CacheOriginalRendererModes()
        {
            if (occluders == null)
            {
                originalShadowCastingModes = null;
                return;
            }

            originalShadowCastingModes = new ShadowCastingMode[occluders.Length];
            hiddenStates = new bool[occluders.Length];
            for (int i = 0; i < occluders.Length; i++)
            {
                Renderer renderer = occluders[i];
                originalShadowCastingModes[i] = renderer == null
                    ? ShadowCastingMode.On
                    : renderer.shadowCastingMode;
            }
        }

        private void ApplyCutaway(
            bool hideSouthExterior,
            bool evaluateForeground)
        {
            if (originalShadowCastingModes == null ||
                originalShadowCastingModes.Length != (occluders?.Length ?? 0))
            {
                CacheOriginalRendererModes();
            }

            if (occluders == null)
            {
                cutawayActive = false;
                return;
            }

            bool anyHidden = false;
            for (int i = 0; i < occluders.Length; i++)
            {
                Renderer renderer = occluders[i];
                if (renderer == null)
                {
                    continue;
                }

                bool hidden =
                    (i < southExteriorOccluderCount && hideSouthExterior) ||
                    (evaluateForeground && DoesRendererOccludePlayer(renderer));
                renderer.shadowCastingMode = hidden
                    ? ShadowCastingMode.ShadowsOnly
                    : originalShadowCastingModes[i];
                if (hiddenStates != null && i < hiddenStates.Length)
                {
                    hiddenStates[i] = hidden;
                }

                anyHidden |= hidden;
            }

            cutawayActive = anyHidden;
        }

        private bool DoesRendererOccludePlayer(Renderer renderer)
        {
            if (gameplayCamera == null || player == null || renderer == null)
            {
                return false;
            }

            Vector3 target = player.position + (Vector3.up * 0.55f);
            Vector3 direction = target - gameplayCamera.transform.position;
            float targetDistance = direction.magnitude;
            if (targetDistance <= 0.001f)
            {
                return false;
            }

            Bounds bounds = renderer.bounds;
            bounds.Expand(playerSightRadius * 2f);
            Ray ray = new Ray(
                gameplayCamera.transform.position,
                direction / targetDistance);
            return bounds.IntersectRay(ray, out float hitDistance) &&
                   hitDistance < targetDistance - 0.05f;
        }

        private void RestoreOriginalRendererModes()
        {
            if (occluders == null || originalShadowCastingModes == null)
            {
                cutawayActive = false;
                return;
            }

            int count = Mathf.Min(occluders.Length, originalShadowCastingModes.Length);
            for (int i = 0; i < count; i++)
            {
                if (occluders[i] != null)
                {
                    occluders[i].shadowCastingMode = originalShadowCastingModes[i];
                }
            }

            cutawayActive = false;
            if (hiddenStates != null)
            {
                System.Array.Clear(hiddenStates, 0, hiddenStates.Length);
            }
        }
    }
}
