using System.Collections;
using Deltatime.Combat;
using Deltatime.Core;
using Deltatime.Enemies;
using Deltatime.InputSystem;
using Deltatime.Player;
using Deltatime.TimeSystem;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.TestTools;

namespace Deltatime.Tests.PlayMode
{
    public sealed class ElevationFireAimTests
    {
        private const float TestWorldX = 2500f;

        [UnityTest]
        public IEnumerator Projectile_HitsTargetsAboveAndBelowMuzzle()
        {
            GameObject timeRoot = new GameObject("Elevation Fire Time");
            timeRoot.SetActive(false);
            WorldTimeActivity activity = timeRoot.AddComponent<WorldTimeActivity>();
            WorldTimeController worldTime = timeRoot.AddComponent<WorldTimeController>();
            worldTime.Configure(activity);
            timeRoot.SetActive(true);
            activity.SetMovement(1f);

            GameObject source = new GameObject("Elevation Fire Source");
            source.transform.position = new Vector3(TestWorldX, 2f, 0f);

            try
            {
                yield return null;

                yield return AssertProjectileHitsTarget(
                    worldTime,
                    source,
                    new Vector3(TestWorldX, 0.5f, 8f),
                    "Lower Target");
                yield return null;
                yield return AssertProjectileHitsTarget(
                    worldTime,
                    source,
                    new Vector3(TestWorldX, 3.5f, 8f),
                    "Upper Target");
            }
            finally
            {
                Object.Destroy(source);
                Object.Destroy(timeRoot);
            }
        }

        [UnityTest]
        public IEnumerator FireAim_UsesDamageableHeight_SkipsHiddenForeground_AndProjectsGround()
        {
            GameObject activityRoot = new GameObject("Elevation Aim Activity");
            WorldTimeActivity activity = activityRoot.AddComponent<WorldTimeActivity>();
            GameObject cameraRoot = new GameObject("Elevation Aim Camera");
            Camera camera = cameraRoot.AddComponent<Camera>();
            camera.farClipPlane = 100f;

            GameObject playerRoot = new GameObject("Elevation Aim Player");
            playerRoot.transform.position = new Vector3(TestWorldX, 2f, 0f);
            playerRoot.SetActive(false);
            playerRoot.AddComponent<Rigidbody>().useGravity = false;
            PlayerInputReader input = playerRoot.AddComponent<PlayerInputReader>();
            input.Configure(activity);
            PlayerAim aim = playerRoot.AddComponent<PlayerAim>();
            aim.Configure(input, activity, camera);
            playerRoot.SetActive(true);

            GameObject lowerTarget = null;
            GameObject upperTarget = null;
            GameObject hiddenForeground = null;
            GameObject floorSurface = null;
            try
            {
                yield return null;

                Vector3 rayOrigin = new Vector3(TestWorldX, 6f, -6f);
                Vector3 muzzle = new Vector3(TestWorldX, 2f, 0f);

                lowerTarget = CreateTarget(
                    "Lower Fire Aim Target",
                    new Vector3(TestWorldX, 0.5f, 8f));
                lowerTarget.AddComponent<DamageableProbe>();
                Ray lowerRay = CreateRay(rayOrigin, lowerTarget.transform.position);
                Physics.SyncTransforms();
                aim.UpdateFireAimPoint(lowerRay);
                Vector3 lowerPoint = aim.FireAimPoint;
                Assert.That(lowerPoint.y, Is.LessThan(muzzle.y));
                Assert.That(aim.GetFireDirectionFrom(muzzle).y, Is.LessThan(0f));
                Assert.That(
                    aim.GetPlanarDirectionFrom(muzzle).y,
                    Is.EqualTo(0f).Within(0.0001f));

                Object.Destroy(lowerTarget);
                lowerTarget = null;
                yield return null;

                upperTarget = CreateTarget(
                    "Upper Fire Aim Target",
                    new Vector3(TestWorldX, 3.5f, 8f));
                upperTarget.AddComponent<DamageableProbe>();
                Ray upperRay = CreateRay(rayOrigin, upperTarget.transform.position);
                Physics.SyncTransforms();
                aim.UpdateFireAimPoint(upperRay);
                Vector3 upperPoint = aim.FireAimPoint;
                Assert.That(upperPoint.y, Is.GreaterThan(muzzle.y));
                Assert.That(aim.GetFireDirectionFrom(muzzle).y, Is.GreaterThan(0f));

                hiddenForeground = GameObject.CreatePrimitive(PrimitiveType.Cube);
                hiddenForeground.name = "Hidden Fire Aim Foreground";
                hiddenForeground.transform.position = upperRay.GetPoint(4f);
                hiddenForeground.transform.localScale = Vector3.one;
                hiddenForeground.GetComponent<Renderer>().shadowCastingMode =
                    ShadowCastingMode.ShadowsOnly;
                Physics.SyncTransforms();

                aim.UpdateFireAimPoint(upperRay);
                Vector3 visiblePoint = aim.FireAimPoint;
                Assert.That(
                    (visiblePoint - upperPoint).sqrMagnitude,
                    Is.LessThan(0.0001f));

                Object.Destroy(upperTarget);
                upperTarget = null;
                Object.Destroy(hiddenForeground);
                hiddenForeground = null;
                yield return null;

                floorSurface = CreateFloorSurface(
                    "Planar Fire Aim Floor",
                    new Vector3(TestWorldX, 0f, 8f));
                Ray floorRay = CreateRay(rayOrigin, floorSurface.transform.position);
                Physics.SyncTransforms();
                Assert.That(
                    Physics.Raycast(
                        floorRay,
                        out RaycastHit floorHit,
                        100f,
                        Physics.DefaultRaycastLayers,
                        QueryTriggerInteraction.Ignore),
                    Is.True);
                aim.UpdateFireAimPoint(floorRay);
                Vector3 floorPoint = aim.FireAimPoint;
                Assert.That(floorPoint.y,
                    Is.EqualTo(playerRoot.transform.position.y).Within(0.0001f));
                Assert.That(floorPoint.x,
                    Is.EqualTo(floorHit.point.x).Within(0.0001f));
                Assert.That(floorPoint.z,
                    Is.EqualTo(floorHit.point.z).Within(0.0001f));
                Assert.That(aim.GetFireDirectionFrom(muzzle).y,
                    Is.EqualTo(0f).Within(0.0001f));

                Object.Destroy(floorSurface);
                floorSurface = null;
                yield return null;

                aim.UpdateFireAimPoint(upperRay);
                Vector3 fallbackPoint = aim.FireAimPoint;
                Plane playerPlane = new Plane(Vector3.up, playerRoot.transform.position);
                Assert.That(playerPlane.Raycast(upperRay, out float fallbackDistance), Is.True);
                Assert.That(
                    (fallbackPoint - upperRay.GetPoint(fallbackDistance)).sqrMagnitude,
                    Is.LessThan(0.0001f));
                Assert.That(aim.GetFireDirectionFrom(muzzle).y,
                    Is.EqualTo(0f).Within(0.0001f));
            }
            finally
            {
                Object.Destroy(lowerTarget);
                Object.Destroy(upperTarget);
                Object.Destroy(hiddenForeground);
                Object.Destroy(floorSurface);
                Object.Destroy(playerRoot);
                Object.Destroy(cameraRoot);
                Object.Destroy(activityRoot);
            }
        }

        [Test]
        public void EnemyFirearmDirection_PreservesVerticalDisplacement()
        {
            Vector3 origin = new Vector3(0f, 2f, 0f);

            Assert.That(
                EnemyCombatant.GetFirearmDirection(origin, new Vector3(0f, 0f, 4f)).y,
                Is.LessThan(0f));
            Assert.That(
                EnemyCombatant.GetFirearmDirection(origin, new Vector3(0f, 4f, 4f)).y,
                Is.GreaterThan(0f));
        }

        private static IEnumerator AssertProjectileHitsTarget(
            WorldTimeController worldTime,
            GameObject source,
            Vector3 targetPosition,
            string targetName)
        {
            GameObject target = CreateTarget(targetName, targetPosition);
            DamageableProbe probe = target.AddComponent<DamageableProbe>();
            GameObject projectileRoot = new GameObject(targetName + " Projectile");
            projectileRoot.transform.position = source.transform.position;
            projectileRoot.AddComponent<LineRenderer>();
            Projectile projectile = projectileRoot.AddComponent<Projectile>();
            Vector3 direction = target.transform.position - projectileRoot.transform.position;
            projectile.Initialize(
                worldTime,
                CombatFaction.Player,
                source,
                direction,
                4000f,
                1,
                0.05f,
                0f);

            try
            {
                float timeout = Time.realtimeSinceStartup + 1f;
                while (probe.HitCount == 0 && Time.realtimeSinceStartup < timeout)
                {
                    yield return null;
                }

                Assert.That(probe.HitCount, Is.EqualTo(1),
                    targetName + " was not hit by the 3D projectile.");
            }
            finally
            {
                Object.Destroy(projectileRoot);
                Object.Destroy(target);
            }
        }

        private static GameObject CreateTarget(string name, Vector3 position)
        {
            GameObject target = new GameObject(name);
            target.transform.position = position;
            SphereCollider collider = target.AddComponent<SphereCollider>();
            collider.radius = 0.6f;
            return target;
        }

        private static Ray CreateRay(Vector3 origin, Vector3 target)
        {
            return new Ray(origin, (target - origin).normalized);
        }

        private static GameObject CreateFloorSurface(string name, Vector3 position)
        {
            GameObject surface = new GameObject(name);
            surface.transform.position = position;
            BoxCollider collider = surface.AddComponent<BoxCollider>();
            collider.size = new Vector3(8f, 0.1f, 8f);
            return surface;
        }

        private sealed class DamageableProbe : MonoBehaviour, IDamageable
        {
            public int HitCount { get; private set; }
            public CombatFaction Faction => CombatFaction.Enemy;
            public bool IsAlive => true;

            public void ReceiveHit(DamageHit hit)
            {
                HitCount++;
            }
        }
    }
}
