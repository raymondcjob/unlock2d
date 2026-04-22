using TMPro;
using UnityEngine;

[RequireComponent(typeof(TMP_Text))]
public class LocalizedText : MonoBehaviour
{
    [SerializeField] private string localizationKey;

    private TMP_Text targetText;

    private void Awake()
    {
        targetText = GetComponent<TMP_Text>();
    }

    private void OnEnable()
    {
        LocalizationManager.OnLanguageChanged += RefreshText;
        RefreshText();
    }

    private void OnDisable()
    {
        LocalizationManager.OnLanguageChanged -= RefreshText;
    }

    public void RefreshText()
    {
        if (targetText == null)
        {
            targetText = GetComponent<TMP_Text>();
        }

        targetText.text = LocalizationManager.GetText(localizationKey);
    }
}
