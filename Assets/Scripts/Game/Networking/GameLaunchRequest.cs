using System;
using UnityEngine;

public enum GameLaunchMode
{
    None,
    Host,
    Join,

    /// <summary>
    /// Editor/development-build shortcut: host locally and drop straight into
    /// a playable developer Runda without waiting for other players.
    /// </summary>
    DeveloperTest
}

public static class GameLaunchRequest
{
    private static GameLaunchMode pendingMode;
    private static ulong pendingSteamLobbyId;
    private static ulong pendingSteamLobbyInviteId;
    private static string pendingSteamLobbyInviterName;

    public static bool HasPendingSteamLobbyJoin => pendingSteamLobbyId != 0;
    public static bool HasPendingSteamLobbyInvite => pendingSteamLobbyInviteId != 0;
    public static string PendingSteamLobbyInviterName => pendingSteamLobbyInviterName ?? string.Empty;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetRuntimeState()
    {
        pendingMode = GameLaunchMode.None;
        pendingSteamLobbyId = 0;
        pendingSteamLobbyInviteId = 0;
        pendingSteamLobbyInviterName = string.Empty;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void CaptureCommandLineInvite()
    {
        CaptureSteamLobbyJoin(Environment.GetCommandLineArgs());
    }

    public static void Set(GameLaunchMode mode) => pendingMode = mode;

    public static GameLaunchMode Consume()
    {
        GameLaunchMode mode = pendingMode;
        pendingMode = GameLaunchMode.None;
        return mode;
    }

    public static void SetSteamLobbyJoin(ulong lobbyId)
    {
        if (lobbyId == 0)
            return;

        pendingSteamLobbyId = lobbyId;
        pendingSteamLobbyInviteId = 0;
        pendingSteamLobbyInviterName = string.Empty;
        pendingMode = GameLaunchMode.Join;
    }

    public static void SetSteamLobbyInvite(ulong lobbyId, string inviterName)
    {
        if (lobbyId == 0)
            return;

        pendingSteamLobbyInviteId = lobbyId;
        pendingSteamLobbyInviterName = inviterName ?? string.Empty;
    }

    public static bool AcceptPendingSteamLobbyInvite()
    {
        if (pendingSteamLobbyInviteId == 0)
            return false;

        ulong lobbyId = pendingSteamLobbyInviteId;
        SetSteamLobbyJoin(lobbyId);
        return true;
    }

    public static void DismissPendingSteamLobbyInvite()
    {
        pendingSteamLobbyInviteId = 0;
        pendingSteamLobbyInviterName = string.Empty;
    }

    public static bool TryConsumeSteamLobbyJoin(out ulong lobbyId)
    {
        lobbyId = pendingSteamLobbyId;
        pendingSteamLobbyId = 0;
        return lobbyId != 0;
    }

    public static bool CaptureSteamLobbyJoin(string[] arguments)
    {
        if (!TryParseSteamLobbyJoin(arguments, out ulong lobbyId))
            return false;

        SetSteamLobbyJoin(lobbyId);
        return true;
    }

    public static bool TryParseSteamLobbyJoin(string[] arguments, out ulong lobbyId)
    {
        lobbyId = 0;
        if (arguments == null)
            return false;

        for (int index = 0; index < arguments.Length - 1; index++)
        {
            if (arguments[index] == "+connect_lobby" &&
                ulong.TryParse(arguments[index + 1], out lobbyId) &&
                lobbyId != 0)
            {
                return true;
            }
        }

        lobbyId = 0;
        return false;
    }

    public static bool WasStartedFromSteamInvite()
    {
        return TryParseSteamLobbyJoin(Environment.GetCommandLineArgs(), out _);
    }
}
