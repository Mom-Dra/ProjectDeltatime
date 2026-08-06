using Deltatime.Replay;
using UnityEngine;
using UnityEngine.Serialization;

namespace Deltatime.Vision
{
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public sealed class VisionCone : MonoBehaviour
    {
        [SerializeField, Range(1f, 179f)] private float viewAngle = 60f;
        [SerializeField, Min(0.1f)] private float viewDistance = 12.5f;
        [SerializeField, Range(16, 256)] private int segments = 96;
        [SerializeField, Min(0f)] private float surfaceOffset = 0.035f;
        [SerializeField, Min(0f)] private float rayHeight = 0.35f;
        [SerializeField] private LayerMask occluderMask;
        [SerializeField] private Material coneMaterial;
        [SerializeField] private StageReplayController replay;

        [Header("Dark Vision Lighting")]
        [SerializeField] private Color visionLightColor =
            new Color(0.55f, 0.78f, 0.9f, 1f);
        [SerializeField, Min(0f)] private float visionLightIntensity = 7.5f;
        [SerializeField, Min(0f)] private float visionLightHeight = 0.7f;
        [SerializeField, Min(0f)] private float visionLightDownwardBias = 0.22f;
        [SerializeField, Range(0.1f, 1f)] private float innerSpotRatio = 0.68f;
        [SerializeField, Min(0f)] private float nearLightIntensity = 4f;
        [FormerlySerializedAs("nearLightRange")]
        [SerializeField, Min(0.1f)] private float nearLightGroundRadius = 4f;
        [SerializeField, Min(0f)] private float nearLightHeight = 1f;

        private Mesh coneMesh;
        private Vector3[] vertices;
        private Vector3[] replayVertices;
        private int[] triangles;
        private Transform lightTrackingTarget;
        private GameObject spotLightObject;
        private GameObject nearLightObject;
        private Light spotLight;
        private Light nearLight;

        public float ViewAngle => viewAngle;
        public float ViewDistance => viewDistance;
        public Light RuntimeVisionSpotLight => spotLight;
        public Light RuntimeNearWallLight => nearLight;

        private void Awake()
        {
            coneMesh = new Mesh { name = "Runtime 3D Vision Cone" };
            coneMesh.MarkDynamic();
            GetComponent<MeshFilter>().sharedMesh = coneMesh;
            GetComponent<MeshRenderer>().sharedMaterial = coneMaterial;
            AllocateGeometry();
            RebuildVisibilityMesh();

            if (occluderMask.value == 0 ||
                coneMaterial == null ||
                replay == null)
            {
                Debug.LogError(
                    $"{nameof(VisionCone)} requires an obstacle mask, cone material, and replay controller.",
                    this);
                enabled = false;
                return;
            }

            CreateDarkVisionLights();
            UpdateDarkVisionLights();
        }

        private void Start()
        {
            replay.RegisterLight(spotLight);
            replay.RegisterLight(nearLight);
        }

        private void LateUpdate()
        {
            RebuildVisibilityMesh();
            UpdateDarkVisionLights();
        }

        public void Configure(
            LayerMask obstacleMask,
            Material material,
            StageReplayController replayController)
        {
            occluderMask = obstacleMask;
            coneMaterial = material;
            replay = replayController;
        }

        public bool ContainsWorldPoint(Vector3 point)
        {
            Vector3 offset = point - transform.position;
            offset.y = 0f;
            float distance = offset.magnitude;
            if (distance <= 0.0001f)
            {
                return true;
            }

            bool isWithinNearLight = distance <= nearLightGroundRadius;
            bool isWithinVisionCone =
                distance <= viewDistance &&
                Vector3.Angle(transform.forward, offset) <= viewAngle * 0.5f;
            if (!isWithinNearLight && !isWithinVisionCone)
            {
                return false;
            }

            Vector3 origin = transform.position + (Vector3.up * rayHeight);
            return !Physics.Raycast(
                origin,
                offset / distance,
                distance,
                occluderMask,
                QueryTriggerInteraction.Ignore);
        }

        public bool RebuildReplayMesh(
            Mesh targetMesh,
            Vector3 worldPosition,
            Quaternion worldRotation)
        {
            if (targetMesh == null || occluderMask.value == 0)
            {
                return false;
            }

            int vertexCount = segments + 2;
            if (replayVertices == null || replayVertices.Length != vertexCount)
            {
                replayVertices = new Vector3[vertexCount];
            }

            if (targetMesh.vertexCount != vertexCount)
            {
                return false;
            }

            PopulateVisibilityVertices(
                replayVertices,
                worldPosition,
                worldRotation);
            targetMesh.SetVertices(replayVertices);
            targetMesh.RecalculateBounds();
            targetMesh.RecalculateNormals();
            return true;
        }

        private void AllocateGeometry()
        {
            vertices = new Vector3[segments + 2];
            triangles = new int[segments * 3];

            for (int i = 0; i < segments; i++)
            {
                int triangleIndex = i * 3;
                triangles[triangleIndex] = 0;
                triangles[triangleIndex + 1] = i + 1;
                triangles[triangleIndex + 2] = i + 2;
            }

            if (coneMesh != null)
            {
                coneMesh.Clear();
                coneMesh.vertices = vertices;
                coneMesh.triangles = triangles;
            }
        }

        private void RebuildVisibilityMesh()
        {
            if (coneMesh == null)
            {
                return;
            }

            if (vertices == null || vertices.Length != segments + 2)
            {
                AllocateGeometry();
            }

            PopulateVisibilityVertices(
                vertices,
                transform.position,
                transform.rotation);
            coneMesh.vertices = vertices;
            coneMesh.RecalculateBounds();
            coneMesh.RecalculateNormals();
        }

        private void PopulateVisibilityVertices(
            Vector3[] targetVertices,
            Vector3 worldPosition,
            Quaternion worldRotation)
        {
            targetVertices[0] = new Vector3(0f, surfaceOffset, 0f);
            Vector3 rayOrigin = worldPosition + (Vector3.up * rayHeight);
            float halfAngle = viewAngle * 0.5f;

            for (int i = 0; i <= segments; i++)
            {
                float angle = Mathf.Lerp(
                    -halfAngle,
                    halfAngle,
                    i / (float)segments);
                Vector3 localDirection =
                    Quaternion.Euler(0f, angle, 0f) * Vector3.forward;
                Vector3 worldDirection = worldRotation * localDirection;
                float visibleDistance = viewDistance;

                if (Physics.Raycast(
                    rayOrigin,
                    worldDirection,
                    out RaycastHit hit,
                    viewDistance,
                    occluderMask,
                    QueryTriggerInteraction.Ignore))
                {
                    visibleDistance = hit.distance;
                }

                targetVertices[i + 1] =
                    (localDirection * visibleDistance) +
                    (Vector3.up * surfaceOffset);
            }
        }

        private void CreateDarkVisionLights()
        {
            lightTrackingTarget = transform.parent != null
                ? transform.parent
                : transform;

            spotLightObject = new GameObject("Runtime Vision Spot Light")
            {
                hideFlags = HideFlags.DontSave
            };
            spotLight = spotLightObject.AddComponent<Light>();
            spotLight.type = LightType.Spot;
            spotLight.color = visionLightColor;
            spotLight.intensity = visionLightIntensity;
            spotLight.range = viewDistance;
            spotLight.spotAngle = viewAngle;
            spotLight.innerSpotAngle = viewAngle * innerSpotRatio;
            spotLight.shadows = LightShadows.Soft;
            spotLight.shadowStrength = 0.9f;
            spotLight.shadowBias = 0.035f;
            spotLight.shadowNormalBias = 0.25f;
            spotLight.renderMode = LightRenderMode.ForcePixel;

            nearLightObject = new GameObject("Runtime Near Wall Light")
            {
                hideFlags = HideFlags.DontSave
            };
            nearLight = nearLightObject.AddComponent<Light>();
            nearLight.type = LightType.Point;
            nearLight.color = visionLightColor;
            nearLight.intensity = nearLightIntensity;
            nearLight.range = CalculateNearLightRange();
            nearLight.shadows = LightShadows.Soft;
            nearLight.shadowStrength = 0.85f;
            nearLight.shadowBias = 0.035f;
            nearLight.shadowNormalBias = 0.25f;
            nearLight.shadowNearPlane = 0.1f;
            nearLight.renderMode = LightRenderMode.ForcePixel;
        }

        private void UpdateDarkVisionLights()
        {
            if (lightTrackingTarget == null || spotLight == null || nearLight == null)
            {
                return;
            }

            Vector3 forward = lightTrackingTarget.forward;
            Vector3 spotDirection =
                (forward + (Vector3.down * visionLightDownwardBias)).normalized;
            Vector3 spotPosition =
                lightTrackingTarget.position +
                (Vector3.up * visionLightHeight);

            spotLightObject.transform.SetPositionAndRotation(
                spotPosition,
                Quaternion.LookRotation(spotDirection, Vector3.up));
            spotLight.color = visionLightColor;
            spotLight.intensity = visionLightIntensity;
            spotLight.range = viewDistance;
            spotLight.spotAngle = viewAngle;
            spotLight.innerSpotAngle = viewAngle * innerSpotRatio;

            nearLightObject.transform.position =
                lightTrackingTarget.position +
                (Vector3.up * nearLightHeight);
            nearLight.color = visionLightColor;
            nearLight.intensity = nearLightIntensity;
            nearLight.range = CalculateNearLightRange();
        }

        private float CalculateNearLightRange()
        {
            if (lightTrackingTarget == null)
            {
                return nearLightGroundRadius;
            }

            float lightHeightFromGround = Mathf.Abs(
                lightTrackingTarget.position.y +
                nearLightHeight -
                transform.position.y);
            return Mathf.Sqrt(
                (nearLightGroundRadius * nearLightGroundRadius) +
                (lightHeightFromGround * lightHeightFromGround));
        }

        private void OnValidate()
        {
            viewDistance = Mathf.Max(0.1f, viewDistance);
            segments = Mathf.Clamp(segments, 16, 256);
            visionLightIntensity = Mathf.Max(0f, visionLightIntensity);
            visionLightHeight = Mathf.Max(0f, visionLightHeight);
            visionLightDownwardBias = Mathf.Max(0f, visionLightDownwardBias);
            innerSpotRatio = Mathf.Clamp(innerSpotRatio, 0.1f, 1f);
            nearLightIntensity = Mathf.Max(0f, nearLightIntensity);
            nearLightGroundRadius = Mathf.Max(0.1f, nearLightGroundRadius);
            nearLightHeight = Mathf.Max(0f, nearLightHeight);
            if (Application.isPlaying)
            {
                AllocateGeometry();
                RebuildVisibilityMesh();
                UpdateDarkVisionLights();
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.2f, 0.95f, 1f, 0.65f);
            Vector3 origin = transform.position + (Vector3.up * surfaceOffset);
            Vector3 left = Quaternion.Euler(0f, -viewAngle * 0.5f, 0f) *
                           transform.forward;
            Vector3 right = Quaternion.Euler(0f, viewAngle * 0.5f, 0f) *
                            transform.forward;
            Gizmos.DrawLine(origin, origin + (left * viewDistance));
            Gizmos.DrawLine(origin, origin + (right * viewDistance));
        }

        private void OnDestroy()
        {
            if (coneMesh != null)
            {
                Destroy(coneMesh);
            }

            if (spotLightObject != null)
            {
                Destroy(spotLightObject);
            }

            if (nearLightObject != null)
            {
                Destroy(nearLightObject);
            }
        }
    }
}
