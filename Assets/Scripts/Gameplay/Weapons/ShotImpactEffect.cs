using UnityEngine;

namespace InterrogationRoom.Gameplay.Weapons
{
    [DisallowMultipleComponent]
    public sealed class ShotImpactEffect : MonoBehaviour
    {
        private const float Lifetime = 0.15f;
        private const float StartScale = 0.07f;
        private const float EndScale = 0.015f;

        private Material impactMaterial;
        private float age;

        public static void Spawn(Vector3 point, Vector3 normal, ShotHitKind hitKind)
        {
            if (hitKind == ShotHitKind.Miss)
            {
                return;
            }

            GameObject impact = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            impact.name = $"Shot Impact ({hitKind})";
            impact.transform.position = point + normal * 0.01f;
            impact.transform.localScale = Vector3.one * StartScale;

            Collider impactCollider = impact.GetComponent<Collider>();
            if (impactCollider != null)
            {
                Destroy(impactCollider);
            }

            ShotImpactEffect effect = impact.AddComponent<ShotImpactEffect>();
            effect.Configure(impact.GetComponent<MeshRenderer>(), hitKind);
        }

        private void Configure(MeshRenderer impactRenderer, ShotHitKind hitKind)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null)
            {
                shader = Shader.Find("Sprites/Default");
            }

            if (shader == null || impactRenderer == null)
            {
                return;
            }

            impactMaterial = new Material(shader)
            {
                color = ColorFor(hitKind)
            };
            impactRenderer.sharedMaterial = impactMaterial;
        }

        private void Update()
        {
            age += Time.deltaTime;
            float progress = Mathf.Clamp01(age / Lifetime);
            transform.localScale = Vector3.one * Mathf.Lerp(StartScale, EndScale, progress);

            if (progress >= 1f)
            {
                Destroy(gameObject);
            }
        }

        private void OnDestroy()
        {
            if (impactMaterial != null)
            {
                Destroy(impactMaterial);
            }
        }

        private static Color ColorFor(ShotHitKind hitKind)
        {
            return hitKind switch
            {
                ShotHitKind.Player => new Color(1f, 0.12f, 0.08f, 1f),
                ShotHitKind.Prop => new Color(0.25f, 0.65f, 1f, 1f),
                _ => new Color(1f, 0.7f, 0.15f, 1f)
            };
        }
    }
}
