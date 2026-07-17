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
    GUIStyle panelStyle;
    GUIStyle titleStyle;
    GUIStyle pageTitleStyle;
    GUIStyle kickerStyle;
    Texture2D panelTexture;
    Texture2D buttonTexture;
    Texture2D buttonHoverTexture;
    Texture2D buttonPressedTexture;
    Texture2D fieldTexture;
    Texture2D scrimTexture;
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
                SetMenuVisible(true, MenuPage.Home);
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

    void OnDestroy()
    {
        DestroyTexture(panelTexture);
        DestroyTexture(buttonTexture);
        DestroyTexture(buttonHoverTexture);
        DestroyTexture(buttonPressedTexture);
        DestroyTexture(fieldTexture);
        DestroyTexture(scrimTexture);
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
            else if (ShouldOpenModeMenu())
            {
                SetMenuVisible(true, MenuPage.Home);
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
        if (stylesInitialized && pageTitleStyle != null && panelStyle != null)
        {
            return;
        }

        stylesInitialized = false;
        DestroyTexture(panelTexture);
        DestroyTexture(buttonTexture);
        DestroyTexture(buttonHoverTexture);
        DestroyTexture(buttonPressedTexture);
        DestroyTexture(fieldTexture);
        DestroyTexture(scrimTexture);

        panelTexture = CreateTexture(new Color32(0xE8, 0xDC, 0xC5, 0xFC));
        buttonTexture = CreateTexture(new Color32(0x41, 0x5B, 0x4C, 0xFF));
        buttonHoverTexture = CreateTexture(new Color32(0x4E, 0x6E, 0x5B, 0xFF));
        buttonPressedTexture = CreateTexture(new Color32(0x33, 0x47, 0x3C, 0xFF));
        fieldTexture = CreateTexture(new Color32(0xD9, 0xCB, 0xAF, 0xFF));
        scrimTexture = CreateTexture(new Color32(0x08, 0x0B, 0x0D, 0xB8));

        buttonStyle = new GUIStyle(GUI.skin.button)
        {
            fontSize = fontSize,
            fixedHeight = buttonHeight,
            alignment = TextAnchor.MiddleCenter,
            fontStyle = FontStyle.Bold,
            normal = { textColor = new Color32(0xE8, 0xE3, 0xD5, 0xFF), background = buttonTexture },
            hover = { textColor = Color.white, background = buttonHoverTexture },
            active = { textColor = Color.white, background = buttonPressedTexture },
            focused = { textColor = Color.white, background = buttonHoverTexture },
            border = new RectOffset(1, 1, 1, 1),
            padding = new RectOffset(18, 18, 8, 8)
        };

        labelStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = fontSize,
            alignment = TextAnchor.MiddleCenter,
            wordWrap = true,
            normal = { textColor = new Color32(0x2B, 0x2A, 0x24, 0xFF) }
        };

        textFieldStyle = new GUIStyle(GUI.skin.textField)
        {
            fontSize = fontSize,
            fixedHeight = fieldHeight,
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = new Color32(0x2B, 0x2A, 0x24, 0xFF), background = fieldTexture },
            focused = { textColor = new Color32(0x2B, 0x2A, 0x24, 0xFF), background = fieldTexture },
            padding = new RectOffset(12, 12, 6, 6)
        };

        homeButtonStyle = new GUIStyle(buttonStyle)
        {
            fontSize = Mathf.Min(fontSize, 20),
            fixedHeight = 44f
        };

        homeDescriptionStyle = new GUIStyle(labelStyle)
        {
            fontSize = Mathf.Min(fontSize, 15),
            normal = { textColor = new Color32(0x6E, 0x68, 0x57, 0xFF) }
        };

        panelStyle = new GUIStyle(GUI.skin.box)
        {
            normal = { background = panelTexture },
            padding = new RectOffset(28, 28, 24, 26),
            border = new RectOffset(2, 2, 2, 2)
        };

        titleStyle = new GUIStyle(labelStyle)
        {
            fontSize = Mathf.Min(fontSize + 10, 38),
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleLeft
        };

        pageTitleStyle = new GUIStyle(titleStyle)
        {
            fontSize = Mathf.Min(fontSize + 2, 28)
        };

        kickerStyle = new GUIStyle(homeDescriptionStyle)
        {
            fontSize = Mathf.Min(fontSize, 14),
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleLeft,
            normal = { textColor = new Color32(0x80, 0x69, 0x48, 0xFF) }
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

        if (currentPage == MenuPage.Home || currentPage == MenuPage.Network)
            GUI.DrawTexture(new Rect(0f, 0f, screenWidth, screenHeight), scrimTexture);

        if (currentPage == MenuPage.NormalRound || currentPage == MenuPage.Sandbox)
        {
            DrawPageHeader(screenWidth);
            GUI.matrix = previousMatrix;
            return;
        }

        float scaledWidth = panelWidth;
        float scaledHeight = currentPage == MenuPage.Home
            ? Mathf.Min(468f, screenHeight - 24f)
            : Mathf.Min(450f, screenHeight - 24f);
        float x = (screenWidth - scaledWidth) * 0.5f + offsetX;
        float y = (screenHeight - scaledHeight) * 0.5f + offsetY;
        GUILayout.BeginArea(new Rect(x, y, scaledWidth, scaledHeight), panelStyle);

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
        GUILayout.Label("AKTA SYSTEMOWE • MENU", kickerStyle, GUILayout.Height(22f));
        GUILayout.Label("WYBIERZ TRYB", titleStyle, GUILayout.Height(48f));
        GUILayout.Label("Uruchom lobby, przejdź do Rundy albo otwórz narzędzia testowe.", homeDescriptionStyle, GUILayout.Height(38f));

        if (GUILayout.Button("SIEĆ / HOST", homeButtonStyle))
        {
            SetMenuVisible(true, MenuPage.Network);
        }

        if (GUILayout.Button("ZWYKŁA RUNDA", homeButtonStyle))
        {
            SetMenuVisible(true, MenuPage.NormalRound);
        }

        if (Application.isEditor)
        {
            if (GUILayout.Button("TRYB DEVELOPERSKI", homeButtonStyle))
            {
                OpenDeveloperMode();
            }
        }

        if (GUILayout.Button("USTAWIENIA", homeButtonStyle))
        {
            SetMenuVisible(false, MenuPage.Home);
            settingsMenu?.Open();
        }

        if (GUILayout.Button("ZAMKNIJ MENU", homeButtonStyle))
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
        float headerHeight = currentPage == MenuPage.Sandbox ? 280f : 150f;
        GUILayout.BeginArea(new Rect(12f, 12f, Mathf.Min(390f, screenWidth - 24f), headerHeight), panelStyle);
        GUILayout.Label("AKTA TRYBU", kickerStyle);
        GUILayout.Label(title, pageTitleStyle);
        if (GUILayout.Button("← Menu", homeButtonStyle, GUILayout.Width(170f)))
        {
            SetMenuVisible(true, MenuPage.Home);
        }
        if (currentPage == MenuPage.Sandbox
            && GUILayout.Button("Graj z panelem", homeButtonStyle, GUILayout.Width(220f)))
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

    bool ShouldOpenModeMenu()
    {
        RoundPhase? phase = roundCoordinator?.CurrentView?.Phase;
        return !phase.HasValue || phase.Value == RoundPhase.Lobby;
    }

    static Texture2D CreateTexture(Color color)
    {
        var texture = new Texture2D(1, 1, TextureFormat.RGBA32, false)
        {
            hideFlags = HideFlags.HideAndDontSave
        };
        texture.SetPixel(0, 0, color);
        texture.Apply();
        return texture;
    }

    static void DestroyTexture(Texture2D texture)
    {
        if (texture != null)
            Destroy(texture);
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
        bool lobbyVisible = isVisible
            ? currentPage == MenuPage.NormalRound
            : ShouldShowLobbyWhenMenuClosed();
        SetExternalMenusVisible(
            lobbyVisible,
            sandboxPinned || (isVisible && currentPage == MenuPage.Sandbox));
    }

    bool ShouldShowLobbyWhenMenuClosed()
    {
        if (!NetworkClient.isConnected && !NetworkServer.active)
            return false;

        RoundPhase? phase = roundCoordinator?.CurrentView?.Phase;
        return !phase.HasValue || phase.Value == RoundPhase.Lobby;
    }

    void SetExternalMenusVisible(bool lobbyVisible, bool sandboxVisible)
    {
        if (roundPresenter != null)
        {
            roundPresenter.SetLobbyMenuVisible(lobbyVisible);
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
