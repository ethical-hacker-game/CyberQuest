using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using RuntimeInspectorNamespace;
using TMPro;

namespace DevTools
{
    public class AppScenarioEditorUI : UI_Screen
    {
        [SerializeField] private RectTransform stagesParent;
        [SerializeField] private GameObject stageButton;
        [SerializeField] private RuntimeInspector runtimeInspector;

        [Space]
        [SerializeField] private TextMeshProUGUI issuesTMP;

        private AppCore ActiveAppCore => DevToolsManager.ActiveAppCore;
        private AppScenario ActiveAppScenario => DevToolsManager.ActiveAppScenario;

        public override void Back()
        {
            DevContentUtilities.SaveAppScenario(ActiveAppScenario);
            DevEditorUIManager.ShowAppEditorUI();
            DevToolsManager.Instance.SetActvieAppScenario(null);
            base.Back();
        }

        public override void ShowScreen()
        {
            UpdateStagesList();
            runtimeInspector.Inspect(ActiveAppScenario);
            base.ShowScreen();
        }

        public override void HideScreen()
        {
            base.HideScreen();
        }

        public void AddStage()
        {
            InputTextUI.SetupInputTextUI("New Stage", $"New Stage ID:");
            InputTextUI.BackButtonEvent.AddListener(DevEditorUIManager.ShowAppScenarioEditorUI);
            InputTextUI.ContinueButtonEvent.AddListener((string stageID) =>
            {
                AppScenario.Stage appScenarioStage = DevToolsManager.Instance.CreateAppScenarioStage(ActiveAppScenario, stageID);

                if (appScenarioStage != null)
                {
                    DevEditorUIManager.ShowAppScenarioEditorUI();
                }
                else
                {
                    DevEditorUIManager.HideAll();
                    ContentUI.SetupContentUI("Error", null, $"Stage with this id already exists:\n{stageID}", "OK");
                    ContentUI.ButtonEvents[0].AddListener(DevEditorUIManager.ShowAppScenarioEditorUI);
                }
            });
        }

        public void LoadStage(string stageID)
        {
            DevToolsManager.Instance.SetActiveAppScenarioStage(ActiveAppScenario.GetStageByID(stageID));
            DevEditorUIManager.ShowAppScenarioStageEditorUI();
        }

        private void UpdateStagesList()
        {
            int currentButtonsCount = stagesParent.childCount;

            List<AppScenario.Stage> stages = ActiveAppScenario.stages;

            for (int i = 0; i < stages.Count; i++)
            {
                DevContentEditorButton button;

                if (i >= currentButtonsCount)
                    Instantiate(stageButton, stagesParent);

                button = stagesParent.GetChild(i).GetComponent<DevContentEditorButton>();
                button.SetupButton(ActiveAppScenario, stages[i]);
            }

            for (int i = currentButtonsCount; i > stages.Count; i--)
            {
                stagesParent.GetChild(i - 1).gameObject.Destroy();
            }
        }

        private void FixedUpdate()
        {
            bool validationGood = ActiveAppScenario.ValidateContent(out string message);
            issuesTMP.gameObject.SetActiveOptimized(!validationGood);

            if (issuesTMP.text == message)
                issuesTMP.text = message + "<size=1>.</size>";

            if (validationGood == false)
                issuesTMP.text = message;
        }
    }
}
