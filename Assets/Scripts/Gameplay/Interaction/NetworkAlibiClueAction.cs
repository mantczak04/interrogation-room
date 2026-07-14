using System;
using System.Globalization;
using Mirror;
using UnityEngine;

namespace InterrogationRoom.Gameplay.Interaction
{
    public sealed class NetworkAlibiClueAction : NetworkObjectiveWorldAction
    {
        [Header("A3 stable ids")]
        [SerializeField] private string clueId = "alibi-clue";
        [SerializeField] private string incidentIdPrefix = "alibi-clue-search";
        [SerializeField] private PhysicalIncidentKind incidentKind = PhysicalIncidentKind.Quiet;
        [SerializeField] private string effectId = "searched-confiscated-property";
        [SerializeField] private string locationId = "evidence-room";

        [Header("Public appearance only")]
        [SerializeField] private string publicPropDescription = "Crumpled receipt";

        public string ClueId => clueId ?? string.Empty;
        public string PublicPropDescription => publicPropDescription ?? string.Empty;
        public string IncidentIdPrefix => incidentIdPrefix ?? string.Empty;
        public PhysicalIncidentKind IncidentKind => incidentKind;
        public string EffectId => effectId ?? string.Empty;
        public string LocationId => locationId ?? string.Empty;
        public string LastIncidentId { get; private set; } = string.Empty;

        public event Action<PhysicalAlibiClueSignal> ClueAcquiredServer;

        protected override bool CanApplyWorldEffectServer(NetworkIdentity interactor, int nextRevision)
        {
            return !string.IsNullOrWhiteSpace(clueId) &&
                   !string.IsNullOrWhiteSpace(incidentIdPrefix) &&
                   !string.IsNullOrWhiteSpace(effectId) &&
                   !string.IsNullOrWhiteSpace(locationId);
        }

        protected override void OnWorldEffectAppliedServer(NetworkIdentity interactor, int revision)
        {
            LastIncidentId = $"{incidentIdPrefix}-{revision.ToString("D3", CultureInfo.InvariantCulture)}";
            ClueAcquiredServer?.Invoke(new PhysicalAlibiClueSignal(
                clueId,
                LastIncidentId,
                incidentKind,
                effectId,
                locationId,
                interactor,
                netIdentity));
        }

        [Server]
        public override void ResetInteractionStateServer()
        {
            base.ResetInteractionStateServer();
            LastIncidentId = string.Empty;
        }
    }
}
