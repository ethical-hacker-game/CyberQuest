using System;
using System.Collections.Generic;
using RuntimeInspectorNamespace;

[Serializable, ExpandArray]
public class TaskActionVariant : TaskBaseVariant, IValidateContent
{
    public QuestActionsBridge.ACTION_TYPE actionType;
    public string actionTag;


    public override bool ValidateContent(out string validationMessage)
    {
        List<string> issues = new List<string>();

        base.ValidateContent(out string baseIssues);

        if (baseIssues.IsNullOrEmpty() == false)
            issues.Add(baseIssues);

        if (actionType == QuestActionsBridge.ACTION_TYPE.None)
            issues.Add("Action type is not set.");

        if (actionTag.IsNullOrEmpty())
            issues.Add("Task has no correct answers.");

        validationMessage = string.Join("\n - ", issues);

        return issues.Count == 0;
    }
}
