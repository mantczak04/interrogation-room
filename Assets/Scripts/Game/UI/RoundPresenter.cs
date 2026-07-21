using System;
using System.Collections.Generic;
using System.Text;
using InterrogationRoom.Domain;
using InterrogationRoom.Networking;
using InterrogationRoom.Settings;
using Mirror;
using UnityEngine;
using UnityEngine.UIElements;

namespace InterrogationRoom.UI
{
    public readonly struct ExecutionTargetView
    {
        public PlayerId PlayerId { get; }
        public string Label { get; }

        public ExecutionTargetView(PlayerId playerId, UiLanguage language = UiLanguage.Polish)
        {
            PlayerId = playerId;
            Label = UiText.Format("Gracz {0}", language, playerId.Value);
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
        public string PreparationInstructionText { get; }
        public bool ReadyButtonVisible { get; }
        public bool ReadyButtonEnabled { get; }
        public string ReadyCountText { get; }
        public bool PreparationTimerVisible { get; }
        public float PreparationRemainingSeconds { get; }
        public bool TimerVisible { get; }
        public float RemainingSeconds { get; }
        public bool UnlimitedTime { get; }
        public bool ExecutionVisible { get; }
        public IReadOnlyList<ExecutionTargetView> ExecutionTargets { get; }
        public bool PrivatePanelVisible { get; }
        public string PrivateTitle { get; }
        public string PrivateMotive { get; }
        public string PrivateStep { get; }
        public string PrivateLocation { get; }
        public string PrivateProgress { get; }
        public string PrivateText { get; }
        public bool ResultVisible { get; }
        public string ResultVerdictText { get; }
        public string ResultReasonText { get; }
        public bool ResultIsLoss { get; }
        public string ResultText { get; }
        public bool ReturnToLobbyVisible { get; }

        public RoundUiState(
            RoundPhase phase,
            string roleText,
            string crimeText,
            bool alibiVisible,
            string alibiText,
            string preparationInstructionText,
            bool readyButtonVisible,
            bool readyButtonEnabled,
            string readyCountText,
            bool preparationTimerVisible,
            float preparationRemainingSeconds,
            bool timerVisible,
            float remainingSeconds,
            bool unlimitedTime,
            bool executionVisible,
            IReadOnlyList<ExecutionTargetView> executionTargets,
            bool privatePanelVisible,
            string privateTitle,
            string privateMotive,
            string privateStep,
            string privateLocation,
            string privateProgress,
            string privateText,
            bool resultVisible,
            string resultVerdictText,
            string resultReasonText,
            bool resultIsLoss,
            string resultText,
            bool returnToLobbyVisible)
        {
            Phase = phase;
            RoleText = roleText;
            CrimeText = crimeText;
            AlibiVisible = alibiVisible;
            AlibiText = alibiText;
            PreparationInstructionText = preparationInstructionText;
            ReadyButtonVisible = readyButtonVisible;
            ReadyButtonEnabled = readyButtonEnabled;
            ReadyCountText = readyCountText;
            PreparationTimerVisible = preparationTimerVisible;
            PreparationRemainingSeconds = preparationRemainingSeconds;
            TimerVisible = timerVisible;
            RemainingSeconds = remainingSeconds;
            UnlimitedTime = unlimitedTime;
            ExecutionVisible = executionVisible;
            ExecutionTargets = executionTargets;
            PrivatePanelVisible = privatePanelVisible;
            PrivateTitle = privateTitle;
            PrivateMotive = privateMotive;
            PrivateStep = privateStep;
            PrivateLocation = privateLocation;
            PrivateProgress = privateProgress;
            PrivateText = privateText;
            ResultVisible = resultVisible;
            ResultVerdictText = resultVerdictText;
            ResultReasonText = resultReasonText;
            ResultIsLoss = resultIsLoss;
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
        private double _preparationEndsAtNetworkTime;
        private VisualElement _lobbyPanel;
        private VisualElement _root;
        private VisualElement _preparationPanel;
        private VisualElement _hudPanel;
        private VisualElement _resultPanel;
        private Label _roleLabel;
        private Label _crimeLabel;
        private Label _hudRoleLabel;
        private Label _hudCrimeLabel;
        private VisualElement _alibiSection;
        private Label _alibiLabel;
        private Label _preparationInstructionLabel;
        private Label _preparationTimerLabel;
        private Label _readyCountLabel;
        private Button _readyButton;
        private Label _timerLabel;
        private VisualElement _privatePanel;
        private VisualElement _privateContent;
        private Button _privateToggleButton;
        private Label _privateTitleLabel;
        private Label _privateMotiveLabel;
        private Label _privateStepLabel;
        private Label _privateLocationLabel;
        private Label _privateProgressLabel;
        private Label _privateLabel;
        private Label _resultLabel;
        private Label _resultVerdictLabel;
        private Label _resultReasonLabel;
        private Label _rejectionLabel;
        private Button _startButton;
        private VisualElement _startButtonHoverArea;
        private Label _startButtonHoverInfo;
        private Button _lobbyReadyButton;
        private Label _playerCountLabel;
        private Button _roundLimit5Button;
        private Button _roundLimit10Button;
        private Button _roundLimit15Button;
        private Button _roundLimit20Button;
        private Toggle _secretObjectiveToggle;
        private Label _secretObjectiveSummary;
        private Button _returnToLobbyButton;
        private Button _lobbyBackButton;
        private Action _lobbyExitHandler;
        private bool _lobbyMenuVisible;
        private bool _developerMenuOpen;
        private bool _unlimitedRound;
        private bool _privatePanelExpanded = true;
        private bool _startButtonHovered;
        private bool _startButtonCanStart;
        private string _startButtonBlockedMessage;
        private RoundPhase? _lastRenderedPhase;
        private readonly Dictionary<TextElement, string> _staticPolishText = new Dictionary<TextElement, string>();

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

            ConfigurePanelScaling(uiDocument.panelSettings);
            BindVisualTree();
            CaptureStaticLocalizedText();
            GameSettingsService.Current.Changed += OnLanguageChanged;
            _rejectionLabel.text = string.Empty;
            SetVisible(_rejectionLabel, false);
            coordinator.ViewReceived += OnViewReceived;
            coordinator.IntentRejected += OnIntentRejected;
            coordinator.LobbyResetReceived += OnLobbyResetReceived;
            _startButton.clicked += OnStartClicked;
            _startButtonHoverArea.RegisterCallback<PointerEnterEvent>(OnStartButtonPointerEnter);
            _startButtonHoverArea.RegisterCallback<PointerLeaveEvent>(OnStartButtonPointerLeave);
            _lobbyReadyButton.clicked += OnLobbyReadyClicked;
            _roundLimit5Button.clicked += OnRoundLimit5Clicked;
            _roundLimit10Button.clicked += OnRoundLimit10Clicked;
            _roundLimit15Button.clicked += OnRoundLimit15Clicked;
            _roundLimit20Button.clicked += OnRoundLimit20Clicked;
            _secretObjectiveToggle.RegisterValueChangedCallback(OnSecretObjectiveChanged);
            _readyButton.clicked += OnReadyClicked;
            _privateToggleButton.clicked += TogglePrivatePanel;
            _root.RegisterCallback<KeyDownEvent>(OnRootKeyDown);
            _returnToLobbyButton.clicked += OnReturnToLobbyClicked;
            _lobbyBackButton.clicked += OnLobbyBackClicked;

            if (coordinator.CurrentView != null)
                OnViewReceived(coordinator.CurrentView, coordinator.CurrentRoundEndsAtNetworkTime);
            else
                RenderLobby();
        }

        private void OnDisable()
        {
            GameSettingsService.Current.Changed -= OnLanguageChanged;
            if (coordinator != null)
            {
                coordinator.ViewReceived -= OnViewReceived;
                coordinator.IntentRejected -= OnIntentRejected;
                coordinator.LobbyResetReceived -= OnLobbyResetReceived;
            }
            if (_startButton != null)
                _startButton.clicked -= OnStartClicked;
            if (_startButtonHoverArea != null)
            {
                _startButtonHoverArea.UnregisterCallback<PointerEnterEvent>(OnStartButtonPointerEnter);
                _startButtonHoverArea.UnregisterCallback<PointerLeaveEvent>(OnStartButtonPointerLeave);
            }
            if (_lobbyReadyButton != null)
                _lobbyReadyButton.clicked -= OnLobbyReadyClicked;
            if (_roundLimit5Button != null)
                _roundLimit5Button.clicked -= OnRoundLimit5Clicked;
            if (_roundLimit10Button != null)
                _roundLimit10Button.clicked -= OnRoundLimit10Clicked;
            if (_roundLimit15Button != null)
                _roundLimit15Button.clicked -= OnRoundLimit15Clicked;
            if (_roundLimit20Button != null)
                _roundLimit20Button.clicked -= OnRoundLimit20Clicked;
            if (_secretObjectiveToggle != null)
                _secretObjectiveToggle.UnregisterValueChangedCallback(OnSecretObjectiveChanged);
            if (_readyButton != null)
                _readyButton.clicked -= OnReadyClicked;
            if (_privateToggleButton != null)
                _privateToggleButton.clicked -= TogglePrivatePanel;
            if (_root != null)
                _root.UnregisterCallback<KeyDownEvent>(OnRootKeyDown);
            if (_returnToLobbyButton != null)
                _returnToLobbyButton.clicked -= OnReturnToLobbyClicked;
            if (_lobbyBackButton != null)
                _lobbyBackButton.clicked -= OnLobbyBackClicked;
        }

        private void Update()
        {
            if (_view == null)
            {
                RenderLobby();
                return;
            }

            if (_view.Phase == RoundPhase.Preparation)
            {
                _preparationTimerLabel.text = FormatTimer(CalculatePreparationRemainingSeconds(
                    _preparationEndsAtNetworkTime,
                    NetworkTime.time,
                    _view.Phase));
                return;
            }

            if (_view.Phase == RoundPhase.Round)
            {
                if (_unlimitedRound)
                {
                    _timerLabel.text = "∞";
                    _timerLabel.EnableInClassList("timer--critical", false);
                    return;
                }

                var remaining = CalculateRemainingSeconds(
                    _roundEndsAtNetworkTime,
                    NetworkTime.time,
                    _view.Phase);
                _timerLabel.text = FormatTimer(remaining);
                _timerLabel.EnableInClassList("timer--critical", IsCriticalRoundTime(remaining));
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

        public static float CalculatePreparationRemainingSeconds(
            double preparationEndsAtNetworkTime,
            double currentNetworkTime,
            RoundPhase phase)
        {
            if (phase != RoundPhase.Preparation || preparationEndsAtNetworkTime <= 0d)
                return 0f;

            return (float)Math.Max(0d, preparationEndsAtNetworkTime - currentNetworkTime);
        }

        public static bool ShouldReleaseCursor(
            RoundPhase phase,
            bool developerMenuOpen,
            bool requiresPointer) =>
            developerMenuOpen || phase != RoundPhase.Round || requiresPointer;

        public static bool IsUnlimitedRound(RoundPhase phase, double roundEndsAtNetworkTime) =>
            phase == RoundPhase.Round && roundEndsAtNetworkTime <= 0d;

        public static bool IsCriticalRoundTime(float remainingSeconds) =>
            remainingSeconds > 0f && remainingSeconds < 60f;

        private static void ConfigurePanelScaling(PanelSettings panelSettings)
        {
            if (panelSettings == null)
                return;

            panelSettings.scaleMode = PanelScaleMode.ScaleWithScreenSize;
            panelSettings.referenceResolution = new Vector2Int(1920, 1080);
            panelSettings.screenMatchMode = PanelScreenMatchMode.MatchWidthOrHeight;
            panelSettings.match = 1f;
        }

        /// <summary>
        /// Leaving the lobby means stopping the host or client and loading the
        /// main menu scene — network work the presenter must not do itself, so
        /// the owner of the transport supplies it.
        /// </summary>
        public void ConfigureLobbyExit(Action exit)
        {
            _lobbyExitHandler = exit;
        }

        private void OnLobbyBackClicked()
        {
            _lobbyExitHandler?.Invoke();
        }

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
            bool unlimitedTime = false,
            float preparationRemainingSeconds = 0f,
            UiLanguage language = UiLanguage.Polish)
        {
            if (view == null)
                throw new ArgumentNullException(nameof(view));

            var preparation = view.Phase == RoundPhase.Preparation;
            var alibiVisible = preparation && view.Alibi != null;
            BuildPrivatePanel(
                view,
                language,
                out string privateTitle,
                out string privateMotive,
                out string privateStep,
                out string privateLocation,
                out string privateProgress,
                out string privateText);

            return new RoundUiState(
                view.Phase,
                FormatRole(view.Role, language),
                view.CrimeDescription,
                alibiVisible,
                alibiVisible ? FormatAlibi(view.Alibi, language) : null,
                preparation ? FormatPreparationInstruction(view.Role, language) : null,
                preparation,
                preparation && !view.IsReady,
                preparation ? $"{UiText.Get("Gotowi", language)}: {view.ReadyPlayerCount}/{view.Players.Count}" : null,
                preparation && preparationRemainingSeconds > 0f,
                Mathf.Max(0f, preparationRemainingSeconds),
                view.Phase == RoundPhase.Round,
                Mathf.Max(0f, remainingSeconds),
                unlimitedTime,
                executionVisible: false,
                Array.Empty<ExecutionTargetView>(),
                view.Phase == RoundPhase.Round,
                privateTitle,
                privateMotive,
                privateStep,
                privateLocation,
                privateProgress,
                privateText,
                view.Phase == RoundPhase.Finished,
                view.Phase == RoundPhase.Finished ? FormatResultVerdict(view.Result, language) : null,
                view.Phase == RoundPhase.Finished ? FormatResultReason(view.Role, view.Result, language) : null,
                view.Phase == RoundPhase.Finished && view.Result != null && !view.Result.Won,
                view.Phase == RoundPhase.Finished ? FormatResult(view.Result, view.RoundReveal, language) : null,
                view.Phase == RoundPhase.Finished && isHost);
        }

        private void OnViewReceived(PlayerRoundView view, double roundEndsAtNetworkTime)
        {
            _view = view;
            _roundEndsAtNetworkTime = Math.Max(0d, roundEndsAtNetworkTime);
            _preparationEndsAtNetworkTime = Math.Max(0d, coordinator.CurrentPreparationEndsAtNetworkTime);
            _unlimitedRound = IsUnlimitedRound(view.Phase, _roundEndsAtNetworkTime);
            _rejectionLabel.text = string.Empty;
            SetVisible(_rejectionLabel, false);
            Render(BuildState(
                view,
                CalculateRemainingSeconds(_roundEndsAtNetworkTime, NetworkTime.time, view.Phase),
                coordinator.IsLocalHost,
                _unlimitedRound,
                CalculatePreparationRemainingSeconds(
                    _preparationEndsAtNetworkTime,
                    NetworkTime.time,
                    view.Phase),
                UiText.CurrentLanguage));
        }

        private void Render(RoundUiState state)
        {
            if (_lastRenderedPhase != state.Phase)
            {
                _privatePanelExpanded = true;
                _lastRenderedPhase = state.Phase;
            }

            SetVisible(_lobbyPanel, false);
            SetVisible(_preparationPanel, state.Phase == RoundPhase.Preparation);
            SetVisible(_hudPanel, state.Phase == RoundPhase.Round);
            SetVisible(_resultPanel, state.ResultVisible);
            _roleLabel.text = state.RoleText;
            _crimeLabel.text = state.CrimeText;
            _hudRoleLabel.text = $"{UiText.Get("Rola")}: {state.RoleText}";
            _hudCrimeLabel.text = $"{UiText.Get("Przestępstwo")}: {state.CrimeText}";
            _alibiLabel.text = state.AlibiText ?? string.Empty;
            _preparationInstructionLabel.text = state.PreparationInstructionText ?? string.Empty;
            SetVisible(_alibiSection, state.AlibiVisible);
            SetVisible(_preparationInstructionLabel, !string.IsNullOrWhiteSpace(state.PreparationInstructionText));
            SetVisible(_readyButton, state.ReadyButtonVisible);
            _readyButton.SetEnabled(state.ReadyButtonEnabled);
            _readyButton.text = UiText.Get(state.ReadyButtonEnabled ? "Gotowy" : "GOTOWY");
            _readyCountLabel.text = state.ReadyCountText ?? string.Empty;
            SetVisible(_readyCountLabel, state.ReadyButtonVisible);
            _preparationTimerLabel.text = FormatTimer(state.PreparationRemainingSeconds);
            SetVisible(_preparationTimerLabel, state.PreparationTimerVisible);
            _timerLabel.text = state.UnlimitedTime ? "∞" : FormatTimer(state.RemainingSeconds);
            _timerLabel.EnableInClassList(
                "timer--critical",
                !state.UnlimitedTime && IsCriticalRoundTime(state.RemainingSeconds));
            SetVisible(_timerLabel, state.TimerVisible);
            SetVisible(_privatePanel, state.PrivatePanelVisible);
            _privatePanel.EnableInClassList(
                "private-card--registry",
                state.PrivateTitle == UiText.Get("Rejestr Incydentów"));
            _privateTitleLabel.text = state.PrivateTitle ?? string.Empty;
            _privateMotiveLabel.text = state.PrivateMotive ?? string.Empty;
            _privateStepLabel.text = state.PrivateStep ?? string.Empty;
            _privateLocationLabel.text = state.PrivateLocation ?? string.Empty;
            _privateProgressLabel.text = state.PrivateProgress ?? string.Empty;
            _privateLabel.text = state.PrivateText ?? string.Empty;
            SetVisible(_privateMotiveLabel, !string.IsNullOrWhiteSpace(state.PrivateMotive));
            SetVisible(_privateStepLabel, !string.IsNullOrWhiteSpace(state.PrivateStep));
            SetVisible(_privateLocationLabel, !string.IsNullOrWhiteSpace(state.PrivateLocation));
            SetVisible(_privateProgressLabel, !string.IsNullOrWhiteSpace(state.PrivateProgress));
            SetVisible(_privateLabel, !string.IsNullOrWhiteSpace(state.PrivateText));
            ApplyPrivatePanelExpansion();
            _resultVerdictLabel.text = state.ResultVerdictText ?? string.Empty;
            _resultReasonLabel.text = state.ResultReasonText ?? string.Empty;
            _resultPanel.EnableInClassList("result-card--loss", state.ResultIsLoss);
            _resultLabel.text = state.ResultText ?? string.Empty;
            SetVisible(_returnToLobbyButton, state.ReturnToLobbyVisible);
            SetCursorFor(state.Phase, requiresPointer: false);
            if (state.Phase == RoundPhase.Round)
                _root.Focus();
        }

        private void RenderLobby()
        {
            bool connected = NetworkClient.isConnected || NetworkServer.active;
            int playerCount = coordinator.PublicLobbyPlayerCount;
            int presentedPlayerCount = coordinator.PublicLobbyPlayers.Count;
            SetVisible(_lobbyPanel, _lobbyMenuVisible || connected);
            SetVisible(_preparationPanel, false);
            SetVisible(_hudPanel, false);
            SetVisible(_resultPanel, false);
            SetVisible(_privatePanel, false);
            bool validPlayerCount = playerCount >= RoundEngine.MinPlayers
                && playerCount <= RoundEngine.MaxPlayers;
            var canStart = coordinator.IsLocalHost
                && validPlayerCount
                && coordinator.AreAllLobbyPlayersReady;
            SetVisible(_startButtonHoverArea, coordinator.IsLocalHost);
            SetVisible(_secretObjectiveToggle, true);
            SetVisible(_secretObjectiveSummary, true);
            _startButton.SetEnabled(canStart);
            _startButtonCanStart = canStart;
            _startButtonBlockedMessage = canStart
                ? string.Empty
                : UiText.Get("Wszyscy gracze muszą być gotowi.");
            _startButton.tooltip = string.Empty;
            RefreshStartButtonHoverInfo();
            _startButton.text = UiText.Get("Start Rundy");
            _playerCountLabel.text = presentedPlayerCount == playerCount
                ? $"{UiText.Get("Gracze w lobby")}: {playerCount}/{RoundEngine.MaxPlayers}"
                : $"{UiText.Get("Podgląd listy")}: {presentedPlayerCount}/{RoundEngine.MaxPlayers} • {UiText.Get("prawdziwi")}: {playerCount}";
            _secretObjectiveToggle.SetValueWithoutNotify(coordinator.HostAllowsSecretObjective);
            RefreshRoundLimitButtons();
            SetVisible(_lobbyReadyButton, connected);
            bool localReady = coordinator.IsLocalLobbyReady;
            _lobbyReadyButton.text = UiText.Get(localReady ? "Anuluj gotowość" : "Gotowy");
            _lobbyReadyButton.EnableInClassList("lobby-ready-button--active", localReady);
            bool secretObjectiveAvailable = playerCount >= RoundEngine.MinPlayersForSecretObjective;
            _secretObjectiveToggle.SetEnabled(coordinator.IsLocalHost && secretObjectiveAvailable);
            _secretObjectiveSummary.text = secretObjectiveAvailable
                ? coordinator.HostAllowsSecretObjective
                    ? UiText.Get("Sekretny Cel będzie użyty w tej Rundzie.")
                    : UiText.Get("Sekretny Cel jest wyłączony przez hosta.")
                : UiText.Get("Sekretny Cel jest dostępny od 5 graczy.");

            if (_lobbyMenuVisible || connected)
                SetCursorFor(RoundPhase.Lobby, false);
        }

        private void BindVisualTree()
        {
            var root = uiDocument.rootVisualElement;
            _root = root;
            _root.focusable = true;
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
            _preparationInstructionLabel = Required<Label>(root, "preparation-instruction-label");
            _preparationTimerLabel = Required<Label>(root, "preparation-timer-label");
            _readyCountLabel = Required<Label>(root, "ready-count-label");
            _readyButton = Required<Button>(root, "ready-button");
            _timerLabel = Required<Label>(root, "timer-label");
            _privatePanel = Required<VisualElement>(root, "private-panel");
            _privateContent = Required<VisualElement>(root, "private-content");
            _privateToggleButton = Required<Button>(root, "private-toggle-button");
            _privateTitleLabel = Required<Label>(root, "private-title-label");
            _privateMotiveLabel = Required<Label>(root, "private-motive-label");
            _privateStepLabel = Required<Label>(root, "private-step-label");
            _privateLocationLabel = Required<Label>(root, "private-location-label");
            _privateProgressLabel = Required<Label>(root, "private-progress-label");
            _privateLabel = Required<Label>(root, "private-label");
            _resultLabel = Required<Label>(root, "result-label");
            _resultVerdictLabel = Required<Label>(root, "result-verdict-label");
            _resultReasonLabel = Required<Label>(root, "result-reason-label");
            _rejectionLabel = Required<Label>(root, "rejection-label");
            _startButton = Required<Button>(root, "start-button");
            _startButtonHoverArea = Required<VisualElement>(root, "start-button-hover-area");
            _startButtonHoverInfo = Required<Label>(root, "start-button-hover-info");
            _lobbyReadyButton = Required<Button>(root, "lobby-ready-button");
            _playerCountLabel = Required<Label>(root, "player-count-label");
            _roundLimit5Button = Required<Button>(root, "round-limit-5-button");
            _roundLimit10Button = Required<Button>(root, "round-limit-10-button");
            _roundLimit15Button = Required<Button>(root, "round-limit-15-button");
            _roundLimit20Button = Required<Button>(root, "round-limit-20-button");
            _secretObjectiveToggle = Required<Toggle>(root, "secret-objective-toggle");
            _secretObjectiveSummary = Required<Label>(root, "secret-objective-summary");
            _returnToLobbyButton = Required<Button>(root, "return-to-lobby-button");
            _lobbyBackButton = Required<Button>(root, "lobby-back-button");

            // Covers the lobby, preparation and result screens. The HUD carries
            // no-hover-sound in UXML: a panel toggle the cursor passes over
            // mid-round should not chirp.
            UiSounds.Bind(root);
        }

        private void CaptureStaticLocalizedText()
        {
            _staticPolishText.Clear();
            foreach (TextElement element in _root.Query<TextElement>().ToList())
            {
                if (!string.IsNullOrWhiteSpace(element.text))
                    _staticPolishText[element] = element.text;
            }
            ApplyStaticLocalizedText();
        }

        private void ApplyStaticLocalizedText()
        {
            foreach (KeyValuePair<TextElement, string> entry in _staticPolishText)
                entry.Key.text = UiText.Get(entry.Value);
        }

        private void OnLanguageChanged()
        {
            ApplyStaticLocalizedText();
            if (_view != null)
                OnViewReceived(_view, _roundEndsAtNetworkTime);
            else
                RenderLobby();
        }

        private void OnStartClicked() => coordinator.RequestStartRound();

        private void OnStartButtonPointerEnter(PointerEnterEvent pointerEvent)
        {
            _startButtonHovered = true;
            RefreshStartButtonHoverInfo();
        }

        private void OnStartButtonPointerLeave(PointerLeaveEvent pointerEvent)
        {
            _startButtonHovered = false;
            RefreshStartButtonHoverInfo();
        }

        private void RefreshStartButtonHoverInfo()
        {
            if (_startButtonHoverInfo == null)
                return;

            _startButtonHoverInfo.text = _startButtonBlockedMessage ?? string.Empty;
            SetVisible(
                _startButtonHoverInfo,
                _startButtonHovered &&
                !_startButtonCanStart &&
                !string.IsNullOrWhiteSpace(_startButtonBlockedMessage));
        }

        private void OnLobbyReadyClicked() =>
            coordinator.RequestSetLobbyReady(!coordinator.IsLocalLobbyReady);

        private void OnRoundLimit5Clicked() => SelectRoundLimit(5);
        private void OnRoundLimit10Clicked() => SelectRoundLimit(10);
        private void OnRoundLimit15Clicked() => SelectRoundLimit(15);
        private void OnRoundLimit20Clicked() => SelectRoundLimit(20);

        private void SelectRoundLimit(int minutes)
        {
            coordinator.TrySetRoundLimitMinutes(minutes);
            RenderLobby();
        }

        private void RefreshRoundLimitButtons()
        {
            bool editable = coordinator.IsLocalHost && coordinator.CurrentView == null;
            int selectedMinutes = coordinator.RoundLimitMinutes;
            RefreshRoundLimitButton(_roundLimit5Button, 5, selectedMinutes, editable);
            RefreshRoundLimitButton(_roundLimit10Button, 10, selectedMinutes, editable);
            RefreshRoundLimitButton(_roundLimit15Button, 15, selectedMinutes, editable);
            RefreshRoundLimitButton(_roundLimit20Button, 20, selectedMinutes, editable);
        }

        private static void RefreshRoundLimitButton(
            Button button,
            int minutes,
            int selectedMinutes,
            bool editable)
        {
            button.SetEnabled(editable);
            button.EnableInClassList("lobby-round-limit-button--selected", minutes == selectedMinutes);
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

        private void OnReadyClicked()
        {
            _readyButton.SetEnabled(false);
            coordinator.RequestPlayerReady();
        }

        private void TogglePrivatePanel()
        {
            if (_view == null || _view.Phase != RoundPhase.Round)
                return;

            _privatePanelExpanded = !_privatePanelExpanded;
            ApplyPrivatePanelExpansion();
        }

        private void OnRootKeyDown(KeyDownEvent keyEvent)
        {
            if (keyEvent.keyCode != KeyCode.I)
                return;

            TogglePrivatePanel();
            keyEvent.StopPropagation();
        }

        private void ApplyPrivatePanelExpansion()
        {
            if (_privateContent == null || _privateToggleButton == null)
                return;

            SetVisible(_privateContent, _privatePanelExpanded);
            _privatePanel.EnableInClassList("private-card--collapsed", !_privatePanelExpanded);
            bool registry = _privatePanel.ClassListContains("private-card--registry");
            _privateToggleButton.text = _privatePanelExpanded
                ? UiText.Get("Zwiń [I]")
                : registry ? UiText.Get("Rejestr [I]") : UiText.Get("Cel [I]");
        }

        private void OnReturnToLobbyClicked() => coordinator.RequestReturnToLobby();

        private void OnLobbyResetReceived()
        {
            _view = null;
            _roundEndsAtNetworkTime = 0d;
            _preparationEndsAtNetworkTime = 0d;
            _unlimitedRound = false;
            _rejectionLabel.text = string.Empty;
            SetVisible(_rejectionLabel, false);
            RenderLobby();
        }

        private void OnIntentRejected(string reason)
        {
            _rejectionLabel.text = UiText.Get("Intencja została odrzucona.");
            SetVisible(_rejectionLabel, true);
        }

        private static string FormatAlibi(AlibiView alibi, UiLanguage language)
        {
            var builder = new StringBuilder();
            for (int index = 0; index < alibi.Entries.Count; index++)
            {
                AlibiEntry entry = alibi.Entries[index];
                builder.AppendLine(entry.IsHidden
                    ? $"{index + 1}. [{UiText.Get("Brak w Twojej wersji Alibi", language)}]"
                    : $"{index + 1}. {entry.Text}");
            }
            return builder.ToString().TrimEnd();
        }

        private static string FormatPreparationInstruction(RoundRole role, UiLanguage language)
        {
            if (role != RoundRole.Detective)
                return UiText.Get("Zapamiętaj swoją wersję Alibi. Po Przygotowaniu nie będzie można jej ponownie otworzyć.", language);

            return UiText.Get(
                "1. Przesłuchaj każdego Podejrzanego.\n" +
                "2. Porównuj zeznania z tym, co widzisz, oraz z Rejestrem Incydentów.\n" +
                "3. Masz jedną Egzekucję — pierwsze trafienie żywego Podejrzanego kończy Rundę.",
                language);
        }

        private static void BuildPrivatePanel(
            PlayerRoundView view,
            UiLanguage language,
            out string title,
            out string motive,
            out string step,
            out string location,
            out string progress,
            out string text)
        {
            motive = null;
            step = null;
            location = null;
            progress = null;
            text = null;
            var builder = new StringBuilder();
            switch (view.Role)
            {
                case RoundRole.Detective:
                    title = UiText.Get("Rejestr Incydentów", language);
                    if (view.IncidentRegistry == null || view.IncidentRegistry.Count == 0)
                    {
                        text = UiText.Get("Brak zgłoszonych lub odkrytych Incydentów.", language);
                        return;
                    }

                    for (int index = view.IncidentRegistry.Count - 1; index >= 0; index--)
                    {
                        IncidentRegistryEntryView incident = view.IncidentRegistry[index];
                        builder.AppendLine(
                            $"{FormatTimestamp(incident.ReportedAt)}\n" +
                            $"{HumanizeIdentifier(incident.Location.Value, language)}\n" +
                            $"{HumanizeIdentifier(incident.Effect.Value, language)} — {FormatIncidentKind(incident.Kind, language)}\n");
                    }
                    text = builder.ToString().TrimEnd();
                    return;

                case RoundRole.Guilty:
                    title = UiText.Get("Tropy do Alibi i Plan Ucieczki", language);
                    if (view.EscapePlan != null)
                    {
                        motive = UiText.Format("Cel: {0}", language, view.EscapePlan.Title) +
                                 $"\n{view.EscapePlan.Motive}";
                        step = view.EscapePlan.CurrentStep.HasValue
                            ? UiText.Format("Szukasz: {0}", language, view.EscapePlan.CurrentStepDescription)
                            : UiText.Get("Szukasz: wybierz i przygotuj punkt końcowy Ucieczki.", language);
                        location = view.EscapePlan.CurrentStep.HasValue
                            ? UiText.Format("Gdzie: {0}", language, view.EscapePlan.CurrentStepLocationHint)
                            : UiText.Get("Gdzie: jeden z opisanych punktów końcowych", language);
                        progress = UiText.Format(
                            "Postęp: {0}/{1} kroków przygotowania",
                            language,
                            view.EscapePlan.CompletedCommonStepCount,
                            view.EscapePlan.TotalCommonStepCount);
                    }

                    builder.AppendLine(UiText.Get("Tropy:", language));
                    if (view.AcquiredAlibiClues == null || view.AcquiredAlibiClues.Count == 0)
                        builder.AppendLine(UiText.Get("• brak", language));
                    else
                    {
                        foreach (AlibiClueView clue in view.AcquiredAlibiClues)
                            builder.AppendLine($"• {clue.Content}");
                    }

                    if (view.EscapePlan != null)
                    {
                        builder.AppendLine(UiText.Get("Wyjścia:", language));
                        foreach (EscapeExitOptionView option in view.EscapePlan.ExitOptions)
                        {
                            builder.AppendLine(
                                $"• {option.Description}\n  " +
                                UiText.Format("Gdzie: {0} — {1}", language, option.LocationHint,
                                    UiText.Get(option.IsPrepared ? "przygotowane" : "nieprzygotowane", language)));
                        }
                        if (view.EscapePlan.ActiveExit.HasValue)
                            builder.AppendLine(UiText.Get("Finał Ucieczki jest gotowy.", language));
                    }
                    text = builder.ToString().TrimEnd();
                    return;

                default:
                    if (view.PrivateObjective == null)
                    {
                        title = UiText.Get("Prywatny Cel", language);
                        text = UiText.Get("Brak przypisanego Celu.", language);
                        return;
                    }

                    PrivateObjectiveView objective = view.PrivateObjective;
                    title = UiText.Format("Cel: {0}", language, objective.Title);
                    motive = objective.Motive;
                    step = objective.CurrentStep.HasValue
                        ? UiText.Format("Szukasz: {0}", language, objective.CurrentStepDescription)
                        : UiText.Get("Szukasz: Cel ukończony.", language);
                    location = objective.CurrentStep.HasValue
                        ? UiText.Format("Gdzie: {0}", language, objective.CurrentStepLocationHint)
                        : null;
                    progress = UiText.Format(
                        "Postęp: {0}/{1}",
                        language,
                        objective.CompletedStepCount,
                        objective.TotalStepCount);
                    if (objective.Target.HasValue)
                        text = UiText.Format("Cel Wrobienia: Gracz {0}", language, objective.Target.Value.Value);
                    return;
            }
        }

        private static string FormatResult(
            PlayerResultView result,
            RoundRevealView reveal,
            UiLanguage language)
        {
            if (result == null)
                return UiText.Get("Brak wyniku Rundy.", language);
            var outcome = UiText.Get(result.Won ? "Wygrana" : "Przegrana", language);
            var survival = UiText.Get(
                result.Survived ? "Przetrwanie" : "Wykonano Egzekucję na Tobie",
                language);
            var cause = FormatEndCause(result.EndCause, language);
            var executed = result.ExecutedPlayer.HasValue
                ? UiText.Format("Gracz {0}", language, result.ExecutedPlayer.Value.Value)
                : UiText.Get("nikt", language);
            var builder = new StringBuilder();
            builder.AppendLine(outcome);
            builder.AppendLine(survival);
            builder.AppendLine(UiText.Format("Przyczyna: {0}", language, cause));
            builder.AppendLine(UiText.Format("Wykonany gracz: {0}", language, executed));
            builder.AppendLine(UiText.Format(
                "Prywatny Cel: {0}",
                language,
                UiText.Get(result.PrivateObjectiveCompleted ? "ukończony" : "nieukończony", language)));
            if (result.Escaped)
                builder.AppendLine(UiText.Get("Ucieczka: ukończona", language));

            if (reveal != null)
                AppendRoundReveal(builder, reveal, language);

            return builder.ToString().TrimEnd();
        }

        private static string FormatResultVerdict(PlayerResultView result, UiLanguage language) =>
            UiText.Get(
                result == null ? "BRAK ROZSTRZYGNIĘCIA" : result.Won ? "WYGRANA" : "PRZEGRANA",
                language);

        private static string FormatResultReason(
            RoundRole role,
            PlayerResultView result,
            UiLanguage language)
        {
            if (result == null)
                return UiText.Get("Runda zakończyła się bez dostępnego wyniku.", language);
            if (result.EndCause == RoundEndCause.Execution)
            {
                if (!result.ExecutedPlayer.HasValue)
                    return UiText.Get("Runda zakończyła się Egzekucją.", language);
                if (!result.Survived)
                    return UiText.Get("Egzekucja została wykonana na Tobie.", language);
                return UiText.Get(result.DetectiveWon
                    ? "Detektyw poprawnie rozpoznał Winnego."
                    : "Detektyw wykonał Egzekucję na Niewinnym.", language);
            }
            if (result.EndCause == RoundEndCause.Escape)
                return UiText.Get(role == RoundRole.Guilty
                    ? "Ucieczka Winnego zakończyła Rundę."
                    : "Winny ukończył Ucieczkę.", language);
            return UiText.Get("Limit Rundy upłynął bez Egzekucji Winnego.", language);
        }

        private static void AppendRoundReveal(
            StringBuilder builder,
            RoundRevealView reveal,
            UiLanguage language)
        {
            builder.AppendLine();
            builder.AppendLine(UiText.Get("UJAWNIENIE RUNDY", language));
            builder.AppendLine(UiText.Get("Role, Cele i indywidualne wyniki:", language));
            foreach (PlayerEndRevealView player in reveal.Players)
            {
                builder.Append(UiText.Format(
                    "• Gracz {0}: {1}",
                    language,
                    player.Player.Value,
                    FormatRole(player.Role, language)));
                if (player.PrivateObjective != null)
                {
                    builder.Append(UiText.Format(
                        "; Cel „{0}” {1}/{2}",
                        language,
                        player.PrivateObjective.Title,
                        player.PrivateObjective.CompletedStepCount,
                        player.PrivateObjective.TotalStepCount));
                    if (player.PrivateObjective.Target.HasValue)
                        builder.Append(UiText.Format(
                            "; Cel Wrobienia: Gracz {0}",
                            language,
                            player.PrivateObjective.Target.Value.Value));
                }
                builder.AppendLine(UiText.Format(
                    "; {0}; {1}",
                    language,
                    UiText.Get(player.Result.Won ? "wygrana" : "przegrana", language),
                    UiText.Get(player.Result.Survived ? "przeżył" : "wyeliminowany", language)));
            }

            builder.AppendLine(UiText.Get("Tropy do Alibi:", language));
            if (reveal.AcquiredAlibiClues.Count == 0)
                builder.AppendLine(UiText.Get("• brak", language));
            foreach (AlibiClueRevealView clue in reveal.AcquiredAlibiClues)
                builder.AppendLine($"• {clue.Content}");

            builder.AppendLine(UiText.Get("Plan Ucieczki:", language));
            if (reveal.EscapePlan.Actions.Count == 0)
                builder.AppendLine(UiText.Get("• brak działań", language));
            foreach (EscapeActionRevealView action in reveal.EscapePlan.Actions)
            {
                builder.AppendLine($"• {FormatEscapeAction(action.Kind, language)}");
            }
            builder.AppendLine(UiText.Get(reveal.EscapePlan.SuccessfulExit.HasValue
                ? "Udane wyjście: przygotowany punkt Ucieczki"
                : "Udane wyjście: brak", language));

            builder.AppendLine(UiText.Get("Incydenty i autorzy:", language));
            if (reveal.Incidents.Count == 0)
                builder.AppendLine(UiText.Get("• brak", language));
            foreach (IncidentRevealView incident in reveal.Incidents)
            {
                builder.AppendLine(
                    UiText.Format(
                        "• {0} / {1}; autor Gracz {2} ({3})",
                        language,
                        HumanizeIdentifier(incident.Effect.Value, language),
                        HumanizeIdentifier(incident.Location.Value, language),
                        incident.Author.Value,
                        FormatIncidentKind(incident.Kind, language)));
            }
        }

        private static string FormatTimestamp(IncidentTimestamp timestamp)
        {
            long totalSeconds = timestamp.MillisecondsSinceRoundStart / 1000;
            return $"{totalSeconds / 60:00}:{totalSeconds % 60:00}";
        }

        private static string FormatIncidentKind(IncidentKind kind, UiLanguage language) =>
            UiText.Get(kind == IncidentKind.Loud ? "głośny" : "cichy", language);

        private static string FormatEscapeAction(EscapeActionKind kind, UiLanguage language)
        {
            string text;
            switch (kind)
            {
                case EscapeActionKind.Completed: text = "ukończono Ucieczkę"; break;
                case EscapeActionKind.PreparedCommonStep: text = "przygotowano element Planu Ucieczki"; break;
                case EscapeActionKind.PreparedExit: text = "przygotowano punkt Ucieczki"; break;
                case EscapeActionKind.AttemptStarted: text = "rozpoczęto finał Ucieczki"; break;
                case EscapeActionKind.AttemptInterrupted: text = "przerwano finał Ucieczki"; break;
                default: text = "wykonano działanie Planu Ucieczki"; break;
            }
            return UiText.Get(text, language);
        }

        private static string HumanizeIdentifier(string value, UiLanguage language)
        {
            if (string.IsNullOrWhiteSpace(value))
                return UiText.Get("nieznane", language);
            string normalized = value.Replace('-', ' ').Replace('_', ' ').Trim();
            if (normalized.Length == 0)
                return UiText.Get("nieznane", language);
            string humanized = char.ToUpperInvariant(normalized[0]) + normalized.Substring(1);
            return UiText.Get(humanized, language);
        }

        private static string FormatEndCause(RoundEndCause cause, UiLanguage language)
        {
            string text;
            switch (cause)
            {
                case RoundEndCause.Execution: text = "Egzekucja"; break;
                case RoundEndCause.Escape: text = "Ucieczka Winnego"; break;
                default: text = "Upłynął Limit Rundy"; break;
            }
            return UiText.Get(text, language);
        }

        private static string FormatRole(RoundRole role, UiLanguage language = UiLanguage.Polish)
        {
            switch (role)
            {
                case RoundRole.Detective: return UiText.Get("Detektyw", language);
                case RoundRole.Guilty: return UiText.Get("Winny", language);
                default: return UiText.Get("Niewinny", language);
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
