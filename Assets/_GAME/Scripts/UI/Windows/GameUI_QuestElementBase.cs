using Michsky.UI.Beam;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(GameUI_QuestElements))]
public class GameUI_QuestElementBase : MonoBehaviour
{
    private GameUI_QuestElements uiElementsComponent;

    private GameObject QuestAnswerArea => uiElementsComponent.QuestAnswerArea;
    private GameObject QuestCompletedArea => uiElementsComponent.QuestCompletedArea;

    private ButtonManager ArticleButton => uiElementsComponent.ArticleButton;
    private List<GameObject> SkipElements => uiElementsComponent.SkipElements;

    protected TextMeshProUGUI DescriptionTMP => uiElementsComponent.DescriptionTMP;
    private TextMeshProUGUI QuestionLineTMP => uiElementsComponent.QuestionLineTMP;
    private TextMeshProUGUI CompleteInfoTMP => uiElementsComponent.CompleteInfoTMP;
    private TextMeshProUGUI TimerTMP => uiElementsComponent.TimerTMP;
    private Image CharacterImage => uiElementsComponent.CharacterImage;
    private TextMeshProUGUI CompleteLabelTMP => uiElementsComponent.CompleteLabelTMP;
    private ButtonManager NextQuestButton => uiElementsComponent.NextQuestButton;

    public QuestReferences QuestRef { get; private set; }
    public TaskReferences TaskRef { get; private set; }

    public virtual void SetupQuestUIElement(QuestReferences _questRef, TaskReferences _taskRef, GameUI_QuestElementBase previousQuestwindow = null)
    {
        QuestRef = _questRef;
        TaskRef = _taskRef;
        SetHeader(QuestRef.data.GetQuestNameWithProgress(_taskRef.taskID, QuestRef.info.persona));
        SetIcon(QuestRef.data.GetIcon());
        DescriptionTMP.text = CyberpediaManager.FormatWithArticleLinks(_taskRef.data.GetTaskDescription());
        QuestionLineTMP.text = _taskRef.data.GetTaskQuestion();
        CompleteInfoTMP.text = _taskRef.data.GetTaskCompleteText();
        CharacterImage.sprite = _questRef.data.GetCharacter();
        UpdateAreas();

        if(TaskRef.data.articleID.IsNullOrEmpty() == false)
        {
            ArticleButton.onClick.AddListener(() =>
            {
                CyberpediaManager.Instance.OpenArticle(TaskRef.data.articleID);
            });
            ArticleButton.Interactable(true);
            ArticleButton.UpdateState();
        }
        else
        {
            ArticleButton.Interactable(false);
            ArticleButton.UpdateState();
        }

        StretchRectTransformToParent();
    }

    public void SetHeader(string header)
    {
        uiElementsComponent.SetHeader(header);
    }
    public void SetIcon(Sprite icon)
    {
        uiElementsComponent.SetIcon(icon);
    }

    public void CheckAnswer(string answer)
    {
        if(QuestManager.CheckAnswer(answer, TaskRef.taskID))
            ToggleAreas(true);
        RefreshLearningPoints();
    }

    public virtual void StartNextQuest()
    {
        PlayerController.Instance.HandleNextQuestOrTask(QuestRef, TaskRef);
    }

    public void SkipTask()
    {
        if (PlayerController.Instance.ActiveTask != TaskRef)
            return;

        string taskID = TaskRef.taskID;
        string header = QuestRef.data.GetQuestNameWithProgress(taskID, QuestRef.info.persona);

        ContentUI.SetupContentUI(header, null,
            "Do you want to skip this task?\n\nYou will not get any points.",
            "Skip", "Back");
        ContentUI.ButtonEvents[0].AddListener(() =>
        {
            if (PlayerController.Instance.CompleteActiveTask(0, false, true, taskID))
                ToggleAreas(true);
            GameplayUIManager.ShowGameUI();
        });
        ContentUI.ButtonEvents[1].AddListener(GameplayUIManager.ShowGameUI);
        ContentUI.DefaultButtonIndex = 1;
    }

    public void UpdateAreas()
    {
        bool isQuestDone = false;

        if (TaskRef.info != null)
            isQuestDone = TaskRef.info.IsDone;

        ToggleAreas(isQuestDone);
    }

    private void Reset()
    {
        if (uiElementsComponent == null)
            uiElementsComponent = GetComponent<GameUI_QuestElements>();
    }

    protected void Awake()
    {
        uiElementsComponent = GetComponent<GameUI_QuestElements>();
    }

    private void Update()
    {
        if(Input.GetKeyDown(KeyCode.X))
            UpdateAreas();

        if (QuestRef.info == null)
            return;

        if (TimerTMP != null)
            TimerTMP.text = QuestRef.info.questTimer.MinutesFloatToTimeString();
        else
            Debug.Log(QuestRef.info.questTimer.MinutesFloatToTimeString());
    }

    protected virtual void ToggleAreas(bool questCompleted)
    {
        RefreshLearningPoints();
        DescriptionTMP.gameObject.SetActiveOptimized(!questCompleted);
        QuestAnswerArea.SetActiveOptimized(!questCompleted);
        QuestCompletedArea.SetActiveOptimized(questCompleted);

        if (questCompleted)
        {
            (string nextQuestText, string labelText) = PlayerController.Instance.GetNextQuestButtonText(QuestRef, TaskRef);
            NextQuestButton.SetText(nextQuestText);
            CompleteLabelTMP.text = labelText;
        }

        for (int i = 0; i < SkipElements.Count; i++)
        {
            SkipElements[i].SetActiveOptimized(!questCompleted);
        }
    }

    private void RefreshLearningPoints()
    {
        if (TaskRef?.data == null || TaskRef.info == null)
            return;

        TaskInfo info = TaskRef.info;
        int maximum = TaskScoring.GetMaxPoints(TaskRef.data, info);
        int points = info.IsDone ? info.educationalPoints : TaskScoring.GetAvailablePoints(TaskRef.data, info);
        string scoreText = $"<b>Learning points:</b> {points} / {maximum}";
        if (!info.IsDone)
            scoreText += " available";
        scoreText += $"  |  <b>Wrong answers:</b> {info.wrongAnswerCount}";

        // Keep the existing task-specific layouts; their text areas already size to content.
        QuestionLineTMP.text = TaskRef.data.GetTaskQuestion() + "\n\n<size=80%>" + scoreText + "</size>";
        CompleteInfoTMP.text = TaskRef.data.GetTaskCompleteText() + "\n\n<size=80%>" + scoreText + "</size>";
    }
    private void StretchRectTransformToParent()
    {
        RectTransform rectTransform = uiElementsComponent.RectTransform;
        if (rectTransform == null || rectTransform.parent == null)
            return;

        rectTransform.anchorMin = new Vector2(0, 0);
        rectTransform.anchorMax = new Vector2(1, 1);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.anchoredPosition = Vector2.zero;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;
    }
}
