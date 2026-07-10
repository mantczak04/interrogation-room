using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using InterrogationRoom.Domain;
using InterrogationRoom.Networking;
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
        public bool ExecutionVisible { get; }
        public IReadOnlyList<ExecutionTargetView> ExecutionTargets { get; }
        public bool ResultVisible { get; }
        public string ResultText { get; }

        public RoundUiState(
            RoundPhase phase,
            string roleText,
            string crimeText,
            bool alibiVisible,
            string alibiText,
            bool endPreparationVisible,
            bool timerVisible,
            float remainingSeconds,
            bool executionVisible,
            IReadOnlyList<ExecutionTargetView> executionTargets,
            bool resultVisible,
            string resultText)
        {
            Phase = phase;
            RoleText = roleText;
            CrimeText = crimeText;
            AlibiVisible = alibiVisible;
            AlibiText = alibiText;
            EndPreparationVisible = endPreparationVisible;
            TimerVisible = timerVisible;
            RemainingSeconds = remainingSeconds;
            ExecutionVisible = executionVisible;
            ExecutionTargets = executionTargets;
            ResultVisible = resultVisible;
            ResultText = resultText;
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
        private float _deliveredRemainingSeconds;
        private float _receivedAt;
        private IReadOnlyList<ExecutionTargetView> _executionTargets = Array.Empty<ExecutionTargetView>();

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
        private Label _resultLabel;
        private Label _rejectionLabel;
        private Button _startButton;
        private DropdownField _caseSelection;
        private Button _endPreparationButton;
        private Button _executeButton;
        private DropdownField _executionTarget;

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
            _startButton.clicked += OnStartClicked;
            _caseSelection.RegisterValueChangedCallback(OnCaseSelectionChanged);
            _endPreparationButton.clicked += OnEndPreparationClicked;
            _executeButton.clicked += OnExecuteClicked;

            if (coordinator.CurrentView != null)
                OnViewReceived(coordinator.CurrentView, coordinator.CurrentRemainingSeconds);
            else
                RenderLobby();
        }

        private void OnDisable()
        {
            if (coordinator != null)
            {
                coordinator.ViewReceived -= OnViewReceived;
                coordinator.IntentRejected -= OnIntentRejected;
            }
            if (_startButton != null)
                _startButton.clicked -= OnStartClicked;
            if (_caseSelection != null)
                _caseSelection.UnregisterValueChangedCallback(OnCaseSelectionChanged);
            if (_endPreparationButton != null)
                _endPreparationButton.clicked -= OnEndPreparationClicked;
            if (_executeButton != null)
                _executeButton.clicked -= OnExecuteClicked;
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
                var remaining = Mathf.Max(0f, _deliveredRemainingSeconds - (Time.unscaledTime - _receivedAt));
                _timerLabel.text = FormatTimer(remaining);
            }
        }

        public static RoundUiState BuildState(PlayerRoundView view, float remainingSeconds, bool isHost)
        {
            if (view == null)
                throw new ArgumentNullException(nameof(view));

            var executionVisible = view.Phase == RoundPhase.Round && view.Role == RoundRole.Detective;
            var executionTargets = executionVisible
                ? view.Players.Where(player => player != view.Detective).Select(player => new ExecutionTargetView(player)).ToArray()
                : Array.Empty<ExecutionTargetView>();
            var alibiVisible = view.Phase == RoundPhase.Preparation && view.Alibi != null;

            return new RoundUiState(
                view.Phase,
                FormatRole(view.Role),
                view.CrimeDescription,
                alibiVisible,
                alibiVisible ? FormatAlibi(view.Alibi) : null,
                view.Phase == RoundPhase.Preparation && isHost,
                view.Phase == RoundPhase.Round,
                Mathf.Max(0f, remainingSeconds),
                executionVisible,
                executionTargets,
                view.Phase == RoundPhase.Finished,
                view.Phase == RoundPhase.Finished ? FormatResult(view.Result) : null);
        }

        private void OnViewReceived(PlayerRoundView view, float remainingSeconds)
        {
            _view = view;
            _deliveredRemainingSeconds = Mathf.Max(0f, remainingSeconds);
            _receivedAt = Time.unscaledTime;
            _rejectionLabel.text = string.Empty;
            SetVisible(_rejectionLabel, false);
            Render(BuildState(view, remainingSeconds, coordinator.IsLocalHost));
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
            _timerLabel.text = FormatTimer(state.RemainingSeconds);
            SetVisible(_timerLabel, state.TimerVisible);
            SetVisible(_executeButton, state.ExecutionVisible);
            SetVisible(_executionTarget, state.ExecutionVisible);
            _executionTargets = state.ExecutionTargets;
            _executionTarget.choices = _executionTargets.Select(target => target.Label).ToList();
            _executionTarget.index = _executionTargets.Count > 0 ? 0 : -1;
            _resultLabel.text = state.ResultText ?? string.Empty;
            SetCursorFor(state.Phase, state.ExecutionVisible);
        }

        private void RenderLobby()
        {
            SetVisible(_lobbyPanel, true);
            SetVisible(_preparationPanel, false);
            SetVisible(_hudPanel, false);
            SetVisible(_resultPanel, false);
            var canStart = coordinator.IsLocalHost
                && coordinator.ConnectedPlayerCount >= RoundEngine.MinPlayers
                && coordinator.ConnectedPlayerCount <= RoundEngine.MaxPlayers;
            SetVisible(_startButton, coordinator.IsLocalHost);
            SetVisible(_caseSelection, coordinator.IsLocalHost);
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
                : $"Start Rundy ({coordinator.ConnectedPlayerCount}/{RoundEngine.MinPlayers})";
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
            _resultLabel = Required<Label>(root, "result-label");
            _rejectionLabel = Required<Label>(root, "rejection-label");
            _startButton = Required<Button>(root, "start-button");
            _caseSelection = Required<DropdownField>(root, "case-selection");
            _endPreparationButton = Required<Button>(root, "end-preparation-button");
            _executeButton = Required<Button>(root, "execute-button");
            _executionTarget = Required<DropdownField>(root, "execution-target");
        }

        private void OnStartClicked() => coordinator.RequestStartRound();

        private void OnCaseSelectionChanged(ChangeEvent<string> changeEvent)
        {
            if (_caseSelection.index >= 0)
                coordinator.TrySelectCase(_caseSelection.index);
        }
        private void OnEndPreparationClicked() => coordinator.RequestEndPreparation();

        private void OnExecuteClicked()
        {
            if (_view == null || _view.Role != RoundRole.Detective || _view.Phase != RoundPhase.Round)
                return;
            if (_executionTarget.index < 0 || _executionTarget.index >= _executionTargets.Count)
                return;
            _executeButton.SetEnabled(false);
            coordinator.RequestExecution(_executionTargets[_executionTarget.index].PlayerId);
        }

        private void OnIntentRejected(string reason)
        {
            _rejectionLabel.text = string.IsNullOrWhiteSpace(reason) ? "Intencja została odrzucona." : reason;
            SetVisible(_rejectionLabel, true);
            if (_view != null && _view.Phase == RoundPhase.Round && _view.Role == RoundRole.Detective)
                _executeButton.SetEnabled(true);
        }

        private static string FormatAlibi(AlibiView alibi)
        {
            var builder = new StringBuilder();
            foreach (var entry in alibi.Entries)
                builder.AppendLine(entry.IsHidden ? "• [UKRYTY FAKT]" : $"• {entry.Text}");
            return builder.ToString().TrimEnd();
        }

        private static string FormatResult(PlayerResultView result)
        {
            if (result == null)
                return "Brak wyniku Rundy.";
            var outcome = result.Won ? "Wygrana" : "Przegrana";
            var survival = result.Survived ? "Przetrwanie" : "Wykonano Egzekucję na Tobie";
            var cause = result.EndCause == RoundEndCause.Execution ? "Egzekucja" : "Upłynął Limit Rundy";
            var executed = result.ExecutedPlayer.HasValue ? $"Gracz {result.ExecutedPlayer.Value.Value}" : "nikt";
            return $"{outcome}\n{survival}\nPrzyczyna: {cause}\nWykonany gracz: {executed}";
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

        private static void SetCursorFor(RoundPhase phase, bool requiresPointer)
        {
            var lockForGameplay = phase == RoundPhase.Round && !requiresPointer;
            UnityEngine.Cursor.visible = !lockForGameplay;
            UnityEngine.Cursor.lockState = lockForGameplay ? CursorLockMode.Locked : CursorLockMode.None;
        }
    }
}
