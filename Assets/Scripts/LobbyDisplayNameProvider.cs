#if !(UNITY_STANDALONE_WIN || UNITY_STANDALONE_LINUX || UNITY_STANDALONE_OSX || UNITY_EDITOR_WIN || UNITY_EDITOR_LINUX || UNITY_EDITOR_OSX || STEAMWORKS_WIN || STEAMWORKS_LIN_OSX)
#define DISABLESTEAMWORKS
#endif

using InterrogationRoom.Networking;
#if !DISABLESTEAMWORKS
using Steamworks;
#endif

public static class LobbyDisplayNameProvider
{
    public static string Resolve(string fallback)
    {
#if !DISABLESTEAMWORKS
        if (SteamManager.Initialized)
        {
            string steamName = SteamFriends.GetPersonaName();
            if (!string.IsNullOrWhiteSpace(steamName))
                return LobbyPlayerPresentation.NormalizeDisplayName(steamName, fallback);
        }
#endif
        return LobbyPlayerPresentation.NormalizeDisplayName(fallback, "Gracz");
    }
}
