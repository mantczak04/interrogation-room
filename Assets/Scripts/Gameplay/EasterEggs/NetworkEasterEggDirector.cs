using System;
using System.Collections.Generic;
using InterrogationRoom.EasterEggs;
using Mirror;
using UnityEngine;

namespace InterrogationRoom.Gameplay.EasterEggs
{
    /// <summary>
    /// Host-authoritative bridge between the pure authored catalog and scene
    /// spots. It never maps a connection to a player and carries no Runda secret.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(NetworkIdentity))]
    public sealed class NetworkEasterEggDirector : NetworkBehaviour
    {
        [SerializeField, Range(1, EasterEggSelector.MaximumSpawnChancePercent)]
        private int spawnChancePercent = EasterEggSelector.DefaultSpawnChancePercent;

        [SerializeField] private NetworkEasterEggSpot[] authoredSpots =
            Array.Empty<NetworkEasterEggSpot>();

        [SyncVar]
        private string activeEasterEggId = string.Empty;

        public string ActiveEasterEggId => activeEasterEggId ?? string.Empty;
        public bool HasActiveEasterEgg => !string.IsNullOrEmpty(ActiveEasterEggId);

        public event Action<EasterEggSelection> SelectionAppliedServer;

        /// <summary>
        /// Call once when a Runda begins. easterEggSeed must be an independent
        /// host-only seed, never the seed used to assign roles or private content.
        /// </summary>
        [Server]
        public EasterEggSelection BeginRundaServer(int easterEggSeed)
        {
            if (!NetworkServer.active)
                return EasterEggSelection.None;

            ResetAllSpotsServer();
            if (!TryBuildConfiguredPool(out List<EasterEggDefinition> configuredPool, out _))
            {
                ApplySelectionServer(EasterEggSelection.None);
                return EasterEggSelection.None;
            }

            EasterEggSelection selection = EasterEggSelector.Select(
                easterEggSeed,
                configuredPool,
                spawnChancePercent);
            ApplySelectionServer(selection);
            return selection;
        }

        [Server]
        public void EndRundaServer()
        {
            if (!NetworkServer.active)
                return;

            ResetAllSpotsServer();
            activeEasterEggId = string.Empty;
        }

        public bool TryValidateWiring(out string error)
        {
            return TryBuildConfiguredPool(out _, out error);
        }

        private bool TryBuildConfiguredPool(
            out List<EasterEggDefinition> configuredPool,
            out string error)
        {
            configuredPool = new List<EasterEggDefinition>();
            if (authoredSpots == null)
            {
                error = "No easter-egg spots are configured.";
                return false;
            }

            IReadOnlyList<EasterEggDefinition> catalog = InitialEasterEggCatalog.Definitions;
            for (int definitionIndex = 0; definitionIndex < catalog.Count; definitionIndex++)
            {
                EasterEggDefinition definition = catalog[definitionIndex];
                int matchCount = 0;
                for (int spotIndex = 0; spotIndex < authoredSpots.Length; spotIndex++)
                {
                    NetworkEasterEggSpot spot = authoredSpots[spotIndex];
                    if (spot != null && spot.EasterEggId == definition.Id)
                    {
                        if (!spot.HasCompleteAuthoredIds() ||
                            spot.PropId != definition.PropId ||
                            spot.LocationId != definition.LocationId ||
                            spot.EffectId != definition.EffectId)
                        {
                            error = $"Spot '{definition.Id}' does not match its hand-authored catalog ids.";
                            return false;
                        }
                        matchCount++;
                    }
                }

                if (matchCount != 1)
                {
                    error = $"Easter egg '{definition.Id}' requires exactly one scene spot; found {matchCount}.";
                    return false;
                }
                configuredPool.Add(definition);
            }

            error = string.Empty;
            return true;
        }

        [Server]
        private void ApplySelectionServer(EasterEggSelection selection)
        {
            activeEasterEggId = selection.HasSpawn ? selection.Definition.Id : string.Empty;
            for (int index = 0; index < authoredSpots.Length; index++)
            {
                NetworkEasterEggSpot spot = authoredSpots[index];
                if (spot != null)
                    spot.SetAvailableForRundaServer(spot.EasterEggId == activeEasterEggId);
            }
            SelectionAppliedServer?.Invoke(selection);
        }

        [Server]
        private void ResetAllSpotsServer()
        {
            if (authoredSpots == null)
                return;
            for (int index = 0; index < authoredSpots.Length; index++)
            {
                NetworkEasterEggSpot spot = authoredSpots[index];
                if (spot != null && spot.HasCompleteAuthoredIds())
                    spot.SetAvailableForRundaServer(false);
            }
        }
    }
}
