using System.Collections.Generic;
using UnityEngine;

public class BoardManager : MonoBehaviour
{
    [Header("Board Settings")]
    [SerializeField] private int boardWidth = 17;
    [SerializeField] private int boardHeight = 8;
    [SerializeField] private float tileSpacingX = 0.64f;
    [SerializeField] private float tileSpacingY = 0.86f;
    [SerializeField] private Vector2 boardOrigin = Vector2.zero;

    [Header("References")]
    [SerializeField] private TileView tilePrefab;
    [SerializeField] private Transform tileContainer;

    [Header("Tile Sprites")]
    [SerializeField] private Sprite[] tileSprites; // Must contain exactly 34 sprites
    [SerializeField] private Sprite backTileSprite;

    private TileView[,] boardTiles;

    private void Start()
    {
        GenerateBoard();
    }

    public void GenerateBoard()
    {
        ClearBoard();

        int totalCells = boardWidth * boardHeight;
        int requiredTileCount = tileSprites.Length * 4;

        if (tileSprites == null || tileSprites.Length != 34)
        {
            Debug.LogError("BoardManager requires exactly 34 tile sprites.");
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
                index++;
            }
        }
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

    
}