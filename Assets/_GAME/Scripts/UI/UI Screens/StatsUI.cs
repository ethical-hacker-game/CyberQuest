using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class StatsUI : UI_Screen
{
    public static StatsUI Instance => GameplayUIManager.Instance.StatsUI;

    [SerializeField] private TextMeshProUGUI playerNameTMP;
    [SerializeField] private TextMeshProUGUI timerTMP;
    [SerializeField] private TextMeshProUGUI questsProgressTMP;

    [SerializeField] private List<GameObject> buttons;
    [SerializeField] private GameObject tutorialButton;

    private void Start()
    {
        UpdatePlayerNameLabel();
        UpdateProgressLabel();
        PlayerController.Instance.OnProgressUpdate.AddListener(UpdateProgressLabel);
    }

    private void Update()
    {
        timerTMP.text = System.DateTime.Now.ToString("tt hh:mm");
        //if(PlayerController.Instance.ActiveQuest == null)
        //{
        //    timerTMP.text = "--:--";
        //    return;
        //}
        //timerTMP.text = PlayerController.Instance.ActiveQuest.info.questTimer.MinutesFloatToTimeString();
    }

    private void UpdatePlayerNameLabel()
    {
        if (playerNameTMP == null)
            return;

        playerNameTMP.text = PlayerText.CurrentName;
    }

    private void UpdateProgressLabel()
    {
        if (PlayerController.Instance == null)
            return;

        UnitReferences activeUnit = PlayerController.Instance.ActiveUnit;
        int startedQuests = 0;
        int allQuestsCount = 0;

        if (activeUnit != null)
        {
            startedQuests = activeUnit.info.quests.Where(q => q.IsDone || q.timerStarted).Count();
            //startedQuests = activeUnit.info.quests.Count();
            allQuestsCount = activeUnit.data.questIds.Count;
        }

        questsProgressTMP.text = $"{startedQuests}/{allQuestsCount}";
    }

    public void ToggleButtons(bool active)
    {
        for (int i = 0; i < buttons.Count; i++)
        {
            buttons[i].SetActiveOptimized(active);
        }

        tutorialButton.SetActiveOptimized(GameplayUIManager.Instance.CurrentUIScreen?.TutorialTag.IsNullOrEmpty() == false);
    }

    public void ShowCyberpedia()
    {
        CyberpediaManager.Instance.OpenLibrary();
    }

    public void ShowLeaderboard()
    {
        GameplayUIManager.ShowLeaderboardUI();
    }

    public void ShowSettings()
    {
        GameplayUIManager.ShowSettingsUI();
    }

    public void ShowProgress()
    {
        GameplayUIManager.ShowProgressUI();
    }

    public void ShowQuests()
    {
        GameplayUIManager.ShowQuestsUI();
    }

    public void ShowMarketplace()
    {
        GameplayUIManager.ShowMarketplaceUI();
    }

    public void ShowTutorial()
    {
        string tutorialTag = GameplayUIManager.Instance.CurrentUIScreen.TutorialTag;

        if (tutorialTag.IsNullOrEmpty() == false)
            TutorialSequences.StartTutorialSequence(tutorialTag, IndicatorTutorialUI.Instance.HideScreen);
    }

    public void Exit()
    {
        GameplayUIManager.ShowPauseUI();
    }
}
