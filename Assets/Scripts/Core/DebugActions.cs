using UnityEngine;

public class DebugActions : MonoBehaviour
{
    [SerializeField] private DebugSettings debugSettings;
    [SerializeField] private BoardInteractionController boardInteractionController;
    [SerializeField] private GameObject menuOverlayRoot;
    [SerializeField] private GameObject debugBarRoot;

    private bool lastDebugMode;

    private void Start()
    {
        RefreshDebugUi();
    }

    private void Update()
    {
        bool currentDebugMode = IsDebugModeEnabled();

        if (currentDebugMode != lastDebugMode)
        {
            RefreshDebugUi();
        }
    }

    public void RefreshDebugUi()
    {
        lastDebugMode = IsDebugModeEnabled();

        if (debugBarRoot != null)
        {
            debugBarRoot.SetActive(lastDebugMode);
        }
    }

    public void OnClickDebugMatchMode()
    {
        if (!IsDebugModeEnabled())
        {
            Debug.Log("Debug match mode is disabled.");
            return;
        }

        if (menuOverlayRoot != null)
        {
            menuOverlayRoot.SetActive(false);
        }

        if (boardInteractionController != null)
        {
            boardInteractionController.BeginDebugMatchSelection();
        }
    }

    private bool IsDebugModeEnabled()
    {
        return debugSettings != null && debugSettings.DebugMode;
    }
}
