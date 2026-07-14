using System;

public enum GameLaunchMode
{
    None,
    Host,
    Join
}

public static class GameLaunchRequest
{
    private static GameLaunchMode pendingMode;

    public static void Set(GameLaunchMode mode) => pendingMode = mode;

    public static GameLaunchMode Consume()
    {
        GameLaunchMode mode = pendingMode;
        pendingMode = GameLaunchMode.None;
        return mode;
    }

    public static bool WasStartedFromSteamInvite()
    {
        string[] arguments = Environment.GetCommandLineArgs();
        for (int index = 0; index < arguments.Length - 1; index++)
        {
            if (arguments[index] == "+connect_lobby" &&
                ulong.TryParse(arguments[index + 1], out ulong lobbyId) &&
                lobbyId != 0)
            {
                return true;
            }
        }

        return false;
    }
}
