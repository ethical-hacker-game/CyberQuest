using System;

[Serializable]
public class TaskInfo
{
    public string taskID;
    public float taskTimer; //in minutes
    public int hintStage;
    public bool timerStarted = false;
    public bool isDone = false;
    public bool isSkipped = false;
    public bool isCheated = false;
    public bool hasHint = false;
    public int persona;
    public int decisionTaken = -1;
    public int score = 0;
    public int educationalPoints;
    public int maxEducationalPoints;
    public int wrongAnswerCount;
    public int scoringVersion;
    public bool scoreMigrated;

    public bool IsDone => isDone || isSkipped || isCheated;

    public TaskInfo(string taskID)
    {
        this.taskID = taskID ?? throw new ArgumentNullException(nameof(taskID));
        this.taskTimer = 0;
        this.hintStage = 0;
        this.timerStarted = false;
        this.isDone = false;
        this.hasHint = false;
        this.score = 0;
    }
}
