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

        [SerializeField, Min(1f)]
        [Tooltip("Limit Rundy measured authoritatively by the server, in seconds.")]
        private float roundLimitSeconds = 600f;

        [SerializeField, Min(0.1f)]
        [Tooltip("How often a fresh targeted view carries updated remaining time.")]
        private float timerDeliveryInterval = 1f;

        private readonly Dictionary<int, NetworkConnectionToClient> _connectionsByPlayerId =
            new Dictionary<int, NetworkConnectionToClient>();
        private readonly HashSet<int> _rejectedLateJoiners = new HashSet<int>();

        private RoundEngine _engine = new RoundEngine();
        private RoundPhase _phase = RoundPhase.Lobby;
        private bool _serverHandlerRegistered;
        private bool _clientHandlersRegistered;
        private double _roundDeadline;
        private double _nextTimerDelivery;

        public PlayerRoundView CurrentView { get; private set; }
        public float CurrentRemainingSeconds { get; private set; }

        public event Action<PlayerRoundView, float> ViewReceived;
        public event Action<string> IntentRejected;

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
            }

            ResetServerRuntime();
            _clientHandlersRegistered = false;
            CurrentView = null;
            CurrentRemainingSeconds = 0f;
        }

        public void RequestStartRound()
        {
            SendIntent(RoundIntentMessage.StartRound());
        }

        public void RequestEndPreparation()
        {
            SendIntent(RoundIntentMessage.EndPreparation());
        }

        public void RequestExecution(PlayerId target)
        {
            SendIntent(RoundIntentMessage.Execute(target));
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
                _connectionsByPlayerId.Remove(disconnectedId);
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

                case RoundIntentKind.Execute:
                    var senderView = _engine.ViewFor(senderId);
                    if (senderView == null
                        || senderView.Phase != RoundPhase.Round
                        || senderView.Role != RoundRole.Detective)
                    {
                        Reject(sender, "Only the Detektyw may perform an Egzekucja during the Runda.");
                        return;
                    }
                    Submit(sender, new RoundCommand.Execute(new PlayerId(message.TargetPlayerId)));
                    break;

                default:
                    Reject(sender, "Unknown Runda intention.");
                    break;
            }
        }

        private void StartRound(NetworkConnectionToClient sender)
        {
            if (caseAsset == null)
            {
                Reject(sender, "The host has not selected a CaseAsset.");
                return;
            }

            CaseDefinition definition;
            try
            {
                definition = caseAsset.ToDefinition();
            }
            catch (Exception exception)
            {
                Debug.LogError($"[NetworkRoundCoordinator] Selected CaseAsset is invalid: {exception}", caseAsset);
                Reject(sender, "The selected CaseAsset is invalid.");
                return;
            }

            var players = _connectionsByPlayerId.Keys
                .OrderBy(id => id)
                .Select(id => new PlayerId(id))
                .ToArray();
            var seed = UnityEngine.Random.Range(int.MinValue, int.MaxValue);
            Submit(sender, new RoundCommand.StartRound(definition, players, seed));
        }

        private void Submit(NetworkConnectionToClient sender, RoundCommand command)
        {
            var transition = _engine.Handle(command);
            if (!transition.Accepted)
            {
                if (sender != null)
                    Reject(sender, transition.RejectionReason);
                return;
            }

            _phase = transition.State.Phase;
            if (command is RoundCommand.EndPreparation)
            {
                _roundDeadline = NetworkTime.time + roundLimitSeconds;
                _nextTimerDelivery = NetworkTime.time;
            }
            else if (_phase == RoundPhase.Finished)
            {
                _roundDeadline = 0d;
                _nextTimerDelivery = 0d;
            }

            DeliverAllViews();
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

            if (now >= _nextTimerDelivery)
            {
                DeliverAllViews();
                _nextTimerDelivery = now + timerDeliveryInterval;
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

            connection.Send(RoundViewMessage.FromView(view, RemainingSeconds()));
        }

        private float RemainingSeconds()
        {
            if (_phase != RoundPhase.Round)
                return 0f;
            return (float)Math.Max(0d, _roundDeadline - NetworkTime.time);
        }

        private static PlayerId ConnectionToPlayerId(NetworkConnectionToClient connection)
        {
            // This is intentionally the only Mirror connection -> PlayerId map.
            // Steam can later replace connectionId with an authenticated SteamID-derived id here.
            return new PlayerId(connection.connectionId);
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
            CurrentRemainingSeconds = message.RemainingSeconds;
            ViewReceived?.Invoke(CurrentView, CurrentRemainingSeconds);
        }

        private void OnClientIntentRejected(RoundIntentRejectedMessage message)
        {
            IntentRejected?.Invoke(message.Reason);
        }

        private void ResetServerRuntime()
        {
            _serverHandlerRegistered = false;
            _connectionsByPlayerId.Clear();
            _rejectedLateJoiners.Clear();
            _engine = new RoundEngine();
            _phase = RoundPhase.Lobby;
            _roundDeadline = 0d;
            _nextTimerDelivery = 0d;
        }
    }
}
