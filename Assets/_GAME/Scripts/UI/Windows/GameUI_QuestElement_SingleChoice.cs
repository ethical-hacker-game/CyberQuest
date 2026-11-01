using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameUI_QuestElement_SingleChoice : GameUI_QuestElementBase
{
    [SerializeField] private List<ButtonExtended> optionButtons = new List<ButtonExtended>();

    public override void SetupQuestUIElement(QuestReferences _questRef, TaskReferences _taskRef, GameUI_QuestElementBase previousQuestwindow = null)
    {
        TaskSingleChoice task = (TaskSingleChoice)_taskRef.data;

        for (int i = 0; i < optionButtons.Count; i++)
        {
            if(i < task.GetAnswers().Count)
            {
                optionButtons[i].gameObject.SetActiveOptimized(true);
                optionButtons[i].TextTMP.text = task.GetAnswers()[i];
            }
            else
            {
                optionButtons[i].gameObject.SetActiveOptimized(false);
            }
        }

        base.SetupQuestUIElement(_questRef, _taskRef, previousQuestwindow);
    }
}
