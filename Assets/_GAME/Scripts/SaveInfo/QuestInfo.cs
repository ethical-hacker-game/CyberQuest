using System.Collections.Generic;
using System;
using System.Linq;

[Serializable]
public class QuestInfo
{
    public string questID;
    public float questTimer; //in minutes
    public int hintStage;
    public bool timerStarted = false;
    public int persona;
    public int questDecision;
    public List<TaskInfo> tasks = new List<TaskInfo>();

    public bool IsDone => tasks.All(task => task.IsDone);

    public QuestInfo(string questID)
    {
        this.questID = questID ?? throw new ArgumentNullException(nameof(questID));
        this.questTimer = 0;
        this.hintStage = 0;
        this.timerStarted = false;
        this.tasks = new List<TaskInfo>();
    }

    public QuestInfo(string questID, float questTimer, int hintStage, bool timerStarted, bool isDone, List<TaskInfo> tasks)
    {
        UnityEngine.Debug.LogError("isDone jednak potrzebne????");
        this.questID = questID ?? throw new ArgumentNullException(nameof(questID));
        this.questTimer = questTimer;
        this.hintStage = hintStage;
        this.timerStarted = timerStarted;
        //this.isDone = isDone;
        this.tasks = tasks ?? throw new ArgumentNullException(nameof(tasks));
    }
}
