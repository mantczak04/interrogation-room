using System;
using Mirror;

namespace InterrogationRoom.Gameplay.Interaction
{
    public readonly struct PhysicalAlibiClueSignal
    {
        public PhysicalAlibiClueSignal(
            string clueId,
            string incidentId,
            PhysicalIncidentKind incidentKind,
            string effectId,
            string locationId,
            NetworkIdentity actor,
            NetworkIdentity source)
        {
            ClueId = clueId;
            IncidentId = incidentId;
            IncidentKind = incidentKind;
            EffectId = effectId;
            LocationId = locationId;
            Actor = actor;
            Source = source;
        }

        public string ClueId { get; }
        public string IncidentId { get; }
        public PhysicalIncidentKind IncidentKind { get; }
        public string EffectId { get; }
        public string LocationId { get; }
        public NetworkIdentity Actor { get; }
        public NetworkIdentity Source { get; }
    }

    public readonly struct PhysicalEscapeAttemptStarted
    {
        public PhysicalEscapeAttemptStarted(
            string planId,
            string exitId,
            string incidentId,
            string locationId,
            NetworkIdentity actor,
            NetworkIdentity source)
        {
            PlanId = planId;
            ExitId = exitId;
            IncidentId = incidentId;
            LocationId = locationId;
            Actor = actor;
            Source = source;
        }

        public string PlanId { get; }
        public string ExitId { get; }
        public string IncidentId { get; }
        public string LocationId { get; }
        public NetworkIdentity Actor { get; }
        public NetworkIdentity Source { get; }
    }

    public readonly struct PhysicalEscapeAttemptInterrupted
    {
        public PhysicalEscapeAttemptInterrupted(
            string planId,
            string exitId,
            NetworkIdentity actor,
            TimedInteractionCancellationReason reason)
        {
            PlanId = planId;
            ExitId = exitId;
            Actor = actor;
            Reason = reason;
        }

        public string PlanId { get; }
        public string ExitId { get; }
        public NetworkIdentity Actor { get; }
        public TimedInteractionCancellationReason Reason { get; }
    }

    public readonly struct PhysicalEscapeAttemptCompleted
    {
        public PhysicalEscapeAttemptCompleted(
            string planId,
            string exitId,
            NetworkIdentity actor)
        {
            PlanId = planId;
            ExitId = exitId;
            Actor = actor;
        }

        public string PlanId { get; }
        public string ExitId { get; }
        public NetworkIdentity Actor { get; }
    }
}
