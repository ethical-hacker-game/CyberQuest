using RuntimeInspectorNamespace;
using System;
using System.Collections.Generic;
using System.Linq;

[Serializable]
public class TaskDialogue : TaskBase, IValidateContent
{
    public override TASK_TYPE TaskType => TASK_TYPE.Dialogue;

    [ExpandArray] public List<TaskDialogueVariant> variants = new List<TaskDialogueVariant>();
    public override List<TaskBaseVariant> Variants
    {
        get => variants.Cast<TaskBaseVariant>().ToList();
        set => variants = value.Cast<TaskDialogueVariant>().ToList();
    }

    //dialogue tasks are narrative-only, any answer completes them
    public override bool CheckAnswer(string answer)
    {
        return true;
    }

    [RuntimeInspectorButton("Add Persona Variant", false, ButtonVisibility.InitializedObjects)]
    public override void AddVariant()
    {
        if (variants.Count >= PersonasManager.Instance.Personas.Count)
            return;

        TaskDialogueVariant newVariant = new TaskDialogueVariant
        {
            persona = PersonasManager.Instance.Personas[variants.Count].personaName
        };

        variants.Add(newVariant);
    }
}
