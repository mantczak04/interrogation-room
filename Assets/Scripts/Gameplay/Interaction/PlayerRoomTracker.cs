using System;
using Mirror;
using UnityEngine;

namespace InterrogationRoom.Gameplay.Interaction
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(NetworkIdentity))]
    public sealed class PlayerRoomTracker : NetworkBehaviour
    {
        [SerializeField, Min(0.05f)] private float refreshInterval = 0.1f;

        [SyncVar(hook = nameof(OnCurrentRoomIdChanged))]
        private string currentRoomId = string.Empty;

        private double nextRefreshAt;

        public string CurrentRoomId => currentRoomId ?? string.Empty;

        public event Action<string, string> CurrentRoomChanged;

        public override void OnStartServer()
        {
            base.OnStartServer();
            RefreshRoomServer();
        }

        [ServerCallback]
        private void Update()
        {
            if (NetworkTime.time < nextRefreshAt)
                return;

            nextRefreshAt = NetworkTime.time + refreshInterval;
            RefreshRoomServer();
        }

        [Server]
        public bool RefreshRoomServer()
        {
            string resolvedRoomId = RoomVolume.ResolveRoomId(transform.position);
            if (string.Equals(currentRoomId, resolvedRoomId, StringComparison.Ordinal))
                return false;

            currentRoomId = resolvedRoomId;
            return true;
        }

        private void OnCurrentRoomIdChanged(string previousRoomId, string nextRoomId)
        {
            CurrentRoomChanged?.Invoke(previousRoomId ?? string.Empty, nextRoomId ?? string.Empty);
        }
    }
}
