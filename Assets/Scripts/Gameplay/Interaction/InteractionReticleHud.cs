using Mirror;
using UnityEngine;
using UnityEngine.UI;

namespace InterrogationRoom.Gameplay.Interaction
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PlayerInteractor))]
    public sealed class InteractionReticleHud : NetworkBehaviour
    {
        [Header("Dot")]
        [SerializeField, Min(2f)] private float idleSize = 6f;
        [SerializeField, Min(2f)] private float targetedSize = 12f;
        [SerializeField] private Color idleColor = new Color(1f, 1f, 1f, 0.7f);
        [SerializeField] private Color targetedColor = new Color(1f, 0.78f, 0.43f, 1f);
        [SerializeField, Min(1f)] private float sizeLerpSpeed = 14f;

        [Header("Hint")]
        [SerializeField] private string hintText = "[E]";

        private PlayerInteractor interactor;
        private Canvas hudCanvas;
        private Image dotImage;
        private RectTransform dotRect;
        private Text hintLabel;
        private float currentSize;

        private void Awake()
        {
            interactor = GetComponent<PlayerInteractor>();
        }

        public override void OnStartLocalPlayer()
        {
            base.OnStartLocalPlayer();
            BuildHud();
        }

        private void OnDestroy()
        {
            if (hudCanvas != null)
            {
                Destroy(hudCanvas.gameObject);
            }
        }

        private void Update()
        {
            if (!isLocalPlayer || hudCanvas == null)
            {
                return;
            }

            bool visible = !PlayerController.CursorReleased;
            if (hudCanvas.enabled != visible)
            {
                hudCanvas.enabled = visible;
            }

            if (!visible)
            {
                return;
            }

            bool targeted = interactor.HasHoveredTarget;
            float desiredSize = targeted ? targetedSize : idleSize;
            currentSize = Mathf.Lerp(currentSize, desiredSize, Time.deltaTime * sizeLerpSpeed);
            dotRect.sizeDelta = new Vector2(currentSize, currentSize);
            dotImage.color = targeted ? targetedColor : idleColor;

            if (hintLabel.enabled != targeted)
            {
                hintLabel.enabled = targeted;
            }
        }

        private void BuildHud()
        {
            var canvasObject = new GameObject("InteractionReticleHud");
            canvasObject.transform.SetParent(transform, false);

            hudCanvas = canvasObject.AddComponent<Canvas>();
            hudCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            hudCanvas.sortingOrder = 100;

            CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            var dotObject = new GameObject("Dot");
            dotObject.transform.SetParent(canvasObject.transform, false);
            dotImage = dotObject.AddComponent<Image>();
            dotImage.sprite = CreateDotSprite();
            dotImage.color = idleColor;
            dotImage.raycastTarget = false;
            dotRect = dotImage.rectTransform;
            dotRect.anchorMin = dotRect.anchorMax = new Vector2(0.5f, 0.5f);
            dotRect.anchoredPosition = Vector2.zero;
            currentSize = idleSize;
            dotRect.sizeDelta = new Vector2(currentSize, currentSize);

            var hintObject = new GameObject("Hint");
            hintObject.transform.SetParent(canvasObject.transform, false);
            hintLabel = hintObject.AddComponent<Text>();
            hintLabel.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            hintLabel.fontSize = 20;
            hintLabel.fontStyle = FontStyle.Bold;
            hintLabel.alignment = TextAnchor.MiddleCenter;
            hintLabel.color = targetedColor;
            hintLabel.text = hintText;
            hintLabel.raycastTarget = false;
            hintLabel.enabled = false;
            RectTransform hintRect = hintLabel.rectTransform;
            hintRect.anchorMin = hintRect.anchorMax = new Vector2(0.5f, 0.5f);
            hintRect.anchoredPosition = new Vector2(0f, -36f);
            hintRect.sizeDelta = new Vector2(160f, 28f);

            hudCanvas.enabled = false;
        }

        private static Sprite CreateDotSprite()
        {
            const int size = 64;
            const float center = (size - 1) * 0.5f;
            const float radius = size * 0.5f - 1f;

            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = "InteractionReticleDot",
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
                hideFlags = HideFlags.HideAndDontSave
            };

            Color32[] pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float distance = Mathf.Sqrt((x - center) * (x - center) + (y - center) * (y - center));
                    float alpha = Mathf.Clamp01(radius - distance);
                    pixels[y * size + x] = new Color32(255, 255, 255, (byte)(alpha * 255f));
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply(false, true);

            return Sprite.Create(
                texture,
                new Rect(0f, 0f, size, size),
                new Vector2(0.5f, 0.5f),
                pixelsPerUnit: 100f);
        }
    }
}
