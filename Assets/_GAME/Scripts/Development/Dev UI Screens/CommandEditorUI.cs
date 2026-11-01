using RuntimeInspectorNamespace;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace DevTools
{
    public class CommandEditorUI : UI_Screen
    {
        [SerializeField] private RuntimeInspector runtimeInspector;

        [Space]
        [SerializeField] private TextMeshProUGUI issuesTMP;

        private Command ActiveCommand => DevToolsManager.ActiveCommand;

        public override void Back()
        {
            DevContentUtilities.SaveCommand(ActiveCommand);
            DevEditorUIManager.ShowCommandsUI();
            DevToolsManager.Instance.SetActvieCommand(null);
            base.Back();
        }

        public override void ShowScreen()
        {
            runtimeInspector.Inspect(ActiveCommand);
            base.ShowScreen();
        }

        public override void HideScreen()
        {
            base.HideScreen();
        }

        private void FixedUpdate()
        {
            bool validationGood = ActiveCommand.ValidateContent(out string message);
            issuesTMP.gameObject.SetActiveOptimized(!validationGood);

            if (issuesTMP.text == message)
                issuesTMP.text = message + "<size=1>.</size>";

            if (validationGood == false)
                issuesTMP.text = message;
        }
    }
}
