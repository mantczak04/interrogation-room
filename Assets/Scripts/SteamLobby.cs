#if !(UNITY_STANDALONE_WIN || UNITY_STANDALONE_LINUX || UNITY_STANDALONE_OSX || UNITY_EDITOR_WIN || UNITY_EDITOR_LINUX || UNITY_EDITOR_OSX || STEAMWORKS_WIN || STEAMWORKS_LIN_OSX)
#define DISABLESTEAMWORKS
#endif

using Mirror;
using InterrogationRoom.Networking;
using UnityEngine;
#if !DISABLESTEAMWORKS
using System;
using Steamworks;
#endif

// Creates and joins Steam lobbies and routes Mirror through the matching transport.
// Execution order sits after SteamManager (-2000) and before NetworkManager (0),
// so Steam is initialized before the transport choice and the choice is final
// before Mirror caches Transport.active.
[DisallowMultipleComponent]
[RequireComponent(typeof(NetworkManager))]
[DefaultExecutionOrder(-1000)]
public class SteamLobby : MonoBehaviour
{
    public const string HostAddressKey = "HostAddress";

    [Tooltip("Route Mirror through the Steam transport whenever the Steam client is available. Disable for local KCP/ParrelSync testing.")]
    public bool useSteamWhenAvailable = true;

    [Tooltip("Transport used when Steam is available (FizzySteamworks).")]
    public Transport steamTransport;

    [Tooltip("Fallback transport for local development (KCP).")]
    public Transport localTransport;

    NetworkManager manager;

    public bool SteamAvailable
    {
        get
        {
#if !DISABLESTEAMWORKS
            return useSteamWhenAvailable
                && !TransportLaunchOptions.ForceKcp(Environment.GetCommandLineArgs())
                && steamTransport != null
                && SteamManager.Initialized;
#else
            return false;
#endif
        }
    }

    void Awake()
    {
        manager = GetComponent<NetworkManager>();

        bool steamAvailable = SteamAvailable;
        SetTransportEnabled(steamTransport, steamAvailable);
        SetTransportEnabled(localTransport, !steamAvailable);

        Transport selected = steamAvailable ? steamTransport : localTransport;
        if (selected != null)
        {
            manager.transport = selected;
        }
    }

    void OnValidate()
    {
        if (localTransport == null)
            Debug.LogError("[SteamLobby] Local KCP transport reference is required.", this);
        if (steamTransport != null && steamTransport == localTransport)
            Debug.LogError("[SteamLobby] Steam and local transports must be different components.", this);
    }

    static void SetTransportEnabled(Transport transport, bool enabled)
    {
        if (transport != null)
        {
            transport.enabled = enabled;
        }
    }

#if !DISABLESTEAMWORKS
    Callback<LobbyCreated_t> lobbyCreated;
    Callback<GameLobbyJoinRequested_t> gameLobbyJoinRequested;
    Callback<LobbyEnter_t> lobbyEntered;

    CSteamID currentLobbyId = CSteamID.Nil;
    bool lobbyPending;

    public bool LobbyPending => lobbyPending;
    public bool InLobby => currentLobbyId.IsValid();
    public string VoiceSessionId => InLobby ? currentLobbyId.ToString() : "local";

    void Start()
    {
        if (!SteamManager.Initialized)
        {
            return;
        }

        lobbyCreated = Callback<LobbyCreated_t>.Create(OnLobbyCreated);
        gameLobbyJoinRequested = Callback<GameLobbyJoinRequested_t>.Create(OnGameLobbyJoinRequested);
        lobbyEntered = Callback<LobbyEnter_t>.Create(OnLobbyEntered);

        TryJoinFromCommandLine();
    }

    void OnDestroy()
    {
        LeaveLobby();
    }

    // Host flow: create the lobby first; StartHost runs once Steam confirms it.
    public void HostLobby()
    {
        if (!SteamAvailable || lobbyPending || NetworkServer.active)
        {
            return;
        }

        lobbyPending = true;
        SteamMatchmaking.CreateLobby(ELobbyType.k_ELobbyTypeFriendsOnly, manager.maxConnections);
    }

    public void OpenInviteDialog()
    {
        if (InLobby)
        {
            SteamFriends.ActivateGameOverlayInviteDialog(currentLobbyId);
        }
    }

    public void LeaveLobby()
    {
        lobbyPending = false;

        if (!InLobby)
        {
            return;
        }

        if (SteamManager.Initialized)
        {
            SteamMatchmaking.LeaveLobby(currentLobbyId);
        }

        currentLobbyId = CSteamID.Nil;
    }

    void OnLobbyCreated(LobbyCreated_t callback)
    {
        lobbyPending = false;

        if (callback.m_eResult != EResult.k_EResultOK)
        {
            Debug.LogError($"[SteamLobby] Lobby creation failed: {callback.m_eResult}");
            return;
        }

        currentLobbyId = new CSteamID(callback.m_ulSteamIDLobby);
        SteamMatchmaking.SetLobbyData(currentLobbyId, HostAddressKey, SteamUser.GetSteamID().ToString());
        SteamMatchmaking.SetLobbyData(currentLobbyId, "name", $"{SteamFriends.GetPersonaName()}'s lobby");

        manager.StartHost();
    }

    void OnGameLobbyJoinRequested(GameLobbyJoinRequested_t callback)
    {
        if (NetworkServer.active || NetworkClient.active)
        {
            return;
        }

        lobbyPending = true;
        SteamMatchmaking.JoinLobby(callback.m_steamIDLobby);
    }

    void OnLobbyEntered(LobbyEnter_t callback)
    {
        lobbyPending = false;
        currentLobbyId = new CSteamID(callback.m_ulSteamIDLobby);

        if (NetworkServer.active)
        {
            return;
        }

        string hostAddress = SteamMatchmaking.GetLobbyData(currentLobbyId, HostAddressKey);
        if (string.IsNullOrEmpty(hostAddress))
        {
            Debug.LogError("[SteamLobby] Lobby has no host address; leaving lobby.");
            LeaveLobby();
            return;
        }

        manager.networkAddress = hostAddress;
        manager.StartClient();
    }

    // Steam passes "+connect_lobby <id>" when the game is launched from a friend invite.
    void TryJoinFromCommandLine()
    {
        if (!SteamAvailable || NetworkServer.active || NetworkClient.active)
        {
            return;
        }

        string[] args = Environment.GetCommandLineArgs();
        for (int i = 0; i < args.Length - 1; i++)
        {
            if (args[i] == "+connect_lobby" && ulong.TryParse(args[i + 1], out ulong lobbyId) && lobbyId != 0)
            {
                lobbyPending = true;
                SteamMatchmaking.JoinLobby(new CSteamID(lobbyId));
                return;
            }
        }
    }
#else
    public bool LobbyPending => false;
    public bool InLobby => false;
    public string VoiceSessionId => "local";

    public void HostLobby() { }
    public void OpenInviteDialog() { }
    public void LeaveLobby() { }
#endif
}
