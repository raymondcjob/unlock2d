using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuManager : MonoBehaviour
{
    [Header("Scene Names")]
    [SerializeField] private string gameSceneName = "GameScene";
    [SerializeField] private string settingsSceneName = "Settings";

    [Header("Buttons")]
    [SerializeField] private UIButtonStateView continueButtonStateView;

    [Header("Back Button")]
    [SerializeField] private float backButtonDoubleTapWindowSeconds = 1.5f;

    private float lastBackButtonTapTime = -999f;

    private void Start()
    {
        RefreshContinueButton();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            HandleBackButton();
        }
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        if (hasFocus)
        {
            RefreshContinueButton();
        }
    }

    private void HandleBackButton()
    {
        if (Time.unscaledTime - lastBackButtonTapTime <= backButtonDoubleTapWindowSeconds)
        {
            OnClickQuit();
            return;
        }

        lastBackButtonTapTime = Time.unscaledTime;
        Debug.Log("Tap back again to quit.");
    }

    public void OnClickContinue()
    {
        if (!SaveGameManager.HasSavedGameFile())
        {
            Debug.Log("Continue ignored: no saved board found.");
            return;
        }

        SaveGameManager.RequestLoadSavedGameOnNextGameScene();
        SceneManager.LoadScene(gameSceneName);
    }

    private void RefreshContinueButton()
    {
        if (continueButtonStateView != null)
        {
            continueButtonStateView.SetAvailable(SaveGameManager.HasSavedGameFile());
        }
    }

    public void OnClickPlay()
    {
        SceneManager.LoadScene(gameSceneName);
    }

    public void OnClickSettings()
    {
        SceneManager.LoadScene(settingsSceneName);
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
