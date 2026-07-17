using System;
using System.Globalization;
using InterrogationRoom.Settings;
using UnityEngine;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Runtime-built player settings menu (noir paper-on-graphite styling). Scenes
/// never reference it directly: hosts create it through <see cref="EnsureInstance"/>
/// and route Esc to it. While open it owns Esc; cursor state goes through the
/// host callbacks so PlayerInputGate stays the single cursor owner.
/// </summary>
public sealed class SettingsMenu : MonoBehaviour
{
    private static SettingsMenu instance;
    private static int escapeConsumedFrame = -1;

    private static readonly Color ScrimColor = new Color32(0x14, 0x17, 0x15, 0xD4);
    private static readonly Color PaperColor = new Color32(0xE8, 0xDC, 0xC5, 0xFF);
    private static readonly Color PaperShadowColor = new Color32(0x0C, 0x0E, 0x0D, 0xB4);
    private static readonly Color InkColor = new Color32(0x2B, 0x2A, 0x24, 0xFF);
    private static readonly Color MutedInkColor = new Color32(0x6E, 0x68, 0x57, 0xFF);
    private static readonly Color AccentGreen = new Color32(0x41, 0x5B, 0x4C, 0xFF);
    private static readonly Color AccentGreenDark = new Color32(0x33, 0x47, 0x3C, 0xFF);
    private static readonly Color TrackColor = new Color32(0xCB, 0xBE, 0xA2, 0xFF);
    private static readonly Color ButtonPaperColor = new Color32(0xD9, 0xCB, 0xAF, 0xFF);
    private static readonly Color DestructiveRedColor = new Color32(0xC2, 0x2E, 0x28, 0xFF);
    private static readonly Color LightTextColor = new Color32(0xE8, 0xE3, 0xD5, 0xFF);

    private Canvas canvas;
    private Font font;
    private Slider sensitivitySlider;
    private Text sensitivityValueLabel;
    private Text contextHintLabel;
    private Text backButtonLabel;
    private Button leaveButton;
    private GameObject leaveDivider;
    private Action onOpened;
    private Action onClosed;
    private Action leaveGame;
    private bool isOpen;

    public static bool IsOpen => instance != null && instance.isOpen;

    public static bool EscapeConsumedThisFrame => escapeConsumedFrame == Time.frameCount;

    public static SettingsMenu EnsureInstance()
    {
        if (instance == null)
        {
            var menuObject = new GameObject("SettingsMenu");
            instance = menuObject.AddComponent<SettingsMenu>();
        }

        return instance;
    }

    public void Configure(
        Action openedCallback,
        Action closedCallback,
        Action leaveGameAction)
    {
        onOpened = openedCallback;
        onClosed = closedCallback;
        leaveGame = leaveGameAction;
        RefreshSectionVisibility();
    }

    public void Open()
    {
        if (isOpen)
        {
            return;
        }

        float sensitivity = GameSettingsService.Current.MouseSensitivity;
        sensitivitySlider.SetValueWithoutNotify(sensitivity);
        UpdateSensitivityLabel(sensitivity);
        RefreshSectionVisibility();
        canvas.enabled = true;
        isOpen = true;
        onOpened?.Invoke();
    }

    public void Close()
    {
        if (!isOpen)
        {
            return;
        }

        isOpen = false;
        canvas.enabled = false;
        onClosed?.Invoke();
    }

    private void Awake()
    {
        instance = this;
        font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        BuildMenu();
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }
    }

    private void Update()
    {
        if (!isOpen)
        {
            return;
        }

        if (WasEscapePressed())
        {
            escapeConsumedFrame = Time.frameCount;
            Close();
        }
    }

    private static bool WasEscapePressed()
    {
#if ENABLE_INPUT_SYSTEM
        return Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame;
#else
        return Input.GetKeyDown(KeyCode.Escape);
#endif
    }

    private void RefreshSectionVisibility()
    {
        if (leaveButton != null)
        {
            leaveButton.gameObject.SetActive(leaveGame != null);
        }

        if (leaveDivider != null)
        {
            leaveDivider.SetActive(leaveGame != null);
        }

        if (contextHintLabel != null)
        {
            contextHintLabel.text = leaveGame != null
                ? "Runda trwa — zmiany ustawień działają natychmiast."
                : "Zmiany ustawień działają natychmiast.";
        }

        if (backButtonLabel != null)
        {
            backButtonLabel.text = leaveGame != null ? "Wróć do gry" : "Wróć do menu";
        }
    }

    private void OnSensitivityChanged(float value)
    {
        GameSettingsService.Current.SetMouseSensitivity(value);
        UpdateSensitivityLabel(GameSettingsService.Current.MouseSensitivity);
    }

    private void UpdateSensitivityLabel(float value)
    {
        sensitivityValueLabel.text = value.ToString("0.0", CultureInfo.InvariantCulture);
    }

    private void OnLeaveClicked()
    {
        Action action = leaveGame;
        Close();
        action?.Invoke();
    }

    private void BuildMenu()
    {
        canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 5000;
        canvas.enabled = false;

        CanvasScaler scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 1f;

        gameObject.AddComponent<GraphicRaycaster>();

        Image scrim = CreateImage(transform, "Scrim", ScrimColor, raycastTarget: true);
        RectTransform scrimRect = scrim.rectTransform;
        scrimRect.anchorMin = Vector2.zero;
        scrimRect.anchorMax = Vector2.one;
        scrimRect.offsetMin = Vector2.zero;
        scrimRect.offsetMax = new Vector2(0f, -72f);

        Image panel = CreateImage(transform, "Panel", PaperColor, raycastTarget: true);
        RectTransform panelRect = panel.rectTransform;
        panelRect.anchorMin = panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.anchoredPosition = Vector2.zero;
        panelRect.sizeDelta = new Vector2(600f, 0f);

        Shadow panelShadow = panel.gameObject.AddComponent<Shadow>();
        panelShadow.effectColor = PaperShadowColor;
        panelShadow.effectDistance = new Vector2(10f, -10f);

        VerticalLayoutGroup layout = panel.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(40, 40, 30, 34);
        layout.spacing = 14f;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        ContentSizeFitter fitter = panel.gameObject.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        CreateLabel(panel.transform, "Title", "USTAWIENIA", 36, InkColor, TextAnchor.MiddleLeft, FontStyle.Bold);
        CreateDivider(panel.transform, "TitleAccent", AccentGreen, 3f);

        contextHintLabel = CreateLabel(
            panel.transform,
            "ContextHint",
            "Zmiany ustawień działają natychmiast.",
            15,
            MutedInkColor,
            TextAnchor.MiddleLeft);

        var sensitivityRow = new GameObject("SensitivityRow", typeof(RectTransform));
        sensitivityRow.transform.SetParent(panel.transform, false);
        HorizontalLayoutGroup rowLayout = sensitivityRow.AddComponent<HorizontalLayoutGroup>();
        rowLayout.childControlWidth = true;
        rowLayout.childControlHeight = true;
        rowLayout.childForceExpandWidth = false;
        rowLayout.childForceExpandHeight = false;
        rowLayout.spacing = 8f;
        Text sensitivityCaption = CreateLabel(
            sensitivityRow.transform, "Caption", "Czułość myszy", 18, InkColor, TextAnchor.MiddleLeft);
        sensitivityCaption.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
        sensitivityValueLabel = CreateLabel(
            sensitivityRow.transform, "Value", "0.0", 18, AccentGreenDark, TextAnchor.MiddleRight, FontStyle.Bold);
        sensitivityValueLabel.gameObject.AddComponent<LayoutElement>().preferredWidth = 64f;

        sensitivitySlider = BuildSensitivitySlider(panel.transform);
        sensitivitySlider.onValueChanged.AddListener(OnSensitivityChanged);

        CreateLabel(
            panel.transform,
            "VoiceHint",
            "V — wycisz / włącz mikrofon",
            16,
            MutedInkColor,
            TextAnchor.MiddleLeft,
            FontStyle.Italic);

        CreateSpacer(panel.transform, "ButtonSpacer", 8f);

        Button backButton = CreateButton(
            panel.transform, "BackButton", "Wróć do menu", AccentGreen, LightTextColor, Close, 52f);
        backButtonLabel = backButton.GetComponentInChildren<Text>();

        leaveDivider = CreateDivider(panel.transform, "LeaveDivider", TrackColor, 1f);
        leaveButton = CreateButton(
            panel.transform, "LeaveButton", "Opuść Rundę", DestructiveRedColor, LightTextColor, OnLeaveClicked, 48f);

        RefreshSectionVisibility();
    }

    private Slider BuildSensitivitySlider(Transform parent)
    {
        var sliderObject = new GameObject("SensitivitySlider", typeof(RectTransform));
        sliderObject.transform.SetParent(parent, false);
        sliderObject.AddComponent<LayoutElement>().preferredHeight = 30f;

        Slider slider = sliderObject.AddComponent<Slider>();

        Image background = CreateImage(sliderObject.transform, "Background", TrackColor, raycastTarget: true);
        RectTransform backgroundRect = background.rectTransform;
        backgroundRect.anchorMin = new Vector2(0f, 0.5f);
        backgroundRect.anchorMax = new Vector2(1f, 0.5f);
        backgroundRect.sizeDelta = new Vector2(0f, 8f);

        var fillArea = new GameObject("Fill Area", typeof(RectTransform)).GetComponent<RectTransform>();
        fillArea.SetParent(sliderObject.transform, false);
        fillArea.anchorMin = new Vector2(0f, 0.5f);
        fillArea.anchorMax = new Vector2(1f, 0.5f);
        fillArea.offsetMin = new Vector2(0f, -4f);
        fillArea.offsetMax = new Vector2(-10f, 4f);

        Image fill = CreateImage(fillArea, "Fill", AccentGreen);
        fill.rectTransform.sizeDelta = new Vector2(10f, 0f);

        var handleArea = new GameObject("Handle Slide Area", typeof(RectTransform)).GetComponent<RectTransform>();
        handleArea.SetParent(sliderObject.transform, false);
        handleArea.anchorMin = Vector2.zero;
        handleArea.anchorMax = Vector2.one;
        handleArea.offsetMin = new Vector2(8f, 0f);
        handleArea.offsetMax = new Vector2(-8f, 0f);

        Image handle = CreateImage(handleArea, "Handle", AccentGreenDark, raycastTarget: true);
        handle.rectTransform.anchorMin = handle.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        handle.rectTransform.sizeDelta = new Vector2(16f, 16f);

        slider.fillRect = fill.rectTransform;
        slider.handleRect = handle.rectTransform;
        slider.targetGraphic = handle;
        slider.direction = Slider.Direction.LeftToRight;
        slider.minValue = GameSettings.MinMouseSensitivity;
        slider.maxValue = GameSettings.MaxMouseSensitivity;
        slider.wholeNumbers = false;
        return slider;
    }

    private Button CreateButton(
        Transform parent,
        string name,
        string label,
        Color background,
        Color textColor,
        Action onClick,
        float height)
    {
        var buttonObject = new GameObject(name, typeof(RectTransform));
        buttonObject.transform.SetParent(parent, false);
        buttonObject.AddComponent<LayoutElement>().preferredHeight = height;

        Image image = buttonObject.AddComponent<Image>();
        image.color = background;

        Button button = buttonObject.AddComponent<Button>();
        button.targetGraphic = image;
        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(0.92f, 0.92f, 0.92f, 1f);
        colors.pressedColor = new Color(0.78f, 0.78f, 0.78f, 1f);
        colors.selectedColor = Color.white;
        button.colors = colors;
        button.onClick.AddListener(() => onClick());

        Text buttonLabel = CreateLabel(
            buttonObject.transform, "Label", label, 22, textColor, TextAnchor.MiddleCenter, FontStyle.Bold);
        RectTransform labelRect = buttonLabel.rectTransform;
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;

        return button;
    }

    private Text CreateLabel(
        Transform parent,
        string name,
        string value,
        int size,
        Color color,
        TextAnchor alignment,
        FontStyle style = FontStyle.Normal)
    {
        var labelObject = new GameObject(name, typeof(RectTransform));
        labelObject.transform.SetParent(parent, false);
        Text label = labelObject.AddComponent<Text>();
        label.font = font;
        label.fontSize = size;
        label.fontStyle = style;
        label.color = color;
        label.alignment = alignment;
        label.text = value;
        label.raycastTarget = false;
        return label;
    }

    private static GameObject CreateDivider(Transform parent, string name, Color color, float height)
    {
        Image divider = CreateImage(parent, name, color);
        divider.gameObject.AddComponent<LayoutElement>().preferredHeight = height;
        return divider.gameObject;
    }

    private static void CreateSpacer(Transform parent, string name, float height)
    {
        var spacerObject = new GameObject(name, typeof(RectTransform));
        spacerObject.transform.SetParent(parent, false);
        spacerObject.AddComponent<LayoutElement>().preferredHeight = height;
    }

    private static Image CreateImage(Transform parent, string name, Color color, bool raycastTarget = false)
    {
        var imageObject = new GameObject(name, typeof(RectTransform));
        imageObject.transform.SetParent(parent, false);
        Image image = imageObject.AddComponent<Image>();
        image.color = color;
        image.raycastTarget = raycastTarget;
        return image;
    }
}
