using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class GameUI_QuestElement_Action : GameUI_QuestElementBase
{
    [SerializeField] private TextMeshProUGUI actionTextTMP;

    public override void SetupQuestUIElement(QuestReferences _questRef, TaskReferences _taskRef, GameUI_QuestElementBase previousQuestwindow = null)
    {
        base.SetupQuestUIElement(_questRef, _taskRef, previousQuestwindow);
        actionTextTMP.text = QuestActionsBridge.GetActionText(_questRef, _taskRef);
    }

    public override void StartNextQuest()
    {
        QuestActionsBridge.ContinueLastAction();
        base.StartNextQuest();
    }
}
