using System.Collections.Generic;
using InterrogationRoom.Networking;
using InterrogationRoom.Settings;
using InterrogationRoom.Voice;
using Mirror;
using UnityEngine;
using UnityEngine.UIElements;

namespace InterrogationRoom.UI
{
    [DisallowMultipleComponent]
    public sealed class VoiceSettingsPresenter : MonoBehaviour
    {
        private readonly Dictionary<uint, string> rosterNames = new();
        private readonly Dictionary<uint, VisualElement> participantRows = new();
        private readonly Dictionary<uint, Label> participantNameLabels = new();
        private readonly Dictionary<uint, Label> participantStateLabels = new();
        private readonly Dictionary<uint, Button> participantMuteButtons = new();
        private readonly List<string> inputDeviceIds = new();

        private Slider microphoneLevelSlider;
        private DropdownField inputDeviceDropdown;
        private Label microphoneLevelValueLabel;
        private Label microphoneStateLabel;
        private Label microphoneTestStatusLabel;
        private Label inputDeviceCaptionLabel;
        private Label inputDeviceStatusLabel;
        private ScrollView participantList;
        private Button microphoneMuteButton;
        private Button microphoneTestButton;
        private Button refreshVoiceDevicesButton;
        private VivoxMicrophoneTestPlayback microphoneTest;
        private VivoxVoiceRuntime voiceRuntime;
        private NetworkRoundCoordinator roundCoordinator;
        private float nextRefresh;
        private float nextCoordinatorDiscovery;
        private int lastMicrophoneLevelPercent = int.MinValue;
        private bool isOpen;
        private bool refreshingDeviceDropdown;

        public void Configure(VisualElement root)
        {
            if (root == null)
            {
                Debug.LogError("VoiceSettingsPresenter requires a UI root.", this);
                enabled = false;
                return;
            }

            microphoneLevelSlider = root.Q<Slider>("microphone-level-slider");
            inputDeviceDropdown = root.Q<DropdownField>("input-device-dropdown");
            microphoneLevelValueLabel = root.Q<Label>("microphone-level-value");
            microphoneStateLabel = root.Q<Label>("microphone-state");
            microphoneTestStatusLabel = root.Q<Label>("microphone-test-status");
            inputDeviceCaptionLabel = root.Q<Label>("input-device-caption");
            inputDeviceStatusLabel = root.Q<Label>("input-device-status");
            participantList = root.Q<ScrollView>("voice-participant-list");
            microphoneMuteButton = root.Q<Button>("microphone-mute-button");
            microphoneTestButton = root.Q<Button>("microphone-test-button");
            refreshVoiceDevicesButton = root.Q<Button>("refresh-voice-devices-button");

            if (microphoneLevelSlider == null ||
                inputDeviceDropdown == null ||
                microphoneLevelValueLabel == null ||
                microphoneStateLabel == null ||
                microphoneTestStatusLabel == null ||
                inputDeviceCaptionLabel == null ||
                inputDeviceStatusLabel == null ||
                participantList == null ||
                microphoneMuteButton == null ||
                microphoneTestButton == null ||
                refreshVoiceDevicesButton == null)
            {
                Debug.LogError(
                    "SettingsMenu.uxml is missing one or more required voice controls.",
                    this);
                enabled = false;
                return;
            }

            microphoneLevelSlider.lowValue = GameSettings.MinVoicePercent;
            microphoneLevelSlider.highValue = GameSettings.MaxVoicePercent;
            microphoneLevelSlider.RegisterValueChangedCallback(
                evt => GameSettingsService.Current.SetMicrophoneLevelPercent(evt.newValue));
            microphoneMuteButton.clicked += OnMicrophoneMuteClicked;
            microphoneTestButton.clicked += OnMicrophoneTestClicked;
            refreshVoiceDevicesButton.clicked += OnRefreshVoiceDevicesClicked;
            inputDeviceDropdown.RegisterValueChangedCallback(OnInputDeviceChanged);

            microphoneTest = GetComponent<VivoxMicrophoneTestPlayback>() ??
                             gameObject.AddComponent<VivoxMicrophoneTestPlayback>();
            microphoneTest.SetLevelPercent(GameSettingsService.Current.MicrophoneLevelPercent);
            microphoneTest.StateChanged += OnMicrophoneTestStateChanged;
            VivoxInputDeviceSettings.Changed += OnInputDeviceSettingsChanged;
            InitializeDeviceSettings();
            Refresh(forceRoster: true);
        }

        public void SetOpen(bool open)
        {
            isOpen = open;
            if (open)
            {
                Refresh(forceRoster: true);
                return;
            }

            microphoneTest?.Cancel();
        }

        public void RefreshLocalizedText()
        {
            RefreshMicrophoneTestText();
            RefreshDeviceLocalizedText();
            RefreshParticipantStates();
            Refresh(forceRoster: true);
        }

        public void Refresh(bool forceRoster = false)
        {
            if (microphoneLevelSlider == null)
                return;

            GameSettings settings = GameSettingsService.Current;
            microphoneLevelSlider.SetValueWithoutNotify(settings.MicrophoneLevelPercent);
            int microphonePercent = Mathf.RoundToInt(settings.MicrophoneLevelPercent);
            if (microphoneLevelValueLabel != null &&
                microphonePercent != lastMicrophoneLevelPercent)
            {
                lastMicrophoneLevelPercent = microphonePercent;
                microphoneLevelValueLabel.text = $"{microphonePercent}%";
            }
            microphoneTest?.SetLevelPercent(settings.MicrophoneLevelPercent);

            if (voiceRuntime != VivoxVoiceRuntime.Instance)
                AttachVoiceRuntime(VivoxVoiceRuntime.Instance);
            if (roundCoordinator == null &&
                (forceRoster || Time.unscaledTime >= nextCoordinatorDiscovery))
            {
                roundCoordinator = FindFirstObjectByType<NetworkRoundCoordinator>();
                nextCoordinatorDiscovery = Time.unscaledTime + 1f;
            }

            bool muted = settings.MicrophoneMuted;
            microphoneStateLabel.text =
                UiText.Get(muted ? "MIKROFON WYCISZONY" : "MIKROFON WŁĄCZONY");
            microphoneMuteButton.text =
                UiText.Get(muted ? "Włącz mikrofon" : "Wycisz mikrofon");
            RefreshMicrophoneTestText();
            RefreshVoiceDevices();
            RefreshParticipantRoster();
            RefreshParticipantStates();
        }

        private void Update()
        {
            if (!isOpen || Time.unscaledTime < nextRefresh)
                return;

            Refresh();
            nextRefresh = Time.unscaledTime + 0.15f;
        }

        private void OnDestroy()
        {
            if (microphoneTest != null)
                microphoneTest.StateChanged -= OnMicrophoneTestStateChanged;
            if (voiceRuntime != null)
                _ = voiceRuntime.SetMicrophoneTestActiveAsync(false);
            AttachVoiceRuntime(null);
            VivoxInputDeviceSettings.Changed -= OnInputDeviceSettingsChanged;
        }

        private void AttachVoiceRuntime(VivoxVoiceRuntime runtime)
        {
            if (voiceRuntime == runtime)
                return;

            voiceRuntime = runtime;
        }

        private async void InitializeDeviceSettings()
        {
            try
            {
                await VivoxInputDeviceSettings.EnsureInitializedAsync();
            }
            catch (System.Exception exception)
            {
                Debug.LogWarning(
                    $"[Vivox] Input device settings are unavailable: {exception.Message}",
                    this);
            }

            RefreshVoiceDevices();
            RefreshMicrophoneTestText();
        }

        private void OnMicrophoneMuteClicked()
        {
            GameSettings settings = GameSettingsService.Current;
            settings.SetMicrophoneMuted(!settings.MicrophoneMuted);
        }

        private void OnMicrophoneTestClicked() => microphoneTest?.StartOrStop();

        private void OnRefreshVoiceDevicesClicked()
        {
            VivoxInputDeviceSettings.Refresh();
            RefreshVoiceDevices();
        }

        private void OnInputDeviceChanged(ChangeEvent<string> change)
        {
            if (refreshingDeviceDropdown)
                return;

            int index = inputDeviceDropdown.index;
            if (index < 0 || index >= inputDeviceIds.Count)
                return;

            VivoxInputDeviceSettings.SetPreferredInputDevice(inputDeviceIds[index]);
            inputDeviceStatusLabel.text = UiText.Get("Przełączanie urządzenia wejściowego…");
        }

        private void OnInputDeviceSettingsChanged()
        {
            microphoneTest?.RefreshInputAvailability(
                VivoxInputDeviceSettings.HasEffectiveInputDevice);
            RefreshVoiceDevices();
            RefreshMicrophoneTestText();
        }

        private void OnMicrophoneTestStateChanged()
        {
            Refresh();
        }

        private void RefreshMicrophoneTestText()
        {
            if (microphoneTestButton == null ||
                microphoneTestStatusLabel == null ||
                microphoneTest == null)
            {
                return;
            }

            switch (microphoneTest.State)
            {
                case MicrophoneTestState.Starting:
                    microphoneTestButton.text = UiText.Get("Zatrzymaj odsłuch");
                    microphoneTestStatusLabel.text =
                        UiText.Get("Uruchamianie odsłuchu mikrofonu…");
                    break;
                case MicrophoneTestState.Monitoring:
                    microphoneTestButton.text = UiText.Get("Zatrzymaj odsłuch");
                    microphoneTestStatusLabel.text = UiText.Get(
                        "Słyszysz mikrofon na żywo — Vivox jest chwilowo wyciszony. Użyj słuchawek, aby uniknąć sprzężenia.");
                    break;
                case MicrophoneTestState.NoInputDevice:
                    microphoneTestButton.text = UiText.Get("Test mikrofonu");
                    microphoneTestStatusLabel.text = UiText.Get("Nie wykryto mikrofonu.");
                    break;
                case MicrophoneTestState.Failed:
                    microphoneTestButton.text = UiText.Get("Spróbuj ponownie");
                    microphoneTestStatusLabel.text =
                        UiText.Get("Nie udało się uruchomić odsłuchu mikrofonu.");
                    break;
                default:
                    microphoneTestButton.text = UiText.Get("Odsłuch mikrofonu");
                    microphoneTestStatusLabel.text = string.Empty;
                    break;
            }
        }

        private void RefreshVoiceDevices()
        {
            if (inputDeviceDropdown == null)
                return;

            refreshingDeviceDropdown = true;
            inputDeviceIds.Clear();
            var choices = new List<string>();
            if (!VivoxInputDeviceSettings.IsInitialized)
            {
                choices.Add(UiText.Get("Wykrywanie urządzeń…"));
                inputDeviceDropdown.choices = choices;
                inputDeviceDropdown.SetValueWithoutNotify(choices[0]);
                inputDeviceDropdown.SetEnabled(false);
                refreshVoiceDevicesButton.SetEnabled(
                    !VivoxInputDeviceSettings.IsInitializing);
                microphoneTestButton.SetEnabled(false);
                inputDeviceStatusLabel.text =
                    string.IsNullOrWhiteSpace(VivoxInputDeviceSettings.LastError)
                        ? UiText.Get(
                            "Urządzenia Vivox są inicjalizowane bez logowania i bez dołączania do kanału.")
                        : UiText.Format(
                            "Nie udało się odczytać urządzeń: {0}",
                            VivoxInputDeviceSettings.LastError);
            }
            else if (VivoxInputDeviceSettings.AvailableInputDevices.Count == 0)
            {
                choices.Add(UiText.Get("Brak urządzenia wejściowego"));
                inputDeviceDropdown.choices = choices;
                inputDeviceDropdown.SetValueWithoutNotify(choices[0]);
                inputDeviceDropdown.SetEnabled(false);
                refreshVoiceDevicesButton.SetEnabled(true);
                microphoneTestButton.SetEnabled(false);
                inputDeviceStatusLabel.text = UiText.Get(
                    "Nie wykryto mikrofonu. Odbiór głosu pozostaje aktywny.");
            }
            else
            {
                var duplicateCounts = new Dictionary<string, int>();
                int activeIndex = 0;
                foreach (VoiceAudioDevice device in
                         VivoxInputDeviceSettings.AvailableInputDevices)
                {
                    string baseName = string.IsNullOrWhiteSpace(device.Name)
                        ? UiText.Get("Urządzenie bez nazwy")
                        : device.Name;
                    duplicateCounts.TryGetValue(baseName, out int count);
                    count++;
                    duplicateCounts[baseName] = count;
                    choices.Add(count == 1 ? baseName : $"{baseName} ({count})");
                    inputDeviceIds.Add(device.Id);
                    if (string.Equals(
                            device.Id,
                            VivoxInputDeviceSettings.ActiveInputDeviceId,
                            System.StringComparison.Ordinal))
                    {
                        activeIndex = choices.Count - 1;
                    }
                }

                inputDeviceDropdown.choices = choices;
                inputDeviceDropdown.index = activeIndex;
                inputDeviceDropdown.SetValueWithoutNotify(choices[activeIndex]);
                inputDeviceDropdown.SetEnabled(true);
                refreshVoiceDevicesButton.SetEnabled(true);
                microphoneTestButton.SetEnabled(
                    voiceRuntime != null &&
                    voiceRuntime.IsReady &&
                    VivoxInputDeviceSettings.HasEffectiveInputDevice);
                inputDeviceStatusLabel.text =
                    VivoxInputDeviceSettings.HasEffectiveInputDevice
                        ? UiText.Format(
                            "Aktywne wejście: {0}",
                            choices[activeIndex])
                        : UiText.Get(
                            "Brak aktywnego mikrofonu. Wybierz dostępne wejście lub odśwież listę.");
            }

            refreshingDeviceDropdown = false;
        }

        public void RefreshDeviceLocalizedText()
        {
            if (inputDeviceCaptionLabel == null ||
                refreshVoiceDevicesButton == null)
            {
                return;
            }

            inputDeviceCaptionLabel.text = UiText.Get("Urządzenie wejściowe");
            refreshVoiceDevicesButton.text = UiText.Get("Odśwież");
            RefreshVoiceDevices();
        }

        private void RefreshParticipantRoster()
        {
            if (participantList == null)
                return;

            IReadOnlyList<VoiceRosterEntry> current =
                roundCoordinator?.VoiceRoster ?? System.Array.Empty<VoiceRosterEntry>();
            uint localNetId =
                NetworkClient.localPlayer != null ? NetworkClient.localPlayer.netId : 0u;
            IReadOnlyList<VoiceRosterChange> changes =
                VoiceRosterDiff.Calculate(rosterNames, current, localNetId);
            foreach (VoiceRosterChange change in changes)
            {
                switch (change.Kind)
                {
                    case VoiceRosterChangeKind.Add:
                        AddParticipant(change.NetworkIdentityNetId, change.DisplayName);
                        break;
                    case VoiceRosterChangeKind.Remove:
                        RemoveParticipant(change.NetworkIdentityNetId);
                        break;
                    case VoiceRosterChangeKind.Update:
                        rosterNames[change.NetworkIdentityNetId] = change.DisplayName;
                        if (participantNameLabels.TryGetValue(
                                change.NetworkIdentityNetId,
                                out Label name))
                        {
                            name.text = change.DisplayName;
                        }
                        break;
                }
            }

        }

        private void AddParticipant(uint netId, string displayName)
        {
            rosterNames[netId] = displayName;
            var row = new VisualElement();
            row.AddToClassList("voice-participant-row");
            var header = new VisualElement();
            header.AddToClassList("voice-participant-header");
            var name = new Label(displayName);
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
            UiControlStates.Normalize(row);
            participantList.Add(row);

            participantRows[netId] = row;
            participantNameLabels[netId] = name;
            participantStateLabels[netId] = state;
            participantMuteButtons[netId] = muteButton;
        }

        private void RemoveParticipant(uint netId)
        {
            if (participantRows.TryGetValue(netId, out VisualElement row))
                row.RemoveFromHierarchy();
            rosterNames.Remove(netId);
            participantRows.Remove(netId);
            participantNameLabels.Remove(netId);
            participantStateLabels.Remove(netId);
            participantMuteButtons.Remove(netId);
        }

        private void RefreshParticipantStates()
        {
            foreach (KeyValuePair<uint, Label> entry in participantStateLabels)
            {
                uint netId = entry.Key;
                bool locallyMuted =
                    voiceRuntime != null && voiceRuntime.IsParticipantLocallyMuted(netId);
                bool microphoneMuted =
                    voiceRuntime != null && voiceRuntime.IsNetworkPlayerMicrophoneMuted(netId);
                bool speaking =
                    voiceRuntime != null && voiceRuntime.IsNetworkPlayerSpeaking(netId);
                entry.Value.text = UiText.Get(
                    locallyMuted ? "WYCISZONY LOKALNIE" :
                    microphoneMuted ? "MIKROFON WYCISZONY" :
                    speaking ? "MÓWI" : "POŁĄCZONY");

                if (participantMuteButtons.TryGetValue(netId, out Button button))
                    button.text = UiText.Get(locallyMuted ? "Włącz dźwięk" : "Wycisz");
            }
        }
    }
}
