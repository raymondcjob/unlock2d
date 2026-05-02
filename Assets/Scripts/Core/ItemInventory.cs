using System;
using UnityEngine;

public class ItemInventory : MonoBehaviour
{
    private const int DebugModeItemCount = 99;

    [Header("References")]
    [SerializeField] private DebugSettings debugSettings;

    [Header("Starting Counts")]
    [SerializeField] private int startingUndoCount;
    [SerializeField] private int startingShuffleCount;
    [SerializeField] private int startingSwapCount;

    private int undoUses;
    private int shuffleUses;
    private int swapUses;
    private int playerUndoUses;
    private int playerShuffleUses;
    private int playerSwapUses;
    private bool lastRequestedDebugMode;
    private bool suppressDebugMode;

    public event Action OnInventoryChanged;

    [Serializable]
    public sealed class SaveData
    {
        public int UndoUses;
        public int ShuffleUses;
        public int SwapUses;
    }

    private void Awake()
    {
        ResetForFreshBoard();
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
        SetPlayerInventoryCounts(startingUndoCount, startingShuffleCount, startingSwapCount);
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
        if (undoUses <= 0)
        {
            return;
        }

        undoUses--;
        OnInventoryChanged?.Invoke();
    }

    public void AddUndoUses(int amount)
    {
        if (amount <= 0)
        {
            return;
        }

        playerUndoUses += amount;
        SyncDebugInventoryState(notifyListeners: true);
    }

    public void AddShuffleUses(int amount)
    {
        if (amount <= 0)
        {
            return;
        }

        playerShuffleUses += amount;
        SyncDebugInventoryState(notifyListeners: true);
    }

    public void AddSwapUses(int amount)
    {
        if (amount <= 0)
        {
            return;
        }

        playerSwapUses += amount;
        SyncDebugInventoryState(notifyListeners: true);
    }

    public void ConsumeShuffleUse()
    {
        if (shuffleUses <= 0)
        {
            return;
        }

        shuffleUses--;
        OnInventoryChanged?.Invoke();
    }

    public void ConsumeSwapUse()
    {
        if (swapUses <= 0)
        {
            return;
        }

        swapUses--;
        OnInventoryChanged?.Invoke();
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
            UndoUses = playerUndoUses,
            ShuffleUses = playerShuffleUses,
            SwapUses = playerSwapUses
        };
    }

    public void RestoreFromSaveData(SaveData saveData)
    {
        if (saveData == null)
        {
            return;
        }

        SetPlayerInventoryCounts(saveData.UndoUses, saveData.ShuffleUses, saveData.SwapUses);
        lastRequestedDebugMode = IsDebugModeRequested();
        SyncDebugInventoryState(notifyListeners: true);
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
            OnInventoryChanged?.Invoke();
        }
    }

}
