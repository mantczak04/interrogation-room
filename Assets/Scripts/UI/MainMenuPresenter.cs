using InterrogationRoom.Settings;
using InterrogationRoom.UI;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace InterrogationRoom.UI
{
/// <summary>
/// Drives the UI Toolkit main menu. Replaces the uGUI canvas menu so that the
/// first screen a player sees is built from the same stylesheet as the rest of
/// the game rather than from hand-tinted widgets.
/// </summary>
[RequireComponent(typeof(UIDocument))]
public class MainMenuPresenter : MonoBehaviour
{
    [Header("Scenes")]
    [SerializeField] private string gameSceneName = "Room";

    private UIDocument document;
    private Button hostButton;
    private Button joinButton;
    private Button developerTestButton;
    private Button settingsButton;
    private Button quitButton;
    private Label kicker;
    private Label note;
    private Label build;
    private VisualElement pendingInvitePanel;
    private Label pendingInviteLabel;
    private Button acceptInviteButton;
    private Button dismissInviteButton;

    private bool loadingGameScene;

    private void Awake()
    {
        document = GetComponent<UIDocument>();
    }

    private void OnEnable()
    {
        VisualElement root = document.rootVisualElement;

        hostButton = root.Q<Button>("host-button");
        joinButton = root.Q<Button>("join-button");
        developerTestButton = root.Q<Button>("developer-test-button");
        settingsButton = root.Q<Button>("settings-button");
        quitButton = root.Q<Button>("quit-button");
        kicker = root.Q<Label>("menu-kicker");
        note = root.Q<Label>("menu-note");
        build = root.Q<Label>("menu-build");
        pendingInvitePanel = root.Q<VisualElement>("pending-invite-panel");
        pendingInviteLabel = root.Q<Label>("pending-invite-label");
        acceptInviteButton = root.Q<Button>("accept-invite-button");
        dismissInviteButton = root.Q<Button>("dismiss-invite-button");

        hostButton.clicked += HostGame;
        joinButton.clicked += JoinServer;
        developerTestButton.clicked += OpenDeveloperTest;
        settingsButton.clicked += OpenSettings;
        quitButton.clicked += QuitGame;
        acceptInviteButton.clicked += AcceptPendingInvite;
        dismissInviteButton.clicked += DismissPendingInvite;

        UiSounds.Bind(root);

        GameSettingsService.Current.Changed += RefreshLocalizedText;
        RefreshLocalizedText();
    }

    private void OnDisable()
    {
        GameSettingsService.Current.Changed -= RefreshLocalizedText;

        if (hostButton != null)
            hostButton.clicked -= HostGame;
        if (joinButton != null)
            joinButton.clicked -= JoinServer;
        if (developerTestButton != null)
            developerTestButton.clicked -= OpenDeveloperTest;
        if (settingsButton != null)
            settingsButton.clicked -= OpenSettings;
        if (quitButton != null)
            quitButton.clicked -= QuitGame;
        if (acceptInviteButton != null)
            acceptInviteButton.clicked -= AcceptPendingInvite;
        if (dismissInviteButton != null)
            dismissInviteButton.clicked -= DismissPendingInvite;
    }

    private void Start()
    {
        PlayerInputGate.SetUiInputBlocked(true);
        TryOpenPendingSteamLobby();
    }

    private void Update()
    {
        TryOpenPendingSteamLobby();
        RefreshPendingInvite();

        if (WasEscapePressed() && !SettingsMenu.IsOpen && !SettingsMenu.EscapeConsumedThisFrame)
            OpenSettings();
    }

    private void RefreshLocalizedText()
    {
        hostButton.text = UiText.Get("Gospodarz gry").ToUpperInvariant();
        joinButton.text = UiText.Get("Dołącz do serwera").ToUpperInvariant();
        developerTestButton.text = UiText.Get("Test deweloperski").ToUpperInvariant();
        settingsButton.text = UiText.Get("Ustawienia").ToUpperInvariant();
        quitButton.text = UiText.Get("Wyjdź").ToUpperInvariant();

        kicker.text = UiText.Get("AKTA SPRAWY");
        note.text = UiText.Get("Gra sieciowa dla 3–8 graczy.");
        build.text = $"v{Application.version}";
        acceptInviteButton.text = UiText.Get("Dołącz").ToUpperInvariant();
        dismissInviteButton.text = UiText.Get("Odrzuć").ToUpperInvariant();
        RefreshPendingInvite();
    }

    private static bool WasEscapePressed()
    {
#if ENABLE_INPUT_SYSTEM
        return Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame;
#else
        return Input.GetKeyDown(KeyCode.Escape);
#endif
    }

    private void TryOpenPendingSteamLobby()
    {
        if (loadingGameScene || !GameLaunchRequest.HasPendingSteamLobbyJoin)
            return;

        loadingGameScene = true;
        SceneManager.LoadScene(gameSceneName);
    }

    private void HostGame()
    {
        loadingGameScene = true;
        GameLaunchRequest.Set(GameLaunchMode.Host);
        SceneManager.LoadScene(gameSceneName);
    }

    private void JoinServer()
    {
        loadingGameScene = true;
        GameLaunchRequest.Set(GameLaunchMode.Join);
        SceneManager.LoadScene(gameSceneName);
    }

    private void AcceptPendingInvite()
    {
        if (!GameLaunchRequest.AcceptPendingSteamLobbyInvite())
            return;

        loadingGameScene = true;
        SceneManager.LoadScene(gameSceneName);
    }

    private void DismissPendingInvite()
    {
        GameLaunchRequest.DismissPendingSteamLobbyInvite();
        RefreshPendingInvite();
    }

    private void RefreshPendingInvite()
    {
        bool hasInvite = GameLaunchRequest.HasPendingSteamLobbyInvite;
        pendingInvitePanel.style.display = hasInvite ? DisplayStyle.Flex : DisplayStyle.None;
        if (!hasInvite)
            return;

        string inviterName = GameLaunchRequest.PendingSteamLobbyInviterName;
        pendingInviteLabel.text = string.IsNullOrWhiteSpace(inviterName)
            ? UiText.Get("Otrzymano zaproszenie do lobby Steam.")
            : UiText.Format("{0} zaprasza Cię do lobby.", inviterName);
    }

    private void OpenDeveloperTest()
    {
        loadingGameScene = true;
        GameLaunchRequest.Set(GameLaunchMode.DeveloperTest);
        SceneManager.LoadScene(gameSceneName);
    }

    private void OpenSettings()
    {
        SettingsMenu.EnsureInstance().Open();
    }

    private void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
}
