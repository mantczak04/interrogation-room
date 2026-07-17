using InterrogationRoom.Domain;

namespace InterrogationRoom.Networking
{
    public static class RoundLobbyRules
    {
        public static int ResolveSecretObjectiveCount(int playerCount, bool hostAllowsSecretObjective) =>
            playerCount >= RoundEngine.MinPlayersForSecretObjective
            && playerCount <= RoundEngine.MaxPlayers
            && hostAllowsSecretObjective
                ? 1
                : 0;
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
