using Mirror;
using UnityEngine;

namespace InterrogationRoom.Gameplay.Interaction
{
    public interface INetworkInteractable
    {
        Vector3 InteractionPosition { get; }

        string InteractionPrompt { get; }

        bool CanInteract(NetworkIdentity interactor);

        bool TryInteractServer(NetworkIdentity interactor);
    }
}
