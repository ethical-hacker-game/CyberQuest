using NUnit.Framework;
using NUnit.Framework.Interfaces;
using Sirenix.OdinInspector;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using WPM;

public class PlayerController : GameSystemComponent
{
    public static PlayerController Instance;
    private WorldMapGlobe globe;
    private List<QuestReferences> questReferences = new List<QuestReferences>();
    private List<TaskReferences> taskReferences = new List<TaskReferences>();
    
    private TaskReferences activeTask = null;
    private QuestReferences activeQuest = null;
    private UnitReferences activeUnit = null;

    private string lastQuestID = string.Empty;
    private bool unitCompletionSummaryShown;

    public TaskReferences ActiveTask { get => activeTask; private set => activeTask = value; }
    public QuestReferences ActiveQuest { get => activeQuest; private set => activeQuest = value; }
    public UnitReferences ActiveUnit { get => activeUnit; private set => activeUnit = value; }

    [ShowInInspector]
    public static PlayerInfo PlayerInfo => GameController.CurrentPlayerInfo;
    public static UnitInfo CurrentUnitInfo => Instance != null ? Instance.ActiveUnit?.info : null;
    public static SetupGameType SetupNewGame { get; set; } = SetupGameType.New;

    public UnityEvent OnProgressUpdate { get; set; } = new UnityEvent();

    public UnitScoreSummary LearningScore => UnitScoring.Calculate(ActiveUnit?.data, ActiveUnit?.info,
        QuestManager.GetQuestByID, QuestManager.GetTaskByID);

    #region Public Methods
    public bool CompleteActiveTask(int minigameScore = 0, bool done = true, bool skipped = false,
        string expectedTaskID = null)
    {
        if (ActiveTask == null || (!done && !skipped) ||
            (expectedTaskID != null && ActiveTask.taskID != expectedTaskID) ||
            !TaskScoring.Complete(ActiveTask.data, ActiveTask.info, minigameScore,
                skipped, CheatsManager.CheatingActivated))
            return false;

        // Currency is independent of learning points; keep the existing dialogue exception.
        if (skipped == false && ActiveTask.data.TaskType != TaskBase.TASK_TYPE.Dialogue)
            SD_MoenyLabel.ChangeMoney(400);

        MailsManager.Instance.RecieveMails(ActiveTask.data.mailsOnEnd);

        OnProgressUpdate.Invoke();

        if (ActiveQuest.info.IsDone)
            CompleteActiveQuest();

        GameController.SaveGame();
        return true;
    }

    public bool RecordWrongAnswer(string taskID)
    {
        if (ActiveTask?.info == null || ActiveTask.taskID != taskID || CheatsManager.CheatingActivated ||
            !TaskScoring.RecordWrongAnswer(ActiveTask.data, ActiveTask.info))
            return false;

        OnProgressUpdate.Invoke();
        GameController.SaveGame();
        return true;
    }

    private void CompleteActiveQuest()
    {
        if (ActiveQuest == null)
            return;

        lastQuestID = ActiveQuest.questID;
        ActiveQuest.info.timerStarted = false;

        OnProgressUpdate.Invoke();
        StickerManager.Instance.UpdateAllQuestStickers();
    }

    public void OpenQuest(string questID)
    {
        GameplayUIManager.ShowQuestsUI();
    }

    public void FlyToQuest(QuestReferences quest)
    {
        if(quest == null)
            return;
        
        int cityIndex = quest.data.cityIndex;   
        globe.FlyToCity(cityIndex, 1f);
    }

    public void HandleNextQuestOrTask(QuestReferences questRef, TaskReferences taskRef)
    {
        if (ActiveQuest != null)
            if (ActiveQuest.info.IsDone)
                CompleteActiveQuest();

        NextQuestAction action = GetNextQuestAction(questRef, taskRef);

        switch (action)
        {
            case NextQuestAction.allDone:
                ActiveUnit.info.isDone = true;
                GameController.SaveGame();
                ShowUnitCompletionSummary();
                //GameplayUIManager.ShowQuestsUI();
                //QuestsUI.Instance.ShowQuestDetails(questRef);
                break;
            case NextQuestAction.nextQuest:
                StartNextQuest();
                break;
            case NextQuestAction.nextTask:
                StartNextTask();
                break;
            case NextQuestAction.showProgressUI:
                GameplayUIManager.ShowQuestsUI();
                QuestsUI.Instance.ShowQuestDetails(questRef);
                break;
            case NextQuestAction.error:
                GameplayUIManager.ShowQuestsUI();
                QuestsUI.Instance.ShowQuestDetails(questRef);
                break;
            default:
                GameplayUIManager.ShowQuestsUI();
                QuestsUI.Instance.ShowQuestDetails(questRef);
                break;
        }
    }

    public void ResetProgressToQuestIndex(int questIndex, bool startImmediately)
    {
        if (ActiveUnit?.info?.quests == null || questIndex < 0 || questIndex >= ActiveUnit.info.quests.Count)
            return;

        string questID = ActiveUnit.info.quests[questIndex].questID;

        for (int i = ActiveUnit.info.quests.Count - 1; i >= questIndex; i--)
        {
            QuestInfo qInfo = ActiveUnit.info.quests[i];
            QuestReferences tempQuestRef = GetQuestReferencesByID(qInfo.questID);
            for (int t = 0; t < qInfo.tasks.Count; t++)
            {
                TaskInfo tInfo = qInfo.tasks[t];
                TaskReferences tempTaskRef = GetTaskReferencesByID(tInfo.taskID);
                if (tempTaskRef != null)
                {
                    if (tempTaskRef.info != null && tempTaskRef.info.decisionTaken >= 0)
                        DecisionsManager.ReverseDecision(tempTaskRef);
                    tempTaskRef.info = null;
                }
            }
            if (tempQuestRef != null)
            {
                if (tempQuestRef.window != null)
                    Destroy(tempQuestRef.window.gameObject);
                tempQuestRef.info = null;
                tempQuestRef.sticker?.UpdateSticker();
            }
            ActiveUnit.info.quests.RemoveAt(i);
        }
        ActiveUnit.info.isDone = false;
        unitCompletionSummaryShown = false;
        DecisionsManager.CancelPendingDecision();
        ActiveQuest = null;
        ActiveTask = null;
        OnProgressUpdate.Invoke();
        
        GameController.SaveGame();

        if (startImmediately)
        {
            StartQuest(questID);
        }
    }
    #endregion

    #region Getters
    public QuestReferences GetQuestReferencesByID(string questID)
    {
        for (int i = 0; i < questReferences.Count; i++)
        {
            if (questReferences[i].questID == questID)
                return questReferences[i];
        }

        return null;
    }

    public int GetQuestIndexByID(string questID)
    {
        if (ActiveUnit.data.questIds.Contains(questID) == false)
            return -1;

        return ActiveUnit.data.questIds.IndexOf(questID);
    }

    public TaskReferences GetTaskReferencesByID(string taskID)
    {
        for (int i = 0; i < taskReferences.Count; i++)
        {
            if (taskReferences[i].taskID == taskID)
                return taskReferences[i];
        }

        return null;
    }

    public UnitInfo GetUnitInfoByID(string unitID)
    {
        if (PlayerInfo == null || PlayerInfo.unit == null)
            return null;

        return PlayerInfo.unit.unitID == unitID ? PlayerInfo.unit : null;
    }

    public QuestInfo GetQuestInfoByID(string questID)
    {
        if (ActiveUnit == null)
            return null;

        for (int i = 0; i < ActiveUnit.info.quests.Count; i++)
        {
            if (questID == ActiveUnit.info.quests[i].questID)
                return ActiveUnit.info.quests[i];
        }

        return null;
    }

    public TaskReferences GetRecentTaskReferencesInQuest(QuestReferences questRef)
    {
        if (questRef == null)
        {
            LogPrompter.ShowError("questReferences.tasks.IsNullOrEmpty()", gameObject);
            return null;
        }

        if (questRef.tasks.IsNullOrEmpty())
        {
            LogPrompter.ShowError("questReferences.tasks.IsNullOrEmpty()", gameObject);
            return null;
        }

        TaskReferences recentTaskReferences = questRef.tasks[0];

        if (recentTaskReferences.info == null && SetupNewGame != SetupGameType.Load)
        {
            LogPrompter.ShowError("recentTaskReferences.info == null", gameObject);
            return recentTaskReferences;
        }

        for (int i = 0; i < questRef.tasks.Count; i++)
        {
            TaskInfo taskInfo = questRef.tasks[i].info;

            if (taskInfo == null)
            {
                break;
            }

            if (taskInfo.IsDone == false)
            {
                recentTaskReferences = questRef.tasks[i];
                break;
            }

            if(taskInfo.IsDone)
            {
                recentTaskReferences = questRef.tasks[i];
            }
        }

        return recentTaskReferences;
    }

    private NextQuestAction GetNextQuestAction(QuestReferences questRef, TaskReferences taskRef)
    {
        int finishedQuests = ActiveUnit.info.quests.Where(x => x.IsDone).Count();
        int allQuests = ActiveUnit.data.questIds.Count;

        if (finishedQuests == allQuests)
            return NextQuestAction.allDone;

        if (questRef.info.IsDone)
        {
            QuestReferences followingQuest = GetQuestReferencesByID(GetFollowingQuest(questRef.questID).questID);

            if (followingQuest == null)
                return NextQuestAction.showProgressUI;
            else
                return NextQuestAction.nextQuest;
        }

        int selectedTaskIndex = questRef.info.tasks.FindIndex(task => task.taskID == taskRef.taskID);
        int progress = questRef.info.tasks.Where(task => task.IsDone).Count();

        if (selectedTaskIndex + 1 != progress)
        {
            LogPrompter.ShowError("Selected Task is not the next one!", gameObject);
            return NextQuestAction.error;
        }

        return NextQuestAction.nextTask;
    }

    public (string, string) GetNextQuestButtonText(QuestReferences questRef, TaskReferences taskRef)
    {
        NextQuestAction action = GetNextQuestAction(questRef, taskRef);

        switch (action)
        {
            case NextQuestAction.allDone:
                return ("Finish Scenario", "Scenario Completed");
            case NextQuestAction.nextQuest:
                return ("Next Mission", "Quest Completed");
            case NextQuestAction.nextTask:
                return ("Continue", "Task Completed");
            case NextQuestAction.showProgressUI:
                return ("Show Available Quests", "Quest Completed");
            case NextQuestAction.error:
                return ("Error", "---");
            default:
                return ("Error", "---");
        }
    }

    private List<QuestReferences> GetUnlockedQuests()
    {
        List<QuestReferences> unlockedQuests = new List<QuestReferences>();

        for (int i = 0; i < questReferences.Count; i++)
        {
            if (questReferences[i].info == null)
                continue;

            if (questReferences[i].info.IsDone)
                continue;

            unlockedQuests.Add(questReferences[i]);
        }

        return unlockedQuests;
    }

    private Quest GetPreviousQuest(string questID)
    {
        int givenQuestIndex = ActiveUnit.data.questIds.IndexOf(questID);

        if (givenQuestIndex <= 0)
            return null;

        return QuestManager.GetQuestByID(ActiveUnit.data.questIds[givenQuestIndex - 1]);
    }

    private Quest GetFollowingQuest(string questID)
    {
        int givenQuestIndex = ActiveUnit.data.questIds.IndexOf(questID);

        if (givenQuestIndex < 0 || givenQuestIndex >= ActiveUnit.data.questIds.Count - 1)
            return null;

        return QuestManager.GetQuestByID(ActiveUnit.data.questIds[givenQuestIndex + 1]);
    }
    #endregion

    #region Lifecycle Methods

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        globe = WorldMapGlobe.instance;
        string status = "";

        switch (SetupNewGame)
        {
            case SetupGameType.New:
                status = "New Game";
                break;
            case SetupGameType.Load:
                status = "Loaded Game";
                break;
            default:
                status = "ERROR!!";
                break;
        }

        Debug.Log($"PlayerController: {status}");

        SetupContent();
        LoadMails();
        PrepareQuests();

        if (SetupNewGame == SetupGameType.New)
        {
            StartCoroutine(ShowEntryMessage());
        }
        else
        {
            AssignLoadedElementsToReferences();
            if (UnitScoring.Migrate(ActiveUnit.data, ActiveUnit.info,
                QuestManager.GetQuestByID, QuestManager.GetTaskByID))
                GameController.SaveGame();
            StartLoadedQuest();
        }

        StickerManager.Instance.UpdateAllQuestStickers();
        SetupNewGame = SetupGameType.None;
    }

    private void Update()
    {
        PlayerInfo.playTime += Time.deltaTime / 60f;

        UpdateActiveQuest();
    }

    private IEnumerator ShowEntryMessage()
    {
        yield return 0;

        if (ActiveUnit.data.unitEntryMessage.IsNullOrEmpty())
            yield break;

        yield return 0;

        ContentUI.SetupContentUI(ActiveUnit.data.unitName, null, ActiveUnit.data.GetUnitEntryMessage(), "Start Tutorial (Recommended)", "Skip Tutorial");
        ContentUI.ButtonEvents[0].AddListener(()=>
        {
            GameplayUIManager.ShowGameUI();
            StartQuest(ActiveUnit.data.questIds[0]);
            TutorialSequences.StartNewGameTutorialSequence();
        });
        ContentUI.ButtonEvents[1].AddListener(() =>
        {
            GameplayUIManager.ShowGameUI();
            StartQuest(ActiveUnit.data.questIds[0]);
        });
        ContentUI.DefaultButtonIndex = 0;
    }

    private void StartLoadedQuest()
    {
        List<QuestInfo> questInfos = ActiveUnit.info.quests;

        if (questInfos.IsNullOrEmpty())
        {
            StartNewGameAfterInvalidSave("The saved game does not contain any quests.");
            return;
        }

        string lastQuestId = null;
        int startRemovalIndex = -1;

        for (int i = 0; i < questInfos.Count; i++)
        {
            if (questInfos[i].timerStarted)
            {
                lastQuestId = questInfos[i].questID;
            }
            else if (questInfos[i].IsDone)
            {
                lastQuestId = questInfos[i].questID;
            }
            else
            {
                startRemovalIndex = i;
                break;
            }
        }

        if (startRemovalIndex >= 0)
            ResetProgressToQuestIndex(startRemovalIndex, false);

        if (string.IsNullOrEmpty(lastQuestId) || QuestManager.GetQuestByID(lastQuestId) == null)
        {
            StartNewGameAfterInvalidSave("The saved game does not contain a resumable quest compatible with the current unit content.");
            return;
        }

        StartQuest(lastQuestId);
    }

    private void StartNewGameAfterInvalidSave(string reason)
    {
        if (ActiveUnit == null || ActiveUnit.data == null || ActiveUnit.data.questIds.IsNullOrEmpty())
        {
            HandleLoadingError($"Cannot recover the saved game\n{reason}\nThe unit does not contain any quests.");
            return;
        }

        Debug.LogWarning($"{reason} Starting unit '{ActiveUnit.unitID}' from the first quest.");

        ActiveUnit.info = new UnitInfo
        {
            unitID = ActiveUnit.unitID,
            quests = new List<QuestInfo>()
        };
        PlayerInfo.unit = ActiveUnit.info;

        foreach (QuestReferences questReference in questReferences)
            questReference.info = null;

        foreach (TaskReferences taskReference in taskReferences)
            taskReference.info = null;

        ActiveQuest = null;
        ActiveTask = null;
        PersonasManager.Instance.CreatePersonas(ActiveUnit.info);
        SetupNewGame = SetupGameType.New;

        StartQuest(ActiveUnit.data.questIds[0]);
    }
    #endregion

    #region General Content
    private void SetupContent()
    {
        if (CanSetupContent() == false)
            return;

        ActiveUnit = new UnitReferences(GameController.SelectedUnit);

        if (SetupNewGame == SetupGameType.New)
        {
            ActiveUnit.info = new UnitInfo();
            ActiveUnit.info.unitID = ActiveUnit.unitID;
            ActiveUnit.info.quests = new List<QuestInfo>();
            PlayerInfo.unit = ActiveUnit.info;

            PersonasManager.Instance.CreatePersonas(ActiveUnit.info);
        }
        else if(SetupNewGame == SetupGameType.Load)
        {
            ActiveUnit.info = PlayerInfo.unit;
        }
        else
        {
            Debug.LogError("Wrong Setup Game Type!!");
        }
    }

    private void LoadMails()
    {
        MailsManager.LoadMails(ActiveUnit.unitID, true);
        SkillsManager.LoadSkills(ActiveUnit.unitID, true);
    }

    private bool CanSetupContent()
    {
        if (GameController.Instance == null)
        {
            HandleLoadingError("Cannot setup content\nGameController.Instance == null");
            return false;
        }

        if (SceneLoader.IsDevScene && CourseManager.Units.IsNullOrEmpty() == false)
            GameController.SetupNewGameplayWithoutReload(CourseManager.Units[0]);

        if (GameController.SelectedUnit == null)
        {
            HandleLoadingError("Cannot setup content\nGameController.SelectedUnit == null");
            return false;
        }

        return true;
    }
    #endregion

    #region Quests
    private void PrepareQuests()
    {
        QuestManager.LoadQuestsAndTasks(ActiveUnit.unitID, true);

        for (int i = 0; i < QuestManager.Quests.Count; i++)
        {
            Quest quest = QuestManager.Quests[i];
            QuestReferences newReferences = new QuestReferences(quest);
            newReferences.sticker = StickerManager.Instance.SpawnSticker(newReferences);
            questReferences.Add(newReferences);
            PrepareTasks(newReferences);
        }
    }

    private void StartNextQuest()
    {
        string nextQuestID = string.Empty;

        for (int i = 0; i < ActiveUnit.data.questIds.Count; i++)
        {
            if (ActiveUnit.info.quests.FirstOrDefault(obj => obj.questID == ActiveUnit.data.questIds[i]) == null)
            {
                StartQuest(ActiveUnit.data.questIds[i]);
                return;
            }
        }

        AssistantUI.Instance.SpawnInfoNotification("<b>Congratulations!</b>\nThat was the last quest!");
    }

    private void StartQuest(string questID)
    {
        QuestReferences questRef = GetQuestReferencesByID(questID);
        Quest questData = QuestManager.GetQuestByID(questID);
        QuestInfo newQuestInfo = null;
        string taskToStart = string.Empty;

        if (questData == null)
            throw new NullReferenceException("Quest Data cannot be null");

        List<QuestInfo> startedQuests = ActiveUnit.info.quests.Where(x => x.questID == questData.questID).ToList();

        if (startedQuests.Count > 1)
        {
            Debug.LogError("More than one started quests with the same ID!");
            ActiveUnit.info.quests.RemoveAll(q => q.questID == questData.questID);
            startedQuests = null;
        }

        //start as new
        if (startedQuests.IsNullOrEmpty())
        {
            newQuestInfo = new QuestInfo(questData.questID);
            questRef.info = newQuestInfo;
            questRef.info.persona = PersonasManager.Instance.CurrentPersonaIndex;
            ActiveUnit.info.quests.Add(newQuestInfo);

            AddTaskInfoToQuest(questRef);

            taskToStart = questRef.info.tasks[0].taskID;
        }
        //load if already started
        else
        {
            newQuestInfo = startedQuests[0];
            questRef.info = newQuestInfo;

            taskToStart = questRef.info.tasks[0].taskID;

            for (int i = 1; i < questRef.info.tasks.Count; i++)
            {
                if (questRef.info.tasks[i].IsDone)
                    taskToStart = questRef.info.tasks[i].taskID;
                else if (questRef.info.tasks[i].timerStarted)
                    taskToStart = questRef.info.tasks[i].taskID;
                else
                    break;
            }
        }

        ActiveQuest = questRef;
        ActiveQuest.info.timerStarted = true;
        PlayerInfo.currentMission = ActiveQuest.data.GetQuestName();

        TaskReferences taskRef = GetTaskReferencesByID(taskToStart);

        StartTask(questRef, taskRef);

        StickerManager.Instance.UpdateAllQuestStickers();
        FlyToQuest(ActiveQuest);
        OnProgressUpdate.Invoke();
    }

    public void StartTask(QuestReferences questRef, TaskReferences taskRef)
    {
        TaskBase taskData = taskRef.data;
        TaskInfo taskInfo = taskRef.info;

        ActiveTask = taskRef;

        bool scoringInitialized = TaskScoring.Initialize(taskData, taskInfo);

        if (taskInfo.IsDone == false)
            taskInfo.timerStarted = true;

        questRef.window = GameUI.Instance.SpawnQuestWindow(questRef, taskRef);

        if (questRef.info.IsDone == false)
            questRef.info.timerStarted = true;

        MailsManager.Instance.RecieveMails(ActiveTask.data.mailsOnStart);

        //dialogue tasks finalize immediately, so the player can continue right away
        if (taskData.TaskType == TaskBase.TASK_TYPE.Dialogue && taskInfo.IsDone == false)
        {
            CompleteActiveTask(0);
            questRef.window.UpdateAreas();
        }

        //an "unlock skill" action task can never fire its unlock event if the skill was already
        //unlocked earlier (e.g. bought before this task became active), which would otherwise
        //soft-lock the player - finalize the task immediately in that case
        if (taskData.TaskType == TaskBase.TASK_TYPE.Action && taskInfo.IsDone == false)
        {
            TaskActionVariant actionVariant = taskData.GetAdaptedTaskVariant() as TaskActionVariant;

            if (actionVariant != null
                && actionVariant.actionType == QuestActionsBridge.ACTION_TYPE.skill_unlocked
                && SkillsManager.IsUnlocked(actionVariant.actionTag))
            {
                CompleteActiveTask(0);
                questRef.window.UpdateAreas();
            }
        }

        OnProgressUpdate.Invoke();
        if (scoringInitialized && !taskInfo.IsDone)
            GameController.SaveGame();
    }

    private void UpdateActiveQuest()
    {
        if (ActiveQuest == null)
            return;

        if (ActiveQuest.info.timerStarted == false)
            return;

        if (ActiveQuest.info.IsDone == true)
            return;

        ActiveQuest.info.questTimer += Time.deltaTime / 60f;
    }
    #endregion

    #region Tasks
    private void PrepareTasks(QuestReferences questRef)
    {
        Quest questData = questRef.data;

        for (int i = 0; i < questData.taskIDs.Count; i++)
        {
            TaskBase task = QuestManager.GetTaskByID(questData.taskIDs[i]);
            TaskReferences newTaskRef = new TaskReferences(task, questRef);
            taskReferences.Add(newTaskRef);
            questRef.tasks.Add(newTaskRef);
        }
    }

    private void AddTaskInfoToQuest(QuestReferences questRef)
    {
        Quest questData = questRef.data;
        QuestInfo questInfo = questRef.info;

        for (int i = 0; i < questData.taskIDs.Count; i++)
        {
            TaskReferences taskRef = GetTaskReferencesByID(questData.taskIDs[i]);
            TaskInfo newTaskInfo = null;
            
            if(SetupNewGame == SetupGameType.Load)
                newTaskInfo = questInfo.tasks.Where(x => x.taskID == questData.taskIDs[i]).FirstOrDefault();

            if(newTaskInfo == null)
            { 
                newTaskInfo = new TaskInfo(questData.taskIDs[i]); 
                
                if(SetupNewGame == SetupGameType.Load)
                    Debug.LogError("Couldn't find existing TaskInfo for non-new game setup!");
            }

            if(SetupNewGame != SetupGameType.Load)
                questRef.info.tasks.Add(newTaskInfo);

            taskRef.info = newTaskInfo;
            taskRef.info.persona = PersonasManager.Instance.CurrentPersonaIndex;
        }
    }

    private void AssignLoadedElementsToReferences()
    {
        for (int iQ = 0; iQ < ActiveUnit.info.quests.Count; iQ++)
        {
            QuestInfo tempQI = ActiveUnit.info.quests[iQ];
            QuestReferences questRef = GetQuestReferencesByID(tempQI.questID);
            questRef.info = tempQI;

            for (int iT = 0; iT < tempQI.tasks.Count; iT++)
            {
                TaskInfo tempTI = tempQI.tasks[iT];
                TaskReferences taskRef = GetTaskReferencesByID(tempTI.taskID);
                taskRef.info = tempTI;
            }
        }
    }

    private void StartNextTask()
    {
        if (ActiveQuest.info.IsDone)
        {
            CompleteActiveQuest();
            return;
        }

        string nextTaskID = string.Empty;

        for (int i = 0; i < ActiveQuest.info.tasks.Count; i++)
        {
            if (ActiveQuest.info.tasks[i].IsDone)
                continue;

            nextTaskID = ActiveQuest.info.tasks[i].taskID;
            break;
        }

        if(nextTaskID.IsNullOrEmpty())
        {
            Debug.LogError("No next task found to start!");
            return;
        }

        TaskReferences taskRef = GetTaskReferencesByID(nextTaskID);
        StartTask(ActiveQuest, taskRef);
    }

    
    #endregion

    private void HandleLoadingError(string message)
    {
        Debug.LogError(message);
        SceneLoader.LoadSceneAsync(SceneLoader.MAIN_MENU_SCENE_INDEX);
    }

    private void ShowUnitCompletionSummary()
    {
        if (unitCompletionSummaryShown)
            return;

        unitCompletionSummaryShown = true;

        UnitScoreSummary learningScore = LearningScore;
        int completedTasks = learningScore.CompletedTasks;
        int totalTasks = learningScore.TotalTasks;
        string personaName = PersonasManager.Instance.CurrentPersonaName;
        string unitTime = PlayerInfo.playTime.MinutesFloatToTimeString();

        string content =
            $"Congratulations! You have completed <b>{ActiveUnit.data.unitName}</b>.\n\n" +
            $"<b>Final persona:</b> {personaName}\n" +
            $"<b>Tasks completed:</b> {completedTasks} / {totalTasks}\n" +
            $"<b>Learning points:</b> {learningScore.Points} / {learningScore.MaxPoints} ({learningScore.ScorePercent}%)\n" +
            $"<b>Time played:</b> {unitTime}";

        const string returnButtonText = "Return to Main Menu";

        ContentUI.SetupContentUI("Unit Complete!", null, content, returnButtonText);
        ContentUI.ButtonEvents[0].AddListener(ReturnAfterUnitCompletion);
        ContentUI.DefaultButtonIndex = 0;
    }

    private void ReturnAfterUnitCompletion()
    {
        SceneLoader.LoadSceneAsync(SceneLoader.MAIN_MENU_SCENE_INDEX);
    }

    public enum SetupGameType
    {
        None,
        New,
        Load
    }

    private enum NextQuestAction
    {
        allDone,
        nextQuest,
        nextTask,
        showProgressUI,
        error
    }
}
