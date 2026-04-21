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
    [SerializeField] private GameObject mainSettingsPanel;
    [SerializeField] private GameObject languageSelectionPanel;

    [Header("Buttons")]
    [SerializeField] private Button musicButton;
    [SerializeField] private Button soundButton;
    [SerializeField] private Button themesButton;
    [SerializeField] private Button languagesButton;
    [SerializeField] private Button backButton;

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

    public void OnClickBack()
    {
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
}
