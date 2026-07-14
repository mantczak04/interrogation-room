using System;
using Mirror;

namespace InterrogationRoom.Gameplay.Interaction
{
    public enum TimedInteractionCancellationReason
    {
        Explicit,
        ValidationFailed,
        PerformerUnavailable,
        CompletionRejected,
        ServerInterruption,
        BeginRejected
    }

    public readonly struct NetworkInteractionCompletion
    {
        public NetworkInteractionCompletion(
            string actionId,
            string payloadId,
            NetworkIdentity actor,
            NetworkIdentity target)
        {
            ActionId = actionId;
            PayloadId = payloadId;
            Actor = actor;
            Target = target;
        }

        public string ActionId { get; }
        public string PayloadId { get; }
        public NetworkIdentity Actor { get; }
        public NetworkIdentity Target { get; }
    }

    public readonly struct NetworkInteractionCancellation
    {
        public NetworkInteractionCancellation(
            string actionId,
            NetworkIdentity actor,
            NetworkIdentity target,
            TimedInteractionCancellationReason reason)
        {
            ActionId = actionId;
            Actor = actor;
            Target = target;
            Reason = reason;
        }

        public string ActionId { get; }
        public NetworkIdentity Actor { get; }
        public NetworkIdentity Target { get; }
        public TimedInteractionCancellationReason Reason { get; }
    }

    public interface INetworkTimedInteractable : INetworkInteractable
    {
        string ActionId { get; }
        string CompletionPayloadId { get; }
        float InteractionDuration { get; }
        bool HasActiveInteractor { get; }

        event Action<NetworkInteractionCompletion> CompletedServer;
        event Action<NetworkInteractionCancellation> CancelledServer;

        bool TryBeginInteractionServer(NetworkIdentity interactor);
        bool TryCompleteInteractionServer(NetworkIdentity interactor);
        void CancelInteractionServer(NetworkIdentity interactor);
        void CancelInteractionServer(
            NetworkIdentity interactor,
            TimedInteractionCancellationReason reason);
    }
}
