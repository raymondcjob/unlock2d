using UnityEngine;

public class UIManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private BoardManager boardManager;
    [SerializeField] private BoardInteractionController boardInteractionController;
    [SerializeField] private DragPreviewController dragPreviewController;

    [Header("Win Popup")]
    [SerializeField] private GameObject winOverlayRoot;

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
        SetWinOverlayVisible(false);
    }

    public void OnClickHome()
    {
        Debug.Log("Home button clicked.");
        // Later: SceneManager.LoadScene("MainMenu");
    }

    public void OnClickResetBoard()
    {
        SetWinOverlayVisible(false);
        ClearBoardInteraction();

        if (boardManager != null)
        {
            boardManager.GenerateNewBoard();
        }
    }

    public void OnClickNextGame()
    {
        SetWinOverlayVisible(false);
        ClearBoardInteraction();

        if (boardManager != null)
        {
            boardManager.GenerateNewBoard();
        }
    }

    public void OnClickReplay()
    {
        SetWinOverlayVisible(false);
        ClearBoardInteraction();

        if (boardManager != null)
        {
            boardManager.ReplayCurrentBoard();
        }
    }

    private void HandleBoardWon(int seed)
    {
        Debug.Log($"Showing win popup for seed: {seed}");
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

    private void SetWinOverlayVisible(bool visible)
    {
        if (winOverlayRoot != null)
        {
            winOverlayRoot.SetActive(visible);
        }
    }
}