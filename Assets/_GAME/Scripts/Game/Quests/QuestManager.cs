using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Type = System.Type;

public class QuestManager : GameSystemComponent
{
    public static QuestManager Instance;
    private static List<Quest> quests = new List<Quest>();
    private static List<TaskBase> tasks = new List<TaskBase>();


    [ShowInInspector]
    public static List<Quest> Quests
    {
        get
        {
            if (IsInitialized == false)
                LoadQuestsAndTasks();

            return quests;
        }
    }
    [ShowInInspector]
    public static List<TaskBase> Tasks
    {
        get
        {
            if(IsInitialized == false)
                LoadQuestsAndTasks();

            return tasks;
        }
    }
    private static bool IsInitialized { get; set; } = false;

    public static bool CheckAnswer(string answer)
    {
        TaskInfo tempQuestInfo = PlayerController.Instance?.ActiveTask?.info;

        if (tempQuestInfo == null)
            return false;

        return CheckAnswer(answer, tempQuestInfo.taskID);
    }

    public static bool CheckAnswer(string answer, string taskID)
    {
        PlayerController player = PlayerController.Instance;
        TaskReferences active = player?.ActiveTask;
        if (active?.info == null || active.taskID != taskID || active.info.IsDone)
            return false;

        // Opening a decision is not its completion. Only ApplyDecision can award it.
        if (active.data is TaskDecision decision)
        {
            if (answer == "decision")
                DecisionsManager.ShowDecisionPanel(decision);
            return false;
        }

        // Action and minigame completion comes from their owning controllers.
        if (!TaskScoring.CanPenalize(active.data.TaskType))
            return false;

        bool result = IsAnswerCorrect(answer, taskID);

        if (result)
        {
            player.CompleteActiveTask(expectedTaskID: taskID);
            AssistantUI.Instance.SpawnInfoNotification("Task Completed!");
        }
        else
        {
            player.RecordWrongAnswer(taskID);
            AssistantUI.Instance.SpawnErrorNotifiaction("Wrong Answer!");
        }

        return result;
    }

    public static Quest GetQuestByID(string questID)
    {
        if (Quests.IsNullOrEmpty())
            return null;

        for (int i = 0; i < Quests.Count; i++)
        {
            if (Quests[i].questID == questID)
                return Quests[i];
        }

        return null;
    }

    public static TaskBase GetTaskByID(string taskID)
    {
        if (Tasks.IsNullOrEmpty())
            return null;

        for (int i = 0; i < Tasks.Count; i++)
        {
            if (Tasks[i].taskID == taskID)
                return Tasks[i];
        }
        return null;
    }

    public static Type GetTaskType(string type)
    {
        Dictionary<string, Type> taskTypes = new Dictionary<string, Type>()
        {
            { "SingleChoice", typeof(TaskSingleChoice) },
            { "TrueFalse", typeof(TaskTrueFalse) },
            { "Input", typeof(TaskInput) },
            { "Website", typeof(TaskWebsite) },
            { "Decision", typeof(TaskDecision) },
            { "Action", typeof(TaskAction) },
            { "Minigame", typeof(TaskMinigame) },
            { "Dialogue", typeof(TaskDialogue) },
        };

        if (taskTypes.TryGetValue(type, out Type taskType))
        {
            return taskType;
        }

        throw new System.ArgumentException($"Unknown task type: {type}");
    }

    public static void LoadQuestsAndTasks(string unitPrefix = "", bool forceReload = false)
    {
        if (IsInitialized && forceReload == false)
            return;

        IsInitialized = true;
        (quests, tasks) = ContentLoader.ReturnListOfQuestsAndTasks(unitPrefix);
    }

    private static bool IsAnswerCorrect(string answer, string taskID)
    {
        if (CheatsManager.CheatingActivated)
            return true;

        TaskBase tempTask = GetTaskByID(taskID);

        if (tempTask == null)
            return false;

        return tempTask.CheckAnswer(answer ?? string.Empty);
    }

    private void Awake()
    {
        Instance = this;

#if UNITY_EDITOR
        LoadQuestsAndTasks();
#endif

    }
}
