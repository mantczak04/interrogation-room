using System.Collections.Generic;
using Mirror;
using UnityEngine;

namespace InterrogationRoom.Gameplay.Interaction
{
    /// <summary>
    /// Reusable-per-player physical action. A foreign actor can visibly use it
    /// once, but cannot consume the assigned owner's required opportunity.
    /// </summary>
    public class NetworkObjectiveWorldAction : NetworkTimedInteractable
    {
        [Header("Authored anchor")]
        [SerializeField] private string anchorId = "objective-anchor";

        [Header("Public world presentation")]
        [SerializeField] private GameObject effectVisual;

        [SyncVar(hook = nameof(OnWorldRevisionChanged))]
        private int worldRevision;

        [SyncVar]
        private uint lastActorNetId;

        private readonly HashSet<int> completedActors = new HashSet<int>();

        public int WorldRevision => worldRevision;
        public uint LastActorNetId => lastActorNetId;
        public int CompletedActorCountServer => completedActors.Count;
        public string AnchorId => anchorId ?? string.Empty;

        protected virtual void Awake()
        {
            // Required items are never globally consumable. Each actor gets one
            // attempt, so a bluff cannot permanently steal an owner's action.
            OneShot = false;
            ApplyPresentation(worldRevision);
        }

        public override void OnStartClient()
        {
            base.OnStartClient();
            ApplyPresentation(worldRevision);
        }

        public override bool CanInteract(NetworkIdentity interactor)
        {
            return base.CanInteract(interactor) &&
                   !completedActors.Contains(GetActorKey(interactor));
        }

        /// <summary>
        /// Area A calls this only when the physical result did not advance the
        /// actor's current domain step. The visible world change is not reverted.
        /// </summary>
        [Server]
        public bool ReleaseActorCompletionServer(NetworkIdentity interactor)
        {
            return NetworkServer.active && completedActors.Remove(GetActorKey(interactor));
        }

        protected sealed override bool ApplyCompletedEffectServer(NetworkIdentity interactor)
        {
            int actorKey = GetActorKey(interactor);
            if (actorKey == 0 || completedActors.Contains(actorKey))
                return false;

            int nextRevision = worldRevision + 1;
            if (!CanApplyWorldEffectServer(interactor, nextRevision))
                return false;

            completedActors.Add(actorKey);
            lastActorNetId = interactor.netId;
            worldRevision = nextRevision;
            ApplyPresentation(worldRevision);
            OnWorldEffectAppliedServer(interactor, worldRevision);
            return true;
        }

        protected virtual bool CanApplyWorldEffectServer(NetworkIdentity interactor, int nextRevision) => true;

        protected virtual void OnWorldEffectAppliedServer(NetworkIdentity interactor, int revision)
        {
        }

        private void OnWorldRevisionChanged(int _, int revision)
        {
            ApplyPresentation(revision);
        }

        private void ApplyPresentation(int revision)
        {
            if (effectVisual != null)
                effectVisual.SetActive(revision > 0);
        }

        private static int GetActorKey(NetworkIdentity interactor)
        {
            if (interactor == null)
                return 0;
            return interactor.netId != 0
                ? unchecked((int)interactor.netId)
                : interactor.GetEntityId().GetHashCode();
        }

        public override void OnStopServer()
        {
            completedActors.Clear();
            base.OnStopServer();
        }

        [Server]
        public override void ResetInteractionStateServer()
        {
            base.ResetInteractionStateServer();
            completedActors.Clear();
            worldRevision = 0;
            lastActorNetId = 0;
            ApplyPresentation(worldRevision);
        }
    }
}
