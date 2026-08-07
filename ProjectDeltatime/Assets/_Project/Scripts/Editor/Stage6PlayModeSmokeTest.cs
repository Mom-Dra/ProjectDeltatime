using System;
using Deltatime.Combat;
using Deltatime.Enemies;
using Deltatime.Level;
using Deltatime.Performance;
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
using UnityEngine.SceneManagement;

namespace Deltatime.EditorTools
{
    [InitializeOnLoad]
    public static class Stage6PlayModeSmokeTest
    {
        private const string ScenePath =
            "Assets/_Project/Scenes/Stage6.unity";
        private const string NavigationPath =
            "Assets/_Project/Scenes/Stage6Navigation.asset";
        private const string EnvironmentRootName =
            "Stage 6 - Neon Overlook";
        private const string RunningKey = "Deltatime.Stage6Smoke.Running";
        private const string FailedKey = "Deltatime.Stage6Smoke.Failed";
        private const string FailureKey = "Deltatime.Stage6Smoke.Failure";
        private const string PhaseKey = "Deltatime.Stage6Smoke.Phase";

        private static bool callbacksAttached;
        private static bool validationRan;
        private static double playStartedAt;
        private static bool movementProbePending;
        private static Rigidbody movementProbeBody;
        private static CapsuleCollider movementProbeCapsule;
        private static Vector3 movementProbeStartPosition;
        private static float movementProbeExpectedGroundOffset;
        private static float movementProbeStartFixedTime;

        static Stage6PlayModeSmokeTest()
        {
            if (SessionState.GetBool(RunningKey, false))
            {
                AttachCallbacks();
                EditorApplication.delayCall += ResumePendingSmoke;
            }
        }

        public static void RunFromCommandLine()
        {
            if (SessionState.GetBool(RunningKey, false))
            {
                AttachCallbacks();
                EditorApplication.delayCall += ResumePendingSmoke;
                return;
            }

            SessionState.SetBool(RunningKey, true);
            SessionState.SetBool(FailedKey, false);
            SessionState.SetString(FailureKey, string.Empty);
            SessionState.SetString(PhaseKey, "entering");
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            AttachCallbacks();
            EditorApplication.isPlaying = true;
        }

        private static void ResumePendingSmoke()
        {
            if (!SessionState.GetBool(RunningKey, false) ||
                EditorApplication.isPlayingOrWillChangePlaymode)
            {
                return;
            }

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
            if (!validationRan && elapsed >= 1.1d)
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
                    ValidateRigidbodyMovementProbe("Stage6");
                }
                catch (Exception exception)
                {
                    RecordFailure(exception.ToString());
                }

                ClearMovementProbe();
                EditorApplication.isPlaying = false;
            }
            else if (elapsed >= 25d)
            {
                RecordFailure("Stage6 play-mode smoke test timed out.");
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
            Stage6PerformanceController performance =
                UnityEngine.Object.FindFirstObjectByType<Stage6PerformanceController>();
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
            TopDownCameraController cameraController = camera == null
                ? null
                : camera.GetComponent<TopDownCameraController>();

            Require(scene.path == ScenePath, $"Unexpected scene: {scene.path}");
            Require(player != null && player.IsAlive,
                "Stage6 player did not initialize alive.");
            Require(deadline != null && deadline.ChargesRemaining == 2,
                "Stage6 Deadline charges did not initialize to 2.");
            Require(worldTime != null && worldTime.enabled,
                "Stage6 world time did not initialize.");
            Require(stage != null &&
                    stage.CurrentState == StageController.StageState.Active,
                "Stage6 did not enter the active state.");
            Require(stage != null && stage.RemainingEnemyCount == 5,
                $"Stage6 registered {stage?.RemainingEnemyCount} enemies instead of 5.");
            Require(enemies.Length == 5 && motors.Length == 5 &&
                    shooters.Length == 3 && chasers.Length == 2,
                "Stage6 enemy combat or navigation components are missing.");
            Require(pickups.Length == 2,
                $"Stage6 has {pickups.Length} weapon pickups instead of 2.");
            Require(replay != null && replay.enabled,
                "Stage6 replay did not initialize.");
            Require(replay != null && replay.UsesOptimizedRendererDiscovery &&
                    replay.RendererDiscoveryRootCount == 9 &&
                    Mathf.Approximately(
                        replay.FallbackRendererDiscoveryInterval,
                        0.25f),
                "Stage6 replay did not initialize its dynamic-root renderer discovery.");
            Require(replay != null && replay.TrackedLightCount == 2,
                $"Stage6 replay tracked {replay?.TrackedLightCount} vision lights instead of 2.");
            Require(replay != null && replay.TrackedReplayVisionConeCount == 1,
                "Stage6 replay vision-cone track did not initialize.");
            Require(replay != null && replay.TrackedExcludedVisualCount == 0,
                $"Stage6 replay tracked {replay?.TrackedExcludedVisualCount} " +
                "static rooftop renderers.");
            Require(surface != null && surface.navMeshData != null,
                "Stage6 NavMesh data is missing at runtime.");
            Require(surface != null &&
                    AssetDatabase.GetAssetPath(surface.navMeshData) == NavigationPath,
                "Stage6 is not using its dedicated NavMesh data.");
            Require(environmentRoot != null &&
                    environmentRoot.GetComponent<ReplayExcluded>() != null,
                "Stage6 static environment is not excluded from replay tracks.");
            Require(performance != null && performance.enabled &&
                    performance.IsRuntimePerformanceBudgetApplied,
                "Stage6 runtime performance budget did not initialize.");
            Require(performance != null &&
                    performance.EnvironmentPointLightCount > 0 &&
                    performance.ActiveEnvironmentShadowedPointLightCount <= 2 &&
                    performance.MaximumShadowedEnvironmentPointLights == 2,
                "Stage6 environment point-light shadow budget exceeds two lights.");
            Require(QualitySettings.shadowDistance <= 40.001f &&
                    QualitySettings.shadowCascades <= 2 &&
                    (int)QualitySettings.shadowResolution <=
                    (int)ShadowResolution.Medium,
                "Stage6 runtime quality shadow budget was not applied.");
            Require(CountSoftVisionLights(out string visionLightState) == 2,
                "Stage6 did not preserve both soft-shadow vision lights: " +
                visionLightState);
            Require(visuals.Length == 6,
                $"Stage6 has {visuals.Length} character visual controllers instead of 6.");
            Require(GameObject.Find("Overlook Character - Player") != null &&
                    GameObject.Find("Overlook Character - West Gunner") != null &&
                    GameObject.Find("Overlook Character - Center Chaser") != null &&
                    GameObject.Find("Overlook Character - East Gunner") != null &&
                    GameObject.Find("Overlook Character - North Gunner") != null &&
                    GameObject.Find("Overlook Character - South Chaser") != null,
                "Stage6 Polygon Nightclubs character visuals are missing.");
            Require(camera != null && cameraController != null,
                "Stage6 constrained Stage5-style camera did not initialize.");
            ValidateElevationTraversal(player.GetComponent<NavMeshGroundMovement>(), "Stage6");
            ValidateOffscreenBackgroundCars(environmentRoot);
            ValidateCameraBoundsAtExtremes(player.transform, camera, cameraController);
            Stage6SceneBuilder.ValidateFurnitureNavMeshExclusion(
                environmentRoot,
                "Stage6 runtime");

            RequireOnNavMesh(player.transform.position, "player");
            for (int i = 0; i < enemies.Length; i++)
            {
                RequireOnNavMesh(enemies[i].transform.position, enemies[i].name);
                RequireCompletePath(
                    player.transform.position,
                    enemies[i].transform.position,
                    enemies[i].name);
            }

            NavMeshTriangulation triangulation = NavMesh.CalculateTriangulation();
            Require(triangulation.vertices.Length > 0 && triangulation.indices.Length > 0,
                "Stage6 runtime NavMesh triangulation is empty.");
            Debug.Log(
                $"Stage6 play-mode smoke runtime validation passed: " +
                $"NavMesh vertices={triangulation.vertices.Length}, " +
                $"indices={triangulation.indices.Length}, complete paths=5/5.");

            StartRigidbodyMovementProbe(player, "Stage6");
        }

        private static int CountSoftVisionLights(out string state)
        {
            VisionCone vision = UnityEngine.Object.FindFirstObjectByType<VisionCone>();
            Light spot = vision == null ? null : vision.RuntimeVisionSpotLight;
            Light near = vision == null ? null : vision.RuntimeNearWallLight;
            int count = 0;
            if (spot != null && spot.isActiveAndEnabled &&
                spot.shadows == LightShadows.Soft)
            {
                count++;
            }

            if (near != null && near.isActiveAndEnabled &&
                near.shadows == LightShadows.Soft)
            {
                count++;
            }

            state = $"spot={spot?.shadows.ToString() ?? "missing"}, " +
                $"near={near?.shadows.ToString() ?? "missing"}";
            return count;
        }

        private static void RequireOnNavMesh(Vector3 position, string subject)
        {
            bool found = NavMesh.SamplePosition(
                position,
                out _,
                1.5f,
                NavMesh.AllAreas);
            Require(found,
                $"Stage6 {subject} spawn is not on the baked NavMesh ({position}).");
        }

        private static void RequireCompletePath(
            Vector3 from,
            Vector3 to,
            string subject)
        {
            Require(NavMesh.SamplePosition(
                    from,
                    out NavMeshHit fromHit,
                    1.5f,
                    NavMesh.AllAreas),
                "Stage6 player path origin is invalid.");
            Require(NavMesh.SamplePosition(
                    to,
                    out NavMeshHit toHit,
                    1.5f,
                    NavMesh.AllAreas),
                $"Stage6 {subject} path destination is invalid.");
            NavMeshPath path = new NavMeshPath();
            bool calculated = NavMesh.CalculatePath(
                fromHit.position,
                toHit.position,
                NavMesh.AllAreas,
                path);
            Require(calculated && path.status == NavMeshPathStatus.PathComplete &&
                    path.corners.Length > 0,
                $"Stage6 {subject} did not create a complete NavMesh path.");
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

        private static void ValidateCameraBoundsAtExtremes(
            Transform player,
            Camera camera,
            TopDownCameraController controller)
        {
            SerializedObject serializedController = new SerializedObject(controller);
            SerializedProperty cameraBoundsProperty = serializedController.FindProperty(
                "cameraBounds");
            Require(cameraBoundsProperty != null,
                "Stage6 camera boundary smoke could not read the configured camera bounds.");
            Bounds navBounds = cameraBoundsProperty.boundsValue;
            Require(navBounds.size.x > 0f && navBounds.size.z > 0f,
                "Stage6 camera boundary smoke requires configured NavMesh bounds.");

            const float PlayerRadiusInset = 0.5f;
            Require(navBounds.extents.x > PlayerRadiusInset &&
                    navBounds.extents.z > PlayerRadiusInset,
                "Stage6 camera boundary smoke requires bounds wider than the player radius.");

            Vector3 originalPosition = player.position;
            Quaternion originalRotation = player.rotation;
            float originalAspect = camera.aspect;
            Vector3[] samples = new Vector3[4];
            for (int i = 0; i < samples.Length; i++)
            {
                Require(TryFindCameraEdgeSample(
                            navBounds,
                            PlayerRadiusInset,
                            i,
                            out samples[i]),
                    $"Stage6 camera edge sample {i} is not on the NavMesh.");
            }

            try
            {
                camera.aspect = 16f / 9f;
                for (int i = 0; i < samples.Length; i++)
                {
                    player.SetPositionAndRotation(samples[i], originalRotation);
                    Physics.SyncTransforms();
                    controller.SnapToTarget();

                    Vector3 viewport = camera.WorldToViewportPoint(player.position);
                    Require(viewport.z > 0f &&
                            viewport.x >= -0.01f && viewport.x <= 1.01f &&
                            viewport.y >= -0.01f && viewport.y <= 1.01f,
                        $"Stage6 player left the viewport at camera edge {i}: {viewport}; " +
                        $"player={player.position}, camera={camera.transform.position}.");
                    Require(!Physics.CheckSphere(
                            camera.transform.position,
                            0.2f,
                            (1 << 0) | (1 << 8),
                            QueryTriggerInteraction.Ignore),
                        $"Stage6 camera entered rooftop geometry at edge {i}: " +
                        camera.transform.position);
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

        private static bool TryFindCameraEdgeSample(
            Bounds bounds,
            float inset,
            int edgeIndex,
            out Vector3 sample)
        {
            sample = Vector3.zero;
            NavMeshTriangulation triangulation = NavMesh.CalculateTriangulation();
            if (triangulation.vertices == null ||
                triangulation.vertices.Length == 0)
            {
                return false;
            }

            bool useZAxis = edgeIndex < 2;
            bool useMaximum = edgeIndex == 1 || edgeIndex == 3;
            float innerMinimum = useZAxis
                ? bounds.min.z + inset
                : bounds.min.x + inset;
            float innerMaximum = useZAxis
                ? bounds.max.z - inset
                : bounds.max.x - inset;
            float crossCentre = useZAxis ? bounds.center.x : bounds.center.z;
            float crossExtent = useZAxis ? bounds.extents.x : bounds.extents.z;

            // A north/south (or east/west) sample must not also be the
            // perpendicular extreme: that corner case only verifies one
            // diagonal and can put the player outside the other screen edge.
            // Widen the preferred centre band only if the concave rooftop
            // NavMesh has no vertex in a narrower band.
            for (int band = 1; band <= 4; band++)
            {
                float crossLimit = crossExtent * (band * 0.25f);
                float bestCoordinate = useMaximum
                    ? float.NegativeInfinity
                    : float.PositiveInfinity;
                for (int i = 0; i < triangulation.vertices.Length; i++)
                {
                    Vector3 vertex = triangulation.vertices[i];
                    if (vertex.x < bounds.min.x || vertex.x > bounds.max.x ||
                        vertex.z < bounds.min.z || vertex.z > bounds.max.z)
                    {
                        continue;
                    }

                    float coordinate = useZAxis ? vertex.z : vertex.x;
                    float crossCoordinate = useZAxis ? vertex.x : vertex.z;
                    if (coordinate < innerMinimum || coordinate > innerMaximum ||
                        Mathf.Abs(crossCoordinate - crossCentre) > crossLimit ||
                        (useMaximum && coordinate <= bestCoordinate) ||
                        (!useMaximum && coordinate >= bestCoordinate) ||
                        !NavMesh.SamplePosition(
                            vertex,
                            out NavMeshHit navHit,
                            0.05f,
                            NavMesh.AllAreas))
                    {
                        continue;
                    }

                    bestCoordinate = coordinate;
                    sample = navHit.position;
                }

                if (useMaximum
                    ? bestCoordinate > float.NegativeInfinity
                    : bestCoordinate < float.PositiveInfinity)
                {
                    return true;
                }
            }

            return false;
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

        private static void ValidateOffscreenBackgroundCars(GameObject environmentRoot)
        {
            Transform backgroundFx = FindDescendant(environmentRoot.transform, "Background_FX");
            Require(backgroundFx != null, "Stage6 Background_FX is missing.");
            Transform[] transforms = backgroundFx.GetComponentsInChildren<Transform>(true);
            int carCount = 0;
            for (int i = 0; i < transforms.Length; i++)
            {
                Transform transform = transforms[i];
                if (!transform.name.StartsWith(
                        "FX_Background_Cars_01",
                        StringComparison.Ordinal))
                {
                    continue;
                }

                carCount++;
                Require(!transform.gameObject.activeSelf,
                    $"Stage6 offscreen background car remains active: {transform.name}.");
            }

            Require(carCount == 8,
                $"Stage6 found {carCount} background cars instead of 8.");
        }

        private static Transform FindDescendant(Transform root, string name)
        {
            Transform[] transforms = root.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < transforms.Length; i++)
            {
                if (transforms[i].name == name)
                {
                    return transforms[i];
                }
            }

            return null;
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
                Debug.LogError("Stage6 play-mode smoke test failed:\n" + failure);
                EditorApplication.Exit(1);
                return;
            }

            Debug.Log("Stage6 play-mode smoke test passed.");
            EditorApplication.Exit(0);
        }
    }
}
