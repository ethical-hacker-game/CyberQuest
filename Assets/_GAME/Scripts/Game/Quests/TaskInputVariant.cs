using System;
using System.Linq;
using System.Collections.Generic;
using RuntimeInspectorNamespace;

[Serializable, ExpandArray]
public class TaskInputVariant : TaskBaseVariant, IValidateContent
{
    [ExpandArray]
    public List<string> correctAnswers = new List<string>();


    public override bool ValidateContent(out string validationMessage)
    {
        List<string> issues = new List<string>();

        base.ValidateContent(out string baseIssues);

        if (baseIssues.IsNullOrEmpty() == false)
            issues.Add(baseIssues);

        if (correctAnswers.IsNullOrEmpty())
            issues.Add("Task has no correct answers.");

        validationMessage = string.Join("\n - ", issues);

        return issues.Count == 0;
    }

    [RuntimeInspectorButton("Add Answer", false, ButtonVisibility.InitializedObjects)]
    public void AddAnswer()
    {
        correctAnswers.Add("");
    }
}
