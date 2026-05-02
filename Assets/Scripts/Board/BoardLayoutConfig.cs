using System;
using UnityEngine;

[Serializable]
public sealed class BoardLayoutConfig
{
    private const float BaseTileScaleX = 0.244f;
    private const float BaseTileScaleY = 0.248f;
    private const float BaseTileSpacingX = 0.61f;
    private const float BaseTileSpacingY = 0.81f;
    private const int ReferenceBoardWidth = 17;
    private const int ReferenceBoardHeight = 8;

    private static readonly BoardLayoutPreset Small12x6Preset = new BoardLayoutPreset(12, 6);
    private static readonly BoardLayoutPreset Medium14x8Preset = new BoardLayoutPreset(14, 8);
    private static readonly BoardLayoutPreset Large17x8Preset = new BoardLayoutPreset(17, 8);

    public int BaseBoardWidth => ReferenceBoardWidth;
    public int BaseBoardHeight => ReferenceBoardHeight;
    public BoardLayoutPreset Small12x6 => Small12x6Preset;
    public BoardLayoutPreset Medium14x8 => Medium14x8Preset;
    public BoardLayoutPreset Large17x8 => Large17x8Preset;

    public static BoardLayoutConfig CreateDefault()
    {
        return new BoardLayoutConfig();
    }

    public float CalculateBoardScaleMultiplier(int boardWidth, int boardHeight)
    {
        if (boardWidth <= 0 || boardHeight <= 0)
        {
            return 0f;
        }

        float widthRatio = (float)ReferenceBoardWidth / boardWidth;
        float heightRatio = (float)ReferenceBoardHeight / boardHeight;
        return Mathf.Min(widthRatio, heightRatio);
    }

    public Vector2 CalculateSpacing(int boardWidth, int boardHeight, Vector3 tilePrefabScale)
    {
        if (Mathf.Approximately(BaseTileScaleX, 0f) ||
            Mathf.Approximately(BaseTileScaleY, 0f))
        {
            return Vector2.zero;
        }

        float boardScaleMultiplier = CalculateBoardScaleMultiplier(boardWidth, boardHeight);

        if (Mathf.Approximately(boardScaleMultiplier, 0f))
        {
            return Vector2.zero;
        }

        float scaleRatioX = tilePrefabScale.x / BaseTileScaleX;
        float scaleRatioY = tilePrefabScale.y / BaseTileScaleY;

        return new Vector2(
            BaseTileSpacingX * scaleRatioX * boardScaleMultiplier,
            BaseTileSpacingY * scaleRatioY * boardScaleMultiplier);
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
        private readonly int width;
        private readonly int height;

        public int Width => width;
        public int Height => height;

        public BoardLayoutPreset(int width, int height)
        {
            this.width = width;
            this.height = height;
        }

        public bool Matches(int candidateWidth, int candidateHeight)
        {
            return width == candidateWidth && height == candidateHeight;
        }
    }
}
