using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections;

public class UIManager : MonoBehaviour
{
    public Func<bool> ShuffleOverride;

    [Header("Scene Names")]
    [SerializeField] private string mainMenuSceneName = "MainMenu";
    [SerializeField] private string settingsSceneName = "Settings";

    [Header("References")]
    [SerializeField] private BoardManager boardManager;
    [SerializeField] private BoardInteractionController boardInteractionController;
    [SerializeField] private DragPreviewController dragPreviewController;
    [SerializeField] private ItemInventory itemInventory;
    [SerializeField] private GameManager gameManager;

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

    [Header("No Match Popup")]
    [SerializeField] private GameObject noMatchPopupRoot;
    [SerializeField] private float noMatchPopupVisibleDurationSeconds = 3f;
    [SerializeField] private float noMatchPopupFadeDurationSeconds = 1f;

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
    private Coroutine hideNoMatchPopupCoroutine;
    private CanvasGroup noMatchPopupCanvasGroup;

    public event Action UndoApplied;
    public event Action ShuffleApplied;
    public event Action SwapModeStarted;

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
        EnsureGameManagerReference();
        SetMenuOverlayVisible(false);
        SetWinOverlayVisible(false);
        SetNoMatchPopupVisible(false);

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
        gameManager?.SaveGameIfAllowed();
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
        EnsureGameManagerReference();
        gameManager?.ReturnToMainMenu();
    }

    public void OnClickNewBoard()
    {
        EnsureGameManagerReference();
        gameManager?.StartNewBoard();
    }

    public void OnClickRestart()
    {
        EnsureGameManagerReference();
        gameManager?.RestartBoard();
    }

    public void OnClickStore()
    {
        EnsureGameManagerReference();
        gameManager?.OpenStore();
    }

    public void OnClickUndo()
    {
        SetMenuOverlayVisible(false);
        SetWinOverlayVisible(false);
        SetNoMatchPopupVisible(false);
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

        UndoApplied?.Invoke();
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
        SetNoMatchPopupVisible(false);
        ClearBoardInteraction();

        if (ShuffleOverride != null && ShuffleOverride())
        {
            return;
        }

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
        ShuffleApplied?.Invoke();
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
            SwapModeStarted?.Invoke();
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
        EnsureGameManagerReference();
        gameManager?.OpenSettingsScene();
    }

    public void PrepareForSceneTransition()
    {
        SetMenuOverlayVisible(false);
        SetWinOverlayVisible(false);
        SetNoMatchPopupVisible(false);
        ClearBoardInteraction();
    }

    public void PrepareForBoardReset()
    {
        PrepareForSceneTransition();
    }

    public void OnClickCancelSelectionMode()
    {
        if (boardInteractionController != null)
        {
            boardInteractionController.CancelSelectionMode();
        }
    }

    public void ShowNoMatchPopup()
    {
        ConfigureNoMatchPopupForPassiveDisplay();
        ResetNoMatchPopupVisualState();
        SetNoMatchPopupVisible(true);

        if (hideNoMatchPopupCoroutine != null)
        {
            StopCoroutine(hideNoMatchPopupCoroutine);
        }

        hideNoMatchPopupCoroutine = StartCoroutine(HideNoMatchPopupAfterDelay());
    }

    public void HideNoMatchPopup()
    {
        if (hideNoMatchPopupCoroutine != null)
        {
            StopCoroutine(hideNoMatchPopupCoroutine);
            hideNoMatchPopupCoroutine = null;
        }

        SetNoMatchPopupVisible(false);
    }

    public bool DismissTransientOverlays()
    {
        bool dismissed = false;

        if (IsNoMatchPopupVisible())
        {
            SetNoMatchPopupVisible(false);
            dismissed = true;
        }

        return dismissed;
    }

    public bool IsModalOverlayVisible()
    {
        return IsMenuOverlayVisible() || IsWinOverlayVisible();
    }

    public bool IsMenuOverlayVisible()
    {
        return menuOverlayRoot != null && menuOverlayRoot.activeSelf;
    }

    public bool IsWinOverlayVisible()
    {
        return winOverlayRoot != null && winOverlayRoot.activeSelf;
    }

    public bool IsNoMatchPopupVisible()
    {
        return noMatchPopupRoot != null && noMatchPopupRoot.activeSelf;
    }

    private void HandleBoardWon(int seed)
    {
        Debug.Log($"Showing win popup for seed: {seed}");
        isTimerRunning = false;
        SetMenuOverlayVisible(false);
        SetNoMatchPopupVisible(false);
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

    private void SetNoMatchPopupVisible(bool visible)
    {
        if (noMatchPopupRoot != null)
        {
            noMatchPopupRoot.SetActive(visible);
        }

        if (!visible)
        {
            ResetNoMatchPopupVisualState();
            hideNoMatchPopupCoroutine = null;
        }
    }

    private IEnumerator HideNoMatchPopupAfterDelay()
    {
        yield return new WaitForSecondsRealtime(noMatchPopupVisibleDurationSeconds);

        if (noMatchPopupCanvasGroup != null)
        {
            float duration = Mathf.Max(0.01f, noMatchPopupFadeDurationSeconds);
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                noMatchPopupCanvasGroup.alpha = Mathf.Lerp(1f, 0f, elapsed / duration);
                yield return null;
            }

            noMatchPopupCanvasGroup.alpha = 0f;
        }

        hideNoMatchPopupCoroutine = null;
        SetNoMatchPopupVisible(false);
    }

    private void ConfigureNoMatchPopupForPassiveDisplay()
    {
        if (noMatchPopupRoot == null)
        {
            return;
        }

        CanvasGroup canvasGroup = noMatchPopupRoot.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = noMatchPopupRoot.AddComponent<CanvasGroup>();
        }

        noMatchPopupCanvasGroup = canvasGroup;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;

        Graphic[] graphics = noMatchPopupRoot.GetComponentsInChildren<Graphic>(true);
        for (int i = 0; i < graphics.Length; i++)
        {
            graphics[i].raycastTarget = false;
        }
    }

    private void ResetNoMatchPopupVisualState()
    {
        if (noMatchPopupCanvasGroup != null)
        {
            noMatchPopupCanvasGroup.alpha = 1f;
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

    private void EnsureGameManagerReference()
    {
        if (gameManager != null)
        {
            gameManager.InitializeIfNeeded(boardManager, itemInventory, this, mainMenuSceneName, settingsSceneName);
        }
    }
}
