using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class IndicatorManager : GameSystemComponent
{
    public static IndicatorManager Instance { get; private set; }

    public static Dictionary<string, IndicatorTag> IndicatorTags = new Dictionary<string, IndicatorTag>();

    private int currentTutorial = 0;
    private Action onTutorialClosed;

    public static void AddIndicator(IndicatorTag indicatorTag)
    {
        IndicatorTags.Add(indicatorTag.IndicatorID, indicatorTag);
    }

    public void ShowTutorial(string indicatorID, Action actionAfterTutorial)
    {
        onTutorialClosed = actionAfterTutorial;

        if (IndicatorTags.ContainsKey(indicatorID) == false)
        {
            Debug.LogError($"IndicatorManager: Indicator with ID {indicatorID} not found!");
            TutorialClosed();
            return;
        }

        IndicatorTutorialUI.SetupIndicatorTutorialUI(IndicatorTags[indicatorID]);
    }

    public void TutorialClosed()
    {
        onTutorialClosed?.Invoke();
    }

    //public void ShowNextTutorial()
    //{
    //    if(currentTutorial >= IndicatorTags.Count)
    //        currentTutorial = 0;

    //    IndicatorTutorialUI.SetupIndicatorTutorialUI(IndicatorTags.ElementAt(currentTutorial).Value);

    //    currentTutorial++;
    //}

    private void Awake()
    {
        Instance = this;
        IndicatorTags.Clear();
    }
}
