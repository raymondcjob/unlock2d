using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Unlock2D.UI
{
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    public class CurrencyTextScaler : MonoBehaviour
    {
        [SerializeField] private GameObject icon;

        private RectTransform rectTransform;
        private RectTransform iconRectTransform;
        private bool isApplying;
#if UNITY_EDITOR
        private bool isEditorRefreshQueued;
#endif

        private void Awake()
        {
            ApplyScale();
        }

        private void OnEnable()
        {
            ApplyScale();
        }

        private void Update()
        {
            ApplyScale();
        }

        private void OnValidate()
        {
            rectTransform = null;
            iconRectTransform = null;

#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                QueueEditorRefresh();
                return;
            }
#endif

            ApplyScale();
        }

        private void OnRectTransformDimensionsChange()
        {
            if (isApplying)
            {
                return;
            }

            ApplyScale();
        }

        private void ApplyScale()
        {
            if (!EnsureRectTransform() || !EnsureIconRectTransform())
            {
                return;
            }

            isApplying = true;

            float stretchValue = iconRectTransform.anchoredPosition.x + iconRectTransform.rect.width;
            Vector2 offsetMin = rectTransform.offsetMin;
            Vector2 offsetMax = rectTransform.offsetMax;

            if (!Mathf.Approximately(offsetMin.x, stretchValue))
            {
                offsetMin.x = stretchValue;
                rectTransform.offsetMin = offsetMin;
            }

            float rightValue = -stretchValue;
            if (!Mathf.Approximately(offsetMax.x, rightValue))
            {
                offsetMax.x = rightValue;
                rectTransform.offsetMax = offsetMax;
            }

            isApplying = false;
        }

        private bool EnsureRectTransform()
        {
            if (rectTransform == null)
            {
                rectTransform = GetComponent<RectTransform>();
            }

            return rectTransform != null;
        }

        private bool EnsureIconRectTransform()
        {
            if (icon == null)
            {
                iconRectTransform = null;
                return false;
            }

            if (icon == gameObject)
            {
                iconRectTransform = null;
                return false;
            }

            if (iconRectTransform == null || iconRectTransform.gameObject != icon)
            {
                iconRectTransform = icon.GetComponent<RectTransform>();
            }

            return iconRectTransform != null;
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

            ApplyScale();
        }
#endif
    }
}
