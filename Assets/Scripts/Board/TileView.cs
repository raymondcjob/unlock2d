using UnityEngine;

public class TileView : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private SpriteRenderer spriteRenderer;

    [Header("Visual Settings")]
    [SerializeField] private float enlargedScaleMultiplier = 1.2f;
    [SerializeField] private Color dragSourceTint = new Color(0.6f, 0.6f, 0.6f, 1f);

    private Vector3 originalScale;
    private int originalSortingOrder;
    private Color originalColor;

    public Vector2Int GridPosition { get; private set; }
    public int TileTypeId { get; private set; }
    public Sprite FaceUpSprite { get; private set; }
    public bool IsPath { get; private set; }

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
            originalColor = spriteRenderer.color;
        }
    }

    public void Initialize(Sprite faceUpSprite, int tileTypeId, Vector2Int gridPosition)
    {
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }

        FaceUpSprite = faceUpSprite;
        TileTypeId = tileTypeId;
        GridPosition = gridPosition;
        IsPath = false;

        spriteRenderer.sprite = FaceUpSprite;
        ResetVisual();
    }

    public void ApplyCellState(Sprite faceUpSprite, int tileTypeId, bool isPath, Sprite backTileSprite)
    {
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }

        FaceUpSprite = faceUpSprite;
        TileTypeId = tileTypeId;
        IsPath = isPath;

        if (spriteRenderer != null)
        {
            spriteRenderer.sprite = isPath && backTileSprite != null
                ? backTileSprite
                : FaceUpSprite;
        }

        ResetVisual();
    }

    public void ConvertToPath(Sprite backTileSprite)
    {
        IsPath = true;

        if (spriteRenderer != null && backTileSprite != null)
        {
            spriteRenderer.sprite = backTileSprite;
        }

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
                ? originalSortingOrder + 10
                : originalSortingOrder;
        }
    }

    public void SetCustomScale(float scaleMultiplier, int sortingOrderOffset = 10)
    {
        transform.localScale = originalScale * scaleMultiplier;

        if (spriteRenderer != null)
        {
            spriteRenderer.sortingOrder = originalSortingOrder + sortingOrderOffset;
        }
    }

    public void SetBaseScale(Vector3 baseScale)
    {
        originalScale = baseScale;
        transform.localScale = baseScale;
    }

    public void SetDragSourceTint(bool tinted)
    {
        if (spriteRenderer == null)
        {
            return;
        }

        spriteRenderer.color = tinted ? dragSourceTint : originalColor;
    }

    public void ResetVisual()
    {
        transform.localScale = originalScale;

        if (spriteRenderer != null)
        {
            spriteRenderer.sortingOrder = originalSortingOrder;
            spriteRenderer.color = originalColor;
        }
    }

    public void SetGridPosition(Vector2Int gridPosition)
    {
        GridPosition = gridPosition;
    }

    public void SetWorldPosition(Vector3 worldPosition)
    {
        transform.position = worldPosition;
    }

}
