using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuManager : MonoBehaviour
{
    [Header("Scenes")]
    [SerializeField] private string gameSceneName = "Room"; // Zmienić w razie innej nazwy sceny

    public void HostGame()
    {
        Debug.Log("Host Game clicked! Loading game scene...");
        // W przyszłości tu wepniemy logikę z SteamLobby
        SceneManager.LoadScene(gameSceneName);
    }

    public void JoinServer()
    {
        Debug.Log("Join Server clicked! (Not implemented yet)");
        // W przyszłości otwieranie nakładki Steam lub UI do wpisywania IP
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
