using System;
using System.Collections.Generic;
using UnityEngine;
using RuntimeInspectorNamespace;

[Serializable, ExpandArray]
public class TaskSingleChoiceVariant : TaskBaseVariant, IValidateContent
{
    [ExpandArray] public List<string> answers = new List<string>();
    [Range(0, 4)] public int correctAnswer;

    public override bool ValidateContent(out string validationMessage)
    {
        List<string> issues = new List<string>();

        base.ValidateContent(out string baseIssues);

        if (baseIssues.IsNullOrEmpty() == false)
            issues.Add(baseIssues);

        if (answers.IsNullOrEmpty())
            issues.Add("Task has no answers.");
        else if(answers.Count > 5)
            issues.Add("Task has too many answers.");

        if(correctAnswer >= answers.Count)
            issues.Add("Correct answer index is out of range.");

        validationMessage = string.Join("\n - ", issues);

        return issues.Count == 0;
    }

    [RuntimeInspectorButton("Add Answer", false, ButtonVisibility.InitializedObjects)]
    public void AddAnswer()
    {
        if (answers.Count >= 5)
            return;

        answers.Add("");
    }
}
