using Newtonsoft.Json;
using RuntimeInspectorNamespace;
using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public abstract class TaskBase : IValidateContent
{
    [LockedField] public string taskID;
    [LockedField] public string parentQuestID;
    [JsonProperty] public abstract TASK_TYPE TaskType { get; }
    public string articleID;
    [UnityEngine.Tooltip("Learning points. Zero uses the default for this task type.")]
    public int points;

    [LockedField]
    public List<string> mailsOnStart = new List<string>();
    [LockedField]
    public List<string> mailsOnEnd = new List<string>();

    public bool universalPersonaContent;
    [DoNotSerialize] public abstract List<TaskBaseVariant> Variants { get; set; }
    [DoNotSerialize] public List<string> Links => GetAdaptedTaskVariant().Links;

    public string GetTaskDescription(int personaIndex = -1)
    {
        TaskBaseVariant variant = GetTaskVariant(personaIndex);

        if (variant == null)
            return "Null - Task Variant";

        return PlayerText.Resolve(variant.TaskDescription);
    }


    public string GetTaskQuestion(int personaIndex = -1)
    {
        TaskBaseVariant variant = GetTaskVariant(personaIndex);

        if (variant == null)
            return "Null - Task Variant";

        return PlayerText.Resolve(variant.taskQuestion);
    }


    public string GetTaskCompleteText(int personaIndex = -1)
    {
        TaskBaseVariant variant = GetTaskVariant(personaIndex);

        if (variant == null)
            return "Null - Task Variant";

        return PlayerText.Resolve(variant.taskCompleteText);
    }

    public abstract bool CheckAnswer(string answer);

    [Serializable]
    public enum TASK_TYPE
    {
        None,
        SingleChoice,
        TrueFalse,
        Input,
        Website,
        Decision,
        Action,
        Minigame,
        Dialogue
    }

    public override string ToString()
    {
        return taskID;
        
        //old soulution
        //return $"{taskID}\n" +
        //    $"{TaskType.ToString()}\n" +
        //    $"{GetTaskDescription()}";
    }

    public string ToFormattedString()
    {
        return $"{taskID}\n" +
            $"Type: {TaskType.ToString()}";

        //old soulution
        //return $"<size=40%><i>{taskID}\n" +
        //    $"{TaskType.ToString()}</i></size>\n" +
        //    $"<size=55%>{GetTaskDescription()}</size>";
    }

    public virtual bool ValidateContent(out string validationMessage)
    {
        List<string> issues = new List<string>();
        issues.Add("Issues:");

        if (points < 0)
            issues.Add("Learning points cannot be negative. Use 0 for the task type default.");

        if (Variants.IsNullOrEmpty())
            issues.Add("Task has no variants.");

        if (QuestManager.GetQuestByID(parentQuestID).universalPersonaContent != universalPersonaContent)
            issues.Add("Universal persona content settings is not the same as in the quest");

        if (universalPersonaContent && Variants.Count > 1)
            issues.Add("Universal persona content should have only one variant.");

        if (universalPersonaContent == false && Variants.Count != PersonasManager.Instance.Personas.Count)
            issues.Add("Quest should have a variant for each persona.");

        for (int i = 0; i < Variants.Count; i++)
        {
            if (Variants[i].ValidateContent(out string variantValidationMessage) == false)
                issues.Add($"{i}. {variantValidationMessage}");
        }

        validationMessage = string.Join("\n - ", issues);

        return issues.Count <= 1;
    }

    [RuntimeInspectorButton("Add Persona Variant", false, ButtonVisibility.InitializedObjects)]
    public abstract void AddVariant();

    public virtual TaskBaseVariant GetTaskVariant(int personaIndex)
    {
        if (Variants.IsNullOrEmpty())
            return null;

        if (universalPersonaContent)
            return Variants[0];

        if (personaIndex < 0)
            return GetAdaptedTaskVariant();

        if (personaIndex < 0 || personaIndex >= Variants.Count)
            return null;

        return Variants[personaIndex];
    }

    public TaskBaseVariant GetAdaptedTaskVariant()
    {
        if (Variants.IsNullOrEmpty())
            return null;

        if (universalPersonaContent)
            return Variants[0];

        int personaIndex = PersonasManager.Instance.CurrentPersonaIndex;

        if (personaIndex < 0 || personaIndex >= Variants.Count)
            return null;

        return Variants[personaIndex];
    }
}
