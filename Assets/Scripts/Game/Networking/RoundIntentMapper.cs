using System;
using InterrogationRoom.Domain;

namespace InterrogationRoom.Networking
{
    public static class RoundLobbyRules
    {
        public static int ResolveSecretObjectiveCount(int playerCount, bool hostAllowsSecretObjective) =>
            playerCount >= 5 && playerCount <= RoundEngine.MaxPlayers && hostAllowsSecretObjective
                ? 1
                : 0;
    }

    /// <summary>
    /// Converts untrusted wire ids into a domain command. Ownership and time
    /// always come from server context, never from the client payload.
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
            rejectionReason = null;

            try
            {
                switch (message.Kind)
                {
                    case RoundIntentKind.AdvancePrivateObjective:
                        command = new RoundCommand.AdvancePrivateObjective(
                            authenticatedSender,
                            new PrivateObjectiveId(message.ObjectiveId),
                            new PrivateObjectiveStepId(message.ObjectiveStepId));
                        break;

                    case RoundIntentKind.RegisterIncident:
                        if (!Enum.IsDefined(typeof(IncidentKind), message.IncidentKind))
                            throw new ArgumentOutOfRangeException(nameof(message.IncidentKind));
                        command = new RoundCommand.RegisterIncident(
                            authenticatedSender,
                            new IncidentId(message.IncidentId),
                            message.IncidentKind,
                            new IncidentEffectId(message.EffectId),
                            new IncidentLocationId(message.LocationId),
                            serverTimestamp,
                            message.HasObjectiveStepReference
                                ? new PrivateObjectiveStepReference(
                                    new PrivateObjectiveId(message.ObjectiveId),
                                    new PrivateObjectiveStepId(message.ObjectiveStepId))
                                : null);
                        break;

                    case RoundIntentKind.DiscoverQuietIncident:
                        command = new RoundCommand.DiscoverQuietIncident(
                            authenticatedSender,
                            new IncidentId(message.IncidentId),
                            serverTimestamp);
                        break;

                    case RoundIntentKind.AcquireAlibiClue:
                        if (!Enum.IsDefined(typeof(IncidentKind), message.IncidentKind))
                            throw new ArgumentOutOfRangeException(nameof(message.IncidentKind));
                        command = new RoundCommand.AcquireAlibiClue(
                            authenticatedSender,
                            new AlibiClueId(message.AlibiClueId),
                            new IncidentId(message.IncidentId),
                            message.IncidentKind,
                            new IncidentEffectId(message.EffectId),
                            new IncidentLocationId(message.LocationId),
                            serverTimestamp);
                        break;

                    case RoundIntentKind.PrepareEscape:
                        command = new RoundCommand.PrepareEscape(
                            authenticatedSender,
                            new EscapePlanId(message.EscapePlanId),
                            new EscapeStepId(message.EscapeStepId));
                        break;

                    case RoundIntentKind.BeginEscape:
                        command = new RoundCommand.BeginEscape(
                            authenticatedSender,
                            new EscapePlanId(message.EscapePlanId),
                            new EscapeExitId(message.EscapeExitId),
                            new IncidentId(message.IncidentId),
                            serverTimestamp);
                        break;

                    case RoundIntentKind.InterruptEscape:
                        command = new RoundCommand.InterruptEscape(
                            authenticatedSender,
                            new EscapePlanId(message.EscapePlanId),
                            new EscapeExitId(message.EscapeExitId));
                        break;

                    case RoundIntentKind.CompleteEscape:
                        command = new RoundCommand.CompleteEscape(
                            authenticatedSender,
                            new EscapePlanId(message.EscapePlanId),
                            new EscapeExitId(message.EscapeExitId));
                        break;

                    default:
                        rejectionReason = "Unsupported player Runda intention.";
                        return false;
                }
            }
            catch (ArgumentException)
            {
                rejectionReason = "Runda intention contains an invalid stable id or enum value.";
                return false;
            }

            return true;
        }
    }
}
