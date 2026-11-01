using RuntimeInspectorNamespace;
using System;
using System.Collections.Generic;
using System.Linq;

[Serializable]
public class TaskAction: TaskBase, IValidateContent
{
    public override TASK_TYPE TaskType => TASK_TYPE.Action;

    [ExpandArray] public List<TaskActionVariant> variants = new List<TaskActionVariant>();
    public override List<TaskBaseVariant> Variants
    {
        get => variants.Cast<TaskBaseVariant>().ToList();
        set => variants = value.Cast<TaskActionVariant>().ToList();
    }

    public static string GetAnswerCoded(QuestActionsBridge.ACTION_TYPE actionType, string actionTag)
    {
        return $"{actionType}#{actionTag}";
    }

    public string GetCorrectAnswer(int personaIndex = -1)
    {
        TaskActionVariant variant = (TaskActionVariant)GetTaskVariant(personaIndex);

        if (variant == null)
            return "Null - Task Variant";

        return GetAnswerCoded(variant.actionType, variant.actionTag);
    }

    public override bool CheckAnswer(string answer)
    {
        return answer.ToLower() == GetCorrectAnswer().ToLower();
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

        TaskActionVariant newVariant = new TaskActionVariant
        {
            persona = PersonasManager.Instance.Personas[variants.Count].personaName
        };

        variants.Add(newVariant);
    }
}
