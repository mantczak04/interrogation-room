using InterrogationRoom.UI;
using Mirror;
using UnityEngine;
using UnityEngine.UI;

namespace InterrogationRoom.Gameplay.Interaction
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PlayerInteractor))]
    public sealed class InteractionReticleHud : NetworkBehaviour
    {
        [Header("Reticle")]
        [SerializeField, Min(2f)] private float idleSize = 6f;
        [SerializeField, Min(2f)] private float targetedSize = 12f;
        [SerializeField] private Color idleColor = new Color(1f, 1f, 1f, 0.7f);
        [SerializeField] private Color targetedColor = new Color(1f, 0.78f, 0.43f, 1f);
        [SerializeField, Min(1f)] private float sizeLerpSpeed = 14f;

        [Header("Copy")]
        [SerializeField] private string standUpPrompt = "Wstań";
        [SerializeField] private string heldItemPrefix = "Niesiesz";
        [SerializeField] private string dropHint = "[G] Upuść";

        [Header("Motion")]
        [SerializeField, Min(1f)] private float panelLerpSpeed = 15f;
        [SerializeField, Min(1f)] private float progressLerpSpeed = 12f;
        [SerializeField, Min(0f)] private float feedbackKick = 0.075f;

        private static readonly Color PanelColor = new Color(0.055f, 0.065f, 0.06f, 0.94f);
        private static readonly Color PanelEdgeColor = new Color(0.12f, 0.14f, 0.12f, 0.98f);
        private static readonly Color PrimaryTextColor = new Color(0.94f, 0.91f, 0.82f, 1f);
        private static readonly Color SecondaryTextColor = new Color(0.66f, 0.68f, 0.62f, 1f);
        private static readonly Color SuccessColor = new Color(0.42f, 0.78f, 0.48f, 1f);
        private static readonly Color WarningColor = new Color(0.95f, 0.68f, 0.28f, 1f);
        private static readonly Color CancelledColor = new Color(0.85f, 0.31f, 0.27f, 1f);

        private PlayerInteractor interactor;
        private PlayerController playerController;
        private Canvas hudCanvas;
        private Image dotImage;
        private RectTransform dotRect;
        private Image progressTrack;
        private Image progressFill;
        private CanvasGroup promptGroup;
        private RectTransform promptRect;
        private Image promptBackground;
        private Image promptEdge;
        private Image keyBackground;
        private Text keyLabel;
        private Text eyebrowLabel;
        private Text titleLabel;
        private Text instructionLabel;
        private CanvasGroup heldItemGroup;
        private RectTransform heldItemRect;
        private Image heldItemEdge;
        private Text heldItemLabel;
        private float currentSize;
        private float promptVisibility;
        private float heldItemVisibility;
        private float displayedProgress;
        private float kickAmount;
        private InteractionHudMode previousMode;
        private Component previousTarget;
        private Sprite dotSprite;
        private Sprite ringSprite;
        private Sprite roundedSprite;
        private Font runtimeFont;

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
                Destroy(hudCanvas.gameObject);

            DestroyGeneratedSprite(dotSprite);
            DestroyGeneratedSprite(ringSprite);
            DestroyGeneratedSprite(roundedSprite);
        }

        private void Update()
        {
            if (!isLocalPlayer || hudCanvas == null)
                return;

            bool visible = !PlayerController.CursorReleased &&
                           (playerController == null || !playerController.IsDead);
            if (hudCanvas.enabled != visible)
                hudCanvas.enabled = visible;

            if (!visible)
                return;

            float deltaTime = Time.unscaledDeltaTime;
            InteractionHudMode mode = ResolveMode();
            bool hasPanel = mode != InteractionHudMode.Hidden;
            bool targeted = hasPanel || interactor.HasHoveredTarget;

            UpdateReticle(mode, targeted, deltaTime);
            UpdatePrompt(mode, hasPanel, deltaTime);
            UpdateHeldItem(deltaTime);
        }

        private InteractionHudMode ResolveMode()
        {
            if (playerController != null && playerController.IsSeated)
                return InteractionHudMode.Seated;

            if (interactor.HasActiveTimedInteraction)
                return InteractionHudMode.Active;

            if (interactor.HasInteractionFeedback)
            {
                switch (interactor.FeedbackKind)
                {
                    case InteractionFeedbackKind.Success:
                        return InteractionHudMode.Success;
                    case InteractionFeedbackKind.Warning:
                        return InteractionHudMode.Warning;
                    default:
                        return InteractionHudMode.Cancelled;
                }
            }

            if (!interactor.HasHoveredTarget)
                return InteractionHudMode.Hidden;

            return interactor.HoveredInteractionRequiresHold
                ? InteractionHudMode.HoldAvailable
                : InteractionHudMode.Available;
        }

        private void UpdateReticle(
            InteractionHudMode mode,
            bool targeted,
            float deltaTime)
        {
            float desiredSize = targeted ? targetedSize : idleSize;
            currentSize = Damp(currentSize, desiredSize, sizeLerpSpeed, deltaTime);
            dotRect.sizeDelta = new Vector2(currentSize, currentSize);

            Color accent = ResolveAccent(mode);
            dotImage.color = targeted ? accent : idleColor;

            bool showProgress = mode == InteractionHudMode.Active ||
                                mode == InteractionHudMode.Success;
            float targetProgress = mode == InteractionHudMode.Success
                ? 1f
                : interactor.TimedInteractionProgress01;
            displayedProgress = Damp(
                displayedProgress,
                targetProgress,
                progressLerpSpeed,
                deltaTime);
            progressFill.fillAmount = displayedProgress;
            progressFill.color = accent;
            progressTrack.enabled = showProgress;
            progressFill.enabled = showProgress;
        }

        private void UpdatePrompt(
            InteractionHudMode mode,
            bool hasPanel,
            float deltaTime)
        {
            Component currentTarget = interactor.HoveredTarget;
            bool changed = mode != previousMode || currentTarget != previousTarget;
            if (changed && hasPanel)
            {
                if (IsOutcome(mode))
                    kickAmount = feedbackKick;
                else if (currentTarget != previousTarget)
                    kickAmount = feedbackKick * 0.45f;
            }

            previousMode = mode;
            previousTarget = currentTarget;

            float targetVisibility = hasPanel ? 1f : 0f;
            promptVisibility = Damp(
                promptVisibility,
                targetVisibility,
                panelLerpSpeed,
                deltaTime);
            promptGroup.alpha = promptVisibility;
            promptGroup.interactable = false;
            promptGroup.blocksRaycasts = false;

            kickAmount = Damp(kickAmount, 0f, panelLerpSpeed * 1.35f, deltaTime);
            float hiddenScale = Mathf.Lerp(0.965f, 1f, promptVisibility);
            float scale = hiddenScale + kickAmount;
            promptRect.localScale = new Vector3(scale, scale, 1f);

            if (!hasPanel && promptVisibility < 0.01f)
                return;

            string action = ResolveAction(mode);
            InteractionHudCopy copy = InteractionHudPresentation.Build(
                mode,
                action,
                interactor.TimedInteractionProgress01,
                UiText.CurrentLanguage);

            keyLabel.text = copy.Key;
            eyebrowLabel.text = copy.Eyebrow;
            titleLabel.text = copy.Title;
            instructionLabel.text = copy.Instruction;

            Color accent = ResolveAccent(mode);
            promptEdge.color = accent;
            keyBackground.color = new Color(accent.r, accent.g, accent.b, 0.18f);
            keyLabel.color = accent;
            eyebrowLabel.color = accent;
            promptBackground.color = PanelColor;
        }

        private string ResolveAction(InteractionHudMode mode)
        {
            switch (mode)
            {
                case InteractionHudMode.Active:
                    return interactor.ActiveTimedInteractionPrompt;
                case InteractionHudMode.Success:
                case InteractionHudMode.Warning:
                case InteractionHudMode.Cancelled:
                    return interactor.InteractionFeedback;
                case InteractionHudMode.Seated:
                    return standUpPrompt;
                default:
                    return interactor.HoveredPrompt;
            }
        }

        private void UpdateHeldItem(float deltaTime)
        {
            var heldItem = interactor.HeldItem;
            float targetVisibility = heldItem != null ? 1f : 0f;
            heldItemVisibility = Damp(
                heldItemVisibility,
                targetVisibility,
                panelLerpSpeed,
                deltaTime);
            heldItemGroup.alpha = heldItemVisibility;
            heldItemRect.localScale = new Vector3(
                Mathf.Lerp(0.97f, 1f, heldItemVisibility),
                Mathf.Lerp(0.97f, 1f, heldItemVisibility),
                1f);

            if (heldItem == null)
                return;

            heldItemLabel.text =
                $"{UiText.Get(heldItemPrefix).ToUpperInvariant()}  •  " +
                $"{UiText.Get(heldItem.DisplayName)}     {UiText.Get(dropHint)}";
            heldItemEdge.color = targetedColor;
        }

        private void BuildHud()
        {
            dotSprite = CreateDotSprite();
            ringSprite = CreateRingSprite();
            roundedSprite = CreateRoundedSprite();
            runtimeFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            var canvasObject = new GameObject("InteractionFocusHud");
            canvasObject.transform.SetParent(transform, false);
            hudCanvas = canvasObject.AddComponent<Canvas>();
            hudCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            hudCanvas.sortingOrder = 100;

            CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            BuildReticle(canvasObject.transform);
            BuildPromptPanel(canvasObject.transform);
            BuildHeldItemPanel(canvasObject.transform);

            hudCanvas.enabled = false;
        }

        private void BuildReticle(Transform parent)
        {
            progressTrack = CreateImage("ProgressTrack", parent, ringSprite);
            SetCenteredRect(progressTrack.rectTransform, Vector2.zero, new Vector2(58f, 58f));
            progressTrack.color = new Color(PrimaryTextColor.r, PrimaryTextColor.g, PrimaryTextColor.b, 0.12f);
            progressTrack.enabled = false;

            progressFill = CreateImage("Progress", parent, ringSprite);
            SetCenteredRect(progressFill.rectTransform, Vector2.zero, new Vector2(58f, 58f));
            progressFill.type = Image.Type.Filled;
            progressFill.fillMethod = Image.FillMethod.Radial360;
            progressFill.fillOrigin = 2;
            progressFill.fillClockwise = true;
            progressFill.fillAmount = 0f;
            progressFill.enabled = false;

            dotImage = CreateImage("FocusDot", parent, dotSprite);
            dotRect = dotImage.rectTransform;
            SetCenteredRect(dotRect, Vector2.zero, new Vector2(idleSize, idleSize));
            dotImage.color = idleColor;
            currentSize = idleSize;
        }

        private void BuildPromptPanel(Transform parent)
        {
            GameObject panelObject = new GameObject("InteractionCard", typeof(RectTransform));
            panelObject.transform.SetParent(parent, false);
            promptRect = panelObject.GetComponent<RectTransform>();
            SetCenteredRect(promptRect, new Vector2(0f, -92f), new Vector2(560f, 96f));
            promptGroup = panelObject.AddComponent<CanvasGroup>();
            promptGroup.alpha = 0f;

            promptBackground = panelObject.AddComponent<Image>();
            promptBackground.sprite = roundedSprite;
            promptBackground.type = Image.Type.Sliced;
            promptBackground.color = PanelColor;
            promptBackground.raycastTarget = false;

            promptEdge = CreateImage("Accent", panelObject.transform, null);
            RectTransform edgeRect = promptEdge.rectTransform;
            edgeRect.anchorMin = new Vector2(0f, 0f);
            edgeRect.anchorMax = new Vector2(0f, 1f);
            edgeRect.pivot = new Vector2(0f, 0.5f);
            edgeRect.anchoredPosition = new Vector2(0f, 0f);
            edgeRect.sizeDelta = new Vector2(4f, 0f);

            keyBackground = CreateImage("Keycap", panelObject.transform, roundedSprite);
            keyBackground.type = Image.Type.Sliced;
            SetCenteredRect(keyBackground.rectTransform, new Vector2(-242f, 0f), new Vector2(52f, 52f));

            keyLabel = CreateText(
                "Key",
                keyBackground.transform,
                17,
                FontStyle.Bold,
                TextAnchor.MiddleCenter,
                PrimaryTextColor);
            Stretch(keyLabel.rectTransform, 0f);

            eyebrowLabel = CreateText(
                "Eyebrow",
                panelObject.transform,
                12,
                FontStyle.Bold,
                TextAnchor.LowerLeft,
                targetedColor);
            SetCenteredRect(eyebrowLabel.rectTransform, new Vector2(42f, 27f), new Vector2(470f, 22f));

            titleLabel = CreateText(
                "Action",
                panelObject.transform,
                20,
                FontStyle.Bold,
                TextAnchor.MiddleLeft,
                PrimaryTextColor);
            SetCenteredRect(titleLabel.rectTransform, new Vector2(42f, 2f), new Vector2(470f, 30f));
            titleLabel.resizeTextForBestFit = true;
            titleLabel.resizeTextMinSize = 14;
            titleLabel.resizeTextMaxSize = 20;
            titleLabel.horizontalOverflow = HorizontalWrapMode.Wrap;
            titleLabel.verticalOverflow = VerticalWrapMode.Truncate;

            instructionLabel = CreateText(
                "Instruction",
                panelObject.transform,
                11,
                FontStyle.Normal,
                TextAnchor.UpperLeft,
                SecondaryTextColor);
            SetCenteredRect(instructionLabel.rectTransform, new Vector2(42f, -28f), new Vector2(470f, 20f));
        }

        private void BuildHeldItemPanel(Transform parent)
        {
            GameObject panelObject = new GameObject("HeldItemCard", typeof(RectTransform));
            panelObject.transform.SetParent(parent, false);
            heldItemRect = panelObject.GetComponent<RectTransform>();
            SetCenteredRect(heldItemRect, new Vector2(0f, -164f), new Vector2(520f, 38f));
            heldItemGroup = panelObject.AddComponent<CanvasGroup>();
            heldItemGroup.alpha = 0f;

            Image background = panelObject.AddComponent<Image>();
            background.sprite = roundedSprite;
            background.type = Image.Type.Sliced;
            background.color = PanelEdgeColor;
            background.raycastTarget = false;

            heldItemEdge = CreateImage("Accent", panelObject.transform, null);
            RectTransform edgeRect = heldItemEdge.rectTransform;
            edgeRect.anchorMin = new Vector2(0f, 0f);
            edgeRect.anchorMax = new Vector2(0f, 1f);
            edgeRect.pivot = new Vector2(0f, 0.5f);
            edgeRect.anchoredPosition = Vector2.zero;
            edgeRect.sizeDelta = new Vector2(3f, 0f);

            heldItemLabel = CreateText(
                "HeldItem",
                panelObject.transform,
                13,
                FontStyle.Bold,
                TextAnchor.MiddleCenter,
                PrimaryTextColor);
            Stretch(heldItemLabel.rectTransform, 12f);
        }

        private Color ResolveAccent(InteractionHudMode mode)
        {
            switch (mode)
            {
                case InteractionHudMode.Success:
                    return SuccessColor;
                case InteractionHudMode.Warning:
                    return WarningColor;
                case InteractionHudMode.Cancelled:
                    return CancelledColor;
                default:
                    return targetedColor;
            }
        }

        private static bool IsOutcome(InteractionHudMode mode) =>
            mode == InteractionHudMode.Success ||
            mode == InteractionHudMode.Warning ||
            mode == InteractionHudMode.Cancelled;

        private static Image CreateImage(string name, Transform parent, Sprite sprite)
        {
            var gameObject = new GameObject(name, typeof(RectTransform));
            gameObject.transform.SetParent(parent, false);
            Image image = gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.raycastTarget = false;
            return image;
        }

        private Text CreateText(
            string name,
            Transform parent,
            int fontSize,
            FontStyle fontStyle,
            TextAnchor alignment,
            Color color)
        {
            var gameObject = new GameObject(name, typeof(RectTransform));
            gameObject.transform.SetParent(parent, false);
            Text label = gameObject.AddComponent<Text>();
            if (runtimeFont != null)
                label.font = runtimeFont;
            label.fontSize = fontSize;
            label.fontStyle = fontStyle;
            label.alignment = alignment;
            label.color = color;
            label.raycastTarget = false;
            label.supportRichText = false;
            return label;
        }

        private static void SetCenteredRect(
            RectTransform rect,
            Vector2 position,
            Vector2 size)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        private static void Stretch(RectTransform rect, float inset)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(inset, inset);
            rect.offsetMax = new Vector2(-inset, -inset);
        }

        private static float Damp(float current, float target, float speed, float deltaTime)
        {
            return Mathf.Lerp(current, target, 1f - Mathf.Exp(-speed * deltaTime));
        }

        private static Sprite CreateDotSprite()
        {
            const int size = 64;
            const float center = (size - 1) * 0.5f;
            const float radius = size * 0.5f - 1f;
            return CreateProceduralSprite(
                "InteractionFocusDot",
                size,
                (x, y) => Mathf.Clamp01(radius - Vector2.Distance(new Vector2(x, y), new Vector2(center, center))));
        }

        private static Sprite CreateRingSprite()
        {
            const int size = 128;
            const float center = (size - 1) * 0.5f;
            const float outerRadius = 62f;
            const float innerRadius = 53f;
            return CreateProceduralSprite(
                "InteractionProgressRing",
                size,
                (x, y) =>
                {
                    float distance = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                    float outer = Mathf.Clamp01(outerRadius - distance + 0.5f);
                    float inner = Mathf.Clamp01(distance - innerRadius + 0.5f);
                    return outer * inner;
                });
        }

        private static Sprite CreateRoundedSprite()
        {
            const int size = 64;
            const float center = (size - 1) * 0.5f;
            const float halfExtent = 31f;
            const float radius = 12f;
            return CreateProceduralSprite(
                "InteractionRoundedPanel",
                size,
                (x, y) =>
                {
                    float qx = Mathf.Max(Mathf.Abs(x - center) - (halfExtent - radius), 0f);
                    float qy = Mathf.Max(Mathf.Abs(y - center) - (halfExtent - radius), 0f);
                    float distance = Mathf.Sqrt(qx * qx + qy * qy) - radius;
                    return Mathf.Clamp01(0.5f - distance);
                },
                new Vector4(16f, 16f, 16f, 16f));
        }

        private static Sprite CreateProceduralSprite(
            string name,
            int size,
            System.Func<float, float, float> alphaAt,
            Vector4 border = default)
        {
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = name,
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
                hideFlags = HideFlags.HideAndDontSave
            };

            var pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    byte alpha = (byte)(Mathf.Clamp01(alphaAt(x, y)) * 255f);
                    pixels[y * size + x] = new Color32(255, 255, 255, alpha);
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply(false, true);

            return Sprite.Create(
                texture,
                new Rect(0f, 0f, size, size),
                new Vector2(0.5f, 0.5f),
                100f,
                0,
                SpriteMeshType.FullRect,
                border);
        }

        private static void DestroyGeneratedSprite(Sprite sprite)
        {
            if (sprite == null)
                return;

            if (sprite.texture != null)
                Destroy(sprite.texture);
            Destroy(sprite);
        }
    }
}
