using Mirror;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(NetworkManager))]
public class CenteredNetworkManagerHUD : MonoBehaviour
{
    NetworkManager manager;
    SteamLobby steamLobby;

    public int offsetX;
    public int offsetY;
    public int panelWidth = 560;
    public int buttonHeight = 72;
    public int fontSize = 28;
    public int fieldHeight = 52;
    public float uiScale = 1.5f;

    GUIStyle buttonStyle;
    GUIStyle labelStyle;
    GUIStyle textFieldStyle;
    bool stylesInitialized;

    void Awake()
    {
        manager = GetComponent<NetworkManager>();
        steamLobby = GetComponent<SteamLobby>();
    }

    bool SteamMode => steamLobby != null && steamLobby.SteamAvailable;

    void InitStyles()
    {
        if (stylesInitialized)
        {
            return;
        }

        buttonStyle = new GUIStyle(GUI.skin.button)
        {
            fontSize = fontSize,
            fixedHeight = buttonHeight,
            alignment = TextAnchor.MiddleCenter
        };

        labelStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = fontSize,
            alignment = TextAnchor.MiddleCenter,
            wordWrap = true
        };

        textFieldStyle = new GUIStyle(GUI.skin.textField)
        {
            fontSize = fontSize,
            fixedHeight = fieldHeight,
            alignment = TextAnchor.MiddleCenter
        };

        stylesInitialized = true;
    }

    void OnGUI()
    {
        InitStyles();

        Matrix4x4 previousMatrix = GUI.matrix;
        float scale = uiScale * Mathf.Min(Screen.width / 1280f, Screen.height / 720f, 2f);
        GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(scale, scale, 1f));

        float scaledWidth = panelWidth;
        float scaledHeight = 420f;
        float x = (Screen.width / scale - scaledWidth) * 0.5f + offsetX;
        float y = (Screen.height / scale - scaledHeight) * 0.5f + offsetY;
        GUILayout.BeginArea(new Rect(x, y, scaledWidth, scaledHeight));

        if (!NetworkClient.isConnected && !NetworkServer.active)
        {
            StartButtons();
        }
        else
        {
            StatusLabels();
        }

        if (NetworkClient.isConnected && !NetworkClient.ready)
        {
            if (GUILayout.Button("Client Ready", buttonStyle))
            {
                NetworkClient.Ready();
                if (NetworkClient.localPlayer == null)
                {
                    NetworkClient.AddPlayer();
                }
            }
        }

        StopButtons();

        GUILayout.EndArea();
        GUI.matrix = previousMatrix;
    }

    void StartButtons()
    {
        if (SteamMode)
        {
            SteamStartButtons();
            return;
        }

        if (!NetworkClient.active)
        {
#if UNITY_WEBGL
            if (GUILayout.Button("Single Player", buttonStyle))
            {
                NetworkServer.listen = false;
                manager.StartHost();
            }
#else
            if (GUILayout.Button("Host (Server + Client)", buttonStyle))
            {
                manager.StartHost();
            }
#endif

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Client", buttonStyle, GUILayout.Width(panelWidth * 0.35f)))
            {
                manager.StartClient();
            }

            manager.networkAddress = GUILayout.TextField(manager.networkAddress, textFieldStyle);

            if (Transport.active is PortTransport portTransport)
            {
                if (ushort.TryParse(GUILayout.TextField(portTransport.Port.ToString(), textFieldStyle, GUILayout.Width(90f)), out ushort port))
                {
                    portTransport.Port = port;
                }
            }

            GUILayout.EndHorizontal();

#if UNITY_WEBGL
            GUILayout.Box("( WebGL cannot be server )", labelStyle);
#else
            if (GUILayout.Button("Server Only", buttonStyle))
            {
                manager.StartServer();
            }
#endif
        }
        else
        {
            GUILayout.Label($"Connecting to {manager.networkAddress}..", labelStyle);
            if (GUILayout.Button("Cancel Connection Attempt", buttonStyle))
            {
                StopClientAndLobby();
            }
        }
    }

    void SteamStartButtons()
    {
        if (NetworkClient.active)
        {
            GUILayout.Label("Connecting via Steam..", labelStyle);
            if (GUILayout.Button("Cancel Connection Attempt", buttonStyle))
            {
                StopClientAndLobby();
            }
            return;
        }

        if (steamLobby.LobbyPending)
        {
            GUILayout.Label("Waiting for Steam lobby..", labelStyle);
            return;
        }

        if (GUILayout.Button("Host Steam Lobby (Friends)", buttonStyle))
        {
            steamLobby.HostLobby();
        }

        GUILayout.Label("Friends join through the Steam overlay: Friends → Join Game", labelStyle);
    }

    void StopClientAndLobby()
    {
        manager.StopClient();
        if (steamLobby != null)
        {
            steamLobby.LeaveLobby();
        }
    }

    void StopHostAndLobby()
    {
        manager.StopHost();
        if (steamLobby != null)
        {
            steamLobby.LeaveLobby();
        }
    }

    void StatusLabels()
    {
        if (NetworkServer.active && NetworkClient.active)
        {
            GUILayout.Label($"<b>Host</b>: running via {Transport.active}", labelStyle);
        }
        else if (NetworkServer.active)
        {
            GUILayout.Label($"<b>Server</b>: running via {Transport.active}", labelStyle);
        }
        else if (NetworkClient.isConnected)
        {
            GUILayout.Label($"<b>Client</b>: connected to {manager.networkAddress} via {Transport.active}", labelStyle);
        }

        if (steamLobby != null && steamLobby.InLobby)
        {
            if (steamLobby.OverlayEnabled)
            {
                if (GUILayout.Button("Invite Friends (Steam Overlay)", buttonStyle))
                {
                    steamLobby.OpenInviteDialog();
                }
            }
            else
            {
                GUILayout.Label("Steam Overlay unavailable — use direct invite below", labelStyle);
            }

            int friendCount = Mathf.Min(steamLobby.DirectInviteFriendCount, 2);
            for (int i = 0; i < friendCount; i++)
            {
                string friendName = steamLobby.GetDirectInviteFriendName(i);
                if (GUILayout.Button($"Invite {friendName}", buttonStyle, GUILayout.Height(52f)))
                {
                    steamLobby.InviteDirectFriend(i);
                }
            }

            if (friendCount == 0)
            {
                GUILayout.Label("No online Steam friends found", labelStyle);
            }

            GUILayout.Label("Esc: release / capture mouse", labelStyle);
        }
    }

    void StopButtons()
    {
        if (NetworkServer.active && NetworkClient.isConnected)
        {
            GUILayout.BeginHorizontal();
#if UNITY_WEBGL
            if (GUILayout.Button("Stop Single Player", buttonStyle))
            {
                StopHostAndLobby();
            }
#else
            if (GUILayout.Button("Stop Host", buttonStyle))
            {
                StopHostAndLobby();
            }

            if (GUILayout.Button("Stop Client", buttonStyle))
            {
                StopClientAndLobby();
            }
#endif
            GUILayout.EndHorizontal();
        }
        else if (NetworkClient.isConnected)
        {
            if (GUILayout.Button("Stop Client", buttonStyle))
            {
                StopClientAndLobby();
            }
        }
        else if (NetworkServer.active)
        {
            if (GUILayout.Button("Stop Server", buttonStyle))
            {
                manager.StopServer();
            }
        }
    }
}
