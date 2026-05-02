using System;
using System.Collections.Generic;
using UnityEngine;

public class BoardInteractionController : MonoBehaviour
{
    private const float TutorialClickEnlargementScale = 1.15f;
    private const string AutoHintEnabledKey = "settings.autoHintEnabled";
    private const int AutoHintFlashCyclesBeforeAdvance = 4;

    public event Action InteractionVisualsCleared;

    private enum SelectionMode
    {
        None,
        Swap,
        Match
    }

    [Header("References")]
    [SerializeField] private Camera mainCamera;
    [SerializeField] private BoardManager boardManager;
    [SerializeField] private DragPreviewController dragPreviewController;
    [SerializeField] private MatchValidator matchValidator;
    [SerializeField] private UIManager uiManager;

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

    [Header("Hint Settings")]
    [SerializeField] private bool autoHintEnabled = true;
    [SerializeField] private float autoHintDelaySeconds = 3f;

    private readonly List<TileView> enlargedTiles = new List<TileView>();
    private readonly List<TileView> currentMatchChoices = new List<TileView>();
    private readonly List<TileView> dragTintedTiles = new List<TileView>();
    private readonly List<TileView> dragCreatedMatchChoices = new List<TileView>();

    private bool isAwaitingDragSelectionCancelRestore;
    private readonly List<TileView> pendingMovedTiles = new List<TileView>();
    private Vector2Int pendingMoveOriginalActivePosition;
    private Vector2Int pendingMoveDirection = Vector2Int.zero;
    private int pendingMoveSteps;
    private Func<TileView, bool> tutorialInteractionFilter;
    private Func<TileView, bool> tutorialSelectionFilter;
    private Func<TileView, bool> tutorialEnlargementFilter;
    private bool suppressSameTypeEnlargement;

    private float autoHintTimer;
    private readonly List<TileView> autoHintTiles = new List<TileView>();
    private readonly List<BoardMoveAnalyzer.HintResult> cachedAutoHints = new List<BoardMoveAnalyzer.HintResult>();
    private int nextAutoHintIndex;
    private bool isAutoHintCacheDirty = true;
    private BoardMoveAnalyzer.HintResult currentAutoHint;
    private bool hasCurrentAutoHint;
    private int currentAutoHintFlashCount;
    private bool currentAutoHintDirectionTopLeftToBottomRight;
    private TileView currentAutoHintTrackedTile;


    private SelectionMode selectionMode = SelectionMode.None;
    private TileView selectedTileA;

    private void OnEnable()
    {
        if (boardManager != null)
        {
            boardManager.OnStableBoardStateChanged += HandleStableBoardStateChanged;
            boardManager.OnTilesMatched += HandleTilesMatched;
            boardManager.OnSwapPerformed += HandleSwapPerformed;
        }
    }

    private void OnDisable()
    {
        if (boardManager != null)
        {
            boardManager.OnStableBoardStateChanged -= HandleStableBoardStateChanged;
            boardManager.OnTilesMatched -= HandleTilesMatched;
            boardManager.OnSwapPerformed -= HandleSwapPerformed;
        }
    }

    private void Start()
    {
        autoHintEnabled = PlayerPrefs.GetInt(AutoHintEnabledKey, 1) == 1;
        ResetAutoHintTimer();
    }

    private void Update()
    {
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }

        if (GameInput.GetPressedTouchCount() > 1 && HasActiveInteraction())
        {
            ForceClearInteractionState();
            return;
        }

        HandlePointerInput();
        TickAutoHint(Time.deltaTime);
    }

    public bool HasActiveInteraction()
    {
        return selectionMode != SelectionMode.None ||
               isPointerHeld ||
               isDragging ||
               isAwaitingMatchChoice ||
               activeTile != null;
    }

    private void TickAutoHint(float deltaTime)
    {
        if (!autoHintEnabled)
        {
            return;
        }

        if (boardManager == null || boardManager.GetRemainingFaceUpTiles() <= 0)
        {
            return;
        }

        if (isPointerHeld || isDragging || isAwaitingMatchChoice || selectionMode != SelectionMode.None)
        {
            return;
        }

        if (uiManager != null && uiManager.IsModalOverlayVisible())
        {
            return;
        }

        if (autoHintTiles.Count > 0)
        {
            return;
        }

        autoHintTimer += deltaTime;

        if (autoHintTimer < autoHintDelaySeconds)
        {
            return;
        }

        autoHintTimer = 0f;

        EnsureAutoHintCache();

        if (TryGetCurrentOrNextAutoHint(out BoardMoveAnalyzer.HintResult hint))
        {
            ApplyAutoHint(hint);
        }
    }

    private void ApplyAutoHint(BoardMoveAnalyzer.HintResult hint)
    {
        ClearAutoHint();
        TileView tileA = hint.SourceTile;
        TileView tileB = hint.TargetTile;

        if (tileA != null)
        {
            tileA.PlayHintFlash(currentAutoHintDirectionTopLeftToBottomRight);
            autoHintTiles.Add(tileA);
        }

        if (tileB != null && tileB != tileA)
        {
            tileB.PlayHintFlash(currentAutoHintDirectionTopLeftToBottomRight);
            autoHintTiles.Add(tileB);
        }

        TrackCurrentAutoHintTile(tileA != null ? tileA : tileB);
    }

    private void ClearAutoHint()
    {
        if (currentAutoHintTrackedTile != null)
        {
            currentAutoHintTrackedTile.HintFlashCycleCompleted -= HandleAutoHintFlashCycleCompleted;
            currentAutoHintTrackedTile = null;
        }

        foreach (TileView tile in autoHintTiles)
        {
            if (tile != null)
            {
                tile.ResetVisual();
            }
        }

        autoHintTiles.Clear();
    }

    private void ResetAutoHintTimer()
    {
        autoHintTimer = 0f;
    }

    private void EnsureAutoHintCache()
    {
        if (!isAutoHintCacheDirty)
        {
            return;
        }

        cachedAutoHints.Clear();
        nextAutoHintIndex = 0;
        isAutoHintCacheDirty = false;

        if (boardManager == null)
        {
            return;
        }

        cachedAutoHints.AddRange(BoardMoveAnalyzer.GetAllHints(boardManager));
        ShuffleAutoHints(cachedAutoHints);
    }

    private bool TryGetNextAutoHint(out BoardMoveAnalyzer.HintResult hint)
    {
        hint = default;

        if (cachedAutoHints.Count == 0)
        {
            return false;
        }

        if (nextAutoHintIndex < 0 || nextAutoHintIndex >= cachedAutoHints.Count)
        {
            nextAutoHintIndex = 0;
        }

        hint = cachedAutoHints[nextAutoHintIndex];
        nextAutoHintIndex = (nextAutoHintIndex + 1) % cachedAutoHints.Count;
        return true;
    }

    private bool TryGetCurrentOrNextAutoHint(out BoardMoveAnalyzer.HintResult hint)
    {
        hint = default;

        if (hasCurrentAutoHint && currentAutoHintFlashCount < AutoHintFlashCyclesBeforeAdvance)
        {
            hint = currentAutoHint;
            return hint.SourceTile != null || hint.TargetTile != null;
        }

        if (!TryGetNextAutoHint(out hint))
        {
            return false;
        }

        hasCurrentAutoHint = true;
        currentAutoHint = hint;
        currentAutoHintFlashCount = 0;
        currentAutoHintDirectionTopLeftToBottomRight = UnityEngine.Random.value >= 0.5f;
        return true;
    }

    private void ResetAutoHintCycle()
    {
        ClearAutoHint();
        ResetAutoHintTimer();
        cachedAutoHints.Clear();
        nextAutoHintIndex = 0;
        isAutoHintCacheDirty = true;
        currentAutoHint = default;
        hasCurrentAutoHint = false;
        currentAutoHintFlashCount = 0;
        currentAutoHintDirectionTopLeftToBottomRight = false;
    }

    private void HandleTilesMatched(TileView _, TileView __)
    {
        ResetAutoHintCycle();
    }

    private void HandleSwapPerformed()
    {
        ResetAutoHintCycle();
    }

    private void TrackCurrentAutoHintTile(TileView tile)
    {
        if (tile == null)
        {
            return;
        }

        currentAutoHintTrackedTile = tile;
        currentAutoHintTrackedTile.HintFlashCycleCompleted += HandleAutoHintFlashCycleCompleted;
    }

    private void HandleAutoHintFlashCycleCompleted(TileView tile)
    {
        if (!hasCurrentAutoHint || tile != currentAutoHintTrackedTile)
        {
            return;
        }

        currentAutoHintFlashCount++;

        if (currentAutoHintFlashCount < AutoHintFlashCyclesBeforeAdvance)
        {
            return;
        }

        hasCurrentAutoHint = false;
        currentAutoHint = default;
        currentAutoHintFlashCount = 0;
        ClearAutoHint();
        ResetAutoHintTimer();
    }

    private void AdvanceCurrentAutoHintToNext()
    {
        if (!hasCurrentAutoHint)
        {
            return;
        }

        hasCurrentAutoHint = false;
        currentAutoHint = default;
        currentAutoHintFlashCount = 0;
        currentAutoHintDirectionTopLeftToBottomRight = false;
    }

    private static void ShuffleAutoHints(List<BoardMoveAnalyzer.HintResult> hints)
    {
        if (hints == null || hints.Count <= 1)
        {
            return;
        }

        for (int i = hints.Count - 1; i > 0; i--)
        {
            int swapIndex = UnityEngine.Random.Range(0, i + 1);
            BoardMoveAnalyzer.HintResult temp = hints[i];
            hints[i] = hints[swapIndex];
            hints[swapIndex] = temp;
        }
    }

    public void ToggleAutoHint()
    {
        autoHintEnabled = !autoHintEnabled;

        ResetAutoHintCycle();

        Debug.Log($"Auto hint {(autoHintEnabled ? "enabled" : "disabled")}");
    }

    public void SetAutoHintEnabled(bool enabled)
    {
        autoHintEnabled = enabled;

        ResetAutoHintCycle();
    }

    public void SetSuppressSameTypeEnlargement(bool suppress)
    {
        suppressSameTypeEnlargement = suppress;

        if (suppressSameTypeEnlargement)
        {
            ClearEnlargedTiles();
        }
    }

    public void SetTutorialEnlargementFilter(Func<TileView, bool> filter)
    {
        tutorialEnlargementFilter = filter;

        if (tutorialEnlargementFilter == null)
        {
            ClearEnlargedTiles();
        }
    }

    private void HandleStableBoardStateChanged()
    {
        ResetAutoHintCycle();

        if (boardManager == null || boardManager.GetRemainingFaceUpTiles() <= 0)
        {
            uiManager?.HideNoMatchPopup();
            return;
        }

        if (BoardMoveAnalyzer.HasAnyAvailableMove(boardManager))
        {
            uiManager?.HideNoMatchPopup();
        }
        else
        {
            uiManager?.ShowNoMatchPopup();
        }
    }

    private void HandlePointerInput()
    {
        if (GameInput.TryGetPointerDownPosition(out _))
        {
            OnPointerDown();
        }

        if (isPointerHeld && GameInput.TryGetPointerHeldPosition(out _))
        {
            OnPointerHeld();
        }

        if (isPointerHeld && GameInput.TryGetPointerUpPosition(out _))
        {
            OnPointerUp();
        }
    }

    private void OnPointerDown()
    {
        if (GameInput.GetPressedTouchCount() > 1)
        {
            return;
        }

        bool dismissedOverlay = false;

        if (uiManager != null && uiManager.IsModalOverlayVisible())
        {
            dismissedOverlay = uiManager.DismissTransientOverlays();

            if (uiManager.IsModalOverlayVisible())
            {
                ClearInteractionVisuals();
                ClearAutoHint();
                ResetAutoHintTimer();
                ResetInteractionState();
                return;
            }
        }

        ClearAutoHint();
        ResetAutoHintTimer();
        AdvanceCurrentAutoHintToNext();

        if (dismissedOverlay)
        {
            ClearInteractionVisuals();
            ResetInteractionState();
            return;
        }

        Vector3 worldPosition = GetPointerWorldPosition();
        TileView clickedTile = GetTileUnderPointer(worldPosition);

        if (selectionMode != SelectionMode.None)
        {
            HandleTileSelection(clickedTile);
            return;
        }

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

        if (tutorialInteractionFilter != null && !tutorialInteractionFilter(clickedTile))
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

        if (!suppressSameTypeEnlargement)
        {
            ShowSameTypeEnlargement(activeTile);
        }
    }

    private void OnPointerHeld()
    {
        Vector3 currentWorldPosition = GetPointerWorldPosition();
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

            if (dragCreatedMatchChoices.Count == 1)
            {
                boardManager.ResolveMatch(activeTile, dragCreatedMatchChoices[0]);
                dragCreatedMatchChoices.Clear();
                ClearPendingDragMove();
                ResetInteractionState();
                return;
            }

            if (dragCreatedMatchChoices.Count > 1)
            {
                isAwaitingDragSelectionCancelRestore = true;
                EnterMatchChoiceState(dragCreatedMatchChoices);
                dragCreatedMatchChoices.Clear();
                isPointerHeld = false;
                isDragging = false;
                currentDragDirection = Vector2Int.zero;
                currentPreviewDistance = 0f;
                return;
            }

            dragCreatedMatchChoices.Clear();
            ResetInteractionState();
            return;
        }

        if (activeTile != null && matchValidator != null)
        {
            List<TileView> candidates = matchValidator.GetMatchCandidates(activeTile);

            ClearInteractionVisuals();

            if (candidates.Count == 1)
            {
                boardManager.RecordUndoSnapshot();
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

    private void HandleTileSelection(TileView clickedTile)
    {
        if (clickedTile == null || clickedTile.IsPath)
        {
            return;
        }

        if (tutorialSelectionFilter != null && !tutorialSelectionFilter(clickedTile))
        {
            return;
        }

        if (selectedTileA == null)
        {
            ClearInteractionVisuals();
            selectedTileA = clickedTile;
            clickedTile.SetCustomScale(1.2f, 12);
            enlargedTiles.Add(clickedTile);
            Debug.Log($"Selection A: {clickedTile.name}");
            return;
        }

        TileView selectedTileB = clickedTile;

        if (selectionMode == SelectionMode.Swap)
        {
            boardManager.RecordUndoSnapshot();

            if (!boardManager.SwapTiles(selectedTileA, selectedTileB))
            {
                boardManager.DiscardLastUndoSnapshot();
            }
        }
        else if (selectionMode == SelectionMode.Match)
        {
            boardManager.RecordUndoSnapshot();

            if (!boardManager.DebugMatchTiles(selectedTileA, selectedTileB))
            {
                boardManager.DiscardLastUndoSnapshot();
            }
        }

        CancelSelectionMode();
    }

    private bool TryResolveDragMove()
    {
        dragCreatedMatchChoices.Clear();

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

        Vector2Int originalActivePosition = activeTile.GridPosition;
        Vector2Int projectedPosition = activeTile.GridPosition + currentDragDirection * chosenSteps;

        List<TileView> projectedMatches = matchValidator.GetProjectedMatchCandidates(
            activeTile,
            projectedPosition,
            movedGroup,
            currentDragDirection,
            chosenSteps);

        foreach (TileView projectedMatch in projectedMatches)
        {
            if (!currentMatches.Contains(projectedMatch))
            {
                dragCreatedMatchChoices.Add(projectedMatch);
            }
        }

        if (dragCreatedMatchChoices.Count == 0)
        {
            return false;
        }

        boardManager.RecordUndoSnapshot();
        StorePendingDragMove(movedGroup, originalActivePosition, currentDragDirection, chosenSteps);
        boardManager.MoveTileGroup(activeTile, connectedTiles, currentDragDirection, chosenSteps);
        return true;
    }

    private void StorePendingDragMove(List<TileView> movedTiles, Vector2Int originalActivePosition, Vector2Int direction, int steps)
    {
        pendingMovedTiles.Clear();
        pendingMovedTiles.AddRange(movedTiles);

        pendingMoveOriginalActivePosition = originalActivePosition;
        pendingMoveDirection = direction;
        pendingMoveSteps = steps;
    }

    private void RestorePendingDragMoveIfNeeded()
    {
        if (!isAwaitingDragSelectionCancelRestore)
        {
            return;
        }

        if (pendingMovedTiles.Count > 0 && boardManager != null)
        {
            boardManager.RestoreMovedTileGroup(
                pendingMovedTiles,
                pendingMoveOriginalActivePosition,
                pendingMoveDirection,
                pendingMoveSteps);
        }

        ClearPendingDragMove();
    }

    private void ClearPendingDragMove()
    {
        isAwaitingDragSelectionCancelRestore = false;
        pendingMovedTiles.Clear();
        pendingMoveOriginalActivePosition = Vector2Int.zero;
        pendingMoveDirection = Vector2Int.zero;
        pendingMoveSteps = 0;
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
            activeTile.SetCustomScale(1.2f, 12);
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
            bool cameFromPendingDragMove = isAwaitingDragSelectionCancelRestore;

            if (!cameFromPendingDragMove)
            {
                boardManager.RecordUndoSnapshot();
            }

            boardManager.ResolveMatch(activeTile, clickedTile);
            ClearPendingDragMove();
            ExitMatchChoiceState();
            return;
        }

        CancelMatchChoice();
    }

    private void CancelMatchChoice()
    {
        bool cameFromPendingDragMove = isAwaitingDragSelectionCancelRestore;

        RestorePendingDragMoveIfNeeded();

        if (cameFromPendingDragMove)
        {
            boardManager.DiscardLastUndoSnapshot();
        }

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
            if (tutorialEnlargementFilter != null && !tutorialEnlargementFilter(tile))
            {
                continue;
            }

            tile.SetCustomScale(TutorialClickEnlargementScale, 10);
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
        InteractionVisualsCleared?.Invoke();
    }

    public void ForceClearInteractionState()
    {
        ClearInteractionVisuals();
        ClearAutoHint();
        ResetAutoHintTimer();
        currentMatchChoices.Clear();
        dragCreatedMatchChoices.Clear();
        CancelSelectionMode();
        ResetInteractionState();
    }

    public void BeginSwapSelection()
    {
        ForceClearInteractionState();
        selectionMode = SelectionMode.Swap;
        Debug.Log("Swap mode enabled. Select 2 tiles.");
    }

    public void BeginDebugMatchSelection()
    {
        ForceClearInteractionState();
        selectionMode = SelectionMode.Match;
        Debug.Log("Debug match mode enabled. Select 2 tiles.");
    }

    public void CancelSelectionMode()
    {
        ClearInteractionVisuals();
        selectedTileA = null;
        selectionMode = SelectionMode.None;
    }

    public void SetTutorialInteractionFilter(Func<TileView, bool> filter)
    {
        tutorialInteractionFilter = filter;
    }

    public void SetTutorialSelectionFilter(Func<TileView, bool> filter)
    {
        tutorialSelectionFilter = filter;
    }

    private void ResetInteractionState()
    {
        activeTile = null;
        isPointerHeld = false;
        isDragging = false;
        isAwaitingMatchChoice = false;
        currentDragDirection = Vector2Int.zero;
        currentPreviewDistance = 0f;
        dragCreatedMatchChoices.Clear();
        ClearPendingDragMove();
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

    private Vector3 GetPointerWorldPosition()
    {
        if (!GameInput.TryGetCurrentPointerScreenPosition(out Vector2 pointerScreenPosition))
        {
            return Vector3.zero;
        }

        Vector3 screenPosition = pointerScreenPosition;
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
