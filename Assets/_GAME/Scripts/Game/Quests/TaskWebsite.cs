using RuntimeInspectorNamespace;
using System;
using System.Collections.Generic;
using System.Linq;

[Serializable]
public class TaskWebsite : TaskBase, IValidateContent
{
    public override TASK_TYPE TaskType => TASK_TYPE.Website;

    [ExpandArray] public List<TaskWebsiteVariant> variants = new List<TaskWebsiteVariant>();
    public override List<TaskBaseVariant> Variants
    {
        get => variants.Cast<TaskBaseVariant>().ToList();
        set => variants = value.Cast<TaskWebsiteVariant>().ToList();
    }

    public string GetCorrectAnswer(int personaIndex = -1)
    {
        TaskWebsiteVariant variant = (TaskWebsiteVariant)GetTaskVariant(personaIndex);

        if (variant == null)
            return "Null - Task Variant";

        return variant.correctAnswer;
    }

    public override bool CheckAnswer(string answer)
    {
        return answer.ToLower().Contains(GetCorrectAnswer().ToLower());
    }

    public override bool ValidateContent(out string validationMessage)
    {
        List<string> issues = new List<string>();

        base.ValidateContent(out string baseIssues);

        if (baseIssues.IsNullOrEmpty() == false)
            issues.Add(baseIssues);

        if (GetCorrectAnswer().IsNullOrEmpty())
            issues.Add("Task has no correct answers.");

        for (int i = 0; i < variants.Count; i++)
        {
            if (variants[i].ValidateContent(out string variantValidationMessage) == false)
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

        TaskWebsiteVariant newVariant = new TaskWebsiteVariant
        {
            persona = PersonasManager.Instance.Personas[variants.Count].personaName
        };

        variants.Add(newVariant);
    }
}
