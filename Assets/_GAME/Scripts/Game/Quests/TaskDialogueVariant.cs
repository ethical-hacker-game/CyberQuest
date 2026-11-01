using System;
using System.Collections.Generic;
using RuntimeInspectorNamespace;

[Serializable, ExpandArray]
public class TaskDialogueVariant : TaskBaseVariant, IValidateContent
{
    public override bool ValidateContent(out string validationMessage)
    {
        List<string> issues = new List<string>();
        issues.Add($"{persona} Variant:");

        if (taskMessage.IsNullOrEmpty())
            issues.Add("Dialogue task has no message text.");

        validationMessage = string.Join("\n - ", issues);

        return issues.Count <= 1;
    }
}
