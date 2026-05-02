using System;
using System.Collections.Generic;
using System.Collections;
using UnityEngine;

public class TutorialBoardManager : MonoBehaviour
{
    private const float SameTypePreviewScale = 1.2f;
    private const float GuideHintPauseSeconds = 0.8f;
    private const float SwapTopRowGuideDelaySeconds = 0.2f;

    private const int TutorialBoardWidth = 12;
    private const int TutorialBoardHeight = 6;

    public enum TutorialBoardFlowStep
    {
        Step1,
        Step2,
        Step3,
        Step4,
        StepUndo,
        StepShuffle,
        StepSwap,
        TutorialCompleted
    }

    [Header("References")]
    [SerializeField] private TileView tilePrefab;
    [SerializeField] private Transform tileContainer;
    [SerializeField] private Sprite[] tileSprites;
    [SerializeField] private Sprite backTileSprite;

    private readonly List<TileView> spawnedTiles = new List<TileView>();
    private readonly List<TileView> guideTiles = new List<TileView>();
    private readonly List<TileView> sameTypePreviewTiles = new List<TileView>();
    private readonly List<TileView> dragPreviewClones = new List<TileView>();
    private readonly List<TileView> activeDragCloneSources = new List<TileView>();
    private readonly List<TileView> dragTintedTiles = new List<TileView>();

    private BoardLayoutConfig boardLayoutConfig = BoardLayoutConfig.CreateDefault();
    private TileView[,] boardTiles;
    private CellState[,] currentBoardState;
    private CellState[,] initialBoardSnapshot;
    private CellState[,] afterShuffleBoardSnapshot;
    private float tileSpacingX;
    private float tileSpacingY;
    private TutorialBoardFlowStep currentFlowStep;
    private TileView activeInteractionTile;
    private TileView activeDragTile;
    private Vector3 activeDragStartWorldPosition;
    private Vector2Int activeDragDirection;
    private int activeDragMaxSteps;
    private int activeDragRequiredSteps;
    private int activeDragCommittedSteps;
    private bool activeDragCompletesStep;
    private bool isSwapSelectionActive;
    private TileView selectedSwapTile;
    private bool suppressGuideHighlights;
    private Coroutine guideHintCoroutine;

    public event Action<TutorialBoardFlowStep> FlowStepChanged;

    public TutorialBoardFlowStep CurrentFlowStep => currentFlowStep;
    public int BoardWidth => TutorialBoardWidth;
    public int BoardHeight => TutorialBoardHeight;
    public IReadOnlyList<TileView> SpawnedTiles => spawnedTiles;

    private void Start()
    {
        EnsureBoardLayoutConfig();
        BuildSnapshots();
        SpawnBoardObjects();
        ApplySnapshot(initialBoardSnapshot);
        suppressGuideHighlights = true;
        SetCurrentFlowStep(TutorialBoardFlowStep.Step1);
        StartCoroutine(ShowInitialGuideHighlightsAfterDelay());
    }

    public void ResetToInitial()
    {
        ApplySnapshot(initialBoardSnapshot);
        SetCurrentFlowStep(TutorialBoardFlowStep.Step1);
    }

    [ContextMenu("Apply Initial Snapshot")]
    private void ApplyInitialSnapshotContext()
    {
        ApplySnapshot(initialBoardSnapshot);
        SetCurrentFlowStep(TutorialBoardFlowStep.Step1);
    }

    [ContextMenu("Apply After Shuffle Snapshot")]
    private void ApplyAfterShuffleSnapshotContext()
    {
        ApplySnapshot(afterShuffleBoardSnapshot);
        SetCurrentFlowStep(TutorialBoardFlowStep.StepShuffle);
    }

    public TileView GetTileAt(Vector2Int position)
    {
        if (boardTiles == null ||
            position.x < 0 || position.x >= TutorialBoardWidth ||
            position.y < 0 || position.y >= TutorialBoardHeight)
        {
            return null;
        }

        return boardTiles[position.x, position.y];
    }

    public Vector2 GetWorldPosition(Vector2Int gridPosition)
    {
        EnsureBoardLayoutConfig();
        return boardLayoutConfig.GetWorldPosition(TutorialBoardWidth, TutorialBoardHeight, tileSpacingX, tileSpacingY, gridPosition.x, gridPosition.y);
    }

    public bool IsBoardInteractionStep()
    {
        return currentFlowStep == TutorialBoardFlowStep.Step1 ||
               currentFlowStep == TutorialBoardFlowStep.Step2 ||
               currentFlowStep == TutorialBoardFlowStep.Step3 ||
               currentFlowStep == TutorialBoardFlowStep.Step4 ||
               (currentFlowStep == TutorialBoardFlowStep.StepSwap && isSwapSelectionActive);
    }

    public bool IsDragStep()
    {
        return currentFlowStep == TutorialBoardFlowStep.Step3 ||
               currentFlowStep == TutorialBoardFlowStep.Step4;
    }

    public bool CanInteractWithTile(TileView tile)
    {
        if (!IsBoardInteractionStep() || tile == null || tile.IsPath)
        {
            return false;
        }

        if (!guideTiles.Contains(tile))
        {
            return false;
        }

        return true;
    }

    public void SetFlowStepOnly(TutorialBoardFlowStep flowStep)
    {
        SetCurrentFlowStep(flowStep);
    }

    public void ApplyUndoVisualToStep4()
    {
        ApplyDeltas(CreateUndoToStep4Deltas());
        SetCurrentFlowStep(TutorialBoardFlowStep.StepShuffle);
    }

    public void ApplyShuffleSnapshot()
    {
        ApplySnapshot(afterShuffleBoardSnapshot);
        SetCurrentFlowStep(TutorialBoardFlowStep.StepSwap);
    }

    public void BeginSwapSelection()
    {
        if (currentFlowStep != TutorialBoardFlowStep.StepSwap)
        {
            return;
        }

        isSwapSelectionActive = true;
        selectedSwapTile = null;
        RefreshGuideTiles();
    }

    public void BeginTileInteraction(TileView tile)
    {
        if (currentFlowStep == TutorialBoardFlowStep.StepSwap && isSwapSelectionActive)
        {
            HandleSwapTileTap(tile);
            return;
        }

        ClearTransientVisuals();

        if (!CanInteractWithTile(tile))
        {
            ReapplyGuideHighlights();
            activeInteractionTile = null;
            activeDragTile = null;
            return;
        }

        activeInteractionTile = tile;
        SuspendGuideHighlightsForInteraction();
        ApplySameTypePreview(tile);

        if (IsDragStep())
        {
            if (!TryConfigureDrag(tile))
            {
                ReapplyGuideHighlights();
                activeInteractionTile = null;
                activeDragTile = null;
                return;
            }

            ApplyDragPreview(tile);
            ApplyDragSourceTint();
        }
    }

    public void UpdateTileInteraction(Vector3 worldPosition)
    {
        if (!IsDragStep() || activeDragTile == null)
        {
            return;
        }

        float previewDistance = GetClampedPreviewDistance(worldPosition);
        Vector3 previewOffset = GetWorldDirectionOffset(activeDragDirection, previewDistance);
        UpdateDragPreviewClonePositions(previewOffset);
        activeDragCommittedSteps = GetCommittedStepCount(previewDistance);
    }

    public void EndTileInteraction(TileView releasedTile)
    {
        bool shouldAdvance = false;

        if (currentFlowStep == TutorialBoardFlowStep.Step1 ||
            currentFlowStep == TutorialBoardFlowStep.Step2)
        {
            shouldAdvance = releasedTile != null &&
                            releasedTile == activeInteractionTile &&
                            guideTiles.Contains(releasedTile);
        }
        else if (IsDragStep() && activeDragTile != null)
        {
            shouldAdvance = activeDragCompletesStep &&
                            activeDragCommittedSteps == activeDragRequiredSteps;
        }

        activeInteractionTile = null;
        activeDragTile = null;

        ClearDragSourceTint();
        ClearTransientVisuals();

        if (shouldAdvance)
        {
            AdvanceFlowStep();
            return;
        }

        ReapplyGuideHighlights();
    }

    private void SpawnBoardObjects()
    {
        if (tilePrefab == null || tileContainer == null)
        {
            Debug.LogWarning("TutorialBoardManager is missing tile prefab or tile container.");
            return;
        }

        if (tileSprites == null || tileSprites.Length == 0)
        {
            Debug.LogWarning("TutorialBoardManager has no tile sprites assigned.");
            return;
        }

        CalculateSpacing();
        boardTiles = new TileView[TutorialBoardWidth, TutorialBoardHeight];
        currentBoardState = new CellState[TutorialBoardWidth, TutorialBoardHeight];

        for (int y = 0; y < TutorialBoardHeight; y++)
        {
            for (int x = 0; x < TutorialBoardWidth; x++)
            {
                Vector2Int position = new Vector2Int(x, y);
                TileView tile = Instantiate(tilePrefab, GetWorldPosition(position), Quaternion.identity, tileContainer);
                tile.Initialize(tileSprites[0], 0, position);
                ApplyTileScale(tile);
                boardTiles[x, y] = tile;
                spawnedTiles.Add(tile);
            }
        }
    }

    private void ApplySnapshot(CellState[,] snapshot)
    {
        if (snapshot == null)
        {
            return;
        }

        ClearTransientVisuals();

        for (int y = 0; y < TutorialBoardHeight; y++)
        {
            for (int x = 0; x < TutorialBoardWidth; x++)
            {
                ApplyCellState(new Vector2Int(x, y), snapshot[x, y]);
            }
        }

        Debug.Log($"[TutorialBoardManager] Applied snapshot for {currentFlowStep}");
    }

    private void ApplyDeltas(CellDelta[] deltas)
    {
        if (deltas == null)
        {
            return;
        }

        ClearTransientVisuals();

        for (int i = 0; i < deltas.Length; i++)
        {
            ApplyCellState(deltas[i].Position, deltas[i].State);
        }
    }

    private void ApplyCellState(Vector2Int position, CellState state)
    {
        TileView tile = GetTileAt(position);
        if (tile == null)
        {
            return;
        }

        currentBoardState[position.x, position.y] = state;
        tile.ApplyCellState(GetTileSprite(state.TileTypeId), state.TileTypeId, state.IsPath, backTileSprite);
    }

    private void SetCurrentFlowStep(TutorialBoardFlowStep flowStep)
    {
        isSwapSelectionActive = false;
        selectedSwapTile = null;
        currentFlowStep = flowStep;
        Debug.Log($"[TutorialBoardManager] Flow step -> {currentFlowStep}");
        RefreshGuideTiles();
        FlowStepChanged?.Invoke(currentFlowStep);
    }

    private void AdvanceFlowStep()
    {
        switch (currentFlowStep)
        {
            case TutorialBoardFlowStep.Step1:
                ApplyDeltas(CreateStep1MatchDeltas());
                SetCurrentFlowStep(TutorialBoardFlowStep.Step2);
                break;

            case TutorialBoardFlowStep.Step2:
                ApplyDeltas(CreateStep2MatchDeltas());
                SetCurrentFlowStep(TutorialBoardFlowStep.Step3);
                break;

            case TutorialBoardFlowStep.Step3:
                ApplyDeltas(CreateStep3MatchDeltas());
                SetCurrentFlowStep(TutorialBoardFlowStep.Step4);
                break;

            case TutorialBoardFlowStep.Step4:
                ApplyDeltas(CreateStep4ResolvedDeltas());
                SetCurrentFlowStep(TutorialBoardFlowStep.StepUndo);
                break;
        }
    }

    private void RefreshGuideTiles()
    {
        guideTiles.Clear();

        Vector2Int[] guidePositions = GetGuidePositionsForCurrentStep();
        for (int i = 0; i < guidePositions.Length; i++)
        {
            TileView tile = GetTileAt(guidePositions[i]);
            if (tile != null && !tile.IsPath)
            {
                guideTiles.Add(tile);
            }
        }

        ReapplyGuideHighlights();
    }

    private void ReapplyGuideHighlights()
    {
        StopGuideHintLoop();

        if (suppressGuideHighlights)
        {
            return;
        }

        List<TileView> tilesToFlash = new List<TileView>();

        for (int i = 0; i < guideTiles.Count; i++)
        {
            TileView tile = guideTiles[i];
            if (tile != null)
            {
                if (tile == activeInteractionTile ||
                    tile == selectedSwapTile ||
                    activeDragCloneSources.Contains(tile) ||
                    sameTypePreviewTiles.Contains(tile))
                {
                    tile.ResetVisual();
                    continue;
                }

                tilesToFlash.Add(tile);
            }
        }

        if (tilesToFlash.Count > 0)
        {
            guideHintCoroutine = StartCoroutine(GuideHintLoop(tilesToFlash, UseTopLeftToBottomRightGuideFlash()));
        }

        if (selectedSwapTile != null)
        {
            selectedSwapTile.SetCustomScale(SameTypePreviewScale, 30);
        }
    }

    private bool UseTopLeftToBottomRightGuideFlash()
    {
        return ((int)currentFlowStep % 2) == 0;
    }

    private static void StopGuideHighlightForTile(TileView tile)
    {
        if (tile == null)
        {
            return;
        }

        tile.ResetVisual();
    }

    private void SuspendGuideHighlightsForInteraction()
    {
        StopGuideHintLoop();

        for (int i = 0; i < guideTiles.Count; i++)
        {
            StopGuideHighlightForTile(guideTiles[i]);
        }
    }

    private IEnumerator GuideHintLoop(List<TileView> tilesToFlash, bool topLeftToBottomRight)
    {
        while (true)
        {
            float sweepDuration = 0f;

            for (int i = 0; i < tilesToFlash.Count; i++)
            {
                TileView tile = tilesToFlash[i];
                if (tile == null)
                {
                    continue;
                }

                sweepDuration = Mathf.Max(sweepDuration, tile.HintFlashSweepDuration);
            }

            if (sweepDuration <= 0f)
            {
                yield break;
            }

            float elapsed = 0f;

            while (elapsed < sweepDuration)
            {
                elapsed += Time.deltaTime;

                for (int i = 0; i < tilesToFlash.Count; i++)
                {
                    TileView tile = tilesToFlash[i];
                    if (tile == null)
                    {
                        continue;
                    }

                    float tileDelay = GetGuideHintDelay(tile);
                    if (elapsed < tileDelay)
                    {
                        tile.HideHintFlash();
                        continue;
                    }

                    float normalizedProgress = Mathf.Clamp01((elapsed - tileDelay) / sweepDuration);
                    tile.SetHintFlashProgress(topLeftToBottomRight, normalizedProgress);
                }

                yield return null;
            }

            for (int i = 0; i < tilesToFlash.Count; i++)
            {
                TileView tile = tilesToFlash[i];
                if (tile == null)
                {
                    continue;
                }

                tile.HideHintFlash();
            }

            yield return new WaitForSeconds(GuideHintPauseSeconds);
        }
    }

    private void StopGuideHintLoop()
    {
        if (guideHintCoroutine != null)
        {
            StopCoroutine(guideHintCoroutine);
            guideHintCoroutine = null;
        }
    }

    private float GetGuideHintDelay(TileView tile)
    {
        if (tile == null)
        {
            return 0f;
        }

        if (currentFlowStep == TutorialBoardFlowStep.StepSwap && tile.GridPosition.y == 0)
        {
            return SwapTopRowGuideDelaySeconds;
        }

        return 0f;
    }

    private IEnumerator ShowInitialGuideHighlightsAfterDelay()
    {
        yield return new WaitForSeconds(2f);
        suppressGuideHighlights = false;
        ReapplyGuideHighlights();
    }

    private void ClearTransientVisuals()
    {
        ClearDragPreviewClones();
        ClearDragSourceTint();

        for (int i = 0; i < sameTypePreviewTiles.Count; i++)
        {
            if (sameTypePreviewTiles[i] != null)
            {
                sameTypePreviewTiles[i].ResetVisual();
            }
        }

        sameTypePreviewTiles.Clear();
    }

    private void HandleSwapTileTap(TileView tile)
    {
        if (tile == null || !CanInteractWithTile(tile))
        {
            return;
        }

        ClearTransientVisuals();
        ReapplyGuideHighlights();

        if (selectedSwapTile == null)
        {
            selectedSwapTile = tile;
            ReapplyGuideHighlights();
            return;
        }

        if (selectedSwapTile == tile)
        {
            selectedSwapTile = null;
            ReapplyGuideHighlights();
            return;
        }

        SwapTileStates(selectedSwapTile.GridPosition, tile.GridPosition);
        selectedSwapTile = null;
        isSwapSelectionActive = false;
        SetCurrentFlowStep(TutorialBoardFlowStep.TutorialCompleted);
    }

    private void ApplySameTypePreview(TileView sourceTile)
    {
        if (sourceTile == null)
        {
            return;
        }

        for (int i = 0; i < spawnedTiles.Count; i++)
        {
            TileView tile = spawnedTiles[i];
            if (tile == null || tile == sourceTile || tile.IsPath || tile.TileTypeId != sourceTile.TileTypeId)
            {
                continue;
            }

            StopGuideHighlightForTile(tile);
            tile.SetCustomScale(SameTypePreviewScale, 30);
            sameTypePreviewTiles.Add(tile);
        }
    }

    private void ApplyDragPreview(TileView sourceTile)
    {
        activeDragCloneSources.Clear();
        PopulateDragCloneSources(sourceTile);

        for (int i = 0; i < activeDragCloneSources.Count; i++)
        {
            StopGuideHighlightForTile(activeDragCloneSources[i]);
        }

        for (int i = 0; i < activeDragCloneSources.Count; i++)
        {
            TileView source = activeDragCloneSources[i];
            if (source == null)
            {
                continue;
            }

            TileView clone = Instantiate(tilePrefab, source.transform.position, Quaternion.identity, tileContainer);
            clone.name = $"TutorialDragPreview_{source.name}";
            clone.Initialize(source.FaceUpSprite, source.TileTypeId, source.GridPosition);
            clone.SetBaseScale(source.transform.localScale);

            Collider2D cloneCollider = clone.GetComponent<Collider2D>();
            if (cloneCollider != null)
            {
                cloneCollider.enabled = false;
            }

            SpriteRenderer cloneRenderer = clone.GetComponent<SpriteRenderer>();
            if (cloneRenderer != null)
            {
                cloneRenderer.sortingOrder += 35;
            }

            dragPreviewClones.Add(clone);
        }
    }

    private void PopulateDragCloneSources(TileView sourceTile)
    {
        if (sourceTile == null)
        {
            return;
        }

        if (currentFlowStep == TutorialBoardFlowStep.Step3)
        {
            activeDragCloneSources.Add(sourceTile);
            return;
        }

        if (currentFlowStep == TutorialBoardFlowStep.Step4)
        {
            AddDragCloneSource(sourceTile);
            AddConnectedCloneSources(sourceTile);
        }
    }

    private void AddDragCloneSource(TileView tile)
    {
        if (tile == null || activeDragCloneSources.Contains(tile))
        {
            return;
        }

        activeDragCloneSources.Add(tile);
    }

    private void AddConnectedCloneSources(TileView sourceTile)
    {
        Vector2Int position = sourceTile.GridPosition + Vector2Int.right;
        while (true)
        {
            TileView tile = GetTileAt(position);
            if (tile == null || tile.IsPath)
            {
                break;
            }

            AddDragCloneSource(tile);
            position += Vector2Int.right;
        }
    }

    private void ApplyDragSourceTint()
    {
        ClearDragSourceTint();

        for (int i = 0; i < activeDragCloneSources.Count; i++)
        {
            TileView tile = activeDragCloneSources[i];
            if (tile == null)
            {
                continue;
            }

            tile.SetDragSourceTint(true);
            dragTintedTiles.Add(tile);
        }
    }

    private void ClearDragSourceTint()
    {
        for (int i = 0; i < dragTintedTiles.Count; i++)
        {
            if (dragTintedTiles[i] != null)
            {
                dragTintedTiles[i].SetDragSourceTint(false);
            }
        }

        dragTintedTiles.Clear();
    }

    private bool TryConfigureDrag(TileView tile)
    {
        activeDragTile = null;
        activeDragDirection = Vector2Int.zero;
        activeDragMaxSteps = 0;
        activeDragRequiredSteps = 0;
        activeDragCommittedSteps = 0;
        activeDragCompletesStep = false;

        if (tile == null)
        {
            return false;
        }

        switch (currentFlowStep)
        {
            case TutorialBoardFlowStep.Step3:
                activeDragTile = tile;
                activeDragStartWorldPosition = tile.transform.position;
                activeDragCompletesStep = true;

                if (tile.GridPosition == new Vector2Int(6, 1))
                {
                    activeDragDirection = Vector2Int.right;
                    activeDragMaxSteps = 4;
                    activeDragRequiredSteps = 3;
                    return true;
                }

                if (tile.GridPosition == new Vector2Int(9, 2))
                {
                    activeDragDirection = Vector2Int.down;
                    activeDragMaxSteps = 1;
                    activeDragRequiredSteps = 1;
                    return true;
                }

                return false;

            case TutorialBoardFlowStep.Step4:
                if (tile.GridPosition != new Vector2Int(1, 1) &&
                    tile.GridPosition != new Vector2Int(4, 2))
                {
                    return false;
                }

                activeDragTile = tile;
                activeDragStartWorldPosition = tile.transform.position;
                activeDragDirection = Vector2Int.right;
                if (tile.GridPosition == new Vector2Int(1, 1))
                {
                    activeDragMaxSteps = 5;
                    activeDragRequiredSteps = 3;
                    activeDragCompletesStep = true;
                }
                else
                {
                    activeDragMaxSteps = 1;
                    activeDragRequiredSteps = 1;
                    activeDragCompletesStep = false;
                }
                return true;

            default:
                return false;
        }
    }

    private float GetClampedPreviewDistance(Vector3 worldPosition)
    {
        Vector3 pointerDelta = worldPosition - activeDragStartWorldPosition;
        Vector3 worldDirection = GetWorldUnitDirection(activeDragDirection);
        float signedDistance = Vector3.Dot(pointerDelta, worldDirection);
        float maxDistance = GetWorldStepDistance(activeDragDirection) * activeDragMaxSteps;
        return Mathf.Clamp(signedDistance, 0f, maxDistance);
    }

    private int GetCommittedStepCount(float previewDistance)
    {
        float stepDistance = GetWorldStepDistance(activeDragDirection);
        if (stepDistance <= 0f)
        {
            return 0;
        }

        return Mathf.Clamp(Mathf.RoundToInt(previewDistance / stepDistance), 0, activeDragMaxSteps);
    }

    private float GetWorldStepDistance(Vector2Int gridDirection)
    {
        if (gridDirection.x != 0)
        {
            return Mathf.Abs(tileSpacingX);
        }

        return Mathf.Abs(tileSpacingY);
    }

    private Vector3 GetWorldDirectionOffset(Vector2Int gridDirection, float distance)
    {
        return GetWorldUnitDirection(gridDirection) * distance;
    }

    private Vector3 GetWorldUnitDirection(Vector2Int gridDirection)
    {
        if (gridDirection.x > 0)
        {
            return Vector3.right;
        }

        if (gridDirection.x < 0)
        {
            return Vector3.left;
        }

        if (gridDirection.y > 0)
        {
            return Vector3.down;
        }

        if (gridDirection.y < 0)
        {
            return Vector3.up;
        }

        return Vector3.zero;
    }

    private void UpdateDragPreviewClonePositions(Vector3 previewOffset)
    {
        for (int i = 0; i < dragPreviewClones.Count && i < activeDragCloneSources.Count; i++)
        {
            TileView clone = dragPreviewClones[i];
            TileView source = activeDragCloneSources[i];
            if (clone == null || source == null)
            {
                continue;
            }

            clone.SetWorldPosition(source.transform.position + previewOffset);
        }
    }

    private void ClearDragPreviewClones()
    {
        for (int i = dragPreviewClones.Count - 1; i >= 0; i--)
        {
            if (dragPreviewClones[i] != null)
            {
                Destroy(dragPreviewClones[i].gameObject);
            }
        }

        dragPreviewClones.Clear();
        activeDragCloneSources.Clear();
    }

    private void BuildSnapshots()
    {
        int[,] initialLayout =
        {
            { 16, 15, 8, 15, 6, 14, 7, 4, 5, 5, 13, 3 },
            { 11, 3, 4, 5, 11, 17, 2, 1, 0, 0, 1, 17 },
            { 2, 4, 12, 8, 3, 14, 13, 9, 10, 2, 7, 9 },
            { 6, 16, 12, 2, 14, 11, 11, 8, 6, 17, 9, 10 },
            { 4, 17, 16, 3, 13, 1, 10, 12, 0, 16, 9, 5 },
            { 15, 6, 15, 13, 1, 7, 12, 8, 0, 14, 7, 10 }
        };

        int[,] shuffleLayout =
        {
            { 16, 14, 5, 10, 0, 12, 17, 3, 9, 5, 3, 13 },
            { 16, 6, 7, 7, 6, 8, 2, 1, 0, 0, 1, 12 },
            { 2, 2, 7, 9, 10, 17, 8, 14, 13, 2, 16, 4 },
            { 13, 11, 17, 17, 15, 0, 8, 15, 10, 13, 9, 5 },
            { 11, 6, 8, 12, 14, 11, 11, 3, 9, 14, 10, 7 },
            { 4, 3, 12, 5, 1, 15, 4, 15, 6, 1, 16, 4 }
        };

        initialBoardSnapshot = CreateSnapshot(initialLayout);
        afterShuffleBoardSnapshot = CreateSnapshot(shuffleLayout,
            new Vector2Int(6, 1),
            new Vector2Int(7, 1),
            new Vector2Int(8, 1),
            new Vector2Int(9, 1),
            new Vector2Int(10, 1),
            new Vector2Int(9, 2));
    }

    private void CalculateSpacing()
    {
        EnsureBoardLayoutConfig();

        if (tilePrefab == null)
        {
            tileSpacingX = 0f;
            tileSpacingY = 0f;
            return;
        }

        Vector2 spacing = boardLayoutConfig.CalculateSpacing(TutorialBoardWidth, TutorialBoardHeight, tilePrefab.transform.localScale);
        tileSpacingX = spacing.x;
        tileSpacingY = spacing.y;
    }

    private void ApplyTileScale(TileView tile)
    {
        if (tile == null || tilePrefab == null)
        {
            return;
        }

        float scaleMultiplier = boardLayoutConfig.CalculateBoardScaleMultiplier(TutorialBoardWidth, TutorialBoardHeight);
        tile.SetBaseScale(tilePrefab.transform.localScale * scaleMultiplier);
    }

    private void EnsureBoardLayoutConfig()
    {
        if (boardLayoutConfig == null)
        {
            boardLayoutConfig = BoardLayoutConfig.CreateDefault();
        }
    }

    private Vector2Int[] GetGuidePositionsForCurrentStep()
    {
        switch (currentFlowStep)
        {
            case TutorialBoardFlowStep.Step1:
                return new[] { new Vector2Int(8, 1), new Vector2Int(9, 1) };
            case TutorialBoardFlowStep.Step2:
                return new[] { new Vector2Int(7, 1), new Vector2Int(10, 1) };
            case TutorialBoardFlowStep.Step3:
                return new[] { new Vector2Int(6, 1), new Vector2Int(9, 2) };
            case TutorialBoardFlowStep.Step4:
                return new[] { new Vector2Int(1, 1), new Vector2Int(4, 2) };
            case TutorialBoardFlowStep.StepSwap:
                return isSwapSelectionActive
                    ? new[] { new Vector2Int(4, 0), new Vector2Int(4, 2) }
                    : Array.Empty<Vector2Int>();
            default:
                return Array.Empty<Vector2Int>();
        }
    }

    private CellDelta[] CreateStep1MatchDeltas()
    {
        return new[]
        {
            CreatePathDelta(8, 1),
            CreatePathDelta(9, 1)
        };
    }

    private CellDelta[] CreateStep2MatchDeltas()
    {
        return new[]
        {
            CreatePathDelta(7, 1),
            CreatePathDelta(10, 1)
        };
    }

    private CellDelta[] CreateStep3MatchDeltas()
    {
        return new[]
        {
            CreatePathDelta(6, 1),
            CreatePathDelta(9, 2)
        };
    }

    private CellDelta[] CreateStep4ResolvedDeltas()
    {
        return new[]
        {
            CreatePathDelta(1, 1),
            CreatePathDelta(2, 1),
            CreatePathDelta(3, 1),
            CreatePathDelta(4, 1),
            CreatePathDelta(4, 2),
            CreateFaceUpDelta(5, 1, initialBoardSnapshot[2, 1].TileTypeId),
            CreateFaceUpDelta(6, 1, initialBoardSnapshot[3, 1].TileTypeId),
            CreateFaceUpDelta(7, 1, initialBoardSnapshot[4, 1].TileTypeId),
            CreateFaceUpDelta(8, 1, initialBoardSnapshot[5, 1].TileTypeId)
        };
    }

    private CellDelta[] CreateUndoToStep4Deltas()
    {
        return new[]
        {
            CreateFaceUpDelta(1, 1, initialBoardSnapshot[1, 1].TileTypeId),
            CreateFaceUpDelta(2, 1, initialBoardSnapshot[2, 1].TileTypeId),
            CreateFaceUpDelta(3, 1, initialBoardSnapshot[3, 1].TileTypeId),
            CreateFaceUpDelta(4, 1, initialBoardSnapshot[4, 1].TileTypeId),
            CreateFaceUpDelta(5, 1, initialBoardSnapshot[5, 1].TileTypeId),
            CreatePathDelta(6, 1),
            CreatePathDelta(7, 1),
            CreatePathDelta(8, 1),
            CreatePathDelta(9, 1),
            CreatePathDelta(10, 1),
            CreateFaceUpDelta(4, 2, initialBoardSnapshot[4, 2].TileTypeId),
            CreatePathDelta(9, 2)
        };
    }

    private CellDelta CreatePathDelta(int x, int y)
    {
        return new CellDelta(new Vector2Int(x, y), CellState.PathAt(currentBoardState[x, y].TileTypeId));
    }

    private CellDelta CreateFaceUpDelta(int x, int y, int tileTypeId)
    {
        return new CellDelta(new Vector2Int(x, y), CellState.FaceUp(tileTypeId));
    }

    private Sprite GetTileSprite(int tileTypeId)
    {
        if (tileTypeId < 0 || tileTypeId >= tileSprites.Length)
        {
            return tileSprites[0];
        }

        return tileSprites[tileTypeId];
    }

    private void SwapTileStates(Vector2Int positionA, Vector2Int positionB)
    {
        CellState stateA = currentBoardState[positionA.x, positionA.y];
        CellState stateB = currentBoardState[positionB.x, positionB.y];

        ApplyCellState(positionA, stateB);
        ApplyCellState(positionB, stateA);
    }

    private static CellState[,] CreateSnapshot(int[,] tileLayout, params Vector2Int[] pathPositions)
    {
        HashSet<Vector2Int> pathSet = new HashSet<Vector2Int>(pathPositions ?? Array.Empty<Vector2Int>());
        CellState[,] snapshot = new CellState[TutorialBoardWidth, TutorialBoardHeight];

        for (int y = 0; y < TutorialBoardHeight; y++)
        {
            for (int x = 0; x < TutorialBoardWidth; x++)
            {
                snapshot[x, y] = new CellState(tileLayout[y, x], pathSet.Contains(new Vector2Int(x, y)));
            }
        }

        return snapshot;
    }

    private readonly struct CellDelta
    {
        public CellDelta(Vector2Int position, CellState state)
        {
            Position = position;
            State = state;
        }

        public Vector2Int Position { get; }
        public CellState State { get; }
    }

    private readonly struct CellState
    {
        public CellState(int tileTypeId, bool isPath)
        {
            TileTypeId = tileTypeId;
            IsPath = isPath;
        }

        public int TileTypeId { get; }
        public bool IsPath { get; }

        public static CellState FaceUp(int tileTypeId)
        {
            return new CellState(tileTypeId, false);
        }

        public static CellState PathAt(int tileTypeId)
        {
            return new CellState(tileTypeId, true);
        }
    }
}
