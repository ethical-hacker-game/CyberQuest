using RuntimeInspectorNamespace;
using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable, ExpandArray]
public class DecisionData : IValidateContent
{
    public string decisionText;
    [Range(0, 10)] public int personaIndex;
    [Range(0, 100)] public int personaValueChange;

    public bool ValidateContent(out string validationMessage)
    {
        List<string> issues = new List<string>();
        issues.Add("Decision Issues:");

        if (decisionText.IsNullOrEmpty())
            issues.Add("Decision Text is epmty.");

        if(personaIndex >= PersonasManager.Instance.Personas.Count)
            issues.Add("Perosna Index is out of Personas Range!");

        validationMessage = string.Join("\n - ", issues);

        return issues.Count <= 1;
    }
}
