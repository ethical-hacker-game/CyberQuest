using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class TutorialSequences
{
    private static readonly Dictionary<string, string[]> Tutorials = new Dictionary<string, string[]>
    {
        {
            "NewGame", new string[]
            {
                "newgame_globe",
                "newgame_globe_quest",
                "newgame_quest_ui",
                "newgame_quest_links",
                "newgame_actions_ui",
                "newgame_webbrowser",
                "newgame_mail",
                "newgame_cmd",
                "newgame_statsui",
            }
        },
    };

    private static int ActiveTutorialStage { get; set; }
    private static string[] ActiveTutorialTags { get; set; }
    private static Action ActiveTutorialFinishAciton { get; set; }

    public static void StartTutorialSequence(string tutorialName, Action finishAction)
    {
        if (!Tutorials.ContainsKey(tutorialName))
            return;

        StartTutorialSequence(Tutorials[tutorialName], finishAction);
    }

    public static void StartNewGameTutorialSequence()
    {
        GameUI.Instance.MinimizeAllWindows();

        ActiveTutorialTags = Tutorials["NewGame"];
        ActiveTutorialFinishAciton = new Action(() =>
        {
            PlayerController.Instance.ActiveQuest.info.questTimer = 0;
            PlayerController.Instance.ActiveTask.info.taskTimer = 0;
            GameplayUIManager.ShowGameUI();
            GameUI.Instance.MaximizeAllWindows();
        });

        StartTutorialSequence(ActiveTutorialTags, ActiveTutorialFinishAciton);
    }

    private static void StartTutorialSequence(string[] tags, Action finishAciton)
    {
        ActiveTutorialStage = -1;
        ActiveTutorialTags = tags;
        ActiveTutorialFinishAciton = finishAciton;

        ShowNextStage();
    }

    private static void ShowNextStage()
    {
        ActiveTutorialStage++;
        if (ActiveTutorialStage >= ActiveTutorialTags.Length)
        {
            ActiveTutorialFinishAciton?.Invoke();
            return;
        }
        string tag = ActiveTutorialTags[ActiveTutorialStage];
        IndicatorManager.Instance.ShowTutorial(tag, ShowNextStage);
    }
}
