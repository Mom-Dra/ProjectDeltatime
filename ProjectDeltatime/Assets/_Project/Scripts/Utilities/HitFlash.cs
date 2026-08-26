using Deltatime.Replay;
using UnityEngine;

namespace Deltatime.Utilities
{
    public sealed class HitFlash : MonoBehaviour
    {
        private const float Lifetime = 0.14f;
        private const int RingSegments = 20;

        private LineRenderer ring;
        private LineRenderer sparks;
        private Material ringMaterial;
        private Material sparkMaterial;
        private Color color;
        private float remaining;

        public static void Create(Vector3 position, Color flashColor)
        {
            Create(position, flashColor, Vector3.forward);
        }

        public static void Create(
            Vector3 position,
            Color flashColor,
            Vector3 direction)
        {
            GameObject flashObject = new GameObject("Hit Flash");
            flashObject.transform.position = position;
            Vector3 flatDirection = Vector3.ProjectOnPlane(direction, Vector3.up);
            if (flatDirection.sqrMagnitude > 0.000001f)
            {
                flashObject.transform.rotation = Quaternion.LookRotation(
                    flatDirection.normalized,
                    Vector3.up);
            }

            HitFlash flash = flashObject.AddComponent<HitFlash>();
            flash.Build(flashColor);
        }

        private void Build(Color flashColor)
        {
            color = flashColor;
            remaining = Lifetime;

            ring = gameObject.AddComponent<LineRenderer>();
            ConfigureLineRenderer(ring, out ringMaterial);
            ring.positionCount = RingSegments;
            ring.loop = true;
            ring.startWidth = 0.045f;
            ring.endWidth = 0.045f;
            for (int i = 0; i < RingSegments; i++)
            {
                float angle = i / (float)RingSegments * Mathf.PI * 2f;
                ring.SetPosition(
                    i,
                    new Vector3(
                        Mathf.Cos(angle) * 0.18f,
                        0.055f,
                        Mathf.Sin(angle) * 0.18f));
            }

            sparks = CreateLineRenderer("Sparks", out sparkMaterial);
            sparks.positionCount = 12;
            sparks.loop = false;
            sparks.startWidth = 0.055f;
            sparks.endWidth = 0.018f;
            sparks.SetPositions(new[]
            {
                new Vector3(-0.42f, 0.06f, 0f),
                new Vector3(-0.12f, 0.06f, 0f),
                new Vector3(0.12f, 0.06f, 0f),
                new Vector3(0.42f, 0.06f, 0f),
                new Vector3(0f, 0.06f, -0.42f),
                new Vector3(0f, 0.06f, -0.12f),
                new Vector3(0f, 0.06f, 0.12f),
                new Vector3(0f, 0.06f, 0.42f),
                new Vector3(-0.27f, 0.06f, -0.27f),
                new Vector3(-0.08f, 0.06f, -0.08f),
                new Vector3(0.08f, 0.06f, 0.08f),
                new Vector3(0.27f, 0.06f, 0.27f)
            });

            SetColor(color);
            ReplayVisualRegistry.Active?.RegisterRenderer(ring);
            ReplayVisualRegistry.Active?.RegisterRenderer(sparks);
        }

        private void Update()
        {
            remaining -= UnityEngine.Time.unscaledDeltaTime;
            float alpha = Mathf.Clamp01(remaining / Lifetime);
            float expansion = Mathf.Lerp(1.45f, 0.45f, alpha);
            transform.localScale = Vector3.one * expansion;
            Color faded = new Color(color.r, color.g, color.b, color.a * alpha);
            SetColor(faded);

            if (remaining <= 0f)
            {
                Destroy(gameObject);
            }
        }

        private void OnDestroy()
        {
            if (ringMaterial != null)
            {
                Destroy(ringMaterial);
            }

            if (sparkMaterial != null)
            {
                Destroy(sparkMaterial);
            }
        }

        private LineRenderer CreateLineRenderer(
            string rendererName,
            out Material runtimeMaterial)
        {
            GameObject rendererObject = new GameObject(rendererName);
            rendererObject.transform.SetParent(transform, false);
            LineRenderer renderer = rendererObject.AddComponent<LineRenderer>();
            ConfigureLineRenderer(renderer, out runtimeMaterial);
            return renderer;
        }

        private static void ConfigureLineRenderer(
            LineRenderer renderer,
            out Material runtimeMaterial)
        {
            renderer.useWorldSpace = false;
            renderer.alignment = LineAlignment.View;
            renderer.sortingOrder = 20;
            runtimeMaterial = null;
            Shader shader = Shader.Find("Sprites/Default");
            if (shader != null)
            {
                runtimeMaterial = new Material(shader);
                renderer.material = runtimeMaterial;
            }
        }

        private void SetColor(Color value)
        {
            if (ring != null)
            {
                ring.startColor = value;
                ring.endColor = value;
            }

            if (sparks != null)
            {
                sparks.startColor = value;
                sparks.endColor = new Color(
                    value.r,
                    value.g,
                    value.b,
                    value.a * 0.28f);
            }
        }
    }
}
