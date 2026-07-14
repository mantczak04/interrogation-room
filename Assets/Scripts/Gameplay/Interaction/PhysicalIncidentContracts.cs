using System;
using System.Collections.Generic;
using Mirror;

namespace InterrogationRoom.Gameplay.Interaction
{
    public enum PhysicalIncidentKind
    {
        Loud,
        Quiet
    }

    /// <summary>
    /// Domain-neutral result of a visible world change. Area A maps these
    /// stable ids and the authenticated actor to the A1/A2 Runda intention.
    /// </summary>
    public readonly struct PhysicalIncidentSignal
    {
        public PhysicalIncidentSignal(
            string incidentId,
            PhysicalIncidentKind kind,
            string effectId,
            string locationId,
            string objectiveStepId,
            NetworkIdentity actor,
            NetworkIdentity source)
        {
            IncidentId = incidentId;
            Kind = kind;
            EffectId = effectId;
            LocationId = locationId;
            ObjectiveStepId = objectiveStepId;
            Actor = actor;
            Source = source;
        }

        public string IncidentId { get; }
        public PhysicalIncidentKind Kind { get; }
        public string EffectId { get; }
        public string LocationId { get; }
        public string ObjectiveStepId { get; }
        public NetworkIdentity Actor { get; }
        public NetworkIdentity Source { get; }
    }

    public readonly struct QuietIncidentDiscoveryCandidate
    {
        public QuietIncidentDiscoveryCandidate(
            string incidentId,
            NetworkIdentity viewer,
            NetworkIdentity source)
        {
            IncidentId = incidentId;
            Viewer = viewer;
            Source = source;
        }

        public string IncidentId { get; }
        public NetworkIdentity Viewer { get; }
        public NetworkIdentity Source { get; }
    }

    public interface IPhysicalIncidentSource
    {
        IReadOnlyList<PhysicalIncidentSignal> RaisedIncidentsServer { get; }

        event Action<PhysicalIncidentSignal> IncidentRaisedServer;
    }
}
