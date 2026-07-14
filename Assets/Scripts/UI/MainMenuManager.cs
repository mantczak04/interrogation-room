using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuManager : MonoBehaviour
{
    [Header("Scenes")]
    [SerializeField] private string gameSceneName = "Room"; // Zmienić w razie innej nazwy sceny

    private bool loadingGameScene;

    private void Start()
    {
        TryOpenPendingSteamLobby();
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
        Debug.Log("Settings clicked! (Not implemented yet)");
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
