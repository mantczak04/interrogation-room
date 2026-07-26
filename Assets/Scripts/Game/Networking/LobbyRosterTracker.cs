using System;
using System.Collections.Generic;
using System.Linq;
using InterrogationRoom.Domain;
using Mirror;

namespace InterrogationRoom.Networking
{
    /// <summary>
    /// Host-side lobby profile and ready-state collaborator. It never maps
    /// Mirror connections to domain PlayerId values; the coordinator remains
    /// the sole owner of that boundary.
    /// </summary>
    public sealed class LobbyRosterTracker
    {
        private readonly Dictionary<int, string> displayNamesByPlayerId =
            new Dictionary<int, string>();
        private readonly HashSet<int> readyPlayerIds = new HashSet<int>();
        private readonly Dictionary<int, uint> networkIdentityNetIdsByPlayerId =
            new Dictionary<int, uint>();

        public bool SetDisplayName(int playerId, string displayName)
        {
            if (displayNamesByPlayerId.TryGetValue(playerId, out string current) &&
                string.Equals(current, displayName, StringComparison.Ordinal))
            {
                return false;
            }

            displayNamesByPlayerId[playerId] = displayName;
            return true;
        }

        public bool SetReady(int playerId, bool ready) =>
            ready ? readyPlayerIds.Add(playerId) : readyPlayerIds.Remove(playerId);

        public bool AreAllReady(IEnumerable<int> playerIds)
        {
            if (playerIds == null)
                return false;

            int count = 0;
            foreach (int playerId in playerIds)
            {
                count++;
                if (!readyPlayerIds.Contains(playerId))
                    return false;
            }

            return count > 0;
        }

        public bool BindNetworkIdentity(int playerId, uint networkIdentityNetId)
        {
            if (networkIdentityNetIdsByPlayerId.TryGetValue(playerId, out uint current) &&
                current == networkIdentityNetId)
            {
                return false;
            }

            networkIdentityNetIdsByPlayerId[playerId] = networkIdentityNetId;
            return true;
        }

        public void Disconnect(int playerId, bool preserveDisplayName)
        {
            networkIdentityNetIdsByPlayerId.Remove(playerId);
            readyPlayerIds.Remove(playerId);
            if (!preserveDisplayName)
                displayNamesByPlayerId.Remove(playerId);
        }

        public void ClearReady() => readyPlayerIds.Clear();

        public void Clear()
        {
            displayNamesByPlayerId.Clear();
            readyPlayerIds.Clear();
            networkIdentityNetIdsByPlayerId.Clear();
        }

        public IReadOnlyList<VoiceRosterEntry> BuildVoiceRoster() =>
            networkIdentityNetIdsByPlayerId
                .Where(entry => entry.Value != 0u)
                .OrderBy(entry => entry.Key)
                .Select(entry => new VoiceRosterEntry(
                    entry.Value,
                    displayNamesByPlayerId.TryGetValue(entry.Key, out string displayName)
                        ? displayName
                        : $"Gracz {entry.Key + 1}"))
                .ToArray();

        public RoundLobbyPlayerMessage[] BuildMessages(
            IReadOnlyDictionary<int, NetworkConnectionToClient> connectionsByPlayerId,
            int simulatedPlayerCount,
            bool activeHost,
            NetworkConnectionToClient localConnection)
        {
            var players = new List<RoundLobbyPlayerMessage>(RoundEngine.MaxPlayers);
            foreach (KeyValuePair<int, NetworkConnectionToClient> entry in
                     connectionsByPlayerId.OrderBy(entry => entry.Key))
            {
                NetworkConnectionToClient connection = entry.Value;
                string fallback = $"Gracz {entry.Key + 1}";
                string displayName = displayNamesByPlayerId.TryGetValue(
                    entry.Key,
                    out string knownName)
                    ? knownName
                    : fallback;
                players.Add(new RoundLobbyPlayerMessage
                {
                    PlayerId = entry.Key,
                    NetworkIdentityNetId = networkIdentityNetIdsByPlayerId.TryGetValue(
                        entry.Key,
                        out uint networkIdentityNetId)
                        ? networkIdentityNetId
                        : connection?.identity != null ? connection.identity.netId : 0u,
                    DisplayName =
                        LobbyPlayerPresentation.NormalizeDisplayName(displayName, fallback),
                    IsHost = activeHost && ReferenceEquals(connection, localConnection),
                    IsSimulated = false,
                    IsReady = readyPlayerIds.Contains(entry.Key)
                });
            }

            foreach (LobbyPlayerInfo simulated in
                     LobbyPlayerPresentation.CreateSimulatedPlayers(simulatedPlayerCount))
            {
                players.Add(new RoundLobbyPlayerMessage
                {
                    PlayerId = simulated.PlayerId,
                    NetworkIdentityNetId = 0u,
                    DisplayName = simulated.DisplayName,
                    IsHost = false,
                    IsSimulated = true,
                    IsReady = true
                });
            }

            return players.Take(RoundEngine.MaxPlayers).ToArray();
        }
    }
}
