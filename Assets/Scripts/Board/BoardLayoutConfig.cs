using System;
using UnityEngine;

[Serializable]
public sealed class BoardLayoutConfig
{
    [SerializeField] private float baseTileScale = 0.2f;
    [SerializeField] private int referenceBoardHeight = 8;
    [SerializeField] private BoardLayoutPreset small12x6 = new BoardLayoutPreset(12, 6, 0.85333335f, 1.1466666f);
    [SerializeField] private BoardLayoutPreset medium14x8 = new BoardLayoutPreset(14, 8, 0.64f, 0.86f);
    [SerializeField] private BoardLayoutPreset large17x8 = new BoardLayoutPreset(17, 8, 0.64f, 0.86f);

    public int ReferenceBoardHeight => referenceBoardHeight;
    public BoardLayoutPreset Small12x6 => small12x6;
    public BoardLayoutPreset Medium14x8 => medium14x8;
    public BoardLayoutPreset Large17x8 => large17x8;

    public static BoardLayoutConfig CreateDefault()
    {
        return new BoardLayoutConfig();
    }

    public Vector2 CalculateSpacing(BoardLayoutPreset preset, Vector3 tilePrefabScale)
    {
        if (preset == null || Mathf.Approximately(baseTileScale, 0f))
        {
            return Vector2.zero;
        }

        float scaleRatioX = tilePrefabScale.x / baseTileScale;
        float scaleRatioY = tilePrefabScale.y / baseTileScale;

        return new Vector2(
            preset.TileSpacingX * scaleRatioX,
            preset.TileSpacingY * scaleRatioY);
    }

    public Vector2 GetWorldPosition(int boardWidth, int boardHeight, float tileSpacingX, float tileSpacingY, int x, int y)
    {
        float boardWorldWidth = (boardWidth - 1) * tileSpacingX;
        float boardWorldHeight = (boardHeight - 1) * tileSpacingY;

        float startX = -boardWorldWidth / 2f;
        float startY = boardWorldHeight / 2f;

        return new Vector2(
            startX + x * tileSpacingX,
            startY - y * tileSpacingY);
    }

    [Serializable]
    public sealed class BoardLayoutPreset
    {
        [SerializeField] private int width;
        [SerializeField] private int height;
        [SerializeField] private float tileSpacingX;
        [SerializeField] private float tileSpacingY;

        public int Width => width;
        public int Height => height;
        public float TileSpacingX => tileSpacingX;
        public float TileSpacingY => tileSpacingY;

        public BoardLayoutPreset(int width, int height, float tileSpacingX, float tileSpacingY)
        {
            this.width = width;
            this.height = height;
            this.tileSpacingX = tileSpacingX;
            this.tileSpacingY = tileSpacingY;
        }

        public bool Matches(int candidateWidth, int candidateHeight)
        {
            return width == candidateWidth && height == candidateHeight;
        }
    }
}
