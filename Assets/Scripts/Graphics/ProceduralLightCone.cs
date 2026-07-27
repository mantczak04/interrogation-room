using UnityEngine;

namespace InterrogationRoom.Graphics
{
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public sealed class ProceduralLightCone : MonoBehaviour
    {
        [SerializeField, Min(3)] private int segments = 64;
        // A real shaft leaves the shade at the width of its mouth, not from a point, so the
        // mesh is a frustum: topRadius matches the fitting aperture, radius the lit pool below.
        [SerializeField, Min(0f)] private float topRadius = 0.185f;
        [SerializeField, Min(0.01f)] private float radius = 1.6f;
        [SerializeField, Min(0.01f)] private float height = 1.5f;

        private Mesh generatedMesh;

        private void OnEnable()
        {
            RebuildMesh();
        }

        private void OnValidate()
        {
            segments = Mathf.Max(3, segments);
            radius = Mathf.Max(0.01f, radius);
            height = Mathf.Max(0.01f, height);
            topRadius = Mathf.Clamp(topRadius, 0f, radius);
            RebuildMesh();
        }

        private void OnDisable()
        {
            ReleaseMesh();
        }

        private void RebuildMesh()
        {
            MeshFilter meshFilter = GetComponent<MeshFilter>();
            if (meshFilter == null)
            {
                return;
            }

            // One extra column duplicates the seam so the UV wrap does not fold back on itself.
            int columns = segments + 1;
            var vertices = new Vector3[columns * 2];
            var normals = new Vector3[columns * 2];
            var uv = new Vector2[columns * 2];
            var triangles = new int[segments * 6];

            // The beam shader shades by normal, so the normals have to be the analytic
            // frustum normals. RecalculateNormals gives every triangle its own facet normal,
            // which shows up as hard radial wedges running down the beam.
            float step = Mathf.PI * 2f / segments;
            float slope = (radius - topRadius) / height;

            for (int index = 0; index < columns; index++)
            {
                float angle = index * step;
                float cos = Mathf.Cos(angle);
                float sin = Mathf.Sin(angle);
                int topIndex = index * 2;
                int baseIndex = topIndex + 1;

                vertices[topIndex] = new Vector3(cos * topRadius, 0f, sin * topRadius);
                vertices[baseIndex] = new Vector3(cos * radius, -height, sin * radius);
                uv[topIndex] = new Vector2(index / (float)segments, 0f);
                uv[baseIndex] = new Vector2(index / (float)segments, 1f);

                Vector3 normal = new Vector3(cos, slope, sin).normalized;
                normals[topIndex] = normal;
                normals[baseIndex] = normal;

                if (index == segments)
                {
                    continue;
                }

                int nextTopIndex = topIndex + 2;
                int nextBaseIndex = nextTopIndex + 1;
                int triangleIndex = index * 6;
                triangles[triangleIndex] = topIndex;
                triangles[triangleIndex + 1] = nextBaseIndex;
                triangles[triangleIndex + 2] = baseIndex;
                triangles[triangleIndex + 3] = topIndex;
                triangles[triangleIndex + 4] = nextTopIndex;
                triangles[triangleIndex + 5] = nextBaseIndex;
            }

            if (generatedMesh == null)
            {
                generatedMesh = new Mesh
                {
                    name = "Procedural Light Cone",
                    hideFlags = HideFlags.HideAndDontSave
                };
            }
            else
            {
                generatedMesh.Clear();
            }

            generatedMesh.vertices = vertices;
            generatedMesh.uv = uv;
            generatedMesh.triangles = triangles;
            generatedMesh.normals = normals;
            generatedMesh.RecalculateBounds();
            meshFilter.sharedMesh = generatedMesh;
        }

        private void ReleaseMesh()
        {
            if (generatedMesh == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Destroy(generatedMesh);
            }
            else
            {
                DestroyImmediate(generatedMesh);
            }

            generatedMesh = null;
        }
    }
}
