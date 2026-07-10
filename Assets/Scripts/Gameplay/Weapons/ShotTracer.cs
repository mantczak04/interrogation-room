using System.Collections;
using UnityEngine;

namespace InterrogationRoom.Gameplay.Weapons
{
    [DisallowMultipleComponent]
    public sealed class ShotTracer : MonoBehaviour
    {
        private LineRenderer lineRenderer;

        public void Initialize(
            Vector3 origin,
            Vector3 endpoint,
            Material material,
            Color color,
            float width,
            float lifetime,
            float length)
        {
            lineRenderer = gameObject.AddComponent<LineRenderer>();
            lineRenderer.useWorldSpace = true;
            lineRenderer.positionCount = 2;
            lineRenderer.startWidth = width;
            lineRenderer.endWidth = width * 0.35f;
            lineRenderer.numCapVertices = 2;

            if (material != null)
            {
                lineRenderer.sharedMaterial = material;
            }

            StartCoroutine(Animate(origin, endpoint, color, lifetime, length));
        }

        private IEnumerator Animate(
            Vector3 origin,
            Vector3 endpoint,
            Color color,
            float lifetime,
            float length)
        {
            Vector3 delta = endpoint - origin;
            float distance = delta.magnitude;
            if (distance < 0.001f)
            {
                Destroy(gameObject);
                yield break;
            }

            Vector3 direction = delta / distance;
            float visibleLength = Mathf.Min(length, distance);
            float elapsed = 0f;

            while (elapsed < lifetime)
            {
                float progress = Mathf.Clamp01(elapsed / lifetime);
                float headDistance = Mathf.Lerp(visibleLength, distance, progress);
                float tailDistance = Mathf.Max(0f, headDistance - visibleLength);
                Color frameColor = color;
                frameColor.a *= 1f - progress;

                lineRenderer.startColor = frameColor;
                lineRenderer.endColor = frameColor;
                lineRenderer.SetPosition(0, origin + direction * tailDistance);
                lineRenderer.SetPosition(1, origin + direction * headDistance);

                elapsed += Time.deltaTime;
                yield return null;
            }

            Destroy(gameObject);
        }
    }
}
