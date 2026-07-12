using UnityEngine;

namespace InterrogationRoom.Graphics
{
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public sealed class ProceduralLightCone : MonoBehaviour
    {
        [SerializeField, Min(3)] private int segments = 32;
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

            var vertices = new Vector3[segments * 2];
            var uv = new Vector2[segments * 2];
            var triangles = new int[segments * 3];

            for (int index = 0; index < segments; index++)
            {
                float angle = index * Mathf.PI * 2f / segments;
                int apexIndex = index * 2;
                int baseIndex = apexIndex + 1;

                vertices[apexIndex] = Vector3.zero;
                vertices[baseIndex] = new Vector3(Mathf.Cos(angle) * radius, -height, Mathf.Sin(angle) * radius);
                uv[apexIndex] = new Vector2(index / (float)segments, 0f);
                uv[baseIndex] = new Vector2(index / (float)segments, 1f);

                int nextBaseIndex = ((index + 1) % segments) * 2 + 1;
                int triangleIndex = index * 3;
                triangles[triangleIndex] = apexIndex;
                triangles[triangleIndex + 1] = nextBaseIndex;
                triangles[triangleIndex + 2] = baseIndex;
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
            generatedMesh.RecalculateNormals();
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
