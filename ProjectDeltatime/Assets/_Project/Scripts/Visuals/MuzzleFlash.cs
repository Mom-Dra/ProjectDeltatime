using Deltatime.Replay;
using UnityEngine;

namespace Deltatime.Visuals
{
    public sealed class MuzzleFlash : MonoBehaviour
    {
        private const float Lifetime = 0.07f;

        private LineRenderer line;
        private Material material;
        private Color color;
        private float remaining;

        public static void Create(
            Transform muzzle,
            Color flashColor,
            float size)
        {
            if (muzzle == null || size <= 0f)
            {
                return;
            }

            GameObject flashObject = new GameObject("Muzzle Flash");
            flashObject.transform.SetPositionAndRotation(
                muzzle.position,
                muzzle.rotation);
            MuzzleFlash flash = flashObject.AddComponent<MuzzleFlash>();
            flash.Build(flashColor, size);
        }

        private void Build(Color flashColor, float size)
        {
            color = new Color(
                Mathf.Max(0.8f, flashColor.r),
                Mathf.Max(0.65f, flashColor.g),
                Mathf.Max(0.25f, flashColor.b),
                1f);
            remaining = Lifetime;

            line = gameObject.AddComponent<LineRenderer>();
            line.useWorldSpace = false;
            line.alignment = LineAlignment.View;
            line.positionCount = 9;
            line.loop = false;
            line.startWidth = size * 0.2f;
            line.endWidth = size * 0.055f;
            line.sortingOrder = 22;
            float radius = size * 0.5f;
            line.SetPositions(new[]
            {
                new Vector3(-radius, 0f, 0f),
                Vector3.zero,
                new Vector3(radius, 0f, 0f),
                Vector3.zero,
                new Vector3(0f, -radius, 0f),
                Vector3.zero,
                new Vector3(0f, radius, 0f),
                Vector3.zero,
                new Vector3(0f, 0f, size * 0.65f)
            });

            Shader shader = Shader.Find("Sprites/Default");
            if (shader != null)
            {
                material = new Material(shader);
                line.material = material;
            }

            line.startColor = color;
            line.endColor = new Color(color.r, color.g, color.b, 0.15f);
            ReplayVisualRegistry.Active?.RegisterRenderer(line);
        }

        private void Update()
        {
            remaining -= UnityEngine.Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(remaining / Lifetime);
            transform.localScale = Vector3.one * Mathf.Lerp(0.55f, 1f, progress);
            Color faded = new Color(
                color.r,
                color.g,
                color.b,
                color.a * progress);
            line.startColor = faded;
            line.endColor = new Color(
                faded.r,
                faded.g,
                faded.b,
                faded.a * 0.15f);
            if (remaining <= 0f)
            {
                Destroy(gameObject);
            }
        }

        private void OnDestroy()
        {
            if (material != null)
            {
                Destroy(material);
            }
        }
    }
}
