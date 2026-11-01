using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using RuntimeInspectorNamespace;
using WPM;
using UnityEngine.UI;
using TMPro;

namespace DevTools
{
    public class TaskEditorUI : UI_Screen
    {
        [SerializeField] private RuntimeInspector runtimeInspector;

        [Space]
        [SerializeField] private TextMeshProUGUI issuesTMP;

        [Space]
        [SerializeField] private RectTransform onStartMailsParent;
        [SerializeField] private RectTransform onEndMailsParent;
        [SerializeField] private GameObject mailButton;

        private TaskBase ActiveTask => DevToolsManager.ActiveTask;

        public override void Back()
        {
            DevContentUtilities.SaveTask(ActiveTask,
                DevToolsManager.ActiveUnit.unitID,
                DevToolsManager.ActiveQuest.questID);
            DevEditorUIManager.ShowQuestEditorUI();
            DevToolsManager.Instance.SetActvieTask("null");
            base.Back();
        }

        public override void ShowScreen()
        {
            UpdateMailsList(ActiveTask.mailsOnStart, onStartMailsParent);
            UpdateMailsList(ActiveTask.mailsOnEnd, onEndMailsParent);

            runtimeInspector.Inspect(ActiveTask);

            base.ShowScreen();
        }

        public override void HideScreen()
        {
            base.HideScreen();
        }

        public void AddMailOnStart()
        {
            AddMail("start", ActiveTask.mailsOnStart);
        }

        public void AddMailOnEnd()
        {
            AddMail("end", ActiveTask.mailsOnEnd);
        }

        public void AddMail(string mailPrefix, List<string> mailsList)
        {
            string prefix = $"{ActiveTask.taskID}_{mailPrefix}_";
            InputTextUI.SetupInputTextUI("New Mail", $"New Quest ID:\n{prefix}");
            InputTextUI.BackButtonEvent.AddListener(DevEditorUIManager.ShowTaskEditorUI);
            InputTextUI.ContinueButtonEvent.AddListener((string mailID) =>
            {
                mailID = prefix + mailID;
                MailData mail = DevToolsManager.Instance.CreateMail(mailID);

                if (mail != null)
                {
                    mail.parentTaskID = ActiveTask.taskID;
                    mailsList.Add(mail.mailID);
                    DevToolsManager.Instance.SetActvieMail(mailID);
                    DevEditorUIManager.ShowMailEditorUI();
                }
                else
                {
                    DevEditorUIManager.HideAll();
                    ContentUI.SetupContentUI("Error", null, $"Mail with this id already exists\n{mailID}", "OK");
                    ContentUI.ButtonEvents[0].AddListener(DevEditorUIManager.ShowTaskEditorUI);
                }
            });
        }

        private void UpdateMailsList(List<string> mailsIDs, RectTransform listParent)
        {
            int currentButtonsCount = listParent.childCount;

            for (int i = 0; i < mailsIDs.Count; i++)
            {
                DevContentEditorButton button;
                string mailID = mailsIDs[i];
                MailData tempMail = MailsManager.GetMailDataByID(mailID);

                if (i >= currentButtonsCount)
                    Instantiate(mailButton, listParent);

                button = listParent.GetChild(i).GetComponent<DevContentEditorButton>();

                if (tempMail == null)
                {
                    button.SetupButton($"Error\nMail {mailID} cannot be found.", mailID);
                    button.CustomDeleteEvent.AddListener(() =>
                    {
                        mailsIDs.RemoveAt(button.transform.GetSiblingIndex());
                        DevEditorUIManager.ShowTaskEditorUI();
                    });
                }
                else
                {
                    button.SetupButton(tempMail, ActiveTask);
                }
            }

            for (int i = currentButtonsCount; i > mailsIDs.Count; i--)
            {
                listParent.GetChild(i - 1).gameObject.Destroy();
            }
        }

        private void FixedUpdate()
        {
            bool validationGood = ActiveTask.ValidateContent(out string message);
            issuesTMP.gameObject.SetActiveOptimized(!validationGood);

            if (issuesTMP.text == message)
                issuesTMP.text = message + "<size=1>.</size>";

            if (validationGood == false)
                issuesTMP.text = message;
        }
    }
}
