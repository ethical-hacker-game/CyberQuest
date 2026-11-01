using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameUI_QuestElement_Website: GameUI_QuestElementBase
{
    public void CheckAnswerOnWebBrowser()
    {
        if(GameUI.Instance.WebBrowserWindow.WindowFlexibilityComponent.IsMaximized == false)
        {
            AssistantUI.Instance.SpawnErrorNotifiaction("You have to open the web browser first.");
            return;
        }

        CheckAnswer(GameUI.Instance.WebBrowserWindow.GetActiveWebsite());
    }
}
