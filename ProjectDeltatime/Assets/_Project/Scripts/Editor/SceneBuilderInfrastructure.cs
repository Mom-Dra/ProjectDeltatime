using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

namespace Deltatime.EditorTools
{
    internal static class SceneBuildCommand
    {
        public static void Run(Action buildAndValidate)
        {
            try
            {
                buildAndValidate();
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                if (Application.isBatchMode)
                {
                    EditorApplication.Exit(1);
                    return;
                }

                throw;
            }
        }
    }

    internal static class SceneValidation
    {
        public static void Require(bool condition, string message)
        {
            if (!condition)
            {
                throw new InvalidOperationException(message);
            }
        }

        public static GameObject FindRoot(Scene scene, string rootName)
        {
            GameObject[] roots = scene.GetRootGameObjects();
            for (int index = 0; index < roots.Length; index++)
            {
                if (roots[index].name == rootName)
                {
                    return roots[index];
                }
            }

            return null;
        }
    }

    internal static class CharacterSceneSetup
    {
        public static void DisableColliders(GameObject root)
        {
            if (root == null)
            {
                return;
            }

            Collider[] colliders = root.GetComponentsInChildren<Collider>(true);
            for (int index = 0; index < colliders.Length; index++)
            {
                colliders[index].enabled = false;
            }
        }
    }

    internal static class NavigationSceneSetup
    {
        public static bool IsOnNavMesh(
            Vector3 position,
            float maximumDistance = 1.5f)
        {
            return NavMesh.SamplePosition(
                position,
                out _,
                maximumDistance,
                NavMesh.AllAreas);
        }

        public static bool HasCompletePath(
            Vector3 from,
            Vector3 to,
            float sampleDistance,
            out NavMeshPathStatus status)
        {
            status = NavMeshPathStatus.PathInvalid;
            if (!NavMesh.SamplePosition(
                    from,
                    out NavMeshHit fromHit,
                    sampleDistance,
                    NavMesh.AllAreas) ||
                !NavMesh.SamplePosition(
                    to,
                    out NavMeshHit toHit,
                    sampleDistance,
                    NavMesh.AllAreas))
            {
                return false;
            }

            NavMeshPath path = new NavMeshPath();
            bool calculated = NavMesh.CalculatePath(
                fromHit.position,
                toHit.position,
                NavMesh.AllAreas,
                path);
            status = path.status;
            return calculated && status == NavMeshPathStatus.PathComplete;
        }
    }

    internal static class PreviewCapture
    {
        public static void CapturePng(
            Camera camera,
            int width,
            int height,
            string outputPath,
            Action beforeRender = null)
        {
            if (camera == null)
            {
                throw new ArgumentNullException(nameof(camera));
            }

            RenderTexture target = new RenderTexture(width, height, 24);
            Texture2D preview = new Texture2D(
                width,
                height,
                TextureFormat.RGB24,
                false);
            RenderTexture previousActive = RenderTexture.active;
            RenderTexture previousTarget = camera.targetTexture;

            try
            {
                beforeRender?.Invoke();
                camera.targetTexture = target;
                RenderTexture.active = target;
                camera.Render();
                preview.ReadPixels(new Rect(0f, 0f, width, height), 0, 0);
                preview.Apply();
                Directory.CreateDirectory(Path.GetDirectoryName(outputPath));
                File.WriteAllBytes(outputPath, preview.EncodeToPNG());
            }
            finally
            {
                camera.targetTexture = previousTarget;
                RenderTexture.active = previousActive;
                UnityEngine.Object.DestroyImmediate(preview);
                UnityEngine.Object.DestroyImmediate(target);
            }
        }
    }
}
