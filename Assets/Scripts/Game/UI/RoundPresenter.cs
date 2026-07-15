using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using InterrogationRoom.Domain;
using InterrogationRoom.Networking;
using Mirror;
using UnityEngine;
using UnityEngine.UIElements;

namespace InterrogationRoom.UI
{
    public readonly struct ExecutionTargetView
    {
        public PlayerId PlayerId { get; }
        public string Label { get; }

        public ExecutionTargetView(PlayerId playerId)
        {
            PlayerId = playerId;
            Label = $"Gracz {playerId.Value}";
        }
    }

    /// <summary>Presentation-only projection of one local PlayerRoundView.</summary>
    public sealed class RoundUiState
    {
        public RoundPhase Phase { get; }
        public string RoleText { get; }
        public string CrimeText { get; }
        public bool AlibiVisible { get; }
        public string AlibiText { get; }
        public bool EndPreparationVisible { get; }
        public bool TimerVisible { get; }
        public float RemainingSeconds { get; }
        public bool UnlimitedTime { get; }
        public bool ExecutionVisible { get; }
        public IReadOnlyList<ExecutionTargetView> ExecutionTargets { get; }
        public bool PrivatePanelVisible { get; }
        public string PrivateTitle { get; }
        public string PrivateText { get; }
        public bool ResultVisible { get; }
        public string ResultText { get; }
        public bool ReturnToLobbyVisible { get; }

        public RoundUiState(
            RoundPhase phase,
            string roleText,
            string crimeText,
            bool alibiVisible,
            string alibiText,
            bool endPreparationVisible,
            bool timerVisible,
            float remainingSeconds,
            bool unlimitedTime,
            bool executionVisible,
            IReadOnlyList<ExecutionTargetView> executionTargets,
            bool privatePanelVisible,
            string privateTitle,
            string privateText,
            bool resultVisible,
            string resultText,
            bool returnToLobbyVisible)
        {
            Phase = phase;
            RoleText = roleText;
            CrimeText = crimeText;
            AlibiVisible = alibiVisible;
            AlibiText = alibiText;
            EndPreparationVisible = endPreparationVisible;
            TimerVisible = timerVisible;
            RemainingSeconds = remainingSeconds;
            UnlimitedTime = unlimitedTime;
            ExecutionVisible = executionVisible;
            ExecutionTargets = executionTargets;
            PrivatePanelVisible = privatePanelVisible;
            PrivateTitle = privateTitle;
            PrivateText = privateText;
            ResultVisible = resultVisible;
            ResultText = resultText;
            ReturnToLobbyVisible = returnToLobbyVisible;
        }
    }

    /// <summary>
    /// Thin local UI adapter. It renders only the targeted PlayerRoundView and
    /// forwards button intentions to NetworkRoundCoordinator; it owns no Runda rules.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(UIDocument))]
    public sealed class RoundPresenter : MonoBehaviour
    {
        [SerializeField] private NetworkRoundCoordinator coordinator;
        [SerializeField] private UIDocument uiDocument;

        private PlayerRoundView _view;
        private double _roundEndsAtNetworkTime;
        private VisualElement _lobbyPanel;
        private VisualElement _preparationPanel;
        private VisualElement _hudPanel;
        private VisualElement _resultPanel;
        private Label _roleLabel;
        private Label _crimeLabel;
        private Label _hudRoleLabel;
        private Label _hudCrimeLabel;
        private VisualElement _alibiSection;
        private Label _alibiLabel;
        private Label _timerLabel;
        private VisualElement _privatePanel;
        private Label _privateTitleLabel;
        private Label _privateLabel;
        private Label _resultLabel;
        private Label _rejectionLabel;
        private Button _startButton;
        private DropdownField _caseSelection;
        private Label _playerCountLabel;
        private Toggle _secretObjectiveToggle;
        private Label _secretObjectiveSummary;
        private Button _endPreparationButton;
        private Button _returnToLobbyButton;
        private bool _lobbyMenuVisible;
        private bool _developerMenuOpen;
        private bool _unlimitedRound;

        private void Reset()
        {
            uiDocument = GetComponent<UIDocument>();
        }

        private void OnValidate()
        {
            if (uiDocument == null)
                uiDocument = GetComponent<UIDocument>();
            if (coordinator == null)
                Debug.LogError("[RoundPresenter] NetworkRoundCoordinator reference is required.", this);
            if (uiDocument == null)
                Debug.LogError("[RoundPresenter] UIDocument reference is required.", this);
        }

        private void OnEnable()
        {
            if (uiDocument == null)
                uiDocument = GetComponent<UIDocument>();
            if (coordinator == null || uiDocument == null)
            {
                Debug.LogError("[RoundPresenter] NetworkRoundCoordinator and UIDocument references are required.", this);
                enabled = false;
                return;
            }

            BindVisualTree();
            _rejectionLabel.text = string.Empty;
            SetVisible(_rejectionLabel, false);
            coordinator.ViewReceived += OnViewReceived;
            coordinator.IntentRejected += OnIntentRejected;
            coordinator.LobbyResetReceived += OnLobbyResetReceived;
            _startButton.clicked += OnStartClicked;
            _caseSelection.RegisterValueChangedCallback(OnCaseSelectionChanged);
            _secretObjectiveToggle.RegisterValueChangedCallback(OnSecretObjectiveChanged);
            _endPreparationButton.clicked += OnEndPreparationClicked;
            _returnToLobbyButton.clicked += OnReturnToLobbyClicked;

            if (coordinator.CurrentView != null)
                OnViewReceived(coordinator.CurrentView, coordinator.CurrentRoundEndsAtNetworkTime);
            else
                RenderLobby();
        }

        private void OnDisable()
        {
            if (coordinator != null)
            {
                coordinator.ViewReceived -= OnViewReceived;
                coordinator.IntentRejected -= OnIntentRejected;
                coordinator.LobbyResetReceived -= OnLobbyResetReceived;
            }
            if (_startButton != null)
                _startButton.clicked -= OnStartClicked;
            if (_caseSelection != null)
                _caseSelection.UnregisterValueChangedCallback(OnCaseSelectionChanged);
            if (_secretObjectiveToggle != null)
                _secretObjectiveToggle.UnregisterValueChangedCallback(OnSecretObjectiveChanged);
            if (_endPreparationButton != null)
                _endPreparationButton.clicked -= OnEndPreparationClicked;
            if (_returnToLobbyButton != null)
                _returnToLobbyButton.clicked -= OnReturnToLobbyClicked;
        }

        private void Update()
        {
            if (_view == null)
            {
                RenderLobby();
                return;
            }

            if (_view.Phase == RoundPhase.Round)
            {
                if (_unlimitedRound)
                {
                    _timerLabel.text = "∞";
                    return;
                }

                var remaining = CalculateRemainingSeconds(
                    _roundEndsAtNetworkTime,
                    NetworkTime.time,
                    _view.Phase);
                _timerLabel.text = FormatTimer(remaining);
            }
        }

        public static float CalculateRemainingSeconds(
            double roundEndsAtNetworkTime,
            double currentNetworkTime,
            RoundPhase phase)
        {
            if (phase != RoundPhase.Round)
                return 0f;

            return (float)Math.Max(0d, roundEndsAtNetworkTime - currentNetworkTime);
        }

        public static bool ShouldReleaseCursor(
            RoundPhase phase,
            bool developerMenuOpen,
            bool requiresPointer) =>
            developerMenuOpen || phase != RoundPhase.Round || requiresPointer;

        public static bool IsUnlimitedRound(RoundPhase phase, double roundEndsAtNetworkTime) =>
            phase == RoundPhase.Round && roundEndsAtNetworkTime <= 0d;

        public void SetLobbyMenuVisible(bool visible)
        {
            _lobbyMenuVisible = visible;
            if (_view == null || _view.Phase == RoundPhase.Lobby)
                SetVisible(_lobbyPanel, visible);
        }

        public void SetDeveloperMenuOpen(bool open)
        {
            _developerMenuOpen = open;
            if (_view != null)
                SetCursorFor(_view.Phase, requiresPointer: false);
        }

        public static RoundUiState BuildState(
            PlayerRoundView view,
            float remainingSeconds,
            bool isHost,
            bool unlimitedTime = false)
        {
            if (view == null)
                throw new ArgumentNullException(nameof(view));

            var alibiVisible = view.Phase == RoundPhase.Preparation && view.Alibi != null;
            BuildPrivatePanel(view, out string privateTitle, out string privateText);

            return new RoundUiState(
                view.Phase,
                FormatRole(view.Role),
                view.CrimeDescription,
                alibiVisible,
                alibiVisible ? FormatAlibi(view.Alibi) : null,
                view.Phase == RoundPhase.Preparation && isHost,
                view.Phase == RoundPhase.Round,
                Mathf.Max(0f, remainingSeconds),
                unlimitedTime,
                executionVisible: false,
                Array.Empty<ExecutionTargetView>(),
                view.Phase == RoundPhase.Round,
                privateTitle,
                privateText,
                view.Phase == RoundPhase.Finished,
                view.Phase == RoundPhase.Finished ? FormatResult(view.Result, view.RoundReveal) : null,
                view.Phase == RoundPhase.Finished && isHost);
        }

        private void OnViewReceived(PlayerRoundView view, double roundEndsAtNetworkTime)
        {
            _view = view;
            _roundEndsAtNetworkTime = Math.Max(0d, roundEndsAtNetworkTime);
            _unlimitedRound = IsUnlimitedRound(view.Phase, _roundEndsAtNetworkTime);
            _rejectionLabel.text = string.Empty;
            SetVisible(_rejectionLabel, false);
            Render(BuildState(
                view,
                CalculateRemainingSeconds(_roundEndsAtNetworkTime, NetworkTime.time, view.Phase),
                coordinator.IsLocalHost,
                _unlimitedRound));
        }

        private void Render(RoundUiState state)
        {
            SetVisible(_lobbyPanel, false);
            SetVisible(_preparationPanel, state.Phase == RoundPhase.Preparation);
            SetVisible(_hudPanel, state.Phase == RoundPhase.Round);
            SetVisible(_resultPanel, state.ResultVisible);
            _roleLabel.text = state.RoleText;
            _crimeLabel.text = state.CrimeText;
            _hudRoleLabel.text = $"Rola: {state.RoleText}";
            _hudCrimeLabel.text = $"Przestępstwo: {state.CrimeText}";
            _alibiLabel.text = state.AlibiText ?? string.Empty;
            SetVisible(_alibiSection, state.AlibiVisible);
            SetVisible(_endPreparationButton, state.EndPreparationVisible);
            _timerLabel.text = state.UnlimitedTime ? "∞" : FormatTimer(state.RemainingSeconds);
            SetVisible(_timerLabel, state.TimerVisible);
            SetVisible(_privatePanel, state.PrivatePanelVisible);
            _privateTitleLabel.text = state.PrivateTitle ?? string.Empty;
            _privateLabel.text = state.PrivateText ?? string.Empty;
            _resultLabel.text = state.ResultText ?? string.Empty;
            SetVisible(_returnToLobbyButton, state.ReturnToLobbyVisible);
            SetCursorFor(state.Phase, requiresPointer: false);
        }

        private void RenderLobby()
        {
            bool connected = NetworkClient.isConnected || NetworkServer.active;
            int playerCount = coordinator.PublicLobbyPlayerCount;
            SetVisible(_lobbyPanel, _lobbyMenuVisible || connected);
            SetVisible(_preparationPanel, false);
            SetVisible(_hudPanel, false);
            SetVisible(_resultPanel, false);
            SetVisible(_privatePanel, false);
            var canStart = coordinator.IsLocalHost
                && playerCount >= RoundEngine.MinPlayers
                && playerCount <= RoundEngine.MaxPlayers;
            SetVisible(_startButton, coordinator.IsLocalHost);
            SetVisible(_caseSelection, coordinator.IsLocalHost);
            SetVisible(_secretObjectiveToggle, coordinator.IsLocalHost);
            SetVisible(_secretObjectiveSummary, coordinator.IsLocalHost);
            var caseTitles = coordinator.AvailableCaseTitles.ToList();
            _caseSelection.choices = caseTitles;
            _caseSelection.SetEnabled(caseTitles.Count > 1);
            if (caseTitles.Count == 0)
                _caseSelection.SetValueWithoutNotify(string.Empty);
            else
                _caseSelection.SetValueWithoutNotify(caseTitles[Mathf.Clamp(coordinator.SelectedCaseIndex, 0, caseTitles.Count - 1)]);
            _startButton.SetEnabled(canStart);
            _startButton.text = canStart
                ? "Start Rundy"
                : $"Start Rundy ({playerCount}/{RoundEngine.MinPlayers})";
            _playerCountLabel.text = $"Gracze w lobby: {playerCount}/{RoundEngine.MaxPlayers}";
            _secretObjectiveToggle.SetValueWithoutNotify(coordinator.HostAllowsSecretObjective);
            _secretObjectiveSummary.text =
                $"Efektywna liczba Sekretnych Celów: {coordinator.EffectiveSecretObjectiveCount}";

            if (_lobbyMenuVisible || connected)
                SetCursorFor(RoundPhase.Lobby, false);
        }

        private void BindVisualTree()
        {
            var root = uiDocument.rootVisualElement;
            _lobbyPanel = Required<VisualElement>(root, "lobby-panel");
            _preparationPanel = Required<VisualElement>(root, "preparation-panel");
            _hudPanel = Required<VisualElement>(root, "round-hud");
            _resultPanel = Required<VisualElement>(root, "result-panel");
            _roleLabel = Required<Label>(root, "role-label");
            _crimeLabel = Required<Label>(root, "crime-label");
            _hudRoleLabel = Required<Label>(root, "hud-role-label");
            _hudCrimeLabel = Required<Label>(root, "hud-crime-label");
            _alibiSection = Required<VisualElement>(root, "alibi-section");
            _alibiLabel = Required<Label>(root, "alibi-label");
            _timerLabel = Required<Label>(root, "timer-label");
            _privatePanel = Required<VisualElement>(root, "private-panel");
            _privateTitleLabel = Required<Label>(root, "private-title-label");
            _privateLabel = Required<Label>(root, "private-label");
            _resultLabel = Required<Label>(root, "result-label");
            _rejectionLabel = Required<Label>(root, "rejection-label");
            _startButton = Required<Button>(root, "start-button");
            _caseSelection = Required<DropdownField>(root, "case-selection");
            _playerCountLabel = Required<Label>(root, "player-count-label");
            _secretObjectiveToggle = Required<Toggle>(root, "secret-objective-toggle");
            _secretObjectiveSummary = Required<Label>(root, "secret-objective-summary");
            _endPreparationButton = Required<Button>(root, "end-preparation-button");
            _returnToLobbyButton = Required<Button>(root, "return-to-lobby-button");
        }

        private void OnStartClicked() => coordinator.RequestStartRound();

        private void OnCaseSelectionChanged(ChangeEvent<string> changeEvent)
        {
            if (_caseSelection.index >= 0)
                coordinator.TrySelectCase(_caseSelection.index);
        }

        private void OnSecretObjectiveChanged(ChangeEvent<bool> changeEvent)
        {
            if (!coordinator.IsLocalHost ||
                !coordinator.TrySetSecretObjectiveEnabled(changeEvent.newValue))
            {
                _secretObjectiveToggle.SetValueWithoutNotify(coordinator.HostAllowsSecretObjective);
                return;
            }

            RenderLobby();
        }

        private void OnEndPreparationClicked() => coordinator.RequestEndPreparation();
        private void OnReturnToLobbyClicked() => coordinator.RequestReturnToLobby();

        private void OnLobbyResetReceived()
        {
            _view = null;
            _roundEndsAtNetworkTime = 0d;
            _unlimitedRound = false;
            _rejectionLabel.text = string.Empty;
            SetVisible(_rejectionLabel, false);
            RenderLobby();
        }

        private void OnIntentRejected(string reason)
        {
            _rejectionLabel.text = string.IsNullOrWhiteSpace(reason) ? "Intencja została odrzucona." : reason;
            SetVisible(_rejectionLabel, true);
        }

        private static string FormatAlibi(AlibiView alibi)
        {
            var builder = new StringBuilder();
            foreach (var entry in alibi.Entries)
                builder.AppendLine(entry.IsHidden ? "• [UKRYTY FAKT]" : $"• {entry.Text}");
            return builder.ToString().TrimEnd();
        }

        private static void BuildPrivatePanel(
            PlayerRoundView view,
            out string title,
            out string text)
        {
            var builder = new StringBuilder();
            switch (view.Role)
            {
                case RoundRole.Detective:
                    title = "Rejestr Incydentów";
                    if (view.IncidentRegistry == null || view.IncidentRegistry.Count == 0)
                    {
                        text = "Brak zgłoszonych lub odkrytych Incydentów.";
                        return;
                    }

                    foreach (IncidentRegistryEntryView incident in view.IncidentRegistry)
                    {
                        builder.AppendLine(
                            $"• {FormatTimestamp(incident.ReportedAt)} — {incident.Effect.Value} / " +
                            $"{incident.Location.Value} ({FormatIncidentKind(incident.Kind)})");
                    }
                    break;

                case RoundRole.Guilty:
                    title = "Tropy do Alibi i Plan Ucieczki";
                    builder.AppendLine("Tropy:");
                    if (view.AcquiredAlibiClues == null || view.AcquiredAlibiClues.Count == 0)
                        builder.AppendLine("• brak");
                    else
                    {
                        foreach (AlibiClueView clue in view.AcquiredAlibiClues)
                            builder.AppendLine($"• {clue.Content}");
                    }

                    if (view.EscapePlan != null)
                    {
                        builder.AppendLine($"Plan: {view.EscapePlan.CompletedCommonStepCount}/" +
                                           $"{view.EscapePlan.TotalCommonStepCount} kroków wspólnych");
                        builder.AppendLine(view.EscapePlan.CurrentStep.HasValue
                            ? $"Aktualny krok: {view.EscapePlan.CurrentStep.Value.Value}"
                            : "Kroki wspólne ukończone.");
                        foreach (EscapeExitOptionView option in view.EscapePlan.ExitOptions)
                        {
                            builder.AppendLine(
                                $"• {option.Location.Value}: " +
                                (option.IsPrepared ? "przygotowane" : "nieprzygotowane"));
                        }
                        if (view.EscapePlan.ActiveExit.HasValue)
                            builder.AppendLine($"Aktywna Ucieczka: {view.EscapePlan.ActiveExit.Value.Value}");
                    }
                    break;

                default:
                    title = "Prywatny Cel";
                    if (view.PrivateObjective == null)
                    {
                        text = "Brak przypisanego Celu.";
                        return;
                    }

                    PrivateObjectiveView objective = view.PrivateObjective;
                    builder.AppendLine(
                        $"Postęp: {objective.CompletedStepCount}/{objective.TotalStepCount}");
                    builder.AppendLine(objective.CurrentStep.HasValue
                        ? $"Aktualny krok: {objective.CurrentStep.Value.Value}"
                        : "Cel ukończony.");
                    if (objective.Target.HasValue)
                        builder.AppendLine($"Cel Wrobienia: Gracz {objective.Target.Value.Value}");
                    break;
            }

            text = builder.ToString().TrimEnd();
        }

        private static string FormatResult(PlayerResultView result, RoundRevealView reveal)
        {
            if (result == null)
                return "Brak wyniku Rundy.";
            var outcome = result.Won ? "Wygrana" : "Przegrana";
            var survival = result.Survived ? "Przetrwanie" : "Wykonano Egzekucję na Tobie";
            var cause = FormatEndCause(result.EndCause);
            var executed = result.ExecutedPlayer.HasValue ? $"Gracz {result.ExecutedPlayer.Value.Value}" : "nikt";
            var builder = new StringBuilder();
            builder.AppendLine(outcome);
            builder.AppendLine(survival);
            builder.AppendLine($"Przyczyna: {cause}");
            builder.AppendLine($"Wykonany gracz: {executed}");
            builder.AppendLine($"Prywatny Cel: {(result.PrivateObjectiveCompleted ? "ukończony" : "nieukończony")}");
            if (result.Escaped)
                builder.AppendLine("Ucieczka: ukończona");

            if (reveal != null)
                AppendRoundReveal(builder, reveal);

            return builder.ToString().TrimEnd();
        }

        private static void AppendRoundReveal(StringBuilder builder, RoundRevealView reveal)
        {
            builder.AppendLine();
            builder.AppendLine("UJAWNIENIE RUNDY");
            builder.AppendLine("Role, Cele i indywidualne wyniki:");
            foreach (PlayerEndRevealView player in reveal.Players)
            {
                builder.Append($"• Gracz {player.Player.Value}: {FormatRole(player.Role)}");
                if (player.PrivateObjective != null)
                {
                    builder.Append(
                        $"; Cel {player.PrivateObjective.Id.Value} " +
                        $"{player.PrivateObjective.CompletedStepCount}/{player.PrivateObjective.TotalStepCount}");
                    if (player.PrivateObjective.Target.HasValue)
                        builder.Append($"; Cel Wrobienia: Gracz {player.PrivateObjective.Target.Value.Value}");
                }
                builder.AppendLine(
                    $"; {(player.Result.Won ? "wygrana" : "przegrana")}; " +
                    $"{(player.Result.Survived ? "przeżył" : "wyeliminowany")}");
            }

            builder.AppendLine("Tropy do Alibi:");
            if (reveal.AcquiredAlibiClues.Count == 0)
                builder.AppendLine("• brak");
            foreach (AlibiClueRevealView clue in reveal.AcquiredAlibiClues)
                builder.AppendLine($"• {clue.Id.Value} → {clue.LinkedFactId}: {clue.Content}");

            builder.AppendLine("Plan Ucieczki:");
            if (reveal.EscapePlan.Actions.Count == 0)
                builder.AppendLine("• brak działań");
            foreach (EscapeActionRevealView action in reveal.EscapePlan.Actions)
            {
                string detail = action.StepId.HasValue
                    ? action.StepId.Value.Value
                    : action.ExitId.HasValue ? action.ExitId.Value.Value : string.Empty;
                builder.AppendLine($"• {action.Kind}: {detail}".TrimEnd());
            }
            builder.AppendLine(reveal.EscapePlan.SuccessfulExit.HasValue
                ? $"Udane wyjście: {reveal.EscapePlan.SuccessfulExit.Value.Value}"
                : "Udane wyjście: brak");

            builder.AppendLine("Incydenty i autorzy:");
            if (reveal.Incidents.Count == 0)
                builder.AppendLine("• brak");
            foreach (IncidentRevealView incident in reveal.Incidents)
            {
                builder.AppendLine(
                    $"• {incident.Id.Value}: {incident.Effect.Value} / {incident.Location.Value}; " +
                    $"autor Gracz {incident.Author.Value} ({FormatIncidentKind(incident.Kind)})");
            }
        }

        private static string FormatTimestamp(IncidentTimestamp timestamp)
        {
            long totalSeconds = timestamp.MillisecondsSinceRoundStart / 1000;
            return $"{totalSeconds / 60:00}:{totalSeconds % 60:00}";
        }

        private static string FormatIncidentKind(IncidentKind kind) =>
            kind == IncidentKind.Loud ? "głośny" : "cichy";

        private static string FormatEndCause(RoundEndCause cause)
        {
            switch (cause)
            {
                case RoundEndCause.Execution: return "Egzekucja";
                case RoundEndCause.Escape: return "Ucieczka Winnego";
                default: return "Upłynął Limit Rundy";
            }
        }

        private static string FormatRole(RoundRole role)
        {
            switch (role)
            {
                case RoundRole.Detective: return "Detektyw";
                case RoundRole.Guilty: return "Winny";
                default: return "Niewinny";
            }
        }

        private static string FormatTimer(float seconds)
        {
            var wholeSeconds = Mathf.CeilToInt(Mathf.Max(0f, seconds));
            return $"{wholeSeconds / 60:00}:{wholeSeconds % 60:00}";
        }

        private static T Required<T>(VisualElement root, string name) where T : VisualElement
        {
            var element = root.Q<T>(name);
            if (element == null)
                throw new InvalidOperationException($"Round UI is missing required element '{name}' ({typeof(T).Name}).");
            return element;
        }

        private static void SetVisible(VisualElement element, bool visible)
        {
            if (element != null)
                element.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private void SetCursorFor(RoundPhase phase, bool requiresPointer)
        {
            PlayerInputGate.SetUiInputBlocked(
                ShouldReleaseCursor(phase, _developerMenuOpen, requiresPointer));
        }
    }
}
