using InterrogationRoom.Domain;

namespace InterrogationRoom.Networking
{
    public static class RoundLobbyRules
    {
        public const int DefaultRoundLimitMinutes = 10;

        public static int ResolveSecretObjectiveCount(int playerCount, bool hostAllowsSecretObjective) =>
            playerCount >= RoundEngine.MinPlayersForSecretObjective
            && playerCount <= RoundEngine.MaxPlayers
            && hostAllowsSecretObjective
                ? 1
                : 0;

        public static bool IsRoundLimitMinutesAllowed(int minutes) =>
            minutes == 5 || minutes == 10 || minutes == 15 || minutes == 20;

        public static bool CanSetRoundLimit(bool isHost, RoundPhase phase, int minutes) =>
            isHost && phase == RoundPhase.Lobby && IsRoundLimitMinutesAllowed(minutes);

        public static double ToRoundLimitSeconds(int minutes)
        {
            if (!IsRoundLimitMinutesAllowed(minutes))
                throw new System.ArgumentOutOfRangeException(nameof(minutes));

            return minutes * 60d;
        }
    }

    /// <summary>
    /// Guards the server boundary for untrusted client intentions. Physical
    /// gameplay results are submitted only by server-side world adapters.
    /// </summary>
    public static class RoundIntentMapper
    {
        public static bool TryMap(
            RoundIntentMessage message,
            PlayerId authenticatedSender,
            IncidentTimestamp serverTimestamp,
            out RoundCommand command,
            out string rejectionReason)
        {
            command = null;
            switch (message.Kind)
            {
                // Gotowość is the only client-authored Runda intention: it is
                // phase-gated and idempotence-checked by RoundEngine and its
                // author is always the authenticated sender connection.
                case RoundIntentKind.PlayerReady:
                    command = new RoundCommand.MarkPlayerReady(authenticatedSender);
                    rejectionReason = null;
                    return true;

                case RoundIntentKind.AdvancePrivateObjective:
                case RoundIntentKind.RegisterIncident:
                case RoundIntentKind.DiscoverQuietIncident:
                case RoundIntentKind.AcquireAlibiClue:
                case RoundIntentKind.PrepareEscape:
                case RoundIntentKind.BeginEscape:
                case RoundIntentKind.InterruptEscape:
                case RoundIntentKind.CompleteEscape:
                    rejectionReason =
                        "Physical Runda actions are server-authoritative and cannot be submitted by a client.";
                    return false;

                default:
                    rejectionReason = "Unsupported player Runda intention.";
                    return false;
            }
        }
    }
}
