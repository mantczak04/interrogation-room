using UnityEngine;

namespace InterrogationRoom.Gameplay.Ambience
{
    /// <summary>
    /// Fluorescent-tube flicker: slow Perlin sway with occasional hard dropouts.
    /// Drives a realtime Light and, optionally, the emissive panel that houses it.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Light))]
    public sealed class FlickeringLight : MonoBehaviour
    {
        [Header("Intensity")]
        [SerializeField, Min(0f)] private float baseIntensity = 0.7f;
        [SerializeField, Range(0f, 1f)] private float swayAmount = 0.25f;
        [SerializeField, Min(0.01f)] private float swaySpeed = 3.5f;

        [Header("Dropouts")]
        [SerializeField, Range(0f, 1f)] private float dropoutChancePerSecond = 0.35f;
        [SerializeField] private Vector2 dropoutDuration = new Vector2(0.03f, 0.18f);
        [SerializeField, Range(0f, 1f)] private float dropoutLevel = 0.12f;

        [Header("Emissive panel (optional)")]
        [SerializeField] private Renderer panelRenderer;
        [SerializeField] private Color panelEmission = new Color(0.42f, 0.74f, 1.15f);

        private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

        private Light flickerLight;
        private MaterialPropertyBlock propertyBlock;
        private float noiseSeed;
        private float dropoutUntil;

        private void Awake()
        {
            flickerLight = GetComponent<Light>();
            propertyBlock = new MaterialPropertyBlock();
            noiseSeed = Random.value * 100f;
        }

        private void Update()
        {
            float now = Time.time;
            float level;

            if (now < dropoutUntil)
            {
                level = dropoutLevel;
            }
            else
            {
                if (Random.value < dropoutChancePerSecond * Time.deltaTime)
                {
                    dropoutUntil = now + Random.Range(dropoutDuration.x, dropoutDuration.y);
                }

                float sway = Mathf.PerlinNoise(noiseSeed, now * swaySpeed) * 2f - 1f;
                level = 1f + sway * swayAmount;
            }

            flickerLight.intensity = baseIntensity * level;

            if (panelRenderer != null)
            {
                propertyBlock.SetColor(EmissionColorId, panelEmission * Mathf.Max(level, dropoutLevel));
                panelRenderer.SetPropertyBlock(propertyBlock);
            }
        }
    }
}
