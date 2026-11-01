using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameUI_QuestElement_Dialogue : GameUI_QuestElementBase
{
    //dialogue tasks finalize immediately, so the story text must stay visible next to the continue button
    protected override void ToggleAreas(bool questCompleted)
    {
        base.ToggleAreas(questCompleted);
        DescriptionTMP.gameObject.SetActiveOptimized(true);
    }
}
