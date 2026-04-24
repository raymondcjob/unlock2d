using System;
using UnityEngine;

public class ItemInventory : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private DebugSettings debugSettings;

    [Header("Starting Counts")]
    [SerializeField] private int startingUndoCount;
    [SerializeField] private int startingShuffleCount;
    [SerializeField] private int startingSwapCount;

    private int undoUses;
    private int shuffleUses;
    private int swapUses;
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
    }

    public void ResetForFreshBoard()
    {
        undoUses = Mathf.Max(0, startingUndoCount);
        shuffleUses = Mathf.Max(0, startingShuffleCount);
        swapUses = Mathf.Max(0, startingSwapCount);
        OnInventoryChanged?.Invoke();
    }

    public bool HasUndoUseAvailable()
    {
        return IsDebugModeEnabled() || undoUses > 0;
    }

    public bool HasShuffleUseAvailable()
    {
        return IsDebugModeEnabled() || shuffleUses > 0;
    }

    public bool HasSwapUseAvailable()
    {
        return IsDebugModeEnabled() || swapUses > 0;
    }

    public bool IsDebugModeEnabled()
    {
        return !suppressDebugMode && debugSettings != null && debugSettings.DebugMode;
    }

    public void SetRuntimeCounts(int undoCount, int shuffleCount, int swapCount, bool ignoreDebugMode = false)
    {
        undoUses = Mathf.Max(0, undoCount);
        shuffleUses = Mathf.Max(0, shuffleCount);
        swapUses = Mathf.Max(0, swapCount);
        suppressDebugMode = ignoreDebugMode;
        OnInventoryChanged?.Invoke();
    }

    public void ClearRuntimeDebugOverride()
    {
        suppressDebugMode = false;
        OnInventoryChanged?.Invoke();
    }

    public void ConsumeUndoUse()
    {
        if (!IsDebugModeEnabled() && undoUses > 0)
        {
            undoUses--;
            OnInventoryChanged?.Invoke();
        }
    }

    public void AddUndoUses(int amount)
    {
        if (amount <= 0)
        {
            return;
        }

        undoUses += amount;
        OnInventoryChanged?.Invoke();
    }

    public void AddShuffleUses(int amount)
    {
        if (amount <= 0)
        {
            return;
        }

        shuffleUses += amount;
        OnInventoryChanged?.Invoke();
    }

    public void AddSwapUses(int amount)
    {
        if (amount <= 0)
        {
            return;
        }

        swapUses += amount;
        OnInventoryChanged?.Invoke();
    }

    public void ConsumeShuffleUse()
    {
        if (!IsDebugModeEnabled() && shuffleUses > 0)
        {
            shuffleUses--;
            OnInventoryChanged?.Invoke();
        }
    }

    public void ConsumeSwapUse()
    {
        if (!IsDebugModeEnabled() && swapUses > 0)
        {
            swapUses--;
            OnInventoryChanged?.Invoke();
        }
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
            UndoUses = undoUses,
            ShuffleUses = shuffleUses,
            SwapUses = swapUses
        };
    }

    public void RestoreFromSaveData(SaveData saveData)
    {
        if (saveData == null)
        {
            return;
        }

        undoUses = Mathf.Max(0, saveData.UndoUses);
        shuffleUses = Mathf.Max(0, saveData.ShuffleUses);
        swapUses = Mathf.Max(0, saveData.SwapUses);
        OnInventoryChanged?.Invoke();
    }

    private string GetDisplayText(int uses)
    {
        return IsDebugModeEnabled() ? "/" : uses.ToString();
    }

}
