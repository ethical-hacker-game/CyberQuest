using System;
using System.Collections.Generic;
using System.Linq;
using RuntimeInspectorNamespace;

[Serializable]
public class TaskTrueFalse : TaskBase, IValidateContent
{
    public override TASK_TYPE TaskType => TASK_TYPE.TrueFalse;

    [ExpandArray] public List<TaskTrueFalseVariant> variants = new List<TaskTrueFalseVariant>();
    public override List<TaskBaseVariant> Variants
    {
        get => variants.Cast<TaskBaseVariant>().ToList();
        set => variants = value.Cast<TaskTrueFalseVariant>().ToList();
    }

    public bool GetCorrectAnswer(int personaIndex = -1)
    {
        TaskTrueFalseVariant variant = (TaskTrueFalseVariant)GetTaskVariant(personaIndex);

        if (variant == null)
            return false;

        return variant.correctAnswer;
    }

    public override bool CheckAnswer(string answer)
    {
        return GetCorrectAnswer().ToString().ToLower() == answer.ToLower();
    }

    [RuntimeInspectorButton("Add Persona Variant", false, ButtonVisibility.InitializedObjects)]
    public override void AddVariant()
    {
        if (variants.Count >= PersonasManager.Instance.Personas.Count)
            return;

        TaskTrueFalseVariant newVariant = new TaskTrueFalseVariant
        {
            persona = PersonasManager.Instance.Personas[variants.Count].personaName
        };

        variants.Add(newVariant);
    }
}
