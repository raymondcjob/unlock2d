using UnityEngine;

public class TileView : MonoBehaviour
{
    [SerializeField] private SpriteRenderer spriteRenderer;

    public Vector2Int GridPosition { get; private set; }
    public int TileTypeId { get; private set; }

    public void Initialize(Sprite sprite, int tileTypeId, Vector2Int gridPosition)
    {
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }

        spriteRenderer.sprite = sprite;
        TileTypeId = tileTypeId;
        GridPosition = gridPosition;
    }
}