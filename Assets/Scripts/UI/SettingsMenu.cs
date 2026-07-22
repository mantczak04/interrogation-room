using System;
using System.Globalization;
using System.Collections.Generic;
using System.Text;
using InterrogationRoom.Networking;
using InterrogationRoom.Settings;
using InterrogationRoom.UI;
using InterrogationRoom.Voice;
using Mirror;
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
    private Slider microphoneLevelSlider;
    private Label sensitivityValueLabel;
    private Label microphoneLevelValueLabel;
    private Label kickerLabel;
    private Label titleLabel;
    private Label contextHintLabel;
    private Label sensitivityCaptionLabel;
    private Label languageCaptionLabel;
    private Label voiceHintLabel;
    private Label microphoneCaptionLabel;
    private Label microphoneStateLabel;
    private Label microphoneTestStatusLabel;
    private Label participantVolumeCaptionLabel;
    private Label participantListEmptyLabel;
    private ScrollView participantList;
    private Button microphoneMuteButton;
    private Button microphoneTestButton;
    private Button polishButton;
    private Button englishButton;
    private Button backButton;
    private Button leaveButton;
    private VisualElement leaveDivider;
    private MicrophoneTestPlayback microphoneTest;
    private VivoxVoiceRuntime voiceRuntime;
    private NetworkRoundCoordinator roundCoordinator;
    private string participantRosterSignature;
    private float nextVoiceRefresh;
    private readonly Dictionary<uint, Label> participantStateLabels = new();
    private readonly Dictionary<uint, Button> participantMuteButtons = new();

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
        RefreshVoiceControls(forceRoster: true);
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
        if (microphoneTest != null)
            microphoneTest.StateChanged -= OnMicrophoneTestStateChanged;
        voiceRuntime?.SetMicrophoneTestActive(false);
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

        if (Time.unscaledTime >= nextVoiceRefresh)
        {
            RefreshVoiceControls();
            nextVoiceRefresh = Time.unscaledTime + 0.15f;
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
        microphoneCaptionLabel = root.Q<Label>("microphone-caption");
        microphoneStateLabel = root.Q<Label>("microphone-state");
        microphoneLevelValueLabel = root.Q<Label>("microphone-level-value");
        microphoneTestStatusLabel = root.Q<Label>("microphone-test-status");
        participantVolumeCaptionLabel = root.Q<Label>("participant-volume-caption");
        participantListEmptyLabel = root.Q<Label>("voice-participant-list-empty");
        participantList = root.Q<ScrollView>("voice-participant-list");
        microphoneLevelSlider = root.Q<Slider>("microphone-level-slider");
        microphoneMuteButton = root.Q<Button>("microphone-mute-button");
        microphoneTestButton = root.Q<Button>("microphone-test-button");
        sensitivitySlider = root.Q<Slider>("sensitivity-slider");
        polishButton = root.Q<Button>("polish-button");
        englishButton = root.Q<Button>("english-button");
        backButton = root.Q<Button>("back-button");
        leaveButton = root.Q<Button>("leave-button");
        leaveDivider = root.Q<VisualElement>("leave-divider");

        sensitivitySlider.lowValue = GameSettings.MinMouseSensitivity;
        sensitivitySlider.highValue = GameSettings.MaxMouseSensitivity;
        sensitivitySlider.RegisterValueChangedCallback(evt => OnSensitivityChanged(evt.newValue));

        microphoneLevelSlider.lowValue = GameSettings.MinVoicePercent;
        microphoneLevelSlider.highValue = GameSettings.MaxVoicePercent;
        microphoneLevelSlider.RegisterValueChangedCallback(
            evt => GameSettingsService.Current.SetMicrophoneLevelPercent(evt.newValue));
        microphoneMuteButton.clicked += OnMicrophoneMuteClicked;
        microphoneTestButton.clicked += OnMicrophoneTestClicked;
        microphoneTest = gameObject.AddComponent<MicrophoneTestPlayback>();
        microphoneTest.SetLevelPercent(GameSettingsService.Current.MicrophoneLevelPercent);
        microphoneTest.StateChanged += OnMicrophoneTestStateChanged;

        polishButton.clicked += () => SetLanguage(UiLanguage.Polish);
        englishButton.clicked += () => SetLanguage(UiLanguage.English);
        backButton.clicked += Close;
        leaveButton.clicked += OnLeaveClicked;

        UiSounds.Bind(root);

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
        RefreshVoiceControls();
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

    private void OnMicrophoneMuteClicked()
    {
        GameSettings settings = GameSettingsService.Current;
        settings.SetMicrophoneMuted(!settings.MicrophoneMuted);
    }

    private void OnMicrophoneTestClicked() => microphoneTest?.StartOrStop();

    private void OnMicrophoneTestStateChanged()
    {
        SyncMicrophoneTestMute();
        RefreshVoiceControls();
    }

    private void RefreshVoiceControls(bool forceRoster = false)
    {
        GameSettings settings = GameSettingsService.Current;
        if (microphoneLevelSlider != null)
            microphoneLevelSlider.SetValueWithoutNotify(settings.MicrophoneLevelPercent);
        if (microphoneLevelValueLabel != null)
            microphoneLevelValueLabel.text = $"{Mathf.RoundToInt(settings.MicrophoneLevelPercent)}%";
        microphoneTest?.SetLevelPercent(settings.MicrophoneLevelPercent);

        if (voiceRuntime == null)
            voiceRuntime = VivoxVoiceRuntime.Instance ?? FindFirstObjectByType<VivoxVoiceRuntime>();
        if (roundCoordinator == null)
            roundCoordinator = FindFirstObjectByType<NetworkRoundCoordinator>();
        SyncMicrophoneTestMute();

        bool muted = settings.MicrophoneMuted;
        if (microphoneStateLabel != null)
            microphoneStateLabel.text = UiText.Get(muted ? "MIKROFON WYCISZONY" : "MIKROFON WŁĄCZONY");
        if (microphoneMuteButton != null)
            microphoneMuteButton.text = UiText.Get(muted ? "Włącz mikrofon" : "Wycisz mikrofon");

        RefreshMicrophoneTestText();
        RefreshParticipantRoster(forceRoster);
        RefreshParticipantStates();
    }

    private void SyncMicrophoneTestMute()
    {
        if (voiceRuntime == null || microphoneTest == null)
            return;

        bool active = microphoneTest.State == MicrophoneTestPlayback.TestState.Starting ||
            microphoneTest.State == MicrophoneTestPlayback.TestState.Monitoring;
        voiceRuntime.SetMicrophoneTestActive(active);
    }

    private void RefreshMicrophoneTestText()
    {
        if (microphoneTestButton == null || microphoneTestStatusLabel == null || microphoneTest == null)
            return;

        switch (microphoneTest.State)
        {
            case MicrophoneTestPlayback.TestState.Starting:
                microphoneTestButton.text = UiText.Get("Zatrzymaj odsłuch");
                microphoneTestStatusLabel.text = UiText.Get("Uruchamianie odsłuchu mikrofonu…");
                break;
            case MicrophoneTestPlayback.TestState.Monitoring:
                microphoneTestButton.text = UiText.Get("Zatrzymaj odsłuch");
                microphoneTestStatusLabel.text = UiText.Get(
                    "Słyszysz mikrofon na żywo — Vivox jest chwilowo wyciszony. Użyj słuchawek, aby uniknąć sprzężenia.");
                break;
            case MicrophoneTestPlayback.TestState.NoInputDevice:
                microphoneTestButton.text = UiText.Get("Test mikrofonu");
                microphoneTestStatusLabel.text = UiText.Get("Nie wykryto mikrofonu.");
                break;
            case MicrophoneTestPlayback.TestState.Failed:
                microphoneTestButton.text = UiText.Get("Spróbuj ponownie");
                microphoneTestStatusLabel.text = UiText.Get("Nie udało się uruchomić odsłuchu mikrofonu.");
                break;
            default:
                microphoneTestButton.text = UiText.Get("Odsłuch mikrofonu");
                microphoneTestStatusLabel.text = UiText.Get(
                    "Usłyszysz siebie od razu. Dźwięk pozostaje lokalny i nie jest wysyłany innym.");
                break;
        }
    }

    private void RefreshParticipantRoster(bool force)
    {
        if (participantList == null)
            return;

        IReadOnlyList<LobbyPlayerInfo> players = roundCoordinator?.PublicLobbyPlayers;
        uint localNetId = NetworkClient.localPlayer != null ? NetworkClient.localPlayer.netId : 0u;
        string signature = BuildParticipantSignature(players, localNetId);
        if (!force && signature == participantRosterSignature)
            return;

        participantRosterSignature = signature;
        participantList.Clear();
        participantStateLabels.Clear();
        participantMuteButtons.Clear();
        int remoteCount = 0;
        if (players != null)
        {
            foreach (LobbyPlayerInfo player in players)
            {
                if (player.IsSimulated ||
                    player.NetworkIdentityNetId == 0u ||
                    player.NetworkIdentityNetId == localNetId)
                {
                    continue;
                }

                remoteCount++;
                uint netId = player.NetworkIdentityNetId;
                var row = new VisualElement();
                row.AddToClassList("voice-participant-row");

                var header = new VisualElement();
                header.AddToClassList("voice-participant-header");
                var name = new Label(player.DisplayName);
                name.AddToClassList("voice-participant-name");
                var state = new Label();
                state.AddToClassList("voice-participant-state");
                header.Add(name);
                header.Add(state);
                row.Add(header);

                var controls = new VisualElement();
                controls.AddToClassList("voice-participant-controls");
                var slider = new Slider(
                    GameSettings.MinVoicePercent,
                    GameSettings.MaxVoicePercent);
                slider.AddToClassList("settings-slider");
                slider.AddToClassList("voice-participant-slider");
                slider.SetValueWithoutNotify(
                    voiceRuntime != null
                        ? voiceRuntime.GetParticipantVolumePercent(netId)
                        : GameSettings.DefaultVoicePercent);
                var value = new Label($"{Mathf.RoundToInt(slider.value)}%");
                value.AddToClassList("voice-participant-value");
                slider.RegisterValueChangedCallback(evt =>
                {
                    voiceRuntime?.SetParticipantVolumePercent(netId, evt.newValue);
                    value.text = $"{Mathf.RoundToInt(evt.newValue)}%";
                });
                var muteButton = new Button(() =>
                {
                    if (voiceRuntime == null)
                        return;
                    voiceRuntime.SetParticipantLocallyMuted(
                        netId,
                        !voiceRuntime.IsParticipantLocallyMuted(netId));
                    RefreshParticipantStates();
                });
                muteButton.AddToClassList("btn");
                muteButton.AddToClassList("btn--paper");
                muteButton.AddToClassList("voice-participant-mute");
                controls.Add(slider);
                controls.Add(value);
                controls.Add(muteButton);
                row.Add(controls);
                participantList.Add(row);
                participantStateLabels[netId] = state;
                participantMuteButtons[netId] = muteButton;
            }
        }

        SetVisible(participantListEmptyLabel, remoteCount == 0);
    }

    private void RefreshParticipantStates()
    {
        foreach (KeyValuePair<uint, Label> entry in participantStateLabels)
        {
            uint netId = entry.Key;
            bool locallyMuted = voiceRuntime != null && voiceRuntime.IsParticipantLocallyMuted(netId);
            bool microphoneMuted = voiceRuntime != null && voiceRuntime.IsNetworkPlayerMicrophoneMuted(netId);
            bool speaking = voiceRuntime != null && voiceRuntime.IsNetworkPlayerSpeaking(netId);
            entry.Value.text = UiText.Get(
                locallyMuted ? "WYCISZONY LOKALNIE" :
                microphoneMuted ? "MIKROFON WYCISZONY" :
                speaking ? "MÓWI" : "POŁĄCZONY");

            if (participantMuteButtons.TryGetValue(netId, out Button button))
                button.text = UiText.Get(locallyMuted ? "Włącz dźwięk" : "Wycisz");
        }
    }

    private static string BuildParticipantSignature(
        IReadOnlyList<LobbyPlayerInfo> players,
        uint localNetId)
    {
        if (players == null)
            return string.Empty;

        var signature = new StringBuilder();
        foreach (LobbyPlayerInfo player in players)
        {
            if (!player.IsSimulated &&
                player.NetworkIdentityNetId != 0u &&
                player.NetworkIdentityNetId != localNetId)
            {
                signature.Append(player.NetworkIdentityNetId)
                    .Append('|')
                    .Append(player.DisplayName)
                    .Append(';');
            }
        }
        return signature.ToString();
    }

    private static void SetVisible(VisualElement element, bool visible)
    {
        if (element != null)
            element.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
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
        microphoneCaptionLabel.text = UiText.Get("Twój mikrofon");
        participantVolumeCaptionLabel.text = UiText.Get("Głośność rozmówców");
        participantListEmptyLabel.text = UiText.Get(
            "Kontrolki rozmówców pojawią się po dołączeniu do lobby.");

        UiLanguage language = GameSettingsService.Current.Language;
        polishButton.text = $"{(language == UiLanguage.Polish ? "● " : string.Empty)}{UiText.Get("Polski")}";
        englishButton.text = $"{(language == UiLanguage.English ? "● " : string.Empty)}{UiText.Get("Angielski")}";

        voiceHintLabel.text = UiText.Get("V — wycisz / włącz mikrofon");
        leaveButton.text = UiText.Get("Opuść Rundę");

        RefreshSectionVisibility();
        RefreshVoiceControls();
    }
}
