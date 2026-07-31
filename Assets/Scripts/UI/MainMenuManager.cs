using InterrogationRoom.UI;
using InterrogationRoom.Settings;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace InterrogationRoom.UI
{
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
        EscapeInputRouter.EnsureInstance().Register(
            this,
            EscapeHandlerPriority.Context,
            () => isActiveAndEnabled && !loadingGameScene,
            OpenSettings);
        GameSettingsService.Current.Changed += RefreshLocalizedText;
    }

    private void OnDisable()
    {
        EscapeInputRouter.UnregisterOwner(this);
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
}
