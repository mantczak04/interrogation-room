using Mirror;
using UnityEngine;

namespace InterrogationRoom.Gameplay.Weapons
{
    public enum ShotHitKind : byte
    {
        Miss,
        Surface,
        Player,
        Prop
    }

    public readonly struct ShotHitContext
    {
        public ShotHitContext(
            NetworkIdentity shooter,
            Collider collider,
            Vector3 point,
            Vector3 normal,
            Vector3 direction)
        {
            Shooter = shooter;
            Collider = collider;
            Point = point;
            Normal = normal;
            Direction = direction;
        }

        public NetworkIdentity Shooter { get; }
        public Collider Collider { get; }
        public Vector3 Point { get; }
        public Vector3 Normal { get; }
        public Vector3 Direction { get; }
    }

    public interface IShotHitReceiver
    {
        ShotHitKind HitKind { get; }

        void ReceiveShotServer(ShotHitContext context);
    }
}
