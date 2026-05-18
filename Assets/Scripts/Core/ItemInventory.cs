using System;
using UnityEngine;

public class ItemInventory : MonoBehaviour
{
    private const int DebugModeItemCount = 99;
    private const int CurrentSaveDataVersion = 1;

    [Header("References")]
    [SerializeField] private DebugSettings debugSettings;

    private int undoUses;
    private int shuffleUses;
    private int swapUses;
    private int playerUndoUses;
    private int playerShuffleUses;
    private int playerSwapUses;
    private int coins;
    private int gems;
    private bool lastRequestedDebugMode;
    private bool suppressDebugMode;
    private bool hasPendingSaveRequest;

    public event Action OnInventoryChanged;
    public event Action OnInventorySaveRequested;

    [Serializable]
    public sealed class SaveData
    {
        public int DataVersion;
        public int UndoUses;
        public int ShuffleUses;
        public int SwapUses;
        public int Coins;
        public int Gems;
    }

    private void Awake()
    {
        lastRequestedDebugMode = IsDebugModeRequested();
        SyncDebugInventoryState(notifyListeners: false);
    }

    private void Update()
    {
        bool debugModeRequested = IsDebugModeRequested();
        if (debugModeRequested == lastRequestedDebugMode)
        {
            return;
        }

        lastRequestedDebugMode = debugModeRequested;
        SyncDebugInventoryState(notifyListeners: true);
    }

    public void ResetForFreshBoard()
    {
        SyncDebugInventoryState(notifyListeners: true);
    }

    public bool HasUndoUseAvailable()
    {
        return undoUses > 0;
    }

    public bool HasShuffleUseAvailable()
    {
        return shuffleUses > 0;
    }

    public bool HasSwapUseAvailable()
    {
        return swapUses > 0;
    }

    public bool IsDebugModeEnabled()
    {
        return IsDebugModeRequested();
    }

    public void SetRuntimeCounts(int undoCount, int shuffleCount, int swapCount, bool ignoreDebugMode = false)
    {
        suppressDebugMode = ignoreDebugMode;
        SetPlayerInventoryCounts(undoCount, shuffleCount, swapCount);
        lastRequestedDebugMode = IsDebugModeRequested();
        SyncDebugInventoryState(notifyListeners: true);
    }

    public void ClearRuntimeDebugOverride()
    {
        suppressDebugMode = false;
        lastRequestedDebugMode = IsDebugModeRequested();
        SyncDebugInventoryState(notifyListeners: true);
    }

    public void ConsumeUndoUse()
    {
        if (!TryConsumeUse(ref undoUses, ref playerUndoUses))
        {
            return;
        }

        NotifyInventoryChanged(requestSave: true);
    }

    public void AddUndoUses(int amount)
    {
        if (amount <= 0)
        {
            return;
        }

        playerUndoUses += amount;
        SyncDebugInventoryState(notifyListeners: true);
        RequestSave();
    }

    public void AddShuffleUses(int amount)
    {
        if (amount <= 0)
        {
            return;
        }

        playerShuffleUses += amount;
        SyncDebugInventoryState(notifyListeners: true);
        RequestSave();
    }

    public void AddSwapUses(int amount)
    {
        if (amount <= 0)
        {
            return;
        }

        playerSwapUses += amount;
        SyncDebugInventoryState(notifyListeners: true);
        RequestSave();
    }

    public void ConsumeShuffleUse()
    {
        if (!TryConsumeUse(ref shuffleUses, ref playerShuffleUses))
        {
            return;
        }

        NotifyInventoryChanged(requestSave: true);
    }

    public void ConsumeSwapUse()
    {
        if (!TryConsumeUse(ref swapUses, ref playerSwapUses))
        {
            return;
        }

        NotifyInventoryChanged(requestSave: true);
    }

    public string GetUndoDisplayText()
    {
        return GetDisplayText(undoUses);
    }

    public string GetShuffleDisplayText()
    {
        return GetDisplayText(shuffleUses);
    }

    public string GetSwapDisplayText()
    {
        return GetDisplayText(swapUses);
    }

    public SaveData CaptureSaveData()
    {
        return new SaveData
        {
            DataVersion = CurrentSaveDataVersion,
            UndoUses = playerUndoUses,
            ShuffleUses = playerShuffleUses,
            SwapUses = playerSwapUses,
            Coins = coins,
            Gems = gems
        };
    }

    public void RestoreFromSaveData(SaveData saveData)
    {
        if (saveData == null)
        {
            SetPlayerInventoryCounts(0, 0, 0);
            coins = 0;
            gems = 0;
            lastRequestedDebugMode = IsDebugModeRequested();
            SyncDebugInventoryState(notifyListeners: true);
            RequestSave();
            return;
        }

        SetPlayerInventoryCounts(saveData.UndoUses, saveData.ShuffleUses, saveData.SwapUses);
        coins = Mathf.Max(0, saveData.Coins);
        gems = Mathf.Max(0, saveData.Gems);
        lastRequestedDebugMode = IsDebugModeRequested();
        SyncDebugInventoryState(notifyListeners: true);

        if (saveData.DataVersion < CurrentSaveDataVersion)
        {
            RequestSave();
        }
    }

    public bool ConsumePendingSaveRequest()
    {
        bool shouldSave = hasPendingSaveRequest;
        hasPendingSaveRequest = false;
        return shouldSave;
    }

    private string GetDisplayText(int uses)
    {
        return uses.ToString();
    }

    private bool IsDebugModeRequested()
    {
        return !suppressDebugMode && debugSettings != null && debugSettings.DebugMode;
    }

    private void SetPlayerInventoryCounts(int undoCount, int shuffleCount, int swapCount)
    {
        playerUndoUses = Mathf.Max(0, undoCount);
        playerShuffleUses = Mathf.Max(0, shuffleCount);
        playerSwapUses = Mathf.Max(0, swapCount);
    }

    private bool TryConsumeUse(ref int runtimeCount, ref int playerCount)
    {
        if (runtimeCount <= 0)
        {
            return false;
        }

        runtimeCount--;

        if (!IsDebugModeRequested())
        {
            playerCount = Mathf.Max(0, playerCount - 1);
        }

        return true;
    }

    private void SyncDebugInventoryState(bool notifyListeners)
    {
        bool shouldUseDebugInventory = IsDebugModeRequested();

        if (shouldUseDebugInventory)
        {
            undoUses = DebugModeItemCount;
            shuffleUses = DebugModeItemCount;
            swapUses = DebugModeItemCount;
        }
        else
        {
            undoUses = playerUndoUses;
            shuffleUses = playerShuffleUses;
            swapUses = playerSwapUses;
        }

        if (notifyListeners)
        {
            NotifyInventoryChanged(requestSave: false);
        }
    }

    private void NotifyInventoryChanged(bool requestSave)
    {
        OnInventoryChanged?.Invoke();

        if (requestSave)
        {
            RequestSave();
        }
    }

    private void RequestSave()
    {
        hasPendingSaveRequest = true;
        OnInventorySaveRequested?.Invoke();
    }

}
