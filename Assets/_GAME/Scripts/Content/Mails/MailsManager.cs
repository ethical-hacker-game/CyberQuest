using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Events;

public class MailsManager : GameSystemComponent
{
    public static MailsManager Instance { get; private set; }

    private static List<MailData> mails = new List<MailData>();

    [ShowInInspector]
    public static List<MailData> Mails
    {
        get
        {
            if (IsInitialized == false)
                LoadMails();

            return mails;
        }
    }
    private static bool IsInitialized { get; set; } = false;

    public UnityEvent OnEmailRecived { get; set; } = new UnityEvent();

    public static MailInfo GetMailInfoByID(string mailID)
    {
        if (PlayerController.Instance == null)
            return null;

        for (int i = 0; i < PlayerController.Instance.ActiveUnit.info.mails.Count; i++)
        {
            if (PlayerController.Instance.ActiveUnit.info.mails[i].mailID == mailID)
                return PlayerController.Instance.ActiveUnit.info.mails[i];
        }

        return null;
    }

    public static MailData GetMailDataByID(string mailID)
    {
        if (Mails.IsNullOrEmpty())
            return null;

        for (int i = 0; i < Mails.Count; i++)
        {
            if (Mails[i].mailID == mailID)
                return Mails[i];
        }

        return null;
    }

    public static void LoadMails(string unitPrefix = "", bool forceReload = false)
    {
        if (IsInitialized && forceReload == false)
            return;

        IsInitialized = true;
        string mailsPath = ContentPaths.UnitFolderOrAll(unitPrefix, ContentConstValues.FOLDER_MAILS);
        mails = ContentLoader.ReturnListOfType<MailData>(mailsPath, ContentConstValues.EXTENSION_MAIL);
    }

    public static string GetMailText(MailInfo mailInfo)
    {
        MailData mailData = GetMailDataByID(mailInfo.mailID);

        if (mailData == null)
            return $"Error, cannot find mail {mailInfo.mailID}";

        return GetMailText(mailInfo, mailData);
    }

    public static string GetShortMailText(MailInfo mailInfo, MailData mailData)
    {
        if (mailInfo.read == false)
            return $"<i>{mailData.ToShortString(mailInfo.persona)}</i>";

        return mailData.ToShortString(mailInfo.persona);
    }

    public static string GetMailSource(MailInfo mailInfo)
    {
        MailData mailData = GetMailDataByID(mailInfo.mailID);

        if (mailData == null)
            return $"Error, cannot find mail {mailInfo.mailID}";

        return GetMailSourceText(mailInfo, mailData);
    }

    private static string GetMailText(MailInfo mailInfo, MailData mailData)
    {
        return string.Format(mailData.ToString(mailInfo.persona), mailInfo.dateTime.ToString("ddd, d MMM yyyy HH:mm:ss"));
    }

    private static string GetMailSourceText(MailInfo mailInfo, MailData mailData)
    {
        string dateString = mailInfo.dateTime.ToString("ddd, dd MMM yyyy HH:mm:ss +0000");
        return string.Format(mailData.ToSourceString(mailInfo.persona), dateString, dateString);
    }

    public bool RecieveMails(List<string> mailIDs)
    {
        int recievedEmails = 0;

        for (int i = 0; i < mailIDs.Count; i++)
        {
            MailData tempMail = GetMailDataByID(mailIDs[i]);

            if (tempMail != null)
            {
                if (RecieveMail(tempMail, false))
                    recievedEmails++;
            }
        }

        if (recievedEmails > 0)
        {
            string header = recievedEmails == 1 ? "New Email" : $"{recievedEmails} New Emails";
            string emailText = recievedEmails == 1 ? "a new email" : $"{recievedEmails} new emails";
            AssistantUI.Instance.SpawnInfoNotification($"{header}<br> recieved.");
            GameUI.Instance.SpawnNotificationWindow(header, $"You have recieved {emailText}.");
        }

        return recievedEmails > 0;
    }

    public bool RecieveMail(MailData mailData, bool showNotification = true)
    {
        MailInfo tempMailInfo = GetMailInfoByID(mailData.mailID);

        if (tempMailInfo != null)
            return false;

        tempMailInfo = new MailInfo()
        {
            mailID = mailData.mailID,
            dateTime = System.DateTime.Now,
            read = false
        };

        PlayerController.Instance.ActiveUnit.info.mails.Add(tempMailInfo);

        if (showNotification)
            AssistantUI.Instance.SpawnInfoNotification("New email recieved.");

        OnEmailRecived.Invoke();
        return true;
    }

    private void Awake()
    {
        Instance = this;
        LoadMails();
    }
}
