using UnityEngine;
using UnityEngine.Events;

namespace InterrogationRoom.Graphics
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Light))]
    public sealed class FlickeringLight : MonoBehaviour
    {
        private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

        [Header("Light")]
        [SerializeField, Min(0f)] private float minimumIntensity = 0.08f;
        [SerializeField, Min(0f)] private float maximumIntensity = 0.8f;
        [SerializeField, Min(0.01f)] private float frequency = 7f;
        [SerializeField, Range(0f, 1f)] private float offTimePercentage = 0.12f;

        [Header("Emission")]
        [SerializeField] private Renderer emissiveRenderer;
        [SerializeField, ColorUsage(true, true)] private Color emissionColor = new Color(1.4f, 2.2f, 1.25f, 1f);

        [Header("Events")]
        [SerializeField] private UnityEvent onFlicker = new UnityEvent();

        private Light controlledLight;
        private MaterialPropertyBlock propertyBlock;
        private float seed;
        private bool wasOff;

        public UnityEvent OnFlicker => onFlicker;

        private void Awake()
        {
            controlledLight = GetComponent<Light>();
            propertyBlock = new MaterialPropertyBlock();
            seed = CalculatePositionSeed(transform.position);
        }

        private void OnValidate()
        {
            maximumIntensity = Mathf.Max(maximumIntensity, minimumIntensity);
            frequency = Mathf.Max(0.01f, frequency);
        }

        private void Update()
        {
            float sample = Mathf.PerlinNoise(seed, Time.time * frequency);
            bool isOff = sample < offTimePercentage;
            float normalizedIntensity = isOff ? 0f : Mathf.InverseLerp(offTimePercentage, 1f, sample);
            float intensity = isOff ? minimumIntensity : Mathf.Lerp(minimumIntensity, maximumIntensity, normalizedIntensity);

            controlledLight.intensity = intensity;
            UpdateEmission(Mathf.InverseLerp(minimumIntensity, maximumIntensity, intensity));

            if (isOff != wasOff)
            {
                onFlicker.Invoke();
                wasOff = isOff;
            }
        }

        private void UpdateEmission(float intensityFactor)
        {
            if (emissiveRenderer == null)
            {
                return;
            }

            emissiveRenderer.GetPropertyBlock(propertyBlock);
            propertyBlock.SetColor(EmissionColorId, emissionColor * intensityFactor);
            emissiveRenderer.SetPropertyBlock(propertyBlock);
        }

        private static float CalculatePositionSeed(Vector3 position)
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 31 + Mathf.RoundToInt(position.x * 100f);
                hash = hash * 31 + Mathf.RoundToInt(position.y * 100f);
                hash = hash * 31 + Mathf.RoundToInt(position.z * 100f);
                return (hash & 0x7fffffff) * 0.0001f;
            }
        }
    }
}
