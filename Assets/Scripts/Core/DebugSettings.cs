using UnityEngine;

public class DebugSettings : MonoBehaviour
{
    private const string DebugModePlayerPrefsKey = "debug.mode.enabled";

    [SerializeField] private bool debugMode;

    public bool DebugMode => debugMode || IsPersistentDebugModeEnabled();

    public static bool IsPersistentDebugModeEnabled()
    {
        return PlayerPrefs.GetInt(DebugModePlayerPrefsKey, 0) == 1;
    }

    public static void SetPersistentDebugMode(bool enabled)
    {
        PlayerPrefs.SetInt(DebugModePlayerPrefsKey, enabled ? 1 : 0);
        PlayerPrefs.Save();
    }
}
