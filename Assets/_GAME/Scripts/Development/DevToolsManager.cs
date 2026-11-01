using DevTools;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DevToolsManager : GameSystemComponent
{
    public static DevToolsManager Instance { get; private set; }
    public static Unit ActiveUnit { get; private set; }
    public static Quest ActiveQuest { get; private set; }
    public static TaskBase ActiveTask { get; private set; }
    public static MailData ActiveMail { get; private set; }
    public static Command ActiveCommand { get; private set; }
    public static POI ActivePOI { get; private set; }
    public static AppCore ActiveAppCore { get; private set; }
    public static AppScenario ActiveAppScenario { get; private set; }
    public static AppScenario.Stage ActiveAppScenarioStage { get; private set; }

    #region Units
    public void SetActvieUnit(Unit unit)
    {
        //ActiveUnit = unit.Clone();
        ActiveUnit = unit;

        // The editor has no GameController.SelectedUnit, so point content-path resolution
        // at the unit being edited and reload its unit-scoped content. Otherwise apps,
        // commands, web and Cyberpedia resolve to the whole units root (every unit's content).
        ContentPaths.EditorActiveUnitID = unit?.unitID;

        if (unit == null)
            return;

        if (AppsManager.Instance != null)
            AppsManager.Instance.ReloadApps();

        if (CommandLineManager.Instance != null)
            CommandLineManager.Instance.PrepareCommands();
    }

    public Unit CreateUnit(string unitID)
    {
        Unit newUnit = CourseManager.GetUnitByID(unitID);

        if (newUnit != null)
        {
            Debug.LogError($"Unit {unitID} already exists!");
            return null;
        }

        newUnit = new Unit()
        {
            unitID = unitID
        };

        CourseManager.Units.Add(newUnit);

        return newUnit;
    }

    public void DeleteUnit(string unitID)
    {
        Unit unitToDelete = CourseManager.GetUnitByID(unitID);

        if (unitToDelete == null)
            return;

        DevContentUtilities.DeleteUnit(unitToDelete);
        CourseManager.Units.Remove(unitToDelete);
    }
    #endregion

    #region Quests
    public void SetActvieQuest(string questID)
    {
        Quest quest = QuestManager.GetQuestByID(questID);

        if (quest == null)
        {
            ActiveQuest = null;
            return;
        }

        //ActiveQuest = quest.Clone(QuestManager.GetQuestType(quest.QuestType.ToString()));
        ActiveQuest = quest;
    }

    public Quest CreateQuest(string questID)
    {
        Quest newQuest = QuestManager.GetQuestByID(questID);

        if (newQuest != null)
        {
            Debug.LogError($"Quest {questID} already exists!");
            return null;
        }

        newQuest = new Quest();
        newQuest.questID = questID;
        QuestManager.Quests.Add(newQuest);

        return newQuest;
    }

    public Quest CreateDecisionQuest(string questID)
    {
        Quest newQuest = CreateQuest(questID);

        if (newQuest == null)
            return null;

        newQuest.decisionQuest = true;
        newQuest.universalPersonaContent = true;
        newQuest.variants = new List<QuestVariant> { new QuestVariant() };

        TaskDecision decisionTask = (TaskDecision)CreateTask(questID + "_decision", TaskBase.TASK_TYPE.Decision, questID);
        decisionTask.universalPersonaContent = true;
        decisionTask.AddVariant();

        newQuest.taskIDs.Add(decisionTask.taskID);

        DevContentUtilities.SaveQuest(newQuest, ActiveUnit.unitID);
        DevContentUtilities.SaveTask(decisionTask, ActiveUnit.unitID, newQuest.questID);

        return newQuest;
    }

    public void DeleteQuest(string questID)
    {
        Quest questToDelete = QuestManager.GetQuestByID(questID);

        if (questToDelete == null)
            return;

        ActiveUnit.questIds.Remove(questID);
        DevContentUtilities.DeleteQuest(questToDelete, ActiveUnit.unitID);
        QuestManager.Quests.Remove(questToDelete);
    }
    #endregion

    #region Tasks
    public void SetActvieTask(string taskID)
    {
        TaskBase task = QuestManager.GetTaskByID(taskID);

        if (task == null)
        {
            ActiveTask = null;
            return;
        }

        //ActiveTask = task.Clone(TaskManager.GetTaskType(task.TaskType.ToString()));
        ActiveTask = task;
    }

    public TaskBase CreateTask(string taskID, TaskBase.TASK_TYPE taskType, string parentQuestID)
    {
        TaskBase newTask = QuestManager.GetTaskByID(taskID);

        if (newTask != null)
        {
            Debug.LogError($"Task {taskID} already exists!");
            return null;
        }

        switch (taskType)
        {
            case TaskBase.TASK_TYPE.SingleChoice:
                newTask = new TaskSingleChoice();
                break;

            case TaskBase.TASK_TYPE.TrueFalse:
                newTask = new TaskTrueFalse();
                break;

            case TaskBase.TASK_TYPE.Input:
                newTask = new TaskInput();
                break;

            case TaskBase.TASK_TYPE.Website:
                newTask = new TaskWebsite();
                break;

            case TaskBase.TASK_TYPE.Decision:
                newTask = new TaskDecision();
                break;

            case TaskBase.TASK_TYPE.Action:
                newTask = new TaskAction();
                break;

            case TaskBase.TASK_TYPE.Minigame:
                newTask = new TaskMinigame();
                break;

            case TaskBase.TASK_TYPE.Dialogue:
                newTask = new TaskDialogue();
                break;

            default:
                Debug.LogError($"Creating TaskBase.TASK_TYPE {taskType} is not implemented!");
                return null;
        }

        newTask.taskID = taskID;
        newTask.parentQuestID = parentQuestID;
        QuestManager.Tasks.Add(newTask);

        return newTask;
    }

    public void DeleteTask(string taskID)
    {
        TaskBase taskToDelete = QuestManager.GetTaskByID(taskID);

        if (taskToDelete == null)
            return;

        ActiveQuest.taskIDs.Remove(taskID);
        DevContentUtilities.DeleteTask(taskToDelete, ActiveUnit.unitID, ActiveQuest.questID);
        QuestManager.Tasks.Remove(taskToDelete);
    }
    #endregion

    #region Commands
    public void SetActvieCommand(Command command)
    {
        ActiveCommand = command;
    }

    public Command CreateCommand(string coreCommand)
    {
        if (CommandLineManager.Commands.ContainsKey(coreCommand))
        {
            Debug.LogError($"Command {coreCommand} already exists!");
            return null;
        }

        Command newCommand = new Command()
        {
            command = coreCommand
        };

        CommandLineManager.Commands.Add(coreCommand, newCommand);
        return newCommand;
    }

    public void DeleteCommand(string questID)
    {
        Command commandToDelete;

        if (CommandLineManager.Commands.TryGetValue(questID, out commandToDelete) == false)
            return;

        DevContentUtilities.DeleteCommand(commandToDelete);
        CommandLineManager.Commands.Remove(questID);
    }
    #endregion

    #region POIs
    public void SetActviePOI(POI poi)
    {
        ActivePOI = poi;
    }
    #endregion

    #region Mails
    public void SetActvieMail(string mailID)
    {
        MailData mail = MailsManager.GetMailDataByID(mailID);

        if (mail == null)
        {
            ActiveMail = null;
            return;
        }

        ActiveMail = mail;
    }

    public MailData CreateMail(string mailID)
    {
        MailData newMail = MailsManager.GetMailDataByID(mailID);

        if (newMail != null)
        {
            Debug.LogError($"Mail {mailID} already exists!");
            return null;
        }

        newMail = new MailData();
        newMail.mailID = mailID;
        MailsManager.Mails.Add(newMail);

        return newMail;
    }

    public void DeleteMail(string mailID)
    {
        MailData mailToDelete = MailsManager.GetMailDataByID(mailID);

        if (mailToDelete == null)
            return;

        ActiveTask.mailsOnStart.Remove(mailID);
        ActiveTask.mailsOnEnd.Remove(mailID);
        DevContentUtilities.DeleteMail(mailToDelete, ActiveUnit.unitID);
        MailsManager.Mails.Remove(mailToDelete);
    }
    #endregion

    #region Apps
    //AppCore
    public void SetActvieAppCore(AppCore appCore)
    {
        ActiveAppCore = appCore;
    }

    public AppCore CreateAppCore(string appCoreID)
    {
        AppCore newAppCore = AppsManager.Instance.GetAppByID(appCoreID);

        if (newAppCore != null)
        {
            Debug.LogError($"App {appCoreID} already exists!");
            return null;
        }

        newAppCore = new AppCore()
        {
            appID = appCoreID
        };

        AppsManager.Instance.Apps.Add(newAppCore);

        return newAppCore;
    }

    public void DeleteAppCore(string appCoreID)
    {
        AppCore appToDelete = AppsManager.Instance.GetAppByID(appCoreID);

        if (appToDelete == null)
            return;

        DevContentUtilities.DeleteApp(appToDelete);
        AppsManager.Instance.Apps.Remove(appToDelete);
    }

    //AppScenario
    public void SetActvieAppScenario(AppScenario appScenario)
    {
        ActiveAppScenario = appScenario;
    }

    public AppScenario CreateAppScenario(string appCoreID, string appScenarioID)
    {
        AppScenario newAppScenario = AppsManager.Instance.GetAppScenarioByID(appScenarioID);

        if (newAppScenario != null)
        {
            Debug.LogError($"App scenario {appScenarioID} already exists!");
            return null;
        }

        newAppScenario = new AppScenario()
        {
            scenarioID = appScenarioID,
            appID = appCoreID
        };

        AppsManager.Instance.Scenarios.Add(newAppScenario);

        return newAppScenario;
    }

    public void DeleteAppScenario(string appScenarioID)
    {
        AppScenario appScenarioToDelete = AppsManager.Instance.GetAppScenarioByID(appScenarioID);

        if (appScenarioToDelete == null)
            return;

        DevContentUtilities.DeleteAppScenario(appScenarioToDelete);
        AppsManager.Instance.Scenarios.Remove(appScenarioToDelete);
    }

    //AppStage
    public void SetActiveAppScenarioStage(AppScenario.Stage appScenarioStage)
    {
        ActiveAppScenarioStage = appScenarioStage;
    }

    public AppScenario.Stage CreateAppScenarioStage(AppScenario appScenario, string appScenarioStageID)
    {
        AppScenario.Stage newAppScenarioStage = appScenario.GetStageByID(appScenarioStageID);

        if (newAppScenarioStage != null)
        {
            Debug.LogError($"App scenario stage {appScenarioStageID} already exists!");
            return null;
        }

        newAppScenarioStage = new AppScenario.Stage()
        {
            stageID = appScenarioStageID
        };

        appScenario.stages.Add(newAppScenarioStage);

        return newAppScenarioStage;
    }

    public void RemoveFramesRangeFromStage(AppScenario.Stage appScenarioStage, int frameIndex, int frameCount)
    {
        List<AppScenario.Frame> frames = ActiveAppScenarioStage.frames;

        if (frameIndex < 0)
        {
            Debug.LogError("RemoveFramesRangeFromStage: frameIndex < 0");
            return;
        }
        if (frameIndex >= frames.Count)
        {
            Debug.LogError($"RemoveFramesRangeFromStage: frameIndex ({frameIndex}) >= frames.Count ({frames.Count})");
            return;
        }
        if (frameCount <= 0)
        {
            Debug.LogError("RemoveFramesRangeFromStage: frameCount <= 0");
            return;
        }
        if (frameIndex + frameCount > frames.Count)
        {
            Debug.LogError($"RemoveFramesRangeFromStage: frameIndex + frameCount ({frameIndex + frameCount}) > frames.Count ({frames.Count})");
            return;
        }

        frames.RemoveRange(frameIndex, frameCount);
    }

    public AppScenario.Stage DivideAppScenarioStage(AppScenario appScenario, AppScenario.Stage appScenarioStage, string newStageID, int frameIndex)
    {
        AppScenario.Stage newStage = appScenario.GetStageByID(newStageID);

        if (newStage != null)
        {
            Debug.LogError($"App scenario stage {newStageID} already exists!");
            return null;
        }

        newStage = new AppScenario.Stage()
        {
            stageID = newStageID,
            customHeader = appScenarioStage.customHeader,
            frames = new List<AppScenario.Frame>(),
            links = new List<AppScenario.Link>()
        };

        newStage.frames.AddRange(appScenarioStage.frames.GetRange(frameIndex, appScenarioStage.frames.Count - frameIndex));
        appScenarioStage.frames.RemoveRange(frameIndex, appScenarioStage.frames.Count - frameIndex);
        appScenario.stages.Insert(appScenario.stages.IndexOf(appScenarioStage) + 1, newStage);
        newStage.PresetupStage(appScenario.scenarioID, appScenario.ScenarioPath, appScenario.extension);

        return newStage;
    }

    public void DeleteAppScenarioStage(string appScenarioID, string appScenarioStageID)
    {
        AppScenario appScenario = AppsManager.Instance.GetAppScenarioByID(appScenarioID);

        if (appScenario == null)
            return;

        AppScenario.Stage stageToDelete = appScenario.GetStageByID(appScenarioStageID);

        if (stageToDelete == null)
            return;

        appScenario.stages.Remove(stageToDelete);
    }
    #endregion

    private void Awake()
    {
        Instance = this;
    }
}
