using System;
using System.Globalization;
using InterrogationRoom.Settings;
using InterrogationRoom.UI;
using UnityEngine;
using UnityEngine.UIElements;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Runtime player settings menu, presented as a sheet from the case file.
/// Scenes never reference it directly: hosts create it through
/// <see cref="EnsureInstance"/> and route Esc to it. While open it owns Esc;
/// cursor state goes through the host callbacks so PlayerInputGate stays the
/// single cursor owner.
/// </summary>
public sealed class SettingsMenu : MonoBehaviour
{
    private const string PanelSettingsResource = "UI/UIPanelSettings";
    private const string VisualTreeResource = "UI/SettingsMenu";

    /// <summary>Draws above the Round UI, which leaves its sorting order at 0.</summary>
    private const float SortingOrder = 100f;

    private static SettingsMenu instance;
    private static int escapeConsumedFrame = -1;

    private UIDocument document;
    private VisualElement scrim;
    private Slider sensitivitySlider;
    private Label sensitivityValueLabel;
    private Label kickerLabel;
    private Label titleLabel;
    private Label contextHintLabel;
    private Label sensitivityCaptionLabel;
    private Label languageCaptionLabel;
    private Label voiceHintLabel;
    private Button polishButton;
    private Button englishButton;
    private Button backButton;
    private Button leaveButton;
    private VisualElement leaveDivider;

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
        scrim.style.display = DisplayStyle.Flex;
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
        scrim.style.display = DisplayStyle.None;
        onClosed?.Invoke();
    }

    private void Awake()
    {
        instance = this;
        BuildMenu();
        GameSettingsService.Current.Changed += OnSettingsChanged;
    }

    private void OnDestroy()
    {
        GameSettingsService.Current.Changed -= OnSettingsChanged;
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

    private void BuildMenu()
    {
        var panelSettings = Resources.Load<PanelSettings>(PanelSettingsResource);
        var visualTree = Resources.Load<VisualTreeAsset>(VisualTreeResource);

        if (panelSettings == null || visualTree == null)
        {
            Debug.LogError(
                $"SettingsMenu could not load '{PanelSettingsResource}' or '{VisualTreeResource}' from Resources.");
            return;
        }

        document = gameObject.AddComponent<UIDocument>();
        document.panelSettings = panelSettings;
        document.sortingOrder = SortingOrder;
        document.visualTreeAsset = visualTree;

        VisualElement root = document.rootVisualElement;
        scrim = root.Q<VisualElement>("settings-scrim");

        kickerLabel = root.Q<Label>("kicker");
        titleLabel = root.Q<Label>("title");
        contextHintLabel = root.Q<Label>("context-hint");
        sensitivityCaptionLabel = root.Q<Label>("sensitivity-caption");
        sensitivityValueLabel = root.Q<Label>("sensitivity-value");
        languageCaptionLabel = root.Q<Label>("language-caption");
        voiceHintLabel = root.Q<Label>("voice-hint");
        sensitivitySlider = root.Q<Slider>("sensitivity-slider");
        polishButton = root.Q<Button>("polish-button");
        englishButton = root.Q<Button>("english-button");
        backButton = root.Q<Button>("back-button");
        leaveButton = root.Q<Button>("leave-button");
        leaveDivider = root.Q<VisualElement>("leave-divider");

        sensitivitySlider.lowValue = GameSettings.MinMouseSensitivity;
        sensitivitySlider.highValue = GameSettings.MaxMouseSensitivity;
        sensitivitySlider.RegisterValueChangedCallback(evt => OnSensitivityChanged(evt.newValue));

        polishButton.clicked += () => SetLanguage(UiLanguage.Polish);
        englishButton.clicked += () => SetLanguage(UiLanguage.English);
        backButton.clicked += Close;
        leaveButton.clicked += OnLeaveClicked;

        scrim.style.display = DisplayStyle.None;
        RefreshLocalizedText();
    }

    private void RefreshSectionVisibility()
    {
        bool inRound = leaveGame != null;

        if (leaveButton != null)
        {
            leaveButton.style.display = inRound ? DisplayStyle.Flex : DisplayStyle.None;
        }

        if (leaveDivider != null)
        {
            leaveDivider.style.display = inRound ? DisplayStyle.Flex : DisplayStyle.None;
        }

        if (contextHintLabel != null)
        {
            contextHintLabel.text = UiText.Get(inRound
                ? "Runda trwa — zmiany ustawień działają natychmiast."
                : "Zmiany ustawień działają natychmiast.");
        }

        if (backButton != null)
        {
            backButton.text = UiText.Get(inRound ? "Wróć do gry" : "Wróć do menu");
        }
    }

    private void OnSettingsChanged()
    {
        RefreshLocalizedText();
    }

    private void SetLanguage(UiLanguage language)
    {
        GameSettingsService.Current.SetLanguage(language);
        RefreshLocalizedText();
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
        Action leave = leaveGame;
        Close();
        leave?.Invoke();
    }

    private void RefreshLocalizedText()
    {
        if (kickerLabel == null)
            return;

        kickerLabel.text = UiText.Get("KARTA USTAWIEŃ • 01");
        titleLabel.text = UiText.Get("USTAWIENIA");
        sensitivityCaptionLabel.text = UiText.Get("Czułość myszy");
        languageCaptionLabel.text = UiText.Get("Język");

        UiLanguage language = GameSettingsService.Current.Language;
        polishButton.text = $"{(language == UiLanguage.Polish ? "● " : string.Empty)}{UiText.Get("Polski")}";
        englishButton.text = $"{(language == UiLanguage.English ? "● " : string.Empty)}{UiText.Get("Angielski")}";

        voiceHintLabel.text = UiText.Get("V — wycisz / włącz mikrofon");
        leaveButton.text = UiText.Get("Opuść Rundę");

        RefreshSectionVisibility();
    }
}
