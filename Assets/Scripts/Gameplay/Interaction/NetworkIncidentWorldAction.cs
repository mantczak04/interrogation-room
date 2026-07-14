using System;
using System.Collections.Generic;
using System.Globalization;
using Mirror;
using UnityEngine;

namespace InterrogationRoom.Gameplay.Interaction
{
    public sealed class NetworkIncidentWorldAction : NetworkObjectiveWorldAction, IPhysicalIncidentSource
    {
        [Header("A2 stable ids")]
        [SerializeField] private string incidentIdPrefix = "physical-incident";
        [SerializeField] private PhysicalIncidentKind incidentKind = PhysicalIncidentKind.Quiet;
        [SerializeField] private string effectId = "changed-world-state";
        [SerializeField] private string locationId = "unknown-location";
        [SerializeField] private string objectiveStepId = string.Empty;

        private readonly List<PhysicalIncidentSignal> raisedIncidents =
            new List<PhysicalIncidentSignal>();

        public PhysicalIncidentKind IncidentKind => incidentKind;
        public string IncidentIdPrefix => incidentIdPrefix ?? string.Empty;
        public string EffectId => effectId ?? string.Empty;
        public string LocationId => locationId ?? string.Empty;
        public string ObjectiveStepId => objectiveStepId ?? string.Empty;
        public bool UsesIncidentForObjectiveProgress => !string.IsNullOrWhiteSpace(objectiveStepId);
        public int RaisedIncidentCount => raisedIncidents.Count;
        public string LastIncidentId => raisedIncidents.Count == 0
            ? string.Empty
            : raisedIncidents[raisedIncidents.Count - 1].IncidentId;
        public string LastEffectId => raisedIncidents.Count == 0
            ? string.Empty
            : raisedIncidents[raisedIncidents.Count - 1].EffectId;
        public string LastLocationId => raisedIncidents.Count == 0
            ? string.Empty
            : raisedIncidents[raisedIncidents.Count - 1].LocationId;

        public IReadOnlyList<PhysicalIncidentSignal> RaisedIncidentsServer => raisedIncidents;

        public event Action<PhysicalIncidentSignal> IncidentRaisedServer;

        protected override bool CanApplyWorldEffectServer(NetworkIdentity interactor, int nextRevision)
        {
            return !string.IsNullOrWhiteSpace(incidentIdPrefix) &&
                   !string.IsNullOrWhiteSpace(effectId) &&
                   !string.IsNullOrWhiteSpace(locationId);
        }

        protected override void OnWorldEffectAppliedServer(NetworkIdentity interactor, int revision)
        {
            string incidentId = $"{incidentIdPrefix}-{revision.ToString("D3", CultureInfo.InvariantCulture)}";
            var signal = new PhysicalIncidentSignal(
                incidentId,
                incidentKind,
                effectId,
                locationId,
                objectiveStepId,
                interactor,
                netIdentity);
            raisedIncidents.Add(signal);
            IncidentRaisedServer?.Invoke(signal);
        }

        public override void OnStopServer()
        {
            raisedIncidents.Clear();
            base.OnStopServer();
        }

        [Server]
        public override void ResetInteractionStateServer()
        {
            base.ResetInteractionStateServer();
            raisedIncidents.Clear();
        }
    }
}
