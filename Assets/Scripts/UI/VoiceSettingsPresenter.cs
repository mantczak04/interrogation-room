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

        private Slider microphoneLevelSlider;
        private Label microphoneLevelValueLabel;
        private Label microphoneStateLabel;
        private Label microphoneTestStatusLabel;
        private Label participantListEmptyLabel;
        private ScrollView participantList;
        private Button microphoneMuteButton;
        private Button microphoneTestButton;
        private MicrophoneTestPlayback microphoneTest;
        private VivoxVoiceRuntime voiceRuntime;
        private NetworkRoundCoordinator roundCoordinator;
        private float nextRefresh;
        private float nextCoordinatorDiscovery;
        private int lastMicrophoneLevelPercent = int.MinValue;
        private bool isOpen;

        public void Configure(VisualElement root)
        {
            microphoneLevelSlider = root.Q<Slider>("microphone-level-slider");
            microphoneLevelValueLabel = root.Q<Label>("microphone-level-value");
            microphoneStateLabel = root.Q<Label>("microphone-state");
            microphoneTestStatusLabel = root.Q<Label>("microphone-test-status");
            participantListEmptyLabel = root.Q<Label>("voice-participant-list-empty");
            participantList = root.Q<ScrollView>("voice-participant-list");
            microphoneMuteButton = root.Q<Button>("microphone-mute-button");
            microphoneTestButton = root.Q<Button>("microphone-test-button");

            microphoneLevelSlider.lowValue = GameSettings.MinVoicePercent;
            microphoneLevelSlider.highValue = GameSettings.MaxVoicePercent;
            microphoneLevelSlider.RegisterValueChangedCallback(
                evt => GameSettingsService.Current.SetMicrophoneLevelPercent(evt.newValue));
            microphoneMuteButton.clicked += OnMicrophoneMuteClicked;
            microphoneTestButton.clicked += OnMicrophoneTestClicked;

            microphoneTest = GetComponent<MicrophoneTestPlayback>() ??
                             gameObject.AddComponent<MicrophoneTestPlayback>();
            microphoneTest.SetLevelPercent(GameSettingsService.Current.MicrophoneLevelPercent);
            microphoneTest.StateChanged += OnMicrophoneTestStateChanged;
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
            voiceRuntime?.SetMicrophoneTestActive(false);
        }

        public void RefreshLocalizedText()
        {
            RefreshMicrophoneTestText();
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

            if (voiceRuntime == null)
                voiceRuntime = VivoxVoiceRuntime.Instance;
            if (roundCoordinator == null &&
                (forceRoster || Time.unscaledTime >= nextCoordinatorDiscovery))
            {
                roundCoordinator = FindFirstObjectByType<NetworkRoundCoordinator>();
                nextCoordinatorDiscovery = Time.unscaledTime + 1f;
            }

            SyncMicrophoneTestMute();
            bool muted = settings.MicrophoneMuted;
            microphoneStateLabel.text =
                UiText.Get(muted ? "MIKROFON WYCISZONY" : "MIKROFON WŁĄCZONY");
            microphoneMuteButton.text =
                UiText.Get(muted ? "Włącz mikrofon" : "Wycisz mikrofon");
            RefreshMicrophoneTestText();
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
            voiceRuntime?.SetMicrophoneTestActive(false);
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
            Refresh();
        }

        private void SyncMicrophoneTestMute()
        {
            if (voiceRuntime == null || microphoneTest == null)
                return;

            bool active = microphoneTest.State == MicrophoneTestState.Starting ||
                          microphoneTest.State == MicrophoneTestState.Monitoring;
            voiceRuntime.SetMicrophoneTestActive(active);
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
                    microphoneTestStatusLabel.text = UiText.Get(
                        "Usłyszysz siebie od razu. Dźwięk pozostaje lokalny i nie jest wysyłany innym.");
                    break;
            }
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

            SetVisible(participantListEmptyLabel, rosterNames.Count == 0);
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

        private static void SetVisible(VisualElement element, bool visible)
        {
            if (element != null)
                element.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
        }
    }
}
