using InterrogationRoom.UI;
using InterrogationRoom.Settings;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class MainMenuManager : MonoBehaviour
{
    [Header("Scenes")]
    [SerializeField] private string gameSceneName = "Room"; // Zmienić w razie innej nazwy sceny

    private bool loadingGameScene;

    private void Start()
    {
        PlayerInputGate.SetUiInputBlocked(true);
        RefreshLocalizedText();
        TryOpenPendingSteamLobby();
    }

    private void OnEnable()
    {
        GameSettingsService.Current.Changed += RefreshLocalizedText;
    }

    private void OnDisable()
    {
        GameSettingsService.Current.Changed -= RefreshLocalizedText;
    }

    private void RefreshLocalizedText()
    {
        foreach (TextMeshProUGUI label in FindObjectsByType<TextMeshProUGUI>(
                     FindObjectsInactive.Include,
                     FindObjectsSortMode.None))
        {
            if (label.gameObject.name != "Text")
                continue;

            string polish = label.transform.parent != null ? label.transform.parent.name switch
            {
                "Button_Host Game" => "Gospodarz gry",
                "Button_Join Server" => "Dołącz do serwera",
                "Button_Settings" => "Ustawienia",
                "Button_Quit" => "Wyjdź",
                _ => null
            } : null;

            if (polish != null)
                label.text = UiText.Get(polish);
        }
    }

    private void Update()
    {
        TryOpenPendingSteamLobby();

        if (WasEscapePressed() && !SettingsMenu.IsOpen && !SettingsMenu.EscapeConsumedThisFrame)
        {
            OpenSettings();
        }
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

    public void HostGame()
    {
        loadingGameScene = true;
        GameLaunchRequest.Set(GameLaunchMode.Host);
        SceneManager.LoadScene(gameSceneName);
    }

    public void JoinServer()
    {
        loadingGameScene = true;
        GameLaunchRequest.Set(GameLaunchMode.Join);
        SceneManager.LoadScene(gameSceneName);
    }

    public void OpenSettings()
    {
        SettingsMenu.EnsureInstance().Open();
    }

    public void QuitGame()
    {
        Debug.Log("Quit Game clicked! Quitting...");
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
