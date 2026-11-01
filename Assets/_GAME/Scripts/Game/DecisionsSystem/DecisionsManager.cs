using Sirenix.OdinInspector;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class DecisionsManager : GameSystemComponent
{
    public static DecisionsManager Instance {  get; private set; }

    [SerializeField] private Color32 colorDecision;
    [SerializeField] private Color32 colorUnloyal;
    [SerializeField] private Color32 colorChangePersona;

    public static Persona CurrentPersona => PersonasManager.Instance.CurrentPersona;
    public static PersonaInfo CurrentPersonaInfo => PersonasManager.Instance.CurrentPersonaInfo;
    public static int CurrentPersonaIndex => PersonasManager.Instance.CurrentPersonaIndex;
    public static List<Persona> Personas => PersonasManager.Instance.Personas;
    public static List<PersonaInfo> PersonaInfos => PersonasManager.Instance.PersonaInfos;

    private static bool decisionActive = false;
    private static TaskDecision currentDecision;

    private float timer = 0;


    public static void ShowDecisionPanel(TaskDecision taskDecision)
    {
        if (decisionActive)
            return;

        currentDecision = taskDecision;
        Instance.timer = 0;
        decisionActive = true;

        string taskQuestion = currentDecision.GetTaskQuestion(0);
        string taskDescription = currentDecision.GetTaskDescription(0);
        string[] decisionTexts = currentDecision.decisions.Select(x => x.decisionText).ToArray();
        ContentUI.SetupContentUI(taskQuestion, null, taskDescription, decisionTexts);
        ContentUI.ShowGradientBackground(Instance.colorDecision);

        for (int i = 0; i < decisionTexts.Length; i++)
        {
            int tempI = i;

            ContentUI.ButtonEvents[tempI].AddListener(() =>
            {
                ApplyDecision(tempI);
            });
        }
    }

    public static void ReverseDecision(TaskReferences taskReferences)
    {
        string taskDecisionId = taskReferences.taskID;

        if (taskReferences == null)
            throw new ArgumentException($"No task found with ID: {taskDecisionId}", nameof(taskDecisionId));

        if(taskReferences.data.TaskType != TaskBase.TASK_TYPE.Decision)
            throw new ArgumentException($"Task with ID: {taskDecisionId} is not a Decision Task.", nameof(taskDecisionId));

        TaskDecision tempTaskData = taskReferences.data as TaskDecision;
        TaskInfo tempTaskInfo = taskReferences.info;

        int decisionIndex = tempTaskInfo.decisionTaken;

        if (decisionIndex < 0 || decisionIndex >= tempTaskData.decisions.Count)
            throw new ArgumentOutOfRangeException(nameof(decisionIndex), "Decision index is out of range.");

        DecisionData decision = tempTaskData.decisions[decisionIndex];

        if (decision.personaIndex < 0 || decision.personaIndex >= Personas.Count)
            throw new ArgumentOutOfRangeException(nameof(decision.personaIndex), "Persona index in decision is out of range.");

        PersonaInfos[decision.personaIndex].personaValue -= decision.personaValueChange;
    }

    private static void ApplyDecision(int decisionIndex)
    {
        if (!decisionActive || currentDecision == null || decisionIndex < 0 ||
            decisionIndex >= currentDecision.decisions.Count)
            return;

        TaskReferences active = PlayerController.Instance?.ActiveTask;
        if (active?.info == null || active.taskID != currentDecision.taskID || active.info.IsDone)
            return;

        DecisionData selected = currentDecision.decisions[decisionIndex];
        if (selected.personaIndex < 0 || selected.personaIndex >= Personas.Count)
            return;

        QuestInfo questInfo = PlayerController.Instance.GetQuestInfoByID(currentDecision.parentQuestID);
        questInfo.questDecision = decisionIndex;
        questInfo.questTimer = Instance.timer;
        TaskInfo taskInfo = PlayerController.Instance.GetTaskReferencesByID(currentDecision.taskID).info;
        taskInfo.taskTimer = Instance.timer;
        taskInfo.decisionTaken = decisionIndex;

        DecisionData decision = currentDecision.decisions[decisionIndex];
        Persona currentPersona = CurrentPersona;
        PersonaInfo currentPersonaInfo = CurrentPersonaInfo;
        int currentPersonaIndex = CurrentPersonaIndex;
        bool playerChosedDifferentPersona = currentPersonaIndex != decision.personaIndex;
        bool firstChoice = currentPersonaInfo.personaValue == 0;

        PersonaInfos[decision.personaIndex].personaValue += decision.personaValueChange;

        if (playerChosedDifferentPersona || firstChoice)
        {
            int newPersonaIndex = CurrentPersonaIndex;
            bool playerChangedPersona = currentPersonaIndex != newPersonaIndex;

            if (playerChangedPersona || currentPersonaInfo.personaValue == 0 || firstChoice)
            {
                ContentUI.SetupContentUI(CurrentPersona.messagePlayerJoinedHeader, null, CurrentPersona.messagePlayerJoined, "Continue");
                ContentUI.ShowGradientBackground(Instance.colorChangePersona);
                ContentUI.ButtonEvents[0].AddListener(GameplayUIManager.ShowGameUI);
            }
            else
            {
                ContentUI.SetupContentUI(currentPersona.messagePlayerUnloyalHeader, null, currentPersona.messagePlayerUnloyal, "Continue");
                ContentUI.ShowGradientBackground(Instance.colorUnloyal);
                ContentUI.ButtonEvents[0].AddListener(GameplayUIManager.ShowGameUI);
            }
        }
        else
        {
            GameplayUIManager.ShowGameUI();
        }

        string completedTaskID = currentDecision.taskID;
        decisionActive = false;
        currentDecision = null;

        // Save the choice, persona change and learning points together.
        PlayerController.Instance.CompleteActiveTask(expectedTaskID: completedTaskID);
        active.window?.UpdateAreas();
    }

    private void Awake()
    {
        Instance = this;
        CancelPendingDecision();
    }

    public static void CancelPendingDecision()
    {
        decisionActive = false;
        currentDecision = null;
    }

    private void Update()
    {
        if (decisionActive == false)
            return;

        timer += Time.deltaTime;
    }
}
