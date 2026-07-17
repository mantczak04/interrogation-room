using Mirror;
using InterrogationRoom.Debugging;
using InterrogationRoom.Domain;
using InterrogationRoom.Networking;
using InterrogationRoom.UI;
using UnityEngine;
using UnityEngine.SceneManagement;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

[DisallowMultipleComponent]
[RequireComponent(typeof(NetworkManager))]
public class CenteredNetworkManagerHUD : MonoBehaviour
{
    private enum MenuPage
    {
        Home,
        Network,
        NormalRound,
        Sandbox
    }

    NetworkManager manager;
    SteamLobby steamLobby;
    NetworkRoundCoordinator roundCoordinator;
    RoundDeveloperPanel developerPanel;
    SettingsMenu settingsMenu;

    [SerializeField] RoundPresenter roundPresenter;
    [SerializeField] string mainMenuSceneName = "MainMenu";

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
    GUIStyle homeButtonStyle;
    GUIStyle homeDescriptionStyle;
    bool stylesInitialized;
    bool isVisible;
    bool sandboxPinned;
    bool hadLocalPlayer;
    MenuPage currentPage = MenuPage.Home;

    public static bool HandlesEscape { get; private set; }

    void Awake()
    {
        manager = GetComponent<NetworkManager>();
        steamLobby = GetComponent<SteamLobby>();
        roundCoordinator = GetComponent<NetworkRoundCoordinator>();
        if (roundPresenter != null)
        {
            LobbyCharacterPresenter lobbyPresenter = roundPresenter.GetComponent<LobbyCharacterPresenter>();
            if (lobbyPresenter == null)
                lobbyPresenter = roundPresenter.gameObject.AddComponent<LobbyCharacterPresenter>();
            lobbyPresenter.Configure(roundCoordinator, steamLobby);
        }
    }

    void OnEnable()
    {
        HandlesEscape = true;
        ApplyPageVisibility();
    }

    void Start()
    {
        settingsMenu = SettingsMenu.EnsureInstance();
        settingsMenu.Configure(
            () => PlayerController.SetCursorReleased(true),
            SetCursorForClosedMenu,
            LeaveToMainMenu);

        GameLaunchMode launchMode = GameLaunchRequest.Consume();
        switch (launchMode)
        {
            case GameLaunchMode.None:
                break;
            case GameLaunchMode.Host:
                if (roundPresenter != null)
                    roundPresenter.SetLobbyMenuVisible(true);
                PlayerController.SetCursorReleased(true);
                if (SteamMode)
                    steamLobby.HostLobby();
                else
                    manager.StartHost();
                break;
            case GameLaunchMode.Join:
                SetMenuVisible(true, MenuPage.Network);
                break;
        }
    }

    void OnDisable()
    {
        HandlesEscape = false;
        sandboxPinned = false;
        if (roundPresenter != null)
        {
            roundPresenter.SetDeveloperMenuOpen(false);
        }
        SetExternalMenusVisible(false, false);
    }

    void OnValidate()
    {
        if (roundPresenter == null)
        {
            Debug.LogError("[CenteredNetworkManagerHUD] RoundPresenter reference is required.", this);
        }
    }

    bool SteamMode => steamLobby != null && steamLobby.SteamAvailable;

    void Update()
    {
        bool hasLocalPlayer = NetworkClient.localPlayer != null;
        bool localPlayerArrived = hasLocalPlayer && !hadLocalPlayer;
        hadLocalPlayer = hasLocalPlayer;

        if (isVisible && currentPage == MenuPage.Network && localPlayerArrived)
        {
            SetMenuVisible(false, MenuPage.Home);
            return;
        }

        if (isVisible
            && currentPage == MenuPage.NormalRound
            && roundCoordinator != null
            && roundCoordinator.CurrentView?.Phase == RoundPhase.Round)
        {
            SetMenuVisible(false, MenuPage.Home);
            return;
        }

#if ENABLE_INPUT_SYSTEM
        bool togglePressed = Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame;
        bool sandboxPressed = Application.isEditor
                              && Keyboard.current != null
                              && Keyboard.current.f8Key.wasPressedThisFrame;
#else
        bool togglePressed = Input.GetKeyDown(KeyCode.Escape);
        bool sandboxPressed = Application.isEditor && Input.GetKeyDown(KeyCode.F8);
#endif
        if (togglePressed && !SettingsMenu.IsOpen && !SettingsMenu.EscapeConsumedThisFrame)
        {
            if (isVisible)
            {
                SetMenuVisible(false, MenuPage.Home);
            }
            else if (settingsMenu != null)
            {
                settingsMenu.Open();
            }
        }
        else if (sandboxPressed)
        {
            if (sandboxPinned || (isVisible && currentPage == MenuPage.Sandbox))
            {
                sandboxPinned = false;
                SetMenuVisible(false, MenuPage.Home);
            }
            else
            {
                SetMenuVisible(true, MenuPage.Sandbox);
            }
        }
    }

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

        homeButtonStyle = new GUIStyle(buttonStyle)
        {
            fontSize = Mathf.Min(fontSize, 20),
            fixedHeight = 44f
        };

        homeDescriptionStyle = new GUIStyle(labelStyle)
        {
            fontSize = Mathf.Min(fontSize, 16)
        };

        stylesInitialized = true;
    }

    void OnGUI()
    {
        InitStyles();

        Matrix4x4 previousMatrix = GUI.matrix;
        float scale = uiScale * Mathf.Min(Screen.width / 1280f, Screen.height / 720f, 2f);
        GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(scale, scale, 1f));

        float screenWidth = Screen.width / scale;
        float screenHeight = Screen.height / scale;
        bool automaticLobbyVisible = (NetworkClient.isConnected || NetworkServer.active)
                                     && (roundCoordinator == null || roundCoordinator.CurrentView == null);
        if (Application.isEditor && !automaticLobbyVisible)
        {
            GUILayout.BeginArea(new Rect(0f, screenHeight - 44f, screenWidth, 40f));
            GUILayout.Label("Esc: menu     F8: sandbox Rundy     V: mute / unmute voice chat", labelStyle);
            GUILayout.EndArea();
        }

        if (!isVisible)
        {
            GUI.matrix = previousMatrix;
            return;
        }

        if (currentPage == MenuPage.NormalRound || currentPage == MenuPage.Sandbox)
        {
            DrawPageHeader(screenWidth);
            GUI.matrix = previousMatrix;
            return;
        }

        float scaledWidth = panelWidth;
        float scaledHeight = currentPage == MenuPage.Home ? 400f : 420f;
        float x = (screenWidth - scaledWidth) * 0.5f + offsetX;
        float y = (screenHeight - scaledHeight) * 0.5f + offsetY;
        GUILayout.BeginArea(new Rect(x, y, scaledWidth, scaledHeight));

        if (currentPage == MenuPage.Home)
        {
            DrawHomeMenu();
            GUILayout.EndArea();
            GUI.matrix = previousMatrix;
            return;
        }

        DrawInlineBackButton("SIEĆ / HOST");

        if (!NetworkClient.isConnected && !NetworkServer.active)
        {
            StartButtons();
        }
        else
        {
            StatusLabels();
        }

        if (Application.isEditor && NetworkClient.isConnected && !NetworkClient.ready)
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

    void DrawHomeMenu()
    {
        GUILayout.Label("MENU", labelStyle);
        GUILayout.Label("Wybierz jedną sekcję. Pozostałe panele pozostaną ukryte.", homeDescriptionStyle);

        if (GUILayout.Button("Sieć / Host", homeButtonStyle))
        {
            SetMenuVisible(true, MenuPage.Network);
        }
        GUILayout.Label("Uruchom hosta, klienta albo sprawdź stan połączenia.", homeDescriptionStyle);

        if (GUILayout.Button("Zwykła Runda", homeButtonStyle))
        {
            SetMenuVisible(true, MenuPage.NormalRound);
        }
        GUILayout.Label("Najpierw połącz się przez Sieć / Host; potem uruchom Rundę dla 3–6 graczy.", homeDescriptionStyle);

        if (Application.isEditor)
        {
            if (GUILayout.Button("Tryb developerski (DEBUG)", homeButtonStyle))
            {
                OpenDeveloperMode();
            }
            GUILayout.Label("Sam uruchamia hosta. Wybór roli, zadań i Runda bez limitu czasu. Skrót: F8.", homeDescriptionStyle);
        }

        if (GUILayout.Button("Zamknij menu", homeButtonStyle))
        {
            SetMenuVisible(false, MenuPage.Home);
        }
    }

    void DrawInlineBackButton(string title)
    {
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("← Menu", buttonStyle, GUILayout.Width(150f)))
        {
            SetMenuVisible(true, MenuPage.Home);
        }
        GUILayout.Label(title, labelStyle);
        GUILayout.EndHorizontal();
    }

    void OpenDeveloperMode()
    {
        if (!Application.isEditor || !NetworkRoundCoordinator.DeveloperToolsAvailable)
            return;

        if (!NetworkClient.active && !NetworkServer.active)
        {
            if (SteamMode)
                steamLobby.HostLobby();
            else
                manager.StartHost();
        }

        SetMenuVisible(true, MenuPage.Sandbox);
    }

    void DrawPageHeader(float screenWidth)
    {
        string title = currentPage == MenuPage.NormalRound ? "ZWYKŁA RUNDA" : "TRYB DEVELOPERSKI";
        float headerHeight = currentPage == MenuPage.Sandbox ? 180f : 120f;
        GUILayout.BeginArea(new Rect(12f, 12f, Mathf.Min(190f, screenWidth - 24f), headerHeight));
        GUILayout.Label(title, labelStyle);
        if (GUILayout.Button("← Menu", buttonStyle, GUILayout.Width(150f)))
        {
            SetMenuVisible(true, MenuPage.Home);
        }
        if (currentPage == MenuPage.Sandbox
            && GUILayout.Button("Graj z panelem", buttonStyle, GUILayout.Width(170f)))
        {
            PinSandboxAndCloseMenu();
        }
        GUILayout.EndArea();
    }

    void SetMenuVisible(bool visible, MenuPage page)
    {
        if (visible && page != MenuPage.Sandbox)
        {
            sandboxPinned = false;
        }

        isVisible = visible;
        currentPage = visible ? page : MenuPage.Home;
        if (roundPresenter != null)
        {
            roundPresenter.SetDeveloperMenuOpen(visible);
        }
        ApplyPageVisibility();

        if (visible)
        {
            PlayerController.SetCursorReleased(true);
            return;
        }

        SetCursorForClosedMenu();
    }

    void PinSandboxAndCloseMenu()
    {
        sandboxPinned = true;
        isVisible = false;
        currentPage = MenuPage.Home;
        if (roundPresenter != null)
        {
            roundPresenter.SetDeveloperMenuOpen(false);
        }
        ApplyPageVisibility();
        SetCursorForClosedMenu();
    }

    void SetCursorForClosedMenu()
    {
        bool roundNeedsPointer = roundCoordinator != null
                                 && roundCoordinator.CurrentView != null
                                 && roundCoordinator.CurrentView.Phase != RoundPhase.Round;
        PlayerController.SetCursorReleased(roundNeedsPointer || NetworkClient.localPlayer == null);
    }

    void ApplyPageVisibility()
    {
        SetExternalMenusVisible(
            isVisible && currentPage == MenuPage.NormalRound,
            sandboxPinned || (isVisible && currentPage == MenuPage.Sandbox));
    }

    void SetExternalMenusVisible(bool normalRoundVisible, bool sandboxVisible)
    {
        if (roundPresenter != null)
        {
            roundPresenter.SetLobbyMenuVisible(normalRoundVisible);
        }

        if (roundCoordinator != null && developerPanel == null)
        {
            developerPanel = roundCoordinator.GetComponent<RoundDeveloperPanel>();
        }

        if (developerPanel != null)
        {
            developerPanel.SetVisible(sandboxVisible);
        }
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
            if (GUILayout.Button("Graj", buttonStyle))
            {
                NetworkServer.listen = false;
                manager.StartHost();
            }
#else
            if (GUILayout.Button("Utwórz lobby", buttonStyle))
            {
                manager.StartHost();
            }
#endif

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Dołącz", buttonStyle, GUILayout.Width(panelWidth * 0.35f)))
            {
                manager.StartClient();
            }

            manager.networkAddress = GUILayout.TextField(manager.networkAddress, textFieldStyle);

            if (Application.isEditor && Transport.active is PortTransport portTransport)
            {
                if (ushort.TryParse(GUILayout.TextField(portTransport.Port.ToString(), textFieldStyle, GUILayout.Width(90f)), out ushort port))
                {
                    portTransport.Port = port;
                }
            }

            GUILayout.EndHorizontal();

#if !UNITY_WEBGL
            if (Application.isEditor && GUILayout.Button("Server Only", buttonStyle))
            {
                manager.StartServer();
            }
#endif
        }
        else
        {
            GUILayout.Label("Łączenie…", labelStyle);
            if (GUILayout.Button("Anuluj", buttonStyle))
            {
                StopClientAndLobby();
            }
        }
    }

    void SteamStartButtons()
    {
        if (NetworkClient.active)
        {
            GUILayout.Label("Łączenie przez Steam…", labelStyle);
            if (GUILayout.Button("Anuluj", buttonStyle))
            {
                StopClientAndLobby();
            }
            return;
        }

        if (steamLobby.LobbyPending)
        {
            GUILayout.Label("Tworzenie lobby Steam…", labelStyle);
            return;
        }

        if (GUILayout.Button("Utwórz lobby dla znajomych", buttonStyle))
        {
            steamLobby.HostLobby();
        }

        GUILayout.Label("Znajomi dołączają przez nakładkę Steam: Znajomi → Dołącz do gry", labelStyle);
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

    void LeaveToMainMenu()
    {
        if (NetworkServer.active && NetworkClient.isConnected)
        {
            StopHostAndLobby();
        }
        else if (NetworkClient.active)
        {
            StopClientAndLobby();
        }
        else if (NetworkServer.active)
        {
            manager.StopServer();
        }

        Destroy(manager.gameObject);
        SceneManager.LoadScene(mainMenuSceneName);
    }

    void StatusLabels()
    {
        if (NetworkServer.active && NetworkClient.active)
        {
            GUILayout.Label(Application.isEditor
                ? $"<b>Host</b>: running via {Transport.active}"
                : "Lobby działa — jesteś hostem.", labelStyle);
        }
        else if (NetworkServer.active)
        {
            GUILayout.Label(Application.isEditor
                ? $"<b>Server</b>: running via {Transport.active}"
                : "Serwer działa.", labelStyle);
        }
        else if (NetworkClient.isConnected)
        {
            GUILayout.Label(Application.isEditor
                ? $"<b>Client</b>: connected to {manager.networkAddress} via {Transport.active}"
                : "Połączono z lobby.", labelStyle);
        }

        if (steamLobby != null && steamLobby.InLobby)
        {
            if (steamLobby.OverlayEnabled)
            {
                if (GUILayout.Button("Zaproś znajomych", buttonStyle))
                {
                    steamLobby.OpenInviteDialog();
                }
            }
            else
            {
                GUILayout.Label("Nakładka Steam jest niedostępna — użyj zaproszenia poniżej.", labelStyle);
            }

            int friendCount = Mathf.Min(steamLobby.DirectInviteFriendCount, 2);
            for (int i = 0; i < friendCount; i++)
            {
                string friendName = steamLobby.GetDirectInviteFriendName(i);
                if (GUILayout.Button($"Zaproś: {friendName}", buttonStyle, GUILayout.Height(52f)))
                {
                    steamLobby.InviteDirectFriend(i);
                }
            }

            if (friendCount == 0)
            {
                GUILayout.Label("Brak znajomych Steam online.", labelStyle);
            }
        }
    }

    void StopButtons()
    {
        if (NetworkServer.active && NetworkClient.isConnected)
        {
#if UNITY_WEBGL
            if (GUILayout.Button("Opuść grę", buttonStyle))
            {
                StopHostAndLobby();
            }
#else
            if (GUILayout.Button("Zamknij lobby", buttonStyle))
            {
                StopHostAndLobby();
            }
#endif
        }
        else if (NetworkClient.isConnected)
        {
            if (GUILayout.Button("Rozłącz", buttonStyle))
            {
                StopClientAndLobby();
            }
        }
        else if (NetworkServer.active)
        {
            if (GUILayout.Button("Zatrzymaj serwer", buttonStyle))
            {
                manager.StopServer();
            }
        }
    }
}
