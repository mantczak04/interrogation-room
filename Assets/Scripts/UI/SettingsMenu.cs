using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using InterrogationRoom.Settings;
using UnityEngine;
using UnityEngine.UIElements;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace InterrogationRoom.UI
{
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

    private UIDocument document;
    private VisualElement scrim;
    private Slider sensitivitySlider;
    private Label sensitivityValueLabel;
    private Label titleLabel;
    private Label sensitivityCaptionLabel;
    private Label languageCaptionLabel;
    private Label microphoneCaptionLabel;
    private Label participantVolumeCaptionLabel;
    private Label controlsHintLabel;
    private Button polishButton;
    private Button englishButton;
    private Button generalTabButton;
    private Button controlsTabButton;
    private Button soundTabButton;
    private Button graphicsTabButton;
    private VisualElement graphicsSection;
    private DropdownField graphicsQuality;
    private Label graphicsCaption;
    private Label graphicsHint;
    private Button backButton;
    private Button leaveButton;
    private VisualElement generalSection;
    private VisualElement controlsSection;
    private VisualElement soundSection;
    private VoiceSettingsPresenter voicePresenter;
    private InputBindingsSettingsPresenter inputBindingsPresenter;

    private Action onOpened;
    private Action onClosed;
    private Action leaveGame;
    private bool isOpen;
    private SettingsSection activeSection = SettingsSection.General;

    private enum SettingsSection
    {
        General,
        Controls,
        Sound,
        Graphics
    }

    public static bool IsOpen => instance != null && instance.isOpen;

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
        voicePresenter?.SetOpen(true);
        inputBindingsPresenter?.SetOpen(true);
        RefreshSectionVisibility();
        scrim.style.display = DisplayStyle.Flex;
        isOpen = true;
        PlayerInputGate.SetModalInputBlocked(this, true);
        onOpened?.Invoke();
    }

    public void Close()
    {
        if (!isOpen)
        {
            return;
        }

        voicePresenter?.SetOpen(false);
        inputBindingsPresenter?.SetOpen(false);
        isOpen = false;
        scrim.style.display = DisplayStyle.None;
        PlayerInputGate.SetModalInputBlocked(this, false);
        onClosed?.Invoke();
    }

    private void Awake()
    {
        instance = this;
        BuildMenu();
        EscapeInputRouter.EnsureInstance().Register(
            this,
            EscapeHandlerPriority.Settings,
            () => isOpen,
            Close);
        GameSettingsService.Current.Changed += OnSettingsChanged;
    }

    private void OnDestroy()
    {
        GameSettingsService.Current.Changed -= OnSettingsChanged;
        EscapeInputRouter.UnregisterOwner(this);
        PlayerInputGate.SetModalInputBlocked(this, false);
        if (instance == this)
        {
            instance = null;
        }
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

        titleLabel = root.Q<Label>("title");
        sensitivityCaptionLabel = root.Q<Label>("sensitivity-caption");
        sensitivityValueLabel = root.Q<Label>("sensitivity-value");
        languageCaptionLabel = root.Q<Label>("language-caption");
        microphoneCaptionLabel = root.Q<Label>("microphone-caption");
        participantVolumeCaptionLabel = root.Q<Label>("participant-volume-caption");
        controlsHintLabel = root.Q<Label>("controls-hint");
        sensitivitySlider = root.Q<Slider>("sensitivity-slider");
        polishButton = root.Q<Button>("polish-button");
        englishButton = root.Q<Button>("english-button");
        generalTabButton = root.Q<Button>("general-tab-button");
        controlsTabButton = root.Q<Button>("controls-tab-button");
        soundTabButton = root.Q<Button>("sound-tab-button");
        backButton = root.Q<Button>("back-button");
        leaveButton = root.Q<Button>("leave-button");
        generalSection = root.Q<VisualElement>("general-section");
        controlsSection = root.Q<VisualElement>("controls-section");
        soundSection = root.Q<VisualElement>("sound-section");
        graphicsTabButton = root.Q<Button>("graphics-tab-button");
        graphicsSection = root.Q<VisualElement>("graphics-section");
        graphicsQuality = root.Q<DropdownField>("graphics-quality");
        graphicsCaption = root.Q<Label>("graphics-caption");
        graphicsHint = root.Q<Label>("graphics-hint");
        graphicsTabButton.clicked += () => SelectSection(SettingsSection.Graphics);
        graphicsQuality.RegisterValueChangedCallback(evt =>
            GameSettingsService.Current.SetGraphicsQuality(graphicsQuality.index));

        sensitivitySlider.lowValue = GameSettings.MinMouseSensitivity;
        sensitivitySlider.highValue = GameSettings.MaxMouseSensitivity;
        sensitivitySlider.RegisterValueChangedCallback(evt => OnSensitivityChanged(evt.newValue));

        voicePresenter = GetComponent<VoiceSettingsPresenter>() ??
                         gameObject.AddComponent<VoiceSettingsPresenter>();
        voicePresenter.Configure(root);
        inputBindingsPresenter = GetComponent<InputBindingsSettingsPresenter>() ??
                                 gameObject.AddComponent<InputBindingsSettingsPresenter>();
        inputBindingsPresenter.Configure(root);

        polishButton.clicked += () => SetLanguage(UiLanguage.Polish);
        englishButton.clicked += () => SetLanguage(UiLanguage.English);
        generalTabButton.clicked += () => SelectSection(SettingsSection.General);
        controlsTabButton.clicked += () => SelectSection(SettingsSection.Controls);
        soundTabButton.clicked += () => SelectSection(SettingsSection.Sound);
        backButton.clicked += Close;
        leaveButton.clicked += OnLeaveClicked;

        UiSounds.Bind(root);

        scrim.style.display = DisplayStyle.None;
        SelectSection(activeSection);
        RefreshLocalizedText();
    }

    private void RefreshSectionVisibility()
    {
        bool inRound = leaveGame != null;

        if (leaveButton != null)
        {
            leaveButton.style.display = inRound ? DisplayStyle.Flex : DisplayStyle.None;
        }

        if (backButton != null)
        {
            backButton.text = UiText.Get(inRound ? "Wróć do gry" : "Wróć do menu");
        }
    }

    private void SelectSection(SettingsSection section)
    {
        activeSection = section;
        UiControlStates.SetSelected(generalTabButton, section == SettingsSection.General);
        UiControlStates.SetSelected(controlsTabButton, section == SettingsSection.Controls);
        UiControlStates.SetSelected(soundTabButton, section == SettingsSection.Sound);
        UiControlStates.SetSelected(generalSection, section == SettingsSection.General);
        UiControlStates.SetSelected(controlsSection, section == SettingsSection.Controls);
        UiControlStates.SetSelected(soundSection, section == SettingsSection.Sound);
        UiControlStates.SetSelected(graphicsTabButton, section == SettingsSection.Graphics);
        UiControlStates.SetSelected(graphicsSection, section == SettingsSection.Graphics);
    }

    private void OnSettingsChanged()
    {
        RefreshLocalizedText();
        voicePresenter?.Refresh();
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
        if (titleLabel == null)
            return;

        titleLabel.text = UiText.Get("USTAWIENIA");
        generalTabButton.text = UiText.Get("Ogólne").ToUpperInvariant();
        controlsTabButton.text = UiText.Get("Sterowanie").ToUpperInvariant();
        soundTabButton.text = UiText.Get("Dźwięk").ToUpperInvariant();
        graphicsTabButton.text = UiText.Get("Grafika").ToUpperInvariant();
        graphicsCaption.text = UiText.Get("Jakość grafiki");
        graphicsHint.text = UiText.Get("Niższa jakość zmniejsza rozdzielczość obrazu i koszt cieni. Zmiany działają natychmiast.");
        graphicsQuality.choices = new List<string> { UiText.Get("Niska"), UiText.Get("Średnia"), UiText.Get("Wysoka"), "Ultra" };
        graphicsQuality.SetValueWithoutNotify(graphicsQuality.choices[GameSettingsService.Current.GraphicsQuality]);
        sensitivityCaptionLabel.text = UiText.Get("Czułość myszy");
        languageCaptionLabel.text = UiText.Get("Język");
        microphoneCaptionLabel.text = UiText.Get("Twój mikrofon");
        participantVolumeCaptionLabel.text = UiText.Get("Głośność rozmówców");
        controlsHintLabel.text = UiText.Get(
            "Wybierz akcję, a następnie naciśnij nowy klawisz lub przycisk myszy. Esc anuluje zmianę.");

        UiLanguage language = GameSettingsService.Current.Language;
        polishButton.text = UiText.Get("Polski");
        englishButton.text = UiText.Get("Angielski");
        UiControlStates.SetSelected(polishButton, language == UiLanguage.Polish);
        UiControlStates.SetSelected(englishButton, language == UiLanguage.English);

        leaveButton.text = UiText.Get("Opuść Rundę");

        RefreshSectionVisibility();
        voicePresenter?.RefreshLocalizedText();
        inputBindingsPresenter?.RefreshLocalizedText();
    }
}

[DisallowMultipleComponent]
public sealed class InputBindingsSettingsPresenter : MonoBehaviour
{
    private readonly Dictionary<GameInputAction, Label> actionLabels = new();
    private readonly Dictionary<GameInputAction, Button> bindingButtons = new();
    private readonly Dictionary<GameInputAction, Button> resetButtons = new();

    private VisualElement bindingsList;
    private VisualElement rebindBlocker;
    private Label rebindBlockerLabel;
    private Label statusLabel;
    private Button resetAllButton;
    private GameInputAction? pendingAction;
    private bool rebindBlockerHeld;
    private Coroutine releaseBlockerCoroutine;

    public void Configure(VisualElement root)
    {
        if (root == null)
        {
            Debug.LogError("InputBindingsSettingsPresenter requires a UI root.", this);
            enabled = false;
            return;
        }

        bindingsList = root.Q<VisualElement>("input-bindings-list");
        rebindBlocker = root.Q<VisualElement>("input-rebind-blocker");
        rebindBlockerLabel = root.Q<Label>("input-rebind-blocker-label");
        statusLabel = root.Q<Label>("input-binding-status");
        resetAllButton = root.Q<Button>("reset-all-bindings-button");
        if (bindingsList == null ||
            rebindBlocker == null ||
            rebindBlockerLabel == null ||
            statusLabel == null ||
            resetAllButton == null)
        {
            Debug.LogError(
                "SettingsMenu.uxml is missing one or more input-binding controls.",
                this);
            enabled = false;
            return;
        }

        resetAllButton.clicked += ResetAll;

        foreach (GameInputAction action in GameInputBindingCatalog.Actions)
            AddBindingRow(action);

        GameInputBindings.BindingsChanged += RefreshBindings;
        GameInputBindings.RebindStateChanged += OnRebindStateChanged;
        RefreshBindings();
    }

    public void SetOpen(bool open)
    {
        if (!open && GameInputBindings.IsRebinding)
            GameInputBindings.CancelInteractiveRebind();

        if (open)
            RefreshBindings();
    }

    public void RefreshLocalizedText()
    {
        if (bindingsList == null)
            return;

        foreach (KeyValuePair<GameInputAction, Label> entry in actionLabels)
            entry.Value.text = GetActionLabel(entry.Key);
        foreach (Button resetButton in resetButtons.Values)
            resetButton.text = UiText.Get("Reset");
        resetAllButton.text = UiText.Get("Przywróć domyślne sterowanie");
        rebindBlockerLabel.text =
            UiText.Get("Naciśnij nowe wejście. Esc anuluje zmianę.");

        if (pendingAction.HasValue && GameInputBindings.IsRebinding)
        {
            statusLabel.text = UiText.Format(
                "Naciśnij nowe wejście dla: {0}. Esc anuluje.",
                GetActionLabel(pendingAction.Value));
        }
    }

    private void OnDestroy()
    {
        GameInputBindings.BindingsChanged -= RefreshBindings;
        GameInputBindings.RebindStateChanged -= OnRebindStateChanged;
        if (GameInputBindings.IsRebinding)
            GameInputBindings.CancelInteractiveRebind();
    }

    private void AddBindingRow(GameInputAction action)
    {
        var row = new VisualElement();
        row.AddToClassList("input-binding-row");

        var actionLabel = new Label();
        actionLabel.AddToClassList("input-binding-action");
        var bindingButton = new Button(() => BeginRebind(action));
        bindingButton.AddToClassList("btn");
        bindingButton.AddToClassList("btn--paper");
        bindingButton.AddToClassList("input-binding-button");
        var resetButton = new Button(() => ResetOne(action));
        resetButton.AddToClassList("btn");
        resetButton.AddToClassList("btn--quiet");
        resetButton.AddToClassList("input-binding-reset");

        row.Add(actionLabel);
        row.Add(bindingButton);
        row.Add(resetButton);
        bindingsList.Add(row);

        actionLabels[action] = actionLabel;
        bindingButtons[action] = bindingButton;
        resetButtons[action] = resetButton;
    }

    private void BeginRebind(GameInputAction action)
    {
        pendingAction = action;
        bool started = GameInputBindings.BeginInteractiveRebind(action, OnRebindCompleted);
        if (!started)
        {
            pendingAction = null;
            statusLabel.text = UiText.Get("Inna zmiana sterowania jest już aktywna.");
            return;
        }

        rebindBlockerHeld = true;
        statusLabel.text = UiText.Format(
            "Naciśnij nowe wejście dla: {0}. Esc anuluje.",
            GetActionLabel(action));
        RefreshBindings();
    }

    private void OnRebindCompleted(InputRebindResult result)
    {
        statusLabel.text = result.Outcome switch
        {
            InputRebindOutcome.Applied => UiText.Format(
                "Przypisano nowe wejście: {0}.",
                GameInputBindings.GetBindingDisplayString(result.Action)),
            InputRebindOutcome.Cancelled => UiText.Get("Zmiana sterowania została anulowana."),
            InputRebindOutcome.Reserved => UiText.Get(
                "Esc, F8 oraz klawisze postaci 1–5 są zarezerwowane."),
            InputRebindOutcome.Conflict => UiText.Format(
                "To wejście jest już przypisane do: {0}.",
                result.ConflictingAction.HasValue
                    ? GetActionLabel(result.ConflictingAction.Value)
                    : UiText.Get("inna akcja")),
            _ => UiText.Get("Nie można przypisać tego wejścia.")
        };
        RefreshBindings();
    }

    private void ResetOne(GameInputAction action)
    {
        GameInputBindings.ResetBinding(action);
        statusLabel.text = UiText.Format(
            "Przywrócono domyślne wejście dla: {0}.",
            GetActionLabel(action));
    }

    private void ResetAll()
    {
        GameInputBindings.ResetAllBindings();
        statusLabel.text = UiText.Get("Przywrócono domyślne sterowanie.");
    }

    private void OnRebindStateChanged()
    {
        if (!GameInputBindings.IsRebinding)
        {
            if (releaseBlockerCoroutine != null)
                StopCoroutine(releaseBlockerCoroutine);
            releaseBlockerCoroutine =
                StartCoroutine(ReleaseRebindBlockerAfterPointerUp());
        }

        RefreshBindings();
    }

    private IEnumerator ReleaseRebindBlockerAfterPointerUp()
    {
        while (IsPointerPressed())
            yield return null;

        yield return new WaitForEndOfFrame();
        rebindBlockerHeld = false;
        pendingAction = null;
        releaseBlockerCoroutine = null;
        RefreshBindings();
    }

    private static bool IsPointerPressed()
    {
#if ENABLE_INPUT_SYSTEM
        return Mouse.current != null &&
               (Mouse.current.leftButton.isPressed ||
                Mouse.current.rightButton.isPressed ||
                Mouse.current.middleButton.isPressed);
#else
        return Input.GetMouseButton(0) ||
               Input.GetMouseButton(1) ||
               Input.GetMouseButton(2);
#endif
    }

    private void RefreshBindings()
    {
        if (bindingsList == null)
            return;

        bool rebinding = GameInputBindings.IsRebinding;
        foreach (GameInputAction action in GameInputBindingCatalog.Actions)
        {
            actionLabels[action].text = GetActionLabel(action);
            bindingButtons[action].text =
                GameInputBindings.GetBindingDisplayString(action);
            bindingButtons[action].SetEnabled(!rebinding);
            resetButtons[action].SetEnabled(!rebinding);
            UiControlStates.SetSelected(
                bindingButtons[action],
                rebinding && pendingAction == action);
        }

        resetAllButton.SetEnabled(!rebinding);
        rebindBlocker.style.display =
            rebinding || rebindBlockerHeld
                ? DisplayStyle.Flex
                : DisplayStyle.None;
    }

    private static string GetActionLabel(GameInputAction action) =>
        UiText.Get(action switch
        {
            GameInputAction.MoveForward => "Ruch do przodu",
            GameInputAction.MoveBackward => "Ruch do tyłu",
            GameInputAction.MoveLeft => "Ruch w lewo",
            GameInputAction.MoveRight => "Ruch w prawo",
            GameInputAction.Sprint => "Sprint",
            GameInputAction.Jump => "Skok",
            GameInputAction.Interact => "Interakcja",
            GameInputAction.Drop => "Upuszczenie",
            GameInputAction.Dance => "Taniec",
            GameInputAction.View => "Zmiana widoku",
            GameInputAction.PrivateObjective => "Panel prywatny",
            GameInputAction.Fire => "Strzał",
            GameInputAction.VoiceMute => "Wyciszenie mikrofonu",
            _ => action.ToString()
        });
}
}
