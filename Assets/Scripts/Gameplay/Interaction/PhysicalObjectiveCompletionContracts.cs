using System;

namespace InterrogationRoom.Gameplay.Interaction
{
    public interface IPhysicalObjectiveCompletionSource
    {
        event Action<NetworkInteractionCompletion> CompletedServer;
    }
}
