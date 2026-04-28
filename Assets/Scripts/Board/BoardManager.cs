using System.Collections.Generic;
using System;
using UnityEngine;

public class BoardManager : MonoBehaviour
{
    private static int nextNewGameBoardWidth = 17;
    private static int nextNewGameBoardHeight = 8;

    private enum BoardSizePreset
    {
        Large17x8,
        Medium14x8,
        Small12x6
    }

    [Header("Board Settings")]
    [SerializeField] private int boardWidth = 17;
    [SerializeField] private int boardHeight = 8;
    [SerializeField] private BoardSizePreset currentBoardSizePreset = BoardSizePreset.Large17x8;

    [Header("Base Layout Tuning")]
    [SerializeField] private float baseTileScale = 0.2f;
    [SerializeField] private float TileSpacing8X = 0.64f;
    [SerializeField] private float TileSpacing8Y = 0.86f;
    [SerializeField] private int referenceBoardHeight = 8;

    [Header("12x6 Layout Tuning")]
    [SerializeField] private float TileSpacing6X = 0.85333335f;
    [SerializeField] private float TileSpacing6Y = 1.1466666f;
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
    public event Action OnBoardGenerated;
    public event Action OnNewRandomBoardGenerated;
    public event Action OnBoardRestarted;
    public event Action<TileView, TileView> OnTilesMatched;

    [Header("References")]
    [SerializeField] private TileView tilePrefab;
    [SerializeField] private Transform tileContainer;
    [SerializeField] private ItemInventory itemInventory;

    [Header("Tile Sprites")]
    [SerializeField] private Sprite[] tileSprites;
    [SerializeField] private Sprite backTileSprite;


    private TileView[,] boardTiles;
    private readonly List<TileView> spawnedTiles = new List<TileView>();

    [Header("Item Settings")]
    [SerializeField] private int maxUndoHistory = 99;
    [SerializeField] private int maxShuffleAttempts = 200;
    public event Action OnSwapPerformed;

    private readonly List<BoardSnapshot> undoHistory = new List<BoardSnapshot>();

    private sealed class BoardSnapshot
    {
        public int RemainingFaceUpTiles;
        public TileState[] TileStates;
        public ItemInventory.SaveData InventoryState;
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

    [Serializable]
    public sealed class SaveData
    {
        public int Seed;
        public int BoardWidth;
        public int BoardHeight;
        public int RemainingFaceUpTiles;
        public List<SavedTileState> Tiles;
    }

    [Serializable]
    public sealed class SavedTileState
    {
        public int TileTypeId;
        public int X;
        public int Y;
        public bool IsPath;
    }

    private void Start()
    {
        ApplyPendingNewGameBoardSize();

        if (GameManager.ShouldSkipBoardAutoGenerateOnSceneStart())
        {
            return;
        }

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
        ApplyBoardSizePreset(currentBoardSizePreset);
        int newSeed = UnityEngine.Random.Range(int.MinValue, int.MaxValue);
        GenerateBoardFromSeed(newSeed);
        OnNewRandomBoardGenerated?.Invoke();
    }

    public static void SetNextNewGameBoardSize(int width, int height)
    {
        nextNewGameBoardWidth = width;
        nextNewGameBoardHeight = height;
    }

    public void ReplayCurrentBoard()
    {
        if (!hasGeneratedBoard)
        {
            GenerateNewBoard();
            return;
        }

        GenerateBoardFromSeed(currentSeed);
        OnBoardRestarted?.Invoke();
    }

    public void GenerateBoardFromSeed(int seed)
    {
        ApplyBoardSizePreset(currentBoardSizePreset);
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
            List<TileEntry> shuffledTiles = BuildShuffledTileList(seededRandom, candidateSeed);
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

    public bool HasFaceDownTiles()
    {
        foreach (TileView tile in spawnedTiles)
        {
            if (tile != null && tile.IsPath)
            {
                return true;
            }
        }

        return false;
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
            TileStates = new TileState[spawnedTiles.Count],
            InventoryState = itemInventory != null ? itemInventory.CaptureSaveData() : null
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

        if (itemInventory != null && snapshot.InventoryState != null)
        {
            itemInventory.RestoreFromSaveData(snapshot.InventoryState);
        }

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
        int[] activeTileSpriteIds = GetActiveTileSpriteIds(currentSeed);
        int requiredTileCount = activeTileSpriteIds.Length * 4;

        if (tileSprites == null || tileSprites.Length != 34)
        {
            Debug.LogError("BoardManager only supports exactly 34 tile sprites for now.");
            return;
        }

        if (requiredTileCount != totalCells)
        {
            Debug.LogError($"Tile count mismatch. Board needs {totalCells} tiles, but the selected sprite pool creates {requiredTileCount} tiles.");
            return;
        }

        System.Random seededRandom = new System.Random(currentSeed);
        List<TileEntry> shuffledTiles = BuildShuffledTileList(seededRandom, currentSeed);
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
                ApplyTileScale(spawnedTile);
                spawnedTile.name = $"Tile_{x}_{y}_{entry.TileTypeId}";
                spawnedTile.Initialize(entry.Sprite, entry.TileTypeId, new Vector2Int(x, y));

                boardTiles[x, y] = spawnedTile;
                spawnedTiles.Add(spawnedTile);
                index++;
            }
        }
    }

    public SaveData CaptureSaveData()
    {
        SaveData saveData = new SaveData
        {
            Seed = currentSeed,
            BoardWidth = boardWidth,
            BoardHeight = boardHeight,
            RemainingFaceUpTiles = remainingFaceUpTiles,
            Tiles = new List<SavedTileState>()
        };

        foreach (TileView tile in spawnedTiles)
        {
            if (tile == null)
            {
                continue;
            }

            saveData.Tiles.Add(new SavedTileState
            {
                TileTypeId = tile.TileTypeId,
                X = tile.GridPosition.x,
                Y = tile.GridPosition.y,
                IsPath = tile.IsPath
            });
        }

        return saveData;
    }

    public bool RestoreFromSaveData(SaveData saveData)
    {
        if (saveData == null || saveData.Tiles == null)
        {
            Debug.LogWarning("RestoreFromSaveData failed: save data is missing.");
            return false;
        }

        if (tileSprites == null || tileSprites.Length != 34)
        {
            Debug.LogError("RestoreFromSaveData failed: BoardManager needs exactly 34 tile sprites.");
            return false;
        }

        if (!TryApplyBoardSizePreset(saveData.BoardWidth, saveData.BoardHeight))
        {
            Debug.LogWarning($"RestoreFromSaveData failed: unsupported saved board size {saveData.BoardWidth}x{saveData.BoardHeight}.");
            return false;
        }

        ClearBoard();
        CalculateSpacingFromPrefabScale();

        hasGeneratedBoard = true;
        currentSeed = saveData.Seed;
        remainingFaceUpTiles = saveData.RemainingFaceUpTiles;
        boardTiles = new TileView[boardWidth, boardHeight];

        foreach (SavedTileState tileState in saveData.Tiles)
        {
            if (!IsInsideBoardPosition(new Vector2Int(tileState.X, tileState.Y)))
            {
                Debug.LogWarning($"RestoreFromSaveData skipped tile outside board: ({tileState.X}, {tileState.Y}).");
                continue;
            }

            if (tileState.TileTypeId < 0 || tileState.TileTypeId >= tileSprites.Length)
            {
                Debug.LogWarning($"RestoreFromSaveData skipped tile with invalid type: {tileState.TileTypeId}.");
                continue;
            }

            Vector2Int gridPosition = new Vector2Int(tileState.X, tileState.Y);
            TileView spawnedTile = Instantiate(tilePrefab, GetWorldPosition(gridPosition), Quaternion.identity, tileContainer);
            ApplyTileScale(spawnedTile);
            spawnedTile.name = $"Tile_{tileState.X}_{tileState.Y}_{tileState.TileTypeId}";
            spawnedTile.Initialize(tileSprites[tileState.TileTypeId], tileState.TileTypeId, gridPosition);

            if (tileState.IsPath)
            {
                spawnedTile.ConvertToPath(backTileSprite);
            }

            boardTiles[tileState.X, tileState.Y] = spawnedTile;
            spawnedTiles.Add(spawnedTile);
        }

        ClearUndoHistory();
        OnStableBoardStateChanged?.Invoke();
        return true;
    }

    public void ResolveMatch(TileView tileA, TileView tileB)
    {
        if (tileA == null || tileB == null || tileA.IsPath || tileB.IsPath)
        {
            return;
        }

        OnTilesMatched?.Invoke(tileA, tileB);
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

    public bool TryShuffleRemainingTiles()
    {
        if (boardTiles == null)
        {
            Debug.LogWarning("Shuffle failed: board has not been generated yet.");
            return false;
        }

        List<TileView> activeTiles = new List<TileView>();
        List<Vector2Int> originalPositions = new List<Vector2Int>();

        foreach (TileView tile in spawnedTiles)
        {
            if (tile == null || tile.IsPath)
            {
                continue;
            }

            activeTiles.Add(tile);
            originalPositions.Add(tile.GridPosition);
        }

        if (activeTiles.Count <= 1)
        {
            Debug.LogWarning("Shuffle failed: not enough remaining tiles.");
            return false;
        }

        System.Random random = new System.Random();

        for (int attempt = 0; attempt < maxShuffleAttempts; attempt++)
        {
            List<Vector2Int> shuffledPositions = new List<Vector2Int>(originalPositions);
            ShuffleVector2IntList(shuffledPositions, random);

            if (!HasAnyTilePositionChanged(activeTiles, shuffledPositions))
            {
                continue;
            }

            ApplyShuffledTilePositions(activeTiles, shuffledPositions);

            if (BoardMoveAnalyzer.HasAnyAvailableMove(this))
            {
                OnStableBoardStateChanged?.Invoke();
                return true;
            }
        }

        ApplyShuffledTilePositions(activeTiles, originalPositions);

        Debug.LogWarning($"Shuffle failed: could not find a valid shuffled layout within {maxShuffleAttempts} attempts.");
        return false;
    }

    private bool HasAnyTilePositionChanged(List<TileView> activeTiles, List<Vector2Int> targetPositions)
    {
        for (int i = 0; i < activeTiles.Count; i++)
        {
            if (activeTiles[i].GridPosition != targetPositions[i])
            {
                return true;
            }
        }

        return false;
    }

    private void ApplyShuffledTilePositions(List<TileView> activeTiles, List<Vector2Int> targetPositions)
    {
        foreach (TileView tile in activeTiles)
        {
            Vector2Int oldPosition = tile.GridPosition;
            boardTiles[oldPosition.x, oldPosition.y] = null;
        }

        for (int i = 0; i < activeTiles.Count; i++)
        {
            TileView tile = activeTiles[i];
            Vector2Int newPosition = targetPositions[i];

            boardTiles[newPosition.x, newPosition.y] = tile;
            tile.SetGridPosition(newPosition);
            tile.SetWorldPosition(GetWorldPosition(newPosition));
        }
    }

    private void ShuffleVector2IntList(List<Vector2Int> list, System.Random random)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int randomIndex = random.Next(0, i + 1);
            (list[i], list[randomIndex]) = (list[randomIndex], list[i]);
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

    private List<TileEntry> BuildShuffledTileList(System.Random seededRandom, int boardSeed)
    {
        List<TileEntry> tilePool = new List<TileEntry>();
        int[] activeTileSpriteIds = GetActiveTileSpriteIds(boardSeed);

        for (int i = 0; i < activeTileSpriteIds.Length; i++)
        {
            for (int copy = 0; copy < 4; copy++)
            {
                int tileSpriteId = activeTileSpriteIds[i];
                tilePool.Add(new TileEntry(tileSprites[tileSpriteId], tileSpriteId));
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
        if (Mathf.Approximately(baseTileScale, 0f))
        {
            Debug.LogError("Base tile scale cannot be 0.");
            tileSpacingX = TileSpacing8X;
            tileSpacingY = TileSpacing8Y;
            return;
        }

        float currentScaleX = tilePrefab.transform.localScale.x;
        float currentScaleY = tilePrefab.transform.localScale.y;
        float scaleRatioX = currentScaleX / baseTileScale;
        float scaleRatioY = currentScaleY / baseTileScale;

        float spacingBaseX = TileSpacing8X;
        float spacingBaseY = TileSpacing8Y;

        if (currentBoardSizePreset == BoardSizePreset.Small12x6)
        {
            spacingBaseX = TileSpacing6X;
            spacingBaseY = TileSpacing6Y;
        }

        tileSpacingX = spacingBaseX * scaleRatioX;
        tileSpacingY = spacingBaseY * scaleRatioY;

        Debug.Log($"Board size: {boardWidth}x{boardHeight}, prefab scale: ({currentScaleX}, {currentScaleY}), spacing: ({tileSpacingX}, {tileSpacingY})");
    }

    private void SelectRandomBoardSizePreset()
    {
        Array presets = Enum.GetValues(typeof(BoardSizePreset));
        currentBoardSizePreset = (BoardSizePreset)presets.GetValue(UnityEngine.Random.Range(0, presets.Length));
        ApplyBoardSizePreset(currentBoardSizePreset);
    }

    private void ApplyBoardSizePreset(BoardSizePreset preset)
    {
        currentBoardSizePreset = preset;

        switch (preset)
        {
            case BoardSizePreset.Small12x6:
                boardWidth = 12;
                boardHeight = 6;
                break;
            case BoardSizePreset.Medium14x8:
                boardWidth = 14;
                boardHeight = 8;
                break;
            case BoardSizePreset.Large17x8:
            default:
                boardWidth = 17;
                boardHeight = 8;
                break;
        }
    }

    private bool TryApplyBoardSizePreset(int width, int height)
    {
        if (width == 12 && height == 6)
        {
            ApplyBoardSizePreset(BoardSizePreset.Small12x6);
            return true;
        }

        if (width == 14 && height == 8)
        {
            ApplyBoardSizePreset(BoardSizePreset.Medium14x8);
            return true;
        }

        if (width == 17 && height == 8)
        {
            ApplyBoardSizePreset(BoardSizePreset.Large17x8);
            return true;
        }

        return false;
    }

    private void ApplyPendingNewGameBoardSize()
    {
        if (!TryApplyBoardSizePreset(nextNewGameBoardWidth, nextNewGameBoardHeight))
        {
            ApplyBoardSizePreset(BoardSizePreset.Large17x8);
        }
    }

    private int[] GetActiveTileSpriteIds(int boardSeed)
    {
        switch (currentBoardSizePreset)
        {
            case BoardSizePreset.Small12x6:
                return BuildSmallBoardTileSpriteIds(boardSeed);
            case BoardSizePreset.Medium14x8:
                return BuildActiveTileSpriteIdArray(0, 26, 31);
            case BoardSizePreset.Large17x8:
            default:
                return BuildActiveTileSpriteIdArray(0, 33);
        }
    }

    private int[] BuildSmallBoardTileSpriteIds(int boardSeed)
    {
        int[][] suitSets =
        {
            BuildActiveTileSpriteIdArray(0, 8),
            BuildActiveTileSpriteIdArray(9, 17),
            BuildActiveTileSpriteIdArray(18, 26)
        };

        int[][] suitSetPairs =
        {
            new[] { 0, 1 },
            new[] { 0, 2 },
            new[] { 1, 2 }
        };

        int pairIndex = Mathf.Abs(boardSeed % suitSetPairs.Length);
        int[] selectedPair = suitSetPairs[pairIndex];
        int[] activeSpriteIds = new int[18];

        for (int i = 0; i < 9; i++)
        {
            activeSpriteIds[i] = suitSets[selectedPair[0]][i];
            activeSpriteIds[i + 9] = suitSets[selectedPair[1]][i];
        }

        return activeSpriteIds;
    }

    private int[] BuildActiveTileSpriteIdArray(int startInclusive, int endInclusive)
    {
        int length = endInclusive - startInclusive + 1;
        int[] activeSpriteIds = new int[length];

        for (int i = 0; i < length; i++)
        {
            activeSpriteIds[i] = startInclusive + i;
        }

        return activeSpriteIds;
    }

    private int[] BuildActiveTileSpriteIdArray(int startInclusive, int endInclusive, int extraSpriteIndex)
    {
        int baseLength = endInclusive - startInclusive + 1;
        int[] activeSpriteIds = new int[baseLength + 1];

        for (int i = 0; i < baseLength; i++)
        {
            activeSpriteIds[i] = startInclusive + i;
        }

        activeSpriteIds[baseLength] = extraSpriteIndex;
        return activeSpriteIds;
    }

    private float GetCurrentTileScaleMultiplier()
    {
        int safeReferenceHeight = Mathf.Max(1, referenceBoardHeight);
        int safeBoardHeight = Mathf.Max(1, boardHeight);
        return (float)safeReferenceHeight / safeBoardHeight;
    }

    private void ApplyTileScale(TileView tile)
    {
        if (tile == null || tilePrefab == null)
        {
            return;
        }

        float scaleMultiplier = GetCurrentTileScaleMultiplier();
        Vector3 scaledTileSize = tilePrefab.transform.localScale * scaleMultiplier;
        tile.SetBaseScale(scaledTileSize);
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
        OnSwapPerformed?.Invoke();
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
            List<TileEntry> shuffledTiles = BuildShuffledTileList(seededRandom, seed);

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
