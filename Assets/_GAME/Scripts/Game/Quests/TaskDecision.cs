using System;
using System.Linq;
using System.Collections.Generic;
using RuntimeInspectorNamespace;
using System.Diagnostics;
using UnityEngine;
using Debug = UnityEngine.Debug;

[Serializable]
public class TaskDecision : TaskBase, IValidateContent
{
    public override TASK_TYPE TaskType => TASK_TYPE.Decision;

    [ExpandArray] public List<DecisionData> decisions = new List<DecisionData>();

    [ExpandArray] public List<TaskDecisionVariant> variants = new List<TaskDecisionVariant>();
    public override List<TaskBaseVariant> Variants
    {
        get => variants.Cast<TaskBaseVariant>().ToList();
        set => variants = value.Cast<TaskDecisionVariant>().ToList();
    }

    public override bool CheckAnswer(string answer)
    {
        if (answer != "decision")
            return false;

        DecisionsManager.ShowDecisionPanel(this);
        return true;
    }

    public override bool ValidateContent(out string validationMessage)
    {
        List<string> issues = new List<string>();

        base.ValidateContent(out string baseIssues);

        if (baseIssues.IsNullOrEmpty() == false)
            issues.Add(baseIssues);

        if (universalPersonaContent == false)
        {
            issues.Add("Decision Task must have Universal Persona Content!!!");
            universalPersonaContent = true;
        }

        if(mailsOnStart.IsNullOrEmpty() == false || mailsOnEnd.IsNullOrEmpty() == false)
            issues.Add("Decision Task cannot have any mails.");

        if (decisions.Count < 2)
            issues.Add("At least 2 decisions must be added.");

        for (int i = 0; i < variants.Count; i++)
        {
            if (variants[i].ValidateContent(out string variantValidationMessage) == false)
                issues.Add($"{i}. {variantValidationMessage}");
        }

        for (int i = 0; i < decisions.Count; i++)
        {
            if (decisions[i].ValidateContent(out string decisionValidationMessage) == false)
                issues.Add($"{i}. {decisionValidationMessage}");
        }

        validationMessage = string.Join("\n - ", issues);

        return issues.Count == 0;
    }

    [RuntimeInspectorButton("Add Decision", false, ButtonVisibility.InitializedObjects)]
    public void AddDecision()
    {
        if (variants.Count >= 10)
            return;

        decisions.Add(new DecisionData());
    }

    [RuntimeInspectorButton("Add Persona Variant", false, ButtonVisibility.InitializedObjects)]
    public override void AddVariant()
    {
        if (variants.Count >= 1)
            return;

        TaskDecisionVariant newVariant = new TaskDecisionVariant
        {
            persona = "Universal Persona Content"
        };

        variants.Add(newVariant);
    }
}
