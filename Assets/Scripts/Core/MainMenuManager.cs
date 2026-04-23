using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuManager : MonoBehaviour
{
    private const int DebugUnlockContinueTapThreshold = 5;

    [Header("Scene Names")]
    [SerializeField] private string gameSceneName = "GameScene";
    [SerializeField] private string settingsSceneName = "Settings";

    [Header("Buttons")]
    [SerializeField] private UIButtonStateView continueButtonStateView;

    [Header("Back Button")]
    [SerializeField] private float backButtonDoubleTapWindowSeconds = 1.5f;

    private float lastBackButtonTapTime = -999f;
    private int disabledContinueTapCount;
    private bool isDebugUnlockArmed;

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

    private void OnApplicationQuit()
    {
        DebugSettings.SetPersistentDebugMode(false);
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
        ResetDebugUnlockSequence();

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
        ResetDebugUnlockSequence();
        SceneManager.LoadScene(gameSceneName);
    }

    public void OnClickSettings()
    {
        ResetDebugUnlockSequence();
        SceneManager.LoadScene(settingsSceneName);
    }

    public void OnClickHowToPlay()
    {
        ResetDebugUnlockSequence();
        Debug.Log("How To Play button clicked.");
        // Later: open tutorial / instructions panel
    }

    public void OnClickQuit()
    {
        if (isDebugUnlockArmed && !SaveGameManager.HasSavedGameFile())
        {
            DebugSettings.SetPersistentDebugMode(true);
            ResetDebugUnlockSequence();
            Debug.Log("Secret unlock activated. Debug mode enabled.");
            return;
        }

        ResetDebugUnlockSequence();
        DebugSettings.SetPersistentDebugMode(false);
        Debug.Log("Quit button clicked.");

        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #else
        Application.Quit();
        #endif
    }

    public void OnDisabledContinueTapped()
    {
        if (SaveGameManager.HasSavedGameFile())
        {
            ResetDebugUnlockSequence();
            return;
        }

        disabledContinueTapCount++;

        if (disabledContinueTapCount >= DebugUnlockContinueTapThreshold)
        {
            isDebugUnlockArmed = true;
            Debug.Log("Secret continue tap sequence armed.");
        }
    }

    private void ResetDebugUnlockSequence()
    {
        disabledContinueTapCount = 0;
        isDebugUnlockArmed = false;
    }
}
