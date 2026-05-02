using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteAlways]
public class SpriteNumberDisplay : MonoBehaviour
{
    private enum HorizontalAlignment
    {
        Left,
        Center,
        Right
    }

    [Serializable]
    private struct SymbolSprite
    {
        public string symbol;
        public Sprite sprite;
    }

    [Header("Source")]
    [SerializeField] private TMP_Text sourceText;
    [SerializeField] private bool watchSourceEveryFrame = true;
    [SerializeField] private bool hideSourceGraphic = true;

    [Header("Sprites")]
    [SerializeField] private Sprite[] digitSprites = new Sprite[10];
    [SerializeField] private SymbolSprite[] extraSymbols;

    [Header("Layout")]
    [SerializeField] private RectTransform imageContainer;
    [SerializeField] private Vector2 fallbackSpriteSize = new Vector2(32f, 32f);
    [SerializeField] private float fixedSpriteWidth = 10f;
    [SerializeField] private float fixedSpriteHeight = 20f;
    [SerializeField] private float sizeScale = 1f;
    [SerializeField] private float colonBaseWidth = 12f;
    [SerializeField] private float colonScale = 1f;
    [SerializeField] private float spacing = 0f;
    [SerializeField] private HorizontalAlignment horizontalAlignment = HorizontalAlignment.Center;
    [SerializeField] private bool preserveAspect = true;
    [SerializeField] private bool clearUnsupportedCharacters = true;

    private readonly List<Image> spawnedImages = new List<Image>();
    private readonly Dictionary<string, Sprite> symbolLookup = new Dictionary<string, Sprite>();
    private string lastRenderedText = string.Empty;
    private bool restoredSourceGraphic = true;
    private bool previousSourceEnabledState = true;
#if UNITY_EDITOR
    private bool isEditorRefreshQueued;
#endif

    private void Awake()
    {
        EnsureContainer();
        RebuildSpawnedImageList();
        RebuildSymbolLookup();
        ApplySourceVisibility();
        Refresh(forceRefresh: true);
    }

    private void OnEnable()
    {
        EnsureContainer();
        RebuildSpawnedImageList();
        RebuildSymbolLookup();
        ApplySourceVisibility();
        Refresh(forceRefresh: true);
    }

    private void LateUpdate()
    {
        if (!watchSourceEveryFrame)
        {
            return;
        }

        Refresh(forceRefresh: false);
    }

    private void OnValidate()
    {
        EnsureContainer();
        RebuildSpawnedImageList();
        RebuildSymbolLookup();
        ApplySourceVisibility();

#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            QueueEditorRefresh();
            return;
        }
#endif

        Refresh(forceRefresh: true);
    }

    private void OnDisable()
    {
        RestoreSourceVisibility();
    }

    private void OnDestroy()
    {
        RestoreSourceVisibility();
    }

    public void Refresh(bool forceRefresh = true)
    {
        string currentText = sourceText != null ? sourceText.text : string.Empty;

        if (!forceRefresh && string.Equals(lastRenderedText, currentText, StringComparison.Ordinal))
        {
            return;
        }

        lastRenderedText = currentText ?? string.Empty;
        Render(lastRenderedText);
    }

    public void SetDisplayText(string value)
    {
        lastRenderedText = value ?? string.Empty;
        Render(lastRenderedText);
    }

    private void Render(string value)
    {
        EnsureContainer();
        RebuildSpawnedImageList();

        string safeValue = value ?? string.Empty;
        List<Sprite> spritesToRender = BuildSpriteList(safeValue);
        float totalWidth = CalculateTotalWidth(spritesToRender);
        float currentX = GetInitialX(totalWidth);

        for (int i = 0; i < spritesToRender.Count; i++)
        {
            Image image = GetOrCreateImage(i);
            Sprite sprite = spritesToRender[i];
            currentX = ConfigureImage(image, sprite, currentX);
        }

        for (int i = spritesToRender.Count; i < spawnedImages.Count; i++)
        {
            if (spawnedImages[i] != null)
            {
                spawnedImages[i].gameObject.SetActive(false);
            }
        }
    }

    private List<Sprite> BuildSpriteList(string value)
    {
        List<Sprite> sprites = new List<Sprite>();

        for (int i = 0; i < value.Length; i++)
        {
            string symbol = value[i].ToString();
            Sprite sprite = ResolveSprite(symbol);

            if (sprite == null && clearUnsupportedCharacters)
            {
                continue;
            }

            sprites.Add(sprite);
        }

        return sprites;
    }

    private Sprite ResolveSprite(string symbol)
    {
        if (string.IsNullOrEmpty(symbol))
        {
            return null;
        }

        if (symbol.Length == 1 && char.IsDigit(symbol[0]))
        {
            int digit = symbol[0] - '0';
            if (digit >= 0 && digit < digitSprites.Length)
            {
                return digitSprites[digit];
            }
        }

        symbolLookup.TryGetValue(symbol, out Sprite sprite);
        return sprite;
    }

    private float ConfigureImage(Image image, Sprite sprite, float currentX)
    {
        if (image == null)
        {
            return currentX;
        }

        image.gameObject.SetActive(true);
        image.sprite = sprite;
        image.preserveAspect = preserveAspect;
        image.enabled = sprite != null;

        RectTransform rectTransform = image.rectTransform;
        float symbolScale = GetSymbolScale(sprite);
        Vector2 preferredSize = GetPreferredSize(sprite, symbolScale);
        rectTransform.sizeDelta = preferredSize;
        rectTransform.anchoredPosition = new Vector2(currentX, 0f);
        return currentX + preferredSize.x + spacing;
    }

    private float CalculateTotalWidth(List<Sprite> sprites)
    {
        if (sprites == null || sprites.Count == 0)
        {
            return 0f;
        }

        float totalWidth = 0f;

        for (int i = 0; i < sprites.Count; i++)
        {
            totalWidth += GetPreferredSize(sprites[i], GetSymbolScale(sprites[i])).x;
        }

        totalWidth += spacing * Mathf.Max(0, sprites.Count - 1);
        return totalWidth;
    }

    private float GetInitialX(float totalWidth)
    {
        if (imageContainer == null)
        {
            return 0f;
        }

        float containerWidth = imageContainer.rect.width;

        switch (horizontalAlignment)
        {
            case HorizontalAlignment.Right:
                return -containerWidth * imageContainer.pivot.x + (containerWidth - totalWidth);
            case HorizontalAlignment.Center:
                return -containerWidth * imageContainer.pivot.x + (containerWidth - totalWidth) * 0.5f;
            case HorizontalAlignment.Left:
            default:
                return -containerWidth * imageContainer.pivot.x;
        }
    }

    private Vector2 GetPreferredSize(Sprite sprite, float symbolScale)
    {
        if (sprite != null && IsColonSprite(sprite))
        {
            float safeColonScale = Mathf.Max(0f, symbolScale);
            float safeColonWidth = Mathf.Max(0f, colonBaseWidth) * safeColonScale;
            float colonAspectRatio = sprite.rect.width > 0f
                ? sprite.rect.height / sprite.rect.width
                : 1f;

            return new Vector2(
                safeColonWidth,
                safeColonWidth * colonAspectRatio);
        }

        float safeScale = Mathf.Max(0f, symbolScale);
        float safeWidth = Mathf.Max(0f, fixedSpriteWidth) * safeScale;
        float safeHeight = Mathf.Max(0f, fixedSpriteHeight) * safeScale;

        if (safeWidth > 0f && safeHeight > 0f)
        {
            return new Vector2(safeWidth, safeHeight);
        }

        if (sprite == null)
        {
            return fallbackSpriteSize;
        }

        Rect rect = sprite.rect;
        return rect.width > 0f && rect.height > 0f
            ? new Vector2(rect.width, rect.height)
            : fallbackSpriteSize;
    }

    private float GetSymbolScale(Sprite sprite)
    {
        if (sprite != null && IsColonSprite(sprite))
        {
            return colonScale;
        }

        return sizeScale;
    }

    private bool IsColonSprite(Sprite sprite)
    {
        if (sprite == null)
        {
            return false;
        }

        if (symbolLookup.TryGetValue(":", out Sprite colonSprite))
        {
            return sprite == colonSprite;
        }

        return false;
    }

    private Image GetOrCreateImage(int index)
    {
        while (spawnedImages.Count <= index)
        {
            spawnedImages.Add(CreateImage(spawnedImages.Count));
        }

        return spawnedImages[index];
    }

    private Image CreateImage(int index)
    {
        GameObject child = new GameObject($"Digit_{index}", typeof(RectTransform), typeof(Image));
        RectTransform rectTransform = child.GetComponent<RectTransform>();
        rectTransform.SetParent(imageContainer, false);
        rectTransform.localScale = Vector3.one;
        rectTransform.anchorMin = new Vector2(0f, 0.5f);
        rectTransform.anchorMax = new Vector2(0f, 0.5f);
        rectTransform.pivot = new Vector2(0f, 0.5f);
        return child.GetComponent<Image>();
    }

    private void EnsureContainer()
    {
        if (imageContainer == null)
        {
            imageContainer = transform as RectTransform;
        }
    }

    private void RebuildSpawnedImageList()
    {
        spawnedImages.Clear();

        if (imageContainer == null)
        {
            return;
        }

        for (int i = 0; i < imageContainer.childCount; i++)
        {
            Image childImage = imageContainer.GetChild(i).GetComponent<Image>();
            if (childImage != null)
            {
                spawnedImages.Add(childImage);
            }
        }
    }

    private void RebuildSymbolLookup()
    {
        symbolLookup.Clear();

        if (extraSymbols == null)
        {
            return;
        }

        for (int i = 0; i < extraSymbols.Length; i++)
        {
            SymbolSprite entry = extraSymbols[i];
            if (string.IsNullOrEmpty(entry.symbol) || entry.sprite == null)
            {
                continue;
            }

            symbolLookup[entry.symbol] = entry.sprite;
        }
    }

    private void ApplySourceVisibility()
    {
        if (sourceText == null || !hideSourceGraphic || !Application.isPlaying || !restoredSourceGraphic)
        {
            return;
        }

        previousSourceEnabledState = sourceText.enabled;
        sourceText.enabled = false;
        restoredSourceGraphic = false;
    }

    private void RestoreSourceVisibility()
    {
        if (sourceText == null || restoredSourceGraphic)
        {
            return;
        }

        sourceText.enabled = previousSourceEnabledState;
        restoredSourceGraphic = true;
    }

#if UNITY_EDITOR
    private void QueueEditorRefresh()
    {
        if (isEditorRefreshQueued)
        {
            return;
        }

        isEditorRefreshQueued = true;
        EditorApplication.delayCall += RefreshInEditor;
    }

    private void RefreshInEditor()
    {
        isEditorRefreshQueued = false;

        if (this == null)
        {
            return;
        }

        EnsureContainer();
        RebuildSpawnedImageList();
        Refresh(forceRefresh: true);
    }
#endif
}
