using System;
using InterrogationRoom.Domain;
using Mirror;
using UnityEngine;

namespace InterrogationRoom.Networking
{
    public readonly struct RoundPlayerHit
    {
        public RoundPlayerHit(NetworkIdentity shooter, NetworkIdentity target)
        {
            Shooter = shooter;
            Target = target;
        }

        public NetworkIdentity Shooter { get; }
        public NetworkIdentity Target { get; }
    }

    /// <summary>Physical weapon capability implemented by the networked player prefab.</summary>
    public interface IRoundWeaponPort
    {
        bool IsWeaponAuthorized { get; }
        bool HasWeapon { get; }

        bool SetWeaponAuthorizationServer(bool authorized);
        bool TryEquipWeaponServer();
    }

    /// <summary>Server-only hit notification. It reports physics; it never resolves Runda rules.</summary>
    public interface IRoundHitSource
    {
        event Action<RoundPlayerHit> PlayerHitReceivedServer;
    }

    /// <summary>Physical elimination effect invoked only after RoundEngine accepts Egzekucja.</summary>
    public interface IRoundEliminationPort
    {
        bool IsEliminated { get; }

        bool TryEliminateServer();
        bool ResetEliminationServer();
    }

    /// <summary>Server-owned movement effect used when a new Runda gathers everyone in the start room.</summary>
    public interface IRoundRelocationPort
    {
        bool RelocateToStartRoomServer(Vector3 position, Quaternion rotation);
    }

    public static class RoundPhysicalRules
    {
        public static bool CanEquipWeapon(bool isAuthorized, bool hasWeapon) =>
            isAuthorized && !hasWeapon;

        public static bool CanFireWeapon(bool isAuthorized, bool hasWeapon) =>
            isAuthorized && hasWeapon;

        public static bool CanSubmitExecutionHit(
            RoundPhase phase,
            RoundRole shooterRole,
            bool shooterAuthorized,
            bool shooterHasWeapon,
            RoundRole targetRole,
            bool targetEliminated) =>
            phase == RoundPhase.Round &&
            shooterRole == RoundRole.Detective &&
            CanFireWeapon(shooterAuthorized, shooterHasWeapon) &&
            targetRole != RoundRole.Detective &&
            !targetEliminated;
    }
}
