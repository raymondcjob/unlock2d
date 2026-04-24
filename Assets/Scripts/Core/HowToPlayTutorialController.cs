using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class HowToPlayTutorialController : MonoBehaviour
{
    private const string HowToPlaySceneName = "HowToPlay";
    private const int TutorialBoardWidth = 12;
    private const int TutorialBoardHeight = 6;
    private const float TileHighlightScale = 1.1f;
    private const float ButtonHighlightScale = 1.15f;

    private enum TutorialStep
    {
        Step1AdjacentMatch,
        Step2StraightMatch,
        Step3DragSingle,
        Step4DragPush,
        IntroUndo,
        IntroShuffle,
        IntroSwapButton,
        IntroSwapTiles,
        Complete
    }

    private readonly List<TileView> highlightedTiles = new List<TileView>();
    private readonly List<RectTransform> highlightedButtons = new List<RectTransform>();
    private readonly Dictionary<RectTransform, Vector3> originalButtonScales = new Dictionary<RectTransform, Vector3>();

    private BoardManager boardManager;
    private UIManager uiManager;
    private BoardInteractionController boardInteractionController;
    private ItemInventory itemInventory;
    private GameObject noMatchesOverlay;
    private GameObject winOverlay;

    private Button undoButton;
    private Button shuffleButton;
    private Button swapButton;
    private Button storeButton;

    private Canvas overlayCanvas;
    private Image dimmerImage;
    private Button tapAnywhereButton;
    private GameObject popupPanel;
    private TMP_Text stepTitleText;
    private TMP_Text bodyText;
    private TileView currentTargetTileA;
    private TileView currentTargetTileB;

    private TutorialStep currentStep;
    private bool isInitialized;

    private void Awake()
    {
        if (SceneManager.GetActiveScene().name != HowToPlaySceneName)
        {
            Destroy(gameObject);
            return;
        }

        boardManager = FindAnyObjectByType<BoardManager>();
        uiManager = FindAnyObjectByType<UIManager>();
        boardInteractionController = FindAnyObjectByType<BoardInteractionController>();
        itemInventory = FindAnyObjectByType<ItemInventory>();

        noMatchesOverlay = GameObject.Find("NoMatchesOverlay");
        winOverlay = GameObject.Find("WinOverlay");
        undoButton = FindButton("Undo");
        shuffleButton = FindButton("Shuffle");
        swapButton = FindButton("Swap");
        storeButton = FindButton("StoreButton");

        if (uiManager != null)
        {
            uiManager.ShuffleOverride = HandleShuffleOverride;
            uiManager.UndoApplied += HandleUndoApplied;
            uiManager.SwapModeStarted += HandleSwapModeStarted;
        }

        if (boardManager != null)
        {
            boardManager.OnTilesMatched += HandleTilesMatched;
            boardManager.OnSwapPerformed += HandleSwapPerformed;
        }

        if (boardInteractionController != null)
        {
            boardInteractionController.InteractionVisualsCleared += HandleInteractionVisualsCleared;
        }

        BuildOverlayUi();
    }

    private void Start()
    {
        if (boardManager == null || uiManager == null || boardInteractionController == null || itemInventory == null)
        {
            Debug.LogWarning("How To Play tutorial could not start because one or more gameplay references are missing.");
            return;
        }

        itemInventory.SetRuntimeCounts(0, 0, 0, ignoreDebugMode: true);

        boardInteractionController.SetAutoHintEnabled(false);
        boardInteractionController.SetSuppressSameTypeEnlargement(false);
        boardInteractionController.SetTutorialEnlargementFilter(null);
        SetStoreButtonAvailable(false);

        LoadBoard(CreateInitialBoardData());
        isInitialized = true;
        StartStep1();
    }

    private void Update()
    {
        if (SceneManager.GetActiveScene().name != HowToPlaySceneName)
        {
            Destroy(gameObject);
            return;
        }

        if (!isInitialized)
        {
            return;
        }

        if (noMatchesOverlay != null && noMatchesOverlay.activeSelf)
        {
            noMatchesOverlay.SetActive(false);
        }

        if (winOverlay != null && winOverlay.activeSelf)
        {
            winOverlay.SetActive(false);
        }
    }

    private void OnDestroy()
    {
        if (boardInteractionController != null)
        {
            boardInteractionController.SetSuppressSameTypeEnlargement(false);
            boardInteractionController.SetTutorialEnlargementFilter(null);
            boardInteractionController.InteractionVisualsCleared -= HandleInteractionVisualsCleared;
        }

        if (uiManager != null)
        {
            if (uiManager.ShuffleOverride == HandleShuffleOverride)
            {
                uiManager.ShuffleOverride = null;
            }

            uiManager.UndoApplied -= HandleUndoApplied;
            uiManager.SwapModeStarted -= HandleSwapModeStarted;
        }

        if (boardManager != null)
        {
            boardManager.OnTilesMatched -= HandleTilesMatched;
            boardManager.OnSwapPerformed -= HandleSwapPerformed;
        }

        if (itemInventory != null)
        {
            itemInventory.ClearRuntimeDebugOverride();
        }
    }

    private void StartStep1()
    {
        currentStep = TutorialStep.Step1AdjacentMatch;
        boardInteractionController.ForceClearInteractionState();
        boardInteractionController.SetTutorialInteractionFilter(tile => MatchesAnyPosition(tile, new Vector2Int(8, 1), new Vector2Int(9, 1)));
        boardInteractionController.SetTutorialSelectionFilter(null);
        SetActionButtonsAvailable(false, false, false);
        SetTrackedTargetTiles(new Vector2Int(8, 1), new Vector2Int(9, 1));
        boardInteractionController.SetTutorialEnlargementFilter(_ => true);

        HighlightTiles(new Vector2Int(8, 1), new Vector2Int(9, 1));
        ShowLocalizedMessage("howTo.step1.title", "howTo.step1.body", waitsForTapAnywhere: false, null);
    }

    private void StartStep2()
    {
        currentStep = TutorialStep.Step2StraightMatch;
        boardInteractionController.ForceClearInteractionState();
        boardInteractionController.SetTutorialInteractionFilter(tile => MatchesAnyPosition(tile, new Vector2Int(7, 1), new Vector2Int(10, 1)));
        boardInteractionController.SetTutorialSelectionFilter(null);
        SetTrackedTargetTiles(new Vector2Int(7, 1), new Vector2Int(10, 1));
        boardInteractionController.SetTutorialEnlargementFilter(_ => true);
        HighlightTiles(new Vector2Int(7, 1), new Vector2Int(10, 1));
        ShowLocalizedMessage("howTo.step2.title", "howTo.step2.body", waitsForTapAnywhere: false, null);
    }

    private void StartStep3()
    {
        currentStep = TutorialStep.Step3DragSingle;
        boardInteractionController.ForceClearInteractionState();
        boardInteractionController.SetTutorialInteractionFilter(tile => MatchesAnyPosition(tile, new Vector2Int(6, 1), new Vector2Int(9, 2)));
        boardInteractionController.SetTutorialSelectionFilter(null);
        SetActionButtonsAvailable(false, false, false);
        SetTrackedTargetTiles(new Vector2Int(6, 1), new Vector2Int(9, 2));
        boardInteractionController.SetTutorialEnlargementFilter(_ => true);
        HighlightTiles(new Vector2Int(6, 1), new Vector2Int(9, 2));
        ShowLocalizedMessage("howTo.step3.title", "howTo.step3.body", waitsForTapAnywhere: false, null);
    }

    private void StartStep4()
    {
        currentStep = TutorialStep.Step4DragPush;
        boardInteractionController.ForceClearInteractionState();
        boardInteractionController.SetTutorialInteractionFilter(tile => MatchesAnyPosition(tile, new Vector2Int(1, 1), new Vector2Int(4, 2)));
        boardInteractionController.SetTutorialSelectionFilter(null);
        SetActionButtonsAvailable(false, false, false);
        SetTrackedTargetTiles(new Vector2Int(1, 1), new Vector2Int(4, 2));
        boardInteractionController.SetTutorialEnlargementFilter(_ => true);
        HighlightTiles(new Vector2Int(1, 1), new Vector2Int(4, 2));
        ShowLocalizedMessage("howTo.step4.title", "howTo.step4.body", waitsForTapAnywhere: false, null);
    }

    private void StartUndoIntro()
    {
        currentStep = TutorialStep.IntroUndo;
        boardInteractionController.ForceClearInteractionState();
        boardInteractionController.SetTutorialInteractionFilter(_ => false);
        boardInteractionController.SetTutorialSelectionFilter(null);
        boardInteractionController.SetTutorialEnlargementFilter(null);
        SetActionButtonsAvailable(true, false, false);
        HighlightButton(undoButton);
        ShowLocalizedMessage("howTo.undo.title", "howTo.undo.body", waitsForTapAnywhere: false, null);
    }

    private void StartShuffleIntro()
    {
        currentStep = TutorialStep.IntroShuffle;
        boardInteractionController.ForceClearInteractionState();
        boardInteractionController.SetTutorialInteractionFilter(_ => false);
        boardInteractionController.SetTutorialSelectionFilter(null);
        boardInteractionController.SetTutorialEnlargementFilter(null);
        SetActionButtonsAvailable(false, true, false);
        HighlightButton(shuffleButton);
        ShowLocalizedMessage("howTo.shuffle.title", "howTo.shuffle.body", waitsForTapAnywhere: false, null);
    }

    private void StartSwapButtonIntro()
    {
        currentStep = TutorialStep.IntroSwapButton;
        boardInteractionController.ForceClearInteractionState();
        boardInteractionController.SetTutorialInteractionFilter(_ => false);
        boardInteractionController.SetTutorialSelectionFilter(null);
        boardInteractionController.SetTutorialEnlargementFilter(null);
        SetActionButtonsAvailable(false, false, true);
        HighlightButton(swapButton);
        ShowLocalizedMessage("howTo.swapButton.title", "howTo.swapButton.body", waitsForTapAnywhere: false, null);
    }

    private void StartSwapTileSelection()
    {
        currentStep = TutorialStep.IntroSwapTiles;
        boardInteractionController.SetTutorialInteractionFilter(_ => false);
        boardInteractionController.SetTutorialSelectionFilter(tile => MatchesAnyPosition(tile, new Vector2Int(2, 1), new Vector2Int(4, 1)));
        boardInteractionController.SetTutorialEnlargementFilter(null);
        SetActionButtonsAvailable(false, false, false);
        HighlightTiles(new Vector2Int(2, 1), new Vector2Int(4, 1));
        ShowLocalizedMessage("howTo.swapTiles.title", "howTo.swapTiles.body", waitsForTapAnywhere: false, null);
    }

    private void StartCompletion()
    {
        currentStep = TutorialStep.Complete;
        boardInteractionController.ForceClearInteractionState();
        ClearHighlights();
        boardInteractionController.SetTutorialInteractionFilter(_ => false);
        boardInteractionController.SetTutorialSelectionFilter(null);
        boardInteractionController.SetTutorialEnlargementFilter(null);
        SetActionButtonsAvailable(true, true, true);
        ShowLocalizedMessage("howTo.complete.title", "howTo.complete.body", waitsForTapAnywhere: true, ReturnToMainMenu);
    }

    private void HandleTilesMatched(TileView tileA, TileView tileB)
    {
        if (!isInitialized)
        {
            return;
        }

        if (currentStep == TutorialStep.Step1AdjacentMatch &&
            MatchesTrackedTiles(tileA, tileB))
        {
            StartStep2();
            return;
        }

        if (currentStep == TutorialStep.Step2StraightMatch &&
            MatchesTrackedTiles(tileA, tileB))
        {
            StartStep3();
            return;
        }

        if (currentStep == TutorialStep.Step3DragSingle &&
            MatchesTrackedTiles(tileA, tileB))
        {
            StartStep4();
            return;
        }

        if (currentStep == TutorialStep.Step4DragPush &&
            MatchesTrackedTiles(tileA, tileB))
        {
            itemInventory.AddUndoUses(1);
            StartUndoIntro();
        }
    }

    private void HandleUndoApplied()
    {
        if (currentStep != TutorialStep.IntroUndo)
        {
            return;
        }

        itemInventory.ConsumeUndoUse();
        itemInventory.AddShuffleUses(1);
        StartShuffleIntro();
    }

    private bool HandleShuffleOverride()
    {
        if (currentStep != TutorialStep.IntroShuffle)
        {
            return false;
        }

        itemInventory.ConsumeShuffleUse();
        itemInventory.AddSwapUses(1);
        LoadBoard(CreateSwapBoardData());
        StartSwapButtonIntro();
        return true;
    }

    private void HandleSwapModeStarted()
    {
        if (currentStep != TutorialStep.IntroSwapButton)
        {
            return;
        }

        StartSwapTileSelection();
    }

    private void HandleSwapPerformed()
    {
        if (currentStep != TutorialStep.IntroSwapTiles)
        {
            return;
        }

        StartCompletion();
    }

    private void HandleInteractionVisualsCleared()
    {
        ReapplyPersistentTileHighlights();
    }

    private void BuildOverlayUi()
    {
        GameObject canvasObject = new GameObject("HowToPlayOverlayCanvas");
        canvasObject.transform.SetParent(transform, false);
        overlayCanvas = canvasObject.AddComponent<Canvas>();
        overlayCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        overlayCanvas.sortingOrder = 200;
        canvasObject.AddComponent<CanvasScaler>();
        canvasObject.AddComponent<GraphicRaycaster>();

        RectTransform canvasRect = canvasObject.GetComponent<RectTransform>();
        canvasRect.anchorMin = Vector2.zero;
        canvasRect.anchorMax = Vector2.one;
        canvasRect.offsetMin = Vector2.zero;
        canvasRect.offsetMax = Vector2.zero;

        GameObject blockerObject = new GameObject("TapAnywhereBlocker");
        blockerObject.transform.SetParent(canvasObject.transform, false);
        RectTransform blockerRect = blockerObject.AddComponent<RectTransform>();
        blockerRect.anchorMin = Vector2.zero;
        blockerRect.anchorMax = Vector2.one;
        blockerRect.offsetMin = Vector2.zero;
        blockerRect.offsetMax = Vector2.zero;

        dimmerImage = blockerObject.AddComponent<Image>();
        dimmerImage.color = new Color(0f, 0f, 0f, 0.15f);
        tapAnywhereButton = blockerObject.AddComponent<Button>();
        tapAnywhereButton.targetGraphic = dimmerImage;

        popupPanel = new GameObject("PopupPanel");
        popupPanel.transform.SetParent(canvasObject.transform, false);
        RectTransform panelRect = popupPanel.AddComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0f);
        panelRect.anchorMax = new Vector2(0.5f, 0f);
        panelRect.pivot = new Vector2(0.5f, 0f);
        panelRect.anchoredPosition = new Vector2(0f, 36f);
        panelRect.sizeDelta = new Vector2(900f, 240f);

        Image panelImage = popupPanel.AddComponent<Image>();
        panelImage.color = new Color(0.96f, 0.92f, 0.82f, 0.95f);
        panelImage.raycastTarget = false;

        GameObject titleObject = new GameObject("Title");
        titleObject.transform.SetParent(popupPanel.transform, false);
        stepTitleText = titleObject.AddComponent<TextMeshProUGUI>();
        LocalizationManager.ApplyFont(stepTitleText);
        stepTitleText.fontSize = 44f;
        stepTitleText.fontStyle = FontStyles.Bold;
        stepTitleText.color = Color.black;
        stepTitleText.alignment = TextAlignmentOptions.TopLeft;
        stepTitleText.raycastTarget = false;
        RectTransform titleRect = titleObject.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0f, 1f);
        titleRect.anchorMax = new Vector2(1f, 1f);
        titleRect.pivot = new Vector2(0.5f, 1f);
        titleRect.offsetMin = new Vector2(32f, -82f);
        titleRect.offsetMax = new Vector2(-32f, -24f);

        GameObject bodyObject = new GameObject("Body");
        bodyObject.transform.SetParent(popupPanel.transform, false);
        bodyText = bodyObject.AddComponent<TextMeshProUGUI>();
        LocalizationManager.ApplyFont(bodyText);
        bodyText.fontSize = 30f;
        bodyText.color = Color.black;
        bodyText.alignment = TextAlignmentOptions.TopLeft;
        bodyText.textWrappingMode = TextWrappingModes.Normal;
        bodyText.raycastTarget = false;
        RectTransform bodyRect = bodyObject.GetComponent<RectTransform>();
        bodyRect.anchorMin = new Vector2(0f, 0f);
        bodyRect.anchorMax = new Vector2(1f, 1f);
        bodyRect.offsetMin = new Vector2(32f, 28f);
        bodyRect.offsetMax = new Vector2(-32f, -88f);
    }

    private void ShowMessage(string title, string body, bool waitsForTapAnywhere, UnityEngine.Events.UnityAction tapHandler)
    {
        stepTitleText.text = title;
        bodyText.fontStyle = LocalizationManager.CurrentLanguageCode == "zh-hk"
            ? FontStyles.Bold
            : FontStyles.Normal;
        bodyText.text = body;

        tapAnywhereButton.onClick.RemoveAllListeners();
        tapAnywhereButton.interactable = waitsForTapAnywhere;
        dimmerImage.raycastTarget = waitsForTapAnywhere;

        if (waitsForTapAnywhere && tapHandler != null)
        {
            tapAnywhereButton.onClick.AddListener(tapHandler);
        }
    }

    private void ShowLocalizedMessage(string titleKey, string bodyKey, bool waitsForTapAnywhere, UnityEngine.Events.UnityAction tapHandler)
    {
        ShowMessage(
            LocalizationManager.GetText(titleKey),
            LocalizationManager.GetText(bodyKey),
            waitsForTapAnywhere,
            tapHandler);
    }

    private void ClearHighlights()
    {
        foreach (TileView tile in highlightedTiles)
        {
            if (tile != null)
            {
                tile.ResetVisual();
            }
        }

        highlightedTiles.Clear();

        foreach (RectTransform rectTransform in highlightedButtons)
        {
            if (rectTransform != null && originalButtonScales.TryGetValue(rectTransform, out Vector3 originalScale))
            {
                rectTransform.localScale = originalScale;
            }
        }

        highlightedButtons.Clear();
        originalButtonScales.Clear();
    }

    private void HighlightTiles(params Vector2Int[] positions)
    {
        ClearHighlights();

        for (int i = 0; i < positions.Length; i++)
        {
            TileView tile = boardManager.GetTileAt(positions[i]);
            if (tile == null || tile.IsPath)
            {
                continue;
            }

            tile.SetCustomScale(TileHighlightScale, 20);
            highlightedTiles.Add(tile);
        }
    }

    private void HighlightButton(Button button)
    {
        ClearHighlights();

        if (button == null)
        {
            return;
        }

        RectTransform rectTransform = button.GetComponent<RectTransform>();
        if (rectTransform == null)
        {
            return;
        }

        originalButtonScales[rectTransform] = rectTransform.localScale;
        rectTransform.localScale = rectTransform.localScale * ButtonHighlightScale;
        highlightedButtons.Add(rectTransform);
    }

    private void SetActionButtonsAvailable(bool undoAvailable, bool shuffleAvailable, bool swapAvailable)
    {
        if (undoButton != null)
        {
            undoButton.interactable = undoAvailable;
        }

        if (shuffleButton != null)
        {
            shuffleButton.interactable = shuffleAvailable;
        }

        if (swapButton != null)
        {
            swapButton.interactable = swapAvailable;
        }
    }

    private void SetStoreButtonAvailable(bool storeAvailable)
    {
        if (storeButton != null)
        {
            storeButton.enabled = storeAvailable;
        }
    }

    private void LoadBoard(BoardManager.SaveData boardData)
    {
        boardInteractionController.ForceClearInteractionState();
        currentTargetTileA = null;
        currentTargetTileB = null;
        boardManager.RestoreFromSaveData(boardData);
        uiManager.RestoreTimerState(0f, false);
        uiManager.HideNoMatchPopup();
    }

    private void SetTrackedTargetTiles(Vector2Int firstPosition, Vector2Int secondPosition)
    {
        currentTargetTileA = boardManager.GetTileAt(firstPosition);
        currentTargetTileB = boardManager.GetTileAt(secondPosition);
    }

    private void ReapplyPersistentTileHighlights()
    {
        for (int i = highlightedTiles.Count - 1; i >= 0; i--)
        {
            TileView tile = highlightedTiles[i];

            if (tile == null || tile.IsPath)
            {
                highlightedTiles.RemoveAt(i);
                continue;
            }

            tile.SetCustomScale(TileHighlightScale, 20);
        }
    }

    private bool MatchesTrackedTiles(TileView tileA, TileView tileB)
    {
        return tileA != null &&
               tileB != null &&
               currentTargetTileA != null &&
               currentTargetTileB != null &&
               ((tileA == currentTargetTileA && tileB == currentTargetTileB) ||
                (tileA == currentTargetTileB && tileB == currentTargetTileA));
    }

    private void ReturnToMainMenu()
    {
        ClearHighlights();

        if (uiManager != null)
        {
            uiManager.OnClickMainMenu();
            return;
        }

        SceneManager.LoadScene("MainMenu");
    }

    private static bool MatchesPosition(TileView tile, Vector2Int position)
    {
        return tile != null && tile.GridPosition == position;
    }

    private static bool MatchesAnyPosition(TileView tile, Vector2Int firstPosition, Vector2Int secondPosition)
    {
        return MatchesPosition(tile, firstPosition) || MatchesPosition(tile, secondPosition);
    }

    private static Button FindButton(string objectName)
    {
        GameObject buttonObject = GameObject.Find(objectName);
        return buttonObject != null ? buttonObject.GetComponent<Button>() : null;
    }

    private static BoardManager.SaveData CreateInitialBoardData()
    {
        int[,] tileLayout =
        {
            { 16, 15, 8, 15, 6, 14, 7, 4, 5, 5, 13, 3 },
            { 11, 3, 4, 5, 11, 17, 2, 1, 0, 0, 1, 17 },
            { 2, 4, 12, 8, 3, 14, 13, 9, 10, 2, 7, 9 },
            { 6, 16, 12, 2, 14, 11, 11, 8, 6, 17, 9, 10 },
            { 4, 17, 16, 3, 13, 1, 10, 12, 0, 16, 9, 5 },
            { 15, 6, 15, 13, 1, 7, 12, 8, 0, 14, 7, 10 }
        };

        return CreatePresetBoardData(tileLayout, null);
    }

    private static BoardManager.SaveData CreateSwapBoardData()
    {
        int[,] tileLayout =
        {
            { 16, 14, 5, 10, 0, 12, 17, 3, 9, 5, 3, 13 },
            { 16, 6, 7, 7, 6, 8, 2, 1, 0, 0, 1, 12 },
            { 2, 2, 7, 9, 10, 17, 8, 14, 13, 2, 16, 4 },
            { 13, 11, 17, 17, 15, 0, 8, 15, 10, 13, 9, 5 },
            { 11, 6, 8, 12, 14, 11, 11, 3, 9, 14, 10, 7 },
            { 4, 3, 12, 5, 1, 15, 4, 15, 6, 1, 16, 4 }
        };

        HashSet<Vector2Int> pathPositions = new HashSet<Vector2Int>
        {
            new Vector2Int(6, 1),
            new Vector2Int(7, 1),
            new Vector2Int(8, 1),
            new Vector2Int(9, 1),
            new Vector2Int(10, 1),
            new Vector2Int(9, 2)
        };

        return CreatePresetBoardData(tileLayout, pathPositions);
    }

    private static BoardManager.SaveData CreatePresetBoardData(int[,] tileLayout, HashSet<Vector2Int> pathPositions)
    {
        int remainingFaceUpTiles = 0;

        for (int y = 0; y < TutorialBoardHeight; y++)
        {
            for (int x = 0; x < TutorialBoardWidth; x++)
            {
                if (pathPositions == null || !pathPositions.Contains(new Vector2Int(x, y)))
                {
                    remainingFaceUpTiles++;
                }
            }
        }

        BoardManager.SaveData saveData = new BoardManager.SaveData
        {
            Seed = 0,
            BoardWidth = TutorialBoardWidth,
            BoardHeight = TutorialBoardHeight,
            RemainingFaceUpTiles = remainingFaceUpTiles,
            Tiles = new List<BoardManager.SavedTileState>(TutorialBoardWidth * TutorialBoardHeight)
        };

        for (int y = 0; y < TutorialBoardHeight; y++)
        {
            for (int x = 0; x < TutorialBoardWidth; x++)
            {
                Vector2Int position = new Vector2Int(x, y);
                saveData.Tiles.Add(new BoardManager.SavedTileState
                {
                    TileTypeId = tileLayout[y, x],
                    X = x,
                    Y = y,
                    IsPath = pathPositions != null && pathPositions.Contains(position)
                });
            }
        }

        return saveData;
    }
}
