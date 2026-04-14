using UnityEngine;

public class TileView : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private SpriteRenderer spriteRenderer;

    [Header("Visual Settings")]
    [SerializeField] private float enlargedScaleMultiplier = 1.15f;
    

    private Vector3 originalScale;

    public Vector2Int GridPosition { get; private set; }
    private int originalSortingOrder;
    public int TileTypeId { get; private set; }
    public Sprite TileSprite { get; private set; }

    private void Awake()
    {
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }

        originalScale = transform.localScale;

        if (spriteRenderer != null)
        {
            originalSortingOrder = spriteRenderer.sortingOrder;
        }
    }

    public void Initialize(Sprite sprite, int tileTypeId, Vector2Int gridPosition)
    {
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }

        spriteRenderer.sprite = sprite;
        TileSprite = sprite;
        TileTypeId = tileTypeId;
        GridPosition = gridPosition;

        ResetVisual();
    }

    public void SetEnlarged(bool enlarged)
    {
        transform.localScale = enlarged
            ? originalScale * enlargedScaleMultiplier
            : originalScale;

        if (spriteRenderer != null)
        {
            spriteRenderer.sortingOrder = enlarged
                ? originalSortingOrder + 1
                : originalSortingOrder;
        }
    }

    public void ResetVisual()
    {
        transform.localScale = originalScale;

        if (spriteRenderer != null)
        {
            spriteRenderer.sortingOrder = originalSortingOrder;
        }
    }
    
    public Sprite GetSprite()
    {
        return TileSprite;
    }

    public void SetCustomScale(float scaleMultiplier, int sortingOrderOffset = 1)
    {
        transform.localScale = originalScale * scaleMultiplier;

        if (spriteRenderer != null)
        {
            spriteRenderer.sortingOrder = originalSortingOrder + sortingOrderOffset;
        }
    }

}