using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Serialization;

namespace Unlock2D.UI
{
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    public class CanvasUIScaler : MonoBehaviour
    {
        private enum ScaleField
        {
            PosX,
            PosY,
            Width,
            Height,
            LeftAndRight,
            TopAndBottom,
            LocalScaleX,
            LocalScaleY,
            LayoutSpacing
        }

        [System.Serializable]
        private class FixedScaleOverride
        {
            public ScaleField scaleField;
            [FormerlySerializedAs("screenWidthOrHeight")]
            public float baseResolution;
            public float fieldValue;
            [FormerlySerializedAs("secondaryFieldValue")]
            public float optionalValue;
        }

        private struct LayoutGroupReferenceValues
        {
            public HorizontalOrVerticalLayoutGroup layoutGroup;
            public float spacing;
        }

        [Header("Scale Fields")]
        [SerializeField] private bool scalePosX;
        [SerializeField] private bool scalePosY;
        [SerializeField] private bool scaleWidth;
        [SerializeField] private bool scaleHeight;
        [SerializeField] private bool scaleLeftAndRight;
        [SerializeField] private bool scaleTopAndBottom;
        [SerializeField] private bool scaleLocalScaleX;
        [SerializeField] private bool scaleLocalScaleY;

        [Header("Fixed Scaling Overrides")]
        [SerializeField] private FixedScaleOverride[] fixedScaleOverrides;

        [Header("Options")]
        [SerializeField] private bool useUniformScale;
        [SerializeField] private bool scaleLayoutSpacing;
        [SerializeField] private bool logScaleFactors;

        private RectTransform rectTransform;
        private LayoutGroupReferenceValues[] referenceLayoutGroupValues;
        private int lastScreenWidth = -1;
        private int lastScreenHeight = -1;
        private bool hasReferenceLayoutGroupValues;
        private string lastMissingBaseValuesLogKey;

        private void Reset()
        {
            rectTransform = GetComponent<RectTransform>();
            LogInvalidScaleFieldSelections();
        }

        private void Awake()
        {
            EnsureRectTransform();

            if (!hasReferenceLayoutGroupValues)
            {
                CaptureReferenceLayoutGroupValues();
            }

            LogInvalidScaleFieldSelections();
        }

        private void OnEnable()
        {
            EnsureRectTransform();

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
            EnsureRectTransform();
            LogInvalidScaleFieldSelections();

            lastScreenWidth = -1;
            lastScreenHeight = -1;

            if (!Application.isPlaying)
            {
                hasReferenceLayoutGroupValues = false;
            }
        }

        private void ApplyScale(bool force)
        {
            if (!EnsureRectTransform())
            {
                return;
            }

            int screenWidth = Mathf.Max(1, Screen.width);
            int screenHeight = Mathf.Max(1, Screen.height);

            if (!force && screenWidth == lastScreenWidth && screenHeight == lastScreenHeight)
            {
                return;
            }

            if (!TryGetScaleFactors(screenWidth, screenHeight, out float scaleX, out float scaleY, out float uniformScale))
            {
                lastScreenWidth = screenWidth;
                lastScreenHeight = screenHeight;
                return;
            }

            float appliedScaleX = useUniformScale ? uniformScale : scaleX;
            float appliedScaleY = useUniformScale ? uniformScale : scaleY;
            Vector2 anchoredPosition = rectTransform.anchoredPosition;
            Vector2 sizeDelta = rectTransform.sizeDelta;
            Vector2 offsetMin = rectTransform.offsetMin;
            Vector2 offsetMax = rectTransform.offsetMax;

            if (scalePosX)
            {
                if (TryGetScaledFieldValue(
                    ScaleField.PosX,
                    appliedScaleX,
                    screenWidth,
                    out float scaledPosX))
                {
                    anchoredPosition.x = scaledPosX;
                    rectTransform.anchoredPosition = anchoredPosition;
                    LogScaleApplied("Pos X", appliedScaleX);
                }
            }

            if (scalePosY)
            {
                if (TryGetScaledFieldValue(
                    ScaleField.PosY,
                    appliedScaleY,
                    screenHeight,
                    out float scaledPosY))
                {
                    anchoredPosition.y = scaledPosY;
                    rectTransform.anchoredPosition = anchoredPosition;
                    LogScaleApplied("Pos Y", appliedScaleY);
                }
            }

            if (scaleWidth)
            {
                if (TryGetScaledFieldValue(
                    ScaleField.Width,
                    appliedScaleX,
                    screenWidth,
                    out float scaledWidth))
                {
                    sizeDelta.x = scaledWidth;
                    rectTransform.sizeDelta = sizeDelta;
                    LogScaleApplied("Width", appliedScaleX);
                }
            }

            if (scaleHeight)
            {
                if (TryGetScaledFieldValue(
                    ScaleField.Height,
                    appliedScaleY,
                    screenHeight,
                    out float scaledHeight))
                {
                    sizeDelta.y = scaledHeight;
                    rectTransform.sizeDelta = sizeDelta;
                    LogScaleApplied("Height", appliedScaleY);
                }
            }

            if (scaleLeftAndRight)
            {
                if (TryGetScaledStretchFieldValue(
                    ScaleField.LeftAndRight,
                    appliedScaleX,
                    screenWidth,
                    out float left,
                    out float right))
                {
                    offsetMin.x = left;
                    offsetMax.x = -right;
                    rectTransform.offsetMin = new Vector2(offsetMin.x, rectTransform.offsetMin.y);
                    rectTransform.offsetMax = new Vector2(offsetMax.x, rectTransform.offsetMax.y);
                    LogScaleApplied("Left & Right", appliedScaleX);
                }
            }

            if (scaleTopAndBottom)
            {
                if (TryGetScaledStretchFieldValue(
                    ScaleField.TopAndBottom,
                    appliedScaleY,
                    screenHeight,
                    out float top,
                    out float bottom))
                {
                    offsetMin.y = bottom;
                    offsetMax.y = -top;
                    rectTransform.offsetMin = new Vector2(rectTransform.offsetMin.x, offsetMin.y);
                    rectTransform.offsetMax = new Vector2(rectTransform.offsetMax.x, offsetMax.y);
                    LogScaleApplied("Top & Bottom", appliedScaleY);
                }
            }

            Vector3 localScale = rectTransform.localScale;

            if (scaleLocalScaleX)
            {
                if (TryGetScaledFieldValue(ScaleField.LocalScaleX, appliedScaleX, screenWidth, out float scaledLocalScaleX))
                {
                    localScale.x = scaledLocalScaleX;
                    LogScaleApplied("Scale X", appliedScaleX);
                }
            }

            if (scaleLocalScaleY)
            {
                if (TryGetScaledFieldValue(ScaleField.LocalScaleY, appliedScaleY, screenHeight, out float scaledLocalScaleY))
                {
                    localScale.y = scaledLocalScaleY;
                    LogScaleApplied("Scale Y", appliedScaleY);
                }
            }

            localScale.z = 1f;
            rectTransform.localScale = localScale;

            if (scaleLayoutSpacing)
            {
                ApplyLayoutSpacingScale(screenWidth, screenHeight, appliedScaleX, appliedScaleY);
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

        private void ApplyLayoutSpacingScale(
            int screenWidth,
            int screenHeight,
            float horizontalScaleFactor,
            float verticalScaleFactor)
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

                int currentScreenSize = referenceLayoutGroupValues[i].layoutGroup is HorizontalLayoutGroup
                    ? screenWidth
                    : screenHeight;

                if (!TryGetScaledFieldValue(
                    ScaleField.LayoutSpacing,
                    scaleFactor,
                    currentScreenSize,
                    out float scaledSpacing))
                {
                    continue;
                }

                referenceLayoutGroupValues[i].layoutGroup.spacing = scaledSpacing;
                LogScaleApplied("Layout Spacing", scaleFactor);
            }
        }

        private bool TryGetScaleFactors(
            int screenWidth,
            int screenHeight,
            out float scaleX,
            out float scaleY,
            out float uniformScale)
        {
            scaleX = 1f;
            scaleY = 1f;
            uniformScale = 1f;

            if (fixedScaleOverrides == null || fixedScaleOverrides.Length == 0)
            {
                LogMissingBaseValuesForCheckedFields();
                return false;
            }

            bool hasXBase = TryGetClosestEntryForAxis(true, screenWidth, out FixedScaleOverride closestXEntry);
            bool hasYBase = TryGetClosestEntryForAxis(false, screenHeight, out FixedScaleOverride closestYEntry);

            if (useUniformScale && (!hasXBase || !hasYBase))
            {
                LogMissingUniformBaseValues(hasXBase, hasYBase);
                return false;
            }

            LogMissingBaseValuesForMissingCheckedFields();

            if (hasXBase)
            {
                scaleX = screenWidth / Mathf.Abs(GetBaseResolutionForAxis(closestXEntry, true));
            }

            if (hasYBase)
            {
                scaleY = screenHeight / Mathf.Abs(GetBaseResolutionForAxis(closestYEntry, false));
            }

            uniformScale = Mathf.Min(scaleX, scaleY);
            return true;
        }

        private bool TryGetScaledStretchFieldValue(
            ScaleField scaleField,
            float scaleFactor,
            int currentScreenSize,
            out float firstValue,
            out float secondValue)
        {
            firstValue = 0f;
            secondValue = 0f;

            if (!TryGetClosestEntry(scaleField, currentScreenSize, out FixedScaleOverride closestOverride))
            {
                return false;
            }

            float firstBaseValue = closestOverride.fieldValue;
            float secondBaseValue = closestOverride.optionalValue;

            if (useUniformScale)
            {
                firstValue = firstBaseValue * scaleFactor;
                secondValue = secondBaseValue * scaleFactor;
            }
            else
            {
                float fieldScaleFactor = currentScreenSize / Mathf.Abs(closestOverride.baseResolution);
                firstValue = firstBaseValue * fieldScaleFactor;
                secondValue = secondBaseValue * fieldScaleFactor;
            }

            return true;
        }

        private bool TryGetScaledFieldValue(
            ScaleField scaleField,
            float scaleFactor,
            int currentScreenSize,
            out float scaledValue)
        {
            scaledValue = 0f;

            if (!TryGetClosestEntry(scaleField, currentScreenSize, out FixedScaleOverride closestOverride))
            {
                return false;
            }

            scaledValue = useUniformScale
                ? closestOverride.fieldValue * scaleFactor
                : closestOverride.fieldValue * currentScreenSize / Mathf.Abs(closestOverride.baseResolution);
            return true;
        }

        private bool TryGetClosestEntry(
            ScaleField scaleField,
            int currentScreenSize,
            out FixedScaleOverride closestOverride)
        {
            closestOverride = default(FixedScaleOverride);

            if (fixedScaleOverrides == null || fixedScaleOverrides.Length == 0)
            {
                return false;
            }

            int closestIndex = -1;
            float closestDistance = float.MaxValue;

            for (int i = 0; i < fixedScaleOverrides.Length; i++)
            {
                FixedScaleOverride fixedScaleOverride = fixedScaleOverrides[i];

                if (!IsValidEntryForScaleField(fixedScaleOverride, scaleField))
                {
                    continue;
                }

                float configuredScreenSize = Mathf.Abs(fixedScaleOverride.baseResolution);
                float distance = Mathf.Abs(currentScreenSize - configuredScreenSize);

                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestIndex = i;
                }
            }

            if (closestIndex < 0)
            {
                return false;
            }

            closestOverride = fixedScaleOverrides[closestIndex];
            return true;
        }

        private bool TryGetClosestEntryForAxis(
            bool horizontalAxis,
            int currentScreenSize,
            out FixedScaleOverride closestOverride)
        {
            closestOverride = default(FixedScaleOverride);

            if (fixedScaleOverrides == null || fixedScaleOverrides.Length == 0)
            {
                return false;
            }

            int closestIndex = -1;
            float closestDistance = float.MaxValue;

            for (int i = 0; i < fixedScaleOverrides.Length; i++)
            {
                FixedScaleOverride fixedScaleOverride = fixedScaleOverrides[i];

                if (!IsValidScaleEntry(fixedScaleOverride) ||
                    !ScaleFieldMatchesAxis(fixedScaleOverride.scaleField, horizontalAxis))
                {
                    continue;
                }

                float configuredScreenSize = Mathf.Abs(GetBaseResolutionForAxis(fixedScaleOverride, horizontalAxis));
                float distance = Mathf.Abs(currentScreenSize - configuredScreenSize);

                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestIndex = i;
                }
            }

            if (closestIndex < 0)
            {
                return false;
            }

            closestOverride = fixedScaleOverrides[closestIndex];
            return true;
        }

        private bool IsValidEntryForScaleField(FixedScaleOverride fixedScaleOverride, ScaleField scaleField)
        {
            return IsValidScaleEntry(fixedScaleOverride) && fixedScaleOverride.scaleField == scaleField;
        }

        private bool IsValidScaleEntry(FixedScaleOverride fixedScaleOverride)
        {
            if (fixedScaleOverride == null)
            {
                return false;
            }

            return !Mathf.Approximately(fixedScaleOverride.baseResolution, 0f) &&
                   !Mathf.Approximately(fixedScaleOverride.fieldValue, 0f);
        }

        private float GetBaseResolutionForAxis(FixedScaleOverride fixedScaleOverride, bool horizontalAxis)
        {
            return fixedScaleOverride != null ? fixedScaleOverride.baseResolution : 0f;
        }

        private bool ScaleFieldMatchesAxis(ScaleField scaleField, bool horizontalAxis)
        {
            if (scaleField == ScaleField.LayoutSpacing)
            {
                return horizontalAxis;
            }

            return IsHorizontalScaleField(scaleField) == horizontalAxis;
        }

        private bool IsHorizontalScaleField(ScaleField scaleField)
        {
            switch (scaleField)
            {
                case ScaleField.PosX:
                case ScaleField.Width:
                case ScaleField.LeftAndRight:
                case ScaleField.LocalScaleX:
                case ScaleField.LayoutSpacing:
                    return true;
                case ScaleField.PosY:
                case ScaleField.Height:
                case ScaleField.TopAndBottom:
                case ScaleField.LocalScaleY:
                    return false;
                default:
                    return true;
            }
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

            LogInvalidFixedScaleOverrides();
        }

        private void LogInvalidFixedScaleOverrides()
        {
            if (fixedScaleOverrides == null || fixedScaleOverrides.Length == 0)
            {
                return;
            }

            for (int i = 0; i < fixedScaleOverrides.Length; i++)
            {
                FixedScaleOverride fixedScaleOverride = fixedScaleOverrides[i];

                if (fixedScaleOverride == null)
                {
                    continue;
                }

                ScaleField scaleField = fixedScaleOverride.scaleField;
                if (IsScaleFieldEnabled(scaleField))
                {
                    continue;
                }

                Debug.LogWarning(
                    $"{nameof(CanvasUIScaler)} on '{name}' has a Fixed Scaling Override for {GetScaleFieldDisplayName(scaleField)}, but that Scale Field is not checked. The override will be ignored until the Scale Field is enabled.",
                    this);
            }
        }

        private void LogMissingBaseValuesForCheckedFields()
        {
            string missingFields = GetCheckedScaleFieldDisplayNames();

            if (string.IsNullOrEmpty(missingFields))
            {
                return;
            }

            LogMissingBaseValuesOnce(missingFields);
        }

        private void LogMissingBaseValuesForMissingCheckedFields()
        {
            string missingFields = string.Empty;

            AppendMissingScaleFieldNameIfChecked(ref missingFields, ScaleField.PosX, scalePosX);
            AppendMissingScaleFieldNameIfChecked(ref missingFields, ScaleField.PosY, scalePosY);
            AppendMissingScaleFieldNameIfChecked(ref missingFields, ScaleField.Width, scaleWidth);
            AppendMissingScaleFieldNameIfChecked(ref missingFields, ScaleField.Height, scaleHeight);
            AppendMissingScaleFieldNameIfChecked(ref missingFields, ScaleField.LeftAndRight, scaleLeftAndRight);
            AppendMissingScaleFieldNameIfChecked(ref missingFields, ScaleField.TopAndBottom, scaleTopAndBottom);
            AppendMissingScaleFieldNameIfChecked(ref missingFields, ScaleField.LocalScaleX, scaleLocalScaleX);
            AppendMissingScaleFieldNameIfChecked(ref missingFields, ScaleField.LocalScaleY, scaleLocalScaleY);
            AppendMissingScaleFieldNameIfChecked(ref missingFields, ScaleField.LayoutSpacing, scaleLayoutSpacing);

            if (!string.IsNullOrEmpty(missingFields))
            {
                LogMissingBaseValuesOnce(missingFields);
            }
        }

        private void LogMissingUniformBaseValues(bool hasXBase, bool hasYBase)
        {
            string missingAxes = string.Empty;

            if (!hasXBase)
            {
                missingAxes = "an X-based scale field";
            }

            if (!hasYBase)
            {
                missingAxes = string.IsNullOrEmpty(missingAxes)
                    ? "a Y-based scale field"
                    : $"{missingAxes}, a Y-based scale field";
            }

            LogMissingBaseValuesOnce(missingAxes);
        }

        private void LogMissingBaseValuesOnce(string missingFields)
        {
            if (string.IsNullOrEmpty(missingFields) || lastMissingBaseValuesLogKey == missingFields)
            {
                return;
            }

            lastMissingBaseValuesLogKey = missingFields;
            Debug.LogWarning(
                $"{nameof(CanvasUIScaler)} on '{name}': No base scaling value exists for {missingFields}.",
                this);
        }

        private string GetCheckedScaleFieldDisplayNames()
        {
            string fieldNames = string.Empty;

            AppendScaleFieldNameIfChecked(ref fieldNames, ScaleField.PosX, scalePosX);
            AppendScaleFieldNameIfChecked(ref fieldNames, ScaleField.PosY, scalePosY);
            AppendScaleFieldNameIfChecked(ref fieldNames, ScaleField.Width, scaleWidth);
            AppendScaleFieldNameIfChecked(ref fieldNames, ScaleField.Height, scaleHeight);
            AppendScaleFieldNameIfChecked(ref fieldNames, ScaleField.LeftAndRight, scaleLeftAndRight);
            AppendScaleFieldNameIfChecked(ref fieldNames, ScaleField.TopAndBottom, scaleTopAndBottom);
            AppendScaleFieldNameIfChecked(ref fieldNames, ScaleField.LocalScaleX, scaleLocalScaleX);
            AppendScaleFieldNameIfChecked(ref fieldNames, ScaleField.LocalScaleY, scaleLocalScaleY);
            AppendScaleFieldNameIfChecked(ref fieldNames, ScaleField.LayoutSpacing, scaleLayoutSpacing);

            return fieldNames;
        }

        private void AppendScaleFieldNameIfChecked(ref string fieldNames, ScaleField scaleField, bool isChecked)
        {
            if (!isChecked)
            {
                return;
            }

            string displayName = GetScaleFieldDisplayName(scaleField);
            fieldNames = string.IsNullOrEmpty(fieldNames)
                ? displayName
                : $"{fieldNames}, {displayName}";
        }

        private void AppendMissingScaleFieldNameIfChecked(ref string fieldNames, ScaleField scaleField, bool isChecked)
        {
            if (!isChecked || HasEntryForScaleField(scaleField))
            {
                return;
            }

            string displayName = GetScaleFieldDisplayName(scaleField);
            fieldNames = string.IsNullOrEmpty(fieldNames)
                ? displayName
                : $"{fieldNames}, {displayName}";
        }

        private bool HasEntryForScaleField(ScaleField scaleField)
        {
            if (fixedScaleOverrides == null)
            {
                return false;
            }

            for (int i = 0; i < fixedScaleOverrides.Length; i++)
            {
                FixedScaleOverride fixedScaleOverride = fixedScaleOverrides[i];

                if (IsValidEntryForScaleField(fixedScaleOverride, scaleField))
                {
                    return true;
                }
            }

            return false;
        }

        private bool IsScaleFieldEnabled(ScaleField scaleField)
        {
            switch (scaleField)
            {
                case ScaleField.PosX:
                    return scalePosX;
                case ScaleField.PosY:
                    return scalePosY;
                case ScaleField.Width:
                    return scaleWidth;
                case ScaleField.Height:
                    return scaleHeight;
                case ScaleField.LeftAndRight:
                    return scaleLeftAndRight;
                case ScaleField.TopAndBottom:
                    return scaleTopAndBottom;
                case ScaleField.LocalScaleX:
                    return scaleLocalScaleX;
                case ScaleField.LocalScaleY:
                    return scaleLocalScaleY;
                case ScaleField.LayoutSpacing:
                    return scaleLayoutSpacing;
                default:
                    return false;
            }
        }

        private string GetScaleFieldDisplayName(ScaleField scaleField)
        {
            switch (scaleField)
            {
                case ScaleField.PosX:
                    return "Pos X";
                case ScaleField.PosY:
                    return "Pos Y";
                case ScaleField.Width:
                    return "Width";
                case ScaleField.Height:
                    return "Height";
                case ScaleField.LeftAndRight:
                    return "Left & Right";
                case ScaleField.TopAndBottom:
                    return "Top & Bottom";
                case ScaleField.LocalScaleX:
                    return "Scale X";
                case ScaleField.LocalScaleY:
                    return "Scale Y";
                case ScaleField.LayoutSpacing:
                    return "Layout Spacing";
                default:
                    return scaleField.ToString();
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
