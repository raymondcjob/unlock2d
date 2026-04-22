using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SettingsMenuManager : MonoBehaviour
{
    private const string MusicMutedKey = "settings.musicMuted";
    private const string SoundMutedKey = "settings.soundMuted";

    [Header("Scene Names")]
    [SerializeField] private string mainMenuSceneName = "MainMenu";

    [Header("Panels")]
    [SerializeField] private Transform settingsOverlaysRoot;
    [SerializeField] private GameObject mainSettingsPanel;
    [SerializeField] private GameObject languageSelectionPanel;

    [Header("Buttons")]
    [SerializeField] private Button musicButton;
    [SerializeField] private Button soundButton;
    [SerializeField] private Button themesButton;
    [SerializeField] private Button languagesButton;
    [SerializeField] private Button backButton;
    [SerializeField] private Button englishButton;
    [SerializeField] private Button traditionalChineseButton;
    [SerializeField] private Button simplifiedChineseButton;

    [Header("Button Images")]
    [SerializeField] private Image musicButtonImage;
    [SerializeField] private Image soundButtonImage;

    [Header("Muted Visual")]
    [SerializeField] private Color mutedColor = new Color(0.7f, 0.7f, 0.7f, 1f);

    private Color musicOriginalColor = Color.white;
    private Color soundOriginalColor = Color.white;
    private bool musicMuted;
    private bool soundMuted;

    private void Awake()
    {
        ShowOnlyMainSettingsPanel();

        if (musicButtonImage != null)
        {
            musicOriginalColor = musicButtonImage.color;
        }

        if (soundButtonImage != null)
        {
            soundOriginalColor = soundButtonImage.color;
        }

        musicMuted = PlayerPrefs.GetInt(MusicMutedKey, 0) == 1;
        soundMuted = PlayerPrefs.GetInt(SoundMutedKey, 0) == 1;

        RegisterButtonListeners();
        RefreshAudioButtonVisuals();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            OnClickBack();
        }
    }

    public void OnClickMusic()
    {
        musicMuted = !musicMuted;
        PlayerPrefs.SetInt(MusicMutedKey, musicMuted ? 1 : 0);
        PlayerPrefs.Save();
        RefreshAudioButtonVisuals();
    }

    public void OnClickSound()
    {
        soundMuted = !soundMuted;
        PlayerPrefs.SetInt(SoundMutedKey, soundMuted ? 1 : 0);
        PlayerPrefs.Save();
        RefreshAudioButtonVisuals();
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

        SceneManager.LoadScene(mainMenuSceneName);
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

    private void RefreshAudioButtonVisuals()
    {
        if (musicButtonImage != null)
        {
            musicButtonImage.color = musicMuted ? mutedColor : musicOriginalColor;
        }

        if (soundButtonImage != null)
        {
            soundButtonImage.color = soundMuted ? mutedColor : soundOriginalColor;
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
}
