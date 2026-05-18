using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Unlock2D.UI
{
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    public class CurrencyIconScaler : MonoBehaviour
    {
        [SerializeField] private float widthFactor = 1f;
        [SerializeField] private float posXFactor = 1f;
        [SerializeField] private bool invertPosX;

        private RectTransform rectTransform;
        private bool isApplying;
#if UNITY_EDITOR
        private bool isEditorRefreshQueued;
#endif

        private void Awake()
        {
            EnsureRectTransform();
            ApplyScale();
        }

        private void OnEnable()
        {
            EnsureRectTransform();
            ApplyScale();
        }

        private void Update()
        {
            ApplyScale();
        }

        private void OnValidate()
        {
            EnsureRectTransform();

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
            if (!EnsureRectTransform())
            {
                return;
            }

            float height = rectTransform.rect.height;
            float posX = height * posXFactor;
            RectTransform parent = rectTransform.parent as RectTransform;
            if (invertPosX && parent != null)
            {
                posX = parent.rect.width - height * posXFactor;
            }

            isApplying = true;
            try
            {
                Vector2 anchoredPosition = rectTransform.anchoredPosition;
                if (!Mathf.Approximately(anchoredPosition.x, posX))
                {
                    anchoredPosition.x = posX;
                    rectTransform.anchoredPosition = anchoredPosition;
                }

                float width = height * widthFactor;
                if (!Mathf.Approximately(rectTransform.rect.width, width))
                {
                    rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
                }
            }
            finally
            {
                isApplying = false;
            }
        }

        private bool EnsureRectTransform()
        {
            if (rectTransform == null)
            {
                rectTransform = GetComponent<RectTransform>();
            }

            return rectTransform != null;
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
