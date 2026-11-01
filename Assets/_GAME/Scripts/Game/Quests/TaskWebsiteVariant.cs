using System;
using System.Collections.Generic;
using RuntimeInspectorNamespace;

[Serializable, ExpandArray]
public class TaskWebsiteVariant : TaskBaseVariant, IValidateContent
{
    public string correctAnswer;


    public override bool ValidateContent(out string validationMessage)
    {
        List<string> issues = new List<string>();

        base.ValidateContent(out string baseIssues);

        if (baseIssues.IsNullOrEmpty() == false)
            issues.Add(baseIssues);

        if (correctAnswer.IsNullOrEmpty())
            issues.Add("Task has no correct answers.");

        validationMessage = string.Join("\n - ", issues);

        return issues.Count == 0;
    }
}
