using System.Collections.Generic;

public class QuestReferences
{
    public string questID;
    public Quest data;
    public QuestInfo info;
    public GameUI_QuestElementBase window;
    public StickerQuest sticker;
    public List<TaskReferences> tasks = new List<TaskReferences>();

    public string AppScenario
    {
        get
        {
            QuestVariant quest = data.GetQuestVariant(-1);
            string scenario = quest.appScenarioID;

            if (scenario.IsNullOrEmpty() == false)
                return scenario;

            return "";
        }
    }

    public QuestReferences() { }
    
    public QuestReferences(Quest quest)
    {
        this.questID = quest.questID;
        this.data = quest;
    }
}
