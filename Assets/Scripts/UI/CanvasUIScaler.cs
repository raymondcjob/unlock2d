using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Unlock2D.UI
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    public class CanvasUIScaler : MonoBehaviour
    {
        [System.Serializable]
        private struct RectTransformValues
        {
            public Vector2 anchoredPosition;
            public Vector2 sizeDelta;
            public Vector2 offsetMin;
            public Vector2 offsetMax;
        }

        private struct TextReferenceValues
        {
            public TMP_Text text;
            public float fontSize;
        }

        private struct LayoutGroupReferenceValues
        {
            public HorizontalOrVerticalLayoutGroup layoutGroup;
            public float spacing;
        }

        [Header("Base")]
        [SerializeField] private Vector2 baseResolution = new Vector2(2400f, 1080f);

        [Header("Scale Fields")]
        [SerializeField] private bool scalePosX;
        [SerializeField] private bool scalePosY;
        [SerializeField] private bool scaleWidth;
        [SerializeField] private bool scaleHeight;
        [SerializeField] private bool scaleLeftAndRight;
        [SerializeField] private bool scaleTopAndBottom;
        [SerializeField] private bool scaleLocalScaleX;
        [SerializeField] private bool scaleLocalScaleY;

        [Header("Options")]
        [SerializeField] private bool useUniformScale;
        [SerializeField] private bool scaleTextSize;
        [SerializeField] private bool scaleLayoutSpacing;
        [SerializeField] private bool logScaleFactors;

        private RectTransform rectTransform;
        private RectTransformValues referenceRectTransformValues;
        private TextReferenceValues[] referenceTextValues;
        private LayoutGroupReferenceValues[] referenceLayoutGroupValues;
        private int lastScreenWidth = -1;
        private int lastScreenHeight = -1;
        private bool hasReferenceRectTransformValues;
        private bool hasReferenceTextValues;
        private bool hasReferenceLayoutGroupValues;

        private void Reset()
        {
            rectTransform = GetComponent<RectTransform>();
            CaptureReferenceValuesFromCurrentRect();
            LogInvalidScaleFieldSelections();
        }

        private void Awake()
        {
            EnsureRectTransform();

            if (!hasReferenceRectTransformValues)
            {
                CaptureReferenceValuesFromCurrentRect();
            }

            if (!hasReferenceTextValues)
            {
                CaptureReferenceTextValues();
            }

            if (!hasReferenceLayoutGroupValues)
            {
                CaptureReferenceLayoutGroupValues();
            }

            LogInvalidScaleFieldSelections();
        }

        private void OnEnable()
        {
            EnsureRectTransform();

            if (!hasReferenceRectTransformValues)
            {
                CaptureReferenceValuesFromCurrentRect();
            }

            if (!hasReferenceTextValues)
            {
                CaptureReferenceTextValues();
            }

            if (!hasReferenceLayoutGroupValues)
            {
                CaptureReferenceLayoutGroupValues();
            }

            LogInvalidScaleFieldSelections();
            ApplyScale(force: true);
        }

        private void Update()
        {
            ApplyScale(force: false);
        }

        private void OnValidate()
        {
            baseResolution.x = Mathf.Max(1f, baseResolution.x);
            baseResolution.y = Mathf.Max(1f, baseResolution.y);

            EnsureRectTransform();
            LogInvalidScaleFieldSelections();

            lastScreenWidth = -1;
            lastScreenHeight = -1;

            if (!Application.isPlaying)
            {
                hasReferenceRectTransformValues = false;
                hasReferenceTextValues = false;
                hasReferenceLayoutGroupValues = false;
            }
        }

        private void ApplyScale(bool force)
        {
            if (!EnsureRectTransform())
            {
                return;
            }

            if (!hasReferenceRectTransformValues)
            {
                CaptureReferenceValuesFromCurrentRect();
            }

            int screenWidth = Mathf.Max(1, Screen.width);
            int screenHeight = Mathf.Max(1, Screen.height);

            if (!force && screenWidth == lastScreenWidth && screenHeight == lastScreenHeight)
            {
                return;
            }

            float scaleX = screenWidth / baseResolution.x;
            float scaleY = screenHeight / baseResolution.y;
            float uniformScale = Mathf.Min(scaleX, scaleY);
            float appliedScaleX = useUniformScale ? uniformScale : scaleX;
            float appliedScaleY = useUniformScale ? uniformScale : scaleY;
            Vector2 anchoredPosition = rectTransform.anchoredPosition;
            Vector2 sizeDelta = rectTransform.sizeDelta;
            Vector2 offsetMin = rectTransform.offsetMin;
            Vector2 offsetMax = rectTransform.offsetMax;

            if (scalePosX)
            {
                anchoredPosition.x = referenceRectTransformValues.anchoredPosition.x * appliedScaleX;
                rectTransform.anchoredPosition = anchoredPosition;
                LogScaleApplied("Pos X", appliedScaleX);
            }

            if (scalePosY)
            {
                anchoredPosition.y = referenceRectTransformValues.anchoredPosition.y * appliedScaleY;
                rectTransform.anchoredPosition = anchoredPosition;
                LogScaleApplied("Pos Y", appliedScaleY);
            }

            if (scaleWidth)
            {
                sizeDelta.x = referenceRectTransformValues.sizeDelta.x * appliedScaleX;
                rectTransform.sizeDelta = sizeDelta;
                LogScaleApplied("Width", appliedScaleX);
            }

            if (scaleHeight)
            {
                sizeDelta.y = referenceRectTransformValues.sizeDelta.y * appliedScaleY;
                rectTransform.sizeDelta = sizeDelta;
                LogScaleApplied("Height", appliedScaleY);
            }

            if (scaleLeftAndRight)
            {
                offsetMin.x = referenceRectTransformValues.offsetMin.x * appliedScaleX;
                offsetMax.x = referenceRectTransformValues.offsetMax.x * appliedScaleX;
                rectTransform.offsetMin = new Vector2(offsetMin.x, rectTransform.offsetMin.y);
                rectTransform.offsetMax = new Vector2(offsetMax.x, rectTransform.offsetMax.y);
                LogScaleApplied("Left & Right", appliedScaleX);
            }

            if (scaleTopAndBottom)
            {
                offsetMin.y = referenceRectTransformValues.offsetMin.y * appliedScaleY;
                offsetMax.y = referenceRectTransformValues.offsetMax.y * appliedScaleY;
                rectTransform.offsetMin = new Vector2(rectTransform.offsetMin.x, offsetMin.y);
                rectTransform.offsetMax = new Vector2(rectTransform.offsetMax.x, offsetMax.y);
                LogScaleApplied("Top & Bottom", appliedScaleY);
            }

            rectTransform.localScale = new Vector3(
                scaleLocalScaleX ? appliedScaleX : 1f,
                scaleLocalScaleY ? appliedScaleY : 1f,
                1f);

            if (scaleLocalScaleX)
            {
                LogScaleApplied("Scale X", appliedScaleX);
            }

            if (scaleLocalScaleY)
            {
                LogScaleApplied("Scale Y", appliedScaleY);
            }

            if (scaleTextSize)
            {
                ApplyTextScale(uniformScale);
            }

            if (scaleLayoutSpacing)
            {
                ApplyLayoutSpacingScale(appliedScaleX, appliedScaleY);
            }

            lastScreenWidth = screenWidth;
            lastScreenHeight = screenHeight;
        }

        private bool EnsureRectTransform()
        {
            if (rectTransform == null)
            {
                rectTransform = GetComponent<RectTransform>();
            }

            return rectTransform != null;
        }

        private void CaptureReferenceValuesFromCurrentRect()
        {
            if (!EnsureRectTransform())
            {
                return;
            }

            referenceRectTransformValues = new RectTransformValues
            {
                anchoredPosition = rectTransform.anchoredPosition,
                sizeDelta = rectTransform.sizeDelta,
                offsetMin = rectTransform.offsetMin,
                offsetMax = rectTransform.offsetMax
            };
            hasReferenceRectTransformValues = true;
        }

        private void CaptureReferenceTextValues()
        {
            TMP_Text[] texts = GetComponentsInChildren<TMP_Text>(true);
            referenceTextValues = new TextReferenceValues[texts.Length];

            for (int i = 0; i < texts.Length; i++)
            {
                referenceTextValues[i] = new TextReferenceValues
                {
                    text = texts[i],
                    fontSize = texts[i].fontSize
                };
            }

            hasReferenceTextValues = true;
        }

        private void CaptureReferenceLayoutGroupValues()
        {
            HorizontalOrVerticalLayoutGroup[] layoutGroups = GetComponents<HorizontalOrVerticalLayoutGroup>();
            referenceLayoutGroupValues = new LayoutGroupReferenceValues[layoutGroups.Length];

            for (int i = 0; i < layoutGroups.Length; i++)
            {
                referenceLayoutGroupValues[i] = new LayoutGroupReferenceValues
                {
                    layoutGroup = layoutGroups[i],
                    spacing = layoutGroups[i].spacing
                };
            }

            hasReferenceLayoutGroupValues = true;
        }

        private void ApplyLayoutSpacingScale(float horizontalScaleFactor, float verticalScaleFactor)
        {
            if (!hasReferenceLayoutGroupValues)
            {
                CaptureReferenceLayoutGroupValues();
            }

            for (int i = 0; i < referenceLayoutGroupValues.Length; i++)
            {
                if (referenceLayoutGroupValues[i].layoutGroup == null)
                {
                    continue;
                }

                float scaleFactor = referenceLayoutGroupValues[i].layoutGroup is HorizontalLayoutGroup
                    ? horizontalScaleFactor
                    : verticalScaleFactor;

                referenceLayoutGroupValues[i].layoutGroup.spacing = referenceLayoutGroupValues[i].spacing * scaleFactor;
                LogScaleApplied("Layout Spacing", scaleFactor);
            }
        }

        private void ApplyTextScale(float scaleFactor)
        {
            if (!hasReferenceTextValues)
            {
                CaptureReferenceTextValues();
            }

            for (int i = 0; i < referenceTextValues.Length; i++)
            {
                if (referenceTextValues[i].text == null)
                {
                    continue;
                }

                referenceTextValues[i].text.fontSize = referenceTextValues[i].fontSize * scaleFactor;
            }

            LogScaleApplied("Text Size", scaleFactor);
        }

        private void LogInvalidScaleFieldSelections()
        {
            if (!EnsureRectTransform())
            {
                return;
            }

            if (scalePosX && !AnchorPresetUsesPosX())
            {
                LogInvalidScaleField("Pos X");
            }

            if (scalePosY && !AnchorPresetUsesPosY())
            {
                LogInvalidScaleField("Pos Y");
            }

            if (scaleWidth && !AnchorPresetUsesWidth())
            {
                LogInvalidScaleField("Width");
            }

            if (scaleHeight && !AnchorPresetUsesHeight())
            {
                LogInvalidScaleField("Height");
            }

            if (scaleLeftAndRight && !AnchorPresetUsesLeftAndRight())
            {
                LogInvalidScaleField("Left & Right");
            }

            if (scaleTopAndBottom && !AnchorPresetUsesTopAndBottom())
            {
                LogInvalidScaleField("Top & Bottom");
            }
        }

        private bool AnchorPresetUsesPosX()
        {
            return !StretchesHorizontally();
        }

        private bool AnchorPresetUsesWidth()
        {
            return !StretchesHorizontally();
        }

        private bool AnchorPresetUsesLeftAndRight()
        {
            return StretchesHorizontally();
        }

        private bool AnchorPresetUsesPosY()
        {
            return !StretchesVertically();
        }

        private bool AnchorPresetUsesHeight()
        {
            return !StretchesVertically();
        }

        private bool AnchorPresetUsesTopAndBottom()
        {
            return StretchesVertically();
        }

        private bool StretchesHorizontally()
        {
            return EnsureRectTransform() && !Mathf.Approximately(rectTransform.anchorMin.x, rectTransform.anchorMax.x);
        }

        private bool StretchesVertically()
        {
            return EnsureRectTransform() && !Mathf.Approximately(rectTransform.anchorMin.y, rectTransform.anchorMax.y);
        }

        private void LogInvalidScaleField(string fieldName)
        {
            Debug.LogError(
                $"{nameof(CanvasUIScaler)} on '{name}' has Scale Fields > {fieldName} checked, but the current anchor preset does not use {fieldName}. Match the checked scale fields to the RectTransform fields shown in the Inspector.",
                this);
        }

        private void LogScaleApplied(string fieldName, float scaleFactor)
        {
            if (!logScaleFactors)
            {
                return;
            }

            Debug.Log($"{fieldName} is scaled by a factor of {scaleFactor:0.###}", this);
        }
    }
}
