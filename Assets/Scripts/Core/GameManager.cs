using System;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    private const int SaveVersion = 1;
    private const string SaveFileName = "savegame.json";

    private static bool shouldLoadSavedGameOnNextGameScene;
    private static bool shouldRestorePendingReturnGameOnNextGameScene;
    private static bool suppressBoardAutoGenerateThisScene;
    private static SaveGameData pendingReturnGameData;

    [Header("References")]
    [SerializeField] private BoardManager boardManager;
    [SerializeField] private ItemInventory itemInventory;
    [SerializeField] private UIManager uiManager;

    [Header("Scene Names")]
    [SerializeField] private string mainMenuSceneName = "MainMenu";
    [SerializeField] private string settingsSceneName = "Settings";

    [Header("Autosave")]
    [SerializeField] private bool autosaveEnabled = true;

    private bool isLoadingGame;
    private bool hasBoardProgress;
    private bool ignoreNextStableBoardStateChanged;

    [Serializable]
    private sealed class SaveGameData
    {
        public int Version;
        public string SavedAtUtc;
        public BoardManager.SaveData Board;
        public ItemInventory.SaveData Inventory;
        public float ElapsedSeconds;
        public bool IsTimerRunning;
    }

    private string SavePath => Path.Combine(Application.persistentDataPath, SaveFileName);

    public static bool HasSavedGameFile()
    {
        return File.Exists(GetSavePath());
    }

    public static bool ShouldSkipBoardAutoGenerateOnSceneStart()
    {
        return suppressBoardAutoGenerateThisScene;
    }

    public static void RequestLoadSavedGameOnNextGameScene()
    {
        shouldLoadSavedGameOnNextGameScene = true;
        suppressBoardAutoGenerateThisScene = true;
    }

    public static void RequestRestorePendingReturnGameOnNextGameScene()
    {
        if (pendingReturnGameData == null)
        {
            return;
        }

        shouldRestorePendingReturnGameOnNextGameScene = true;
        suppressBoardAutoGenerateThisScene = true;
    }

    public void InitializeIfNeeded(BoardManager board, ItemInventory inventory, UIManager ui, string mainMenuScene, string settingsScene)
    {
        if (boardManager == null)
        {
            boardManager = board;
        }

        if (itemInventory == null)
        {
            itemInventory = inventory;
        }

        if (uiManager == null)
        {
            uiManager = ui;
        }

        if (!string.IsNullOrEmpty(mainMenuScene))
        {
            mainMenuSceneName = mainMenuScene;
        }

        if (!string.IsNullOrEmpty(settingsScene))
        {
            settingsSceneName = settingsScene;
        }
    }

    public bool HasSavedGame()
    {
        return HasSavedGameFile();
    }

    public void SaveGame()
    {
        if (boardManager == null || itemInventory == null)
        {
            Debug.LogWarning("SaveGame failed: missing BoardManager or ItemInventory reference.");
            return;
        }

        SaveGameData saveData = CreateSaveGameData();
        string json = JsonUtility.ToJson(saveData, true);
        File.WriteAllText(SavePath, json);
    }

    public void SaveGameIfAllowed()
    {
        Autosave();
    }

    public void StashCurrentGameForSceneReturn()
    {
        if (boardManager == null || itemInventory == null)
        {
            Debug.LogWarning("StashCurrentGameForSceneReturn failed: missing BoardManager or ItemInventory reference.");
            return;
        }

        pendingReturnGameData = CreateSaveGameData();
    }

    public bool TryLoadGame()
    {
        if (!HasSavedGame())
        {
            return false;
        }

        try
        {
            string json = File.ReadAllText(SavePath);
            SaveGameData saveData = JsonUtility.FromJson<SaveGameData>(json);
            return TryLoadGameData(saveData);
        }
        catch (Exception ex)
        {
            isLoadingGame = false;
            Debug.LogWarning($"Load game failed: {ex.Message}");
            return false;
        }
    }

    public void DeleteSave()
    {
        if (HasSavedGame())
        {
            File.Delete(SavePath);
        }
    }

    public void ReturnToMainMenu()
    {
        SaveGameIfAllowed();
        uiManager?.PrepareForSceneTransition();
        SceneManager.LoadScene(mainMenuSceneName);
    }

    public void StartNewBoard()
    {
        uiManager?.PrepareForBoardReset();
        boardManager?.GenerateNewBoard();
    }

    public void RestartBoard()
    {
        uiManager?.PrepareForBoardReset();
        boardManager?.ReplayCurrentBoard();
    }

    public void OpenStore()
    {
        SaveGameIfAllowed();
        Debug.Log("Store button clicked.");
    }

    public void OpenSettingsScene()
    {
        StashCurrentGameForSceneReturn();
        SaveGameIfAllowed();
        uiManager?.PrepareForSceneTransition();
        SettingsMenuManager.SetReturnScene(SceneManager.GetActiveScene().name);
        SceneManager.LoadScene(settingsSceneName);
    }

    private static bool ConsumeLoadSavedGameRequest()
    {
        bool shouldLoad = shouldLoadSavedGameOnNextGameScene;
        shouldLoadSavedGameOnNextGameScene = false;
        return shouldLoad;
    }

    private static bool ConsumeRestorePendingReturnGameRequest()
    {
        bool shouldRestore = shouldRestorePendingReturnGameOnNextGameScene;
        shouldRestorePendingReturnGameOnNextGameScene = false;
        return shouldRestore;
    }

    private static string GetSavePath()
    {
        return Path.Combine(Application.persistentDataPath, SaveFileName);
    }

    private void OnEnable()
    {
        if (boardManager != null)
        {
            boardManager.OnStableBoardStateChanged += HandleStableBoardStateChanged;
            boardManager.OnBoardWon += HandleBoardWon;
            boardManager.OnBoardGenerated += HandleBoardGenerated;
            boardManager.OnNewRandomBoardGenerated += HandleNewRandomBoardGenerated;
            boardManager.OnBoardRestarted += HandleBoardRestarted;
            boardManager.OnSwapPerformed += HandleSwapPerformed;
        }

        if (itemInventory != null)
        {
            itemInventory.OnInventoryChanged += HandleInventoryChanged;
        }
    }

    private void OnDisable()
    {
        suppressBoardAutoGenerateThisScene = false;

        if (boardManager != null)
        {
            boardManager.OnStableBoardStateChanged -= HandleStableBoardStateChanged;
            boardManager.OnBoardWon -= HandleBoardWon;
            boardManager.OnBoardGenerated -= HandleBoardGenerated;
            boardManager.OnNewRandomBoardGenerated -= HandleNewRandomBoardGenerated;
            boardManager.OnBoardRestarted -= HandleBoardRestarted;
            boardManager.OnSwapPerformed -= HandleSwapPerformed;
        }

        if (itemInventory != null)
        {
            itemInventory.OnInventoryChanged -= HandleInventoryChanged;
        }
    }

    private void Start()
    {
        if (ConsumeRestorePendingReturnGameRequest())
        {
            if (TryLoadGameData(pendingReturnGameData))
            {
                pendingReturnGameData = null;
                return;
            }

            pendingReturnGameData = null;
            boardManager?.GenerateNewBoard();
            return;
        }

        if (ConsumeLoadSavedGameRequest() && !TryLoadGame())
        {
            boardManager?.GenerateNewBoard();
        }
    }

    private void HandleBoardGenerated()
    {
        hasBoardProgress = false;
        ignoreNextStableBoardStateChanged = true;
    }

    private void HandleNewRandomBoardGenerated()
    {
        hasBoardProgress = false;
        DeleteSave();
    }

    private void HandleBoardRestarted()
    {
        hasBoardProgress = true;
        Autosave();
    }

    private void HandleStableBoardStateChanged()
    {
        if (ignoreNextStableBoardStateChanged)
        {
            ignoreNextStableBoardStateChanged = false;
            return;
        }

        hasBoardProgress = true;
        Autosave();
    }

    private void HandleSwapPerformed()
    {
        hasBoardProgress = true;
        Autosave();
    }

    private void HandleInventoryChanged()
    {
        Autosave();
    }

    private void HandleBoardWon(int seed)
    {
        DeleteSave();
    }

    private void Autosave()
    {
        if (!autosaveEnabled || isLoadingGame || !hasBoardProgress)
        {
            return;
        }

        if (boardManager != null && boardManager.GetRemainingFaceUpTiles() <= 0)
        {
            DeleteSave();
            return;
        }

        SaveGame();
    }

    private SaveGameData CreateSaveGameData()
    {
        return new SaveGameData
        {
            Version = SaveVersion,
            SavedAtUtc = DateTime.UtcNow.ToString("o"),
            Board = boardManager.CaptureSaveData(),
            Inventory = itemInventory.CaptureSaveData(),
            ElapsedSeconds = uiManager != null ? uiManager.GetElapsedSeconds() : 0f,
            IsTimerRunning = uiManager != null && uiManager.IsTimerRunning()
        };
    }

    private bool TryLoadGameData(SaveGameData saveData)
    {
        if (saveData == null || saveData.Version != SaveVersion)
        {
            Debug.LogWarning("Load game failed: save data is missing or uses an unsupported version.");
            return false;
        }

        if (boardManager == null || itemInventory == null)
        {
            Debug.LogWarning("Load game failed: missing BoardManager or ItemInventory reference.");
            return false;
        }

        isLoadingGame = true;

        if (!boardManager.RestoreFromSaveData(saveData.Board))
        {
            isLoadingGame = false;
            return false;
        }

        itemInventory.RestoreFromSaveData(saveData.Inventory);
        uiManager?.RestoreTimerState(saveData.ElapsedSeconds, saveData.IsTimerRunning);
        hasBoardProgress = true;
        isLoadingGame = false;
        return true;
    }
}
