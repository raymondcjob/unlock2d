using System.Collections;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class UIButtonHintSweep : MonoBehaviour
{
    [SerializeField] private Color sweepColor = new Color(0.82f, 0.82f, 0.82f, 0.18f);
    [SerializeField] private float sweepDuration = 1f;
    [SerializeField] private float pauseDuration = 1f;
    [SerializeField] private float minSweepWidth = 52f;
    [SerializeField] private float sweepWidthRatio = 0.3f;

    private RectTransform rectTransform;
    private RectTransform maskRectTransform;
    private RectTransform sweepRectTransform;
    private Image maskImage;
    private Image sweepImage;
    private RectMask2D rectMask;
    private Coroutine sweepCoroutine;

    private void Awake()
    {
        rectTransform = transform as RectTransform;
        EnsureOverlay();
        StopSweep();
    }

    public void PlaySweep(bool topLeftToBottomRight)
    {
        EnsureOverlay();

        if (sweepRectTransform == null || sweepImage == null)
        {
            return;
        }

        if (sweepCoroutine != null)
        {
            StopCoroutine(sweepCoroutine);
        }

        sweepCoroutine = StartCoroutine(SweepLoop(topLeftToBottomRight));
    }

    public void StopSweep()
    {
        if (sweepCoroutine != null)
        {
            StopCoroutine(sweepCoroutine);
            sweepCoroutine = null;
        }

        if (maskRectTransform != null)
        {
            maskRectTransform.gameObject.SetActive(false);
        }
    }

    private IEnumerator SweepLoop(bool topLeftToBottomRight)
    {
        EnsureOverlay();

        if (rectTransform == null || maskRectTransform == null || sweepRectTransform == null || sweepImage == null)
        {
            yield break;
        }

        while (true)
        {
            maskRectTransform.gameObject.SetActive(true);
            ConfigureSweepVisual(topLeftToBottomRight);

            float elapsed = 0f;

            while (elapsed < sweepDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / sweepDuration);
                UpdateSweepPosition(topLeftToBottomRight, t);
                yield return null;
            }

            maskRectTransform.gameObject.SetActive(false);
            yield return new WaitForSecondsRealtime(pauseDuration);
        }
    }

    private void EnsureOverlay()
    {
        if (rectTransform == null)
        {
            rectTransform = transform as RectTransform;
        }

        if (rectTransform == null)
        {
            return;
        }

        if (maskRectTransform == null)
        {
            Transform existingMask = transform.Find("HintSweepMask");

            if (existingMask != null)
            {
                maskRectTransform = existingMask as RectTransform;
                rectMask = existingMask.GetComponent<RectMask2D>();
                maskImage = existingMask.GetComponent<Image>();
            }
            else
            {
                GameObject maskObject = new GameObject("HintSweepMask", typeof(RectTransform), typeof(Image), typeof(RectMask2D));
                maskRectTransform = maskObject.GetComponent<RectTransform>();
                maskRectTransform.SetParent(transform, false);
                maskRectTransform.anchorMin = Vector2.zero;
                maskRectTransform.anchorMax = Vector2.one;
                maskRectTransform.offsetMin = Vector2.zero;
                maskRectTransform.offsetMax = Vector2.zero;
                maskRectTransform.SetAsLastSibling();

                maskImage = maskObject.GetComponent<Image>();
                maskImage.color = new Color(1f, 1f, 1f, 0.001f);
                maskImage.raycastTarget = false;

                rectMask = maskObject.GetComponent<RectMask2D>();
            }
        }

        if (sweepRectTransform == null && maskRectTransform != null)
        {
            Transform existingSweep = maskRectTransform.Find("HintSweepBand");

            if (existingSweep != null)
            {
                sweepRectTransform = existingSweep as RectTransform;
                sweepImage = existingSweep.GetComponent<Image>();
            }
            else
            {
                GameObject sweepObject = new GameObject("HintSweepBand", typeof(RectTransform), typeof(Image));
                sweepRectTransform = sweepObject.GetComponent<RectTransform>();
                sweepRectTransform.SetParent(maskRectTransform, false);
                sweepRectTransform.anchorMin = new Vector2(0.5f, 0.5f);
                sweepRectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                sweepRectTransform.pivot = new Vector2(0.5f, 0.5f);

                sweepImage = sweepObject.GetComponent<Image>();
                sweepImage.color = sweepColor;
                sweepImage.raycastTarget = false;
            }
        }

        if (sweepImage != null)
        {
            sweepImage.color = sweepColor;
        }
    }

    private void ConfigureSweepVisual(bool topLeftToBottomRight)
    {
        Vector2 size = rectTransform.rect.size;
        float sweepWidth = Mathf.Max(minSweepWidth, Mathf.Min(size.x, size.y) * sweepWidthRatio);
        float diagonal = Mathf.Sqrt(size.x * size.x + size.y * size.y) + sweepWidth * 2f;
        sweepRectTransform.sizeDelta = new Vector2(sweepWidth, diagonal);
        sweepRectTransform.localRotation = Quaternion.Euler(0f, 0f, topLeftToBottomRight ? -45f : 45f);
        UpdateSweepPosition(topLeftToBottomRight, 0f);
    }

    private void UpdateSweepPosition(bool topLeftToBottomRight, float t)
    {
        Vector2 size = rectTransform.rect.size;
        float sweepWidth = Mathf.Max(minSweepWidth, Mathf.Min(size.x, size.y) * sweepWidthRatio);
        Vector2 direction = topLeftToBottomRight
            ? new Vector2(1f, -1f).normalized
            : new Vector2(-1f, -1f).normalized;
        float travel = GetTravelDistance(size, direction) + sweepWidth;
        Vector2 start = -direction * travel;
        Vector2 end = direction * travel;

        sweepRectTransform.anchoredPosition = Vector2.Lerp(start, end, t);
    }

    private static float GetTravelDistance(Vector2 size, Vector2 direction)
    {
        Vector2 halfSize = size * 0.5f;
        Vector2[] corners =
        {
            new Vector2(-halfSize.x, -halfSize.y),
            new Vector2(-halfSize.x, halfSize.y),
            new Vector2(halfSize.x, -halfSize.y),
            new Vector2(halfSize.x, halfSize.y)
        };

        float maxProjection = 0f;

        for (int i = 0; i < corners.Length; i++)
        {
            float projection = Mathf.Abs(Vector2.Dot(corners[i], direction));
            if (projection > maxProjection)
            {
                maxProjection = projection;
            }
        }

        return maxProjection;
    }
}
