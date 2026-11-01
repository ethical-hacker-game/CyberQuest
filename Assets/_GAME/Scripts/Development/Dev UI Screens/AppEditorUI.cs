using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using RuntimeInspectorNamespace;
using TMPro;

namespace DevTools
{
    public class AppEditorUI : UI_Screen
    {
        [SerializeField] private RectTransform scenariosParent;
        [SerializeField] private GameObject scenarioButton;
        [SerializeField] private RuntimeInspector runtimeInspector;

        [Space]
        [SerializeField] private TextMeshProUGUI issuesTMP;

        private AppCore ActiveAppCore => DevToolsManager.ActiveAppCore;
        
        public override void Back()
        {
            DevContentUtilities.SaveApp(ActiveAppCore);
            DevEditorUIManager.ShowAppsUI();
            DevToolsManager.Instance.SetActvieAppCore(null);
            base.Back();
        }

        public override void ShowScreen()
        {
            UpdateScenariosList();
            runtimeInspector.Inspect(ActiveAppCore);
            base.ShowScreen();
        }

        public override void HideScreen()
        {
            base.HideScreen();
        }

        public void AddScenario()
        {
            InputTextUI.SetupInputTextUI("New Scenario", $"New Scenario ID:\n{ActiveAppCore.appID}_");
            InputTextUI.BackButtonEvent.AddListener(DevEditorUIManager.ShowAppEditorUI);
            InputTextUI.ContinueButtonEvent.AddListener((string appScenarioID) =>
            {
                AppScenario appScenario = DevToolsManager.Instance.CreateAppScenario(ActiveAppCore.appID, ActiveAppCore.appID + "_" + appScenarioID);

                if (appScenario != null)
                {
                    DevEditorUIManager.ShowAppEditorUI();
                }
                else
                {
                    DevEditorUIManager.HideAll();
                    ContentUI.SetupContentUI("Error", null, $"App scenario with this id already exists:\n{appScenarioID}", "OK");
                    ContentUI.ButtonEvents[0].AddListener(DevEditorUIManager.ShowAppEditorUI);
                }
            });
        }

        public void LoadScenario(string scenarioID)
        {
            DevToolsManager.Instance.SetActvieAppScenario(AppsManager.Instance.GetAppScenarioByID(scenarioID));
            DevEditorUIManager.ShowAppScenarioEditorUI();
        }

        public void CleanupUnusedImages()
        {
            ContentUI.SetupContentUI(
                "Delete unused app images?",
                null,
                $"This will delete image files from <b>{ActiveAppCore.appID}</b> that are not used by any scenario stage frame.",
                "Delete unused images",
                "Back");

            ContentUI.ButtonEvents[0].AddListener(() =>
            {
                DevContentUtilities.AppImageCleanupResult result = DevContentUtilities.DeleteUnusedAppImages(ActiveAppCore);

                ContentUI.SetupContentUI("Unused app images deleted", null, result.ToSummary(), "OK");
                ContentUI.ButtonEvents[0].AddListener(DevEditorUIManager.ShowAppEditorUI);
            });

            ContentUI.ButtonEvents[1].AddListener(DevEditorUIManager.ShowAppEditorUI);
        }

        private void UpdateScenariosList()
        {
            int currentButtonsCount = scenariosParent.childCount;

            List<AppScenario> scenarios = AppsManager.Instance.GetAppScenariosByAppCoreID(ActiveAppCore.appID);

            for (int i = 0; i < scenarios.Count; i++)
            {
                DevContentEditorButton button;

                if (i >= currentButtonsCount)
                    Instantiate(scenarioButton, scenariosParent);

                button = scenariosParent.GetChild(i).GetComponent<DevContentEditorButton>();
                button.SetupButton(scenarios[i], ActiveAppCore);
            }

            for (int i = currentButtonsCount; i > scenarios.Count; i--)
            {
                scenariosParent.GetChild(i - 1).gameObject.Destroy();
            }
        }

        private void FixedUpdate()
        {
            bool validationGood = ActiveAppCore.ValidateContent(out string message);
            issuesTMP.gameObject.SetActiveOptimized(!validationGood);

            if (issuesTMP.text == message)
                issuesTMP.text = message + "<size=1>.</size>";

            if (validationGood == false)
                issuesTMP.text = message;
        }
    }
}
