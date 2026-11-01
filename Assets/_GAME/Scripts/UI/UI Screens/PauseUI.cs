using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PauseUI : UI_Screen
{
    public override void Back()
    {
        GameplayUIManager.ShowGameUI();
        base.Back();
    }
}
