using InterrogationRoom.Settings;
using InterrogationRoom.UI;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

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
    private Button settingsButton;
    private Button quitButton;
    private Label kicker;
    private Label note;
    private Label build;

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
        settingsButton = root.Q<Button>("settings-button");
        quitButton = root.Q<Button>("quit-button");
        kicker = root.Q<Label>("menu-kicker");
        note = root.Q<Label>("menu-note");
        build = root.Q<Label>("menu-build");

        hostButton.clicked += HostGame;
        joinButton.clicked += JoinServer;
        settingsButton.clicked += OpenSettings;
        quitButton.clicked += QuitGame;

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
        if (settingsButton != null)
            settingsButton.clicked -= OpenSettings;
        if (quitButton != null)
            quitButton.clicked -= QuitGame;
    }

    private void Start()
    {
        PlayerInputGate.SetUiInputBlocked(true);
        TryOpenPendingSteamLobby();
    }

    private void Update()
    {
        TryOpenPendingSteamLobby();

        if (WasEscapePressed() && !SettingsMenu.IsOpen && !SettingsMenu.EscapeConsumedThisFrame)
            OpenSettings();
    }

    private void RefreshLocalizedText()
    {
        hostButton.text = UiText.Get("Gospodarz gry").ToUpperInvariant();
        joinButton.text = UiText.Get("Dołącz do serwera").ToUpperInvariant();
        settingsButton.text = UiText.Get("Ustawienia").ToUpperInvariant();
        quitButton.text = UiText.Get("Wyjdź").ToUpperInvariant();

        kicker.text = UiText.Get("AKTA SPRAWY");
        note.text = UiText.Get("Gra sieciowa dla 3–6 graczy.");
        build.text = $"v{Application.version}";
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
