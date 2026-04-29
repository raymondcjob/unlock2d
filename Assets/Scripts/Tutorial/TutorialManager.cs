using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public class TutorialManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TutorialBoardManager tutorialBoardManager;
    [SerializeField] private TMP_Text guideTitleText;
    [SerializeField] private TMP_Text guideBodyText;

    [Header("Undo UI")]
    [SerializeField] private Button undoButton;
    [SerializeField] private TMP_Text undoCountText;

    [Header("Shuffle UI")]
    [SerializeField] private Button shuffleButton;
    [SerializeField] private TMP_Text shuffleCountText;

    [Header("Swap UI")]
    [SerializeField] private Button swapButton;
    [SerializeField] private TMP_Text swapCountText;
    [SerializeField] private string mainMenuSceneName = "MainMenu";

    private int undoCount;
    private int shuffleCount;
    private int swapCount;
    private bool undoGranted;
    private bool shuffleGranted;
    private bool swapGranted;
    private string currentGuideTitleKey;
    private string currentGuideBodyKey;
    private bool isWaitingForTutorialCompleteRelease;

    private void Awake()
    {
        if (tutorialBoardManager == null)
        {
            tutorialBoardManager = FindAnyObjectByType<TutorialBoardManager>();
        }

        undoCount = 0;
        shuffleCount = 0;
        swapCount = 0;
        undoGranted = false;
        shuffleGranted = false;
        swapGranted = false;
        isWaitingForTutorialCompleteRelease = false;

        RefreshItemButtons();
    }

    private void Update()
    {
        if (tutorialBoardManager == null || tutorialBoardManager.CurrentFlowStep != TutorialBoardManager.TutorialBoardFlowStep.TutorialCompleted)
        {
            return;
        }

        if (isWaitingForTutorialCompleteRelease)
        {
            if (!IsPointerHeld())
            {
                isWaitingForTutorialCompleteRelease = false;
            }

            return;
        }

        if (IsPointerPressedThisFrame())
        {
            SceneManager.LoadScene(mainMenuSceneName);
        }
    }

    private void OnEnable()
    {
        if (tutorialBoardManager != null)
        {
            tutorialBoardManager.FlowStepChanged += HandleFlowStepChanged;
        }

        LocalizationManager.OnLanguageChanged += RefreshGuideText;
    }

    private void Start()
    {
        if (tutorialBoardManager != null)
        {
            HandleFlowStepChanged(tutorialBoardManager.CurrentFlowStep);
        }
    }

    private void OnDisable()
    {
        if (tutorialBoardManager != null)
        {
            tutorialBoardManager.FlowStepChanged -= HandleFlowStepChanged;
        }

        LocalizationManager.OnLanguageChanged -= RefreshGuideText;
    }

    private void HandleFlowStepChanged(TutorialBoardManager.TutorialBoardFlowStep flowStep)
    {
        switch (flowStep)
        {
            case TutorialBoardManager.TutorialBoardFlowStep.StepUndo:
                if (!undoGranted)
                {
                    undoCount += 1;
                    undoGranted = true;
                }
                break;

            case TutorialBoardManager.TutorialBoardFlowStep.StepShuffle:
                if (!shuffleGranted)
                {
                    shuffleCount += 1;
                    shuffleGranted = true;
                }
                break;

            case TutorialBoardManager.TutorialBoardFlowStep.StepSwap:
                if (!swapGranted)
                {
                    swapCount += 1;
                    swapGranted = true;
                }
                break;

            case TutorialBoardManager.TutorialBoardFlowStep.TutorialCompleted:
                isWaitingForTutorialCompleteRelease = true;
                break;
        }

        SetGuideTextForFlowStep(flowStep);
        RefreshItemButtons();
    }

    public void ConsumeUndoUse()
    {
        if (undoCount > 0)
        {
            undoCount--;
            RefreshItemButtons();
        }
    }

    public void ConsumeShuffleUse()
    {
        if (shuffleCount > 0)
        {
            shuffleCount--;
            RefreshItemButtons();
        }
    }

    public void ConsumeSwapUse()
    {
        if (swapCount > 0)
        {
            swapCount--;
            RefreshItemButtons();
        }
    }

    public void OnClickUndo()
    {
        if (tutorialBoardManager == null || tutorialBoardManager.CurrentFlowStep != TutorialBoardManager.TutorialBoardFlowStep.StepUndo)
        {
            Debug.Log("Tutorial undo button clicked outside StepUndo.");
            return;
        }

        if (!CanUseUndo())
        {
            Debug.Log("No tutorial undo uses remaining.");
            return;
        }

        ConsumeUndoUse();
        tutorialBoardManager.ApplyUndoVisualToStep4();
    }

    public void OnClickShuffle()
    {
        if (tutorialBoardManager == null || tutorialBoardManager.CurrentFlowStep != TutorialBoardManager.TutorialBoardFlowStep.StepShuffle)
        {
            Debug.Log("Tutorial shuffle button clicked outside StepShuffle.");
            return;
        }

        if (!CanUseShuffle())
        {
            Debug.Log("No tutorial shuffle uses remaining.");
            return;
        }

        ConsumeShuffleUse();
        tutorialBoardManager.ApplyShuffleSnapshot();
    }

    public void OnClickSwap()
    {
        if (tutorialBoardManager == null || tutorialBoardManager.CurrentFlowStep != TutorialBoardManager.TutorialBoardFlowStep.StepSwap)
        {
            Debug.Log("Tutorial swap button clicked outside StepSwap.");
            return;
        }

        if (!CanUseSwap())
        {
            Debug.Log("No tutorial swap uses remaining.");
            return;
        }

        ConsumeSwapUse();
        tutorialBoardManager.BeginSwapSelection();
        SetGuideText("howTo.swapTiles.title", "howTo.swapTiles.body");
    }

    public void OnClickMainMenu()
    {
        SceneManager.LoadScene(mainMenuSceneName);
    }

    public bool CanUseUndo()
    {
        return undoCount > 0;
    }

    public bool CanUseShuffle()
    {
        return shuffleCount > 0;
    }

    public bool CanUseSwap()
    {
        return swapCount > 0;
    }

    private void RefreshItemButtons()
    {
        if (undoCountText != null)
        {
            undoCountText.text = undoCount.ToString();
        }

        if (shuffleCountText != null)
        {
            shuffleCountText.text = shuffleCount.ToString();
        }

        if (swapCountText != null)
        {
            swapCountText.text = swapCount.ToString();
        }

        ApplyButtonState(undoButton, CanUseUndo());
        ApplyButtonState(shuffleButton, CanUseShuffle());
        ApplyButtonState(swapButton, CanUseSwap());
    }

    private void SetGuideTextForFlowStep(TutorialBoardManager.TutorialBoardFlowStep flowStep)
    {
        switch (flowStep)
        {
            case TutorialBoardManager.TutorialBoardFlowStep.Step1:
                SetGuideText("howTo.step1.title", "howTo.step1.body");
                break;
            case TutorialBoardManager.TutorialBoardFlowStep.Step2:
                SetGuideText("howTo.step2.title", "howTo.step2.body");
                break;
            case TutorialBoardManager.TutorialBoardFlowStep.Step3:
                SetGuideText("howTo.step3.title", "howTo.step3.body");
                break;
            case TutorialBoardManager.TutorialBoardFlowStep.Step4:
                SetGuideText("howTo.step4.title", "howTo.step4.body");
                break;
            case TutorialBoardManager.TutorialBoardFlowStep.StepUndo:
                SetGuideText("howTo.undo.title", "howTo.undo.body");
                break;
            case TutorialBoardManager.TutorialBoardFlowStep.StepShuffle:
                SetGuideText("howTo.shuffle.title", "howTo.shuffle.body");
                break;
            case TutorialBoardManager.TutorialBoardFlowStep.StepSwap:
                SetGuideText("howTo.swapButton.title", "howTo.swapButton.body");
                break;
            case TutorialBoardManager.TutorialBoardFlowStep.TutorialCompleted:
                SetGuideText("howTo.complete.title", "howTo.complete.body");
                break;
        }
    }

    private void SetGuideText(string titleKey, string bodyKey)
    {
        currentGuideTitleKey = titleKey;
        currentGuideBodyKey = bodyKey;
        RefreshGuideText();
    }

    private void RefreshGuideText()
    {
        if (guideTitleText != null)
        {
            LocalizationManager.ApplyFont(guideTitleText);
            guideTitleText.text = LocalizationManager.GetText(currentGuideTitleKey);
        }

        if (guideBodyText != null)
        {
            LocalizationManager.ApplyFont(guideBodyText);
            guideBodyText.fontStyle = LocalizationManager.CurrentLanguageCode == "zh-hk"
                ? FontStyles.Bold
                : FontStyles.Normal;
            guideBodyText.text = LocalizationManager.GetText(currentGuideBodyKey);
        }
    }

    private static void ApplyButtonState(Button button, bool isAvailable)
    {
        if (button != null)
        {
            button.interactable = isAvailable;
        }
    }

    private static bool IsPointerPressedThisFrame()
    {
        if (Input.touchCount > 0)
        {
            for (int i = 0; i < Input.touchCount; i++)
            {
                if (Input.GetTouch(i).phase == TouchPhase.Began)
                {
                    return true;
                }
            }
        }

        return Input.GetMouseButtonDown(0);
    }

    private static bool IsPointerHeld()
    {
        if (Input.touchCount > 0)
        {
            return true;
        }

        return Input.GetMouseButton(0);
    }
}
