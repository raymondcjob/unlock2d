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
    [SerializeField] private float moveCommitThresholdRatio = 0.5f;

    private TileView activeTile;
    private Vector3 pointerDownWorldPosition;
    private bool isPointerHeld;
    private bool isDragging;
    private bool isAwaitingMatchChoice;
    private Vector2Int currentDragDirection = Vector2Int.zero;
    private float currentPreviewDistance;

    private readonly List<TileView> enlargedTiles = new List<TileView>();
    private readonly List<TileView> currentMatchChoices = new List<TileView>();
    private readonly List<TileView> dragTintedTiles = new List<TileView>();

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

        if (clickedTile == null || clickedTile.IsPath)
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
        currentPreviewDistance = 0f;

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

        if (!isDragging || activeTile == null || activeTile.IsPath)
        {
            return;
        }

        Vector2Int direction = GetCardinalDirection(dragDelta);

        if (direction == Vector2Int.zero)
        {
            ClearDragVisuals();
            currentDragDirection = Vector2Int.zero;
            currentPreviewDistance = 0f;
            return;
        }

        currentDragDirection = direction;

        List<TileView> connectedTiles = boardManager.GetConnectedTilesInDirection(activeTile, currentDragDirection);
        int maxMoveSteps = boardManager.GetMaxMoveSteps(activeTile, connectedTiles, currentDragDirection);

        if (maxMoveSteps <= 0)
        {
            ClearDragVisuals();
            currentPreviewDistance = 0f;
            return;
        }

        float maxPreviewDistance = GetDistanceForSteps(currentDragDirection, maxMoveSteps);
        float previewDistance = GetPreviewDistanceFromPointer(dragDelta, currentDragDirection, maxPreviewDistance);

        currentPreviewDistance = previewDistance;

        ShowDragSourceTint(activeTile, connectedTiles);
        dragPreviewController.ShowPreview(activeTile, connectedTiles, currentDragDirection, previewDistance, boardManager);
    }

    private void OnPointerUp()
    {
        if (isDragging)
        {
            bool moveResolved = TryResolveDragMove();

            ClearInteractionVisuals();

            if (!moveResolved)
            {
                ResetInteractionState();
                return;
            }

            List<TileView> candidates = matchValidator.GetMatchCandidates(activeTile);

            if (candidates.Count == 1)
            {
                boardManager.ResolveMatch(activeTile, candidates[0]);
                ResetInteractionState();
                return;
            }

            if (candidates.Count > 1)
            {
                EnterMatchChoiceState(candidates);
                isPointerHeld = false;
                isDragging = false;
                currentDragDirection = Vector2Int.zero;
                currentPreviewDistance = 0f;
                return;
            }

            ResetInteractionState();
            return;
        }

        if (activeTile != null && matchValidator != null)
        {
            List<TileView> candidates = matchValidator.GetMatchCandidates(activeTile);

            ClearInteractionVisuals();

            if (candidates.Count == 1)
            {
                boardManager.ResolveMatch(activeTile, candidates[0]);
                ResetInteractionState();
                return;
            }

            if (candidates.Count > 1)
            {
                EnterMatchChoiceState(candidates);
                isPointerHeld = false;
                isDragging = false;
                currentDragDirection = Vector2Int.zero;
                currentPreviewDistance = 0f;
                return;
            }
        }
        else
        {
            ClearInteractionVisuals();
        }

        ResetInteractionState();
    }

    private bool TryResolveDragMove()
    {
        if (activeTile == null || activeTile.IsPath || currentDragDirection == Vector2Int.zero)
        {
            return false;
        }

        List<TileView> connectedTiles = boardManager.GetConnectedTilesInDirection(activeTile, currentDragDirection);
        int maxMoveSteps = boardManager.GetMaxMoveSteps(activeTile, connectedTiles, currentDragDirection);

        if (maxMoveSteps <= 0)
        {
            return false;
        }

        int chosenSteps = GetCommittedStepCount(currentDragDirection, currentPreviewDistance, maxMoveSteps);

        if (chosenSteps <= 0)
        {
            return false;
        }

        List<TileView> currentMatches = matchValidator.GetMatchCandidates(activeTile);

        List<TileView> movedGroup = new List<TileView> { activeTile };
        movedGroup.AddRange(connectedTiles);

        Vector2Int projectedPosition = activeTile.GridPosition + currentDragDirection * chosenSteps;

        List<TileView> projectedMatches = matchValidator.GetProjectedMatchCandidates(
            activeTile,
            projectedPosition,
            movedGroup,
            currentDragDirection,
            chosenSteps);

        if (projectedMatches.Count == 0)
        {
            return false;
        }

        bool createsNewMatch = false;

        foreach (TileView projectedMatch in projectedMatches)
        {
            if (!currentMatches.Contains(projectedMatch))
            {
                createsNewMatch = true;
                break;
            }
        }

        if (!createsNewMatch)
        {
            return false;
        }

        boardManager.MoveTileGroup(activeTile, connectedTiles, currentDragDirection, chosenSteps);
        return true;
    }

    private int GetCommittedStepCount(Vector2Int direction, float previewDistance, int maxMoveSteps)
    {
        float spacing = direction.x != 0
            ? boardManager.GetTileSpacingX()
            : boardManager.GetTileSpacingY();

        if (spacing <= 0f)
        {
            return 0;
        }

        int fullSteps = Mathf.FloorToInt(previewDistance / spacing);
        float remainder = previewDistance - fullSteps * spacing;

        if (remainder >= spacing * moveCommitThresholdRatio)
        {
            fullSteps++;
        }

        return Mathf.Clamp(fullSteps, 0, maxMoveSteps);
    }

    private void EnterMatchChoiceState(List<TileView> candidates)
    {
        isAwaitingMatchChoice = true;
        currentMatchChoices.Clear();
        currentMatchChoices.AddRange(candidates);

        if (activeTile != null)
        {
            activeTile.SetCustomScale(1.15f, 12);
            enlargedTiles.Add(activeTile);
        }

        foreach (TileView tile in currentMatchChoices)
        {
            if (tile != null)
            {
                tile.SetCustomScale(1.075f, 11);
                enlargedTiles.Add(tile);
            }
        }
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
            boardManager.ResolveMatch(activeTile, clickedTile);
            ExitMatchChoiceState();
            return;
        }

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

    private void ShowSameTypeEnlargement(TileView sourceTile)
    {
        ClearEnlargedTiles();

        if (sourceTile == null || sourceTile.IsPath)
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

    private void ShowDragSourceTint(TileView sourceTile, List<TileView> connectedTiles)
    {
        ClearDragSourceTint();

        if (sourceTile != null)
        {
            sourceTile.SetDragSourceTint(true);
            dragTintedTiles.Add(sourceTile);
        }

        foreach (TileView tile in connectedTiles)
        {
            if (tile != null)
            {
                tile.SetDragSourceTint(true);
                dragTintedTiles.Add(tile);
            }
        }
    }

    private void ClearDragSourceTint()
    {
        foreach (TileView tile in dragTintedTiles)
        {
            if (tile != null)
            {
                tile.SetDragSourceTint(false);
            }
        }

        dragTintedTiles.Clear();
    }

    private void ClearDragVisuals()
    {
        ClearDragSourceTint();

        if (dragPreviewController != null)
        {
            dragPreviewController.ClearPreview();
        }
    }

    private void ClearInteractionVisuals()
    {
        ClearEnlargedTiles();
        ClearDragSourceTint();

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

    public void ForceClearInteractionState()
    {
        ClearInteractionVisuals();
        currentMatchChoices.Clear();
        ResetInteractionState();
    }

    private void ResetInteractionState()
    {
        activeTile = null;
        isPointerHeld = false;
        isDragging = false;
        isAwaitingMatchChoice = false;
        currentDragDirection = Vector2Int.zero;
        currentPreviewDistance = 0f;
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
            return dragDelta.y > 0f ? Vector2Int.down : Vector2Int.up;
        }

        return Vector2Int.zero;
    }

    private float GetDistanceForSteps(Vector2Int direction, int steps)
    {
        float spacing = direction.x != 0
            ? boardManager.GetTileSpacingX()
            : boardManager.GetTileSpacingY();

        return spacing * steps;
    }

    private float GetPreviewDistanceFromPointer(Vector3 dragDelta, Vector2Int direction, float maxPreviewDistance)
    {
        float distance = direction.x != 0
            ? Mathf.Abs(dragDelta.x)
            : Mathf.Abs(dragDelta.y);

        return Mathf.Clamp(distance, 0f, maxPreviewDistance);
    }
}