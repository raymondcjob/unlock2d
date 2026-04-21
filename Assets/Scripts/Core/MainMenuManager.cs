using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuManager : MonoBehaviour
{
    [Header("Scene Names")]
    [SerializeField] private string gameSceneName = "GameScene";

    public void OnClickContinue()
    {
        Debug.Log("Continue button clicked.");
        // Later: resume last session / load saved progress
    }

    public void OnClickPlay()
    {
        SceneManager.LoadScene(gameSceneName);
    }

    public void OnClickSettings()
    {
        Debug.Log("Settings button clicked.");
        // Later: open settings panel
    }

    public void OnClickHowToPlay()
    {
        Debug.Log("How To Play button clicked.");
        // Later: open tutorial / instructions panel
    }

    public void OnClickQuit()
    {
        Debug.Log("Quit button clicked.");

        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #else
        Application.Quit();
        #endif
    }
}