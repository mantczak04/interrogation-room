using System;
using InterrogationRoom.Networking;
using Mirror;
using UnityEngine;

namespace InterrogationRoom.Gameplay.EasterEggs
{
    /// <summary>
    /// Scene-level lifecycle adapter. NetworkRoundCoordinator announces only
    /// public Runda lifecycle events; easter-egg selection remains independent
    /// from role, Alibi, objective and outcome seeds.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class EasterEggRoundBridge : MonoBehaviour
    {
        [SerializeField] private NetworkRoundCoordinator coordinator;
        [SerializeField] private NetworkEasterEggDirector director;

        private void OnEnable()
        {
            if (coordinator == null)
                coordinator = FindFirstObjectByType<NetworkRoundCoordinator>();
            if (director == null)
                director = GetComponent<NetworkEasterEggDirector>();

            if (coordinator == null || director == null)
            {
                Debug.LogError(
                    "[EasterEggRoundBridge] Coordinator and director are required.",
                    this);
                enabled = false;
                return;
            }

            coordinator.ServerGameplayRoundStarted += OnGameplayRoundStartedServer;
            coordinator.ServerGameplayRoundEnded += OnGameplayRoundEndedServer;
        }

        private void OnDisable()
        {
            if (coordinator == null)
                return;

            coordinator.ServerGameplayRoundStarted -= OnGameplayRoundStartedServer;
            coordinator.ServerGameplayRoundEnded -= OnGameplayRoundEndedServer;
        }

        [Server]
        private void OnGameplayRoundStartedServer()
        {
            director.BeginRundaServer(Guid.NewGuid().GetHashCode());
        }

        [Server]
        private void OnGameplayRoundEndedServer()
        {
            director.EndRundaServer();
        }

        private void OnValidate()
        {
            if (director != null && !director.TryValidateWiring(out string error))
                Debug.LogError($"[EasterEggRoundBridge] {error}", director);
        }
    }
}
