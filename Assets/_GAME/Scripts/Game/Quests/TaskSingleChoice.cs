using System;
using System.Collections.Generic;
using UnityEngine;
using RuntimeInspectorNamespace;
using System.Linq;

[Serializable]
public class TaskSingleChoice : TaskBase, IValidateContent
{
    public override TASK_TYPE TaskType => TASK_TYPE.SingleChoice;

    [ExpandArray] public List<TaskSingleChoiceVariant> variants = new List<TaskSingleChoiceVariant>();
    public override List<TaskBaseVariant> Variants
    {
        get => variants.Cast<TaskBaseVariant>().ToList();
        set => variants = value.Cast<TaskSingleChoiceVariant>().ToList();
    }

    public List<string> GetAnswers(int personaIndex = -1)
    {
        TaskSingleChoiceVariant variant = (TaskSingleChoiceVariant)GetTaskVariant(personaIndex);

        if (variant == null)
            return new List<string>();

        return variant.answers;
    }

    public int GetCorrectAnswer(int personaIndex = -1)
    {
        TaskSingleChoiceVariant variant = (TaskSingleChoiceVariant)GetTaskVariant(personaIndex);

        if (variant == null)
            return 0;

        return variant.correctAnswer;
    }

    public override bool CheckAnswer(string answer)
    {
        if (int.TryParse(answer, out int answerIndex))
            return answerIndex == GetCorrectAnswer();

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

        TaskSingleChoiceVariant newVariant = new TaskSingleChoiceVariant
        {
            persona = PersonasManager.Instance.Personas[variants.Count].personaName
        };

        variants.Add(newVariant);
    }
}
