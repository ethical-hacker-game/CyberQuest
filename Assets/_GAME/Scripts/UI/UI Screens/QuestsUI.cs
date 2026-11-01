using ChartAndGraph;
using GIGA.AutoRadialLayout;
using Michsky.UI.Beam;
using RuntimeInspectorNamespace;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class QuestsUI : UI_Screen
{
    public static QuestsUI Instance => GameplayUIManager.Instance.QuestsUI;

    private const string PersonaCategory = "Player 1";
    private const string PersonaLabelTemplate = "Current Persona: <b>{0}</b>";

    [Header("Quest Tree")]
    [SerializeField] private RectTransform questTreeArea;
    [SerializeField] private RadialLayout radialLayout;

    [Header("Quest Detalis")]
    [SerializeField] private LayoutGroupFix layoutRebuilder;
    [SerializeField] private TextMeshProUGUI titleTMP;
    [SerializeField] private TextMeshProUGUI descriptionTMP;
    [SerializeField] private TextMeshProUGUI statusTMP;

    [Header("Persona")]
    [SerializeField] private RadarChart radar;
    [SerializeField] private TextMeshProUGUI personaTMP;

    private Coroutine personaRadarRefresh;

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
        VisibleQuest.data = QuestManager.GetQuestByID(VisibleQuest.info.questID);

        base.ShowScreen();
        ShowQuestDetails();
        UpdateNodeVisuals();
        GenerateQuestTree();
        RefreshPersonaRadar();

        if (personaRadarRefresh != null)
            StopCoroutine(personaRadarRefresh);

        personaRadarRefresh = StartCoroutine(RefreshPersonaRadarNextFrame());
    }

    private IEnumerator RefreshPersonaRadarNextFrame()
    {
        yield return null;
        RefreshPersonaRadar();
        personaRadarRefresh = null;
    }

    private void RefreshPersonaRadar()
    {
        if (radar == null || PersonasManager.Instance == null)
            return;

        List<Persona> personas = PersonasManager.Instance.Personas;
        if (personas == null || personas.Count < 2)
            return;

        if (GroupsMatchPersonas(personas) == false)
        {
            radar.DataSource.ClearGroups();

            for (int i = 0; i < personas.Count; i++)
                radar.DataSource.AddGroup(personas[i].personaID);
        }

        radar.DataSource.MinValue = -10;
        radar.DataSource.MaxValue = 100;

        List<PersonaInfo> personaInfos = PersonasManager.Instance.PersonaInfos;
        if (personaInfos != null)
        {
            for (int i = 0; i < personaInfos.Count; i++)
            {
                PersonaInfo personaInfo = personaInfos[i];
                radar.DataSource.SetValue(PersonaCategory, personaInfo.personaID, personaInfo.personaValue);
            }
        }

        if (personaTMP != null)
            personaTMP.text = string.Format(PersonaLabelTemplate, PersonasManager.Instance.CurrentPersonaName);

        if (radar.isActiveAndEnabled)
            radar.GenerateChart();
    }

    private bool GroupsMatchPersonas(List<Persona> personas)
    {
        if (radar.DataSource.TotalGroups != personas.Count)
            return false;

        for (int i = 0; i < personas.Count; i++)
        {
            if (radar.DataSource.GetGroupName(i) != personas[i].personaID)
                return false;
        }

        return true;
    }

    public void FlyToCity()
    {
        WPM.WorldMapGlobe.instance.FlyToCity(VisibleQuest.data.cityIndex);
        GameplayUIManager.ShowGameUI();
    }

    public void ResetToQuest()
    {
        int questIndex = PlayerController.Instance.GetQuestIndexByID(VisibleQuest.data.questID);

        if (questIndex < 0)
        {
            GameplayUIManager.ShowGameUI();
            return;
        }

        ContentUI.SetupContentUI("Reset progress", null,
            "Are you sure you want to reset your progress to this quest?\n" +
            "All progress after this quest will be lost.", "Yes, reset", "Cancel");
        ContentUI.ButtonEvents[0].AddListener(() =>
        {
            GameplayUIManager.ShowGameUI();
            PlayerController.Instance.ResetProgressToQuestIndex(questIndex, true);
        });
        ContentUI.ButtonEvents[1].AddListener(() =>
        {
            GameplayUIManager.ShowQuestsUI();
        });

    }

    private void Start()
    {
        questTreeArea.sizeDelta = PlayerController.Instance.ActiveUnit.data.questsAreaSize;

        radialLayout.GetComponent<RectTransform>().anchoredPosition =
            new Vector2(PlayerController.Instance.ActiveUnit.data.questTreeOffset,
            radialLayout.transform.localPosition.y);

        GenerateQuestTree();
    }

    private void GenerateQuestTree()
    {
        questNodes.Clear();
        questNodes.AddRange(QuestsTreeBuilder.PrepareQuestView(radialLayout, QuestIDs, false));
        HighlightSelectedNodeVisuals();
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

        if (VisibleQuest.info == null)
        {
            titleTMP.text = "LOCKED";
            statusTMP.text = "LOCKED";
            descriptionTMP.text = "Quest is not available yet. Complete previous quests to unlock it.";
            layoutRebuilder.FixLayout();
            return;
        }

        titleTMP.text = VisibleQuest.data.GetQuestName(VisibleQuest.info.persona);
        descriptionTMP.text = VisibleQuest.data.GetQuestDescription();

        if (titleTMP.text.Length > 25)
            titleTMP.text = titleTMP.text.Substring(0, 22) + "...";

        if (descriptionTMP.text.Length > 500)
            descriptionTMP.text = descriptionTMP.text.Substring(0, 500) + "...";

        layoutRebuilder.FixLayout();


        if (VisibleQuest.info.IsDone)
            statusTMP.text = "COMPLETED";
        else if (VisibleQuest.info.timerStarted)
            statusTMP.text = "IN PROGRESS";
        else
            statusTMP.text = "READY TO START";
    }

    private void UpdateNodeVisuals()
    {
        QuestNode_Visuals[] nodeVisuals = radialLayout.transform.GetComponentsInChildren<QuestNode_Visuals>();

        foreach (QuestNode_Visuals nodeVisual in nodeVisuals)
        {
            nodeVisual.UpdateVisuals();
        }
    }

    private void HighlightSelectedNodeVisuals()
    {
        if (VisibleQuest == null)
            return;

        if(VisibleQuest.data == null)
            return;

        for (int i = 0; i < questNodes.Count; i++)
        {
            questNodes[i].ToggleSelection(questNodes[i].QuestID == VisibleQuest.data.questID);
        }
    }
}
