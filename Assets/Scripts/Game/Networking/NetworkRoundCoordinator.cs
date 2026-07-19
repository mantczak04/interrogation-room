using System;
using System.Collections.Generic;
using System.Linq;
using InterrogationRoom.Content;
using InterrogationRoom.Debugging;
using InterrogationRoom.Domain;
using Mirror;
using UnityEngine;

namespace InterrogationRoom.Networking
{
    /// <summary>
    /// The sole Mirror adapter for a Runda. The host owns RoundEngine and all
    /// secrets; clients send intentions and receive only their targeted view.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class NetworkRoundCoordinator : MonoBehaviour
    {
        public const float PreparationLimitSeconds = 30f;
        public const float AllReadyPreparationSeconds = 3f;
        private const float ReusedStartPositionOffset = 0.75f;

        private static readonly AlibiClueId DeveloperPhysicalClueId =
            new AlibiClueId("paragon-cztery-kompoty");

        [SerializeField]
        [Tooltip("Sprawa converted to an immutable CaseDefinition when the host starts the Runda.")]
        private CaseAsset caseAsset;

        [SerializeField]
        [Tooltip("Additional hand-authored Sprawy available to the host in the lobby.")]
        private List<CaseAsset> additionalCases = new List<CaseAsset>();

        [SerializeField]
        [Tooltip("Authorowane Osobiste Sprawy losowane i przypisywane Niewinnym przez RoundEngine.")]
        private List<PersonalMatterAsset> personalMatterAssets = new List<PersonalMatterAsset>();

        [SerializeField, Min(1f)]
        [Tooltip("Limit Rundy measured authoritatively by the server, in seconds.")]
        private float roundLimitSeconds = 600f;

        private readonly Dictionary<int, NetworkConnectionToClient> _connectionsByPlayerId =
            new Dictionary<int, NetworkConnectionToClient>();
        private readonly HashSet<int> _rejectedLateJoiners = new HashSet<int>();
        private readonly Dictionary<int, IRoundHitSource> _hitSourcesByPlayerId =
            new Dictionary<int, IRoundHitSource>();

        private RoundEngine _engine = new RoundEngine();
        private RoundPhase _phase = RoundPhase.Lobby;
        private bool _serverHandlerRegistered;
        private bool _clientHandlersRegistered;
        private bool _hostAllowsSecretObjective = true;
        private int _publicLobbyPlayerCount;
        private int _lastBroadcastLobbyPlayerCount = -1;
        private double _roundDeadline;
        private double _roundStartedAtNetworkTime;
        private double _preparationDeadline;
        private RoundDeveloperPlan _developerPlan;

        public PlayerRoundView CurrentView { get; private set; }
        public double CurrentRoundEndsAtNetworkTime { get; private set; }
        public double CurrentPreparationEndsAtNetworkTime { get; private set; }
        public bool IsLocalHost => NetworkServer.activeHost;
        public int ConnectedPlayerCount => _connectionsByPlayerId.Count;
        public int PublicLobbyPlayerCount => NetworkServer.active
            ? ConnectedPlayerCount
            : _publicLobbyPlayerCount;
        public bool AllowsPhysicalRoundActions => NetworkServer.active
            ? _phase == RoundPhase.Round
            : CurrentView?.Phase == RoundPhase.Round;
        public bool HostAllowsSecretObjective => _hostAllowsSecretObjective;
        public static bool DeveloperToolsAvailable => Application.isEditor || Debug.isDebugBuild;
        public RoundDeveloperPlan ActiveDeveloperPlan => DeveloperToolsAvailable ? _developerPlan : null;
        public bool IsDeveloperRoundUnlimited => DeveloperToolsAvailable && _developerPlan != null;
        public PlayerRoundView DeveloperControlledView =>
            DeveloperToolsAvailable && _developerPlan != null
                ? _engine.ViewFor(_developerPlan.ControlledPlayer)
                : null;
        public IReadOnlyList<PlayerId> ConnectedPlayers => _connectionsByPlayerId.Keys
            .OrderBy(id => id)
            .Select(id => new PlayerId(id))
            .ToArray();
        public int EffectiveSecretObjectiveCount => RoundLobbyRules.ResolveSecretObjectiveCount(
            ConnectedPlayerCount,
            _hostAllowsSecretObjective);

        public event Action<PlayerRoundView, double> ViewReceived;
        public event Action<string> IntentRejected;
        public event Action LobbyResetReceived;
        public event Action ServerRoundReset;
        public event Action<NetworkIdentity> ServerExecutionAccepted;
        public event Action ServerGameplayRoundStarted;
        public event Action ServerGameplayRoundEnded;

        private void Awake()
        {
            RoundMessageSerialization.Register();
            if (DeveloperToolsAvailable && GetComponent<RoundDeveloperPanel>() == null)
                gameObject.AddComponent<RoundDeveloperPanel>();
        }

        private void Update()
        {
            UpdateServer();
            UpdateClient();
        }

        private void OnDisable()
        {
            if (_serverHandlerRegistered && NetworkServer.active)
                NetworkServer.UnregisterHandler<RoundIntentMessage>();
            if (_clientHandlersRegistered && NetworkClient.active)
            {
                NetworkClient.UnregisterHandler<RoundViewMessage>();
                NetworkClient.UnregisterHandler<RoundIntentRejectedMessage>();
                NetworkClient.UnregisterHandler<RoundLobbyResetMessage>();
                NetworkClient.UnregisterHandler<RoundLobbyStateMessage>();
            }

            ResetServerRuntime();
            _clientHandlersRegistered = false;
            CurrentView = null;
            CurrentRoundEndsAtNetworkTime = 0d;
            CurrentPreparationEndsAtNetworkTime = 0d;
            _publicLobbyPlayerCount = 0;
        }

        public void RequestStartRound()
        {
            SendIntent(RoundIntentMessage.StartRound());
        }

        public void RequestEndPreparation()
        {
            SendIntent(RoundIntentMessage.EndPreparation());
        }

        public void RequestPlayerReady()
        {
            SendIntent(RoundIntentMessage.PlayerReady());
        }

        public void RequestReturnToLobby()
        {
            SendIntent(RoundIntentMessage.ReturnToLobby());
        }

        public bool TrySetSecretObjectiveEnabled(bool enabled)
        {
            if (!IsLocalHost || _phase != RoundPhase.Lobby)
                return false;

            _hostAllowsSecretObjective = enabled;
            return true;
        }

        public bool TryStartDeveloperScenario(
            RoundDeveloperScenario scenario,
            int targetPlayerCount,
            PlayerId controlledPlayer,
            out string rejectionReason)
        {
            rejectionReason = null;
            if (!DeveloperToolsAvailable)
                return RejectDeveloper("Developer scenarios are disabled in release builds.", out rejectionReason);
            if (!NetworkServer.activeHost || !IsLocalHost)
                return RejectDeveloper("A developer scenario can be started only by the local host.", out rejectionReason);
            if (_phase != RoundPhase.Lobby)
                return RejectDeveloper("Return to the lobby before starting another developer scenario.", out rejectionReason);

            var cases = AvailableCases();
            var caseIndex = SelectRandomValidCaseIndex(cases, _ => 0);
            if (caseIndex < 0)
                return RejectDeveloper("No configured CaseAsset passes validation.", out rejectionReason);
            if (!HasCompletePhysicalRoster())
                return RejectDeveloper("Wait until every connected player has spawned its physical Runda components.", out rejectionReason);

            CaseDefinition definition;
            try
            {
                definition = cases[caseIndex].ToDefinition();
            }
            catch (Exception exception)
            {
                Debug.LogError($"[NetworkRoundCoordinator] Developer CaseAsset is invalid: {exception}", cases[caseIndex]);
                return RejectDeveloper("The selected CaseAsset is invalid.", out rejectionReason);
            }

            if (!TryBuildPersonalMatterPool(out var personalMatters, out var personalMatterError))
                return RejectDeveloper(personalMatterError, out rejectionReason);

            if (!RoundDeveloperScenarioPlanner.TryCreate(
                    definition,
                    ConnectedPlayers,
                    controlledPlayer,
                    targetPlayerCount,
                    scenario,
                    DeveloperPhysicalClueId,
                    out var plan,
                    out rejectionReason))
            {
                return false;
            }

            _developerPlan = plan;
            if (Submit(null, new RoundCommand.StartRound(
                    definition,
                    plan.Players,
                    plan.Seed,
                    plan.SecretObjectiveCount,
                    personalMatters: personalMatters)))
            {
                return true;
            }

            _developerPlan = null;
            return RejectDeveloper("RoundEngine rejected the prepared developer scenario.", out rejectionReason);
        }

        public bool TryFinishDeveloperScenario(
            RoundDeveloperFinish finish,
            out string rejectionReason)
        {
            rejectionReason = null;
            if (!DeveloperToolsAvailable || _developerPlan == null)
                return RejectDeveloper("No developer scenario is active.", out rejectionReason);
            if (!NetworkServer.activeHost || _phase != RoundPhase.Round)
                return RejectDeveloper("A developer ending is available only to the local host during Runda.", out rejectionReason);

            if (finish == RoundDeveloperFinish.TimeExpired)
                return SubmitDeveloper(new RoundCommand.TimeExpired(), out rejectionReason);

            PlayerId? target = null;
            if (finish == RoundDeveloperFinish.ExecuteSecretTarget)
            {
                target = _engine.ViewFor(_developerPlan.ControlledPlayer)
                    ?.PrivateObjective?.Target;
            }
            else
            {
                var requestedRole = finish == RoundDeveloperFinish.ExecuteGuilty
                    ? RoundRole.Guilty
                    : RoundRole.Innocent;
                foreach (var player in _developerPlan.Players)
                {
                    if (_engine.ViewFor(player)?.Role != requestedRole)
                        continue;

                    target = player;
                    break;
                }
            }

            if (!target.HasValue)
                return RejectDeveloper("The selected developer execution target is unavailable.", out rejectionReason);

            return SubmitDeveloper(new RoundCommand.Execute(target.Value), out rejectionReason);
        }

        [Server]
        public bool TryAdvancePhysicalObjective(
            NetworkIdentity actor,
            string objectiveStepId)
        {
            if (!TryGetPhysicalActor(actor, out var playerId) ||
                string.IsNullOrWhiteSpace(objectiveStepId))
                return false;

            var objective = _engine.ViewFor(playerId)?.PrivateObjective;
            if (objective == null)
                return false;

            return Submit(null, new RoundCommand.AdvancePrivateObjective(
                playerId,
                objective.Id,
                new PrivateObjectiveStepId(objectiveStepId)));
        }

        [Server]
        public bool TryRegisterPhysicalIncident(
            NetworkIdentity actor,
            string incidentId,
            IncidentKind kind,
            string effectId,
            string locationId,
            string objectiveStepId,
            out bool objectiveAdvanced)
        {
            objectiveAdvanced = false;
            if (!TryGetPhysicalActor(actor, out var physicalPlayerId))
                return false;

            var playerId = ResolveDeveloperIncidentAuthor(physicalPlayerId);

            var before = _engine.ViewFor(playerId)?.PrivateObjective;
            PrivateObjectiveStepReference objectiveStep = null;
            if (!string.IsNullOrWhiteSpace(objectiveStepId) && before != null)
            {
                objectiveStep = new PrivateObjectiveStepReference(
                    before.Id,
                    new PrivateObjectiveStepId(objectiveStepId));
            }

            bool accepted = Submit(null, new RoundCommand.RegisterIncident(
                playerId,
                new IncidentId(incidentId),
                kind,
                new IncidentEffectId(effectId),
                new IncidentLocationId(locationId),
                CurrentRoundTimestamp(),
                objectiveStep));
            if (!accepted)
                return false;

            var after = _engine.ViewFor(playerId)?.PrivateObjective;
            objectiveAdvanced = before != null && after != null &&
                                after.CompletedStepCount > before.CompletedStepCount;
            return true;
        }

        [Server]
        public bool TryDiscoverPhysicalQuietIncident(NetworkIdentity viewer, string incidentId)
        {
            return TryGetPhysicalActor(viewer, out var playerId) &&
                   Submit(null, new RoundCommand.DiscoverQuietIncident(
                       playerId,
                       new IncidentId(incidentId),
                       CurrentRoundTimestamp()));
        }

        [Server]
        public bool TryAcquirePhysicalAlibiClue(
            NetworkIdentity actor,
            string clueId,
            string incidentId,
            IncidentKind kind,
            string effectId,
            string locationId)
        {
            return TryGetPhysicalActor(actor, out var playerId) &&
                   Submit(null, new RoundCommand.AcquireAlibiClue(
                       playerId,
                       new AlibiClueId(clueId),
                       new IncidentId(incidentId),
                       kind,
                       new IncidentEffectId(effectId),
                       new IncidentLocationId(locationId),
                       CurrentRoundTimestamp()));
        }

        [Server]
        public bool TryPreparePhysicalEscape(NetworkIdentity actor, string planId, string stepId)
        {
            return TryGetPhysicalActor(actor, out var playerId) &&
                   Submit(null, new RoundCommand.PrepareEscape(
                       playerId,
                       new EscapePlanId(planId),
                       new EscapeStepId(stepId)));
        }

        [Server]
        public bool TryBeginPhysicalEscape(
            NetworkIdentity actor,
            string planId,
            string exitId,
            string incidentId)
        {
            return TryGetPhysicalActor(actor, out var playerId) &&
                   Submit(null, new RoundCommand.BeginEscape(
                       playerId,
                       new EscapePlanId(planId),
                       new EscapeExitId(exitId),
                       new IncidentId(incidentId),
                       CurrentRoundTimestamp()));
        }

        [Server]
        public bool TryInterruptPhysicalEscape(NetworkIdentity actor, string planId, string exitId)
        {
            return TryGetPhysicalActor(actor, out var playerId) &&
                   Submit(null, new RoundCommand.InterruptEscape(
                       playerId,
                       new EscapePlanId(planId),
                       new EscapeExitId(exitId)));
        }

        [Server]
        public bool TryCompletePhysicalEscape(NetworkIdentity actor, string planId, string exitId)
        {
            return TryGetPhysicalActor(actor, out var playerId) &&
                   Submit(null, new RoundCommand.CompleteEscape(
                       playerId,
                       new EscapePlanId(planId),
                       new EscapeExitId(exitId)));
        }

        private static void SendIntent(RoundIntentMessage message)
        {
            if (!NetworkClient.isConnected)
            {
                Debug.LogWarning("[NetworkRoundCoordinator] Cannot send a Runda intention without a connected client.");
                return;
            }

            NetworkClient.Send(message);
        }

        private void UpdateServer()
        {
            if (!NetworkServer.active)
            {
                if (_serverHandlerRegistered)
                    ResetServerRuntime();
                return;
            }

            if (!_serverHandlerRegistered)
            {
                NetworkServer.RegisterHandler<RoundIntentMessage>(OnServerIntent);
                _serverHandlerRegistered = true;
            }

            SynchronizeConnections();
            UpdateRoundTimer();
        }

        private void UpdateClient()
        {
            if (!NetworkClient.active)
            {
                _clientHandlersRegistered = false;
                _publicLobbyPlayerCount = 0;
                return;
            }

            if (_clientHandlersRegistered)
                return;

            NetworkClient.RegisterHandler<RoundViewMessage>(OnClientView);
            NetworkClient.RegisterHandler<RoundIntentRejectedMessage>(OnClientIntentRejected);
            NetworkClient.RegisterHandler<RoundLobbyResetMessage>(OnClientLobbyReset);
            NetworkClient.RegisterHandler<RoundLobbyStateMessage>(OnClientLobbyState);
            _clientHandlersRegistered = true;
        }

        private void SynchronizeConnections()
        {
            var connectedIds = new HashSet<int>();
            foreach (var connection in NetworkServer.connections.Values)
            {
                if (connection == null || !connection.isAuthenticated)
                    continue;

                var playerId = ConnectionToPlayerId(connection);
                connectedIds.Add(playerId.Value);
                BindHitSource(playerId, connection);
                if (_connectionsByPlayerId.ContainsKey(playerId.Value)
                    || _rejectedLateJoiners.Contains(playerId.Value))
                    continue;

                if (_phase == RoundPhase.Lobby)
                {
                    _connectionsByPlayerId.Add(playerId.Value, connection);
                    continue;
                }

                // A reconnect is accepted only when the transport/authentication
                // layer restores the same stable id. KCP currently derives it
                // from connectionId; a genuinely new late join remains outside MVP.
                if (_engine.ViewFor(playerId) != null)
                {
                    _connectionsByPlayerId.Add(playerId.Value, connection);
                    DeliverView(playerId, connection);
                }
                else
                {
                    _rejectedLateJoiners.Add(playerId.Value);
                    Debug.LogWarning($"[NetworkRoundCoordinator] Rejecting late join connection {connection.connectionId} during an active Runda.");
                    connection.Disconnect();
                }
            }

            foreach (var disconnectedId in _connectionsByPlayerId.Keys.Where(id => !connectedIds.Contains(id)).ToArray())
            {
                UnbindHitSource(disconnectedId);
                _connectionsByPlayerId.Remove(disconnectedId);
            }
            _rejectedLateJoiners.RemoveWhere(id => !connectedIds.Contains(id));

            if (_phase == RoundPhase.Lobby)
                BroadcastLobbyState();
        }

        private void OnServerIntent(NetworkConnectionToClient sender, RoundIntentMessage message)
        {
            if (sender == null || !sender.isAuthenticated)
                return;

            var senderId = ConnectionToPlayerId(sender);
            if (!_connectionsByPlayerId.ContainsKey(senderId.Value))
            {
                if (_phase == RoundPhase.Lobby)
                    _connectionsByPlayerId[senderId.Value] = sender;
                else
                {
                    Reject(sender, "Sender is not part of the current Skład Rundy.");
                    return;
                }
            }

            switch (message.Kind)
            {
                case RoundIntentKind.StartRound:
                    if (!IsHost(sender))
                    {
                        Reject(sender, "Only the host may start the Runda.");
                        return;
                    }
                    StartRound(sender);
                    break;

                case RoundIntentKind.EndPreparation:
                    if (!IsHost(sender))
                    {
                        Reject(sender, "Only the host may end Przygotowanie.");
                        return;
                    }
                    Submit(sender, new RoundCommand.EndPreparation());
                    break;

                case RoundIntentKind.ReturnToLobby:
                    if (!IsHost(sender))
                    {
                        Reject(sender, "Only the host may return the Runda to lobby.");
                        return;
                    }
                    ReturnToLobby(sender);
                    break;

                default:
                    if (!RoundIntentMapper.TryMap(
                            message,
                            senderId,
                            CurrentRoundTimestamp(),
                            out var command,
                            out var rejectionReason))
                    {
                        Reject(sender, rejectionReason);
                        return;
                    }

                    Submit(sender, command);
                    break;
            }
        }

        private void StartRound(NetworkConnectionToClient sender)
        {
            var cases = AvailableCases();
            var caseIndex = SelectRandomValidCaseIndex(
                cases,
                count => UnityEngine.Random.Range(0, count));
            if (caseIndex < 0)
            {
                Reject(sender, "No configured CaseAsset passes validation.");
                return;
            }

            var selectedCase = cases[caseIndex];

            CaseDefinition definition;
            try
            {
                definition = selectedCase.ToDefinition();
            }
            catch (Exception exception)
            {
                Debug.LogError($"[NetworkRoundCoordinator] Selected CaseAsset is invalid: {exception}", selectedCase);
                Reject(sender, "The selected CaseAsset is invalid.");
                return;
            }

            if (!TryBuildPersonalMatterPool(out var personalMatters, out var personalMatterError))
            {
                Reject(sender, personalMatterError);
                return;
            }

            var players = _connectionsByPlayerId.Keys
                .OrderBy(id => id)
                .Select(id => new PlayerId(id))
                .ToArray();
            if (!HasCompletePhysicalRoster())
            {
                Reject(sender, "Every player must be spawned with all physical Runda components before the Runda starts.");
                return;
            }

            var seed = UnityEngine.Random.Range(int.MinValue, int.MaxValue);
            Submit(sender, new RoundCommand.StartRound(
                definition,
                players,
                seed,
                EffectiveSecretObjectiveCount,
                personalMatters: personalMatters));
        }

        private void ReturnToLobby(NetworkConnectionToClient sender)
        {
            if (_phase != RoundPhase.Finished)
            {
                Reject(sender, "Return to lobby is only allowed after the Runda ends.");
                return;
            }

            foreach (var connection in _connectionsByPlayerId.Values)
            {
                if (connection?.identity == null)
                    continue;

                FindPort<IRoundWeaponPort>(connection.identity)
                    ?.SetWeaponAuthorizationServer(false);
                FindPort<IRoundEliminationPort>(connection.identity)
                    ?.ResetEliminationServer();
            }

            _engine = new RoundEngine();
            _phase = RoundPhase.Lobby;
            _developerPlan = null;
            _hostAllowsSecretObjective = true;
            _roundDeadline = 0d;
            _roundStartedAtNetworkTime = 0d;
            _preparationDeadline = 0d;
            RelocatePlayersToStartRoom();
            ServerGameplayRoundEnded?.Invoke();
            ServerRoundReset?.Invoke();

            foreach (var connection in _connectionsByPlayerId.Values)
            {
                if (connection != null && connection.isAuthenticated)
                    connection.Send(new RoundLobbyResetMessage());
            }

            BroadcastLobbyState(force: true);
        }

        /// <summary>
        /// The Sprawa is drawn by the server, never picked by the host. Only
        /// cases whose authoring validation is clean can be drawn; returns -1
        /// when no case qualifies.
        /// </summary>
        public static int SelectRandomValidCaseIndex(
            IReadOnlyList<CaseAsset> cases,
            Func<int, int> pickIndex)
        {
            var validIndices = new List<int>();
            for (var index = 0; index < cases.Count; index++)
            {
                if (cases[index] != null && cases[index].Validate().Count == 0)
                    validIndices.Add(index);
            }

            if (validIndices.Count == 0)
                return -1;

            var picked = pickIndex(validIndices.Count);
            if (picked < 0 || picked >= validIndices.Count)
                return -1;

            return validIndices[picked];
        }

        private bool TryBuildPersonalMatterPool(
            out IReadOnlyList<PersonalMatterDefinition> definitions,
            out string rejectionReason)
        {
            var converted = new List<PersonalMatterDefinition>();
            var configured = personalMatterAssets ?? new List<PersonalMatterAsset>();
            var invalid = false;
            foreach (var asset in configured)
            {
                if (asset == null)
                {
                    Debug.LogError("[NetworkRoundCoordinator] Personal matter list contains a missing asset reference.", this);
                    invalid = true;
                    continue;
                }

                var errors = asset.Validate();
                if (errors.Count > 0)
                {
                    foreach (var error in errors)
                    {
                        Debug.LogError(
                            $"[NetworkRoundCoordinator] PersonalMatterAsset '{asset.name}': {error}",
                            asset);
                    }
                    invalid = true;
                    continue;
                }

                try
                {
                    converted.Add(asset.ToDefinition());
                }
                catch (Exception exception)
                {
                    Debug.LogError(
                        $"[NetworkRoundCoordinator] PersonalMatterAsset '{asset.name}' is invalid: {exception}",
                        asset);
                    invalid = true;
                }
            }

            if (converted.Select(definition => definition.Id).Distinct().Count() != converted.Count)
            {
                Debug.LogError("[NetworkRoundCoordinator] Personal matter pool contains duplicate stable ids.", this);
                invalid = true;
            }

            definitions = converted;
            rejectionReason = invalid
                ? "At least one configured PersonalMatterAsset is invalid."
                : null;
            return !invalid;
        }

        private IReadOnlyList<CaseAsset> AvailableCases()
        {
            var result = new List<CaseAsset>();
            if (caseAsset != null)
                result.Add(caseAsset);
            if (additionalCases != null)
            {
                foreach (var candidate in additionalCases)
                {
                    if (candidate != null && !result.Contains(candidate))
                        result.Add(candidate);
                }
            }
            return result;
        }

        private void OnValidate()
        {
            var cases = AvailableCases();
            if (cases.Count == 0)
            {
                Debug.LogError("[NetworkRoundCoordinator] At least one CaseAsset is required.", this);
                return;
            }

            foreach (var authoredCase in cases)
            {
                foreach (var error in authoredCase.Validate())
                    Debug.LogError($"[NetworkRoundCoordinator] CaseAsset '{authoredCase.name}': {error}", authoredCase);
            }

            var configuredPersonalMatters = personalMatterAssets ?? new List<PersonalMatterAsset>();
            foreach (var personalMatter in configuredPersonalMatters)
            {
                if (personalMatter == null)
                {
                    Debug.LogError("[NetworkRoundCoordinator] Personal matter list contains a missing asset reference.", this);
                    continue;
                }

                foreach (var error in personalMatter.Validate())
                {
                    Debug.LogError(
                        $"[NetworkRoundCoordinator] PersonalMatterAsset '{personalMatter.name}': {error}",
                        personalMatter);
                }
            }

        }

        private bool Submit(NetworkConnectionToClient sender, RoundCommand command)
        {
            if (command is RoundCommand.StartRound &&
                !NetworkManager.startPositions.Any(position => position != null))
            {
                const string reason = "At least one NetworkStartPosition is required to start a Runda.";
                if (sender != null)
                {
                    Reject(sender, reason);
                }
                else
                {
                    Debug.LogError($"[NetworkRoundCoordinator] {reason}", this);
                }
                return false;
            }

            var transition = _engine.Handle(command);
            if (!transition.Accepted)
            {
                if (sender != null)
                    Reject(sender, transition.RejectionReason);
                return false;
            }

            _phase = transition.State.Phase;
            if (command is RoundCommand.StartRound)
            {
                _preparationDeadline = IsDeveloperRoundUnlimited
                    ? 0d
                    : NetworkTime.time + PreparationLimitSeconds;
                RelocatePlayersToStartRoom();
                ConfigureRoundWeapons();
            }
            else if (command is RoundCommand.MarkPlayerReady
                     && transition.State.ReadyPlayerCount == transition.State.Players.Count)
            {
                _preparationDeadline = ShortenedPreparationDeadline(
                    _preparationDeadline,
                    NetworkTime.time,
                    AllReadyPreparationSeconds);
            }
            else if (command is RoundCommand.EndPreparation)
            {
                _preparationDeadline = 0d;
                _roundStartedAtNetworkTime = NetworkTime.time;
                _roundDeadline = _roundStartedAtNetworkTime + roundLimitSeconds;
                ServerGameplayRoundStarted?.Invoke();
            }
            else if (_phase == RoundPhase.Finished)
            {
                _roundDeadline = 0d;
                ServerGameplayRoundEnded?.Invoke();
            }

            DeliverAllViews();
            return true;
        }

        private IncidentTimestamp CurrentRoundTimestamp()
        {
            if (_phase != RoundPhase.Round)
                return new IncidentTimestamp(0);

            double elapsedMilliseconds = Math.Max(
                0d,
                (NetworkTime.time - _roundStartedAtNetworkTime) * 1000d);
            return new IncidentTimestamp((long)Math.Min(long.MaxValue, elapsedMilliseconds));
        }

        private void BindHitSource(PlayerId playerId, NetworkConnectionToClient connection)
        {
            if (_hitSourcesByPlayerId.ContainsKey(playerId.Value) || connection?.identity == null)
                return;

            var hitSource = FindPort<IRoundHitSource>(connection.identity);
            if (hitSource == null)
                return;

            hitSource.PlayerHitReceivedServer += OnPlayerHitServer;
            _hitSourcesByPlayerId.Add(playerId.Value, hitSource);
        }

        private void UnbindHitSource(int playerId)
        {
            if (!_hitSourcesByPlayerId.TryGetValue(playerId, out var hitSource))
                return;

            hitSource.PlayerHitReceivedServer -= OnPlayerHitServer;
            _hitSourcesByPlayerId.Remove(playerId);
        }

        private bool HasCompletePhysicalRoster()
        {
            foreach (var connection in _connectionsByPlayerId.Values)
            {
                if (connection?.identity == null ||
                    FindPort<IRoundWeaponPort>(connection.identity) == null ||
                    FindPort<IRoundHitSource>(connection.identity) == null ||
                    FindPort<IRoundEliminationPort>(connection.identity) == null ||
                    FindPort<IRoundRelocationPort>(connection.identity) == null)
                {
                    return false;
                }
            }

            return true;
        }

        private void RelocatePlayersToStartRoom()
        {
            var startPositions = NetworkManager.startPositions
                .Where(position => position != null)
                .ToArray();
            if (startPositions.Length == 0)
            {
                Debug.LogError(
                    "[NetworkRoundCoordinator] At least one NetworkStartPosition is required to start a Runda.",
                    this);
                return;
            }

            var players = _connectionsByPlayerId
                .OrderBy(pair => pair.Key)
                .ToArray();
            for (var index = 0; index < players.Length; index++)
            {
                var connection = players[index].Value;
                var relocation = FindPort<IRoundRelocationPort>(connection?.identity);
                var startPosition = startPositions[index % startPositions.Length];
                var reuseLayer = index / startPositions.Length;
                var reuseDirection = index % 2 == 0 ? 1f : -1f;
                var destination = startPosition.position +
                    startPosition.right * (reuseLayer * ReusedStartPositionOffset * reuseDirection);
                if (relocation == null ||
                    !relocation.RelocateToStartRoomServer(
                        destination,
                        startPosition.rotation))
                {
                    Debug.LogError(
                        $"[NetworkRoundCoordinator] Failed to relocate player {players[index].Key} for the new Runda.",
                        this);
                }
            }
        }

        private void ConfigureRoundWeapons()
        {
            foreach (var pair in _connectionsByPlayerId)
            {
                var playerId = new PlayerId(pair.Key);
                var view = _engine.ViewFor(playerId);
                var weapon = FindPort<IRoundWeaponPort>(pair.Value?.identity);
                if (view == null || weapon == null)
                    continue;

                bool isDetective = view.Role == RoundRole.Detective;
                weapon.SetWeaponAuthorizationServer(isDetective);
                if (isDetective)
                    weapon.TryEquipWeaponServer();
            }
        }

        private void OnPlayerHitServer(RoundPlayerHit hit)
        {
            if (!NetworkServer.active || hit.Shooter == null || hit.Target == null)
                return;

            var shooterConnection = hit.Shooter.connectionToClient;
            var targetConnection = hit.Target.connectionToClient;
            if (shooterConnection == null || targetConnection == null)
                return;

            var shooterId = ConnectionToPlayerId(shooterConnection);
            var targetId = ConnectionToPlayerId(targetConnection);
            if (!_connectionsByPlayerId.TryGetValue(shooterId.Value, out var registeredShooter) ||
                !_connectionsByPlayerId.TryGetValue(targetId.Value, out var registeredTarget) ||
                !ReferenceEquals(registeredShooter, shooterConnection) ||
                !ReferenceEquals(registeredTarget, targetConnection) ||
                !ReferenceEquals(shooterConnection.identity, hit.Shooter) ||
                !ReferenceEquals(targetConnection.identity, hit.Target))
            {
                return;
            }

            var shooterView = _engine.ViewFor(shooterId);
            var targetView = _engine.ViewFor(targetId);
            var weapon = FindPort<IRoundWeaponPort>(hit.Shooter);
            var elimination = FindPort<IRoundEliminationPort>(hit.Target);
            if (shooterView == null || targetView == null || weapon == null || elimination == null)
                return;

            if (!RoundPhysicalRules.CanSubmitExecutionHit(
                    _phase,
                    shooterView.Role,
                    weapon.IsWeaponAuthorized,
                    weapon.HasWeapon,
                    targetView.Role,
                    elimination.IsEliminated))
            {
                return;
            }

            if (Submit(null, new RoundCommand.Execute(targetId)))
            {
                ServerExecutionAccepted?.Invoke(hit.Target);
                elimination.TryEliminateServer();
            }
        }

        private static T FindPort<T>(NetworkIdentity identity) where T : class
        {
            if (identity == null)
                return null;

            foreach (var component in identity.GetComponents<MonoBehaviour>())
            {
                if (component is T port)
                    return port;
            }

            return null;
        }

        private void UpdateRoundTimer()
        {
            if (ShouldEndPreparation(
                    _phase,
                    IsDeveloperRoundUnlimited,
                    NetworkTime.time,
                    _preparationDeadline))
            {
                Submit(null, new RoundCommand.EndPreparation());
                return;
            }

            if (!ShouldExpireRound(
                    _phase,
                    IsDeveloperRoundUnlimited,
                    NetworkTime.time,
                    _roundDeadline))
                return;

            Submit(null, new RoundCommand.TimeExpired());
        }

        public static bool ShouldExpireRound(
            RoundPhase phase,
            bool developerRoundUnlimited,
            double now,
            double deadline) =>
            phase == RoundPhase.Round &&
            !developerRoundUnlimited &&
            now >= deadline;

        /// <summary>Missing Gotowość never blocks the Runda: the deadline always ends Przygotowanie.</summary>
        public static bool ShouldEndPreparation(
            RoundPhase phase,
            bool developerRoundUnlimited,
            double now,
            double deadline) =>
            phase == RoundPhase.Preparation &&
            !developerRoundUnlimited &&
            deadline > 0d &&
            now >= deadline;

        /// <summary>All-ready only shortens the deadline; it is never extended.</summary>
        public static double ShortenedPreparationDeadline(
            double deadline,
            double now,
            double allReadySeconds) =>
            deadline <= 0d ? deadline : Math.Min(deadline, now + allReadySeconds);

        private void DeliverAllViews()
        {
            foreach (var pair in _connectionsByPlayerId)
                DeliverView(new PlayerId(pair.Key), pair.Value);
        }

        private void DeliverView(PlayerId recipient, NetworkConnectionToClient connection)
        {
            if (connection == null || !connection.isAuthenticated)
                return;

            var view = _engine.ViewFor(recipient);
            if (view == null)
                return;

            connection.Send(RoundViewMessage.FromView(
                view,
                _phase == RoundPhase.Round && !IsDeveloperRoundUnlimited
                    ? _roundDeadline
                    : 0d,
                _phase == RoundPhase.Preparation && !IsDeveloperRoundUnlimited
                    ? _preparationDeadline
                    : 0d));
        }

        private void BroadcastLobbyState(bool force = false)
        {
            int playerCount = ConnectedPlayerCount;
            if (!force && playerCount == _lastBroadcastLobbyPlayerCount)
                return;

            var message = new RoundLobbyStateMessage { PlayerCount = playerCount };
            foreach (var connection in _connectionsByPlayerId.Values)
            {
                if (connection != null &&
                    connection.isAuthenticated &&
                    !ReferenceEquals(connection, NetworkServer.localConnection))
                {
                    connection.Send(message);
                }
            }

            _publicLobbyPlayerCount = playerCount;
            _lastBroadcastLobbyPlayerCount = playerCount;
        }

        private static PlayerId ConnectionToPlayerId(NetworkConnectionToClient connection)
        {
            // This is intentionally the only Mirror connection -> PlayerId map.
            // Steam can later replace connectionId with an authenticated SteamID-derived id here.
            return new PlayerId(connection.connectionId);
        }

        private bool TryGetPhysicalActor(NetworkIdentity actor, out PlayerId playerId)
        {
            playerId = default;
            if (!NetworkServer.active || actor?.connectionToClient == null)
                return false;

            var connection = actor.connectionToClient;
            playerId = ConnectionToPlayerId(connection);
            return _connectionsByPlayerId.TryGetValue(playerId.Value, out var registered) &&
                   ReferenceEquals(registered, connection) &&
                   ReferenceEquals(connection.identity, actor);
        }

        private static bool IsHost(NetworkConnectionToClient sender) =>
            NetworkServer.activeHost && ReferenceEquals(sender, NetworkServer.localConnection);

        private static void Reject(NetworkConnectionToClient sender, string reason)
        {
            if (sender != null && sender.isAuthenticated)
                sender.Send(new RoundIntentRejectedMessage { Reason = reason });
        }

        private void OnClientView(RoundViewMessage message)
        {
            CurrentView = message.ToView();
            CurrentRoundEndsAtNetworkTime = message.RoundEndsAtNetworkTime;
            CurrentPreparationEndsAtNetworkTime = message.PreparationEndsAtNetworkTime;
            ViewReceived?.Invoke(CurrentView, CurrentRoundEndsAtNetworkTime);
        }

        private void OnClientIntentRejected(RoundIntentRejectedMessage message)
        {
            IntentRejected?.Invoke(message.Reason);
        }

        private void OnClientLobbyReset(RoundLobbyResetMessage _)
        {
            CurrentView = null;
            CurrentRoundEndsAtNetworkTime = 0d;
            CurrentPreparationEndsAtNetworkTime = 0d;
            LobbyResetReceived?.Invoke();
        }

        private void OnClientLobbyState(RoundLobbyStateMessage message)
        {
            _publicLobbyPlayerCount = Math.Max(0, message.PlayerCount);
        }

        private void ResetServerRuntime()
        {
            foreach (var playerId in _hitSourcesByPlayerId.Keys.ToArray())
                UnbindHitSource(playerId);

            _serverHandlerRegistered = false;
            _connectionsByPlayerId.Clear();
            _rejectedLateJoiners.Clear();
            _engine = new RoundEngine();
            _phase = RoundPhase.Lobby;
            _developerPlan = null;
            _hostAllowsSecretObjective = true;
            _publicLobbyPlayerCount = 0;
            _lastBroadcastLobbyPlayerCount = -1;
            _roundDeadline = 0d;
            _roundStartedAtNetworkTime = 0d;
            _preparationDeadline = 0d;
        }

        private PlayerId ResolveDeveloperIncidentAuthor(PlayerId physicalPlayer)
        {
            if (!DeveloperToolsAvailable
                || _developerPlan?.Scenario != RoundDeveloperScenario.DetectiveIncidents
                || physicalPlayer != _developerPlan.ControlledPlayer
                || _engine.ViewFor(physicalPlayer)?.Role != RoundRole.Detective)
            {
                return physicalPlayer;
            }

            return _developerPlan.Players.First(player =>
                player != physicalPlayer && _engine.ViewFor(player)?.Role != RoundRole.Detective);
        }

        private bool SubmitDeveloper(RoundCommand command, out string rejectionReason)
        {
            if (Submit(null, command))
            {
                rejectionReason = null;
                return true;
            }

            return RejectDeveloper("RoundEngine rejected the developer action.", out rejectionReason);
        }

        private static bool RejectDeveloper(string reason, out string rejectionReason)
        {
            rejectionReason = reason;
            return false;
        }
    }
}
