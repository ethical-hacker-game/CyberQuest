using System;
using System.Collections.Generic;

public readonly struct UnitScoreSummary
{
    public readonly long Points;
    public readonly long MaxPoints;
    public readonly int CompletedTasks;
    public readonly int TotalTasks;

    public int ScorePercent => Percent(Points, MaxPoints);
    public int ProgressPercent => Percent(CompletedTasks, TotalTasks);

    public UnitScoreSummary(long points, long maxPoints, int completedTasks, int totalTasks)
    {
        Points = points;
        MaxPoints = maxPoints;
        CompletedTasks = completedTasks;
        TotalTasks = totalTasks;
    }

    private static int Percent(long value, long maximum) => maximum <= 0 ? 0 :
        (int)Math.Min(100, Math.Max(0, Math.Round(100m * value / maximum, MidpointRounding.AwayFromZero)));
}

/// <summary>Traverses the playable graph, ignoring orphan saves and duplicate task references.</summary>
public static class UnitScoring
{
    public static UnitScoreSummary Calculate(Unit unit, UnitInfo info,
        Func<string, Quest> findQuest, Func<string, TaskBase> findTask)
    {
        long points = 0;
        long maximum = 0;
        int completed = 0;
        int total = 0;
        VisitTasks(unit, info, findQuest, findTask, (task, saved) =>
        {
            int taskMaximum = TaskScoring.GetMaxPoints(task, saved);
            maximum += taskMaximum;
            total++;
            if (saved == null || !saved.IsDone)
                return;

            completed++;
            if (saved.isDone && !saved.isSkipped && !saved.isCheated)
                points += Math.Min(taskMaximum, Math.Max(0, saved.educationalPoints));
        });
        return new UnitScoreSummary(points, maximum, completed, total);
    }

    public static void VisitTasks(Unit unit, UnitInfo info, Func<string, Quest> findQuest,
        Func<string, TaskBase> findTask, Action<TaskBase, TaskInfo> visit)
    {
        if (unit?.questIds == null)
            return;

        var visited = new HashSet<string>(StringComparer.Ordinal);
        foreach (string questId in unit.questIds)
        {
            Quest quest = findQuest(questId);
            if (quest?.taskIDs == null)
                continue;

            QuestInfo savedQuest = info?.quests?.Find(candidate => candidate?.questID == questId);
            foreach (string taskId in quest.taskIDs)
            {
                if (string.IsNullOrEmpty(taskId) || !visited.Add(taskId))
                    continue;
                TaskBase task = findTask(taskId);
                if (task != null)
                    visit(task, savedQuest?.tasks?.Find(candidate => candidate?.taskID == taskId));
            }
        }
    }

    public static bool Migrate(Unit unit, UnitInfo info, Func<string, Quest> findQuest,
        Func<string, TaskBase> findTask)
    {
        bool changed = false;
        VisitTasks(unit, info, findQuest, findTask, (task, saved) =>
        {
            if (saved != null && (saved.IsDone || saved.timerStarted))
                changed |= TaskScoring.Initialize(task, saved);
        });
        return changed;
    }
}
