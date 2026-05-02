using System;
using UnityEngine;

public class TileView : MonoBehaviour
{
    private const string HintScanOverlayName = "HintScanOverlay";
    private const string MainTextureProperty = "_MainTex";
    private const string SweepProperty = "_Sweep";
    private const string ReverseProperty = "_Reverse";

    [Header("References")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Material hintFlashMaterialTemplate;

    [Header("Visual Settings")]
    [SerializeField] private Color dragSourceTint = new Color(0.6f, 0.6f, 0.6f, 1f);
    [SerializeField] private Color hintFlashColor = new Color(0.82f, 0.82f, 0.82f, 0.9f);
    [SerializeField] private float hintFlashSweepDuration = 1.0f;
    [SerializeField] private float hintFlashPauseDuration = 1.0f;
    [SerializeField] private int hintFlashSortingOrderOffset = 15;

    private Vector3 originalScale;
    private int originalSortingOrder;
    private Color originalColor;
    private SpriteRenderer hintFlashRenderer;
    private Material hintFlashMaterialInstance;
    private Coroutine hintFlashCoroutine;

    public event Action<TileView> HintFlashCycleCompleted;
    public float HintFlashSweepDuration => hintFlashSweepDuration;

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
        UpdateHintFlashSprite();
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

        UpdateHintFlashSprite();
        ResetVisual();
    }

    public void ConvertToPath(Sprite backTileSprite)
    {
        IsPath = true;

        if (spriteRenderer != null && backTileSprite != null)
        {
            spriteRenderer.sprite = backTileSprite;
        }

        UpdateHintFlashSprite();
        ResetVisual();
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

    public void PlayHintFlash(bool topLeftToBottomRight)
    {
        StartHintFlash(topLeftToBottomRight, true);
    }

    public void PlayHintFlashOnce(bool topLeftToBottomRight)
    {
        StartHintFlash(topLeftToBottomRight, false);
    }

    public void SetHintFlashProgress(bool topLeftToBottomRight, float normalizedProgress)
    {
        EnsureHintFlashRenderer();

        if (hintFlashRenderer == null || hintFlashMaterialInstance == null)
        {
            return;
        }

        if (hintFlashCoroutine != null)
        {
            StopCoroutine(hintFlashCoroutine);
            hintFlashCoroutine = null;
        }

        hintFlashRenderer.transform.localPosition = Vector3.zero;
        hintFlashRenderer.transform.localEulerAngles = Vector3.zero;
        hintFlashRenderer.transform.localScale = Vector3.one;
        hintFlashRenderer.enabled = true;
        hintFlashMaterialInstance.SetFloat(ReverseProperty, topLeftToBottomRight ? 0f : 1f);
        hintFlashMaterialInstance.SetFloat(SweepProperty, Mathf.Lerp(-0.5f, 2.5f, Mathf.Clamp01(normalizedProgress)));
    }

    public void HideHintFlash()
    {
        StopHintFlash();
    }

    private void StartHintFlash(bool topLeftToBottomRight, bool loop)
    {
        EnsureHintFlashRenderer();

        if (hintFlashRenderer == null || hintFlashMaterialInstance == null)
        {
            return;
        }

        if (hintFlashCoroutine != null)
        {
            StopCoroutine(hintFlashCoroutine);
        }

        Debug.Log($"[TileView] Starting hint flash on {name}. Direction: {(topLeftToBottomRight ? "top-left to bottom-right" : "top-right to bottom-left")}");
        hintFlashCoroutine = StartCoroutine(HintFlashLoop(topLeftToBottomRight, loop));
    }

    public void ResetVisual()
    {
        transform.localScale = originalScale;

        if (spriteRenderer != null)
        {
            spriteRenderer.sortingOrder = originalSortingOrder;
            spriteRenderer.color = originalColor;
        }

        StopHintFlash();
    }

    public void SetGridPosition(Vector2Int gridPosition)
    {
        GridPosition = gridPosition;
    }

    public void SetWorldPosition(Vector3 worldPosition)
    {
        transform.position = worldPosition;
    }

    private void StopHintFlash()
    {
        if (hintFlashCoroutine != null)
        {
            StopCoroutine(hintFlashCoroutine);
            hintFlashCoroutine = null;
        }

        if (hintFlashRenderer != null)
        {
            hintFlashRenderer.enabled = false;
        }

        if (hintFlashMaterialInstance != null)
        {
            hintFlashMaterialInstance.SetFloat(SweepProperty, -1f);
        }
    }

    private System.Collections.IEnumerator HintFlashLoop(bool topLeftToBottomRight, bool loop)
    {
        EnsureHintFlashRenderer();

        if (hintFlashRenderer == null || hintFlashMaterialInstance == null)
        {
            yield break;
        }

        hintFlashRenderer.transform.localPosition = Vector3.zero;
        hintFlashRenderer.transform.localEulerAngles = Vector3.zero;
        hintFlashRenderer.transform.localScale = Vector3.one;
        hintFlashMaterialInstance.SetFloat(ReverseProperty, topLeftToBottomRight ? 0f : 1f);

        do
        {
            float elapsed = 0f;
            hintFlashRenderer.enabled = true;

            while (elapsed < hintFlashSweepDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / hintFlashSweepDuration);
                hintFlashMaterialInstance.SetFloat(SweepProperty, Mathf.Lerp(-0.5f, 2.5f, t));

                yield return null;
            }

            hintFlashRenderer.enabled = false;
            HintFlashCycleCompleted?.Invoke(this);
            if (loop)
            {
                yield return new WaitForSeconds(hintFlashPauseDuration);
            }
        }
        while (loop);

        hintFlashCoroutine = null;
    }

    private void EnsureHintFlashRenderer()
    {
        if (hintFlashRenderer != null)
        {
            return;
        }

        GameObject flashObject = new GameObject(HintScanOverlayName);
        flashObject.transform.SetParent(transform, false);

        hintFlashRenderer = flashObject.AddComponent<SpriteRenderer>();
        hintFlashRenderer.sprite = spriteRenderer != null ? spriteRenderer.sprite : null;
        hintFlashRenderer.maskInteraction = SpriteMaskInteraction.None;
        hintFlashRenderer.sortingLayerID = spriteRenderer != null ? spriteRenderer.sortingLayerID : hintFlashRenderer.sortingLayerID;
        hintFlashRenderer.sortingLayerName = spriteRenderer != null ? spriteRenderer.sortingLayerName : hintFlashRenderer.sortingLayerName;
        hintFlashRenderer.sortingOrder = originalSortingOrder + hintFlashSortingOrderOffset;
        hintFlashRenderer.color = hintFlashColor;
        hintFlashRenderer.enabled = false;

        if (hintFlashMaterialTemplate == null)
        {
            Debug.LogWarning($"[TileView] Hint flash material template is missing for {name}.");
            return;
        }

        hintFlashMaterialInstance = new Material(hintFlashMaterialTemplate)
        {
            name = $"{name}_HintScanMaterial"
        };
        hintFlashMaterialInstance.color = hintFlashColor;
        hintFlashMaterialInstance.SetFloat(SweepProperty, -1f);
        hintFlashMaterialInstance.SetFloat(ReverseProperty, 0f);
        hintFlashRenderer.material = hintFlashMaterialInstance;
        UpdateHintFlashSprite();
    }

    private void UpdateHintFlashSprite()
    {
        if (hintFlashRenderer == null || spriteRenderer == null)
        {
            return;
        }

        hintFlashRenderer.sprite = spriteRenderer.sprite;

        if (hintFlashMaterialInstance != null && spriteRenderer.sprite != null)
        {
            hintFlashMaterialInstance.SetTexture(MainTextureProperty, spriteRenderer.sprite.texture);
        }
    }

    private void OnDestroy()
    {
        if (hintFlashMaterialInstance != null)
        {
            Destroy(hintFlashMaterialInstance);
        }
    }

}
