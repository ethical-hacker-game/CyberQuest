using Minigames;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameUI_QuestElement_Minigame : GameUI_QuestElementBase
{
    public void StartMinigame()
    {
        if (MinigamesController.Instance.ActiveMinigame != null)
            return;

        if (PlayerController.Instance.ActiveTask.data.GetAdaptedTaskVariant().GetType() != typeof(TaskMinigameVariant))
        {
            Debug.LogError("Active task is not a minigame task.");
            AssistantUI.Instance.SpawnErrorNotifiaction("Active task is not a minigame task.");
            return;
        }

        TaskMinigameVariant minigameVariant = (TaskMinigameVariant)PlayerController.Instance.ActiveTask.data.GetAdaptedTaskVariant();

        if (minigameVariant.minigameType == MinigamesController.MinigameType.None)
        {
            Debug.LogError("Minigame type is not set.");
            AssistantUI.Instance.SpawnErrorNotifiaction("Minigame type is not set.");
            return;
        }

        MinigamesController.Instance.SetupMinigame(minigameVariant.minigameType, minigameVariant.minigameHeaderText, minigameVariant);
    }
}
