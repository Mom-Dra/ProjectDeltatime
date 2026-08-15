#if UNITY_EDITOR
using System.Collections;
using System.Linq;
using Deltatime.Combat;
using Deltatime.Player;
using Deltatime.Replay;
using Deltatime.TimeSystem;
using Deltatime.Visuals;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.TestTools;

namespace Deltatime.Tests.PlayMode
{
    public sealed class WeaponPickupOutlineTests
    {
        private const string OutlineMaterialPath =
            "Assets/_Project/Materials/WeaponPickupOutline.mat";
        private const string BasePickupPrefabPath =
            "Assets/_Project/Prefabs/WeaponPickup.prefab";
        private const string PistolDefinitionPath =
            "Assets/_Project/Pistol.asset";
        private const string AutomaticRifleDefinitionPath =
            "Assets/_Project/AutomaticRifle.asset";
        private const string ThrownWeaponPrefabPath =
            "Assets/_Project/Prefabs/ThrownWeapon.prefab";
        private const string InterceptableWeaponPrefabPath =
            "Assets/_Project/Prefabs/InterceptableWeapon.prefab";

        private static readonly string[] ConfiguredPickupPrefabPaths =
        {
            "Assets/_Project/Prefabs/PistolPickup.prefab",
            "Assets/_Project/Prefabs/AutomaticRiflePickup.prefab",
            "Assets/_Project/Prefabs/ShotgunPickup.prefab",
            "Assets/_Project/Prefabs/MeleeWeaponPickup.prefab"
        };

        [UnityTest]
        public IEnumerator ConfiguredGroundWeapons_CreateSharedOutlineRenderers()
        {
            Material outlineMaterial =
                AssetDatabase.LoadAssetAtPath<Material>(OutlineMaterialPath);
            Assert.That(outlineMaterial, Is.Not.Null);
            Assert.That(
                outlineMaterial.shader.name,
                Is.EqualTo("Deltatime/Weapon Pickup Outline"));

            foreach (string prefabPath in ConfiguredPickupPrefabPaths)
            {
                GameObject prefab =
                    AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                Assert.That(prefab, Is.Not.Null, prefabPath);

                GameObject instance = Object.Instantiate(prefab);
                yield return null;

                try
                {
                    WeaponPickup pickup = instance.GetComponent<WeaponPickup>();
                    WeaponPickupOutline outline =
                        instance.GetComponent<WeaponPickupOutline>();
                    Assert.That(pickup, Is.Not.Null, prefabPath);
                    Assert.That(pickup.Definition, Is.Not.Null, prefabPath);
                    Assert.That(outline, Is.Not.Null, prefabPath);
                    Assert.That(
                        outline.OutlineMaterial,
                        Is.SameAs(outlineMaterial),
                        prefabPath);

                    Transform visualRoot =
                        instance.transform.Find("Weapon Model Visual");
                    Assert.That(visualRoot, Is.Not.Null, prefabPath);
                    AssertOutlineHierarchy(
                        visualRoot,
                        outline,
                        outlineMaterial,
                        prefabPath);
                }
                finally
                {
                    Object.Destroy(instance);
                }

                yield return null;
            }

            Assert.That(
                AssetDatabase.LoadAssetAtPath<GameObject>(ThrownWeaponPrefabPath)
                    .GetComponent<WeaponPickupOutline>(),
                Is.Null);
            Assert.That(
                AssetDatabase.LoadAssetAtPath<GameObject>(
                        InterceptableWeaponPrefabPath)
                    .GetComponent<WeaponPickupOutline>(),
                Is.Null);
        }

        [UnityTest]
        public IEnumerator InitializeWithDifferentWeapon_RebuildsOutline()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                BasePickupPrefabPath);
            WeaponDefinition pistol =
                AssetDatabase.LoadAssetAtPath<WeaponDefinition>(
                    PistolDefinitionPath);
            WeaponDefinition rifle =
                AssetDatabase.LoadAssetAtPath<WeaponDefinition>(
                    AutomaticRifleDefinitionPath);
            Material outlineMaterial =
                AssetDatabase.LoadAssetAtPath<Material>(OutlineMaterialPath);
            Assert.That(prefab, Is.Not.Null);
            Assert.That(pistol, Is.Not.Null);
            Assert.That(rifle, Is.Not.Null);
            Assert.That(outlineMaterial, Is.Not.Null);

            GameObject instance = Object.Instantiate(prefab);
            try
            {
                WeaponPickup pickup = instance.GetComponent<WeaponPickup>();
                WeaponPickupOutline outline =
                    instance.GetComponent<WeaponPickupOutline>();
                pickup.Initialize(pistol, pistol.AmmunitionCapacity);
                yield return null;

                Transform pistolVisual =
                    instance.transform.Find("Weapon Model Visual");
                Assert.That(pistolVisual, Is.Not.Null);
                int[] pistolOutlineIds = GetGeneratedRenderers(pistolVisual)
                    .Select(renderer => renderer.GetInstanceID())
                    .ToArray();
                Assert.That(pistolOutlineIds, Is.Not.Empty);

                pickup.Initialize(rifle, rifle.AmmunitionCapacity);
                yield return null;

                Transform rifleVisual =
                    instance.transform.Find("Weapon Model Visual");
                Assert.That(rifleVisual, Is.Not.Null);
                Assert.That(rifleVisual, Is.Not.SameAs(pistolVisual));
                Assert.That(pickup.Definition, Is.SameAs(rifle));
                AssertOutlineHierarchy(
                    rifleVisual,
                    outline,
                    outlineMaterial,
                    "Weapon swap");
                int[] rifleOutlineIds = GetGeneratedRenderers(rifleVisual)
                    .Select(renderer => renderer.GetInstanceID())
                    .ToArray();
                CollectionAssert.IsNotSubsetOf(
                    rifleOutlineIds,
                    pistolOutlineIds);
            }
            finally
            {
                Object.Destroy(instance);
            }

            yield return null;
        }

        [UnityTest]
        public IEnumerator DynamicPickupAndOutline_AreHiddenWhenReplayStarts()
        {
            GameObject replayObject = new GameObject("Replay Outline Test");
            replayObject.SetActive(false);
            WorldTimeActivity activity =
                replayObject.AddComponent<WorldTimeActivity>();
            WorldTimeController worldTime =
                replayObject.AddComponent<WorldTimeController>();
            worldTime.Configure(activity);
            Camera replayCamera = replayObject.AddComponent<Camera>();
            DeadlineController deadline =
                replayObject.AddComponent<DeadlineController>();
            StageReplayController replay =
                replayObject.AddComponent<StageReplayController>();
            replay.Configure(worldTime, replayCamera, deadline);
            replay.ConfigureRendererDiscovery(new Transform[0], 0f);
            LogAssert.Expect(
                LogType.Error,
                "DeadlineController is missing required references.");
            replayObject.SetActive(true);

            yield return null;

            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                ConfiguredPickupPrefabPaths[0]);
            Assert.That(prefab, Is.Not.Null);

            int trackedVisualsBeforePickup = replay.TrackedVisualCount;
            GameObject pickupObject = Object.Instantiate(prefab);
            yield return null;

            try
            {
                Renderer[] replayableRenderers = pickupObject
                    .GetComponentsInChildren<Renderer>(true)
                    .Where(renderer => renderer is MeshRenderer ||
                                       renderer is LineRenderer)
                    .ToArray();
                Assert.That(replayableRenderers, Is.Not.Empty);
                Assert.That(
                    replayableRenderers.Any(renderer =>
                        renderer.gameObject.name ==
                        WeaponPickupOutline.GeneratedRendererName),
                    Is.True,
                    "The dynamic pickup did not create an outline renderer.");
                Assert.That(
                    replay.TrackedVisualCount,
                    Is.GreaterThan(trackedVisualsBeforePickup),
                    "The dynamic pickup hierarchy was not registered for replay.");

                Assert.That(replay.RequestReplay(), Is.True);
                yield return null;

                Assert.That(replay.IsReplaying, Is.True);
                foreach (Renderer renderer in replayableRenderers)
                {
                    Assert.That(
                        renderer.enabled,
                        Is.False,
                        $"Live replay source remained visible: {renderer.name}");
                }
            }
            finally
            {
                Object.Destroy(pickupObject);
                Object.Destroy(replayObject);
            }

            yield return null;
        }

        private static void AssertOutlineHierarchy(
            Transform visualRoot,
            WeaponPickupOutline outline,
            Material outlineMaterial,
            string context)
        {
            Renderer[] sourceRenderers = visualRoot
                .GetComponentsInChildren<Renderer>(true)
                .Where(renderer =>
                    renderer.gameObject.name !=
                    WeaponPickupOutline.GeneratedRendererName)
                .Where(IsSupportedSourceRenderer)
                .ToArray();
            Renderer[] generatedRenderers =
                GetGeneratedRenderers(visualRoot);

            Assert.That(sourceRenderers, Is.Not.Empty, context);
            Assert.That(
                generatedRenderers.Length,
                Is.EqualTo(sourceRenderers.Length),
                context);
            Assert.That(
                outline.GeneratedRendererCount,
                Is.EqualTo(generatedRenderers.Length),
                context);

            foreach (Renderer source in sourceRenderers)
            {
                CollectionAssert.DoesNotContain(
                    source.sharedMaterials,
                    outlineMaterial,
                    context);
            }

            foreach (Renderer generated in generatedRenderers)
            {
                Mesh mesh = generated is SkinnedMeshRenderer skinned
                    ? skinned.sharedMesh
                    : generated.GetComponent<MeshFilter>().sharedMesh;
                Assert.That(mesh, Is.Not.Null, context);
                Assert.That(
                    generated.sharedMaterials.Length,
                    Is.EqualTo(Mathf.Max(1, mesh.subMeshCount)),
                    context);
                Assert.That(
                    generated.sharedMaterials.All(material =>
                        material == outlineMaterial),
                    Is.True,
                    context);
                Assert.That(
                    generated.shadowCastingMode,
                    Is.EqualTo(ShadowCastingMode.Off),
                    context);
                Assert.That(generated.receiveShadows, Is.False, context);
                Assert.That(
                    generated.GetComponents<Collider>(),
                    Is.Empty,
                    context);
            }
        }

        private static Renderer[] GetGeneratedRenderers(Transform visualRoot)
        {
            return visualRoot
                .GetComponentsInChildren<Renderer>(true)
                .Where(renderer =>
                    renderer.gameObject.name ==
                    WeaponPickupOutline.GeneratedRendererName)
                .ToArray();
        }

        private static bool IsSupportedSourceRenderer(Renderer renderer)
        {
            if (renderer is SkinnedMeshRenderer skinned)
            {
                return skinned.sharedMesh != null;
            }

            return renderer is MeshRenderer &&
                   renderer.GetComponent<MeshFilter>()?.sharedMesh != null;
        }
    }
}
#endif
