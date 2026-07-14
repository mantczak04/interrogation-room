using System;
using UnityEngine;

public enum GameLaunchMode
{
    None,
    Host,
    Join
}

public static class GameLaunchRequest
{
    private static GameLaunchMode pendingMode;
    private static ulong pendingSteamLobbyId;

    public static bool HasPendingSteamLobbyJoin => pendingSteamLobbyId != 0;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetRuntimeState()
    {
        pendingMode = GameLaunchMode.None;
        pendingSteamLobbyId = 0;
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
        pendingMode = GameLaunchMode.Join;
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
