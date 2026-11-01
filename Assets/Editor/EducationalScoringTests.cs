using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Runs against the real game models and Unity JSON serializer, without entering Play Mode.
/// Editor: Tools → CyberQuest → Verify Educational Scoring.
/// Batch: Unity with -executeMethod EducationalScoringTests.RunAll -quit.
/// Fixtures are the local Assets/_CONTENT/units graphs. When the curriculum changes,
/// update the expected task counts and point maxima in VerifyContentAndExport on purpose.
/// The run writes output/scoring/verification.txt. It checks points, maxima, percent and
/// the wrong-answer penalty through UnitScoring and TaskScoring.
/// </summary>
public static class EducationalScoringTests
{
    private static int assertions;
    private const string Output = "output/scoring";

    [MenuItem("Tools/CyberQuest/Verify Educational Scoring")]
    public static void RunAll()
    {
        if (EditorApplication.isPlaying)
            throw new InvalidOperationException("Run scoring verification outside Play Mode.");

        assertions = 0;
        Directory.CreateDirectory(Output);
        try
        {
            VerifyPolicy();
            VerifySaveAndCompletion();
            VerifyAggregationAndMigration();
            VerifyLifecycleGuards();
            VerifyPrefabs();
            VerifyContentAndExport();
            string result = $"PASS: {assertions} assertions. Policy, persistence, migration, aggregation, prefab wiring and six content graphs verified.";
            File.WriteAllText(Path.Combine(Output, "verification.txt"), result);
            Debug.Log("[EducationalScoringTests] " + result);
        }
        catch (Exception error)
        {
            File.WriteAllText(Path.Combine(Output, "verification.txt"), "FAIL: " + error);
            Debug.LogException(error);
            if (Application.isBatchMode)
                EditorApplication.Exit(1);
            throw;
        }
    }

    private static void VerifyPolicy()
    {
        foreach (TaskBase.TASK_TYPE type in Enum.GetValues(typeof(TaskBase.TASK_TYPE)))
        {
            if (type == TaskBase.TASK_TYPE.None)
                continue;
            TaskBase task = CreateTask(type);
            int expected = type == TaskBase.TASK_TYPE.Dialogue ? 0 : 10;
            Require(TaskScoring.ResolveBasePoints(task) == expected, type + " default");
            task.points = 25;
            Require(TaskScoring.ResolveBasePoints(task) == 25, type + " override");
            task.points = 0;
            Require(TaskScoring.ResolveBasePoints(task) == expected, type + " zero fallback");
            task.points = -1;
            Require(TaskScoring.ResolveBasePoints(task) == expected, type + " invalid fallback");
            var info = new TaskInfo(task.taskID);
            bool penalized = TaskScoring.RecordWrongAnswer(task, info);
            Require(penalized == (type == TaskBase.TASK_TYPE.SingleChoice || type == TaskBase.TASK_TYPE.TrueFalse ||
                type == TaskBase.TASK_TYPE.Input || type == TaskBase.TASK_TYPE.Website), type + " penalty eligibility");
        }
        int[] expectedPoints = { 10, 8, 6, 4, 2, 2, 2 };
        for (int errors = 0; errors < expectedPoints.Length; errors++)
            Require(TaskScoring.CalculatePoints(10, errors) == expectedPoints[errors], "Penalty step " + errors);
        Require(TaskScoring.CalculatePoints(7, 1) == 6, "Round custom base up");
        Require(TaskScoring.CalculatePoints(7, 100) == 2, "Round minimum up");
        Require(TaskScoring.CalculatePoints(1, 100) == 1, "Small positive base");
        Require(TaskScoring.CalculatePoints(0, 100) == 0, "Zero base");
        Require(TaskScoring.CalculatePoints(int.MaxValue, 0) == int.MaxValue, "No integer overflow");
        Require(TaskScoring.CalculatePoints(10, int.MaxValue) == 2, "Large error count");
        Require(TaskScoring.ResolveBasePoints(JsonConvert.DeserializeObject<TaskInput>("{}")) == 10, "Missing JSON points");
        Require(TaskScoring.ResolveBasePoints(JsonConvert.DeserializeObject<TaskInput>("{\"points\":0}")) == 10, "JSON zero points");
        Require(TaskScoring.ResolveBasePoints(JsonConvert.DeserializeObject<TaskInput>("{\"points\":35}")) == 35, "JSON override points");
    }

    private static void VerifySaveAndCompletion()
    {
        TaskBase task = CreateTask(TaskBase.TASK_TYPE.Input);
        var info = new TaskInfo(task.taskID);
        Require(TaskScoring.RecordWrongAnswer(task, info), "First wrong answer recorded");
        Require(TaskScoring.RecordWrongAnswer(task, info), "Second wrong answer recorded");
        info = JsonUtility.FromJson<TaskInfo>(JsonUtility.ToJson(info));
        task.points = 100;
        Require(info.wrongAnswerCount == 2 && TaskScoring.GetAvailablePoints(task, info) == 6, "Reload keeps errors and frozen base");
        Require(TaskScoring.Complete(task, info), "First completion accepted");
        Require(info.educationalPoints == 6 && info.maxEducationalPoints == 10, "Award after errors");
        Require(!TaskScoring.Complete(task, info, 9000), "Duplicate completion ignored");
        Require(!TaskScoring.RecordWrongAnswer(task, info), "Late answer ignored");
        Require(info.educationalPoints == 6 && info.score == 0, "Duplicate did not change scores");

        var skipped = new TaskInfo(task.taskID);
        TaskScoring.Complete(task, skipped, skipped: true);
        Require(skipped.IsDone && skipped.educationalPoints == 0, "Skip advances without points");
        var cheated = new TaskInfo(task.taskID);
        TaskScoring.Complete(task, cheated, cheated: true);
        Require(cheated.IsDone && cheated.educationalPoints == 0, "Cheat advances without points");

        TaskBase minigame = CreateTask(TaskBase.TASK_TYPE.Minigame);
        var arcade = new TaskInfo(minigame.taskID);
        Require(!TaskScoring.RecordWrongAnswer(minigame, arcade), "Minigame loss is not an educational penalty");
        TaskScoring.Complete(minigame, arcade, 9876);
        Require(arcade.score == 9876 && arcade.educationalPoints == 10, "Arcade score is independent");
        var loaded = JsonUtility.FromJson<TaskInfo>(JsonUtility.ToJson(arcade));
        Require(loaded.score == 9876 && loaded.educationalPoints == 10, "Both scores persist");
    }

    private static void VerifyAggregationAndMigration()
    {
        var unit = new Unit { questIds = new List<string> { "q", "q" } };
        var quest = new Quest { questID = "q", taskIDs = new List<string> { "a", "b", "c", "a" } };
        TaskBase a = CreateTask(TaskBase.TASK_TYPE.Input); a.taskID = "a";
        TaskBase b = CreateTask(TaskBase.TASK_TYPE.SingleChoice); b.taskID = "b";
        TaskBase c = CreateTask(TaskBase.TASK_TYPE.Dialogue); c.taskID = "c";
        var tasks = new Dictionary<string, TaskBase> { { "a", a }, { "b", b }, { "c", c } };
        Func<string, Quest> findQuest = id => id == "q" ? quest : null;
        Func<string, TaskBase> findTask = id => tasks.TryGetValue(id, out TaskBase task) ? task : null;
        var saved = new UnitInfo { quests = new List<QuestInfo> { new QuestInfo("q") } };
        TaskInfo old = JsonUtility.FromJson<TaskInfo>("{\"taskID\":\"a\",\"isDone\":true,\"score\":99}");
        saved.quests[0].tasks.Add(old);
        Require(UnitScoring.Migrate(unit, saved, findQuest, findTask), "Legacy migration ran");
        Require(old.scoreMigrated && old.educationalPoints == 10 && old.score == 99, "Legacy award preserves arcade field");
        Require(!UnitScoring.Migrate(unit, saved, findQuest, findTask), "Migration is idempotent");
        foreach (string flag in new[] { "isSkipped", "isCheated" })
        {
            TaskInfo legacy = JsonUtility.FromJson<TaskInfo>("{\"taskID\":\"a\",\"isDone\":true,\"" + flag + "\":true}");
            TaskScoring.Initialize(a, legacy);
            Require(legacy.educationalPoints == 0 && legacy.scoreMigrated, "Legacy " + flag + " gives zero");
        }
        saved.quests[0].tasks.Add(new TaskInfo("orphan") { isDone = true, educationalPoints = 999 });
        UnitScoreSummary summary = UnitScoring.Calculate(unit, saved, findQuest, findTask);
        Require(summary.Points == 10 && summary.MaxPoints == 20 && summary.TotalTasks == 3, "Graph deduplicates and excludes orphan save");
        Require(summary.ScorePercent == 50 && summary.ProgressPercent == 33, "Score and progress differ");
        var skipped = new TaskInfo("b"); TaskScoring.Complete(b, skipped, skipped: true);
        saved.quests[0].tasks.Add(skipped);
        summary = UnitScoring.Calculate(unit, saved, findQuest, findTask);
        Require(summary.MaxPoints == 20 && summary.Points == 10 && summary.CompletedTasks == 2, "Skipped task remains in denominator");
        Require(new UnitScoreSummary(1, 8, 0, 0).ScorePercent == 13, "Percent rounds halves up");
        Require(new UnitScoreSummary(0, 0, 0, 0).ScorePercent == 0, "Zero denominator");
        saved.quests.Clear();
        UnitScoreSummary reset = UnitScoring.Calculate(unit, saved, findQuest, findTask);
        Require(reset.Points == 0 && reset.MaxPoints == summary.MaxPoints && reset.ScorePercent == 0,
            "Reset clears points and percent and keeps the unit maximum");
        Require(reset.CompletedTasks == 0 && reset.TotalTasks == summary.TotalTasks,
            "Reset clears completion and keeps the task count");

        var penalized = new TaskInfo("a");
        Require(TaskScoring.RecordWrongAnswer(a, penalized), "Wrong answer is recorded for unit scoring");
        Require(TaskScoring.Complete(a, penalized), "Completion after one wrong answer");
        var penalizedSave = new UnitInfo { quests = new List<QuestInfo> { new QuestInfo("q") } };
        penalizedSave.quests[0].tasks.Add(penalized);
        UnitScoreSummary penalty = UnitScoring.Calculate(unit, penalizedSave, findQuest, findTask);
        Require(penalty.MaxPoints == 20 && penalty.Points == 8 && penalty.ScorePercent == 40,
            "One wrong answer lowers unit points and percent without changing the maximum");
    }

    private static void VerifyPrefabs()
    {
        var progress = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_GAME/Prefabs/UI/Screens/Progress_UI.prefab");
        var serialized = new SerializedObject(progress.GetComponent<ProgressUI>());
        Require(serialized.FindProperty("learningScoreTMP").objectReferenceValue != null, "Progress score label is wired");
        foreach (string type in new[] { "Action", "Decision", "Dialogue", "Input", "Minigame", "SingleChoice", "TrueFalse", "Website" })
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/_GAME/Prefabs/UI/Quests/QuestUIElement_{type}.prefab");
            GameUI_QuestElements elements = prefab.GetComponent<GameUI_QuestElements>();
            Require(elements != null && elements.QuestionLineTMP != null && elements.CompleteInfoTMP != null, type + " score text areas are wired");
        }
    }

    private static void VerifyLifecycleGuards()
    {
        PlayerController previousPlayer = PlayerController.Instance;
        Minigames.MinigamesController previousMinigames = Minigames.MinigamesController.Instance;
        var host = new GameObject("ScoringVerification") { hideFlags = HideFlags.HideAndDontSave };
        host.SetActive(false);
        try
        {
            PlayerController player = host.AddComponent<PlayerController>();
            PlayerController.Instance = player;
            Require(!player.RecordWrongAnswer(null), "Missing active task ignores wrong answer");
            TaskBase task = CreateTask(TaskBase.TASK_TYPE.Minigame);
            var quest = new Quest { questID = "q", taskIDs = new List<string> { task.taskID } };
            var questInfo = new QuestInfo("q");
            var questRef = new QuestReferences(quest) { info = questInfo };
            var taskRef = new TaskReferences(task, questRef) { info = new TaskInfo(task.taskID) };
            questInfo.tasks.Add(taskRef.info);
            SetField(player, "activeTask", taskRef);
            Require(!player.CompleteActiveTask(expectedTaskID: "old_task"), "Stale completion targets are ignored");
            Require(!QuestManager.CheckAnswer("answer", "old_task"), "Stale answer targets are ignored");
            Require(!taskRef.info.IsDone, "Rejected callbacks do not complete current task");

            var minigames = host.AddComponent<Minigames.MinigamesController>();
            SetField(minigames, "minigameTaskInfo", new TaskInfo(task.taskID));
            typeof(Minigames.MinigamesController).GetMethod("CompleteMinigameTask", BindingFlags.NonPublic | BindingFlags.Instance)
                .Invoke(minigames, new object[] { 9999 });
            Require(!taskRef.info.IsDone && taskRef.info.score == 0, "Old minigame attempt cannot complete a new attempt of the same task");

            TaskScoring.Complete(task, taskRef.info, 9999);
            Require(!player.CompleteActiveTask(), "Controller rejects duplicate rewards");
            var unitInfo = new UnitInfo { quests = new List<QuestInfo> { questInfo }, isDone = true };
            SetField(player, "activeUnit", new UnitReferences { info = unitInfo });
            SetField(player, "questReferences", new List<QuestReferences> { questRef });
            SetField(player, "taskReferences", new List<TaskReferences> { taskRef });
            player.ResetProgressToQuestIndex(0, false);
            Require(unitInfo.quests.Count == 0 && !unitInfo.isDone, "Rewind removes scored progress");
            Require(taskRef.info == null && questRef.info == null && player.ActiveTask == null, "Rewind removes old attempt references");

            typeof(DecisionsManager).GetField("decisionActive", BindingFlags.NonPublic | BindingFlags.Static).SetValue(null, true);
            DecisionsManager.CancelPendingDecision();
            Require(!(bool)typeof(DecisionsManager).GetField("decisionActive", BindingFlags.NonPublic | BindingFlags.Static).GetValue(null), "Pending decisions clear on restart");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(host);
            PlayerController.Instance = previousPlayer;
            Minigames.MinigamesController.Instance = previousMinigames;
        }
    }

    private static void SetField(object target, string name, object value) =>
        target.GetType().GetField(name, BindingFlags.NonPublic | BindingFlags.Instance).SetValue(target, value);

    private static void VerifyContentAndExport()
    {
        int[] expectedMaxima = { 210, 960, 510, 840, 770, 590 };
        int totalTasks = 0;
        var report = new List<object>();
        for (int i = 0; i < 6; i++)
        {
            string id = $"ismg6860_u{i:00}";
            string root = "Assets/_CONTENT/units/" + id;
            Unit unit = JsonConvert.DeserializeObject<Unit>(File.ReadAllText(root + "/" + id + ".unit"));
            var quests = new Dictionary<string, Quest>();
            var tasks = new Dictionary<string, TaskBase>();
            var saved = new UnitInfo { unitID = id };
            foreach (string questId in unit.questIds)
            {
                string folder = root + "/quests/" + questId + "/";
                Quest quest = JsonConvert.DeserializeObject<Quest>(File.ReadAllText(folder + questId + ".quest"));
                quests.Add(questId, quest);
                var questInfo = new QuestInfo(questId);
                saved.quests.Add(questInfo);
                foreach (string taskId in quest.taskIDs)
                {
                    string json = File.ReadAllText(folder + taskId + ".task");
                    string type = (string)JObject.Parse(json)["TaskType"];
                    TaskBase task = (TaskBase)JsonConvert.DeserializeObject(json, QuestManager.GetTaskType(type));
                    tasks.Add(taskId, task);
                    Require(task.points >= 0 && task.taskID == taskId, taskId + " content score and ID");
                    var info = new TaskInfo(taskId);
                    TaskScoring.Complete(task, info);
                    questInfo.tasks.Add(info);
                }
            }
            Func<string, Quest> findQuest = key => quests.TryGetValue(key, out Quest value) ? value : null;
            Func<string, TaskBase> findTask = key => tasks.TryGetValue(key, out TaskBase value) ? value : null;
            UnitScoreSummary summary = UnitScoring.Calculate(unit, saved, findQuest, findTask);
            Require(summary.MaxPoints == expectedMaxima[i] && summary.Points == expectedMaxima[i], id + " maximum");
            Require(summary.ScorePercent == 100 && summary.ProgressPercent == 100, id + " full completion");
            Require(TryScoreWithOneWrongAnswer(tasks, out TaskBase penalizedTask, out TaskInfo penaltyInfo, out int pointsLost),
                id + " wrong answer lowers task points");
            UnitInfo penalizedSave = JsonUtility.FromJson<UnitInfo>(JsonUtility.ToJson(saved));
            bool replaced = false;
            foreach (string questId in unit.questIds)
            {
                Quest quest = findQuest(questId);
                if (quest?.taskIDs == null || !quest.taskIDs.Contains(penalizedTask.taskID))
                    continue;
                QuestInfo questInfo = penalizedSave.quests.Find(candidate => candidate.questID == questId);
                int index = questInfo.tasks.FindIndex(taskInfo => taskInfo.taskID == penalizedTask.taskID);
                Require(index >= 0, id + " penalized task is in the save");
                questInfo.tasks[index] = penaltyInfo;
                replaced = true;
                break;
            }
            Require(replaced, id + " penalty is applied on the scored quest");
            UnitScoreSummary penalizedSummary = UnitScoring.Calculate(unit, penalizedSave, findQuest, findTask);
            Require(penalizedSummary.MaxPoints == summary.MaxPoints, id + " penalty keeps the unit maximum");
            Require(penalizedSummary.Points == summary.Points - pointsLost, id + " penalty lowers unit points");
            Require(penalizedSummary.ScorePercent ==
                new UnitScoreSummary(penalizedSummary.Points, penalizedSummary.MaxPoints, 0, 0).ScorePercent,
                id + " percent follows the penalized points");
            report.Add(new { unit = id, tasks = summary.TotalTasks, points = summary.Points, maxPoints = summary.MaxPoints, scorePercent = summary.ScorePercent });
            totalTasks += summary.TotalTasks;
        }
        Require(totalTasks == 421, "Reachable task count");
        File.WriteAllText(Path.Combine(Output, "content-report.json"), JsonConvert.SerializeObject(report, Formatting.Indented));
    }

    private static bool TryScoreWithOneWrongAnswer(Dictionary<string, TaskBase> tasks, out TaskBase task,
        out TaskInfo info, out int pointsLost)
    {
        foreach (TaskBase candidate in tasks.Values)
        {
            info = new TaskInfo(candidate.taskID);
            if (!TaskScoring.CanPenalize(candidate.TaskType) ||
                !TaskScoring.RecordWrongAnswer(candidate, info) ||
                !TaskScoring.Complete(candidate, info))
                continue;

            pointsLost = TaskScoring.GetMaxPoints(candidate, info) - info.educationalPoints;
            if (pointsLost <= 0)
                continue;

            task = candidate;
            return true;
        }

        task = null;
        info = null;
        pointsLost = 0;
        return false;
    }

    private static TaskBase CreateTask(TaskBase.TASK_TYPE type)
    {
        TaskBase task = (TaskBase)Activator.CreateInstance(QuestManager.GetTaskType(type.ToString()));
        task.taskID = "test_" + type;
        return task;
    }

    private static void Require(bool condition, string message)
    {
        assertions++;
        if (!condition)
            throw new InvalidOperationException("Scoring assertion failed: " + message);
    }
}
