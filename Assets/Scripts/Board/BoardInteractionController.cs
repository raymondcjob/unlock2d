using System.Collections.Generic;
using UnityEngine;

public class BoardInteractionController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera mainCamera;
    [SerializeField] private BoardManager boardManager;
    [SerializeField] private DragPreviewController dragPreviewController;
    [SerializeField] private MatchValidator matchValidator;

    [Header("Input Settings")]
    [SerializeField] private float dragThreshold = 0.2f;

    private TileView activeTile;
    private Vector3 pointerDownWorldPosition;
    private bool isPointerHeld;
    private bool isDragging;
    private bool isAwaitingMatchChoice;
    private Vector2Int currentDragDirection = Vector2Int.zero;

    private List<TileView> enlargedTiles = new List<TileView>();
    private List<TileView> currentMatchChoices = new List<TileView>();

    private void Update()
    {
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }

        HandleMouseInput();
    }

    private void HandleMouseInput()
    {
        if (Input.GetMouseButtonDown(0))
        {
            OnPointerDown();
        }

        if (Input.GetMouseButton(0) && isPointerHeld)
        {
            OnPointerHeld();
        }

        if (Input.GetMouseButtonUp(0) && isPointerHeld)
        {
            OnPointerUp();
        }
    }

    private void OnPointerDown()
    {
        Vector3 worldPosition = GetMouseWorldPosition();
        TileView clickedTile = GetTileUnderPointer(worldPosition);

        if (isAwaitingMatchChoice)
        {
            HandleMatchChoiceClick(clickedTile);
            return;
        }

        if (clickedTile == null)
        {
            ClearInteractionVisuals();
            ResetInteractionState();
            return;
        }

        activeTile = clickedTile;
        pointerDownWorldPosition = worldPosition;
        isPointerHeld = true;
        isDragging = false;
        currentDragDirection = Vector2Int.zero;

        ShowSameTypeEnlargement(activeTile);
    }

    private void OnPointerHeld()
    {
        Vector3 currentWorldPosition = GetMouseWorldPosition();
        Vector3 dragDelta = currentWorldPosition - pointerDownWorldPosition;

        if (!isDragging && dragDelta.magnitude >= dragThreshold)
        {
            isDragging = true;
        }

        if (!isDragging || activeTile == null)
        {
            return;
        }

        Vector2Int direction = GetCardinalDirection(dragDelta);

        if (direction == Vector2Int.zero)
        {
            dragPreviewController.ClearPreview();
            currentDragDirection = Vector2Int.zero;
            return;
        }

        if (direction != currentDragDirection)
        {
            currentDragDirection = direction;
        }

        List<TileView> connectedTiles = boardManager.GetConnectedTilesInDirection(activeTile, currentDragDirection);
        int maxPreviewSteps = GetMaxPreviewSteps(activeTile, connectedTiles, currentDragDirection);

        if (maxPreviewSteps <= 0)
        {
            dragPreviewController.ClearPreview();
            return;
        }

        int previewStep = GetPreviewStepFromPointer(dragDelta, currentDragDirection, maxPreviewSteps);
        dragPreviewController.ShowPreview(activeTile, connectedTiles, currentDragDirection, previewStep, boardManager);
    }

    private void OnPointerUp()
    {
        if (!isDragging && activeTile != null && matchValidator != null)
        {
            List<TileView> candidates = matchValidator.GetMatchCandidates(activeTile);

            ClearInteractionVisuals();

            if (candidates.Count == 0)
            {
                Debug.Log($"No valid matches");
            }
            else if (candidates.Count == 1)
            {
                Debug.Log($"Confirmed match: {activeTile.name} with {candidates[0].name}");
                ResetInteractionState();
                return;
            } else if (candidates.Count > 1)
            {
                EnterMatchChoiceState(candidates);
                isPointerHeld = false;
                isDragging = false;
                currentDragDirection = Vector2Int.zero;
                return;
            }
        } else
        {
            ClearInteractionVisuals();
        }

        ResetInteractionState();
    }

    private void ShowSameTypeEnlargement(TileView sourceTile)
    {
        ClearEnlargedTiles();

        if (sourceTile == null)
        {
            return;
        }

        List<TileView> sameTypeTiles = boardManager.GetTilesOfSameType(sourceTile.TileTypeId, sourceTile);

        foreach (TileView tile in sameTypeTiles)
        {
            tile.SetEnlarged(true);
            enlargedTiles.Add(tile);
        }
    }

    private void ClearInteractionVisuals()
    {
        ClearEnlargedTiles();

        if (dragPreviewController != null)
        {
            dragPreviewController.ClearPreview();
        }
    }

    private void ClearEnlargedTiles()
    {
        foreach (TileView tile in enlargedTiles)
        {
            if (tile != null)
            {
                tile.ResetVisual();
            }
        }

        enlargedTiles.Clear();
    }

    private void ResetInteractionState()
    {
        activeTile = null;
        isPointerHeld = false;
        isDragging = false;
        isAwaitingMatchChoice = false;
        currentDragDirection = Vector2Int.zero;
    }

    private TileView GetTileUnderPointer(Vector3 worldPosition)
    {
        Collider2D hit = Physics2D.OverlapPoint(worldPosition);

        if (hit == null)
        {
            return null;
        }

        return hit.GetComponent<TileView>();
    }

    private Vector3 GetMouseWorldPosition()
    {
        Vector3 screenPosition = Input.mousePosition;
        screenPosition.z = Mathf.Abs(mainCamera.transform.position.z);
        Vector3 worldPosition = mainCamera.ScreenToWorldPoint(screenPosition);
        worldPosition.z = 0f;
        return worldPosition;
    }

    private Vector2Int GetCardinalDirection(Vector3 dragDelta)
    {
        if (Mathf.Abs(dragDelta.x) > Mathf.Abs(dragDelta.y))
        {
            return dragDelta.x > 0f ? Vector2Int.right : Vector2Int.left;
        }

        if (Mathf.Abs(dragDelta.y) > 0f)
        {
            return dragDelta.y > 0f ? Vector2Int.up : Vector2Int.down;
        }

        return Vector2Int.zero;
    }

    private int GetMaxPreviewSteps(TileView sourceTile, List<TileView> connectedTiles, Vector2Int direction)
    {
        if (sourceTile == null || direction == Vector2Int.zero)
        {
            return 0;
        }

        TileView frontTile = connectedTiles.Count > 0
            ? connectedTiles[connectedTiles.Count - 1]
            : sourceTile;

        Vector2Int checkPosition = frontTile.GridPosition + direction;
        int steps = 0;

        while (boardManager.IsInsideBoard(checkPosition) &&
           boardManager.GetTileAt(checkPosition) == null)
        {
            steps++;
            checkPosition += direction;
        }

        return steps;
    }

    private int GetPreviewStepFromPointer(Vector3 dragDelta, Vector2Int direction, int maxPreviewSteps)
    {
        float distance = direction.x != 0
            ? Mathf.Abs(dragDelta.x)
            : Mathf.Abs(dragDelta.y);

        float spacing = direction.x != 0
            ? boardManager.GetTileSpacingX()
            : boardManager.GetTileSpacingY();

        int step = Mathf.Clamp(Mathf.FloorToInt(distance / spacing) + 1, 1, maxPreviewSteps);
        return step;
    }

    private void EnterMatchChoiceState(List<TileView> candidates)
    {
        isAwaitingMatchChoice = true;
        currentMatchChoices.Clear();
        currentMatchChoices.AddRange(candidates);

        if (activeTile != null)
        {
            activeTile.SetCustomScale(1.15f, 3);
            enlargedTiles.Add(activeTile);
        }

        foreach (TileView tile in currentMatchChoices)
        {
            if (tile != null)
            {
                tile.SetCustomScale(1.075f, 2);
                enlargedTiles.Add(tile);
            }
        }

        Debug.Log($"Awaiting match choice for {activeTile.name}. Valid choices: {currentMatchChoices.Count}");
    }

    private void HandleMatchChoiceClick(TileView clickedTile)
    {
        if (activeTile == null)
        {
            CancelMatchChoice();
            return;
        }

        if (clickedTile != null && currentMatchChoices.Contains(clickedTile))
        {
            Debug.Log($"Confirmed match: {activeTile.name} with {clickedTile.name}");
            ExitMatchChoiceState();
            return;
        }

        Debug.Log("Match choice cancelled.");
        CancelMatchChoice();
    }

    private void CancelMatchChoice()
    {
        ExitMatchChoiceState();
    }

    private void ExitMatchChoiceState()
    {
        ClearInteractionVisuals();
        currentMatchChoices.Clear();
        ResetInteractionState();
    }

    public void ForceClearInteractionState()
    {
        ClearInteractionVisuals();
        currentMatchChoices.Clear();
        ResetInteractionState();
    }

}