using RuntimeInspectorNamespace;
using System;
using System.Collections.Generic;
using System.Net.Mail;
using UnityEngine;

[Serializable]
public class MailData : IValidateContent
{
    [LockedField] public string mailID;
    [LockedField] public string parentTaskID;

    public bool universalPersonaContent;
    [ExpandArray] public List<MailDataVariant> variants = new List<MailDataVariant>();

    public static MailData Example => new MailData
    {
        mailID = "mailID",
        parentTaskID = "parentTaskID",
        variants = new List<MailDataVariant>
        {
            new MailDataVariant()
            {
                author = "John Black",
                subject = "Subject",
                shortSummary = "Short summmary",
                date = DateTime.Now,
                body = "Text, Text, Text \nText, Text, Text, Text, Text"
            }
        }
    };

    public string GetAuthor(int personaIndex = -1)
    {
        MailDataVariant variant = GetMailDataVariant(personaIndex);

        if (variant == null)
            return "Null - Mail Variant";

        return variant.author;
    }

    public string GetSubject(int personaIndex = -1)
    {
        MailDataVariant variant = GetMailDataVariant(personaIndex);

        if (variant == null)
            return "Null - Mail Variant";

        return PlayerText.Resolve(variant.subject);
    }

    public string GetShortSummary(int personaIndex = -1)
    {
        MailDataVariant variant = GetMailDataVariant(personaIndex);

        if (variant == null)
            return "Null - Mail Variant";

        return PlayerText.Resolve(variant.shortSummary);
    }

    public DateTime GetDate(int personaIndex = -1)
    {
        MailDataVariant variant = GetMailDataVariant(personaIndex);

        if (variant == null)
            return default(DateTime);

        return variant.date;
    }

    public string GetBody(int personaIndex = -1)
    {
        MailDataVariant variant = GetMailDataVariant(personaIndex);

        if (variant == null)
            return "Null - Mail Variant";

        return PlayerText.Resolve(variant.body);
    }

    public List<string> GetAttachements(int personaIndex = -1)
    {
        MailDataVariant variant = GetMailDataVariant(personaIndex);

        if (variant == null)
            return new List<string>();

        return variant.attachements;
    }

    public MailDataVariant GetMailDataVariant(int personaIndex = -1)
    {
        if (variants.IsNullOrEmpty())
            return null;

        if (universalPersonaContent)
            return variants[0];

        if (personaIndex < 0)
            return GetAdaptedMailDataVariant();

        if (personaIndex < 0 || personaIndex >= variants.Count)
            return null;

        return variants[personaIndex];
    }

    public string ToShortString(int personaIndex = -1)
    {
        string authorShort = GetAuthor(personaIndex);
        authorShort = authorShort.Split(" <")[0];

        return $"{authorShort}\n" +
            $"<b>{GetSubject(personaIndex)}</b>\n" +
            $"{GetShortSummary(personaIndex)}";
    }

    public string ToString(int personaIndex = -1)
    {
        return
            $"From: <b>{GetAuthor(personaIndex)}</b>\n" +
            $"To: <b>You</b>\n" +
            $"Subject: <b>{GetSubject(personaIndex)}</b>\n" +
            "Date: <b>{0}</b>\n" +
            "\n" +
            "--------------------------------------------------------------\n" +
            "\n" +
            $"{GetBody(personaIndex)}";
    }

    public string ToSourceString(int personaIndex = -1)
    {
        string messageId = $"<{Guid.NewGuid()}@game-mail.net>";
        string returnPath = $"{GetAuthor(personaIndex).Replace(" ", ".").ToLower()}@example-alerts.net";

        return
            $"Return-Path: <{returnPath}>\n" +
            $"Received: from mx.example-alerts.net (mx.example-alerts.net [192.0.2.41])\n" +
            $"        by inbound.game-mail.net with ESMTPS id X1Y2Z3\n" +
            "        for <you@game-mail.net>; {0}\n" +
            $"Message-ID: {messageId}\n" +
            "Date: {1}\n" +
            $"From: {GetAuthor(personaIndex)} <{returnPath}>\n" +
            $"To: You <you@game-mail.net>\n" +
            $"Subject: {GetSubject(personaIndex)}\n" +
            "MIME-Version: 1.0\n" +
            "Content-Type: text/plain; charset=\"UTF-8\"\n" +
            "Content-Transfer-Encoding: 7bit\n" +
            "\n" +
            "--------------------------------------------------------------\n" +
            "\n" +
            $"{GetBody(personaIndex)}";
    }

    public string ToFormattedString(int personaIndex = -1)
    {
        return mailID;

        //old solution
        //return $"<b>{GetSubject(personaIndex)}</b>\n" +
        //    $"<size=40%><i>{mailID}\n" +
        //    $"<size=55%>{GetBody(personaIndex)}</size>";
    }

    public bool ValidateContent(out string validationMessage)
    {
        List<string> issues = new List<string>();
        issues.Add("Issues:");

        if (mailID.IsNullOrEmpty())
            issues.Add("Mail ID is incorrect.");

        if (QuestManager.GetTaskByID(parentTaskID).universalPersonaContent != universalPersonaContent)
            issues.Add("Universal persona content settings is not the same as in the task.");

        if (variants.IsNullOrEmpty())
            issues.Add("Mail has no variants!");

        if (universalPersonaContent && variants.Count > 1)
            issues.Add("Universal persona content should have only one variant.");

        if (universalPersonaContent == false && variants.Count != PersonasManager.Instance.Personas.Count)
            issues.Add("Mail should have a variant for each persona.");

        for (int i = 0; i < variants.Count; i++)
        {
            if (variants[i].ValidateContent(out string variantValidationMessage) == false)
                issues.Add($"{i}. {variantValidationMessage}");
        }

        validationMessage = string.Join("\n - ", issues);

        return issues.Count <= 1;
    }

    [RuntimeInspectorButton("Add Persona Variant", false, ButtonVisibility.InitializedObjects)]
    public void AddVariant()
    {
        if (variants.Count >= PersonasManager.Instance.Personas.Count)
            return;

        MailDataVariant source = GetMailDataVariant();

        MailDataVariant newVariant = new MailDataVariant
        {
            persona = PersonasManager.Instance.Personas[variants.Count].personaName,
            author = source != null ? source.author : string.Empty,
            subject = source != null ? source.subject : string.Empty,
            shortSummary = source != null ? source.shortSummary : string.Empty,
            date = source != null ? source.date : default,
            body = source != null ? source.body : string.Empty,
            attachements = source != null ? new List<string>(source.attachements) : new List<string>()
        };

        variants.Add(newVariant);
    }

    private MailDataVariant GetAdaptedMailDataVariant()
    {
        if (variants.IsNullOrEmpty())
            return null;

        int personaIndex = PersonasManager.Instance.CurrentPersonaIndex;

        if (personaIndex < 0 || personaIndex >= variants.Count)
            return null;

        if (universalPersonaContent)
            return variants[0];

        return variants[personaIndex];
    }
}
