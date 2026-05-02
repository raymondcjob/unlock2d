using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

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
                    { "game.title", "Magic Seal" },
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
                    { "gameMenu.noMoves", "No further matches available" },
                    { "gameMenu.winTitle", "You sealed it!" },
                    { "gameMenu.match", "Match" },
                    { "gameMenu.cancel", "Cancel" },
                    { "howTo.step1.title", "Course 1" },
                    { "howTo.step1.body", "Here is your magic course. The two tiles right next to each other is on fire.\nTap on either one to seal them." },
                    { "howTo.step2.title", "Course 2" },
                    { "howTo.step2.body", "The seals become a path and allow tiles to be sealed without blocking them.\nLet's seal the water now." },
                    { "howTo.step3.title", "Course 3" },
                    { "howTo.step3.body", "Now try a drag seal. Drag the boulder along the path to seal it with another." },
                    { "howTo.step4.title", "Course 4" },
                    { "howTo.step4.body", "Finally, you can also drag and push connected tiles along the path. Try to seal the two wind tiles." },
                    { "howTo.undo.title", "Spell - Undo" },
                    { "howTo.undo.body", "Undo rewinds your last move. You may also rewind item moves. Cast Undo to bring back the tiles you just sealed." },
                    { "howTo.shuffle.title", "Spell - Shuffle" },
                    { "howTo.shuffle.body", "Shuffle rearranges the remaining board when the magic cannot resolve. Let's mess it up!" },
                    { "howTo.swapButton.title", "Spell - Swap" },
                    { "howTo.swapButton.body", "Swap lets you exchange the positions of two tiles. Try it out!" },
                    { "howTo.swapTiles.title", "Casting - Swap" },
                    { "howTo.swapTiles.body", "Swap the two tiles to continue." },
                    { "howTo.complete.title", "Course Completed" },
                    { "howTo.complete.body", "You have completed the magic course. Your goal is to seal the whole board. Note that some boards are unsolvable." },
                    { "settings.title", "Settings" },
                    { "settings.hint", "Hint" },
                    { "settings.themes", "Theme" },
                    { "settings.languages", "Languages" },
                    { "settings.languageTitle", "Languages" },
                    { "settings.back", "Return" }
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
                    { "gameMenu.noMoves", "\u4f60\u5df2\u9032\u5165\u6b7b\u5c40 \u4f7f\u7528\u9053\u5177\u89e3\u958b\u6216\u91cd\u65b0\u958b\u59cb\u5427" },
                    { "gameMenu.winTitle", "\u904e\u95dc\u4e86\uff01" },
                    { "gameMenu.match", "\u914d\u5c0d" },
                    { "gameMenu.cancel", "\u53d6\u6d88" },
                    { "howTo.step1.title", "\u6559\u7a0b 1" },
                    { "howTo.step1.body", "\u4f60\u53ef\u4ee5\u76f4\u63a5\u6d88\u9664\u5169\u5f35\u76f8\u540c\u4e14\u9130\u8fd1\u7684\u9ebb\u5c07\u724c\u3002\u9ede\u64ca\u5176\u4e2d\u4e00\u5f35\u4ee5\u7e7c\u7e8c\u3002" },
                    { "howTo.step2.title", "\u6559\u7a0b 2" },
                    { "howTo.step2.body", "\u5982\u679c\u5169\u5f35\u76f8\u540c\u724c\u5728\u540c\u4e00\u884c\u6216\u540c\u4e00\u5217\uff0c\u4e14\u4e2d\u9593\u6c92\u6709\u7ffb\u958b\u7684\u724c\uff0c\u4e5f\u53ef\u4ee5\u76f4\u63a5\u6d88\u9664\u3002\u9ede\u64ca\u5176\u4e2d\u4e00\u5f35\u4ee5\u7e7c\u7e8c\u3002" },
                    { "howTo.step3.title", "\u6559\u7a0b 3" },
                    { "howTo.step3.body", "\u73fe\u5728\u8a66\u8a66\u62d6\u66f3\u79fb\u52d5\u3002\u6b63\u9762\u9ebb\u5c07\u53ef\u4ee5\u6cbf\u8457\u53cd\u9762\u9ebb\u5c07\u5728\u540c\u4e00\u884c\u6216\u540c\u4e00\u5217\u9032\u884c\u62d6\u62fd\uff0c\u4ee5\u914d\u5c0d\u8207\u5176\u5728\u4e0d\u540c\u884c\u6216\u4e0d\u540c\u5217\u7684\u9ebb\u5c07\u3002" },
                    { "howTo.step4.title", "\u6559\u7a0b 4" },
                    { "howTo.step4.body", "\u6700\u5f8c\uff0c\u4f60\u4e5f\u53ef\u4ee5\u63a8\u52d5\u5176\u4ed6\u9ebb\u5c07\u4ee5\u9032\u884c\u914d\u5c0d\u3002\u4f86\u8a66\u8a66\u5427\u3002" },
                    { "howTo.undo.title", "\u5fa9\u539f\u9053\u5177" },
                    { "howTo.undo.body", "\u5fa9\u539f\u53ef\u4ee5\u64a4\u92b7\u4e0a\u4e00\u6b65\u6d88\u9664\u6216\u9053\u5177\u6548\u679c\u3002\u9ede\u64ca\u300c\u5fa9\u539f\u300d\u6309\u9215\uff0c\u628a\u525b\u525b\u6d88\u9664\u7684\u90a3\u4e00\u5c0d\u724c\u5e36\u56de\u4f86\u5427\u3002" },
                    { "howTo.shuffle.title", "\u6d17\u724c\u9053\u5177" },
                    { "howTo.shuffle.body", "\u6d17\u724c\u6703\u91cd\u65b0\u6253\u4e82\u5269\u4e0b\u7684\u724c\u9762\u3002\u9ede\u64ca\u300c\u6d17\u724c\u300d\u8a66\u8a66\u770b\u5427\u3002" },
                    { "howTo.swapButton.title", "\u4ea4\u63db\u9053\u5177" },
                    { "howTo.swapButton.body", "\u4ea4\u63db\u53ef\u4ee5\u8b93\u5169\u5f35\u724c\u5c0d\u8abf\u4f4d\u7f6e\u3002\u9ede\u64ca\u300c\u4ea4\u63db\u300d\u6309\u9215\uff0c\u7136\u5f8c\u9078\u64c7\u60f3\u8981\u4ea4\u63db\u7684\u5169\u5f35\u724c\u3002" },
                    { "howTo.swapTiles.title", "\u4ea4\u63db\u724c\u9762" },
                    { "howTo.swapTiles.body", "\u4ea4\u63db\u6307\u5b9a\u7684\u5169\u5f35\u9ebb\u5c07\u4ee5\u7e7c\u7e8c\u3002" },
                    { "howTo.complete.title", "\u6559\u5b78\u5b8c\u6210" },
                    { "howTo.complete.body", "\u4f60\u5df2\u7d93\u638c\u63e1\u904a\u6232\u7684\u6838\u5fc3\u73a9\u6cd5\u4e86\uff0c\u76e1\u60c5\u53bb\u958b\u9396\u5427~" },
                    { "settings.title", "\u8a2d\u5b9a" },
                    { "settings.hint", "\u63d0\u793a" },
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
                    { "gameMenu.noMoves", "\u5f53\u524d\u724c\u9762\u5df2\u8fdb\u5165\u6b7b\u9501 \u4f7f\u7528\u9053\u5177\u6216\u91cd\u65b0\u5f00\u59cb\u5427" },
                    { "gameMenu.winTitle", "\u8fc7\u5173\u4e86\uff01" },
                    { "gameMenu.match", "\u914d\u5bf9" },
                    { "gameMenu.cancel", "\u53d6\u6d88" },
                    { "howTo.step1.title", "\u6559\u7a0b 1" },
                    { "howTo.step1.body", "\u4e24\u5f20\u7d27\u6328\u7684\u76f8\u540c\u9ebb\u5c06\u724c\u53ef\u4ee5\u76f4\u63a5\u7ffb\u9762\u3002\u70b9\u51fb\u5176\u4e2d\u4e00\u5f20\u4ee5\u7ffb\u9762\u3002" },
                    { "howTo.step2.title", "\u6559\u7a0b 2" },
                    { "howTo.step2.body", "\u5f53\u4e24\u5f20\u76f8\u540c\u724c\u5728\u540c\u4e00\u884c\u6216\u540c\u4e00\u5217\uff0c\u4e14\u4e2d\u95f4\u90fd\u662f\u7ffb\u9762\u724c\u65f6\uff0c\u4f60\u53ef\u4ee5\u70b9\u51fb\u5176\u4e2d\u4e00\u5f20\u4ee5\u7ffb\u9762\u3002" },
                    { "howTo.step3.title", "\u6559\u7a0b 3" },
                    { "howTo.step3.body", "\u73b0\u5728\u8bd5\u8bd5\u62d6\u52a8\u79fb\u52a8\u3002\u672a\u914d\u5bf9\u7684\u9ebb\u5c06\u724c\u53ef\u4ee5\u5728\u7ffb\u9762\u9ebb\u5c06\u4e0a\u88ab\u62d6\u52a8\uff0c\u8ba9\u5b83\u914d\u5bf9\u4e0d\u5728\u540c\u4e00\u884c\u6216\u540c\u4e00\u5217\u7684\u9ebb\u5c07\u724c\u3002" },
                    { "howTo.step4.title", "\u6559\u7a0b 4" },
                    { "howTo.step4.body", "\u9664\u4e86\u76f4\u63a5\u62d6\u62fd\uff0c\u4f60\u4e5f\u53ef\u4ee5\u63a8\u52a8\u90bb\u8fd1\u7684\u9ebb\u5c06\u724c\u4ee5\u8fdb\u884c\u914d\u5bf9\u3002\u5feb\u8bd5\u8bd5\u5427\u3002" },
                    { "howTo.undo.title", "\u64a4\u9500\u9053\u5177" },
                    { "howTo.undo.body", "\u64a4\u9500\u4e0a\u4e00\u6b65\u7684\u7ffb\u9762\u6216\u9053\u5177\u6548\u679c\u3002\u70b9\u51fb\u300c\u64a4\u9500\u300d\u6309\u94ae\uff0c\u628a\u521a\u624d\u7684\u7ffb\u9762\u884c\u52a8\u64a4\u56de\u5427\u3002" },
                    { "howTo.shuffle.title", "\u6d17\u724c\u9053\u5177" },
                    { "howTo.shuffle.body", "\u6d17\u724c\u4f1a\u91cd\u65b0\u6253\u4e71\u5269\u4e0b\u7684\u724c\u9762\u3002\u70b9\u51fb\u300c\u6d17\u724c\u300d\u8bd5\u8bd5\u770b\u5427\u3002" },
                    { "howTo.swapButton.title", "\u6362\u4f4d\u9053\u5177" },
                    { "howTo.swapButton.body", "\u6362\u4f4d\u53ef\u4ee5\u8ba9\u4e24\u5f20\u724c\u5bf9\u8c03\u4f4d\u7f6e\u3002\u5148\u70b9\u51fb\u300c\u6362\u4f4d\u300d\u6309\u94ae\uff0c\u7136\u540e\u70b9\u51fb\u60f3\u8981\u4ea4\u6362\u7684\u4e24\u5f20\u9ebb\u5c06\u724c\u3002" },
                    { "howTo.swapTiles.title", "\u4ea4\u6362\u724c\u9762" },
                    { "howTo.swapTiles.body", "\u4f9d\u6b21\u70b9\u51fb\u4e24\u5f20\u9ebb\u5c06\u724c\u6765\u5bf9\u8c03\u4ed6\u4eec\u7684\u4f4d\u7f6e\u3002" },
                    { "howTo.complete.title", "\u6559\u5b66\u5b8c\u6210" },
                    { "howTo.complete.body", "\u4f60\u5df2\u7ecf\u5b66\u6709\u6240\u6210\u4e86\uff0c\u73b0\u5728\u8be5\u662f\u4f60\u53d1\u6325\u7684\u65f6\u5019\u4e86\uff0c\u5c3d\u60c5\u53bb\u5f00\u9501\u5427~" },
                    { "settings.title", "\u8bbe\u7f6e" },
                    { "settings.hint", "\u63d0\u793a" },
                    { "settings.themes", "\u4e3b\u9898" },
                    { "settings.languages", "\u8bed\u8a00" },
                    { "settings.languageTitle", "\u8bed\u8a00" },
                    { "settings.back", "\u8fd4\u56de" }
                }
            }
        };

    public static event Action OnLanguageChanged;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void RegisterSceneFontRefresh()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        SceneManager.sceneLoaded += HandleSceneLoaded;
        RefreshActiveTextFonts();
    }

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
        RefreshActiveTextFonts();
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

    public static void ApplyFont(TMP_Text text)
    {
        if (text == null)
        {
            return;
        }

        TMP_FontAsset fontAsset = GetFontForLanguage(CurrentLanguageCode);
        if (fontAsset == null)
        {
            return;
        }

        if (text.font != fontAsset)
        {
            text.font = fontAsset;
        }

        if (text.fontSharedMaterial != fontAsset.material)
        {
            text.fontSharedMaterial = fontAsset.material;
        }

        text.havePropertiesChanged = true;
    }

    public static void RefreshActiveTextFonts()
    {
        TMP_Text[] textObjects = UnityEngine.Object.FindObjectsByType<TMP_Text>(FindObjectsInactive.Include);
        for (int i = 0; i < textObjects.Length; i++)
        {
            ApplyFont(textObjects[i]);
        }
    }

    private static void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        RefreshActiveTextFonts();
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

    private static TMP_FontAsset GetFontForLanguage(string languageCode)
    {
        TMP_FontAsset defaultFont = TMP_Settings.defaultFontAsset;
        List<TMP_FontAsset> fallbackFonts = TMP_Settings.fallbackFontAssets;

        switch (languageCode)
        {
            case "zh-cn":
                if (fallbackFonts != null && fallbackFonts.Count > 0 && fallbackFonts[0] != null)
                {
                    return fallbackFonts[0];
                }
                break;
            case "zh-hk":
                if (fallbackFonts != null)
                {
                    if (fallbackFonts.Count > 1 && fallbackFonts[1] != null)
                    {
                        return fallbackFonts[1];
                    }

                    if (fallbackFonts.Count > 0 && fallbackFonts[0] != null)
                    {
                        return fallbackFonts[0];
                    }
                }
                break;
        }

        return defaultFont;
    }
}
