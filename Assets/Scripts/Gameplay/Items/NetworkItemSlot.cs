using System;
using System.Collections.Generic;
using InterrogationRoom.Gameplay.Interaction;
using InterrogationRoom.Items;
using Mirror;
using UnityEngine;

namespace InterrogationRoom.Gameplay.Items
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(NetworkIdentity))]
    public sealed class NetworkItemSlot : NetworkBehaviour, INetworkInteractable, IPhysicalObjectiveCompletionSource
    {
        private static readonly HashSet<NetworkItemSlot> Instances =
            new HashSet<NetworkItemSlot>();

        [Header("Placement")]
        [SerializeField] private Transform interactionPoint;
        [SerializeField] private Transform placementAnchor;
        [SerializeField] private string interactionPrompt = "Odłóż przedmiot";
        [SerializeField] private bool acceptsAnyItem;
        [SerializeField] private string[] acceptedItemIds = Array.Empty<string>();

        [Header("Runda result")]
        [SerializeField] private string actionId = "place-significant-item";
        [SerializeField] private string objectiveStepId = string.Empty;

        [SyncVar]
        private uint placedItemNetId;

        private NetworkCarryableItem placedItemServer;

        public Vector3 InteractionPosition => interactionPoint != null
            ? interactionPoint.position
            : transform.position;
        public Transform PlacementAnchor => placementAnchor != null ? placementAnchor : transform;
        public string InteractionPrompt => interactionPrompt;
        public uint PlacedItemNetId => placedItemNetId;
        public bool IsOccupied => placedItemServer != null || placedItemNetId != 0;
        public string ObjectiveStepId => objectiveStepId ?? string.Empty;

        public event Action<NetworkInteractionCompletion> CompletedServer;

        private void OnEnable()
        {
            Instances.Add(this);
        }

        private void OnDisable()
        {
            Instances.Remove(this);
        }

        public bool CanInteract(NetworkIdentity interactor)
        {
            NetworkCarryableItem carriedItem = NetworkCarryableItem.FindCarriedBy(interactor);
            return carriedItem != null && CanAccept(carriedItem);
        }

        [Server]
        public bool TryInteractServer(NetworkIdentity interactor)
        {
            if (!NetworkServer.active || interactor == null)
                return false;

            NetworkCarryableItem carriedItem = NetworkCarryableItem.FindCarriedBy(interactor);
            return carriedItem != null && carriedItem.TryPlaceServer(this, interactor);
        }

        public bool CanAccept(NetworkCarryableItem item)
        {
            return item != null &&
                   !IsOccupied &&
                   CarryItemRules.SlotAccepts(item.ItemId, acceptsAnyItem, acceptedItemIds);
        }

        [Server]
        internal void CommitPlacementServer(NetworkCarryableItem item, NetworkIdentity actor)
        {
            if (!NetworkServer.active || item == null || actor == null)
                return;

            placedItemServer = item;
            NetworkIdentity itemIdentity = item.GetComponent<NetworkIdentity>();
            placedItemNetId = itemIdentity != null ? itemIdentity.netId : 0;
            if (!string.IsNullOrWhiteSpace(objectiveStepId))
            {
                CompletedServer?.Invoke(new NetworkInteractionCompletion(
                    actionId,
                    objectiveStepId,
                    actor,
                    GetComponent<NetworkIdentity>()));
            }
        }

        [Server]
        public bool ReleasePlacementServer(NetworkCarryableItem item)
        {
            if (!NetworkServer.active || item == null)
                return false;
            NetworkIdentity itemIdentity = item.GetComponent<NetworkIdentity>();
            uint itemNetId = itemIdentity != null ? itemIdentity.netId : 0;
            if (!ReferenceEquals(placedItemServer, item) &&
                (itemNetId == 0 || itemNetId != placedItemNetId))
            {
                return false;
            }

            placedItemServer = null;
            placedItemNetId = 0;
            return true;
        }

        [Server]
        public void ResetInteractionStateServer()
        {
            if (!NetworkServer.active)
                return;

            NetworkCarryableItem item = placedItemServer;
            placedItemServer = null;
            placedItemNetId = 0;
            if (item != null && item.State == CarryItemState.Placed)
                item.ReturnHomeServer();
        }

        public static NetworkItemSlot FindByNetId(uint netId)
        {
            if (netId == 0)
                return null;

            foreach (NetworkItemSlot slot in Instances)
            {
                NetworkIdentity identity = slot != null
                    ? slot.GetComponent<NetworkIdentity>()
                    : null;
                if (identity != null && identity.netId == netId)
                    return slot;
            }

            return null;
        }
    }
}
