using System;
using Deltatime.Combat;
using Deltatime.Enemies;
using Deltatime.Level;
using Deltatime.Player;
using Deltatime.Replay;
using Deltatime.TimeSystem;
using Deltatime.Visuals;
using Deltatime.Vision;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace Deltatime.EditorTools
{
    [InitializeOnLoad]
    public static class Stage5PlayModeSmokeTest
    {
        private const string ScenePath =
            "Assets/_Project/Scenes/Stage5.unity";
        private const string NavigationPath =
            "Assets/_Project/Scenes/Stage5Navigation.asset";
        private const string EnvironmentRootName =
            "Stage 5 - Undertow Dive";
        private const string RunningKey = "Deltatime.Stage5Smoke.Running";
        private const string FailedKey = "Deltatime.Stage5Smoke.Failed";
        private const string FailureKey = "Deltatime.Stage5Smoke.Failure";
        private const string PhaseKey = "Deltatime.Stage5Smoke.Phase";
        private const float CameraEdgeActorInset = 0.5f;

        private static bool callbacksAttached;
        private static bool validationRan;
        private static double playStartedAt;
        private static bool movementProbePending;
        private static Rigidbody movementProbeBody;
        private static CapsuleCollider movementProbeCapsule;
        private static Vector3 movementProbeStartPosition;
        private static float movementProbeExpectedGroundOffset;
        private static float movementProbeStartFixedTime;

        static Stage5PlayModeSmokeTest()
        {
            if (SessionState.GetBool(RunningKey, false))
            {
                AttachCallbacks();
            }
        }

        public static void RunFromCommandLine()
        {
            SessionState.SetBool(RunningKey, true);
            SessionState.SetBool(FailedKey, false);
            SessionState.SetString(FailureKey, string.Empty);
            SessionState.SetString(PhaseKey, "entering");
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            AttachCallbacks();
            EditorApplication.isPlaying = true;
        }

        private static void AttachCallbacks()
        {
            if (callbacksAttached)
            {
                return;
            }

            callbacksAttached = true;
            EditorApplication.update += Tick;
            EditorApplication.playModeStateChanged += HandlePlayModeStateChanged;
            Application.logMessageReceived += HandleLog;
        }

        private static void DetachCallbacks()
        {
            if (!callbacksAttached)
            {
                return;
            }

            callbacksAttached = false;
            EditorApplication.update -= Tick;
            EditorApplication.playModeStateChanged -= HandlePlayModeStateChanged;
            Application.logMessageReceived -= HandleLog;
        }

        private static void HandlePlayModeStateChanged(PlayModeStateChange state)
        {
            if (!SessionState.GetBool(RunningKey, false))
            {
                return;
            }

            if (state == PlayModeStateChange.EnteredPlayMode)
            {
                playStartedAt = EditorApplication.timeSinceStartup;
                validationRan = false;
                ClearMovementProbe();
                SessionState.SetString(PhaseKey, "playing");
            }
            else if (state == PlayModeStateChange.ExitingPlayMode)
            {
                SessionState.SetString(PhaseKey, "stopping");
            }
            else if (state == PlayModeStateChange.EnteredEditMode &&
                     SessionState.GetString(PhaseKey, string.Empty) == "stopping")
            {
                Finish();
            }
        }

        private static void Tick()
        {
            if (!SessionState.GetBool(RunningKey, false))
            {
                DetachCallbacks();
                return;
            }

            if (!EditorApplication.isPlaying)
            {
                return;
            }

            double elapsed = EditorApplication.timeSinceStartup - playStartedAt;
            if (!validationRan && elapsed >= 0.9d)
            {
                validationRan = true;
                try
                {
                    ValidateRuntimeState();
                }
                catch (Exception exception)
                {
                    RecordFailure(exception.ToString());
                }

                if (!movementProbePending)
                {
                    EditorApplication.isPlaying = false;
                }
            }
            else if (movementProbePending &&
                     Time.fixedTime > movementProbeStartFixedTime)
            {
                try
                {
                    ValidateRigidbodyMovementProbe("Stage5");
                }
                catch (Exception exception)
                {
                    RecordFailure(exception.ToString());
                }

                ClearMovementProbe();
                EditorApplication.isPlaying = false;
            }
            else if (elapsed >= 20d)
            {
                RecordFailure("Stage5 play-mode smoke test timed out.");
                EditorApplication.isPlaying = false;
            }
        }

        private static void ValidateRuntimeState()
        {
            Scene scene = SceneManager.GetActiveScene();
            PlayerHealth player = UnityEngine.Object.FindFirstObjectByType<PlayerHealth>();
            DeadlineController deadline =
                UnityEngine.Object.FindFirstObjectByType<DeadlineController>();
            WorldTimeController worldTime =
                UnityEngine.Object.FindFirstObjectByType<WorldTimeController>();
            StageController stage =
                UnityEngine.Object.FindFirstObjectByType<StageController>();
            StageReplayController replay =
                UnityEngine.Object.FindFirstObjectByType<StageReplayController>();
            NavMeshSurface surface =
                UnityEngine.Object.FindFirstObjectByType<NavMeshSurface>();
            EnemyHealth[] enemies = UnityEngine.Object.FindObjectsByType<EnemyHealth>(
                FindObjectsSortMode.None);
            EnemyMotor[] motors = UnityEngine.Object.FindObjectsByType<EnemyMotor>(
                FindObjectsSortMode.None);
            EnemyShooter[] shooters = UnityEngine.Object.FindObjectsByType<EnemyShooter>(
                FindObjectsSortMode.None);
            EnemyChaser[] chasers = UnityEngine.Object.FindObjectsByType<EnemyChaser>(
                FindObjectsSortMode.None);
            WeaponPickup[] pickups = UnityEngine.Object.FindObjectsByType<WeaponPickup>(
                FindObjectsSortMode.None);
            CharacterVisualController[] visuals =
                UnityEngine.Object.FindObjectsByType<CharacterVisualController>(
                    FindObjectsSortMode.None);
            GameObject environmentRoot = GameObject.Find(EnvironmentRootName);
            Camera camera = Camera.main;
            PlayerAim aim = player == null ? null : player.GetComponent<PlayerAim>();
            TopDownCameraController cameraController = camera == null
                ? null
                : camera.GetComponent<TopDownCameraController>();
            Stage5SouthExteriorCutaway southCutaway = environmentRoot == null
                ? null
                : environmentRoot.GetComponent<Stage5SouthExteriorCutaway>();

            Require(scene.path == ScenePath, $"Unexpected scene: {scene.path}");
            Require(player != null && player.IsAlive,
                "Stage5 player did not initialize alive.");
            Require(deadline != null && deadline.ChargesRemaining == 2,
                "Stage5 Deadline charges did not initialize to 2.");
            Require(worldTime != null && worldTime.enabled,
                "Stage5 world time did not initialize.");
            Require(stage != null &&
                    stage.CurrentState == StageController.StageState.Active,
                "Stage5 did not enter the active state.");
            Require(stage != null && stage.RemainingEnemyCount == 5,
                $"Stage5 registered {stage?.RemainingEnemyCount} enemies instead of 5.");
            Require(enemies.Length == 5 && motors.Length == 5 &&
                    shooters.Length == 3 && chasers.Length == 2,
                "Stage5 enemy combat or navigation components are missing.");
            Require(pickups.Length == 2,
                $"Stage5 has {pickups.Length} weapon pickups instead of 2.");
            Require(replay != null && replay.enabled,
                "Stage5 replay did not initialize.");
            Require(replay != null && replay.TrackedLightCount == 2,
                $"Stage5 replay tracked {replay?.TrackedLightCount} vision lights instead of 2.");
            Require(replay != null && replay.TrackedReplayVisionConeCount == 1,
                "Stage5 replay vision-cone track did not initialize.");
            Require(replay != null && replay.TrackedExcludedVisualCount == 0,
                $"Stage5 replay tracked {replay?.TrackedExcludedVisualCount} " +
                "static dive-bar renderers.");
            Require(surface != null && surface.navMeshData != null,
                "Stage5 NavMesh data is missing at runtime.");
            Require(surface != null &&
                    AssetDatabase.GetAssetPath(surface.navMeshData) == NavigationPath,
                "Stage5 is not using its dedicated NavMesh data.");
            Require(environmentRoot != null &&
                    environmentRoot.GetComponent<ReplayExcluded>() != null,
                "Stage5 static environment is not excluded from replay tracks.");
            Require(visuals.Length == 6,
                $"Stage5 has {visuals.Length} character visual controllers instead of 6.");
            Require(GameObject.Find("Dive Bar Character - Player") != null &&
                    GameObject.Find("Dive Bar Character - West Gunner") != null &&
                    GameObject.Find("Dive Bar Character - Center Chaser") != null &&
                    GameObject.Find("Dive Bar Character - East Gunner") != null &&
                    GameObject.Find("Dive Bar Character - North Gunner") != null &&
                    GameObject.Find("Dive Bar Character - South Chaser") != null,
                "Stage5 Polygon Nightclubs character visuals are missing.");
            Require(camera != null && cameraController != null,
                "Stage5 constrained camera did not initialize.");
            Require(aim != null,
                "Stage5 player aim did not initialize.");
            Require(southCutaway != null && southCutaway.Occluders != null &&
                    southCutaway.Occluders.Length > 0,
                "Stage5 south exterior cutaway did not initialize.");

            ValidateCombatIdentityRings(scene);
            ValidateCombatIdentityRingVisibility(enemies);
            ValidateCameraBoundsAtExtremes(
                player.transform,
                camera,
                cameraController);
            ValidateSouthExteriorCutaway(player.transform, southCutaway, environmentRoot);
            ValidateAimIgnoresForegroundCollider(aim, player.transform, camera);
            ValidateElevationTraversal(player.GetComponent<NavMeshGroundMovement>(), "Stage5");
            Stage5SceneBuilder.ValidateFurnitureNavMeshExclusion(
                environmentRoot,
                "Stage5 runtime");

            RequireOnNavMesh(player.transform.position, "player");
            for (int i = 0; i < enemies.Length; i++)
            {
                RequireOnNavMesh(enemies[i].transform.position, enemies[i].name);
                RequireCompletePath(
                    player.transform.position,
                    enemies[i].transform.position,
                    enemies[i].name);
            }

            StartRigidbodyMovementProbe(player, "Stage5");
        }

        private static void ValidateCombatIdentityRings(Scene scene)
        {
            string[] owners =
            {
                "Player",
                "Enemy West",
                "Enemy Center",
                "Enemy East",
                "Enemy North Gunner",
                "Enemy South Chaser"
            };
            for (int i = 0; i < owners.Length; i++)
            {
                GameObject owner = FindSceneRoot(scene, owners[i]);
                Transform ring = owner == null
                    ? null
                    : owner.transform.Find("Combat Identity Ring");
                Renderer renderer = ring == null ? null : ring.GetComponent<Renderer>();
                Material material = renderer == null ? null : renderer.sharedMaterial;
                Require(material != null && material.shader != null &&
                        material.shader.name == "Unlit/Color",
                    $"Stage5 identity marker is not unlit for {owners[i]}.");
                Require(renderer.shadowCastingMode == ShadowCastingMode.Off &&
                        !renderer.receiveShadows &&
                        renderer.lightProbeUsage == LightProbeUsage.Off &&
                        renderer.reflectionProbeUsage == ReflectionProbeUsage.Off,
                    $"Stage5 identity marker still uses scene lighting for {owners[i]}.");
            }
        }

        private static void ValidateCombatIdentityRingVisibility(
            EnemyHealth[] enemies)
        {
            VisionCone vision = UnityEngine.Object.FindFirstObjectByType<VisionCone>();
            Require(vision != null && !vision.HasUnlimitedVision,
                "Stage5 identity-ring visibility requires limited player vision.");

            int hiddenEnemyCount = 0;
            for (int i = 0; i < enemies.Length; i++)
            {
                EnemyCombatant combatant = enemies[i] == null
                    ? null
                    : enemies[i].GetComponent<EnemyCombatant>();
                Renderer bodyRenderer = enemies[i] == null
                    ? null
                    : enemies[i].GetComponent<Renderer>();
                Transform ring = enemies[i] == null
                    ? null
                    : enemies[i].transform.Find("Combat Identity Ring");
                Renderer ringRenderer = ring == null
                    ? null
                    : ring.GetComponent<Renderer>();
                Require(combatant != null && bodyRenderer != null &&
                        ringRenderer != null,
                    $"Stage5 enemy identity-ring references are missing for {enemies[i]?.name}.");

                bool expectedVisible =
                    vision.ContainsWorldPoint(bodyRenderer.bounds.center) &&
                    !combatant.IsDead;
                Require(ringRenderer.enabled == expectedVisible,
                    $"Stage5 identity ring visibility diverged for {enemies[i].name}: " +
                    $"expected={expectedVisible}, actual={ringRenderer.enabled}.");

                if (!expectedVisible)
                {
                    hiddenEnemyCount++;
                }
            }

            Require(hiddenEnemyCount > 0,
                "Stage5 identity-ring visibility smoke did not exercise an enemy outside player vision.");
        }

        private static void ValidateCameraBoundsAtExtremes(
            Transform player,
            Camera camera,
            TopDownCameraController controller)
        {
            NavMeshTriangulation triangulation = NavMesh.CalculateTriangulation();
            Require(triangulation.vertices.Length > 0,
                "Stage5 camera boundary smoke requires a baked NavMesh.");
            Bounds navBounds = new Bounds(triangulation.vertices[0], Vector3.zero);
            for (int i = 1; i < triangulation.vertices.Length; i++)
            {
                navBounds.Encapsulate(triangulation.vertices[i]);
            }
            Bounds cameraBounds = navBounds;

            Vector3 originalPosition = player.position;
            Quaternion originalRotation = player.rotation;
            float originalAspect = camera.aspect;
            Vector3[] samples =
            {
                new Vector3(
                    navBounds.min.x + CameraEdgeActorInset,
                    originalPosition.y,
                    navBounds.center.z),
                new Vector3(
                    navBounds.max.x - CameraEdgeActorInset,
                    originalPosition.y,
                    navBounds.center.z),
                new Vector3(
                    navBounds.center.x,
                    originalPosition.y,
                    navBounds.min.z + CameraEdgeActorInset),
                new Vector3(
                    navBounds.center.x,
                    originalPosition.y,
                    navBounds.max.z - CameraEdgeActorInset)
            };
            string[] sampleNames = { "west", "east", "south", "north" };

            try
            {
                camera.aspect = 16f / 9f;
                for (int i = 0; i < samples.Length; i++)
                {
                    player.position = samples[i];
                    Physics.SyncTransforms();
                    controller.SnapToTarget();
                    Require(TryCalculateGroundBounds(
                            camera,
                            cameraBounds.center.y,
                            out Bounds visibleBounds),
                        $"Stage5 camera did not see the ground at the {sampleNames[i]} edge.");
                    RequireFootprintConstrained(
                        visibleBounds,
                        cameraBounds,
                        sampleNames[i]);

                    Vector3 viewport = camera.WorldToViewportPoint(player.position);
                    Require(viewport.z > 0f &&
                            viewport.x >= -0.01f && viewport.x <= 1.01f &&
                            viewport.y >= -0.01f && viewport.y <= 1.01f,
                        $"Stage5 player left the viewport at the {sampleNames[i]} edge: " +
                        viewport);
                }
            }
            finally
            {
                player.SetPositionAndRotation(originalPosition, originalRotation);
                Physics.SyncTransforms();
                camera.aspect = originalAspect;
                controller.SnapToTarget();
            }
        }

        private static void ValidateSouthExteriorCutaway(
            Transform player,
            Stage5SouthExteriorCutaway cutaway,
            GameObject environmentRoot)
        {
            Renderer[] occluders = cutaway.Occluders;
            ShadowCastingMode[] originalModes = new ShadowCastingMode[occluders.Length];
            for (int i = 0; i < occluders.Length; i++)
            {
                Require(occluders[i] != null && occluders[i].gameObject.activeInHierarchy,
                    "Stage5 south cutaway has an inactive exterior renderer.");
                originalModes[i] = occluders[i].shadowCastingMode;
            }

            int originalVisionColliderCount = CountEnabledVisionColliders(environmentRoot);
            Vector3 originalPosition = player.position;
            Quaternion originalRotation = player.rotation;
            try
            {
                Vector3 hiddenPosition = FindNavMeshPosition(
                    new Vector3(
                        originalPosition.x,
                        originalPosition.y,
                        cutaway.HideBelowZ - 0.1f),
                    "south cutaway hide sample");
                player.position = new Vector3(
                    hiddenPosition.x,
                    originalPosition.y,
                    hiddenPosition.z);
                Physics.SyncTransforms();
                cutaway.EvaluateNow();
                Require(cutaway.IsCutawayActive,
                    "Stage5 south exterior did not hide near the south boundary.");
                for (int i = 0; i < cutaway.SouthExteriorOccluderCount; i++)
                {
                    Require(occluders[i].shadowCastingMode == ShadowCastingMode.ShadowsOnly,
                        "Stage5 south exterior remained visible during the cutaway.");
                }

                Require(CountEnabledVisionColliders(environmentRoot) == originalVisionColliderCount,
                    "Stage5 south cutaway changed structural collision or vision blockers.");

                Vector3 restoredPosition = FindNavMeshPosition(
                    new Vector3(
                        originalPosition.x,
                        originalPosition.y,
                        cutaway.RestoreAboveZ + 0.1f),
                    "south cutaway restore sample");
                player.position = new Vector3(
                    restoredPosition.x,
                    originalPosition.y,
                    restoredPosition.z);
                Physics.SyncTransforms();
                cutaway.EvaluateNow();
                for (int i = 0; i < cutaway.SouthExteriorOccluderCount; i++)
                {
                    Require(occluders[i].shadowCastingMode == originalModes[i],
                        "Stage5 south exterior did not restore its original renderer mode.");
                }
            }
            finally
            {
                player.SetPositionAndRotation(originalPosition, originalRotation);
                Physics.SyncTransforms();
                cutaway.EvaluateNow();
            }
        }

        private static void ValidateAimIgnoresForegroundCollider(
            PlayerAim aim,
            Transform player,
            Camera camera)
        {
            const int TestLayer = 31;
            Ray pointerRay = camera.ViewportPointToRay(new Vector3(0.5f, 0.15f, 0f));
            Plane actorPlane = new Plane(Vector3.up, player.position);
            Require(actorPlane.Raycast(pointerRay, out float expectedDistance),
                "Stage5 aim validation ray did not reach the actor plane.");

            GameObject blockerRoot = new GameObject("Stage5 Aim Foreground Probe")
            {
                hideFlags = HideFlags.HideAndDontSave,
                layer = TestLayer
            };
            BoxCollider blocker = blockerRoot.AddComponent<BoxCollider>();
            blocker.size = Vector3.one;
            blockerRoot.transform.position = pointerRay.GetPoint(expectedDistance * 0.5f);

            try
            {
                Physics.SyncTransforms();
                Require(Physics.Raycast(
                        pointerRay,
                        out RaycastHit blockerHit,
                        expectedDistance,
                        1 << TestLayer,
                        QueryTriggerInteraction.Ignore) &&
                    blockerHit.collider == blocker,
                    "Stage5 aim foreground probe did not intercept the camera ray.");

                System.Reflection.MethodInfo resolveAimPoint =
                    typeof(PlayerAim).GetMethod(
                        "TryResolveAimPoint",
                        System.Reflection.BindingFlags.Instance |
                        System.Reflection.BindingFlags.NonPublic);
                Require(resolveAimPoint != null,
                    "Stage5 player aim resolver is missing.");
                object[] arguments = { pointerRay, Vector3.zero };
                bool resolved = (bool)resolveAimPoint.Invoke(aim, arguments);
                Vector3 resolvedPoint = (Vector3)arguments[1];
                Vector3 expectedPoint = pointerRay.GetPoint(expectedDistance);
                Require(resolved &&
                        (resolvedPoint - expectedPoint).sqrMagnitude < 0.0001f,
                    "Stage5 player aim was deflected by a foreground collider.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(blockerRoot);
            }
        }

        private static Vector3 FindNavMeshPosition(Vector3 requested, string subject)
        {
            Require(NavMesh.SamplePosition(
                    requested,
                    out NavMeshHit hit,
                    2f,
                    NavMesh.AllAreas),
                $"Stage5 {subject} is not on the NavMesh: {requested}.");
            return hit.position;
        }

        private static int CountEnabledVisionColliders(GameObject environmentRoot)
        {
            Collider[] colliders = environmentRoot.GetComponentsInChildren<Collider>(true);
            int count = 0;
            for (int i = 0; i < colliders.Length; i++)
            {
                if (colliders[i].enabled && colliders[i].gameObject.activeInHierarchy &&
                    colliders[i].gameObject.layer == 8)
                {
                    count++;
                }
            }

            return count;
        }

        private static bool TryCalculateGroundBounds(
            Camera camera,
            float groundHeight,
            out Bounds bounds)
        {
            bounds = default;
            bool found = false;
            Plane ground = new Plane(Vector3.up, new Vector3(0f, groundHeight, 0f));
            Vector2[] corners =
            {
                new Vector2(0f, 0f),
                new Vector2(1f, 0f),
                new Vector2(0f, 1f),
                new Vector2(1f, 1f)
            };
            for (int i = 0; i < corners.Length; i++)
            {
                Ray ray = camera.ViewportPointToRay(corners[i]);
                if (!ground.Raycast(ray, out float distance))
                {
                    return false;
                }

                Vector3 point = ray.GetPoint(distance);
                if (!found)
                {
                    bounds = new Bounds(point, Vector3.zero);
                    found = true;
                }
                else
                {
                    bounds.Encapsulate(point);
                }
            }

            return found;
        }

        private static void RequireFootprintConstrained(
            Bounds visibleBounds,
            Bounds navBounds,
            string sample)
        {
            const float tolerance = 0.05f;
            if (visibleBounds.size.x <= navBounds.size.x + tolerance)
            {
                Require(visibleBounds.min.x >= navBounds.min.x - tolerance &&
                        visibleBounds.max.x <= navBounds.max.x + tolerance,
                    $"Stage5 camera escaped horizontally at {sample}: {visibleBounds}");
            }
            else
            {
                Require(Mathf.Abs(visibleBounds.center.x - navBounds.center.x) <= tolerance,
                    $"Stage5 wide viewport is not centered at {sample}: {visibleBounds}");
            }

            if (visibleBounds.size.z <= navBounds.size.z + tolerance)
            {
                Require(visibleBounds.min.z >= navBounds.min.z - tolerance &&
                        visibleBounds.max.z <= navBounds.max.z + tolerance,
                    $"Stage5 camera escaped vertically at {sample}: {visibleBounds}");
            }
            else
            {
                Require(Mathf.Abs(visibleBounds.center.z - navBounds.center.z) <= tolerance,
                    $"Stage5 tall viewport is not centered at {sample}: {visibleBounds}");
            }
        }

        private static GameObject FindSceneRoot(Scene scene, string name)
        {
            GameObject[] roots = scene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                if (roots[i].name == name)
                {
                    return roots[i];
                }
            }

            return null;
        }

        private static void RequireOnNavMesh(Vector3 position, string subject)
        {
            bool found = NavMesh.SamplePosition(
                position,
                out _,
                1.5f,
                NavMesh.AllAreas);
            Require(found,
                $"Stage5 {subject} spawn is not on the baked NavMesh ({position}).");
        }

        private static void RequireCompletePath(
            Vector3 from,
            Vector3 to,
            string subject)
        {
            Require(NavMesh.SamplePosition(from, out NavMeshHit fromHit, 1.5f, NavMesh.AllAreas),
                "Stage5 player path origin is invalid.");
            Require(NavMesh.SamplePosition(to, out NavMeshHit toHit, 1.5f, NavMesh.AllAreas),
                $"Stage5 {subject} path destination is invalid.");
            NavMeshPath path = new NavMeshPath();
            bool calculated = NavMesh.CalculatePath(
                fromHit.position,
                toHit.position,
                NavMesh.AllAreas,
                path);
            Require(calculated && path.status == NavMeshPathStatus.PathComplete &&
                    path.corners.Length > 0,
                $"Stage5 {subject} did not create a complete NavMesh path.");
        }

        private static void ValidateElevationTraversal(
            NavMeshGroundMovement movement,
            string stageName)
        {
            Require(movement != null,
                $"{stageName} player has no NavMesh ground movement projector.");
            NavMeshTriangulation triangulation = NavMesh.CalculateTriangulation();
            Require(TryFindTraversableElevationPair(
                    triangulation.vertices,
                    out Vector3 lower,
                    out Vector3 upper,
                    out NavMeshPath path),
                $"{stageName} has no complete NavMesh path across an elevation change.");

            Vector3 projected = lower;
            for (int i = 1; i < path.corners.Length; i++)
            {
                Vector3 corner = path.corners[i];
                int guard = 0;
                while (true)
                {
                    Vector3 planar = corner - projected;
                    planar.y = 0f;
                    if (planar.magnitude <= 0.025f)
                    {
                        break;
                    }

                    Vector3 next = projected;
                    bool projectedStep = movement.TryProjectDisplacement(
                        projected,
                        planar.normalized * Mathf.Min(0.1f, planar.magnitude),
                        out next);
                    Require(++guard < 1024 && projectedStep,
                        $"{stageName} NavMesh ground projector cannot traverse its elevation path.");
                    projected = next;
                }
            }

            Require(Mathf.Abs(projected.y - lower.y) >= 0.3f &&
                    Mathf.Abs(projected.y - upper.y) <= 0.15f,
                $"{stageName} ground projector did not follow the upper NavMesh height. " +
                $"lower={lower}, upper={upper}, projected={projected}.");
        }

        private static void StartRigidbodyMovementProbe(
            PlayerHealth player,
            string stageName)
        {
            Rigidbody body = player == null ? null : player.GetComponent<Rigidbody>();
            CapsuleCollider capsule = player == null
                ? null
                : player.GetComponent<CapsuleCollider>();
            NavMeshGroundMovement movement = player == null
                ? null
                : player.GetComponent<NavMeshGroundMovement>();
            Require(body != null && capsule != null && movement != null,
                $"{stageName} Rigidbody ground-movement probe is missing player components.");
            Require(NavMesh.SamplePosition(
                    body.position,
                    out NavMeshHit startHit,
                    1.5f,
                    NavMesh.AllAreas),
                $"{stageName} Rigidbody ground-movement probe has no start NavMesh hit.");

            float expectedGroundOffset = body.position.y - startHit.position.y;
            Require(expectedGroundOffset > 0.05f,
                $"{stageName} player root is not above its NavMesh surface before moving: " +
                $"root={body.position}, surface={startHit.position}.");

            Vector3[] directions =
            {
                Vector3.forward,
                Vector3.back,
                Vector3.right,
                Vector3.left,
                new Vector3(1f, 0f, 1f).normalized,
                new Vector3(-1f, 0f, 1f).normalized,
                new Vector3(1f, 0f, -1f).normalized,
                new Vector3(-1f, 0f, -1f).normalized
            };

            Vector3 selectedDirection = Vector3.zero;
            Vector3 projectedRoot = body.position;
            float projectedDistance = 0f;
            for (int i = 0; i < directions.Length; i++)
            {
                if (movement.TryProjectRigidbodyDisplacement(
                        body,
                        directions[i] * 0.1f,
                        out projectedRoot,
                        out projectedDistance))
                {
                    selectedDirection = directions[i];
                    break;
                }
            }

            Require(selectedDirection.sqrMagnitude > 0.0001f &&
                    projectedDistance > 0.001f,
                $"{stageName} player has no local NavMesh displacement for the Rigidbody probe.");
            Require(NavMesh.SamplePosition(
                    projectedRoot,
                    out NavMeshHit projectedHit,
                    1.5f,
                    NavMesh.AllAreas),
                $"{stageName} Rigidbody ground-movement probe produced no target NavMesh hit.");
            Require(Mathf.Abs(
                        (projectedRoot.y - projectedHit.position.y) -
                        expectedGroundOffset) <= 0.03f,
                $"{stageName} projected Rigidbody root lost its ground offset: " +
                $"expected={expectedGroundOffset}, target={projectedRoot}, " +
                $"surface={projectedHit.position}.");

            PlayerMovement playerMovement = player.GetComponent<PlayerMovement>();
            if (playerMovement != null)
            {
                playerMovement.enabled = false;
            }

            movementProbeStartPosition = body.position;
            Require(movement.TryMove(
                    body,
                    selectedDirection * 0.1f,
                    out float movedDistance) &&
                    movedDistance > 0.001f,
                $"{stageName} Rigidbody ground-movement probe could not issue movement.");

            movementProbeBody = body;
            movementProbeCapsule = capsule;
            movementProbeExpectedGroundOffset = expectedGroundOffset;
            movementProbeStartFixedTime = Time.fixedTime;
            movementProbePending = true;
        }

        private static void ValidateRigidbodyMovementProbe(string stageName)
        {
            Require(movementProbeBody != null && movementProbeCapsule != null,
                $"{stageName} Rigidbody ground-movement probe lost its player references.");

            Vector3 actualPosition = movementProbeBody.position;
            Vector3 planarMovement = actualPosition - movementProbeStartPosition;
            planarMovement.y = 0f;
            Require(planarMovement.magnitude > 0.001f,
                $"{stageName} Rigidbody ground-movement probe did not move after a fixed step.");
            Require(NavMesh.SamplePosition(
                    actualPosition,
                    out NavMeshHit groundHit,
                    1.5f,
                    NavMesh.AllAreas),
                $"{stageName} Rigidbody ground-movement probe lost its NavMesh surface.");

            float actualGroundOffset = actualPosition.y - groundHit.position.y;
            Require(Mathf.Abs(
                        actualGroundOffset -
                        movementProbeExpectedGroundOffset) <= 0.05f,
                $"{stageName} Rigidbody ground-movement probe changed root height after physics: " +
                $"expected={movementProbeExpectedGroundOffset}, actual={actualGroundOffset}.");
            Require(movementProbeCapsule.bounds.min.y >= groundHit.position.y - 0.05f,
                $"{stageName} player capsule is embedded below the NavMesh surface after moving: " +
                $"capsuleBottom={movementProbeCapsule.bounds.min.y}, " +
                $"surface={groundHit.position.y}.");
        }

        private static void ClearMovementProbe()
        {
            movementProbePending = false;
            movementProbeBody = null;
            movementProbeCapsule = null;
            movementProbeStartPosition = Vector3.zero;
            movementProbeExpectedGroundOffset = 0f;
            movementProbeStartFixedTime = 0f;
        }

        private static bool TryFindTraversableElevationPair(
            Vector3[] vertices,
            out Vector3 lower,
            out Vector3 upper,
            out NavMeshPath path)
        {
            lower = Vector3.zero;
            upper = Vector3.zero;
            path = null;
            if (vertices == null || vertices.Length == 0)
            {
                return false;
            }

            int stride = Mathf.Max(1, Mathf.CeilToInt(vertices.Length / 72f));
            for (int lowIndex = 0; lowIndex < vertices.Length; lowIndex += stride)
            {
                for (int highIndex = 0; highIndex < vertices.Length; highIndex += stride)
                {
                    if (vertices[highIndex].y - vertices[lowIndex].y < 0.3f)
                    {
                        continue;
                    }

                    NavMeshPath candidate = new NavMeshPath();
                    if (NavMesh.CalculatePath(
                            vertices[lowIndex],
                            vertices[highIndex],
                            NavMesh.AllAreas,
                            candidate) &&
                        candidate.status == NavMeshPathStatus.PathComplete &&
                        candidate.corners.Length > 1)
                    {
                        lower = candidate.corners[0];
                        upper = candidate.corners[candidate.corners.Length - 1];
                        path = candidate;
                        return true;
                    }
                }
            }

            return false;
        }

        private static void Require(bool condition, string message)
        {
            if (!condition)
            {
                throw new InvalidOperationException(message);
            }
        }

        private static void HandleLog(
            string condition,
            string stackTrace,
            LogType type)
        {
            if (!SessionState.GetBool(RunningKey, false) ||
                (type != LogType.Error && type != LogType.Exception &&
                 type != LogType.Assert))
            {
                return;
            }

            RecordFailure(condition + Environment.NewLine + stackTrace);
        }

        private static void RecordFailure(string failure)
        {
            if (SessionState.GetBool(FailedKey, false))
            {
                return;
            }

            SessionState.SetBool(FailedKey, true);
            SessionState.SetString(FailureKey, failure);
        }

        private static void Finish()
        {
            bool failed = SessionState.GetBool(FailedKey, false);
            string failure = SessionState.GetString(FailureKey, string.Empty);
            SessionState.SetBool(RunningKey, false);
            SessionState.SetString(PhaseKey, string.Empty);
            ClearMovementProbe();
            DetachCallbacks();

            if (failed)
            {
                Debug.LogError("Stage5 play-mode smoke test failed:\n" + failure);
                EditorApplication.Exit(1);
                return;
            }

            Debug.Log("Stage5 play-mode smoke test passed.");
            EditorApplication.Exit(0);
        }
    }
}
