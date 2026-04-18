using UnityEngine;

public class UIManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private BoardManager boardManager;
    [SerializeField] private BoardInteractionController boardInteractionController;
    [SerializeField] private DragPreviewController dragPreviewController;

    [Header("Menu Overlay")]
    [SerializeField] private GameObject menuOverlayRoot;

    [Header("Win Popup")]
    [SerializeField] private GameObject winOverlayRoot;

    [Header("No Moves Popup")]
    [SerializeField] private GameObject noMovesOverlayRoot;

    private void OnEnable()
    {
        if (boardManager != null)
        {
            boardManager.OnBoardWon += HandleBoardWon;
        }
    }

    private void OnDisable()
    {
        if (boardManager != null)
        {
            boardManager.OnBoardWon -= HandleBoardWon;
        }
    }

    private void Start()
    {
        SetMenuOverlayVisible(false);
        SetWinOverlayVisible(false);
        SetNoMovesOverlayVisible(false);
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
        Debug.Log("Undo button clicked.");
        // Later: undo logic
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