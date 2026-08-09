using Deltatime.Combat;
using UnityEngine;

namespace Deltatime.Visuals
{
    /// <summary>
    /// Displays a weapon definition's world model while the weapon is in
    /// flight, replacing the prototype cube when a model is available.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class WeaponFlightVisualPresenter : MonoBehaviour
    {
        private const string FlightModelName = "Flying Weapon Model";

        private GameObject displayedModel;

        public bool HasCustomModel => displayedModel != null;

        public void Apply(WeaponDefinition definition, Renderer fallbackRenderer)
        {
            ClearCustomModel();

            if (definition == null)
            {
                SetFallbackVisible(fallbackRenderer, false);
                return;
            }

            if (!definition.HasCustomWorldVisual)
            {
                SetFallbackVisible(fallbackRenderer, true);
                return;
            }

            SetFallbackVisible(fallbackRenderer, false);
            displayedModel = Instantiate(definition.WorldVisualPrefab, transform, false);
            displayedModel.name = FlightModelName;

            Transform modelTransform = displayedModel.transform;
            modelTransform.localPosition = definition.WorldModelLocalPosition;
            modelTransform.localRotation = Quaternion.Euler(
                definition.WorldModelLocalEulerAngles);
            modelTransform.localScale = definition.WorldModelLocalScale;
        }

        private void OnDestroy()
        {
            ClearCustomModel();
        }

        private void ClearCustomModel()
        {
            if (displayedModel == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Destroy(displayedModel);
            }
            else
            {
                DestroyImmediate(displayedModel);
            }

            displayedModel = null;
        }

        private static void SetFallbackVisible(Renderer fallbackRenderer, bool visible)
        {
            if (fallbackRenderer != null)
            {
                fallbackRenderer.enabled = visible;
            }
        }
    }
}
