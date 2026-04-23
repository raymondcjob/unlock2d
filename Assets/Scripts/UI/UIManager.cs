using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class UIManager : MonoBehaviour
{
    [Header("Scene Names")]
    [SerializeField] private string mainMenuSceneName = "MainMenu";

    [Header("References")]
    [SerializeField] private BoardManager boardManager;
    [SerializeField] private BoardInteractionController boardInteractionController;
    [SerializeField] private DragPreviewController dragPreviewController;
    [SerializeField] private ItemInventory itemInventory;
    [SerializeField] private SaveGameManager saveGameManager;

    [Header("Menu Overlay")]
    [SerializeField] private GameObject menuOverlayRoot;

    [Header("Item Counts")]
    [SerializeField] private UIButtonStateView undoButtonStateView;
    [SerializeField] private GameObject undoBadgeRoot;
    [SerializeField] private TMP_Text undoCountText;
    [SerializeField] private UIButtonStateView shuffleButtonStateView;
    [SerializeField] private GameObject shuffleBadgeRoot;
    [SerializeField] private TMP_Text shuffleCountText;
    [SerializeField] private UIButtonStateView swapButtonStateView;
    [SerializeField] private GameObject swapBadgeRoot;
    [SerializeField] private TMP_Text swapCountText;

    [Header("No Moves Popup")]
    [SerializeField] private GameObject noMovesOverlayRoot;

    [Header("Win Popup")]
    [SerializeField] private GameObject winOverlayRoot;

    [Header("Top Bar")]
    [SerializeField] private TMP_Text timerText;

    private bool lastDebugMode;
    private bool isShuffleAvailableForCurrentBoard;
    private bool ignoreNextStableBoardStateChanged;
    private float elapsedSeconds;
    private int lastDisplayedSecond = -1;
    private bool isTimerRunning;

    private void OnEnable()
    {
        if (boardManager != null)
        {
            boardManager.OnBoardWon += HandleBoardWon;
            boardManager.OnBoardGenerated += HandleBoardGenerated;
            boardManager.OnSwapPerformed += HandleSwapPerformed;
            boardManager.OnStableBoardStateChanged += HandleStableBoardStateChanged;
        }

        if (itemInventory != null)
        {
            itemInventory.OnInventoryChanged += HandleInventoryChanged;
        }
    }

    private void OnDisable()
    {
        if (boardManager != null)
        {
            boardManager.OnBoardWon -= HandleBoardWon;
            boardManager.OnBoardGenerated -= HandleBoardGenerated;
            boardManager.OnSwapPerformed -= HandleSwapPerformed;
            boardManager.OnStableBoardStateChanged -= HandleStableBoardStateChanged;
        }

        if (itemInventory != null)
        {
            itemInventory.OnInventoryChanged -= HandleInventoryChanged;
        }
    }

    private void Start()
    {
        SetMenuOverlayVisible(false);
        SetWinOverlayVisible(false);
        SetNoMovesOverlayVisible(false);

        RefreshUndoBadge();
        RefreshShuffleBadge();
        RefreshSwapBadge();
        lastDebugMode = IsInventoryDebugModeEnabled();
        RestartTimer();
    }

    private void Update()
    {
        UpdateTimer();

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            HandleBackButton();
        }

        if (lastDebugMode != IsInventoryDebugModeEnabled())
        {
            lastDebugMode = IsInventoryDebugModeEnabled();
            RefreshItemBadges();
        }
    }

    public void OnClickOpenMenu()
    {
        ClearBoardInteraction();
        PauseTimer();
        SetMenuOverlayVisible(true);
        saveGameManager?.SaveGameIfAllowed();
    }

    public void OnClickCloseMenu()
    {
        SetMenuOverlayVisible(false);
        ResumeTimer();
    }

    private void HandleBackButton()
    {
        if (IsMenuOverlayVisible())
        {
            OnClickCloseMenu();
            return;
        }

        if (IsNoMovesOverlayVisible())
        {
            SetNoMovesOverlayVisible(false);
            return;
        }

        if (IsWinOverlayVisible())
        {
            return;
        }

        if (boardInteractionController != null && boardInteractionController.HasActiveInteraction())
        {
            boardInteractionController.ForceClearInteractionState();
            return;
        }

        OnClickOpenMenu();
    }

    public void OnClickMainMenu()
    {
        saveGameManager?.SaveGameIfAllowed();
        SetMenuOverlayVisible(false);
        SetWinOverlayVisible(false);
        SetNoMovesOverlayVisible(false);
        ClearBoardInteraction();

        SceneManager.LoadScene(mainMenuSceneName);
    }

    public void OnClickNewBoard()
    {
        SetMenuOverlayVisible(false);
        SetWinOverlayVisible(false);
        SetNoMovesOverlayVisible(false);
        ClearBoardInteraction();

        if (boardManager != null)
        {
            boardManager.GenerateNewBoard();
        }
    }

    public void OnClickRestart()
    {
        SetMenuOverlayVisible(false);
        SetWinOverlayVisible(false);
        SetNoMovesOverlayVisible(false);
        ClearBoardInteraction();

        if (boardManager != null)
        {
            boardManager.ReplayCurrentBoard();
        }
    }

    public void OnClickStore()
    {
        saveGameManager?.SaveGameIfAllowed();
        Debug.Log("Store button clicked.");
        // Later: open store overlay / panel
    }

    public void OnClickUndo()
    {
        SetMenuOverlayVisible(false);
        SetWinOverlayVisible(false);
        SetNoMovesOverlayVisible(false);
        ClearBoardInteraction();

        if (boardManager == null)
        {
            return;
        }

        if (!CanUseUndo())
        {
            Debug.Log("No undo uses remaining.");
            return;
        }

        if (!boardManager.TryUndoLastMove())
        {
            Debug.Log("No moves available to undo.");
            return;
        }
    }

    private void HandleBoardGenerated()
    {
        isShuffleAvailableForCurrentBoard = false;
        ignoreNextStableBoardStateChanged = true;
        itemInventory?.ResetForFreshBoard();
        RefreshItemBadges();
        RestartTimer();
    }

    private void HandleStableBoardStateChanged()
    {
        if (ignoreNextStableBoardStateChanged)
        {
            ignoreNextStableBoardStateChanged = false;
            RefreshItemBadges();
            return;
        }

        isShuffleAvailableForCurrentBoard = true;
        RefreshItemBadges();
    }

    private void HandleInventoryChanged()
    {
        RefreshItemBadges();
    }

    private void RefreshUndoBadge()
    {
        if (undoCountText != null)
        {
            undoCountText.text = itemInventory != null ? itemInventory.GetUndoDisplayText() : "0";
        }

        if (undoBadgeRoot != null)
        {
            undoBadgeRoot.SetActive(true);
        }

        if (undoButtonStateView != null)
        {
            undoButtonStateView.SetAvailable(CanUseUndo());
        }
    }

    public void OnClickShuffle()
    {
        SetMenuOverlayVisible(false);
        SetWinOverlayVisible(false);
        SetNoMovesOverlayVisible(false);
        ClearBoardInteraction();

        if (boardManager == null)
        {
            return;
        }

        if (!CanUseShuffle())
        {
            Debug.Log("No shuffle uses remaining.");
            return;
        }

        boardManager.RecordUndoSnapshot();

        if (!boardManager.TryShuffleRemainingTiles())
        {
            boardManager.DiscardLastUndoSnapshot();
            Debug.Log("Shuffle failed.");
            return;
        }

        itemInventory.ConsumeShuffleUse();
        RefreshShuffleBadge();
        RefreshUndoBadge();
    }

    private void RefreshShuffleBadge()
    {
        if (shuffleCountText != null)
        {
            shuffleCountText.text = itemInventory != null ? itemInventory.GetShuffleDisplayText() : "0";
        }

        if (shuffleBadgeRoot != null)
        {
            shuffleBadgeRoot.SetActive(true);
        }

        if (shuffleButtonStateView != null)
        {
            shuffleButtonStateView.SetAvailable(CanUseShuffle());
        }
    }

    public void OnClickSwapMode()
    {
        SetMenuOverlayVisible(false);

        if (itemInventory == null || !itemInventory.HasSwapUseAvailable())
        {
            Debug.Log("No swap uses remaining.");
            return;
        }

        if (boardInteractionController != null)
        {
            boardInteractionController.BeginSwapSelection();
        }
    }

    private void RefreshSwapBadge()
    {
        if (swapCountText != null)
        {
            swapCountText.text = itemInventory != null ? itemInventory.GetSwapDisplayText() : "0";
        }

        if (swapBadgeRoot != null)
        {
            swapBadgeRoot.SetActive(true);
        }

        if (swapButtonStateView != null)
        {
            swapButtonStateView.SetAvailable(itemInventory != null && itemInventory.HasSwapUseAvailable());
        }
    }

    private void HandleSwapPerformed()
    {
        isShuffleAvailableForCurrentBoard = true;
        itemInventory?.ConsumeSwapUse();
        RefreshItemBadges();
    }

    public void OnClickSettings()
    {
        // Implement Settings Overlay later
        
        if (boardInteractionController != null)
        {
            boardInteractionController.ToggleAutoHint();
        }

        SetMenuOverlayVisible(false);
        ResumeTimer();
    }

    public void OnClickCancelSelectionMode()
    {
        if (boardInteractionController != null)
        {
            boardInteractionController.CancelSelectionMode();
        }
    }

    public void ShowNoMovesOverlay()
    {
        SetNoMovesOverlayVisible(true);
    }

    public void HideNoMovesOverlay()
    {
        SetNoMovesOverlayVisible(false);
    }

    public bool DismissTransientOverlays()
    {
        bool dismissed = false;

        if (IsNoMovesOverlayVisible())
        {
            SetNoMovesOverlayVisible(false);
            dismissed = true;
        }

        return dismissed;
    }

    public bool IsModalOverlayVisible()
    {
        return IsMenuOverlayVisible() || IsWinOverlayVisible() || IsNoMovesOverlayVisible();
    }

    public bool IsMenuOverlayVisible()
    {
        return menuOverlayRoot != null && menuOverlayRoot.activeSelf;
    }

    public bool IsWinOverlayVisible()
    {
        return winOverlayRoot != null && winOverlayRoot.activeSelf;
    }

    public bool IsNoMovesOverlayVisible()
    {
        return noMovesOverlayRoot != null && noMovesOverlayRoot.activeSelf;
    }

    private void HandleBoardWon(int seed)
    {
        Debug.Log($"Showing win popup for seed: {seed}");
        isTimerRunning = false;
        SetMenuOverlayVisible(false);
        SetNoMovesOverlayVisible(false);
        SetWinOverlayVisible(true);
    }

    private void ClearBoardInteraction()
    {
        if (boardInteractionController != null)
        {
            boardInteractionController.ForceClearInteractionState();
        }

        if (dragPreviewController != null)
        {
            dragPreviewController.ClearPreview();
        }
    }

    private void SetMenuOverlayVisible(bool visible)
    {
        if (menuOverlayRoot != null)
        {
            menuOverlayRoot.SetActive(visible);
        }
    }

    private void SetWinOverlayVisible(bool visible)
    {
        if (winOverlayRoot != null)
        {
            winOverlayRoot.SetActive(visible);
        }
    }

    private void SetNoMovesOverlayVisible(bool visible)
    {
        if (noMovesOverlayRoot != null)
        {
            noMovesOverlayRoot.SetActive(visible);
        }
    }

    private void RefreshItemBadges()
    {
        RefreshUndoBadge();
        RefreshShuffleBadge();
        RefreshSwapBadge();
    }

    private bool CanUseUndo()
    {
        return itemInventory != null &&
               itemInventory.HasUndoUseAvailable() &&
               boardManager != null &&
               boardManager.GetUndoHistoryCount() > 0;
    }

    private bool CanUseShuffle()
    {
        return itemInventory != null &&
               itemInventory.HasShuffleUseAvailable() &&
               isShuffleAvailableForCurrentBoard &&
               boardManager != null &&
               boardManager.HasFaceDownTiles();
    }

    private bool IsInventoryDebugModeEnabled()
    {
        return itemInventory != null && itemInventory.IsDebugModeEnabled();
    }

    private void RestartTimer()
    {
        elapsedSeconds = 0f;
        lastDisplayedSecond = -1;
        isTimerRunning = true;
        RefreshTimerText(forceRefresh: true);
    }

    private void PauseTimer()
    {
        isTimerRunning = false;
        RefreshTimerText(forceRefresh: true);
    }

    private void ResumeTimer()
    {
        if (IsWinOverlayVisible())
        {
            return;
        }

        isTimerRunning = true;
        RefreshTimerText(forceRefresh: true);
    }

    private void UpdateTimer()
    {
        if (!isTimerRunning)
        {
            return;
        }

        elapsedSeconds += Time.unscaledDeltaTime;
        RefreshTimerText(forceRefresh: false);
    }

    private void RefreshTimerText(bool forceRefresh)
    {
        if (timerText == null)
        {
            return;
        }

        int totalSeconds = Mathf.Max(0, Mathf.FloorToInt(elapsedSeconds));

        if (!forceRefresh && totalSeconds == lastDisplayedSecond)
        {
            return;
        }

        lastDisplayedSecond = totalSeconds;

        int hours = totalSeconds / 3600;
        int minutes = (totalSeconds / 60) % 60;
        int seconds = totalSeconds % 60;

        timerText.text = hours > 0
            ? $"{hours:00}:{minutes:00}:{seconds:00}"
            : $"{minutes:00}:{seconds:00}";
    }

    public float GetElapsedSeconds()
    {
        return elapsedSeconds;
    }

    public bool IsTimerRunning()
    {
        return isTimerRunning;
    }

    public void RestoreTimerState(float savedElapsedSeconds, bool savedIsRunning)
    {
        elapsedSeconds = Mathf.Max(0f, savedElapsedSeconds);
        isTimerRunning = savedIsRunning && !IsWinOverlayVisible();
        lastDisplayedSecond = -1;
        RefreshTimerText(forceRefresh: true);
    }
}
