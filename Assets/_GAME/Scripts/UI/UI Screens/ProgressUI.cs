using GIGA.AutoRadialLayout;
using Michsky.UI.Beam;
using RuntimeInspectorNamespace;
using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ProgressUI : UI_Screen
{
    public static ProgressUI Instance => GameplayUIManager.Instance.ProgressUI;

    private const string PersonaLabelTemplate = "Current Persona: <b>{0}</b>";
    private const string ReputationLabelTemplate = "Reputation: <b>{0}</b>";
    private const string EthicalLabelTemplate = "Ethical Standing: <b>{0}</b>";//Ethical / Questionable / Malicious
    private const string AffiliationLabelTemplate = "Affiliation: <b>{0}</b>";//Freelancer / Corporate Security / Cybercrime Group
    private const string TitleLabelTemplate = "Title: <b>{0}</b>";//Novice / Security Analyst / Penetration Tester / Ethical Hacker / Elite Hacker

    [Header("Persona Stats")]
    [SerializeField] private Image reputationBar;
    [SerializeField] private Gradient reputationGradient;
    [Space]
    [SerializeField] private TextMeshProUGUI personaTMP;
    [SerializeField] private TextMeshProUGUI reputationTMP;
    [SerializeField] private TextMeshProUGUI ethicalTMP;
    [SerializeField] private TextMeshProUGUI affiliationTMP;
    [SerializeField] private TextMeshProUGUI titleTMP;
    [SerializeField] private TextMeshProUGUI articlesReadTMP;


    [Header("Unit Stats")]
    [SerializeField] private TextMeshProUGUI learningScoreTMP;

    [Header("Quest Map")]
    [SerializeField] private RectTransform mapObject;
    [SerializeField] private GameObject mapQuestButton;


    [Header("Quest Stats")]

    private void RefreshPersonaStats()
    {
        personaTMP.text = string.Format(PersonaLabelTemplate,
            PersonasManager.Instance != null ? PersonasManager.Instance.CurrentPersonaName : "None");
        reputationTMP.text = string.Format(ReputationLabelTemplate, "Not tracked");
        ethicalTMP.text = string.Format(EthicalLabelTemplate, "Not tracked");
        affiliationTMP.text = string.Format(AffiliationLabelTemplate, "Not tracked");
        titleTMP.text = string.Format(TitleLabelTemplate, "Not tracked");
        reputationBar.gameObject.SetActive(false);
    }

    private List<QuestNode_Visuals> questNodes = new List<QuestNode_Visuals>();

    private List<string> QuestIDs => PlayerController.Instance.ActiveUnit.data.questIds;
    public QuestReferences VisibleQuest { get; set; } = new QuestReferences();


    public override void Back()
    {
        GameplayUIManager.ShowGameUI();
        base.Back();
    }

    public override void ShowScreen()
    {
        VisibleQuest.info = PlayerController.Instance.ActiveUnit.info.quests.LastOrDefault();
        VisibleQuest.data = VisibleQuest.info == null ? null : QuestManager.GetQuestByID(VisibleQuest.info.questID);

        base.ShowScreen();
        RefreshPersonaStats();
        RefreshCyberpediaProgress();
        RefreshLearningScore();
        ShowQuestDetails();
        //UpdateNodeVisuals();
    }

    public void ShowQuestsUI()
    {
        GameplayUIManager.ShowQuestsUI();
    }

    public void ShowQuestDetails(QuestReferences questRef)
    {
        VisibleQuest.data = questRef.data;
        VisibleQuest.info = questRef.info;
        ShowQuestDetails();
    }

    public void ShowQuestDetails(QuestNode_Visuals questNode_Visuals)
    {
        VisibleQuest.data = QuestManager.GetQuestByID(questNode_Visuals.QuestID);
        VisibleQuest.info = PlayerController.Instance.GetQuestReferencesByID(questNode_Visuals.QuestID).info;
        ShowQuestDetails();
    }

    public void ShowQuestDetails()
    {
        HighlightSelectedNodeVisuals();

        //titleTMP.text = VisibleQuest.data.GetQuestName(VisibleQuest.info.persona);
        //descriptionTMP.text = VisibleQuest.data.GetQuestDescription();

        //if (titleTMP.text.Length > 25)
        //    titleTMP.text = titleTMP.text.Substring(0, 22) + "...";

        //if (descriptionTMP.text.Length > 500)
        //    descriptionTMP.text = descriptionTMP.text.Substring(0, 500) + "...";

        //layoutRebuilder.FixLayout();

        //if (VisibleQuest.info == null)
        //{
        //    statusTMP.text = "LOCKED";
        //    return;
        //}

        //if (VisibleQuest.info.IsDone)
        //    statusTMP.text = "COMPLETED";
        //else if (VisibleQuest.info.timerStarted)
        //    statusTMP.text = "IN PROGRESS";
        //else
        //    statusTMP.text = "READY TO START";
    }

    public void RefreshLearningScore()
    {
        if (learningScoreTMP == null || PlayerController.Instance == null)
            return;

        UnitScoreSummary score = PlayerController.Instance.LearningScore;
        learningScoreTMP.text = $"Learning points: <b>{score.Points} / {score.MaxPoints} ({score.ScorePercent}%)</b>";
    }

    public void RefreshCyberpediaProgress()
    {
        if (articlesReadTMP == null)
            return;

        int articleCount = CyberpediaManager.Instance?.Articles.Count ?? 0;
        List<string> readArticleIDs = PlayerController.CurrentUnitInfo?.readArticleIDs;
        int readCount = 0;

        if (readArticleIDs != null && CyberpediaManager.Instance != null)
        {
            readCount = readArticleIDs
                .Distinct()
                .Count(articleID => CyberpediaManager.Instance.GetArticleByID(articleID) != null);
        }

        articlesReadTMP.text = $"Articles Read: <b>{readCount} / {articleCount}</b>";
    }

    private void HighlightSelectedNodeVisuals()
    {
        //if (VisibleQuest == null)
        //    return;

        //if(VisibleQuest.data == null)
        //    return;

        //for (int i = 0; i < questNodes.Count; i++)
        //{
        //    questNodes[i].ToggleSelection(questNodes[i].QuestID == VisibleQuest.data.questID);
        //}
    }
}
