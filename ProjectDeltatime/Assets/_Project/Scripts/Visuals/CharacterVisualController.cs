using System;
using System.Collections.Generic;
using UnityEngine;

namespace Deltatime.Visuals
{
    /// <summary>
    /// Owns a character prefab used as a visual child of a gameplay collider.
    /// The gameplay root remains the authority for physics, while this component
    /// applies visibility and colour feedback to every renderer in the model.
    /// </summary>
    public sealed class CharacterVisualController : MonoBehaviour
    {
        private const string BaseColorProperty = "_BaseColor";
        private const string ColorProperty = "_Color";

        [SerializeField] private Transform visualRoot;
        [SerializeField] private Renderer[] visualRenderers = Array.Empty<Renderer>();

        private readonly List<MaterialColor> originalColors =
            new List<MaterialColor>();

        public Transform VisualRoot => visualRoot;

        public void Configure(Transform root)
        {
            visualRoot = root;
            RefreshRenderers();
        }

        public void RefreshRenderers()
        {
            visualRenderers = visualRoot == null
                ? Array.Empty<Renderer>()
                : visualRoot.GetComponentsInChildren<Renderer>(true);
            originalColors.Clear();
        }

        public bool ContainsRenderer(Renderer renderer)
        {
            if (renderer == null)
            {
                return false;
            }

            for (int i = 0; i < visualRenderers.Length; i++)
            {
                if (visualRenderers[i] == renderer)
                {
                    return true;
                }
            }

            return false;
        }

        public void SetVisible(bool visible)
        {
            for (int i = 0; i < visualRenderers.Length; i++)
            {
                Renderer renderer = visualRenderers[i];
                if (renderer != null)
                {
                    renderer.enabled = visible;
                }
            }
        }

        public void SetTint(Color color)
        {
            CaptureOriginalColors();
            for (int i = 0; i < originalColors.Count; i++)
            {
                SetMaterialColor(originalColors[i].Material, color);
            }
        }

        public void RestoreTint()
        {
            for (int i = 0; i < originalColors.Count; i++)
            {
                MaterialColor original = originalColors[i];
                SetMaterialColor(original.Material, original.Color);
            }
        }

        private void CaptureOriginalColors()
        {
            if (originalColors.Count > 0)
            {
                return;
            }

            HashSet<Material> captured = new HashSet<Material>();
            for (int i = 0; i < visualRenderers.Length; i++)
            {
                Renderer renderer = visualRenderers[i];
                if (renderer == null)
                {
                    continue;
                }

                Material[] materials = renderer.materials;
                for (int j = 0; j < materials.Length; j++)
                {
                    Material material = materials[j];
                    if (material == null || !captured.Add(material) ||
                        !TryGetMaterialColor(material, out Color color))
                    {
                        continue;
                    }

                    originalColors.Add(new MaterialColor(material, color));
                }
            }
        }

        private static bool TryGetMaterialColor(
            Material material,
            out Color color)
        {
            if (material != null && material.HasProperty(BaseColorProperty))
            {
                color = material.GetColor(BaseColorProperty);
                return true;
            }

            if (material != null && material.HasProperty(ColorProperty))
            {
                color = material.GetColor(ColorProperty);
                return true;
            }

            color = Color.white;
            return false;
        }

        private static void SetMaterialColor(Material material, Color color)
        {
            if (material == null)
            {
                return;
            }

            if (material.HasProperty(BaseColorProperty))
            {
                material.SetColor(BaseColorProperty, color);
            }
            else if (material.HasProperty(ColorProperty))
            {
                material.SetColor(ColorProperty, color);
            }
        }

        private readonly struct MaterialColor
        {
            public readonly Material Material;
            public readonly Color Color;

            public MaterialColor(Material material, Color color)
            {
                Material = material;
                Color = color;
            }
        }
    }
}
