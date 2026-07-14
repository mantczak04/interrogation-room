using System;
using System.Collections.Generic;
using System.Linq;
using InterrogationRoom.Content;
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
        [SerializeField]
        [Tooltip("Sprawa converted to an immutable CaseDefinition when the host starts the Runda.")]
        private CaseAsset caseAsset;

        [SerializeField]
        [Tooltip("Additional hand-authored Sprawy available to the host in the lobby.")]
        private List<CaseAsset> additionalCases = new List<CaseAsset>();

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
        private double _roundDeadline;
        private double _roundStartedAtNetworkTime;

        public PlayerRoundView CurrentView { get; private set; }
        public double CurrentRoundEndsAtNetworkTime { get; private set; }
        public bool IsLocalHost => NetworkServer.activeHost;
        public int ConnectedPlayerCount => _connectionsByPlayerId.Count;
        public int SelectedCaseIndex { get; private set; }
        public bool HostAllowsSecretObjective => _hostAllowsSecretObjective;
        public int EffectiveSecretObjectiveCount => RoundLobbyRules.ResolveSecretObjectiveCount(
            ConnectedPlayerCount,
            _hostAllowsSecretObjective);

        /// <summary>Public lobby data only. Never exposes Alibi facts or hidden-fact flags.</summary>
        public IReadOnlyList<string> AvailableCaseTitles => AvailableCases()
            .Select(asset => string.IsNullOrWhiteSpace(asset.title) ? asset.name : asset.title.Trim())
            .ToArray();

        public event Action<PlayerRoundView, double> ViewReceived;
        public event Action<string> IntentRejected;
        public event Action LobbyResetReceived;
        public event Action ServerRoundReset;
        public event Action<NetworkIdentity> ServerExecutionAccepted;

        private void Awake()
        {
            RoundMessageSerialization.Register();
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
            }

            ResetServerRuntime();
            _clientHandlersRegistered = false;
            CurrentView = null;
            CurrentRoundEndsAtNetworkTime = 0d;
        }

        public void RequestStartRound()
        {
            SendIntent(RoundIntentMessage.StartRound());
        }

        public void RequestEndPreparation()
        {
            SendIntent(RoundIntentMessage.EndPreparation());
        }

        public void RequestReturnToLobby()
        {
            SendIntent(RoundIntentMessage.ReturnToLobby());
        }

        public void RequestAdvancePrivateObjective(
            PrivateObjectiveId objectiveId,
            PrivateObjectiveStepId stepId) =>
            SendIntent(RoundIntentMessage.AdvancePrivateObjective(objectiveId, stepId));

        public void RequestRegisterIncident(
            IncidentId incidentId,
            IncidentKind kind,
            IncidentEffectId effect,
            IncidentLocationId location,
            PrivateObjectiveStepReference objectiveStep = null) =>
            SendIntent(RoundIntentMessage.RegisterIncident(
                incidentId,
                kind,
                effect,
                location,
                objectiveStep));

        public void RequestDiscoverQuietIncident(IncidentId incidentId) =>
            SendIntent(RoundIntentMessage.DiscoverQuietIncident(incidentId));

        public void RequestAcquireAlibiClue(
            AlibiClueId clueId,
            IncidentId incidentId,
            IncidentKind kind,
            IncidentEffectId effect,
            IncidentLocationId location) =>
            SendIntent(RoundIntentMessage.AcquireAlibiClue(
                clueId,
                incidentId,
                kind,
                effect,
                location));

        public void RequestPrepareEscape(EscapePlanId planId, EscapeStepId stepId) =>
            SendIntent(RoundIntentMessage.PrepareEscape(planId, stepId));

        public void RequestBeginEscape(
            EscapePlanId planId,
            EscapeExitId exitId,
            IncidentId incidentId) =>
            SendIntent(RoundIntentMessage.BeginEscape(planId, exitId, incidentId));

        public void RequestInterruptEscape(EscapePlanId planId, EscapeExitId exitId) =>
            SendIntent(RoundIntentMessage.InterruptEscape(planId, exitId));

        public void RequestCompleteEscape(EscapePlanId planId, EscapeExitId exitId) =>
            SendIntent(RoundIntentMessage.CompleteEscape(planId, exitId));

        public bool TrySelectCase(int index)
        {
            var cases = AvailableCases();
            if (!IsLocalHost || _phase != RoundPhase.Lobby || index < 0 || index >= cases.Count)
                return false;

            SelectedCaseIndex = index;
            return true;
        }

        public bool TrySetSecretObjectiveEnabled(bool enabled)
        {
            if (!IsLocalHost || _phase != RoundPhase.Lobby)
                return false;

            _hostAllowsSecretObjective = enabled;
            return true;
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
            if (!TryGetPhysicalActor(actor, out var playerId))
                return false;

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
                return;
            }

            if (_clientHandlersRegistered)
                return;

            NetworkClient.RegisterHandler<RoundViewMessage>(OnClientView);
            NetworkClient.RegisterHandler<RoundIntentRejectedMessage>(OnClientIntentRejected);
            NetworkClient.RegisterHandler<RoundLobbyResetMessage>(OnClientLobbyReset);
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
            if (cases.Count == 0 || SelectedCaseIndex < 0 || SelectedCaseIndex >= cases.Count)
            {
                Reject(sender, "The host has not selected a CaseAsset.");
                return;
            }

            var selectedCase = cases[SelectedCaseIndex];

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

            var players = _connectionsByPlayerId.Keys
                .OrderBy(id => id)
                .Select(id => new PlayerId(id))
                .ToArray();
            if (!HasCompletePhysicalRoster())
            {
                Reject(sender, "Every player must be spawned with weapon, hit, and elimination components before the Runda starts.");
                return;
            }

            var seed = UnityEngine.Random.Range(int.MinValue, int.MaxValue);
            Submit(sender, new RoundCommand.StartRound(
                definition,
                players,
                seed,
                EffectiveSecretObjectiveCount));
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
            _hostAllowsSecretObjective = true;
            _roundDeadline = 0d;
            _roundStartedAtNetworkTime = 0d;
            ServerRoundReset?.Invoke();

            foreach (var connection in _connectionsByPlayerId.Values)
            {
                if (connection != null && connection.isAuthenticated)
                    connection.Send(new RoundLobbyResetMessage());
            }
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

            SelectedCaseIndex = Mathf.Clamp(SelectedCaseIndex, 0, cases.Count - 1);
        }

        private bool Submit(NetworkConnectionToClient sender, RoundCommand command)
        {
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
                ConfigureRoundWeapons();
            }
            else if (command is RoundCommand.EndPreparation)
            {
                _roundStartedAtNetworkTime = NetworkTime.time;
                _roundDeadline = _roundStartedAtNetworkTime + roundLimitSeconds;
            }
            else if (_phase == RoundPhase.Finished)
            {
                _roundDeadline = 0d;
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
                    FindPort<IRoundEliminationPort>(connection.identity) == null)
                {
                    return false;
                }
            }

            return true;
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
            if (_phase != RoundPhase.Round)
                return;

            var now = NetworkTime.time;
            if (now >= _roundDeadline)
            {
                Submit(null, new RoundCommand.TimeExpired());
                return;
            }

        }

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
                _phase == RoundPhase.Round ? _roundDeadline : 0d));
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
            LobbyResetReceived?.Invoke();
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
            _hostAllowsSecretObjective = true;
            _roundDeadline = 0d;
            _roundStartedAtNetworkTime = 0d;
        }
    }
}
