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
        [SerializeField] private string hintKey = "[E]";
        [SerializeField] private string standUpPrompt = "Wstań";
        [SerializeField] private string heldItemPrefix = "Niesiesz";
        [SerializeField] private string dropHint = "[G] Upuść";

        private PlayerInteractor interactor;
        private PlayerController playerController;
        private Canvas hudCanvas;
        private Image dotImage;
        private RectTransform dotRect;
        private Text hintLabel;
        private Text heldItemLabel;
        private float currentSize;

        private void Awake()
        {
            interactor = GetComponent<PlayerInteractor>();
            playerController = GetComponent<PlayerController>();
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

            bool visible = !PlayerController.CursorReleased &&
                           (playerController == null || !playerController.IsDead);
            if (hudCanvas.enabled != visible)
            {
                hudCanvas.enabled = visible;
            }

            if (!visible)
            {
                return;
            }

            bool timedInteractionActive = interactor.HasActiveTimedInteraction;
            bool targeted = timedInteractionActive || interactor.HasHoveredTarget;
            float desiredSize = targeted ? targetedSize : idleSize;
            currentSize = Mathf.Lerp(currentSize, desiredSize, Time.deltaTime * sizeLerpSpeed);
            dotRect.sizeDelta = new Vector2(currentSize, currentSize);
            dotImage.color = targeted ? targetedColor : idleColor;

            string prompt = ResolvePrompt(targeted, timedInteractionActive);
            bool showHint = !string.IsNullOrEmpty(prompt);
            if (showHint)
            {
                hintLabel.text = timedInteractionActive ? prompt : $"{hintKey} {prompt}";
            }

            if (hintLabel.enabled != showHint)
            {
                hintLabel.enabled = showHint;
            }

            var heldItem = interactor.HeldItem;
            bool showHeldItem = heldItem != null;
            if (showHeldItem)
                heldItemLabel.text = $"■  {heldItemPrefix}: {heldItem.DisplayName}   {dropHint}";
            if (heldItemLabel.enabled != showHeldItem)
                heldItemLabel.enabled = showHeldItem;
        }

        private string ResolvePrompt(bool targeted, bool timedInteractionActive)
        {
            if (playerController != null && playerController.IsSeated)
            {
                return standUpPrompt;
            }

            if (timedInteractionActive)
            {
                int progressPercent = Mathf.RoundToInt(interactor.TimedInteractionProgress01 * 100f);
                return $"{interactor.ActiveTimedInteractionPrompt} {progressPercent}%";
            }

            return targeted ? interactor.HoveredPrompt : null;
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
            hintLabel.text = hintKey;
            hintLabel.raycastTarget = false;
            hintLabel.enabled = false;
            RectTransform hintRect = hintLabel.rectTransform;
            hintRect.anchorMin = hintRect.anchorMax = new Vector2(0.5f, 0.5f);
            hintRect.anchoredPosition = new Vector2(0f, -36f);
            hintRect.sizeDelta = new Vector2(480f, 28f);

            var heldItemObject = new GameObject("HeldItem");
            heldItemObject.transform.SetParent(canvasObject.transform, false);
            heldItemLabel = heldItemObject.AddComponent<Text>();
            heldItemLabel.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            heldItemLabel.fontSize = 17;
            heldItemLabel.fontStyle = FontStyle.Bold;
            heldItemLabel.alignment = TextAnchor.MiddleCenter;
            heldItemLabel.color = new Color32(0xB8, 0xC7, 0xA8, 0xFF);
            heldItemLabel.raycastTarget = false;
            heldItemLabel.enabled = false;
            RectTransform heldItemRect = heldItemLabel.rectTransform;
            heldItemRect.anchorMin = heldItemRect.anchorMax = new Vector2(0.5f, 0.5f);
            heldItemRect.anchoredPosition = new Vector2(0f, -68f);
            heldItemRect.sizeDelta = new Vector2(640f, 26f);

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
