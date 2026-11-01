using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using RuntimeInspectorNamespace;

[Serializable]
public class Quest : IValidateContent
{
    [LockedField]
    public string questID;
    public int cityIndex;
    [LockedField] public string questIconID;
    [LockedField] public bool decisionQuest;

    public List<string> taskIDs = new List<string>();

    [HideInInspector] public float fanOffset;
    [HideInInspector] public bool overrideFanSpan;
    [HideInInspector] public float fanSpan;

    public bool universalPersonaContent;
    [ExpandArray] public List<QuestVariant> variants = new List<QuestVariant>();

    public string GetQuestName(int personaIndex = -1)
    {
        QuestVariant variant = GetQuestVariant(personaIndex);

        if (variant == null)
            return "Null - Quest Variant";

        return PlayerText.Resolve(variant.questName);
    }

    public string GetQuestDescription(int personaIndex = -1)
    {
        QuestVariant variant = GetQuestVariant(personaIndex);

        if (variant == null)
            return "Null - Quest Variant";

        return PlayerText.Resolve(variant.questDescription);
    }
    
    public string GetQuestCompleteText(int personaIndex = -1)
    {
        QuestVariant variant = GetQuestVariant(personaIndex);

        if (variant == null)
            return "Null - Quest Variant";

        return PlayerText.Resolve(variant.questCompleteText);
    }

    public string GetQuestCharacterID(int personaIndex = -1)
    {
        QuestVariant variant = GetQuestVariant(personaIndex);

        if (variant == null)
            return "";

        return variant.questCharacterID;
    }

    public Sprite GetIcon()
    {
        return PicturesManager.GetQuestIconForTaskType(GetPrimaryTaskType());
    }

    /// <summary>
    /// Quest tree icons come from built-in task-type symbols (game Resources), not unit content.
    /// Uses Decision for decision quests; otherwise the first non-Dialogue task, else the first task.
    /// </summary>
    public TaskBase.TASK_TYPE GetPrimaryTaskType()
    {
        if (decisionQuest)
            return TaskBase.TASK_TYPE.Decision;

        if (taskIDs.IsNullOrEmpty())
            return TaskBase.TASK_TYPE.None;

        TaskBase.TASK_TYPE fallback = TaskBase.TASK_TYPE.None;

        for (int i = 0; i < taskIDs.Count; i++)
        {
            TaskBase task = QuestManager.GetTaskByID(taskIDs[i]);
            if (task == null)
                continue;

            if (fallback == TaskBase.TASK_TYPE.None)
                fallback = task.TaskType;

            if (task.TaskType != TaskBase.TASK_TYPE.Dialogue)
                return task.TaskType;
        }

        return fallback;
    }

    public Sprite GetCharacter()
    {
        return PicturesManager.GetPicture(GetQuestCharacterID(), PicturesManager.PictureType.Character);
    }

    public QuestVariant GetQuestVariant(int personaIndex)
    {
        if (variants.IsNullOrEmpty())
            return null;

        if (universalPersonaContent)
            return variants[0];

        if (personaIndex < 0)
            return GetAdaptedQuestVariant();

        if (personaIndex < 0 || personaIndex >= variants.Count)
            return null;

        return variants[personaIndex];
    }

    public int GetDecisionsCount()
    {
        if(decisionQuest == false)
        {
            Debug.LogError($"{questID} This is not a decision quest!");
            return 0;
        }

        if (taskIDs.IsNullOrEmpty())
        {
            Debug.LogError($"{questID} There are 0 tasks!");
            return 0;
        }

        if (taskIDs.Count > 1)
        {
            Debug.LogError($"{questID} There are too many tasks!");
            return 0;
        }

        TaskBase task = QuestManager.GetTaskByID(taskIDs[0]);

        if(task == null)
        {
            Debug.LogError($"{questID} Incorrect decision task ID!");
            return 0;
        }

        if(task.GetType() != typeof(TaskDecision))
        {
            Debug.LogError($"{questID} Incorrect decision task type!");
            return 0;
        }

        TaskDecision taskDecision = (TaskDecision)task;
        return taskDecision.decisions.Count;
    }

    public override string ToString()
    {
        return questID;

        //old soulution
        //return $"{GetQuestName()}\n" +
        //    $"{questID}\n" +
        //    $"{GetQuestDescription()}";
    }

    public string ToFormattedString()
    {
        return $"<b>{questID}</b>";

        //old soulution
        //return $"<b>{GetQuestName()}</b>\n" +
        //    $"<size=40%><i>{questID}\n" +
        //    $"<size=55%>{GetQuestDescription()}</size>";
    }

    public string GetQuestNameWithProgress(string taskID, int personaIndex)
    {
        string taskIndex = "X";

        for (int i = 0; i < taskIDs.Count; i++)
        {
            if (taskIDs[i] == taskID)
            {
                taskIndex = (i + 1).ToString();
                break;
            }
        }
        
        if (taskIDs.Count > 1)
            return $"<size=75%>({taskIndex}/{taskIDs.Count})</size> {GetQuestName(personaIndex)}";
        else
            return GetQuestName(personaIndex);
    }

    public bool ValidateContent(out string validationMessage)
    {
        List<string> issues = new List<string>();
        issues.Add("Issues:");

        if (cityIndex <= 0)
            issues.Add("City index is not set.");

        if (decisionQuest)
        {
            if (universalPersonaContent == false)
                issues.Add("Decision Quest must have Universal Persona Content!");

            if(taskIDs.Count != 1)
                issues.Add("Decision Quest must have only one task!");
            else
            {
                TaskBase task = QuestManager.GetTaskByID(taskIDs[0]);

                if (task == null)
                    issues.Add("Task cannot be found!");
                else
                {
                    if (task.TaskType != TaskBase.TASK_TYPE.Decision)
                        issues.Add("Decision Quest must have decision task!");

                    if(task.universalPersonaContent || task.Variants.Count != 1)
                        issues.Add("Decision Task is not implemented properly! Edit this task!");
                }
            }
        }

        if (taskIDs.IsNullOrEmpty())
            issues.Add("Quest has no tasks.");

        if(variants.IsNullOrEmpty())
            issues.Add("Quest has no variants.");

        if(universalPersonaContent && variants.Count > 1)
            issues.Add("Universal persona content should have only one variant.");

        if (universalPersonaContent == false && variants.Count != PersonasManager.Instance.Personas.Count)
            issues.Add("Quest should have a variant for each persona.");

        for (int i = 0; i < variants.Count; i++)
        {
            if (variants[i].ValidateContent(out string variantValidationMessage) == false)
                issues.Add($"{i}. {variantValidationMessage}");
        }

        validationMessage = string.Join("\n - ", issues);

        return issues.Count == 0;
    }

    [RuntimeInspectorButton("Add Persona Variant", false, ButtonVisibility.InitializedObjects)]
    public void AddVariant()
    {
        if (variants.Count >= PersonasManager.Instance.Personas.Count)
            return;

        if (decisionQuest && variants.Count >= 1)
            return;

        QuestVariant newVariant = new QuestVariant
        {
            persona = PersonasManager.Instance.Personas[variants.Count].personaName
        };

        variants.Add(newVariant);
    }

    private QuestVariant GetAdaptedQuestVariant()
    {
        if (variants.IsNullOrEmpty())
            return null;

        int personaIndex = PersonasManager.Instance.CurrentPersonaIndex;

        if (personaIndex < 0 || personaIndex >= variants.Count)
            return null;

        if (universalPersonaContent)
            return variants[0];

        return variants[personaIndex];
    }
}
