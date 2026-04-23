using System;
using System.Collections.Generic;
using UnityEngine;

public static class LocalizationManager
{
    public const string LanguageKey = "settings.language";
    public const string DefaultLanguageCode = "en";

    private static readonly Dictionary<string, Dictionary<string, string>> Translations =
        new Dictionary<string, Dictionary<string, string>>
        {
            {
                "en",
                new Dictionary<string, string>
                {
                    { "game.title", "Lockpick" },
                    { "main.continue", "Continue" },
                    { "main.newGame", "New Game" },
                    { "main.boardSizeTitle", "Board Difficulty" },
                    { "main.boardSize.twoSuits", "Easy" },
                    { "main.boardSize.redDragon", "Normal" },
                    { "main.boardSize.fullTiles", "Hard" },
                    { "main.settings", "Settings" },
                    { "main.howToPlay", "How To Play" },
                    { "main.quit", "Quit Game" },
                    { "gameMenu.title", "Game Paused" },
                    { "gameMenu.resume", "Resume" },
                    { "gameMenu.restart", "Restart" },
                    { "gameMenu.mainMenu", "Main Menu" },
                    { "settings.title", "Settings" },
                    { "settings.themes", "Theme" },
                    { "settings.languages", "Languages" },
                    { "settings.languageTitle", "Languages" },
                    { "settings.back", "Return / Close" }
                }
            },
            {
                "zh-hk",
                new Dictionary<string, string>
                {
                    { "game.title", "\u958b\u9396" },
                    { "main.continue", "\u7e7c\u7e8c" },
                    { "main.newGame", "\u65b0\u904a\u6232" },
                    { "main.boardSizeTitle", "\u96e3\u5ea6\u9078\u64c7" },
                    { "main.boardSize.twoSuits", "\u7f3a\u4e00\u8272" },
                    { "main.boardSize.redDragon", "\u7d05\u4e2d\u7028" },
                    { "main.boardSize.fullTiles", "\u7121\u82b1\u724c" },
                    { "main.settings", "\u8a2d\u5b9a" },
                    { "main.howToPlay", "\u73a9\u6cd5" },
                    { "main.quit", "\u96e2\u958b\u904a\u6232" },
                    { "gameMenu.title", "\u904a\u6232\u66ab\u505c" },
                    { "gameMenu.resume", "\u7e7c\u7e8c" },
                    { "gameMenu.restart", "\u91cd\u65b0\u958b\u59cb" },
                    { "gameMenu.mainMenu", "\u4e3b\u9078\u55ae" },
                    { "settings.title", "\u8a2d\u5b9a" },
                    { "settings.themes", "\u4e3b\u984c" },
                    { "settings.languages", "\u8a9e\u8a00" },
                    { "settings.languageTitle", "\u8a9e\u8a00" },
                    { "settings.back", "\u8fd4\u56de" }
                }
            },
            {
                "zh-cn",
                new Dictionary<string, string>
                {
                    { "game.title", "\u5f00\u9501" },
                    { "main.continue", "\u7ee7\u7eed" },
                    { "main.newGame", "\u65b0\u6e38\u620f" },
                    { "main.boardSizeTitle", "\u96be\u5ea6\u9009\u62e9" },
                    { "main.boardSize.twoSuits", "\u56db\u5ddd\u9ebb" },
                    { "main.boardSize.redDragon", "\u7ea2\u4e2d\u98de" },
                    { "main.boardSize.fullTiles", "\u6807\u51c6\u9ebb" },
                    { "main.settings", "\u8bbe\u7f6e" },
                    { "main.howToPlay", "\u73a9\u6cd5" },
                    { "main.quit", "\u9000\u51fa\u6e38\u620f" },
                    { "gameMenu.title", "\u6e38\u620f\u6682\u505c" },
                    { "gameMenu.resume", "\u7ee7\u7eed" },
                    { "gameMenu.restart", "\u91cd\u65b0\u5f00\u59cb" },
                    { "gameMenu.mainMenu", "\u4e3b\u83dc\u5355" },
                    { "settings.title", "\u8bbe\u7f6e" },
                    { "settings.themes", "\u4e3b\u9898" },
                    { "settings.languages", "\u8bed\u8a00" },
                    { "settings.languageTitle", "\u8bed\u8a00" },
                    { "settings.back", "\u8fd4\u56de" }
                }
            }
        };

    public static event Action OnLanguageChanged;

    public static string CurrentLanguageCode
    {
        get
        {
            EnsureLanguageInitialized();
            return PlayerPrefs.GetString(LanguageKey, DefaultLanguageCode);
        }
    }

    public static void EnsureLanguageInitialized()
    {
        if (PlayerPrefs.HasKey(LanguageKey))
        {
            return;
        }

        PlayerPrefs.SetString(LanguageKey, GetSystemLanguageCode());
        PlayerPrefs.Save();
    }

    public static void SetLanguage(string languageCode)
    {
        if (string.IsNullOrEmpty(languageCode) || !Translations.ContainsKey(languageCode))
        {
            languageCode = DefaultLanguageCode;
        }

        PlayerPrefs.SetString(LanguageKey, languageCode);
        PlayerPrefs.Save();
        OnLanguageChanged?.Invoke();
    }

    public static string GetText(string key)
    {
        if (string.IsNullOrEmpty(key))
        {
            return string.Empty;
        }

        string languageCode = CurrentLanguageCode;

        if (Translations.TryGetValue(languageCode, out Dictionary<string, string> languageTexts) &&
            languageTexts.TryGetValue(key, out string translatedText))
        {
            return translatedText;
        }

        if (Translations[DefaultLanguageCode].TryGetValue(key, out string defaultText))
        {
            return defaultText;
        }

        return key;
    }

    private static string GetSystemLanguageCode()
    {
        switch (Application.systemLanguage)
        {
            case SystemLanguage.ChineseTraditional:
                return "zh-hk";
            case SystemLanguage.Chinese:
            case SystemLanguage.ChineseSimplified:
                return "zh-cn";
            case SystemLanguage.English:
            default:
                return DefaultLanguageCode;
        }
    }
}
