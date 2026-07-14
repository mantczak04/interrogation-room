using Mirror;
using UnityEngine;

namespace InterrogationRoom.Gameplay.Interaction
{
    /// <summary>Neutral placeholder used by the isolated B2 verification harness.</summary>
    public sealed class NetworkTimedInteractionHarnessTarget : NetworkTimedInteractable
    {
        [SyncVar]
        private int completionCount;

        public int CompletionCount => completionCount;

        [Server]
        protected override bool ApplyCompletedEffectServer(NetworkIdentity interactor)
        {
            if (!NetworkServer.active || interactor == null)
                return false;

            completionCount++;
            return true;
        }
    }
}
