using DevTools;
using RuntimeInspectorNamespace;
using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable, ExpandArray]
public class MailDataVariant : IValidateContent
{
    [LockedField] public string persona;
    public string author;
    public string subject;
    public string shortSummary;
    public DateTime date;
    [TextArea] public string body;
    public List<string> attachements = new List<string>();

    public string ToShortString()
    {
        return $"{author}\n" +
            $"<b>{subject}</b>\n" +
            $"{shortSummary}";
    }

    public override string ToString()
    {
        return
            $"From: <b>{author}</b>\n" +
            $"To: <b>You</b>\n" +
            $"Subject: <b>{subject}</b>\n" +
            "Date: <b>{0}</b>\n" +
            "\n" +
            "--------------------------------------------------------------\n" +
            "\n" +
            $"{body}";
    }

    public string ToDevString()
    {
        return $"<b>{subject}</b>\n" +
            $"<size=55%>{body}</size>";
    }

    public bool ValidateContent(out string validationMessage)
    {
        List<string> issues = new List<string>();
        issues.Add($"{persona} Variant:");

        if (author.IsNullOrEmpty())
            issues.Add("Author field is empty.");

        if (subject.IsNullOrEmpty())
            issues.Add("Subject is empty.");

        if (shortSummary.IsNullOrEmpty())
            issues.Add("Short summary is empty.");

        if (body.IsNullOrEmpty())
            issues.Add("Body is empty.");

        if (attachements.IsNullOrEmpty() == false)
            issues.Add("Attachements are not yet implemented. Contact Lukasz.");

        validationMessage = string.Join("\n - ", issues);

        return issues.Count <= 1;
    }
}
