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

    [Header("Buttons")]
    [SerializeField] private UIButtonStateView continueButtonStateView;

    [Header("Board Size Selection")]
    [SerializeField] private GameObject boardSizeSelectionOverlay;

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
        OpenBoardSizeSelection();
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

    public void OnClickSelectTwoSuits()
    {
        StartNewGameWithBoardSize(12, 6);
    }

    public void OnClickSelectRedDragon()
    {
        StartNewGameWithBoardSize(14, 8);
    }

    public void OnClickSelectFullTiles()
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
        return boardSizeSelectionOverlay != null && boardSizeSelectionOverlay.activeSelf;
    }

    private void SetBoardSizeSelectionVisible(bool visible)
    {
        if (boardSizeSelectionOverlay != null)
        {
            boardSizeSelectionOverlay.SetActive(visible);
        }
    }

    private void ResolveBoardSizeSelectionReferences()
    {
        if (boardSizeSelectionOverlay == null)
        {
            GameObject overlayObject = GameObject.Find("BoardSizeSelection");
            if (overlayObject != null)
            {
                boardSizeSelectionOverlay = overlayObject;
            }
        }

        if (boardSizeSelectionOverlay == null)
        {
            return;
        }

        boardSizeTitleText = FindChildText(boardSizeSelectionOverlay.transform, "Title");
        boardSizeSelectionRect = boardSizeSelectionOverlay.GetComponent<RectTransform>();
        twoSuitsButton = FindChildButton(boardSizeSelectionOverlay.transform, "TwoSuits");
        redDragonButton = FindChildButton(boardSizeSelectionOverlay.transform, "RedDragon");
        fullTilesButton = FindChildButton(boardSizeSelectionOverlay.transform, "FullTiles");
        twoSuitsText = FindButtonText(twoSuitsButton);
        redDragonText = FindButtonText(redDragonButton);
        fullTilesText = FindButtonText(fullTilesButton);
    }

    private void WireBoardSizeSelectionButtons()
    {
        AddBoardSizeButtonListener(twoSuitsButton, OnClickSelectTwoSuits);
        AddBoardSizeButtonListener(redDragonButton, OnClickSelectRedDragon);
        AddBoardSizeButtonListener(fullTilesButton, OnClickSelectFullTiles);
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
        if (Input.touchCount > 0)
        {
            for (int i = 0; i < Input.touchCount; i++)
            {
                Touch touch = Input.GetTouch(i);
                if (touch.phase == TouchPhase.Began)
                {
                    screenPosition = touch.position;
                    return true;
                }
            }
        }

        if (Input.GetMouseButtonDown(0))
        {
            screenPosition = Input.mousePosition;
            return true;
        }

        screenPosition = default;
        return false;
    }

    private void RefreshBoardSizeSelectionTexts()
    {
        if (boardSizeSelectionOverlay == null)
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
            textComponent.text = LocalizationManager.GetText(localizationKey);
        }
    }
}
