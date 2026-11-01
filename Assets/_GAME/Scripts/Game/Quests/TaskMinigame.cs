using RuntimeInspectorNamespace;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class TaskMinigame : TaskBase, IValidateContent
{
    public override TASK_TYPE TaskType => TASK_TYPE.Minigame;

    [ExpandArray] public List<TaskMinigameVariant> variants = new List<TaskMinigameVariant>();

    public override List<TaskBaseVariant> Variants
    {
        get => variants.Cast<TaskBaseVariant>().ToList();
        set => variants = value.Cast<TaskMinigameVariant>().ToList();
    }

    public override bool CheckAnswer(string answer)
    {
        if (answer != "minigame")
            return false;

        return true;
    }

    public override bool ValidateContent(out string validationMessage)
    {
        List<string> issues = new List<string>();

        base.ValidateContent(out string baseIssues);

        if(baseIssues.IsNullOrEmpty() == false)
            issues.Add(baseIssues);

        for (int i = 0; i < variants.Count; i++)
        {
            if(variants[i].ValidateContent(out string variantValidationMessage) == false)
                issues.Add($"{i}. {variantValidationMessage}");
        }

        validationMessage = string.Join("\n - ", issues);

        return issues.Count == 0;
    }


    [RuntimeInspectorButton("Add Persona Variant", false, ButtonVisibility.InitializedObjects)]
    public override void AddVariant()
    {
        if (variants.Count >= PersonasManager.Instance.Personas.Count)
            return;

        TaskMinigameVariant newVariant = new TaskMinigameVariant
        {
            persona = PersonasManager.Instance.Personas[variants.Count].personaName
        };

        variants.Add(newVariant);
    }
}
