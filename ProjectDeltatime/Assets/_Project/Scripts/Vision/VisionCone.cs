using UnityEngine;

namespace Deltatime.Vision
{
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public sealed class VisionCone : MonoBehaviour
    {
        [SerializeField, Range(1f, 179f)] private float viewAngle = 110f;
        [SerializeField, Min(0.1f)] private float viewDistance = 7f;
        [SerializeField, Range(16, 256)] private int segments = 96;
        [SerializeField, Min(0f)] private float surfaceOffset = 0.035f;
        [SerializeField, Min(0f)] private float rayHeight = 0.35f;
        [SerializeField] private LayerMask occluderMask;
        [SerializeField] private Material coneMaterial;

        private Mesh coneMesh;
        private Vector3[] vertices;
        private int[] triangles;

        public float ViewAngle => viewAngle;
        public float ViewDistance => viewDistance;

        private void Awake()
        {
            coneMesh = new Mesh { name = "Runtime 3D Vision Cone" };
            coneMesh.MarkDynamic();
            GetComponent<MeshFilter>().sharedMesh = coneMesh;
            GetComponent<MeshRenderer>().sharedMaterial = coneMaterial;
            AllocateGeometry();
            RebuildVisibilityMesh();

            if (occluderMask.value == 0 || coneMaterial == null)
            {
                Debug.LogError(
                    $"{nameof(VisionCone)} requires an obstacle mask and cone material.",
                    this);
                enabled = false;
            }
        }

        private void LateUpdate()
        {
            RebuildVisibilityMesh();
        }

        public void Configure(LayerMask obstacleMask, Material material)
        {
            occluderMask = obstacleMask;
            coneMaterial = material;
        }

        public bool ContainsWorldPoint(Vector3 point)
        {
            Vector3 offset = point - transform.position;
            offset.y = 0f;
            float distance = offset.magnitude;
            if (distance > viewDistance)
            {
                return false;
            }

            if (distance <= 0.0001f)
            {
                return true;
            }

            if (Vector3.Angle(transform.forward, offset) > viewAngle * 0.5f)
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

            vertices[0] = new Vector3(0f, surfaceOffset, 0f);
            Vector3 rayOrigin = transform.position + (Vector3.up * rayHeight);
            float halfAngle = viewAngle * 0.5f;

            for (int i = 0; i <= segments; i++)
            {
                float angle = Mathf.Lerp(-halfAngle, halfAngle, i / (float)segments);
                Vector3 localDirection =
                    Quaternion.Euler(0f, angle, 0f) * Vector3.forward;
                Vector3 worldDirection = transform.TransformDirection(localDirection);
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

                vertices[i + 1] =
                    (localDirection * visibleDistance) +
                    (Vector3.up * surfaceOffset);
            }

            coneMesh.Clear();
            coneMesh.vertices = vertices;
            coneMesh.triangles = triangles;
            coneMesh.RecalculateBounds();
            coneMesh.RecalculateNormals();
        }

        private void OnValidate()
        {
            viewDistance = Mathf.Max(0.1f, viewDistance);
            segments = Mathf.Clamp(segments, 16, 256);
            if (Application.isPlaying)
            {
                AllocateGeometry();
                RebuildVisibilityMesh();
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
        }
    }
}
