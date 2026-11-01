using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class QuestActionsBridge : GameSystemComponent
{
    private static IQuestActionContinue lastActionObject;

    public static QuestActionsBridge Instance { get; private set; }

    public static void CallQuestAction(IQuestActionContinue actionObject, ACTION_TYPE actionType, string actionTag, out bool waitForAction)
    {
        TaskReferences activeTask = PlayerController.Instance.ActiveTask;
        waitForAction = false;

        if (activeTask?.info == null || activeTask.info.IsDone)
            return;

        if(activeTask.data.TaskType != TaskBase.TASK_TYPE.Action)
            return;

        string codedAnswer = TaskAction.GetAnswerCoded(actionType, actionTag);

        bool isAnswerCorrect = activeTask.data.CheckAnswer(codedAnswer);

        if (isAnswerCorrect == false)
            return;

        waitForAction = true;
        lastActionObject = actionObject;
        PlayerController.Instance.CompleteActiveTask(expectedTaskID: activeTask.taskID);
        //quest window is visible all the time
        //activeTask.window.WindowFlexibilityComponent.MaximizeWindow();
        activeTask.window.UpdateAreas();
    }

    public static void ContinueLastAction()
    {
        if (lastActionObject == null)
            return;

        lastActionObject.Continue();
        lastActionObject = null;
    }

    public static string GetActionText(QuestReferences questRef, TaskReferences taskRef)
    {
        TaskBaseVariant taskVariant = taskRef.data.GetTaskVariant(-1);

        if(taskVariant.GetType() != typeof(TaskActionVariant))
            return "ERROR - Unknown Task Variant";

        ACTION_TYPE actionType = ((TaskActionVariant)taskVariant).actionType;

        switch (actionType)
        {
            case ACTION_TYPE.app_stage_changed:
            case ACTION_TYPE.app_stage_completed:
            case ACTION_TYPE.app_scenario_finished:
                return GetActionText_App(questRef, taskRef);

            case ACTION_TYPE.mail_opened:
                return "To continue open the mail.";

            case ACTION_TYPE.mail_source_copied:
                return "To continue copy the source from the mail.";

            case ACTION_TYPE.skill_unlocked:
                return "To continue unlock the skill.";

            default:
                return "ERROR - Unknown Action";
        }
    }

    private static string GetActionText_App(QuestReferences questRef , TaskReferences taskRef)
    {
        string appScenarioID = questRef.data.GetQuestVariant(-1).appScenarioID;
        AppCore app = AppsManager.Instance.GetAppByScenarioID(appScenarioID);

        if (app == null)
            return "ERROR - Unknown App";

        return $"To continue use <b>{app.header}</b> app.";
    }

    private void Awake()
    {
        Instance = this;
    }

    public enum ACTION_TYPE
    {
        None,
        app_stage_changed,
        app_stage_completed,
        app_scenario_finished,
        mail_opened,
        mail_source_copied,
        skill_unlocked
    }
}
