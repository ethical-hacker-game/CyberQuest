using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace DevTools
{
    public class CommandsUI : UI_Screen
    {
        [SerializeField] private RectTransform commandsParent;
        [SerializeField] private GameObject commadButtonPrefab;

        public override void Back()
        {
            DevEditorUIManager.ShowUnitEditorUI();
            base.Back();
        }

        public override void ShowScreen()
        {
            UpdateCommandsList();
            base.ShowScreen();
        }

        public override void HideScreen()
        {
            base.HideScreen();
        }

        public void AddCommand()
        {
            InputTextUI.SetupInputTextUI("New Command", string.Empty);
            InputTextUI.BackButtonEvent.AddListener(DevEditorUIManager.ShowCommandsUI);
            InputTextUI.ContinueButtonEvent.AddListener((string commandID) =>
            {
                Command commad = DevToolsManager.Instance.CreateCommand(commandID);

                if (commad != null)
                {
                    DevToolsManager.Instance.SetActvieCommand(commad);
                    DevEditorUIManager.ShowCommandEditorUI();
                }
                else
                {
                    DevEditorUIManager.HideAll();
                    ContentUI.SetupContentUI("Error", null, $"Command with this id already exists:\n{commandID}", "OK");
                    ContentUI.ButtonEvents[0].AddListener(DevEditorUIManager.ShowCommandsUI);
                }
            });
        }

        public void LoadCommand(string commandID)
        {
            DevToolsManager.Instance.SetActvieCommand(CommandLineManager.Commands[commandID]);
            DevEditorUIManager.ShowCommandEditorUI();
        }

        private void UpdateCommandsList()
        {
            int currentButtonsCount = commandsParent.childCount;

            for (int i = 0; i < CommandLineManager.Commands.Count; i++)
            {
                DevContentEditorButton button;

                if (i >= currentButtonsCount)
                    Instantiate(commadButtonPrefab, commandsParent);

                button = commandsParent.GetChild(i).GetComponent<DevContentEditorButton>();
                button.SetupButton(CommandLineManager.Commands.Values.ToList()[i]);
            }

            for (int i = currentButtonsCount; i > CommandLineManager.Commands.Count; i--)
            {
                commandsParent.GetChild(i - 1).gameObject.Destroy();
            }
        }
    }
}
