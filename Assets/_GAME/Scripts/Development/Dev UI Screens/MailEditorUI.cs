using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using RuntimeInspectorNamespace;
using TMPro;

namespace DevTools
{
    public class MailEditorUI : UI_Screen
    {
        [SerializeField] private RuntimeInspector runtimeInspector;

        [Space]
        [SerializeField] private TextMeshProUGUI issuesTMP;

        private MailData ActiveMail => DevToolsManager.ActiveMail;
        private Unit ActiveUnit => DevToolsManager.ActiveUnit;

        public override void Back()
        {
            DevContentUtilities.SaveMail(ActiveMail, ActiveUnit.unitID);
            DevEditorUIManager.ShowTaskEditorUI();
            DevToolsManager.Instance.SetActvieMail("null");
            base.Back();
        }

        public override void ShowScreen()
        {
            runtimeInspector.Inspect(ActiveMail);
            base.ShowScreen();
        }

        public override void HideScreen()
        {
            base.HideScreen();
        }

        private void FixedUpdate()
        {
            bool validationGood = ActiveMail.ValidateContent(out string message);
            issuesTMP.gameObject.SetActiveOptimized(!validationGood);

            if (issuesTMP.text == message)
                issuesTMP.text = message + "<size=1>.</size>";

            if (validationGood == false)
                issuesTMP.text = message;
        }
    }
}
