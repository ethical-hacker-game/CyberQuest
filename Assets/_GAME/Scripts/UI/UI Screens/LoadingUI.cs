using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class LoadingUI : UI_Screen
{
    [SerializeField] private TextMeshProUGUI progressLabel;

    public override void ShowScreen()
    {
        base.ShowScreen();
        Update();
    }

    public override void HideScreen()
    {
        base.HideScreen();
    }

    private void Update()
    {
        progressLabel.text = SceneLoader.Progress + "%";
    }

}
