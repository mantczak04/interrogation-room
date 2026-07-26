using System;
using System.Collections.Generic;
using System.Text;

namespace InterrogationRoom.Networking
{
    public readonly struct LobbyPlayerInfo
    {
        public int PlayerId { get; }
        public uint NetworkIdentityNetId { get; }
        public string DisplayName { get; }
        public bool IsHost { get; }
        public bool IsSimulated { get; }
        public bool IsReady { get; }

        public LobbyPlayerInfo(
            int playerId,
            uint networkIdentityNetId,
            string displayName,
            bool isHost,
            bool isSimulated,
            bool isReady)
        {
            PlayerId = playerId;
            NetworkIdentityNetId = networkIdentityNetId;
            DisplayName = displayName ?? string.Empty;
            IsHost = isHost;
            IsSimulated = isSimulated;
            IsReady = isReady;
        }
    }

    public readonly struct VoiceRosterEntry
    {
        public uint NetworkIdentityNetId { get; }
        public string DisplayName { get; }

        public VoiceRosterEntry(uint networkIdentityNetId, string displayName)
        {
            NetworkIdentityNetId = networkIdentityNetId;
            DisplayName = displayName ?? string.Empty;
        }
    }

    public static class VoiceRosterView
    {
        public static IReadOnlyList<VoiceRosterEntry> Build(
            IReadOnlyList<LobbyPlayerInfo> players,
            Func<uint, bool> isConnected)
        {
            if (players == null || isConnected == null)
                return Array.Empty<VoiceRosterEntry>();

            var entries = new List<VoiceRosterEntry>(players.Count);
            foreach (LobbyPlayerInfo player in players)
            {
                if (!player.IsSimulated &&
                    player.NetworkIdentityNetId != 0u &&
                    isConnected(player.NetworkIdentityNetId))
                {
                    entries.Add(new VoiceRosterEntry(
                        player.NetworkIdentityNetId,
                        player.DisplayName));
                }
            }

            return entries;
        }
    }

    public static class LobbyPlayerPresentation
    {
        public const int MaxDisplayNameLength = 16;

        private static readonly string[] SimulatedNames =
        {
            "Alicja Żur",
            "Łukasz Śledź",
            "Mikołaj Ćwiek",
            "Zośka Bąk",
            "Grzegorz Źrebak",
            "Iga Gęś",
            "Paweł Wróbel"
        };

        public static string NormalizeDisplayName(string value, string fallback)
        {
            var builder = new StringBuilder(MaxDisplayNameLength);
            bool previousWasWhitespace = false;
            string source = string.IsNullOrWhiteSpace(value) ? fallback : value;
            source ??= "Gracz";

            foreach (char character in source.Trim())
            {
                bool whitespace = char.IsWhiteSpace(character);
                if (whitespace && previousWasWhitespace)
                    continue;
                if (char.IsControl(character) && !whitespace)
                    continue;

                builder.Append(whitespace ? ' ' : character);
                previousWasWhitespace = whitespace;
                if (builder.Length >= MaxDisplayNameLength)
                    break;
            }

            string normalized = builder.ToString().Trim();
            return string.IsNullOrWhiteSpace(normalized) ? "Gracz" : normalized;
        }

        public static IReadOnlyList<LobbyPlayerInfo> CreateSimulatedPlayers(int count)
        {
            int boundedCount = Math.Max(0, Math.Min(count, SimulatedNames.Length));
            var players = new LobbyPlayerInfo[boundedCount];
            for (int index = 0; index < boundedCount; index++)
            {
                players[index] = new LobbyPlayerInfo(
                    playerId: -1 - index,
                    networkIdentityNetId: 0,
                    displayName: SimulatedNames[index],
                    isHost: false,
                    isSimulated: true,
                    isReady: true);
            }

            return players;
        }
    }
}
