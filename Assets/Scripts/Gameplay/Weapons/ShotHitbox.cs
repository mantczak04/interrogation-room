using System;
using InterrogationRoom.Networking;
using Mirror;
using UnityEngine;

namespace InterrogationRoom.Gameplay.Weapons
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(NetworkIdentity))]
    public sealed class ShotHitbox : MonoBehaviour, IShotHitReceiver, IRoundHitSource
    {
        [SerializeField] private ShotHitKind hitKind = ShotHitKind.Surface;

        public event Action<RoundPlayerHit> PlayerHitReceivedServer;

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

            NetworkIdentity target = GetComponent<NetworkIdentity>();
            if (target == null || context.Shooter == null)
            {
                Debug.LogError(
                    $"[{nameof(ShotHitbox)}] Player hits require shooter and target NetworkIdentity values.",
                    this);
                return;
            }

            ServerHitCount++;
            PlayerHitReceivedServer?.Invoke(new RoundPlayerHit(context.Shooter, target));
        }
    }
}
