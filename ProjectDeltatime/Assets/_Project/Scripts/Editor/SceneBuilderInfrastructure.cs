using System;
using System.Collections.Generic;
using System.IO;
using Deltatime.Combat;
using Deltatime.Enemies;
using Deltatime.Player;
using Unity.AI.Navigation;
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
        private const float DirectGroundSampleDistance = 1.5f;
        private const float DirectGroundHorizontalTolerance = 0.1f;

        public static void BuildNavMeshExcludingDynamicGameplayColliders(
            NavMeshSurface surface,
            Scene scene)
        {
            if (surface == null)
            {
                throw new ArgumentNullException(nameof(surface));
            }

            List<Collider> disabledColliders =
                DisableDynamicGameplayColliders(scene);
            try
            {
                Physics.SyncTransforms();
                surface.BuildNavMesh();
            }
            finally
            {
                for (int i = 0; i < disabledColliders.Count; i++)
                {
                    Collider collider = disabledColliders[i];
                    if (collider != null)
                    {
                        collider.enabled = true;
                    }
                }

                Physics.SyncTransforms();
            }
        }

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

        public static bool IsDirectlyAboveNavMesh(
            Vector3 position,
            out NavMeshHit hit,
            float maximumDistance = DirectGroundSampleDistance,
            float maximumHorizontalOffset =
                DirectGroundHorizontalTolerance)
        {
            if (!NavMesh.SamplePosition(
                    position,
                    out hit,
                    maximumDistance,
                    NavMesh.AllAreas))
            {
                return false;
            }

            Vector2 horizontalDelta = new Vector2(
                position.x - hit.position.x,
                position.z - hit.position.z);
            return horizontalDelta.sqrMagnitude <=
                   maximumHorizontalOffset * maximumHorizontalOffset;
        }

        public static void ValidateDynamicGameplayCoverage(
            Scene scene,
            string context)
        {
            PlayerHealth[] players = FindSceneComponents<PlayerHealth>(scene);
            EnemyHealth[] enemies = FindSceneComponents<EnemyHealth>(scene);
            WeaponPickup[] pickups = FindSceneComponents<WeaponPickup>(scene);
            SceneValidation.Require(
                players.Length == 1,
                $"{context} expected one player for NavMesh validation, " +
                $"found {players.Length}.");

            Vector3 playerPosition = players[0].transform.position;
            RequireDirectGround(playerPosition, context + " player");
            for (int i = 0; i < enemies.Length; i++)
            {
                Vector3 enemyPosition = enemies[i].transform.position;
                RequireDirectGround(enemyPosition, context + " " + enemies[i].name);
                bool complete = HasCompletePath(
                    enemyPosition,
                    playerPosition,
                    DirectGroundSampleDistance,
                    out NavMeshPathStatus status);
                SceneValidation.Require(
                    complete,
                    $"{context} {enemies[i].name} cannot reach the player; " +
                    $"path={status}.");
            }

            for (int i = 0; i < pickups.Length; i++)
            {
                RequireDirectGround(
                    pickups[i].transform.position,
                    context + " " + pickups[i].name);
            }
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

        private static List<Collider> DisableDynamicGameplayColliders(
            Scene scene)
        {
            List<Collider> disabled = new List<Collider>();
            GameObject[] roots = scene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                Collider[] colliders =
                    roots[i].GetComponentsInChildren<Collider>(true);
                for (int j = 0; j < colliders.Length; j++)
                {
                    Collider collider = colliders[j];
                    if (collider == null || !collider.enabled ||
                        !collider.gameObject.activeInHierarchy ||
                        !IsDynamicGameplayCollider(collider))
                    {
                        continue;
                    }

                    collider.enabled = false;
                    disabled.Add(collider);
                }
            }

            return disabled;
        }

        private static bool IsDynamicGameplayCollider(Collider collider)
        {
            return collider.GetComponentInParent<PlayerHealth>() != null ||
                   collider.GetComponentInParent<EnemyHealth>() != null ||
                   collider.GetComponentInParent<WeaponPickup>() != null;
        }

        private static void RequireDirectGround(
            Vector3 position,
            string subject)
        {
            bool found = IsDirectlyAboveNavMesh(position, out NavMeshHit hit);
            float horizontalOffset = found
                ? Vector2.Distance(
                    new Vector2(position.x, position.z),
                    new Vector2(hit.position.x, hit.position.z))
                : float.PositiveInfinity;
            SceneValidation.Require(
                found,
                $"{subject} has no NavMesh directly below its position " +
                $"({position}); horizontalOffset={horizontalOffset:0.###}, " +
                $"limit={DirectGroundHorizontalTolerance:0.###}.");
        }

        private static T[] FindSceneComponents<T>(Scene scene)
            where T : Component
        {
            List<T> results = new List<T>();
            GameObject[] roots = scene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                results.AddRange(roots[i].GetComponentsInChildren<T>(true));
            }

            return results.ToArray();
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
