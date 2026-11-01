using Michsky.UI.Beam;
using Minigames;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.Serialization;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem.XR;
using UnityEngine.UI;

namespace Minigames
{
    public class MinigamesController : GameSystemComponent
    {
        public static MinigamesController Instance;
        [SerializeField] private List<MinigameBehaviour> minigames = new List<MinigameBehaviour>();

        private MinigameBehaviour activeMinigame;
        private bool finalResult;
        private bool showingResult = false;
        private float timer;
        private float maxTimer;

        private bool testMode = false;
        private TaskInfo minigameTaskInfo;
        public MinigameBehaviour ActiveMinigame { get => activeMinigame; }
        public bool FinalResult { get => finalResult; }
        public bool ShowingResult { get => showingResult; }
        public float Timer { get => timer; }
        public float MaxTimer { get => maxTimer; }

        public static bool isAnyMinigameActive
        {
            get
            {
                if (Instance == null)
                    return false;
                return Instance.activeMinigame == null ? false : true;
            }
        }

        public void SetupMinigame(MinigameType minigameType, string label = "", TaskMinigameVariant config = null)
        {
            if (PlayerController.PlayerInfo == null)
                return;
            if (minigameType == MinigameType.None)
                return;

            minigameTaskInfo = PlayerController.Instance?.ActiveTask?.info;
            testMode = false;

            SetActiveGameObjects(false);
            GameplayUIManager.HideAll();
            timer = 30;
            maxTimer = 30;
            for (int i = 0; i < minigames.Count; i++)
            {
                if (minigames[i].MinigameType == minigameType)
                {
                    activeMinigame = Instantiate(minigames[i]).GetComponent<MinigameBehaviour>();
                    activeMinigame.ApplyConfig(config);
                    label = label.IsNullOrEmpty() ? activeMinigame.InstructionsLabel : label;
                    activeMinigame.MinigameUIPrefab.gameObject.SetActive(true);
                    activeMinigame.MinigameUIPrefab.UpdateUI();
                    activeMinigame.PauseMinigame();
                    MinigamesUIManager.Instance.SetupUI(label, activeMinigame.MinigameUIPrefab);

                    timer = activeMinigame.GetMinigameDuration;
                    maxTimer = timer;

                    ContentUI.SetupContentUI(
                        activeMinigame.DescriptionHeader,
                        activeMinigame.DescriptionImage,
                        activeMinigame.Description,
                        "Continue");

                    ContentUI.ButtonEvents[0].AddListener(() =>
                    {
                        GameplayUIManager.HideAll();
                        activeMinigame.ResumeMinigame();
                    });

                    return;
                }
            }

            SetActiveGameObjects(true);
            GameplayUIManager.ShowGameUI();
            throw new Exception("Minigame type not found in the list " + minigameType.ToString());
        }

        public void SetupMinigameTest(MinigameType minigameType)
        {
            if (minigameType == MinigameType.None)
                return;

            SetActiveGameObjects(false);
            for (int i = 0; i < minigames.Count; i++)
            {
                if (minigames[i].MinigameType == minigameType)
                {
                    activeMinigame = Instantiate(minigames[i]).GetComponent<MinigameBehaviour>();
                    // Mirror the real flow: minigames may do one-time setup in ApplyConfig
                    // (e.g. Firewall builds its dropdown pools there). Null config is a
                    // safe no-op for every minigame and keeps prefab defaults.
                    activeMinigame.ApplyConfig(null);
                    activeMinigame.MinigameUIPrefab.gameObject.SetActive(true);
                    activeMinigame.MinigameUIPrefab.UpdateUI();
                    activeMinigame.PauseMinigame();
                    MinigamesUIManager.Instance.SetupUI(activeMinigame.InstructionsLabel, activeMinigame.MinigameUIPrefab);

                    timer = activeMinigame.GetMinigameDuration;
                    maxTimer = timer;

                    activeMinigame.ResumeMinigame();
                    testMode = true;
                    return;
                }
            }

            throw new Exception("Minigame type not found in the list");
        }

        public void FinishMinigame(bool result, int score, string summary)
        {
            if (showingResult)
                return;

            if(CheatsManager.CheatingActivated)
                result = true;

            showingResult = true;
            timer = 0;
            finalResult = result;
            activeMinigame.PauseMinigame();
            MinigamesUIManager.Instance.ShowResultPanel(result, score, summary);
            //SoundSystem.Instance.PlayMinigameResultSound();

            if (result)
            {
                if (testMode == false)
                    CompleteMinigameTask(score);
            }
        }

        private void CompleteMinigameTask(int score)
        {
            TaskReferences activeTask = PlayerController.Instance.ActiveTask;

            if (activeTask?.info == null || activeTask.info != minigameTaskInfo || activeTask.info.IsDone)
                return;

            if (activeTask.data.TaskType != TaskBase.TASK_TYPE.Minigame)
                return;

            activeTask.data.CheckAnswer("minigame");
            PlayerController.Instance.CompleteActiveTask(score, expectedTaskID: activeTask.taskID);
            //quest window is visible all the time
            //activeTask.window.WindowFlexibilityComponent.MaximizeWindow();
            activeTask.window.UpdateAreas();
        }

        public void CloseMinigame()
        {
            if (finalResult == true)
            {
                /*PlayerController.PlayerInfo.objects[objectIndex].doneResearches++;
                PlayerController.PlayerInfo.objects[objectIndex].recievedResearchPoins += 3;
                PlayerController.PlayerInfo.score += 45;
                PlayerController.PlayerInfo.conductedResearch++;
                PlayerController.PlayerInfo.researchPoints += PlayerController.GetManualResearchReward();
                switch (ActiveMinigame.GetType().ToString())
                {
                    case "Minigames.ConnectionsMinigame":
                        PlayerController.PlayerInfo.conducted_researches_def++;
                        PlayerController.PlayerInfo.AnalyticsInfo.conducted_researches_def++;
                        break;
                    case "Minigames.WavesMinigame":
                        PlayerController.PlayerInfo.conducted_researches_waves++;
                        PlayerController.PlayerInfo.AnalyticsInfo.conducted_researches_waves++;
                        break;
                    case "Minigames.TripMinigame":
                        PlayerController.PlayerInfo.conducted_researches_trip++;
                        PlayerController.PlayerInfo.AnalyticsInfo.conducted_researches_trip++;
                        break;
                    case "Minigames.MathMinigame":
                        PlayerController.PlayerInfo.conducted_researches_math++;
                        PlayerController.PlayerInfo.AnalyticsInfo.conducted_researches_math++;
                        break;
                    default:
                        Debug.Log("This minigame analytics is not supported!");
                        break;
                }
                AchievementsManager.Instance.CheckResearch();*/
            }
            if (testMode == false)
            {
                GameplayUIManager.ShowGameUI();
            }
            MinigamesUIManager.Instance.CloseUI();
            SetActiveGameObjects(true);
            Destroy(activeMinigame.gameObject);
            activeMinigame = null;
            showingResult = false;
            //SoundSystem.Instance.SetUpVolume();
        }

        private void Awake()
        {
            Instance = this;
            // UI panels now hidden by MinigamesUIManager
        }

        private void Start()
        {
            //only for tests
            //SetupTestMinigame(ObjectsManager.MinigameType.Waves);
        }

        private void Update()
        {
            UpdateTimer();
        }

        private void SetActiveGameObjects(bool status)
        {
            ObjectsHider.SetVisibility("hide_on_minigame", status);
        }

        private void UpdateTimer()
        {
            if (activeMinigame == null)
                return;

            if (activeMinigame.IsPaused && timer > 0)
                return;

            timer -= Time.deltaTime;
            MinigamesUIManager.Instance.UpdateTimer();

            if (timer < 0)
            {
                if (!showingResult)
                {
                    int score = activeMinigame.ResultOnTimeout ? activeMinigame.Score : 0;
                    FinishMinigame(activeMinigame.ResultOnTimeout, score, ActiveMinigame.Summary);
                }
            }
        }

        public static float GetTimerPositive
        {
            get
            {
                return Mathf.Clamp(Instance.timer, 0, int.MaxValue);
            }
        }


        [Serializable]
        public enum MinigameType
        {
            None = -1,
            Connections = 11,
            Waves = 21,
            NetworkDistance = 31,
            NetworkDevices = 32,
            NetworkTrace = 33,
            FirewallRuleset = 41,
            TraceIntruder = 51,
            StrengthForge = 61,
            DecryptIntercept = 71,
            SequenceOrder = 81,
            PacketRush = 91,
            PatchHunt = 101,
            DataRover = 111,
            SonarSweep = 121,
            CamBlindSpotAudit = 131,
            CamCleanDeskSweep = 132,
            CamBlindRoute = 133,
        }
    }
}
