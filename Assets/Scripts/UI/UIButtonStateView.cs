using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UIButtonStateView : MonoBehaviour
{
    [System.Serializable]
    private struct SpriteSwapTarget
    {
        public Image image;
        public Sprite disabledSprite;
    }

    [SerializeField] private Button button;
    [SerializeField] private bool includeChildGraphics;
    [SerializeField] private bool tintTextGraphics;
    [SerializeField] private bool tintGraphicsWhenDisabled = true;
    [SerializeField] private Color disabledColor = new Color(0.7f, 0.7f, 0.7f, 1f);
    [SerializeField] private Graphic[] targetGraphics;
    [SerializeField] private SpriteSwapTarget[] spriteSwapTargets;

    private ColorBlock originalButtonColors;
    private Color[] originalGraphicColors;
    private Sprite[] originalSprites;
    private bool hasResolvedReferences;

    private void Awake()
    {
        ResolveReferencesIfNeeded();
    }

    public void SetAvailable(bool available)
    {
        ResolveReferencesIfNeeded();

        if (button != null)
        {
            ColorBlock colors = originalButtonColors;
            colors.disabledColor = disabledColor;
            colors.disabledColor = new Color(
                colors.disabledColor.r,
                colors.disabledColor.g,
                colors.disabledColor.b,
                1f);
            button.colors = colors;
            button.interactable = available;
        }

        for (int i = 0; i < targetGraphics.Length; i++)
        {
            Graphic graphic = targetGraphics[i];

            if (graphic == null)
            {
                continue;
            }

            if (!tintTextGraphics && graphic is TMP_Text)
            {
                graphic.color = originalGraphicColors[i];
                continue;
            }

            graphic.color = available || !tintGraphicsWhenDisabled
                ? originalGraphicColors[i]
                : GetOpaqueDisabledColor(originalGraphicColors[i]);
        }

        for (int i = 0; i < spriteSwapTargets.Length; i++)
        {
            SpriteSwapTarget spriteSwapTarget = spriteSwapTargets[i];

            if (spriteSwapTarget.image == null)
            {
                continue;
            }

            spriteSwapTarget.image.sprite = !available && spriteSwapTarget.disabledSprite != null
                ? spriteSwapTarget.disabledSprite
                : originalSprites[i];
        }
    }

    private void ResolveReferencesIfNeeded()
    {
        if (hasResolvedReferences)
        {
            return;
        }

        if (button == null)
        {
            button = GetComponent<Button>();
        }

        if (button != null)
        {
            originalButtonColors = button.colors;
        }

        if (targetGraphics != null && targetGraphics.Length > 0)
        {
            CacheOriginalGraphicColors();
            CacheOriginalSprites();
            hasResolvedReferences = true;
            return;
        }

        targetGraphics = includeChildGraphics
            ? GetComponentsInChildren<Graphic>(true)
            : new[] { GetComponent<Graphic>() };

        CacheOriginalGraphicColors();
        CacheOriginalSprites();
        hasResolvedReferences = true;
    }

    private void CacheOriginalGraphicColors()
    {
        originalGraphicColors = new Color[targetGraphics.Length];

        for (int i = 0; i < targetGraphics.Length; i++)
        {
            originalGraphicColors[i] = targetGraphics[i] != null
                ? targetGraphics[i].color
                : Color.white;
        }
    }

    private void CacheOriginalSprites()
    {
        originalSprites = new Sprite[spriteSwapTargets.Length];

        for (int i = 0; i < spriteSwapTargets.Length; i++)
        {
            originalSprites[i] = spriteSwapTargets[i].image != null
                ? spriteSwapTargets[i].image.sprite
                : null;
        }
    }

    private Color GetOpaqueDisabledColor(Color originalColor)
    {
        return new Color(
            disabledColor.r,
            disabledColor.g,
            disabledColor.b,
            originalColor.a);
    }
}
