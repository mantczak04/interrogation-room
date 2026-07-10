using System;
using Mirror;
using UnityEngine;

namespace InterrogationRoom.Gameplay.Weapons
{
    [DisallowMultipleComponent]
    public sealed class ShotHitbox : MonoBehaviour, IShotHitReceiver
    {
        [SerializeField] private ShotHitKind hitKind = ShotHitKind.Surface;

        public event Action<ShotHitContext> HitReceivedServer;

        public ShotHitKind HitKind => hitKind;
        public uint ServerHitCount { get; private set; }

        public void ReceiveShotServer(ShotHitContext context)
        {
            if (!NetworkServer.active)
            {
                Debug.LogError(
                    $"[{nameof(ShotHitbox)}] Shot hits must be resolved by the server.",
                    this);
                return;
            }

            ServerHitCount++;
            HitReceivedServer?.Invoke(context);
        }
    }
}
