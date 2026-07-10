using Mirror;
using UnityEngine;

namespace InterrogationRoom.Gameplay.Interaction
{
    public interface INetworkInteractable
    {
        Vector3 InteractionPosition { get; }

        bool TryInteractServer(NetworkIdentity interactor);
    }
}
