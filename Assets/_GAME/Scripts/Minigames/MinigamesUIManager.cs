using System;
using UnityEngine;
using Minigames;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

namespace Minigames
{
    public class MinigamesUIManager : GameSystemComponent
    {
        public static MinigamesUIManager Instance;

        [SerializeField] private RectTransform minigameUIsParent;
        [Space]
        [SerializeField] private MinigameMainUI mainUI;
        [SerializeField] private MinigameResultUI resultUI;
        [Space]
        [SerializeField, Range(0,1)] private float maxResultAlfa = 1f;

        public MinigameMainUI MainUI => mainUI;
        public MinigameUI ActiveMinigameUI {get; private set; }

        private float MaxTimer => MinigamesController.Instance.MaxTimer;
        private float Timer => MinigamesController.Instance.Timer;
        private bool ShowingResult => MinigamesController.Instance.ShowingResult;
        private bool FinalResult => MinigamesController.Instance.FinalResult;
        private MinigameBehaviour ActiveMinigame => MinigamesController.Instance.ActiveMinigame;

        private void Awake()
        {
            Instance = this;
            HideAllPanels();
        }

        public void SetupUI(string objectText, MinigameUI minigameUIPrefab)
        {
            mainUI.ShowScreen();
            mainUI.Setup(objectText);
            ActiveMinigameUI = Instantiate(minigameUIPrefab, minigameUIsParent);
            ActiveMinigameUI.name = minigameUIPrefab.name;
        }

        public void ShowResultPanel(bool result, int score, string summary)
        {
            resultUI.gameObject.SetActiveOptimized(true);
            resultUI.ShowResult(result, score, summary);
            UpdateTimer();
        }

        public void CloseUI()
        {
            HideAllPanels();

            if (ActiveMinigameUI != null)
                Destroy(ActiveMinigameUI.gameObject);
            
            ActiveMinigameUI = null;
        }

        public void UpdateTimer()
        {
            if (!ShowingResult)
            {
                string timeString = Mathf.Clamp(Timer, 0, int.MaxValue).ToString("#00.00");
                mainUI.UpdateTimerLabel(timeString);
                float tempTimer = Mathf.Clamp(Timer, 0, int.MaxValue) / MaxTimer;
                if (ActiveMinigame.ResultOnTimeout)
                    tempTimer = 1 - tempTimer;
                mainUI.UpdateTimerImage(tempTimer);
            }
            if (Timer < 0 && ShowingResult)
            {
                float tempAlfa = Mathf.Clamp01(-Timer * 3);
                resultUI.UpdateResultColors(tempAlfa * maxResultAlfa, tempAlfa);
            }
        }

        public void HideAllPanels()
        {
            mainUI.HideScreen();
            resultUI.HideResult();
        }
    }
}
