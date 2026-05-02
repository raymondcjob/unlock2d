using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(Canvas))]
public class CanvasFixedReferenceScaler : MonoBehaviour
{
    [SerializeField] private Vector2 referenceResolution = new Vector2(1920f, 1080f);
    [SerializeField] private bool autoFindTargets = true;
    [SerializeField] private bool scaleTargetsEnabled = true;
    [SerializeField] private string[] autoPositionTargetNames = { "LeftBar", "RightBar" };
    [SerializeField] private string[] autoStretchOnlyTargetNames = { "ItemBar" };
    [SerializeField] private string[] autoWidthAndStretchTargetNames = {};
    [SerializeField] private string[] autoSizeAndYPositionTargetNames = {};
    [SerializeField] private string[] autoChildTextScaleParentNames = {};
    [SerializeField] private string[] autoChildTextScaleExcludePaths = {};
    [SerializeField] private string[] autoUniformScaleTargetNames = { "MenuButton", "Undo", "Shuffle", "Swap" };
    [SerializeField] private string[] autoScreenRatioScaleTargetNames = { "Timer Panel", "TimerPanel" };
    [SerializeField] private string[] autoPositionTargetPaths = {};
    [SerializeField] private string[] autoStretchOnlyTargetPaths = {};
    [SerializeField] private string[] autoXYPositionOnlyTargetPaths = {};
    [SerializeField] private string[] autoYPositionOnlyTargetPaths = {};
    [SerializeField] private string[] autoHorizontalStretchYPositionHeightTargetPaths = {};
    [SerializeField] private string[] autoStretchOffsetsTargetPaths = {};
    [SerializeField] private string[] autoWidthAndStretchTargetPaths = {};
    [SerializeField] private string[] autoSizeAndLayoutSpacingTargetPaths = {};
    [SerializeField] private string[] autoSizeAndYPositionTargetPaths = {};
    [SerializeField] private string[] autoChildTextScaleParentPaths = {};
    [SerializeField] private string[] autoDirectScaleTargetPaths = {};
    [SerializeField] private string[] autoDirectScaleXOnlyTargetPaths = {};
    [SerializeField] private string[] autoUniformScaleTargetPaths = {};
    [SerializeField] private string[] autoScreenRatioScaleTargetPaths = {};
    [SerializeField] private RectTransform[] positionTargets;
    [SerializeField] private RectTransform[] stretchOnlyTargets;
    [SerializeField] private RectTransform[] widthAndStretchTargets;
    [SerializeField] private RectTransform[] sizeAndYPositionTargets;
    [SerializeField] private RectTransform[] directScaleTargets;
    [SerializeField] private RectTransform[] uniformScaleTargets;
    [SerializeField] private RectTransform[] screenRatioScaleTargets;

    private readonly List<RectTransform> resolvedPositionTargets = new List<RectTransform>();
    private readonly List<RectTransform> resolvedStretchOnlyTargets = new List<RectTransform>();
    private readonly List<RectTransform> resolvedXYPositionOnlyTargets = new List<RectTransform>();
    private readonly List<RectTransform> resolvedYPositionOnlyTargets = new List<RectTransform>();
    private readonly List<RectTransform> resolvedHorizontalStretchYPositionHeightTargets = new List<RectTransform>();
    private readonly List<RectTransform> resolvedStretchOffsetsTargets = new List<RectTransform>();
    private readonly List<RectTransform> resolvedWidthAndStretchTargets = new List<RectTransform>();
    private readonly List<RectTransform> resolvedSizeAndLayoutSpacingTargets = new List<RectTransform>();
    private readonly List<RectTransform> resolvedSizeAndYPositionTargets = new List<RectTransform>();
    private readonly List<RectTransform> resolvedDirectScaleTargets = new List<RectTransform>();
    private readonly List<RectTransform> resolvedDirectScaleXOnlyTargets = new List<RectTransform>();
    private readonly List<RectTransform> resolvedUniformScaleTargets = new List<RectTransform>();
    private readonly List<RectTransform> resolvedScreenRatioScaleTargets = new List<RectTransform>();
    private readonly List<PositionSnapshot> positionSnapshots = new List<PositionSnapshot>();
    private readonly List<VerticalStretchSnapshot> stretchOnlySnapshots = new List<VerticalStretchSnapshot>();
    private readonly List<XYPositionOnlySnapshot> xyPositionOnlySnapshots = new List<XYPositionOnlySnapshot>();
    private readonly List<YPositionOnlySnapshot> yPositionOnlySnapshots = new List<YPositionOnlySnapshot>();
    private readonly List<HorizontalStretchYPositionHeightSnapshot> horizontalStretchYPositionHeightSnapshots = new List<HorizontalStretchYPositionHeightSnapshot>();
    private readonly List<StretchOffsetsSnapshot> stretchOffsetsSnapshots = new List<StretchOffsetsSnapshot>();
    private readonly List<WidthAndStretchSnapshot> widthAndStretchSnapshots = new List<WidthAndStretchSnapshot>();
    private readonly List<SizeAndLayoutSpacingSnapshot> sizeAndLayoutSpacingSnapshots = new List<SizeAndLayoutSpacingSnapshot>();
    private readonly List<SizeAndYPositionSnapshot> sizeAndYPositionSnapshots = new List<SizeAndYPositionSnapshot>();
    private readonly List<DirectScaleSnapshot> directScaleSnapshots = new List<DirectScaleSnapshot>();
    private readonly List<DirectScaleXOnlySnapshot> directScaleXOnlySnapshots = new List<DirectScaleXOnlySnapshot>();
    private readonly List<UniformScaleSnapshot> uniformScaleSnapshots = new List<UniformScaleSnapshot>();
    private readonly List<ScreenRatioScaleSnapshot> screenRatioScaleSnapshots = new List<ScreenRatioScaleSnapshot>();

    private int lastScreenWidth = -1;
    private int lastScreenHeight = -1;
    private bool hasCapturedReferenceLayout;

    private void Awake()
    {
        CaptureReferenceLayout();
        ApplyScaleIfNeeded(force: true);
    }

    private void OnEnable()
    {
        CaptureReferenceLayout();
        ApplyScaleIfNeeded(force: true);
    }

    private void Update()
    {
        ApplyScaleIfNeeded(force: false);
    }

    private void OnValidate()
    {
        referenceResolution.x = Mathf.Max(1f, referenceResolution.x);
        referenceResolution.y = Mathf.Max(1f, referenceResolution.y);
        hasCapturedReferenceLayout = false;
    }

    private void CaptureReferenceLayout()
    {
        if (hasCapturedReferenceLayout)
        {
            return;
        }

        resolvedPositionTargets.Clear();
        resolvedStretchOnlyTargets.Clear();
        resolvedXYPositionOnlyTargets.Clear();
        resolvedYPositionOnlyTargets.Clear();
        resolvedHorizontalStretchYPositionHeightTargets.Clear();
        resolvedStretchOffsetsTargets.Clear();
        resolvedWidthAndStretchTargets.Clear();
        resolvedSizeAndLayoutSpacingTargets.Clear();
        resolvedSizeAndYPositionTargets.Clear();
        resolvedDirectScaleTargets.Clear();
        resolvedDirectScaleXOnlyTargets.Clear();
        resolvedUniformScaleTargets.Clear();
        resolvedScreenRatioScaleTargets.Clear();
        positionSnapshots.Clear();
        stretchOnlySnapshots.Clear();
        xyPositionOnlySnapshots.Clear();
        yPositionOnlySnapshots.Clear();
        horizontalStretchYPositionHeightSnapshots.Clear();
        stretchOffsetsSnapshots.Clear();
        widthAndStretchSnapshots.Clear();
        sizeAndLayoutSpacingSnapshots.Clear();
        sizeAndYPositionSnapshots.Clear();
        directScaleSnapshots.Clear();
        directScaleXOnlySnapshots.Clear();
        uniformScaleSnapshots.Clear();
        screenRatioScaleSnapshots.Clear();

        ResolveTargets();

        for (int i = 0; i < resolvedPositionTargets.Count; i++)
        {
            positionSnapshots.Add(new PositionSnapshot(resolvedPositionTargets[i]));
        }

        for (int i = 0; i < resolvedStretchOnlyTargets.Count; i++)
        {
            stretchOnlySnapshots.Add(new VerticalStretchSnapshot(resolvedStretchOnlyTargets[i]));
        }

        for (int i = 0; i < resolvedXYPositionOnlyTargets.Count; i++)
        {
            xyPositionOnlySnapshots.Add(new XYPositionOnlySnapshot(resolvedXYPositionOnlyTargets[i]));
        }

        for (int i = 0; i < resolvedYPositionOnlyTargets.Count; i++)
        {
            yPositionOnlySnapshots.Add(new YPositionOnlySnapshot(resolvedYPositionOnlyTargets[i]));
        }

        for (int i = 0; i < resolvedHorizontalStretchYPositionHeightTargets.Count; i++)
        {
            horizontalStretchYPositionHeightSnapshots.Add(new HorizontalStretchYPositionHeightSnapshot(resolvedHorizontalStretchYPositionHeightTargets[i]));
        }

        for (int i = 0; i < resolvedStretchOffsetsTargets.Count; i++)
        {
            stretchOffsetsSnapshots.Add(new StretchOffsetsSnapshot(resolvedStretchOffsetsTargets[i]));
        }

        for (int i = 0; i < resolvedWidthAndStretchTargets.Count; i++)
        {
            widthAndStretchSnapshots.Add(new WidthAndStretchSnapshot(resolvedWidthAndStretchTargets[i]));
        }

        for (int i = 0; i < resolvedSizeAndLayoutSpacingTargets.Count; i++)
        {
            sizeAndLayoutSpacingSnapshots.Add(new SizeAndLayoutSpacingSnapshot(resolvedSizeAndLayoutSpacingTargets[i]));
        }

        for (int i = 0; i < resolvedSizeAndYPositionTargets.Count; i++)
        {
            sizeAndYPositionSnapshots.Add(new SizeAndYPositionSnapshot(resolvedSizeAndYPositionTargets[i]));
        }

        for (int i = 0; i < resolvedDirectScaleTargets.Count; i++)
        {
            directScaleSnapshots.Add(new DirectScaleSnapshot(resolvedDirectScaleTargets[i]));
        }

        for (int i = 0; i < resolvedDirectScaleXOnlyTargets.Count; i++)
        {
            directScaleXOnlySnapshots.Add(new DirectScaleXOnlySnapshot(resolvedDirectScaleXOnlyTargets[i]));
        }

        for (int i = 0; i < resolvedUniformScaleTargets.Count; i++)
        {
            uniformScaleSnapshots.Add(new UniformScaleSnapshot(resolvedUniformScaleTargets[i]));
        }

        for (int i = 0; i < resolvedScreenRatioScaleTargets.Count; i++)
        {
            screenRatioScaleSnapshots.Add(new ScreenRatioScaleSnapshot(resolvedScreenRatioScaleTargets[i]));
        }

        hasCapturedReferenceLayout = true;
    }

    private void ResolveTargets()
    {
        AddManualTargets(positionTargets, resolvedPositionTargets);
        AddManualTargets(stretchOnlyTargets, resolvedStretchOnlyTargets);
        AddManualTargets(widthAndStretchTargets, resolvedWidthAndStretchTargets);
        AddManualTargets(sizeAndYPositionTargets, resolvedSizeAndYPositionTargets);
        AddManualTargets(directScaleTargets, resolvedDirectScaleTargets);
        AddManualTargets(uniformScaleTargets, resolvedUniformScaleTargets);
        AddManualTargets(screenRatioScaleTargets, resolvedScreenRatioScaleTargets);

        if (!autoFindTargets)
        {
            return;
        }

        AddNamedTargets(autoPositionTargetNames, resolvedPositionTargets);
        AddNamedTargets(autoStretchOnlyTargetNames, resolvedStretchOnlyTargets);
        AddNamedTargets(autoWidthAndStretchTargetNames, resolvedWidthAndStretchTargets);
        AddNamedTargets(autoSizeAndYPositionTargetNames, resolvedSizeAndYPositionTargets);
        AddChildTextTargets(autoChildTextScaleParentNames, resolvedDirectScaleTargets);
        AddNamedTargets(autoUniformScaleTargetNames, resolvedUniformScaleTargets);
        AddNamedTargets(autoScreenRatioScaleTargetNames, resolvedScreenRatioScaleTargets);
        AddPathTargets(autoPositionTargetPaths, resolvedPositionTargets);
        AddPathTargets(autoStretchOnlyTargetPaths, resolvedStretchOnlyTargets);
        AddPathTargets(autoXYPositionOnlyTargetPaths, resolvedXYPositionOnlyTargets);
        AddPathTargets(autoYPositionOnlyTargetPaths, resolvedYPositionOnlyTargets);
        AddPathTargets(autoHorizontalStretchYPositionHeightTargetPaths, resolvedHorizontalStretchYPositionHeightTargets);
        AddPathTargets(autoStretchOffsetsTargetPaths, resolvedStretchOffsetsTargets);
        AddPathTargets(autoWidthAndStretchTargetPaths, resolvedWidthAndStretchTargets);
        AddPathTargets(autoSizeAndLayoutSpacingTargetPaths, resolvedSizeAndLayoutSpacingTargets);
        AddPathTargets(autoSizeAndYPositionTargetPaths, resolvedSizeAndYPositionTargets);
        AddChildTextTargetsByPath(autoChildTextScaleParentPaths, resolvedDirectScaleTargets);
        AddPathTargets(autoDirectScaleTargetPaths, resolvedDirectScaleTargets);
        AddPathTargets(autoDirectScaleXOnlyTargetPaths, resolvedDirectScaleXOnlyTargets);
        AddPathTargets(autoUniformScaleTargetPaths, resolvedUniformScaleTargets);
        AddPathTargets(autoScreenRatioScaleTargetPaths, resolvedScreenRatioScaleTargets);
    }

    private void AddNamedTargets(string[] targetNames, List<RectTransform> destination)
    {
        if (targetNames == null || targetNames.Length == 0)
        {
            return;
        }

        RectTransform[] rectTransforms = GetComponentsInChildren<RectTransform>(true);
        for (int i = 0; i < rectTransforms.Length; i++)
        {
            for (int nameIndex = 0; nameIndex < targetNames.Length; nameIndex++)
            {
                if (rectTransforms[i].name == targetNames[nameIndex])
                {
                    AddUnique(destination, rectTransforms[i]);
                    break;
                }
            }
        }
    }

    private void ApplyScaleIfNeeded(bool force)
    {
        if (referenceResolution.x <= 0f || referenceResolution.y <= 0f)
        {
            return;
        }

        int screenWidth = Mathf.Max(1, Screen.width);
        int screenHeight = Mathf.Max(1, Screen.height);

        if (!force && screenWidth == lastScreenWidth && screenHeight == lastScreenHeight)
        {
            return;
        }

        CaptureReferenceLayout();

        float scaleX = screenWidth / referenceResolution.x;
        float scaleY = screenHeight / referenceResolution.y;
        float uniformSizeScale = Mathf.Min(scaleX, scaleY);

        for (int i = 0; i < positionSnapshots.Count; i++)
        {
            positionSnapshots[i].Apply(scaleX, scaleY);
        }

        for (int i = 0; i < stretchOnlySnapshots.Count; i++)
        {
            stretchOnlySnapshots[i].Apply(scaleY);
        }

        for (int i = 0; i < xyPositionOnlySnapshots.Count; i++)
        {
            xyPositionOnlySnapshots[i].Apply(scaleX, scaleY);
        }

        for (int i = 0; i < yPositionOnlySnapshots.Count; i++)
        {
            yPositionOnlySnapshots[i].Apply(scaleY);
        }

        for (int i = 0; i < horizontalStretchYPositionHeightSnapshots.Count; i++)
        {
            horizontalStretchYPositionHeightSnapshots[i].Apply(scaleX, scaleY);
        }

        for (int i = 0; i < stretchOffsetsSnapshots.Count; i++)
        {
            stretchOffsetsSnapshots[i].Apply(scaleX, scaleY);
        }

        for (int i = 0; i < widthAndStretchSnapshots.Count; i++)
        {
            widthAndStretchSnapshots[i].Apply(scaleX, scaleY);
        }

        for (int i = 0; i < sizeAndLayoutSpacingSnapshots.Count; i++)
        {
            sizeAndLayoutSpacingSnapshots[i].Apply(scaleX, scaleY);
        }

        for (int i = 0; i < sizeAndYPositionSnapshots.Count; i++)
        {
            sizeAndYPositionSnapshots[i].Apply(scaleX, scaleY);
        }

        for (int i = 0; i < directScaleSnapshots.Count; i++)
        {
            directScaleSnapshots[i].Apply(scaleX, scaleY);
        }

        for (int i = 0; i < directScaleXOnlySnapshots.Count; i++)
        {
            directScaleXOnlySnapshots[i].Apply(scaleX);
        }

        if (scaleTargetsEnabled)
        {
            for (int i = 0; i < uniformScaleSnapshots.Count; i++)
            {
                uniformScaleSnapshots[i].Apply(uniformSizeScale);
            }

            for (int i = 0; i < screenRatioScaleSnapshots.Count; i++)
            {
                screenRatioScaleSnapshots[i].Apply(scaleX, scaleY);
            }
        }

        lastScreenWidth = screenWidth;
        lastScreenHeight = screenHeight;
    }

    private void AddChildTextTargets(string[] parentNames, List<RectTransform> destination)
    {
        if (parentNames == null || parentNames.Length == 0)
        {
            return;
        }

        RectTransform[] rectTransforms = GetComponentsInChildren<RectTransform>(true);
        for (int i = 0; i < rectTransforms.Length; i++)
        {
            if (!NameMatches(rectTransforms[i].name, parentNames))
            {
                continue;
            }

            TMPro.TMP_Text[] texts = rectTransforms[i].GetComponentsInChildren<TMPro.TMP_Text>(true);
            for (int textIndex = 0; textIndex < texts.Length; textIndex++)
            {
                AddUnique(destination, texts[textIndex].transform as RectTransform);
            }
        }
    }

    private void AddPathTargets(string[] targetPaths, List<RectTransform> destination)
    {
        if (targetPaths == null || targetPaths.Length == 0)
        {
            return;
        }

        for (int i = 0; i < targetPaths.Length; i++)
        {
            RectTransform target = FindRectTransformByPath(targetPaths[i]);
            AddUnique(destination, target);
        }
    }

    private void AddChildTextTargetsByPath(string[] parentPaths, List<RectTransform> destination)
    {
        if (parentPaths == null || parentPaths.Length == 0)
        {
            return;
        }

        for (int i = 0; i < parentPaths.Length; i++)
        {
            RectTransform parent = FindRectTransformByPath(parentPaths[i]);
            if (parent == null)
            {
                continue;
            }

            TMPro.TMP_Text[] texts = parent.GetComponentsInChildren<TMPro.TMP_Text>(true);
            for (int textIndex = 0; textIndex < texts.Length; textIndex++)
            {
                RectTransform textTransform = texts[textIndex].transform as RectTransform;
                if (!IsExcludedChildTextScaleTarget(textTransform))
                {
                    AddUnique(destination, textTransform);
                }
            }
        }
    }

    private bool IsExcludedChildTextScaleTarget(RectTransform target)
    {
        if (target == null || autoChildTextScaleExcludePaths == null || autoChildTextScaleExcludePaths.Length == 0)
        {
            return false;
        }

        for (int i = 0; i < autoChildTextScaleExcludePaths.Length; i++)
        {
            if (FindRectTransformByPath(autoChildTextScaleExcludePaths[i]) == target)
            {
                return true;
            }
        }

        return false;
    }

    private RectTransform FindRectTransformByPath(string targetPath)
    {
        if (string.IsNullOrWhiteSpace(targetPath))
        {
            return null;
        }

        Transform target = transform.Find(targetPath.Replace('\\', '/'));
        return target as RectTransform;
    }

    private static bool NameMatches(string candidate, string[] targetNames)
    {
        for (int i = 0; i < targetNames.Length; i++)
        {
            if (candidate == targetNames[i])
            {
                return true;
            }
        }

        return false;
    }

    private static void AddManualTargets(RectTransform[] targets, List<RectTransform> destination)
    {
        if (targets == null)
        {
            return;
        }

        for (int i = 0; i < targets.Length; i++)
        {
            AddUnique(destination, targets[i]);
        }
    }

    private static void AddUnique(List<RectTransform> list, RectTransform rectTransform)
    {
        if (rectTransform != null && !list.Contains(rectTransform))
        {
            list.Add(rectTransform);
        }
    }

    private readonly struct PositionSnapshot
    {
        private readonly RectTransform rectTransform;
        private readonly Vector2 anchoredPosition;
        private readonly Vector2 offsetMin;
        private readonly Vector2 offsetMax;
        private readonly bool scalesVerticalStretchOffsets;

        public PositionSnapshot(RectTransform rectTransform)
        {
            this.rectTransform = rectTransform;
            anchoredPosition = rectTransform.anchoredPosition;
            offsetMin = rectTransform.offsetMin;
            offsetMax = rectTransform.offsetMax;
            scalesVerticalStretchOffsets = !Mathf.Approximately(rectTransform.anchorMin.y, rectTransform.anchorMax.y);
        }

        public void Apply(float positionScaleX, float stretchScaleY)
        {
            if (rectTransform == null)
            {
                return;
            }

            rectTransform.anchoredPosition = new Vector2(
                anchoredPosition.x * positionScaleX,
                anchoredPosition.y);

            if (!scalesVerticalStretchOffsets)
            {
                return;
            }

            rectTransform.offsetMin = new Vector2(
                rectTransform.offsetMin.x,
                offsetMin.y * stretchScaleY);

            rectTransform.offsetMax = new Vector2(
                rectTransform.offsetMax.x,
                offsetMax.y * stretchScaleY);
        }
    }

    private readonly struct VerticalStretchSnapshot
    {
        private readonly RectTransform rectTransform;
        private readonly VerticalLayoutGroup verticalLayoutGroup;
        private readonly Vector2 offsetMin;
        private readonly Vector2 offsetMax;
        private readonly float spacing;

        public VerticalStretchSnapshot(RectTransform rectTransform)
        {
            this.rectTransform = rectTransform;
            verticalLayoutGroup = rectTransform.GetComponent<VerticalLayoutGroup>();
            offsetMin = rectTransform.offsetMin;
            offsetMax = rectTransform.offsetMax;
            spacing = verticalLayoutGroup != null ? verticalLayoutGroup.spacing : 0f;
        }

        public void Apply(float stretchScaleY)
        {
            if (rectTransform == null)
            {
                return;
            }

            rectTransform.offsetMin = new Vector2(
                rectTransform.offsetMin.x,
                offsetMin.y * stretchScaleY);

            rectTransform.offsetMax = new Vector2(
                rectTransform.offsetMax.x,
                offsetMax.y * stretchScaleY);

            if (verticalLayoutGroup != null)
            {
                verticalLayoutGroup.spacing = spacing * stretchScaleY;
            }
        }
    }

    private readonly struct YPositionOnlySnapshot
    {
        private readonly RectTransform rectTransform;
        private readonly Vector2 anchoredPosition;

        public YPositionOnlySnapshot(RectTransform rectTransform)
        {
            this.rectTransform = rectTransform;
            anchoredPosition = rectTransform.anchoredPosition;
        }

        public void Apply(float positionScaleY)
        {
            if (rectTransform == null)
            {
                return;
            }

            rectTransform.anchoredPosition = new Vector2(
                anchoredPosition.x,
                anchoredPosition.y * positionScaleY);
        }
    }

    private readonly struct XYPositionOnlySnapshot
    {
        private readonly RectTransform rectTransform;
        private readonly Vector2 anchoredPosition;

        public XYPositionOnlySnapshot(RectTransform rectTransform)
        {
            this.rectTransform = rectTransform;
            anchoredPosition = rectTransform.anchoredPosition;
        }

        public void Apply(float scaleX, float scaleY)
        {
            if (rectTransform == null)
            {
                return;
            }

            rectTransform.anchoredPosition = new Vector2(
                anchoredPosition.x * scaleX,
                anchoredPosition.y * scaleY);
        }
    }

    private readonly struct HorizontalStretchYPositionHeightSnapshot
    {
        private readonly RectTransform rectTransform;
        private readonly Vector2 anchoredPosition;
        private readonly Vector2 sizeDelta;
        private readonly Vector2 offsetMin;
        private readonly Vector2 offsetMax;

        public HorizontalStretchYPositionHeightSnapshot(RectTransform rectTransform)
        {
            this.rectTransform = rectTransform;
            anchoredPosition = rectTransform.anchoredPosition;
            sizeDelta = rectTransform.sizeDelta;
            offsetMin = rectTransform.offsetMin;
            offsetMax = rectTransform.offsetMax;
        }

        public void Apply(float scaleX, float scaleY)
        {
            if (rectTransform == null)
            {
                return;
            }

            rectTransform.anchoredPosition = new Vector2(
                anchoredPosition.x,
                anchoredPosition.y * scaleY);

            rectTransform.sizeDelta = new Vector2(
                rectTransform.sizeDelta.x,
                sizeDelta.y * scaleY);

            rectTransform.offsetMin = new Vector2(
                offsetMin.x * scaleX,
                rectTransform.offsetMin.y);

            rectTransform.offsetMax = new Vector2(
                offsetMax.x * scaleX,
                rectTransform.offsetMax.y);
        }
    }

    private readonly struct StretchOffsetsSnapshot
    {
        private readonly RectTransform rectTransform;
        private readonly Vector2 offsetMin;
        private readonly Vector2 offsetMax;

        public StretchOffsetsSnapshot(RectTransform rectTransform)
        {
            this.rectTransform = rectTransform;
            offsetMin = rectTransform.offsetMin;
            offsetMax = rectTransform.offsetMax;
        }

        public void Apply(float scaleX, float scaleY)
        {
            if (rectTransform == null)
            {
                return;
            }

            rectTransform.offsetMin = new Vector2(
                offsetMin.x * scaleX,
                offsetMin.y * scaleY);

            rectTransform.offsetMax = new Vector2(
                offsetMax.x * scaleX,
                offsetMax.y * scaleY);
        }
    }

    private readonly struct WidthAndStretchSnapshot
    {
        private readonly RectTransform rectTransform;
        private readonly VerticalLayoutGroup verticalLayoutGroup;
        private readonly Vector2 sizeDelta;
        private readonly Vector2 offsetMin;
        private readonly Vector2 offsetMax;
        private readonly float spacing;

        public WidthAndStretchSnapshot(RectTransform rectTransform)
        {
            this.rectTransform = rectTransform;
            verticalLayoutGroup = rectTransform.GetComponent<VerticalLayoutGroup>();
            sizeDelta = rectTransform.sizeDelta;
            offsetMin = rectTransform.offsetMin;
            offsetMax = rectTransform.offsetMax;
            spacing = verticalLayoutGroup != null ? verticalLayoutGroup.spacing : 0f;
        }

        public void Apply(float widthScaleX, float stretchScaleY)
        {
            if (rectTransform == null)
            {
                return;
            }

            rectTransform.sizeDelta = new Vector2(
                sizeDelta.x * widthScaleX,
                rectTransform.sizeDelta.y);

            rectTransform.offsetMin = new Vector2(
                rectTransform.offsetMin.x,
                offsetMin.y * stretchScaleY);

            rectTransform.offsetMax = new Vector2(
                rectTransform.offsetMax.x,
                offsetMax.y * stretchScaleY);

            if (verticalLayoutGroup != null)
            {
                verticalLayoutGroup.spacing = spacing * stretchScaleY;
            }
        }
    }

    private readonly struct SizeAndLayoutSpacingSnapshot
    {
        private readonly RectTransform rectTransform;
        private readonly VerticalLayoutGroup verticalLayoutGroup;
        private readonly Vector2 sizeDelta;
        private readonly float spacing;

        public SizeAndLayoutSpacingSnapshot(RectTransform rectTransform)
        {
            this.rectTransform = rectTransform;
            verticalLayoutGroup = rectTransform.GetComponent<VerticalLayoutGroup>();
            sizeDelta = rectTransform.sizeDelta;
            spacing = verticalLayoutGroup != null ? verticalLayoutGroup.spacing : 0f;
        }

        public void Apply(float scaleX, float scaleY)
        {
            if (rectTransform == null)
            {
                return;
            }

            rectTransform.sizeDelta = new Vector2(
                sizeDelta.x * scaleX,
                sizeDelta.y * scaleY);

            if (verticalLayoutGroup != null)
            {
                verticalLayoutGroup.spacing = spacing * scaleY;
            }
        }
    }

    private readonly struct SizeAndYPositionSnapshot
    {
        private readonly RectTransform rectTransform;
        private readonly Vector2 anchoredPosition;
        private readonly Vector2 sizeDelta;

        public SizeAndYPositionSnapshot(RectTransform rectTransform)
        {
            this.rectTransform = rectTransform;
            anchoredPosition = rectTransform.anchoredPosition;
            sizeDelta = rectTransform.sizeDelta;
        }

        public void Apply(float scaleX, float scaleY)
        {
            if (rectTransform == null)
            {
                return;
            }

            rectTransform.anchoredPosition = new Vector2(
                anchoredPosition.x,
                anchoredPosition.y * scaleY);

            rectTransform.sizeDelta = new Vector2(
                sizeDelta.x * scaleX,
                sizeDelta.y * scaleY);
        }
    }

    private readonly struct DirectScaleSnapshot
    {
        private readonly RectTransform rectTransform;
        private readonly Vector3 localScale;

        public DirectScaleSnapshot(RectTransform rectTransform)
        {
            this.rectTransform = rectTransform;
            localScale = rectTransform.localScale;
        }

        public void Apply(float scaleX, float scaleY)
        {
            if (rectTransform != null)
            {
                rectTransform.localScale = new Vector3(
                    localScale.x * scaleX,
                    localScale.y * scaleY,
                    localScale.z);
            }
        }
    }

    private readonly struct DirectScaleXOnlySnapshot
    {
        private readonly RectTransform rectTransform;
        private readonly Vector3 localScale;

        public DirectScaleXOnlySnapshot(RectTransform rectTransform)
        {
            this.rectTransform = rectTransform;
            localScale = rectTransform.localScale;
        }

        public void Apply(float scaleX)
        {
            if (rectTransform != null)
            {
                rectTransform.localScale = new Vector3(
                    localScale.x * scaleX,
                    localScale.y,
                    localScale.z);
            }
        }
    }

    private readonly struct UniformScaleSnapshot
    {
        private readonly RectTransform rectTransform;
        private readonly Vector3 localScale;

        public UniformScaleSnapshot(RectTransform rectTransform)
        {
            this.rectTransform = rectTransform;
            localScale = rectTransform.localScale;
        }

        public void Apply(float scale)
        {
            if (rectTransform != null)
            {
                rectTransform.localScale = localScale * scale;
            }
        }
    }

    private readonly struct ScreenRatioScaleSnapshot
    {
        private readonly RectTransform rectTransform;
        private readonly Vector3 localScale;

        public ScreenRatioScaleSnapshot(RectTransform rectTransform)
        {
            this.rectTransform = rectTransform;
            localScale = rectTransform.localScale;
        }

        public void Apply(float scaleX, float scaleY)
        {
            if (rectTransform != null)
            {
                rectTransform.localScale = new Vector3(
                    localScale.x * scaleX,
                    localScale.y * scaleY,
                    localScale.z);
            }
        }
    }
}
