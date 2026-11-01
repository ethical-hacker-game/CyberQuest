using DevTools;
using Michsky.UI.Beam;
using RuntimeInspectorNamespace;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;

public class MailsUI : UI_Screen, IQuestActionContinue
{
    [SerializeField] private Transform mailButtonsParent;
    [SerializeField] private GameObject mailButtonPrefab;
    [SerializeField] private TextMeshProUGUI mailContentTMP;
    [SerializeField] private LayoutGroupFix layoutGroupFix;
    [SerializeField] private GameObject mailOptionsButton;

    private UnitInfo ActiveUnitInfo => PlayerController.Instance.ActiveUnit.info;

    private int selectedMailIndex = -1;

    public override void Back()
    {
        GameplayUIManager.ShowGameUI();
        base.Back();
    }

    public override void ShowScreen()
    {
        UpdateMailsList();

        if (ActiveUnitInfo.mails.IsNullOrEmpty())
        {
            mailContentTMP.text = "<align=\"center\">Your inbox is empty.";
            mailOptionsButton.SetActiveOptimized(false);
            selectedMailIndex = -1;
        }
        else
        {
            ShowMail(ActiveUnitInfo.mails.Count - 1, false);
            mailOptionsButton.SetActiveOptimized(true);
        }

        base.ShowScreen();
    }

    public override void HideScreen()
    {
        base.HideScreen();
    }

    public void ShowMail(int mailIndex, bool updateList = true)
    {
        selectedMailIndex = mailIndex;
        ActiveUnitInfo.mails[mailIndex].read = true;
        mailContentTMP.text = MailsManager.GetMailText(ActiveUnitInfo.mails[mailIndex]);
        layoutGroupFix.FixLayout();

        QuestActionsBridge.CallQuestAction(this, QuestActionsBridge.ACTION_TYPE.mail_opened,
            ActiveUnitInfo.mails[selectedMailIndex].mailID, out _);

        if (updateList)
            UpdateMailsList();
    }

    private void UpdateMailsList()
    {
        int currentButtonsCount = mailButtonsParent.childCount;

        for (int i = 0; i < ActiveUnitInfo.mails.Count; i++)
        {
            ButtonManager button;

            MailInfo mailInfo = ActiveUnitInfo.mails[i];
            string mailID = mailInfo.mailID;
            MailData mailData = MailsManager.GetMailDataByID(mailID);

            if (i >= currentButtonsCount)
            {
                ButtonManager newButton = Instantiate(mailButtonPrefab, mailButtonsParent).GetComponent<ButtonManager>();
                newButton.onClick.AddListener(() =>
                {
                    ShowMail(newButton.transform.GetSiblingIndex());
                });
            }

            button = mailButtonsParent.GetChild(i).GetComponent<ButtonManager>();

            if(mailData == null)
            {
                button.enableIcon = false;
                button.SetText($"Mail error: {mailID}");
                button.UpdateUI();
            }
            else
            {
                button.enableIcon = !mailInfo.read;
                button.SetText(mailData.ToShortString(mailInfo.persona));
                button.UpdateUI();
            }
        }

        for (int i = currentButtonsCount; i > ActiveUnitInfo.mails.Count; i--)
        {
            mailButtonsParent.GetChild(i - 1).gameObject.Destroy();
        }
    }

    public void CopyMessageSource()
    {
        if(selectedMailIndex < 0 || selectedMailIndex >= ActiveUnitInfo.mails.Count)
            return;

        mailContentTMP.text = MailsManager.GetMailSource(ActiveUnitInfo.mails[selectedMailIndex]);
        layoutGroupFix.FixLayout();

        QuestActionsBridge.CallQuestAction(this, QuestActionsBridge.ACTION_TYPE.mail_source_copied, 
            ActiveUnitInfo.mails[selectedMailIndex].mailID, out _);
    }

    public void Continue()
    {
        // No specific action needed after mail-related quest action
    }
}
