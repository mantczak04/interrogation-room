using UnityEngine;

namespace InterrogationRoom.Gameplay.Interaction
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PlayerInteractor))]
    public sealed class InteractionHighlighter : MonoBehaviour
    {
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int LegacyColorId = Shader.PropertyToID("_Color");
        private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

        [Header("Highlight")]
        [SerializeField] private Color highlightColor = new Color(1f, 0.78f, 0.43f, 1f);
        [SerializeField, Range(0f, 1f)] private float minBlend = 0.15f;
        [SerializeField, Range(0f, 1f)] private float maxBlend = 0.45f;
        [SerializeField, Min(0.1f)] private float pulseSpeed = 4f;

        private PlayerInteractor interactor;
        private MaterialPropertyBlock propertyBlock;
        private Renderer[] targetRenderers = System.Array.Empty<Renderer>();
        private Color[] targetBaseColors = System.Array.Empty<Color>();
        private int[] targetColorIds = System.Array.Empty<int>();

        private void Awake()
        {
            interactor = GetComponent<PlayerInteractor>();
            propertyBlock = new MaterialPropertyBlock();
        }

        private void OnEnable()
        {
            interactor.HoveredTargetChanged += OnHoveredTargetChanged;
        }

        private void OnDisable()
        {
            interactor.HoveredTargetChanged -= OnHoveredTargetChanged;
            ClearTarget();
        }

        private void Update()
        {
            if (targetRenderers.Length == 0)
            {
                return;
            }

            float wave = 0.5f + 0.5f * Mathf.Sin(Time.time * pulseSpeed);
            float blend = Mathf.Lerp(minBlend, maxBlend, wave);

            for (int i = 0; i < targetRenderers.Length; i++)
            {
                Renderer targetRenderer = targetRenderers[i];
                if (targetRenderer == null)
                {
                    continue;
                }

                propertyBlock.Clear();
                propertyBlock.SetColor(targetColorIds[i], Color.Lerp(targetBaseColors[i], highlightColor, blend));
                propertyBlock.SetColor(EmissionColorId, highlightColor * (blend * 0.5f));
                targetRenderer.SetPropertyBlock(propertyBlock);
            }
        }

        private void OnHoveredTargetChanged(Component target)
        {
            ClearTarget();

            if (target == null)
            {
                return;
            }

            Renderer[] renderers = target.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0)
            {
                return;
            }

            targetRenderers = renderers;
            targetBaseColors = new Color[renderers.Length];
            targetColorIds = new int[renderers.Length];

            for (int i = 0; i < renderers.Length; i++)
            {
                Material material = renderers[i].sharedMaterial;
                if (material != null && material.HasProperty(BaseColorId))
                {
                    targetColorIds[i] = BaseColorId;
                    targetBaseColors[i] = material.GetColor(BaseColorId);
                }
                else if (material != null && material.HasProperty(LegacyColorId))
                {
                    targetColorIds[i] = LegacyColorId;
                    targetBaseColors[i] = material.GetColor(LegacyColorId);
                }
                else
                {
                    targetColorIds[i] = BaseColorId;
                    targetBaseColors[i] = Color.white;
                }
            }
        }

        private void ClearTarget()
        {
            foreach (Renderer targetRenderer in targetRenderers)
            {
                if (targetRenderer != null)
                {
                    targetRenderer.SetPropertyBlock(null);
                }
            }

            targetRenderers = System.Array.Empty<Renderer>();
            targetBaseColors = System.Array.Empty<Color>();
            targetColorIds = System.Array.Empty<int>();
        }
    }
}
