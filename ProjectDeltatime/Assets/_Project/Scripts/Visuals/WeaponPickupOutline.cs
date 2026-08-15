using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Deltatime.Visuals
{
    [DisallowMultipleComponent]
    public sealed class WeaponPickupOutline : MonoBehaviour
    {
        internal const string GeneratedRendererName =
            "Weapon Pickup Outline Renderer";

        [SerializeField] private Material outlineMaterial;

        private readonly List<GameObject> generatedObjects =
            new List<GameObject>();

        public Material OutlineMaterial => outlineMaterial;

        internal int GeneratedRendererCount => generatedObjects.Count;

        public void Configure(Material material)
        {
            outlineMaterial = material;
        }

        internal void Refresh(Transform visualRoot)
        {
            Clear();
            if (!Application.isPlaying ||
                !enabled ||
                !gameObject.activeInHierarchy ||
                outlineMaterial == null ||
                visualRoot == null)
            {
                return;
            }

            Renderer[] sourceRenderers =
                visualRoot.GetComponentsInChildren<Renderer>(true);
            foreach (Renderer source in sourceRenderers)
            {
                if (source == null ||
                    source.gameObject.name == GeneratedRendererName)
                {
                    continue;
                }

                if (source is MeshRenderer meshRenderer)
                {
                    CreateMeshOutline(meshRenderer);
                }
                else if (source is SkinnedMeshRenderer skinnedRenderer)
                {
                    CreateSkinnedOutline(skinnedRenderer);
                }
            }
        }

        internal void Clear()
        {
            for (int index = generatedObjects.Count - 1; index >= 0; index--)
            {
                GameObject generated = generatedObjects[index];
                if (generated == null)
                {
                    continue;
                }

                if (Application.isPlaying)
                {
                    Destroy(generated);
                }
                else
                {
                    DestroyImmediate(generated);
                }
            }

            generatedObjects.Clear();
        }

        private void OnDestroy()
        {
            Clear();
        }

        private void CreateMeshOutline(MeshRenderer source)
        {
            MeshFilter sourceFilter = source.GetComponent<MeshFilter>();
            if (sourceFilter == null || sourceFilter.sharedMesh == null)
            {
                return;
            }

            GameObject generated = CreateGeneratedObject(source);
            MeshFilter filter = generated.AddComponent<MeshFilter>();
            filter.sharedMesh = sourceFilter.sharedMesh;

            MeshRenderer outlineRenderer =
                generated.AddComponent<MeshRenderer>();
            ConfigureRenderer(
                source,
                outlineRenderer,
                sourceFilter.sharedMesh.subMeshCount);
        }

        private void CreateSkinnedOutline(SkinnedMeshRenderer source)
        {
            if (source.sharedMesh == null)
            {
                return;
            }

            GameObject generated = CreateGeneratedObject(source);
            SkinnedMeshRenderer outlineRenderer =
                generated.AddComponent<SkinnedMeshRenderer>();
            outlineRenderer.sharedMesh = source.sharedMesh;
            outlineRenderer.bones = source.bones;
            outlineRenderer.rootBone = source.rootBone;
            outlineRenderer.localBounds = source.localBounds;
            outlineRenderer.quality = source.quality;
            outlineRenderer.updateWhenOffscreen = source.updateWhenOffscreen;
            ConfigureRenderer(
                source,
                outlineRenderer,
                source.sharedMesh.subMeshCount);
        }

        private GameObject CreateGeneratedObject(Renderer source)
        {
            GameObject generated = new GameObject(GeneratedRendererName)
            {
                layer = source.gameObject.layer
            };
            Transform generatedTransform = generated.transform;
            generatedTransform.SetParent(source.transform, false);
            generatedTransform.localPosition = Vector3.zero;
            generatedTransform.localRotation = Quaternion.identity;
            generatedTransform.localScale = Vector3.one;
            generatedObjects.Add(generated);
            return generated;
        }

        private void ConfigureRenderer(
            Renderer source,
            Renderer target,
            int subMeshCount)
        {
            int materialCount = Mathf.Max(1, subMeshCount);
            Material[] materials = new Material[materialCount];
            for (int index = 0; index < materialCount; index++)
            {
                materials[index] = outlineMaterial;
            }

            target.sharedMaterials = materials;
            target.enabled = source.enabled;
            target.shadowCastingMode = ShadowCastingMode.Off;
            target.receiveShadows = false;
            target.lightProbeUsage = LightProbeUsage.Off;
            target.reflectionProbeUsage = ReflectionProbeUsage.Off;
            target.motionVectorGenerationMode =
                MotionVectorGenerationMode.ForceNoMotion;
            target.sortingLayerID = source.sortingLayerID;
            target.sortingOrder = source.sortingOrder;
        }
    }
}
