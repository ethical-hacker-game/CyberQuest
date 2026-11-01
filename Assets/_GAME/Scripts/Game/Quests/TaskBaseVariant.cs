using RuntimeInspectorNamespace;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.Windows;

[Serializable, ExpandArray]
public abstract class TaskBaseVariant : IValidateContent
{
    [LockedField] public string persona;
    [TextArea] public string taskMessage;
    [TextArea] public string taskInstructions;
    [TextArea] public string taskQuestion;
    [TextArea] public string taskCompleteText;

    /// <summary>
    /// Combined player-facing text: NPC message, plus instructions when present.
    /// </summary>
    [DoNotSerialize]
    public string TaskDescription
    {
        get
        {
            if (string.IsNullOrEmpty(taskInstructions))
                return taskMessage ?? string.Empty;

            if (string.IsNullOrEmpty(taskMessage))
                return taskInstructions;

            return taskMessage + "\n\n" + taskInstructions;
        }
    }

    [DoNotSerialize] public List<string> Links
    {
        get
        {
            List<string> results = new List<string>();

            string description = TaskDescription;
            if (string.IsNullOrEmpty(description))
                return results;

            var matches = Regex.Matches(description, @"<link=""(.*?)"">");

            foreach (Match match in matches)
            {
                results.Add(match.Groups[1].Value);
            }

            return results;
        }
    }


    public virtual bool ValidateContent(out string validationMessage)
    {
        List<string> issues = new List<string>();
        issues.Add($"{persona} Variant:");

        validationMessage = string.Join("\n - ", issues);

        return issues.Count <= 1;
    }
}
