using System;
using System.Collections.Generic;
using System.Linq;

[Serializable]
public class UnitInfo
{
    public string unitID;
    public float unitTimer; //in minutes
    public bool isDone = false;

    public List<QuestInfo> quests = new List<QuestInfo>();
    public List<MailInfo> mails = new List<MailInfo>();
    public List<PersonaInfo> personas = new List<PersonaInfo>();
    public List<AppInfo> oppenedApps = new List<AppInfo>();
    public List<string> readArticleIDs = new List<string>();
    public List<string> skills = new List<string>();

    public int UnreadEmailsCount => mails.Where(m => m.read == false).Count();
    public bool HasUnreadEmails => UnreadEmailsCount > 0;
}
