using System.Collections.Generic;
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
    [SerializeField] private Vector2 boardOrigin = Vector2.zero;

    [Header("References")]
    [SerializeField] private TileView tilePrefab;
    [SerializeField] private Transform tileContainer;

    [Header("Tile Sprites")]
    [SerializeField] private Sprite[] tileSprites;
    [SerializeField] private Sprite backTileSprite;

    private TileView[,] boardTiles;
    private readonly List<TileView> spawnedTiles = new List<TileView>();

    private void Start()
    {
        GenerateBoard();
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

        List<TileEntry> shuffledTiles = BuildShuffledTileList();
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

    public List<TileView> GetTilesOfSameType(int tileTypeId, TileView excludeTile = null)
    {
        List<TileView> result = new List<TileView>();

        foreach (TileView tile in spawnedTiles)
        {
            if (tile == null)
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

        if (activeTile == null || direction == Vector2Int.zero)
        {
            return connectedTiles;
        }

        Vector2Int current = activeTile.GridPosition + direction;

        while (IsInsideBoard(current))
        {
            TileView tile = GetTileAt(current);

            if (tile == null)
            {
                break;
            }

            connectedTiles.Add(tile);
            current += direction;
        }

        return connectedTiles;
    }

    public TileView GetTileAt(Vector2Int position)
    {
        if (!IsInsideBoard(position) || boardTiles == null)
        {
            return null;
        }

        return boardTiles[position.x, position.y];
    }

    public Vector3 GetWorldPosition(Vector2Int gridPosition)
    {
        return GetWorldPosition(gridPosition.x, gridPosition.y);
    }

    private List<TileEntry> BuildShuffledTileList()
    {
        List<TileEntry> tilePool = new List<TileEntry>();

        for (int i = 0; i < tileSprites.Length; i++)
        {
            for (int copy = 0; copy < 4; copy++)
            {
                tilePool.Add(new TileEntry(tileSprites[i], i));
            }
        }

        Shuffle(tilePool);
        return tilePool;
    }

    private void Shuffle(List<TileEntry> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int randomIndex = Random.Range(0, i + 1);
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

    public bool IsInsideBoard(Vector2Int position)
    {
        return position.x >= 0 &&
               position.x < boardWidth &&
               position.y >= 0 &&
               position.y < boardHeight;
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
    
    public float GetTileSpacingX()
    {
        return tileSpacingX;
    }

    public float GetTileSpacingY()
    {
        return tileSpacingY;
    }

    public bool HasTileAt(Vector2Int position)
    {
        return GetTileAt(position) != null;
    }
    
}