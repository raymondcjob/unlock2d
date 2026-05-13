using TMPro;
using UnityEngine;

namespace Unlock2D.UI
{
    [ExecuteAlways]
    public class TMPTextSizeScaler : MonoBehaviour
    {
        [SerializeField] private TMP_Text[] targetTexts;
        [SerializeField] private float baseScreenWidth;
        [SerializeField] private float baseScreenHeight;
        [SerializeField] private float baseFontSize;
        [SerializeField] private bool logScaleFactor;

        private int lastScreenWidth = -1;
        private int lastScreenHeight = -1;
        private bool hasLoggedMissingBaseValues;

        private void OnEnable()
        {
            ApplyScale(force: true);
        }

        private void Update()
        {
            ApplyScale(force: false);
        }

        private void OnValidate()
        {
            lastScreenWidth = -1;
            lastScreenHeight = -1;
            hasLoggedMissingBaseValues = false;
        }

        private void ApplyScale(bool force)
        {
            int screenWidth = Mathf.Max(1, Screen.width);
            int screenHeight = Mathf.Max(1, Screen.height);

            if (!force && screenWidth == lastScreenWidth && screenHeight == lastScreenHeight)
            {
                return;
            }

            if (!HasValidBaseValues())
            {
                LogMissingBaseValuesOnce();
                lastScreenWidth = screenWidth;
                lastScreenHeight = screenHeight;
                return;
            }

            float scaleX = screenWidth / Mathf.Abs(baseScreenWidth);
            float scaleY = screenHeight / Mathf.Abs(baseScreenHeight);
            float scaleFactor = Mathf.Min(scaleX, scaleY);
            float scaledFontSize = baseFontSize * scaleFactor;

            if (targetTexts != null)
            {
                for (int i = 0; i < targetTexts.Length; i++)
                {
                    if (targetTexts[i] == null)
                    {
                        continue;
                    }

                    targetTexts[i].fontSize = scaledFontSize;
                }
            }

            LogScaleApplied(scaleFactor);
            lastScreenWidth = screenWidth;
            lastScreenHeight = screenHeight;
        }

        private bool HasValidBaseValues()
        {
            return !Mathf.Approximately(baseScreenWidth, 0f) &&
                   !Mathf.Approximately(baseScreenHeight, 0f) &&
                   !Mathf.Approximately(baseFontSize, 0f);
        }

        private void LogMissingBaseValuesOnce()
        {
            if (hasLoggedMissingBaseValues)
            {
                return;
            }

            hasLoggedMissingBaseValues = true;
            Debug.LogWarning(
                $"{nameof(TMPTextSizeScaler)} on '{name}': No base scaling value exists for Text Size.",
                this);
        }

        private void LogScaleApplied(float scaleFactor)
        {
            if (!logScaleFactor)
            {
                return;
            }

            Debug.Log($"Text Size is scaled by a factor of {scaleFactor:0.###}", this);
        }
    }
}
