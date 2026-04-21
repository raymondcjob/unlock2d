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
    [SerializeField] private DebugSettings debugSettings;

    [Header("Menu Overlay")]
    [SerializeField] private GameObject menuOverlayRoot;

    [Header("Item Counts")]
    [SerializeField] private GameObject undoBadgeRoot;
    [SerializeField] private TMP_Text undoCountText;
    private int undoUsesRemaining;
    [SerializeField] private GameObject shuffleBadgeRoot;
    [SerializeField] private TMP_Text shuffleCountText;
    private int shuffleUsesRemaining;
    [SerializeField] private GameObject swapBadgeRoot;
    [SerializeField] private TMP_Text swapCountText;
    private int swapUsesRemaining;

    [Header("No Moves Popup")]
    [SerializeField] private GameObject noMovesOverlayRoot;

    [Header("Win Popup")]
    [SerializeField] private GameObject winOverlayRoot;


    private void OnEnable()
    {
        if (boardManager != null)
        {
            boardManager.OnBoardWon += HandleBoardWon;
            boardManager.OnBoardGenerated += HandleBoardGenerated;
            boardManager.OnSwapPerformed += HandleSwapPerformed;
        }
    }

    private void OnDisable()
    {
        if (boardManager != null)
        {
            boardManager.OnBoardWon -= HandleBoardWon;
            boardManager.OnBoardGenerated -= HandleBoardGenerated;
            boardManager.OnSwapPerformed -= HandleSwapPerformed;
        }
    }

    private void Start()
    {
        SetMenuOverlayVisible(false);
        SetWinOverlayVisible(false);
        SetNoMovesOverlayVisible(false);

        ResetItemCountsForFreshBoard();
        RefreshUndoBadge();
        RefreshShuffleBadge();
        RefreshSwapBadge();
    }

    public void OnClickOpenMenu()
    {
        ClearBoardInteraction();
        SetMenuOverlayVisible(true);
    }

    public void OnClickCloseMenu()
    {
        SetMenuOverlayVisible(false);
    }

    public void OnClickMainMenu()
    {
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

        if (!HasUndoUseAvailable())
        {
            Debug.Log("No undo uses remaining.");
            return;
        }

        if (!boardManager.TryUndoLastMove())
        {
            Debug.Log("No moves available to undo.");
            return;
        }

        ConsumeUndoUse();
        RefreshUndoBadge();
    }

    private void HandleBoardGenerated()
    {
        ResetItemCountsForFreshBoard();
        RefreshUndoBadge();
        RefreshShuffleBadge();
        RefreshSwapBadge();
    }

    private void RefreshUndoBadge()
    {
        if (undoCountText != null)
        {
            undoCountText.text = IsDebugModeEnabled() ? "Free" : undoUsesRemaining.ToString();
        }

        if (undoBadgeRoot != null)
        {
            undoBadgeRoot.SetActive(true);
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

        if (!HasShuffleUseAvailable())
        {
            Debug.Log("No shuffle uses remaining.");
            return;
        }

        if (!boardManager.TryShuffleRemainingTiles())
        {
            Debug.Log("Shuffle failed.");
            return;
        }

        ConsumeShuffleUse();
        RefreshShuffleBadge();
        RefreshUndoBadge();
    }

    private void RefreshShuffleBadge()
    {
        if (shuffleCountText != null)
        {
            shuffleCountText.text = IsDebugModeEnabled() ? "Free" : shuffleUsesRemaining.ToString();
        }

        if (shuffleBadgeRoot != null)
        {
            shuffleBadgeRoot.SetActive(true);
        }
    }

    public void OnClickSwapMode()
    {
        SetMenuOverlayVisible(false);

        if (!HasSwapUseAvailable())
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
            swapCountText.text = IsDebugModeEnabled() ? "Free" : swapUsesRemaining.ToString();
        }

        if (swapBadgeRoot != null)
        {
            swapBadgeRoot.SetActive(true);
        }
    }

    private void HandleSwapPerformed()
    {
        ConsumeSwapUse();
        RefreshSwapBadge();
    }

    public void OnClickSettings()
    {
        // Implement Settings Overlay later
        
        if (boardInteractionController != null)
        {
            boardInteractionController.ToggleAutoHint();
        }

        SetMenuOverlayVisible(false);
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

    private void ResetItemCountsForFreshBoard()
    {
        undoUsesRemaining = 0;
        shuffleUsesRemaining = 0;
        swapUsesRemaining = 0;
    }

    private bool HasUndoUseAvailable()
    {
        return IsDebugModeEnabled() || undoUsesRemaining > 0;
    }

    private bool HasShuffleUseAvailable()
    {
        return IsDebugModeEnabled() || shuffleUsesRemaining > 0;
    }

    private bool HasSwapUseAvailable()
    {
        return IsDebugModeEnabled() || swapUsesRemaining > 0;
    }

    private void ConsumeUndoUse()
    {
        if (!IsDebugModeEnabled() && undoUsesRemaining > 0)
        {
            undoUsesRemaining--;
        }
    }

    private void ConsumeShuffleUse()
    {
        if (!IsDebugModeEnabled() && shuffleUsesRemaining > 0)
        {
            shuffleUsesRemaining--;
        }
    }

    private void ConsumeSwapUse()
    {
        if (!IsDebugModeEnabled() && swapUsesRemaining > 0)
        {
            swapUsesRemaining--;
        }
    }

    private bool IsDebugModeEnabled()
    {
        return debugSettings != null && debugSettings.DebugMode;
    }
}
