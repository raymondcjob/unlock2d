using System.Collections.Generic;
using System;
using UnityEngine;

public class BoardManager : MonoBehaviour
{
    [Header("Board Settings")]
    [SerializeField] private int boardWidth = 17;
    [SerializeField] private int boardHeight = 8;

    [Header("Base Layout Tuning")]
    [SerializeField] private float baseTileScale = 0.2f;
    [SerializeField] private float baseTileSpacingX = 0.64f;
    [SerializeField] private float baseTileSpacingY = 0.86f;
    private float tileSpacingX;
    private float tileSpacingY;

    [Header("Seed Settings")]
    [SerializeField] private bool generateRandomSeedOnStart = true;

    private int remainingFaceUpTiles;
    private int currentSeed;
    private bool hasGeneratedBoard;

    [Header("Board Validation")]
    [SerializeField] private int maxSeedValidationAttempts = 500;

    public event Action OnStableBoardStateChanged;

    public event Action<int> OnBoardWon;

    [Header("References")]
    [SerializeField] private TileView tilePrefab;
    [SerializeField] private Transform tileContainer;

    [Header("Tile Sprites")]
    [SerializeField] private Sprite[] tileSprites;
    [SerializeField] private Sprite backTileSprite;


    private TileView[,] boardTiles;
    private readonly List<TileView> spawnedTiles = new List<TileView>();

    [Header("Undo Settings")]
    [SerializeField] private int maxUndoHistory = 99;

    public event Action OnBoardGenerated;

    private readonly List<BoardSnapshot> undoHistory = new List<BoardSnapshot>();

    private sealed class BoardSnapshot
    {
        public int RemainingFaceUpTiles;
        public TileState[] TileStates;
    }

    private struct TileState
    {
        public int TileTypeId;
        public Vector2Int GridPosition;
        public bool IsPath;

        public TileState(int tileTypeId, Vector2Int gridPosition, bool isPath)
        {
            TileTypeId = tileTypeId;
            GridPosition = gridPosition;
            IsPath = isPath;
        }
    }

    private void Start()
    {
        if (generateRandomSeedOnStart)
        {
            GenerateNewBoard();
        }
        else
        {
            GenerateBoardFromSeed(0);
        }
    }

    public void GenerateNewBoard()
    {
        int newSeed = UnityEngine.Random.Range(int.MinValue, int.MaxValue);
        GenerateBoardFromSeed(newSeed);
    }

    public void ReplayCurrentBoard()
    {
        if (!hasGeneratedBoard)
        {
            GenerateNewBoard();
            return;
        }

        GenerateBoardFromSeed(currentSeed);
    }

    public void GenerateBoardFromSeed(int seed)
    {
        hasGeneratedBoard = true;
        currentSeed = FindNextValidSeed(seed);
        GenerateBoard();

        ClearUndoHistory();
        OnBoardGenerated?.Invoke();
        OnStableBoardStateChanged?.Invoke();
    }

    private int FindNextValidSeed(int startingSeed)
    {
        int candidateSeed = startingSeed;

        for (int attempt = 0; attempt < maxSeedValidationAttempts; attempt++)
        {
            System.Random seededRandom = new System.Random(candidateSeed);
            List<TileEntry> shuffledTiles = BuildShuffledTileList(seededRandom);
            int[,] layout = BuildTileTypeGrid(shuffledTiles);

            if (!BoardMoveAnalyzer.ShouldRegenerateInitialLayout(layout))
            {
                return candidateSeed;
            }

            candidateSeed++;
        }

        Debug.LogWarning(
            $"Board validation could not find a valid seed within {maxSeedValidationAttempts} attempts. " +
            $"Falling back to starting seed {startingSeed}.");

        return startingSeed;
    }

    private int[,] BuildTileTypeGrid(List<TileEntry> shuffledTiles)
    {
        int[,] layout = new int[boardWidth, boardHeight];
        int index = 0;

        for (int y = 0; y < boardHeight; y++)
        {
            for (int x = 0; x < boardWidth; x++)
            {
                layout[x, y] = shuffledTiles[index].TileTypeId;
                index++;
            }
        }

        return layout;
    }

    public int GetCurrentSeed()
    {
        return currentSeed;
    }

    public int GetRemainingFaceUpTiles()
    {
        return remainingFaceUpTiles;
    }

    public int GetUndoHistoryCount()
    {
        return undoHistory.Count;
    }

    public void ClearUndoHistory()
    {
        undoHistory.Clear();
    }

    public void RecordUndoSnapshot()
    {
        if (spawnedTiles.Count == 0)
        {
            return;
        }

        BoardSnapshot snapshot = new BoardSnapshot
        {
            RemainingFaceUpTiles = remainingFaceUpTiles,
            TileStates = new TileState[spawnedTiles.Count]
        };

        for (int i = 0; i < spawnedTiles.Count; i++)
        {
            TileView tile = spawnedTiles[i];

            if (tile == null)
            {
                continue;
            }

            snapshot.TileStates[i] = new TileState(
                tile.TileTypeId,
                tile.GridPosition,
                tile.IsPath);
        }

        undoHistory.Add(snapshot);

        if (maxUndoHistory > 0 && undoHistory.Count > maxUndoHistory)
        {
            undoHistory.RemoveAt(0);
        }
    }

    public void DiscardLastUndoSnapshot()
    {
        if (undoHistory.Count == 0)
        {
            return;
        }

        undoHistory.RemoveAt(undoHistory.Count - 1);
    }

    public bool TryUndoLastMove()
    {
        if (undoHistory.Count == 0)
        {
            return false;
        }

        BoardSnapshot snapshot = undoHistory[undoHistory.Count - 1];
        undoHistory.RemoveAt(undoHistory.Count - 1);

        RestoreSnapshot(snapshot);
        OnStableBoardStateChanged?.Invoke();
        return true;
    }

    private void RestoreSnapshot(BoardSnapshot snapshot)
    {
        if (snapshot == null || snapshot.TileStates == null)
        {
            Debug.LogWarning("RestoreSnapshot failed: snapshot is null.");
            return;
        }

        if (snapshot.TileStates.Length != spawnedTiles.Count)
        {
            Debug.LogWarning("RestoreSnapshot failed: snapshot size does not match spawned tile count.");
            return;
        }

        boardTiles = new TileView[boardWidth, boardHeight];
        remainingFaceUpTiles = snapshot.RemainingFaceUpTiles;

        for (int i = 0; i < spawnedTiles.Count; i++)
        {
            TileView tile = spawnedTiles[i];
            TileState state = snapshot.TileStates[i];

            if (tile == null)
            {
                continue;
            }

            Sprite faceUpSprite = tileSprites[state.TileTypeId];

            tile.Initialize(faceUpSprite, state.TileTypeId, state.GridPosition);

            if (state.IsPath)
            {
                tile.ConvertToPath(backTileSprite);
            }

            tile.SetWorldPosition(GetWorldPosition(state.GridPosition));
            boardTiles[state.GridPosition.x, state.GridPosition.y] = tile;
        }
    }

    public int GetBoardWidth()
    {
        return boardWidth;
    }

    public int GetBoardHeight()
    {
        return boardHeight;
    }

    public void GenerateBoard()
    {
        ClearBoard();
        CalculateSpacingFromPrefabScale();

        int totalCells = boardWidth * boardHeight;
        int requiredTileCount = tileSprites.Length * 4;

        if (tileSprites == null || tileSprites.Length != 34)
        {
            Debug.LogError("BoardManager only supports exactly 34 tile sprites for now.");
            return;
        }

        if (requiredTileCount != totalCells)
        {
            Debug.LogError($"Tile count mismatch. Board needs {totalCells} tiles, but 34 x 4 = {requiredTileCount}.");
            return;
        }

        System.Random seededRandom = new System.Random(currentSeed);
        List<TileEntry> shuffledTiles = BuildShuffledTileList(seededRandom);
        remainingFaceUpTiles = shuffledTiles.Count;
        boardTiles = new TileView[boardWidth, boardHeight];

        int index = 0;

        for (int y = 0; y < boardHeight; y++)
        {
            for (int x = 0; x < boardWidth; x++)
            {
                TileEntry entry = shuffledTiles[index];
                Vector2 spawnPosition = GetWorldPosition(x, y);

                TileView spawnedTile = Instantiate(tilePrefab, spawnPosition, Quaternion.identity, tileContainer);
                spawnedTile.name = $"Tile_{x}_{y}_{entry.TileTypeId}";
                spawnedTile.Initialize(entry.Sprite, entry.TileTypeId, new Vector2Int(x, y));

                boardTiles[x, y] = spawnedTile;
                spawnedTiles.Add(spawnedTile);
                index++;
            }
        }
    }

    public void ResolveMatch(TileView tileA, TileView tileB)
    {
        if (tileA == null || tileB == null || tileA.IsPath || tileB.IsPath)
        {
            return;
        }

        tileA.ConvertToPath(backTileSprite);
        tileB.ConvertToPath(backTileSprite);

        remainingFaceUpTiles -= 2;

        Debug.Log($"Matched: {tileA.name} with {tileB.name}");
        Debug.Log($"Remaining face-up tiles: {remainingFaceUpTiles}");

        CheckWinCondition();
        OnStableBoardStateChanged?.Invoke();
    }

    private void CheckWinCondition()
    {
        if (remainingFaceUpTiles == 0)
        {
            Debug.Log($"You win! Seed: {currentSeed}");
            OnBoardWon?.Invoke(currentSeed);
        }
    }

    public List<TileView> GetTilesOfSameType(int tileTypeId, TileView excludeTile = null)
    {
        List<TileView> result = new List<TileView>();

        foreach (TileView tile in spawnedTiles)
        {
            if (tile == null || tile.IsPath)
            {
                continue;
            }

            if (tile == excludeTile)
            {
                continue;
            }

            if (tile.TileTypeId == tileTypeId)
            {
                result.Add(tile);
            }
        }

        return result;
    }

    public List<TileView> GetConnectedTilesInDirection(TileView activeTile, Vector2Int direction)
    {
        List<TileView> connectedTiles = new List<TileView>();

        if (activeTile == null || activeTile.IsPath || direction == Vector2Int.zero)
        {
            return connectedTiles;
        }

        Vector2Int current = activeTile.GridPosition + direction;

        while (IsInsideBoardPosition(current))
        {
            TileView tile = GetTileAt(current);

            if (tile == null || tile.IsPath)
            {
                break;
            }

            connectedTiles.Add(tile);
            current += direction;
        }

        return connectedTiles;
    }

    public int GetMaxMoveSteps(TileView activeTile, List<TileView> connectedTiles, Vector2Int direction)
    {
        if (activeTile == null || activeTile.IsPath || direction == Vector2Int.zero)
        {
            return 0;
        }

        TileView frontTile = connectedTiles.Count > 0
            ? connectedTiles[connectedTiles.Count - 1]
            : activeTile;

        Vector2Int checkPosition = frontTile.GridPosition + direction;
        int steps = 0;

        while (IsInsideBoardPosition(checkPosition) && IsPathAt(checkPosition))
        {
            steps++;
            checkPosition += direction;
        }

        return steps;
    }

    public void MoveTileGroup(TileView activeTile, List<TileView> connectedTiles, Vector2Int direction, int steps)
    {
        if (activeTile == null || activeTile.IsPath || direction == Vector2Int.zero || steps <= 0)
        {
            return;
        }

        List<TileView> movingTiles = new List<TileView> { activeTile };
        movingTiles.AddRange(connectedTiles);

        List<Vector2Int> originalPositions = new List<Vector2Int>();
        foreach (TileView tile in movingTiles)
        {
            originalPositions.Add(tile.GridPosition);
        }

        Vector2Int originalActivePosition = activeTile.GridPosition;

        TileView frontTile = connectedTiles.Count > 0
            ? connectedTiles[connectedTiles.Count - 1]
            : activeTile;

        List<TileView> pathTilesToRecycle = new List<TileView>();

        for (int step = 1; step <= steps; step++)
        {
            Vector2Int pathPosition = frontTile.GridPosition + direction * step;
            TileView pathTile = GetTileAt(pathPosition);

            if (pathTile == null || !pathTile.IsPath)
            {
                Debug.LogError($"Expected a path tile at {pathPosition} while moving.");
                return;
            }

            pathTilesToRecycle.Add(pathTile);
        }

        foreach (Vector2Int position in originalPositions)
        {
            boardTiles[position.x, position.y] = null;
        }

        foreach (TileView pathTile in pathTilesToRecycle)
        {
            Vector2Int oldPathPosition = pathTile.GridPosition;
            boardTiles[oldPathPosition.x, oldPathPosition.y] = null;
        }

        for (int i = 0; i < movingTiles.Count; i++)
        {
            TileView tile = movingTiles[i];
            Vector2Int newPosition = originalPositions[i] + direction * steps;

            boardTiles[newPosition.x, newPosition.y] = tile;
            tile.SetGridPosition(newPosition);
            tile.SetWorldPosition(GetWorldPosition(newPosition));
        }

        for (int i = 0; i < steps; i++)
        {
            TileView pathTile = pathTilesToRecycle[i];
            Vector2Int fillPosition = originalActivePosition + direction * i;

            boardTiles[fillPosition.x, fillPosition.y] = pathTile;
            pathTile.SetGridPosition(fillPosition);
            pathTile.SetWorldPosition(GetWorldPosition(fillPosition));
        }
    }

    public void RestoreMovedTileGroup(
        List<TileView> movingTiles,
        Vector2Int originalActivePosition,
        Vector2Int direction,
        int steps)
    {
        if (movingTiles == null || movingTiles.Count == 0 || direction == Vector2Int.zero || steps <= 0)
        {
            return;
        }

        List<Vector2Int> currentPositions = new List<Vector2Int>();

        foreach (TileView tile in movingTiles)
        {
            if (tile == null)
            {
                Debug.LogError("RestoreMovedTileGroup failed: moving tile is null.");
                return;
            }

            currentPositions.Add(tile.GridPosition);
        }

        List<TileView> recycledPathTiles = new List<TileView>();

        for (int i = 0; i < steps; i++)
        {
            Vector2Int trailPosition = originalActivePosition + direction * i;
            TileView pathTile = GetTileAt(trailPosition);

            if (pathTile == null || !pathTile.IsPath)
            {
                Debug.LogError($"RestoreMovedTileGroup failed: expected a path tile at {trailPosition}.");
                return;
            }

            recycledPathTiles.Add(pathTile);
        }

        foreach (Vector2Int position in currentPositions)
        {
            boardTiles[position.x, position.y] = null;
        }

        for (int i = 0; i < steps; i++)
        {
            Vector2Int trailPosition = originalActivePosition + direction * i;
            boardTiles[trailPosition.x, trailPosition.y] = null;
        }

        for (int i = 0; i < movingTiles.Count; i++)
        {
            TileView tile = movingTiles[i];
            Vector2Int restoredPosition = currentPositions[i] - direction * steps;

            boardTiles[restoredPosition.x, restoredPosition.y] = tile;
            tile.SetGridPosition(restoredPosition);
            tile.SetWorldPosition(GetWorldPosition(restoredPosition));
        }

        Vector2Int originalFrontPosition = originalActivePosition + direction * (movingTiles.Count - 1);

        for (int i = 0; i < recycledPathTiles.Count; i++)
        {
            TileView pathTile = recycledPathTiles[i];
            Vector2Int restoredPathPosition = originalFrontPosition + direction * (i + 1);

            boardTiles[restoredPathPosition.x, restoredPathPosition.y] = pathTile;
            pathTile.SetGridPosition(restoredPathPosition);
            pathTile.SetWorldPosition(GetWorldPosition(restoredPathPosition));
        }
    }

    public TileView GetTileAt(Vector2Int position)
    {
        if (!IsInsideBoardPosition(position) || boardTiles == null)
        {
            return null;
        }

        return boardTiles[position.x, position.y];
    }

    public TileView GetActiveTileAt(Vector2Int position)
    {
        TileView tile = GetTileAt(position);

        if (tile == null || tile.IsPath)
        {
            return null;
        }

        return tile;
    }

    public bool HasTileAt(Vector2Int position)
    {
        return GetActiveTileAt(position) != null;
    }

    public bool IsPathAt(Vector2Int position)
    {
        TileView tile = GetTileAt(position);
        return tile == null || tile.IsPath;
    }

    public bool IsInsideBoardPosition(Vector2Int position)
    {
        return position.x >= 0 &&
               position.x < boardWidth &&
               position.y >= 0 &&
               position.y < boardHeight;
    }

    public Vector3 GetWorldPosition(Vector2Int gridPosition)
    {
        return GetWorldPosition(gridPosition.x, gridPosition.y);
    }

    public float GetTileSpacingX()
    {
        return tileSpacingX;
    }

    public float GetTileSpacingY()
    {
        return tileSpacingY;
    }

    public Sprite GetBackTileSprite()
    {
        return backTileSprite;
    }

    private List<TileEntry> BuildShuffledTileList(System.Random seededRandom)
    {
        List<TileEntry> tilePool = new List<TileEntry>();

        for (int i = 0; i < tileSprites.Length; i++)
        {
            for (int copy = 0; copy < 4; copy++)
            {
                tilePool.Add(new TileEntry(tileSprites[i], i));
            }
        }

        Shuffle(tilePool, seededRandom);
        return tilePool;
    }

    private void Shuffle(List<TileEntry> list, System.Random seededRandom)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int randomIndex = seededRandom.Next(0, i + 1);
            (list[i], list[randomIndex]) = (list[randomIndex], list[i]);
        }
    }

    private Vector2 GetWorldPosition(int x, int y)
    {
        float boardWorldWidth = (boardWidth - 1) * tileSpacingX;
        float boardWorldHeight = (boardHeight - 1) * tileSpacingY;

        float startX = -boardWorldWidth / 2f;
        float startY = boardWorldHeight / 2f;

        return new Vector2(
            startX + x * tileSpacingX,
            startY - y * tileSpacingY
        );
    }

    private void ClearBoard()
    {
        if (tileContainer == null)
        {
            return;
        }

        for (int i = tileContainer.childCount - 1; i >= 0; i--)
        {
            Destroy(tileContainer.GetChild(i).gameObject);
        }

        spawnedTiles.Clear();
    }

    private void CalculateSpacingFromPrefabScale()
    {
        float currentScaleX = tilePrefab.transform.localScale.x;
        float currentScaleY = tilePrefab.transform.localScale.y;

        if (Mathf.Approximately(baseTileScale, 0f))
        {
            Debug.LogError("Base tile scale cannot be 0.");
            tileSpacingX = baseTileSpacingX;
            tileSpacingY = baseTileSpacingY;
            return;
        }

        float scaleRatioX = currentScaleX / baseTileScale;
        float scaleRatioY = currentScaleY / baseTileScale;

        tileSpacingX = baseTileSpacingX * scaleRatioX;
        tileSpacingY = baseTileSpacingY * scaleRatioY;

        Debug.Log($"Prefab scale: ({currentScaleX}, {currentScaleY}), spacing: ({tileSpacingX}, {tileSpacingY})");
    }

    private struct TileEntry
    {
        public Sprite Sprite { get; }
        public int TileTypeId { get; }

        public TileEntry(Sprite sprite, int tileTypeId)
        {
            Sprite = sprite;
            TileTypeId = tileTypeId;
        }
    }

        public bool SwapTiles(TileView tileA, TileView tileB)
    {
        if (tileA == null || tileB == null)
        {
            Debug.LogWarning("SwapTiles failed: one or both tiles are null.");
            return false;
        }

        if (tileA == tileB)
        {
            Debug.LogWarning("SwapTiles failed: cannot swap the same tile.");
            return false;
        }

        Vector2Int positionA = tileA.GridPosition;
        Vector2Int positionB = tileB.GridPosition;

        if (!IsInsideBoardPosition(positionA) || !IsInsideBoardPosition(positionB))
        {
            Debug.LogWarning("SwapTiles failed: one or both tiles are outside the board.");
            return false;
        }

        boardTiles[positionA.x, positionA.y] = tileB;
        boardTiles[positionB.x, positionB.y] = tileA;

        tileA.SetGridPosition(positionB);
        tileA.SetWorldPosition(GetWorldPosition(positionB));

        tileB.SetGridPosition(positionA);
        tileB.SetWorldPosition(GetWorldPosition(positionA));

        Debug.Log($"SwapTiles success: {tileA.name} <-> {tileB.name}");
        return true;
    }



    // Debug Functions
    [ContextMenu("Force Win")]
    public void DebugForceWin()
    {
        foreach (TileView tile in spawnedTiles)
        {
            if (tile != null && !tile.IsPath)
            {
                tile.ConvertToPath(backTileSprite);
            }
        }

        remainingFaceUpTiles = 0;
        Debug.Log("DebugForceWin called. remainingFaceUpTiles set to 0.");
        CheckWinCondition();
    }



    public bool DebugMatchTiles(TileView tileA, TileView tileB)
    {
        if (tileA == null || tileB == null)
        {
            Debug.LogWarning("DebugMatchTiles failed: one or both tiles are null.");
            return false;
        }

        if (tileA == tileB)
        {
            Debug.LogWarning("DebugMatchTiles failed: cannot match the same tile object.");
            return false;
        }

        if (tileA.IsPath || tileB.IsPath)
        {
            Debug.LogWarning("DebugMatchTiles failed: one or both tiles are already path tiles.");
            return false;
        }

        if (tileA.TileTypeId != tileB.TileTypeId)
        {
            Debug.LogWarning("DebugMatchTiles failed: tiles are not the same type.");
            return false;
        }

        ResolveMatch(tileA, tileB);
        Debug.Log($"DebugMatchTiles success: {tileA.name} matched with {tileB.name}");
        return true;
    }

    [ContextMenu("Generate Board With One Opening Clickable Match")]
    public void DebugGenerateBoardWithOneOpeningClickableMatch()
    {
        const int maxAttempts = 5000;

        for (int attempt = 0; attempt < maxAttempts; attempt++)
        {
            int seed = UnityEngine.Random.Range(int.MinValue, int.MaxValue);
            System.Random seededRandom = new System.Random(seed);
            List<TileEntry> shuffledTiles = BuildShuffledTileList(seededRandom);

            if (CountAdjacentOpeningMatches(shuffledTiles) == 1)
            {
                hasGeneratedBoard = true;
                currentSeed = seed;
                GenerateBoard();
                OnStableBoardStateChanged?.Invoke();

                Debug.Log($"Generated debug board with exactly one opening clickable match. Seed: {currentSeed}");
                return;
            }
        }

        Debug.LogWarning("Could not find a board with exactly one opening clickable match.");
    }

    private int CountAdjacentOpeningMatches(List<TileEntry> shuffledTiles)
    {
        int count = 0;

        for (int i = 0; i < shuffledTiles.Count; i++)
        {
            int x1 = i % boardWidth;
            int y1 = i / boardWidth;
            TileEntry tileA = shuffledTiles[i];

            for (int j = i + 1; j < shuffledTiles.Count; j++)
            {
                TileEntry tileB = shuffledTiles[j];

                if (tileA.TileTypeId != tileB.TileTypeId)
                {
                    continue;
                }

                int x2 = j % boardWidth;
                int y2 = j / boardWidth;

                bool adjacentHorizontally = y1 == y2 && Mathf.Abs(x1 - x2) == 1;
                bool adjacentVertically = x1 == x2 && Mathf.Abs(y1 - y2) == 1;

                if (adjacentHorizontally || adjacentVertically)
                {
                    count++;
                }
            }
        }

        return count;
    }

}