using System;
using System.IO;
using UnityEngine;

public class SaveGameManager : MonoBehaviour
{
    private const int SaveVersion = 1;
    private const string SaveFileName = "savegame.json";

    private static bool shouldLoadSavedGameOnNextGameScene;
    private static bool suppressBoardAutoGenerateThisScene;

    [Header("References")]
    [SerializeField] private BoardManager boardManager;
    [SerializeField] private ItemInventory itemInventory;

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

    private static bool ConsumeLoadSavedGameRequest()
    {
        bool shouldLoad = shouldLoadSavedGameOnNextGameScene;
        shouldLoadSavedGameOnNextGameScene = false;
        return shouldLoad;
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
        if (ConsumeLoadSavedGameRequest() && !TryLoadGame())
        {
            boardManager?.GenerateNewBoard();
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

        SaveGameData saveData = new SaveGameData
        {
            Version = SaveVersion,
            SavedAtUtc = DateTime.UtcNow.ToString("o"),
            Board = boardManager.CaptureSaveData(),
            Inventory = itemInventory.CaptureSaveData()
        };

        string json = JsonUtility.ToJson(saveData, true);
        File.WriteAllText(SavePath, json);
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
            hasBoardProgress = true;
            isLoadingGame = false;
            return true;
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
}
