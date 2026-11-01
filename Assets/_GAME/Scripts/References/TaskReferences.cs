public class TaskReferences
{
    public string taskID;
    public TaskBase data;
    public TaskInfo info;
    public QuestReferences questReferences;
    public GameUI_QuestElementBase window => questReferences.window;
    public StickerQuest sticker => questReferences.sticker;

    public TaskReferences() { }

    public TaskReferences(TaskBase task, QuestReferences questReferences)
    {
        this.taskID = task.taskID;
        this.data = task;
        this.questReferences = questReferences;
    }
}
