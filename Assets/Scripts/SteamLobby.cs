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
    Callback<LobbyEnter_t> lobbyEntered;
    Callback<GameOverlayActivated_t> gameOverlayActivated;

    CSteamID currentLobbyId = CSteamID.Nil;
    bool lobbyPending;
    bool cursorWasReleasedBeforeOverlay;

    public bool LobbyPending => lobbyPending;
    public bool InLobby => currentLobbyId.IsValid();
    public string VoiceSessionId => InLobby ? currentLobbyId.ToString() : "local";
    public bool OverlayEnabled => SteamManager.Initialized && SteamUtils.IsOverlayEnabled();
    public int DirectInviteFriendCount => CountDirectInviteFriends();

    void Start()
    {
        if (!SteamManager.Initialized)
        {
            return;
        }

        lobbyCreated = Callback<LobbyCreated_t>.Create(OnLobbyCreated);
        lobbyEntered = Callback<LobbyEnter_t>.Create(OnLobbyEntered);
        gameOverlayActivated = Callback<GameOverlayActivated_t>.Create(OnGameOverlayActivated);

        Debug.Log($"[SteamLobby] Overlay enabled: {OverlayEnabled}");

        TryJoinPendingLaunchRequest();
    }

    void Update()
    {
        TryJoinPendingLaunchRequest();
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
            PlayerController.SetCursorReleased(true);
            Debug.Log($"[SteamLobby] Opening invite overlay for lobby {currentLobbyId}. Overlay enabled: {OverlayEnabled}");
            SteamFriends.ActivateGameOverlayInviteDialog(currentLobbyId);
        }
    }

    public string GetDirectInviteFriendName(int visibleIndex)
    {
        CSteamID friendId = GetDirectInviteFriend(visibleIndex);
        return friendId.IsValid() ? SteamFriends.GetFriendPersonaName(friendId) : "Unknown friend";
    }

    public bool InviteDirectFriend(int visibleIndex)
    {
        if (!InLobby)
        {
            return false;
        }

        CSteamID friendId = GetDirectInviteFriend(visibleIndex);
        if (!friendId.IsValid())
        {
            return false;
        }

        bool sent = SteamMatchmaking.InviteUserToLobby(currentLobbyId, friendId);
        Debug.Log($"[SteamLobby] Direct invite to {SteamFriends.GetFriendPersonaName(friendId)} ({friendId}): {sent}");
        return sent;
    }

    int CountDirectInviteFriends()
    {
        int visibleCount = 0;
        int friendCount = SteamFriends.GetFriendCount(EFriendFlags.k_EFriendFlagImmediate);
        for (int i = 0; i < friendCount; i++)
        {
            CSteamID friendId = SteamFriends.GetFriendByIndex(i, EFriendFlags.k_EFriendFlagImmediate);
            if (friendId.IsValid() && SteamFriends.GetFriendPersonaState(friendId) != EPersonaState.k_EPersonaStateOffline)
            {
                visibleCount++;
            }
        }

        return visibleCount;
    }

    CSteamID GetDirectInviteFriend(int visibleIndex)
    {
        int visibleCount = 0;
        int friendCount = SteamFriends.GetFriendCount(EFriendFlags.k_EFriendFlagImmediate);
        for (int i = 0; i < friendCount; i++)
        {
            CSteamID friendId = SteamFriends.GetFriendByIndex(i, EFriendFlags.k_EFriendFlagImmediate);
            if (!friendId.IsValid() || SteamFriends.GetFriendPersonaState(friendId) == EPersonaState.k_EPersonaStateOffline)
            {
                continue;
            }

            if (visibleCount == visibleIndex)
            {
                return friendId;
            }

            visibleCount++;
        }

        return CSteamID.Nil;
    }

    void OnGameOverlayActivated(GameOverlayActivated_t callback)
    {
        bool overlayActive = callback.m_bActive != 0;
        if (overlayActive)
        {
            cursorWasReleasedBeforeOverlay = PlayerController.CursorReleased;
            PlayerController.SetCursorReleased(true);
            return;
        }

        PlayerController.SetCursorReleased(cursorWasReleasedBeforeOverlay);
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

    void TryJoinPendingLaunchRequest()
    {
        if (!GameLaunchRequest.HasPendingSteamLobbyJoin)
        {
            return;
        }

        if (NetworkServer.active || NetworkClient.active)
        {
            GameLaunchRequest.TryConsumeSteamLobbyJoin(out _);
            return;
        }

        if (!SteamAvailable || lobbyPending ||
            !GameLaunchRequest.TryConsumeSteamLobbyJoin(out ulong lobbyId))
            return;

        lobbyPending = true;
        SteamMatchmaking.JoinLobby(new CSteamID(lobbyId));
    }
#else
    public bool LobbyPending => false;
    public bool InLobby => false;
    public string VoiceSessionId => "local";
    public bool OverlayEnabled => false;
    public int DirectInviteFriendCount => 0;

    public void HostLobby() { }
    public void OpenInviteDialog() { }
    public string GetDirectInviteFriendName(int visibleIndex) => "Unknown friend";
    public bool InviteDirectFriend(int visibleIndex) => false;
    public void LeaveLobby() { }
#endif
}
