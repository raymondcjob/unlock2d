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
                    { "game.title", "開鎖" },
                    { "main.continue", "繼續" },
                    { "main.newGame", "新遊戲" },
                    { "main.settings", "設定" },
                    { "main.howToPlay", "玩法" },
                    { "main.quit", "離開遊戲" },
                    { "gameMenu.title", "遊戲暫停" },
                    { "gameMenu.resume", "繼續" },
                    { "gameMenu.restart", "重新開始" },
                    { "gameMenu.mainMenu", "主選單" },
                    { "settings.title", "設定" },
                    { "settings.themes", "主題" },
                    { "settings.languages", "語言" },
                    { "settings.languageTitle", "語言" },
                    { "settings.back", "返回" }
                }
            },
            {
                "zh-cn",
                new Dictionary<string, string>
                {
                    { "game.title", "开锁" },
                    { "main.continue", "继续" },
                    { "main.newGame", "新游戏" },
                    { "main.settings", "设置" },
                    { "main.howToPlay", "玩法" },
                    { "main.quit", "退出游戏" },
                    { "gameMenu.title", "游戏暂停" },
                    { "gameMenu.resume", "继续" },
                    { "gameMenu.restart", "重新开始" },
                    { "gameMenu.mainMenu", "主菜单" },
                    { "settings.title", "设置" },
                    { "settings.themes", "主题" },
                    { "settings.languages", "语言" },
                    { "settings.languageTitle", "语言" },
                    { "settings.back", "返回" }
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
