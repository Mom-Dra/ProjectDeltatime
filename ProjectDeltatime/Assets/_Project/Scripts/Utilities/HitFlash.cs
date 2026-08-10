using Deltatime.Replay;
using UnityEngine;

namespace Deltatime.Utilities
{
    public sealed class HitFlash : MonoBehaviour
    {
        private const float Lifetime = 0.12f;

        private LineRenderer line;
        private Material material;
        private Color color;
        private float remaining;

        public static void Create(Vector3 position, Color flashColor)
        {
            GameObject flashObject = new GameObject("Hit Flash");
            flashObject.transform.position = position;
            HitFlash flash = flashObject.AddComponent<HitFlash>();
            flash.Build(flashColor);
        }

        private void Build(Color flashColor)
        {
            color = flashColor;
            remaining = Lifetime;

            line = gameObject.AddComponent<LineRenderer>();
            line.useWorldSpace = false;
            line.positionCount = 5;
            line.loop = false;
            line.startWidth = 0.08f;
            line.endWidth = 0.02f;
            line.sortingOrder = 20;
            line.SetPositions(new[]
            {
                new Vector3(-0.35f, 0.05f, 0f),
                new Vector3(0.35f, 0.05f, 0f),
                new Vector3(0f, 0.05f, 0f),
                new Vector3(0f, 0.05f, -0.35f),
                new Vector3(0f, 0.05f, 0.35f)
            });

            Shader shader = Shader.Find("Sprites/Default");
            if (shader != null)
            {
                material = new Material(shader);
                line.material = material;
            }

            line.startColor = color;
            line.endColor = color;
            StageReplayController.ActiveRecorder?.RegisterRenderer(line);
        }

        private void Update()
        {
            remaining -= UnityEngine.Time.unscaledDeltaTime;
            float alpha = Mathf.Clamp01(remaining / Lifetime);
            Color faded = new Color(color.r, color.g, color.b, color.a * alpha);
            line.startColor = faded;
            line.endColor = faded;

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
