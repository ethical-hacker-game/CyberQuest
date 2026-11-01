using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class SkillData : IValidateContent
{
    public string skillID;
    public string skillName;
    [TextArea] public string skillDescription;
    public int price = 0;
    [Range(0, 100)] public int order = 0;
    public bool requireTask = false;
    public bool lockInDemo = false;

    public override string ToString()
    {
        return $"{skillName}\n" +
            $"{skillID}\n" +
            $"{skillDescription}";
    }

    public bool ValidateContent(out string validationMessage)
    {
        List<string> issues = new List<string>();
        issues.Add("Issues:");

        if (skillID.IsNullOrEmpty())
            issues.Add("Skill ID is empty.");

        if (skillName.IsNullOrEmpty())
            issues.Add("Skill name is empty.");

        if (skillDescription.IsNullOrEmpty())
            issues.Add("Skill description is empty.");

        if (price < 0)
            issues.Add("Skill price is negative.");

        validationMessage = string.Join("\n - ", issues);

        return issues.Count <= 1;
    }
}
