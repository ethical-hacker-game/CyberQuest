using System;
using System.Collections.Generic;

/// <summary>Educational scoring policy. Arcade scores and currency never enter this calculation.</summary>
public static class TaskScoring
{
    public const int Version = 1;

    private static readonly IReadOnlyDictionary<TaskBase.TASK_TYPE, int> DefaultPoints =
        new Dictionary<TaskBase.TASK_TYPE, int>
        {
            { TaskBase.TASK_TYPE.SingleChoice, 10 },
            { TaskBase.TASK_TYPE.TrueFalse, 10 },
            { TaskBase.TASK_TYPE.Input, 10 },
            { TaskBase.TASK_TYPE.Website, 10 },
            { TaskBase.TASK_TYPE.Decision, 10 },
            { TaskBase.TASK_TYPE.Action, 10 },
            { TaskBase.TASK_TYPE.Minigame, 10 },
            { TaskBase.TASK_TYPE.Dialogue, 0 }
        };

    public static int ResolveBasePoints(TaskBase task)
    {
        if (task == null)
            return 0;

        return task.points > 0 ? task.points :
            DefaultPoints.TryGetValue(task.TaskType, out int points) ? points : 0;
    }

    public static int GetMaxPoints(TaskBase task, TaskInfo info) =>
        info != null && info.scoringVersion > 0
            ? Math.Max(0, info.maxEducationalPoints)
            : ResolveBasePoints(task);

    public static int CalculatePoints(int basePoints, int wrongAnswers)
    {
        // Integer fifths avoid floating-point errors. Round up to retain at least 20%.
        long fifths = 5 - Math.Min(4, Math.Max(0, wrongAnswers));
        return (int)((Math.Max(0, basePoints) * fifths + 4) / 5);
    }

    public static int GetAvailablePoints(TaskBase task, TaskInfo info) =>
        CalculatePoints(GetMaxPoints(task, info), info?.wrongAnswerCount ?? 0);

    public static bool CanPenalize(TaskBase.TASK_TYPE type) =>
        type == TaskBase.TASK_TYPE.SingleChoice || type == TaskBase.TASK_TYPE.TrueFalse ||
        type == TaskBase.TASK_TYPE.Input || type == TaskBase.TASK_TYPE.Website;

    /// <summary>Freeze the base on first use; migrate completed legacy tasks without replaying rewards.</summary>
    public static bool Initialize(TaskBase task, TaskInfo info)
    {
        if (task == null || info == null || info.scoringVersion > 0)
            return false;

        info.maxEducationalPoints = ResolveBasePoints(task);
        info.wrongAnswerCount = 0;
        info.scoringVersion = Version;
        info.scoreMigrated = info.IsDone;
        info.educationalPoints = info.isDone && !info.isSkipped && !info.isCheated
            ? info.maxEducationalPoints : 0;
        return true;
    }

    public static bool RecordWrongAnswer(TaskBase task, TaskInfo info)
    {
        if (task == null || info == null || info.IsDone || !CanPenalize(task.TaskType))
            return false;

        Initialize(task, info);
        if (info.wrongAnswerCount < int.MaxValue)
            info.wrongAnswerCount++;
        return true;
    }

    public static bool Complete(TaskBase task, TaskInfo info, int arcadeScore = 0,
        bool skipped = false, bool cheated = false)
    {
        if (task == null || info == null || info.IsDone)
            return false;

        Initialize(task, info);
        info.timerStarted = false;
        info.isSkipped = skipped;
        info.isCheated = cheated;
        info.isDone = !skipped && !cheated;
        info.educationalPoints = skipped || cheated ? 0 : GetAvailablePoints(task, info);
        info.score = task.TaskType == TaskBase.TASK_TYPE.Minigame ? arcadeScore : 0;
        return true;
    }
}
