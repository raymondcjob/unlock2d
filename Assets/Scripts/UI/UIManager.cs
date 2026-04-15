using UnityEngine;

public class UIManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private BoardManager boardManager;
    [SerializeField] private BoardInteractionController boardInteractionController;
    [SerializeField] private DragPreviewController dragPreviewController;

    public void OnClickHome()
    {
        Debug.Log("Home button clicked.");
        // Later: SceneManager.LoadScene("MainMenu");
    }

    public void OnClickResetBoard()
    {
        if (boardInteractionController != null)
        {
            boardInteractionController.ForceClearInteractionState();
        }

        if (dragPreviewController != null)
        {
            dragPreviewController.ClearPreview();
        }

        if (boardManager != null)
        {
            boardManager.GenerateBoard();
        }
    }
}