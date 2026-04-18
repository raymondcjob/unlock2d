using UnityEngine;
using TMPro;

public class UIManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private BoardManager boardManager;
    [SerializeField] private BoardInteractionController boardInteractionController;
    [SerializeField] private DragPreviewController dragPreviewController;

    [Header("Menu Overlay")]
    [SerializeField] private GameObject menuOverlayRoot;

    [Header("Item Counts")]
    [SerializeField] private int debugStartingUndoCount = 99;
    [SerializeField] private GameObject undoBadgeRoot;
    [SerializeField] private TMP_Text undoCountText;
    private int undoUsesRemaining;

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
        }
    }

    private void OnDisable()
    {
        if (boardManager != null)
        {
            boardManager.OnBoardWon -= HandleBoardWon;
            boardManager.OnBoardGenerated -= HandleBoardGenerated;
        }
    }

    private void Start()
    {
        SetMenuOverlayVisible(false);
        SetWinOverlayVisible(false);
        SetNoMovesOverlayVisible(false);

        ResetItemCountsForFreshBoard();
        RefreshUndoBadge();
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
        Debug.Log("Main Menu button clicked.");
        // Later: SceneManager.LoadScene("MainMenu");
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

        if (undoUsesRemaining <= 0)
        {
            Debug.Log("No undo uses remaining.");
            return;
        }

        if (!boardManager.TryUndoLastMove())
        {
            Debug.Log("No moves available to undo.");
            return;
        }

        undoUsesRemaining--;
        RefreshUndoBadge();
    }

    private void HandleBoardGenerated()
    {
        ResetItemCountsForFreshBoard();
        RefreshUndoBadge();
    }

    private void ResetItemCountsForFreshBoard()
    {
        undoUsesRemaining = debugStartingUndoCount;
    }

    private void RefreshUndoBadge()
    {
        if (undoCountText != null)
        {
            undoCountText.text = undoUsesRemaining.ToString();
        }

        if (undoBadgeRoot != null)
        {
            undoBadgeRoot.SetActive(undoUsesRemaining > 0);
        }
    }

    public void OnClickShuffle()
    {
        Debug.Log("Shuffle button clicked.");
        // Later: shuffle logic
    }

    public void OnClickSwapMode()
    {
        SetMenuOverlayVisible(false);

        if (boardInteractionController != null)
        {
            boardInteractionController.BeginSwapSelection();
        }
    }

    public void OnClickDebugMatchMode()
    {
        SetMenuOverlayVisible(false);

        if (boardInteractionController != null)
        {
            boardInteractionController.BeginDebugMatchSelection();
        }
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
}