using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class MainMenuManager : MonoBehaviour
{
    private const int DebugUnlockContinueTapThreshold = 5;

    [Header("Scene Names")]
    [SerializeField] private string gameSceneName = "GameScene";
    [SerializeField] private string settingsSceneName = "Settings";
    [SerializeField] private string howToPlaySceneName = "HowToPlay";

    [Header("Buttons")]
    [SerializeField] private UIButtonStateView continueButtonStateView;

    [Header("Board Selection")]
    [SerializeField] private GameObject mainMenu;
    [SerializeField] private GameObject boardDifficulties;

    [Header("Back Button")]
    [SerializeField] private float backButtonDoubleTapWindowSeconds = 1.5f;

    private float lastBackButtonTapTime = -999f;
    private int disabledContinueTapCount;
    private bool isDebugUnlockArmed;
    private RectTransform boardSizeSelectionRect;
    private TMP_Text boardSizeTitleText;
    private TMP_Text twoSuitsText;
    private TMP_Text redDragonText;
    private TMP_Text fullTilesText;
    private Button twoSuitsButton;
    private Button redDragonButton;
    private Button fullTilesButton;

    private void Awake()
    {
        ResolveBoardSizeSelectionReferences();
        WireBoardSizeSelectionButtons();
        SetBoardSizeSelectionVisible(false);
    }

    private void Start()
    {
        RefreshContinueButton();
        RefreshBoardSizeSelectionTexts();
    }

    private void OnEnable()
    {
        LocalizationManager.OnLanguageChanged += RefreshBoardSizeSelectionTexts;
    }

    private void OnDisable()
    {
        LocalizationManager.OnLanguageChanged -= RefreshBoardSizeSelectionTexts;
    }

    private void Update()
    {
        HandleBoardSizeSelectionOutsideClick();

        if (GameInput.IsBackPressedThisFrame())
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
        if (IsBoardSizeSelectionVisible())
        {
            CloseBoardSizeSelection();
            return;
        }

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

        if (!GameManager.HasSavedGameFile())
        {
            Debug.Log("Continue ignored: no saved board found.");
            return;
        }

        GameManager.RequestLoadSavedGameOnNextGameScene();
        SceneManager.LoadScene(gameSceneName);
    }

    private void RefreshContinueButton()
    {
        if (continueButtonStateView != null)
        {
            continueButtonStateView.SetAvailable(GameManager.HasSavedGameFile());
        }
    }

    public void OnClickPlay()
    {
        ResetDebugUnlockSequence();
        OpenBoardSizeSelection();
    }

    public void OnClickSettings()
    {
        ResetDebugUnlockSequence();
        SettingsMenuManager.SetReturnScene(SceneManager.GetActiveScene().name);
        SceneManager.LoadScene(settingsSceneName);
    }

    public void OnClickHowToPlay()
    {
        ResetDebugUnlockSequence();
        SceneManager.LoadScene(howToPlaySceneName);
    }

    public void OnClickQuit()
    {
        if (isDebugUnlockArmed && !GameManager.HasSavedGameFile())
        {
            DebugSettings.SetPersistentDebugMode(true);
            ResetDebugUnlockSequence();
            Debug.Log("Secret unlock activated. Debug mode enabled.");
            return;
        }

        ResetDebugUnlockSequence();
        Debug.Log("Quit button clicked.");

        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #else
        Application.Quit();
        #endif
    }

    public void OnDisabledContinueTapped()
    {
        if (GameManager.HasSavedGameFile())
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

    public void OnClickSelectEasy()
    {
        StartNewGameWithBoardSize(12, 6);
    }

    public void OnClickSelectNormal()
    {
        StartNewGameWithBoardSize(14, 8);
    }

    public void OnClickSelectHard()
    {
        StartNewGameWithBoardSize(17, 8);
    }

    public void CloseBoardSizeSelection()
    {
        SetBoardSizeSelectionVisible(false);
    }

    private void OpenBoardSizeSelection()
    {
        SetBoardSizeSelectionVisible(true);
        RefreshBoardSizeSelectionTexts();
    }

    private void StartNewGameWithBoardSize(int width, int height)
    {
        ResetDebugUnlockSequence();
        BoardManager.SetNextNewGameBoardSize(width, height);
        SceneManager.LoadScene(gameSceneName);
    }

    private bool IsBoardSizeSelectionVisible()
    {
        return boardDifficulties != null && boardDifficulties.activeSelf;
    }

    private void SetBoardSizeSelectionVisible(bool visible)
    {
        if (mainMenu != null)
        {
            mainMenu.SetActive(!visible);
        }

        if (boardDifficulties != null)
        {
            boardDifficulties.SetActive(visible);
        }
    }

    private void ResolveBoardSizeSelectionReferences()
    {
        if (mainMenu == null)
        {
            mainMenu = GameObject.Find("MenuContent");
        }

        if (boardDifficulties == null)
        {
            boardDifficulties = GameObject.Find("BoardDifficulties");
        }

        if (boardDifficulties == null)
        {
            return;
        }

        boardSizeTitleText = FindChildText(boardDifficulties.transform, "DifficultyTitle");
        boardSizeSelectionRect = boardDifficulties.GetComponent<RectTransform>();
        twoSuitsButton = FindChildButton(boardDifficulties.transform, "Easy");
        redDragonButton = FindChildButton(boardDifficulties.transform, "Normal");
        fullTilesButton = FindChildButton(boardDifficulties.transform, "Hard");
        twoSuitsText = FindButtonText(twoSuitsButton);
        redDragonText = FindButtonText(redDragonButton);
        fullTilesText = FindButtonText(fullTilesButton);
    }

    private void WireBoardSizeSelectionButtons()
    {
        AddBoardSizeButtonListener(twoSuitsButton, OnClickSelectEasy);
        AddBoardSizeButtonListener(redDragonButton, OnClickSelectNormal);
        AddBoardSizeButtonListener(fullTilesButton, OnClickSelectHard);
    }

    private void HandleBoardSizeSelectionOutsideClick()
    {
        if (!IsBoardSizeSelectionVisible() || boardSizeSelectionRect == null)
        {
            return;
        }

        if (TryGetPointerDownScreenPosition(out Vector2 screenPosition) &&
            !RectTransformUtility.RectangleContainsScreenPoint(boardSizeSelectionRect, screenPosition, null))
        {
            CloseBoardSizeSelection();
        }
    }

    private static bool TryGetPointerDownScreenPosition(out Vector2 screenPosition)
    {
        return GameInput.TryGetPointerDownPosition(out screenPosition);
    }

    private void RefreshBoardSizeSelectionTexts()
    {
        if (boardDifficulties == null)
        {
            ResolveBoardSizeSelectionReferences();
        }

        SetText(boardSizeTitleText, "main.boardSizeTitle");
        SetText(twoSuitsText, "main.boardSize.twoSuits");
        SetText(redDragonText, "main.boardSize.redDragon");
        SetText(fullTilesText, "main.boardSize.fullTiles");
    }

    private static void AddBoardSizeButtonListener(Button button, UnityEngine.Events.UnityAction action)
    {
        if (button == null || action == null)
        {
            return;
        }

        button.onClick.RemoveListener(action);
        button.onClick.AddListener(action);
    }

    private static TMP_Text FindChildText(Transform parent, string childName)
    {
        if (parent == null)
        {
            return null;
        }

        Transform child = parent.Find(childName);
        return child != null ? child.GetComponent<TMP_Text>() : null;
    }

    private static Button FindChildButton(Transform parent, string childName)
    {
        if (parent == null)
        {
            return null;
        }

        Transform child = parent.Find($"Buttons/{childName}");
        return child != null ? child.GetComponent<Button>() : null;
    }

    private static TMP_Text FindButtonText(Button button)
    {
        if (button == null)
        {
            return null;
        }

        return button.GetComponentInChildren<TMP_Text>(true);
    }

    private static void SetText(TMP_Text textComponent, string localizationKey)
    {
        if (textComponent != null)
        {
            LocalizationManager.ApplyFont(textComponent);
            textComponent.text = LocalizationManager.GetText(localizationKey);
        }
    }
}
