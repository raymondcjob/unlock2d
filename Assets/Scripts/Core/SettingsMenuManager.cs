using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class SettingsMenuManager : MonoBehaviour
{
    private const string MusicMutedKey = "settings.musicMuted";
    private const string SoundMutedKey = "settings.soundMuted";
    private const string AutoHintEnabledKey = "settings.autoHintEnabled";
    private static string returnSceneName = "MainMenu";

    [Header("Scene Names")]
    [SerializeField] private string mainMenuSceneName = "MainMenu";

    [Header("Panels")]
    [SerializeField] private Transform settingsOverlaysRoot;
    [SerializeField] private GameObject mainSettingsPanel;
    [SerializeField] private GameObject languageSelectionPanel;

    [Header("Buttons")]
    [SerializeField] private Button musicButton;
    [SerializeField] private Button soundButton;
    [SerializeField] private Button hintButton;
    [SerializeField] private Button themesButton;
    [SerializeField] private Button languagesButton;
    [SerializeField] private Button backButton;
    [SerializeField] private Button englishButton;
    [SerializeField] private Button traditionalChineseButton;
    [SerializeField] private Button simplifiedChineseButton;

    [Header("Button Images")]
    [SerializeField] private Image musicButtonImage;
    [SerializeField] private Image soundButtonImage;
    [SerializeField] private Image hintButtonImage;

    [Header("Muted Visual")]
    [SerializeField] private Color mutedColor = new Color(0.7f, 0.7f, 0.7f, 1f);

    private Color musicOriginalColor = Color.white;
    private Color soundOriginalColor = Color.white;
    private Color hintOriginalColor = Color.white;
    private Color hintTextOriginalColor = Color.white;
    private bool musicMuted;
    private bool soundMuted;
    private bool autoHintEnabled;
    private TMP_Text hintButtonText;

    public static void SetReturnScene(string sceneName)
    {
        if (!string.IsNullOrEmpty(sceneName))
        {
            returnSceneName = sceneName;
        }
    }

    private void Awake()
    {
        ShowOnlyMainSettingsPanel();
        ResolveHintButtonReferences();

        if (musicButtonImage != null)
        {
            musicOriginalColor = musicButtonImage.color;
        }

        if (soundButtonImage != null)
        {
            soundOriginalColor = soundButtonImage.color;
        }

        if (hintButtonImage != null)
        {
            hintOriginalColor = hintButtonImage.color;
        }

        if (hintButtonText != null)
        {
            hintTextOriginalColor = hintButtonText.color;
        }

        musicMuted = PlayerPrefs.GetInt(MusicMutedKey, 0) == 1;
        soundMuted = PlayerPrefs.GetInt(SoundMutedKey, 0) == 1;
        autoHintEnabled = PlayerPrefs.GetInt(AutoHintEnabledKey, 1) == 1;

        RegisterButtonListeners();
        RefreshToggleButtonVisuals();
        RefreshLocalizedTexts();
    }

    private void OnEnable()
    {
        LocalizationManager.OnLanguageChanged += RefreshLocalizedTexts;
    }

    private void OnDisable()
    {
        LocalizationManager.OnLanguageChanged -= RefreshLocalizedTexts;
    }

    private void Update()
    {
        if (GameInput.IsBackPressedThisFrame())
        {
            OnClickBack();
        }
    }

    public void OnClickMusic()
    {
        musicMuted = !musicMuted;
        PlayerPrefs.SetInt(MusicMutedKey, musicMuted ? 1 : 0);
        PlayerPrefs.Save();
        RefreshToggleButtonVisuals();
    }

    public void OnClickSound()
    {
        soundMuted = !soundMuted;
        PlayerPrefs.SetInt(SoundMutedKey, soundMuted ? 1 : 0);
        PlayerPrefs.Save();
        RefreshToggleButtonVisuals();
    }

    public void OnClickHint()
    {
        autoHintEnabled = !autoHintEnabled;
        PlayerPrefs.SetInt(AutoHintEnabledKey, autoHintEnabled ? 1 : 0);
        PlayerPrefs.Save();
        RefreshToggleButtonVisuals();
    }

    public void OnClickThemes()
    {
        Debug.Log("Themes button clicked. Store scene/panel will be connected later.");
    }

    public void OnClickLanguages()
    {
        if (mainSettingsPanel != null && languageSelectionPanel != null)
        {
            mainSettingsPanel.SetActive(false);
            languageSelectionPanel.SetActive(true);
            return;
        }

        Debug.Log("Languages button clicked. Language selection panel will be connected later.");
    }

    public void OnClickEnglish()
    {
        SelectLanguage("en");
    }

    public void OnClickTraditionalChinese()
    {
        SelectLanguage("zh-hk");
    }

    public void OnClickSimplifiedChinese()
    {
        SelectLanguage("zh-cn");
    }

    public void OnClickBack()
    {
        if (languageSelectionPanel != null && languageSelectionPanel.activeSelf)
        {
            ShowMainSettingsPanel();
            return;
        }

        string targetSceneName = string.IsNullOrEmpty(returnSceneName) ? mainMenuSceneName : returnSceneName;

        if (targetSceneName == "GameScene")
        {
            GameManager.RequestRestorePendingReturnGameOnNextGameScene();
        }

        SceneManager.LoadScene(targetSceneName);
    }

    private void RegisterButtonListeners()
    {
        if (musicButton != null)
        {
            musicButton.onClick.AddListener(OnClickMusic);
        }

        if (soundButton != null)
        {
            soundButton.onClick.AddListener(OnClickSound);
        }

        if (hintButton != null)
        {
            hintButton.onClick.AddListener(OnClickHint);
        }

        if (themesButton != null)
        {
            themesButton.onClick.AddListener(OnClickThemes);
        }

        if (languagesButton != null)
        {
            languagesButton.onClick.AddListener(OnClickLanguages);
        }

        if (backButton != null)
        {
            backButton.onClick.AddListener(OnClickBack);
        }

        if (englishButton != null)
        {
            englishButton.onClick.AddListener(OnClickEnglish);
        }

        if (traditionalChineseButton != null)
        {
            traditionalChineseButton.onClick.AddListener(OnClickTraditionalChinese);
        }

        if (simplifiedChineseButton != null)
        {
            simplifiedChineseButton.onClick.AddListener(OnClickSimplifiedChinese);
        }
    }

    private void RefreshToggleButtonVisuals()
    {
        if (musicButtonImage != null)
        {
            musicButtonImage.color = musicMuted ? mutedColor : musicOriginalColor;
        }

        if (soundButtonImage != null)
        {
            soundButtonImage.color = soundMuted ? mutedColor : soundOriginalColor;
        }

        if (hintButtonImage != null)
        {
            hintButtonImage.color = autoHintEnabled ? hintOriginalColor : mutedColor;
        }

        if (hintButtonText != null)
        {
            hintButtonText.color = autoHintEnabled ? hintTextOriginalColor : mutedColor;
        }
    }

    private void SelectLanguage(string languageCode)
    {
        LocalizationManager.SetLanguage(languageCode);
        ShowMainSettingsPanel();
        Debug.Log($"Selected language: {languageCode}");
    }

    private void ShowMainSettingsPanel()
    {
        ShowOnlyMainSettingsPanel();
    }

    private void ShowOnlyMainSettingsPanel()
    {
        Transform root = settingsOverlaysRoot != null ? settingsOverlaysRoot : transform;

        foreach (Transform child in root)
        {
            child.gameObject.SetActive(mainSettingsPanel != null && child.gameObject == mainSettingsPanel);
        }

        if (mainSettingsPanel != null)
        {
            mainSettingsPanel.SetActive(true);
        }

        if (languageSelectionPanel != null)
        {
            languageSelectionPanel.SetActive(false);
        }
    }

    private void ResolveHintButtonReferences()
    {
        if (hintButton == null)
        {
            Transform hintTransform = null;

            if (settingsOverlaysRoot != null)
            {
                hintTransform = settingsOverlaysRoot.Find("SettingsPanel/Content/Hint");
            }

            if (hintTransform == null)
            {
                hintTransform = transform.Find("SettingsOverlays/SettingsPanel/Content/Hint");
            }

            if (hintTransform == null)
            {
                hintTransform = transform.Find("SettingsPanel/Content/Hint");
            }

            if (hintTransform == null)
            {
                GameObject hintObject = GameObject.Find("Hint");
                if (hintObject != null)
                {
                    hintTransform = hintObject.transform;
                }
            }

            if (hintTransform != null)
            {
                hintButton = hintTransform.GetComponent<Button>();
            }
        }

        if (hintButton != null)
        {
            if (hintButtonImage == null)
            {
                hintButtonImage = hintButton.GetComponent<Image>();
            }

            if (hintButtonText == null)
            {
                hintButtonText = hintButton.GetComponentInChildren<TMP_Text>(true);
            }
        }
    }

    private void RefreshLocalizedTexts()
    {
        if (hintButtonText == null)
        {
            ResolveHintButtonReferences();
        }

        if (hintButtonText != null)
        {
            hintButtonText.text = LocalizationManager.GetText("settings.hint");
        }
    }
}
