using System;
using Mirror;
using UnityEngine;

namespace InterrogationRoom.Gameplay.Interaction
{
    /// <summary>
    /// Shared reservation and one-shot state for multi-second world actions.
    /// Subclasses apply a world effect only from ApplyCompletedEffectServer.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(NetworkIdentity))]
    public abstract class NetworkTimedInteractable : NetworkBehaviour, INetworkTimedInteractable
    {
        [Header("Interaction")]
        [SerializeField] private Transform interactionPoint;
        [SerializeField] private string interactionPrompt = "Interact";
        [SerializeField] private string actionId = "timed-action";
        [SerializeField] private string completionPayloadId = "timed-action-completed";
        [SerializeField, Min(0.05f)] private float interactionDuration = 2f;
        [SerializeField] private bool oneShot = true;

        [SyncVar]
        private bool consumed;

        private NetworkIdentity activeInteractor;

        public Vector3 InteractionPosition => interactionPoint != null
            ? interactionPoint.position
            : transform.position;
        public string InteractionPrompt => interactionPrompt;
        public string ActionId => actionId;
        public string CompletionPayloadId => completionPayloadId;
        public float InteractionDuration => interactionDuration;
        public bool IsConsumed => consumed;
        public bool HasActiveInteractor => activeInteractor != null;

        protected bool OneShot
        {
            get => oneShot;
            set => oneShot = value;
        }

        public event Action<NetworkInteractionCompletion> CompletedServer;
        public event Action<NetworkInteractionCancellation> CancelledServer;

        public virtual bool CanInteract(NetworkIdentity interactor) =>
            interactor != null && !consumed &&
            (activeInteractor == null || ReferenceEquals(activeInteractor, interactor));

        [Server]
        public bool TryBeginInteractionServer(NetworkIdentity interactor)
        {
            if (!NetworkServer.active || !CanInteract(interactor) || activeInteractor != null)
                return false;

            activeInteractor = interactor;
            OnInteractionBeganServer(interactor);
            return true;
        }

        [Server]
        public bool TryCompleteInteractionServer(NetworkIdentity interactor)
        {
            if (!NetworkServer.active || !ReferenceEquals(activeInteractor, interactor) || consumed)
                return false;

            if (!ApplyCompletedEffectServer(interactor))
                return false;

            activeInteractor = null;
            if (oneShot)
                consumed = true;

            CompletedServer?.Invoke(new NetworkInteractionCompletion(
                actionId,
                completionPayloadId,
                interactor,
                netIdentity));
            return true;
        }

        [Server]
        public void CancelInteractionServer(NetworkIdentity interactor)
        {
            CancelInteractionServer(interactor, TimedInteractionCancellationReason.Explicit);
        }

        [Server]
        public void CancelInteractionServer(
            NetworkIdentity interactor,
            TimedInteractionCancellationReason reason)
        {
            if (!ReferenceEquals(activeInteractor, interactor))
                return;

            activeInteractor = null;
            OnInteractionCancelledServer(interactor, reason);
            CancelledServer?.Invoke(new NetworkInteractionCancellation(
                actionId,
                interactor,
                netIdentity,
                reason));
        }

        public bool TryInteractServer(NetworkIdentity interactor) => false;

        [Server]
        public virtual void ResetInteractionStateServer()
        {
            if (!NetworkServer.active)
                return;

            activeInteractor = null;
            consumed = false;
        }

        protected abstract bool ApplyCompletedEffectServer(NetworkIdentity interactor);

        protected virtual void OnInteractionBeganServer(NetworkIdentity interactor)
        {
        }

        protected virtual void OnInteractionCancelledServer(
            NetworkIdentity interactor,
            TimedInteractionCancellationReason reason)
        {
        }

        public override void OnStopServer()
        {
            activeInteractor = null;
            base.OnStopServer();
        }
    }
}
