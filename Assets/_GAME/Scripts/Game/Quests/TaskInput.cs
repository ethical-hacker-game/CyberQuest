using System;
using System.Linq;
using System.Collections.Generic;
using RuntimeInspectorNamespace;

[Serializable]
public class TaskInput : TaskBase, IValidateContent
{
    public override TASK_TYPE TaskType => TASK_TYPE.Input;

    [ExpandArray] public List<TaskInputVariant> variants = new List<TaskInputVariant>();
    public override List<TaskBaseVariant> Variants 
    { 
        get => variants.Cast<TaskBaseVariant>().ToList(); 
        set => variants = value.Cast<TaskInputVariant>().ToList(); 
    }

    public override bool CheckAnswer(string answer)
    {
        answer = answer.ToLower();
        List<string> correctAnswers = GetCorrectAnswers();
        for (int i = 0; i < correctAnswers.Count; i++)
        {
            if (answer.Contains(correctAnswers[i].ToLower()))
                return true;
        }

        return false;
    }

    public override bool ValidateContent(out string validationMessage)
    {
        List<string> issues = new List<string>();

        base.ValidateContent(out string baseIssues);

        if (baseIssues.IsNullOrEmpty() == false)
            issues.Add(baseIssues);

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

        TaskInputVariant newVariant = new TaskInputVariant
        {
            persona = PersonasManager.Instance.Personas[variants.Count].personaName
        };

        variants.Add(newVariant);
    }

    private List<string> GetCorrectAnswers (int personaIndex = -1)
    {
        TaskInputVariant variant = (TaskInputVariant)GetTaskVariant(personaIndex);

        if (variant == null)
            return new List<string>();

        return variant.correctAnswers;
    }
}
