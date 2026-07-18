using System.Text;
using Mirror;
using InterrogationRoom.Debugging;
using InterrogationRoom.Domain;
using InterrogationRoom.Networking;
using InterrogationRoom.Settings;
using InterrogationRoom.UI;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Mode and network menu. Players reach it on the host/join path and with Esc
/// in the lobby, so it is built from the same case-file sheet as the rest of
/// the UI rather than from IMGUI defaults.
///
/// The document is created at runtime, like <see cref="SettingsMenu"/>, so the
/// scene needs no extra wiring beyond the RoundPresenter reference.
/// </summary>
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

    private const string PanelSettingsResource = "UI/UIPanelSettings";
    private const string VisualTreeResource = "UI/NetworkMenu";

    /// <summary>Above the Round UI, below the settings sheet.</summary>
    private const float SortingOrder = 50f;

    NetworkManager manager;
    SteamLobby steamLobby;
    NetworkRoundCoordinator roundCoordinator;
    RoundDeveloperPanel developerPanel;
    SettingsMenu settingsMenu;

    [SerializeField] RoundPresenter roundPresenter;
    [SerializeField] string mainMenuSceneName = "MainMenu";

    UIDocument document;
    VisualElement scrim;
    VisualElement sheetBody;
    VisualElement header;
    VisualElement headerActions;
    Label kickerLabel;
    Label titleLabel;
    Label descriptionLabel;
    Label headerKickerLabel;
    Label headerTitleLabel;
    Label hintLabel;

    bool isVisible;
    bool sandboxPinned;
    bool hadLocalPlayer;
    MenuPage currentPage = MenuPage.Home;
    string renderedSignature;

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

        BuildDocument();
    }

    void OnEnable()
    {
        HandlesEscape = true;
        GameSettingsService.Current.Changed += OnLanguageChanged;
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
        GameSettingsService.Current.Changed -= OnLanguageChanged;
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

    void BuildDocument()
    {
        var panelSettings = Resources.Load<PanelSettings>(PanelSettingsResource);
        var visualTree = Resources.Load<VisualTreeAsset>(VisualTreeResource);
        if (panelSettings == null || visualTree == null)
        {
            Debug.LogError(
                $"[CenteredNetworkManagerHUD] Could not load '{PanelSettingsResource}' or '{VisualTreeResource}'.", this);
            enabled = false;
            return;
        }

        document = gameObject.AddComponent<UIDocument>();
        document.panelSettings = panelSettings;
        document.sortingOrder = SortingOrder;
        document.visualTreeAsset = visualTree;

        VisualElement root = document.rootVisualElement;
        scrim = root.Q<VisualElement>("network-scrim");
        sheetBody = root.Q<VisualElement>("network-body");
        header = root.Q<VisualElement>("network-header");
        headerActions = root.Q<VisualElement>("header-actions");
        kickerLabel = root.Q<Label>("network-kicker");
        titleLabel = root.Q<Label>("network-title");
        descriptionLabel = root.Q<Label>("network-description");
        headerKickerLabel = root.Q<Label>("header-kicker");
        headerTitleLabel = root.Q<Label>("header-title");
        hintLabel = root.Q<Label>("network-hint");

        UiSounds.Bind(root);
        SetVisible(scrim, false);
        SetVisible(header, false);
    }

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
            return;
        }

        if (sandboxPressed)
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
            return;
        }

        RefreshHint();
        RenderIfChanged();
    }

    void OnLanguageChanged()
    {
        renderedSignature = null;
        RenderIfChanged();
    }

    /// <summary>
    /// IMGUI rebuilt the whole panel every frame, which is how the status text
    /// stayed current. Rebuilding a retained tree that often would drop focus
    /// out of the address field mid-keystroke, so the page is rebuilt only when
    /// something it displays actually changed. The address text is deliberately
    /// absent from the signature — it is owned by the field while it has focus.
    /// </summary>
    string BuildSignature()
    {
        var sb = new StringBuilder();
        sb.Append(isVisible ? '1' : '0').Append(currentPage).Append('|');
        sb.Append(NetworkServer.active ? '1' : '0');
        sb.Append(NetworkClient.active ? '1' : '0');
        sb.Append(NetworkClient.isConnected ? '1' : '0');
        sb.Append(NetworkClient.ready ? '1' : '0');
        sb.Append('|').Append(Transport.active != null ? Transport.active.ToString() : "-");
        if (steamLobby != null)
        {
            sb.Append('|').Append(steamLobby.SteamAvailable ? '1' : '0');
            sb.Append(steamLobby.InLobby ? '1' : '0');
            sb.Append(steamLobby.LobbyPending ? '1' : '0');

            // Only inside a lobby, because these reach into Steamworks and
            // throw outright when it is not initialised — which is every run
            // that starts from the KCP path.
            if (steamLobby.InLobby)
            {
                sb.Append(steamLobby.OverlayEnabled ? '1' : '0');
                sb.Append(Mathf.Min(steamLobby.DirectInviteFriendCount, 2));
            }
        }

        return sb.ToString();
    }

    void RenderIfChanged()
    {
        string signature = BuildSignature();
        if (signature == renderedSignature)
            return;

        renderedSignature = signature;
        Render();
    }

    void Render()
    {
        bool sheetVisible = isVisible && (currentPage == MenuPage.Home || currentPage == MenuPage.Network);
        bool headerVisible = isVisible && (currentPage == MenuPage.NormalRound || currentPage == MenuPage.Sandbox);
        SetVisible(scrim, sheetVisible);
        SetVisible(header, headerVisible);

        if (sheetVisible)
        {
            sheetBody.Clear();
            if (currentPage == MenuPage.Home)
                RenderHome();
            else
                RenderNetwork();
        }

        if (headerVisible)
            RenderModeHeader();
    }

    void RenderHome()
    {
        kickerLabel.text = UiText.Get("AKTA SYSTEMOWE • MENU");
        titleLabel.text = UiText.Get("WYBIERZ TRYB");
        descriptionLabel.text = UiText.Get("Uruchom lobby, przejdź do Rundy albo otwórz narzędzia testowe.");

        AddAction(sheetBody, UiText.Get("Sieć / host"), "btn--ink", () => SetMenuVisible(true, MenuPage.Network));
        AddAction(sheetBody, UiText.Get("Zwykła Runda"), "btn--paper", () => SetMenuVisible(true, MenuPage.NormalRound));

        if (Application.isEditor)
            AddAction(sheetBody, UiText.Get("Tryb developerski"), "btn--paper", OpenDeveloperMode);

        AddAction(sheetBody, UiText.Get("Ustawienia"), "btn--paper", () =>
        {
            SetMenuVisible(false, MenuPage.Home);
            settingsMenu?.Open();
        });

        sheetBody.Add(new VisualElement { name = "home-divider" });
        sheetBody[sheetBody.childCount - 1].AddToClassList("network-divider");

        AddAction(sheetBody, UiText.Get("Zamknij menu"), "btn--paper", () => SetMenuVisible(false, MenuPage.Home));
    }

    void RenderNetwork()
    {
        kickerLabel.text = UiText.Get("AKTA SYSTEMOWE • SIEĆ");
        titleLabel.text = UiText.Get("SIEĆ / HOST");
        descriptionLabel.text = UiText.Get("Utwórz lobby albo dołącz do istniejącego.");

        if (!NetworkClient.isConnected && !NetworkServer.active)
            RenderStartControls();
        else
            RenderStatus();

        if (Application.isEditor && NetworkClient.isConnected && !NetworkClient.ready)
        {
            AddAction(sheetBody, UiText.Get("Klient gotowy"), "btn--paper", () =>
            {
                NetworkClient.Ready();
                if (NetworkClient.localPlayer == null)
                    NetworkClient.AddPlayer();
            });
        }

        RenderStopControls();

        var divider = new VisualElement();
        divider.AddToClassList("network-divider");
        sheetBody.Add(divider);
        AddAction(sheetBody, UiText.Get("← Menu"), "btn--paper", () => SetMenuVisible(true, MenuPage.Home));
    }

    void RenderStartControls()
    {
        if (SteamMode)
        {
            RenderSteamStartControls();
            return;
        }

        if (NetworkClient.active)
        {
            AddStatus(UiText.Get("Łączenie…"));
            AddAction(sheetBody, UiText.Get("Anuluj"), "btn--paper", StopClientAndLobby);
            return;
        }

#if UNITY_WEBGL
        AddAction(sheetBody, UiText.Get("Graj"), "btn--ink", () =>
        {
            NetworkServer.listen = false;
            manager.StartHost();
        });
#else
        AddAction(sheetBody, UiText.Get("Utwórz lobby"), "btn--ink", () => manager.StartHost());
#endif

        var row = new VisualElement();
        row.AddToClassList("network-join-row");

        var joinButton = new Button(() => manager.StartClient()) { text = UiText.Get("Dołącz") };
        joinButton.AddToClassList("btn");
        joinButton.AddToClassList("btn--paper");
        row.Add(joinButton);

        var addressField = new TextField { value = manager.networkAddress };
        addressField.AddToClassList("field");
        addressField.AddToClassList("network-address-field");
        addressField.RegisterValueChangedCallback(evt => manager.networkAddress = evt.newValue);
        row.Add(addressField);

        if (Application.isEditor && Transport.active is PortTransport portTransport)
        {
            var portField = new TextField { value = portTransport.Port.ToString() };
            portField.AddToClassList("field");
            portField.AddToClassList("network-port-field");
            portField.RegisterValueChangedCallback(evt =>
            {
                if (ushort.TryParse(evt.newValue, out ushort port))
                    portTransport.Port = port;
            });
            row.Add(portField);
        }

        sheetBody.Add(row);

#if !UNITY_WEBGL
        if (Application.isEditor)
            AddAction(sheetBody, UiText.Get("Tylko serwer"), "btn--paper", () => manager.StartServer());
#endif
    }

    void RenderSteamStartControls()
    {
        if (NetworkClient.active)
        {
            AddStatus(UiText.Get("Łączenie przez Steam…"));
            AddAction(sheetBody, UiText.Get("Anuluj"), "btn--paper", StopClientAndLobby);
            return;
        }

        if (steamLobby.LobbyPending)
        {
            AddStatus(UiText.Get("Tworzenie lobby Steam…"));
            return;
        }

        AddAction(sheetBody, UiText.Get("Utwórz lobby dla znajomych"), "btn--ink", () => steamLobby.HostLobby());
        AddStatus(UiText.Get("Znajomi dołączają przez nakładkę Steam: Znajomi → Dołącz do gry"));
    }

    void RenderStatus()
    {
        if (NetworkServer.active && NetworkClient.active)
        {
            AddStatus(Application.isEditor
                ? $"Host: running via {Transport.active}"
                : UiText.Get("Lobby działa — jesteś hostem."));
        }
        else if (NetworkServer.active)
        {
            AddStatus(Application.isEditor
                ? $"Server: running via {Transport.active}"
                : UiText.Get("Serwer działa."));
        }
        else if (NetworkClient.isConnected)
        {
            AddStatus(Application.isEditor
                ? $"Client: connected to {manager.networkAddress} via {Transport.active}"
                : UiText.Get("Połączono z lobby."));
        }

        if (steamLobby == null || !steamLobby.InLobby)
            return;

        if (steamLobby.OverlayEnabled)
            AddAction(sheetBody, UiText.Get("Zaproś znajomych"), "btn--ink", () => steamLobby.OpenInviteDialog());
        else
            AddStatus(UiText.Get("Nakładka Steam jest niedostępna — użyj zaproszenia poniżej."));

        int friendCount = Mathf.Min(steamLobby.DirectInviteFriendCount, 2);
        for (int i = 0; i < friendCount; i++)
        {
            int index = i;
            string friendName = steamLobby.GetDirectInviteFriendName(index);
            AddAction(
                sheetBody,
                $"{UiText.Get("Zaproś")}: {friendName}",
                "btn--paper",
                () => steamLobby.InviteDirectFriend(index));
        }

        if (friendCount == 0)
            AddStatus(UiText.Get("Brak znajomych Steam online."));
    }

    void RenderStopControls()
    {
        if (NetworkServer.active && NetworkClient.isConnected)
        {
#if UNITY_WEBGL
            AddAction(sheetBody, UiText.Get("Opuść grę"), "btn--destructive", StopHostAndLobby);
#else
            AddAction(sheetBody, UiText.Get("Zamknij lobby"), "btn--destructive", StopHostAndLobby);
#endif
        }
        else if (NetworkClient.isConnected)
        {
            AddAction(sheetBody, UiText.Get("Rozłącz"), "btn--destructive", StopClientAndLobby);
        }
        else if (NetworkServer.active)
        {
            AddAction(sheetBody, UiText.Get("Zatrzymaj serwer"), "btn--destructive", () => manager.StopServer());
        }
    }

    void RenderModeHeader()
    {
        bool sandbox = currentPage == MenuPage.Sandbox;
        headerKickerLabel.text = UiText.Get("AKTA TRYBU");
        headerTitleLabel.text = UiText.Get(sandbox ? "TRYB DEVELOPERSKI" : "ZWYKŁA RUNDA");

        headerActions.Clear();
        AddAction(headerActions, UiText.Get("← Menu"), "btn--paper", () => SetMenuVisible(true, MenuPage.Home));
        if (sandbox)
            AddAction(headerActions, UiText.Get("Graj z panelem"), "btn--ink", PinSandboxAndCloseMenu);
    }

    void RefreshHint()
    {
        bool automaticLobbyVisible = (NetworkClient.isConnected || NetworkServer.active)
                                     && (roundCoordinator == null || roundCoordinator.CurrentView == null);
        bool show = Application.isEditor && !automaticLobbyVisible;
        SetVisible(hintLabel, show);
        if (show)
            hintLabel.text = UiText.Get("Esc: menu     F8: sandbox Rundy     V: mikrofon");
    }

    static void AddAction(VisualElement parent, string text, string modifier, System.Action action)
    {
        var button = new Button(action) { text = text };
        button.AddToClassList("btn");
        button.AddToClassList(modifier);
        button.AddToClassList("network-action");
        parent.Add(button);
    }

    void AddStatus(string text)
    {
        var label = new Label(text);
        label.AddToClassList("network-status");
        label.AddToClassList("document-font");
        sheetBody.Add(label);
    }

    static void SetVisible(VisualElement element, bool visible)
    {
        if (element != null)
            element.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
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
        renderedSignature = null;
        RenderIfChanged();

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
        renderedSignature = null;
        RenderIfChanged();
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
}
