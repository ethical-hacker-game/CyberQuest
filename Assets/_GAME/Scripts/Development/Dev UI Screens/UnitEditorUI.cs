using RuntimeInspectorNamespace;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

namespace DevTools
{
    public class UnitEditorUI : UI_Screen
    {
        [SerializeField] private RectTransform questsParent;
        [SerializeField] private GameObject questsButton;
        [SerializeField] private RuntimeInspector runtimeInspector;

        [Space]
        [SerializeField] private TextMeshProUGUI issuesTMP;

        private Unit ActiveUnit => DevToolsManager.ActiveUnit;

        public override void Back()
        {
            DevContentUtilities.SaveUnit(ActiveUnit);
            DevEditorUIManager.ShowUnitsUI();
            base.Back();
        }

        public override void ShowScreen()
        {
            UpdateQuestsList();
            runtimeInspector.Inspect(ActiveUnit);
            base.ShowScreen();
        }

        public override void HideScreen()
        {
            base.HideScreen();
        }

        public void EditPOIs()
        {
            DevEditorUIManager.ShowPOIsEditorUI();
        }

        public void EditQuestsTree()
        {
            DevEditorUIManager.ShowQuestsTreeEditorUI();
        }

        public void ClickCommands()
        {
            DevEditorUIManager.ShowCommandsUI();
        }

        public void ClickAppsEditor()
        {
            DevEditorUIManager.ShowAppsUI();
        }

        public void AddApp()
        {
            List<string> availableApps = AppsManager.Instance.Apps.Select(app => app.appID).Distinct().ToList();

            TextSelectionUI.SetupTextSelectionUI(availableApps, "");
            TextSelectionUI.BackButtonEvent.AddListener(DevEditorUIManager.ShowUnitEditorUI);
            TextSelectionUI.SelectStringEvent.AddListener((appID) =>
            {
                if (appID.IsNullOrEmpty() == false && ActiveUnit.appIDs.Contains(appID) == false)
                {
                    ActiveUnit.appIDs.Add(appID);
                    DevContentUtilities.SaveUnit(ActiveUnit);
                }

                DevEditorUIManager.ShowUnitEditorUI();
            });
        }

        public void AddQuest()
        {
            string prefix = $"{ActiveUnit.unitID}_";
            InputTextUI.SetupInputTextUI("New Quest", $"New Quest ID:\n{prefix}");
            InputTextUI.BackButtonEvent.AddListener(DevEditorUIManager.ShowUnitEditorUI);
            InputTextUI.ContinueButtonEvent.AddListener((string questID) =>
            {
                questID = prefix + questID;
                Quest quest = DevToolsManager.Instance.CreateQuest(questID);

                if (quest != null)
                {
                    ActiveUnit.questIds.Add(questID);
                    DevEditorUIManager.ShowUnitEditorUI();
                }
                else
                {
                    DevEditorUIManager.HideAll();
                    ContentUI.SetupContentUI("Error", null, $"Quest with this id already exists\n{questID}", "OK");
                    ContentUI.ButtonEvents[0].AddListener(DevEditorUIManager.ShowUnitEditorUI);
                }
            });
        }

        public void AddDecision()
        {
            string prefix = $"{ActiveUnit.unitID}_";
            InputTextUI.SetupInputTextUI("New Decision", $"New Decision ID:\n{prefix}");
            InputTextUI.BackButtonEvent.AddListener(DevEditorUIManager.ShowUnitEditorUI);
            InputTextUI.ContinueButtonEvent.AddListener((string questID) =>
            {
                questID = prefix + questID;
                Quest quest = DevToolsManager.Instance.CreateDecisionQuest(questID);

                if (quest != null)
                {
                    ActiveUnit.questIds.Add(questID);
                    DevEditorUIManager.ShowUnitEditorUI();
                }
                else
                {
                    DevEditorUIManager.HideAll();
                    ContentUI.SetupContentUI("Error", null, $"Decision with this id already exists\n{questID}", "OK");
                    ContentUI.ButtonEvents[0].AddListener(DevEditorUIManager.ShowUnitEditorUI);
                }
            });
        }

        public void LoadQuest(string questID)
        {
            DevToolsManager.Instance.SetActvieQuest(questID);
            DevEditorUIManager.ShowQuestEditorUI();
        }

        private void UpdateQuestsList()
        {
            int currentButtonsCount = questsParent.childCount;

            for (int i = 0; i < ActiveUnit.questIds.Count; i++)
            {
                DevContentEditorButton button;
                string questID = ActiveUnit.questIds[i];
                Quest tempQuest = QuestManager.GetQuestByID(questID);

                if (i >= currentButtonsCount)
                    Instantiate(questsButton, questsParent);

                button = questsParent.GetChild(i).GetComponent<DevContentEditorButton>();

                if (tempQuest == null)
                {
                    button.SetupButton($"Error\nQuest {questID} cannot be found.", questID);
                    button.CustomDeleteEvent.AddListener(() =>
                    {
                        ActiveUnit.questIds.RemoveAt(button.transform.GetSiblingIndex());
                        DevEditorUIManager.ShowUnitEditorUI();
                    });
                }
                else
                {
                    button.SetupButton(tempQuest, ActiveUnit);
                }
            }

            for (int i = currentButtonsCount; i > ActiveUnit.questIds.Count; i--)
            {
                questsParent.GetChild(i - 1).gameObject.Destroy();
            }
        }

        private void FixedUpdate()
        {
            bool validationGood = ActiveUnit.ValidateContent(out string message);
            issuesTMP.gameObject.SetActiveOptimized(!validationGood);

            if (issuesTMP.text == message)
                issuesTMP.text = message + "<size=1>.</size>";

            if (validationGood == false)
                issuesTMP.text = message;
        }
    }
}
