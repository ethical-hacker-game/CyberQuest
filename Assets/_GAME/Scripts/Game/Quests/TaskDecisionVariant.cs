using System;
using System.Collections.Generic;
using RuntimeInspectorNamespace;

[Serializable, ExpandArray]
public class TaskDecisionVariant : TaskBaseVariant, IValidateContent
{
    public override bool ValidateContent(out string validationMessage)
    {
        List<string> issues = new List<string>();

        base.ValidateContent(out string baseIssues);

        if (baseIssues.IsNullOrEmpty() == false)
            issues.Add(baseIssues);

        validationMessage = string.Join("\n - ", issues);

        return issues.Count == 0;
    }
}
