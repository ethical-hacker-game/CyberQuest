using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;
using UnityEngine;
using RuntimeInspectorNamespace;

[Serializable]
public class Unit : IValidateContent
{
    [LockedField] public string unitID;
    public bool placeholder;
    [Range(0, 100)] public int order = 0;
    public string unitName;
    [TextArea] public string unitDescription;
    [TextArea] public string unitEntryMessage;
    /// <summary>Default player name. Story text uses <c>{player}</c>.</summary>
    public string defaultPlayerName = "Alex";
    public List<string> questIds = new List<string>();
    [LockedField] public Vector2 questsAreaSize = new Vector2(1200, 600);
    [LockedField] public float questTreeOffset = 50;
    [ExpandArray] public List<string> appIDs = new List<string>();
    [ExpandArray] public List<Persona> personas = new List<Persona>();


    public string GetUnitEntryMessage() => PlayerText.Resolve(unitEntryMessage);

    public string GetUnitDescription() => PlayerText.Resolve(unitDescription);

    public Sprite GetPicture()
    {
        if (CourseManager.Pictures.TryGetValue(unitID, out Texture2D picture))
        {
            return Sprite.Create(picture,
                new Rect(0, 0, picture.width, picture.height),
                new Vector2(picture.width / 2, picture.height / 2));
        }

        return null;
    }

    public override string ToString()
    {
        return $"{unitName}\n" +
        $"{unitID}\n" +
            $"{unitDescription}";
    }

    public string ToFormattedString()
    {
        return $"<b>{unitName}</b>\n" +
            $"<size=40%><i>{unitID}</i></size>\n" +
            $"<size=55%>{unitDescription}</size>";
    }

    public bool ValidateContent(out string validationMessage)
    {
        List<string> issues = new List<string>();
        issues.Add("Issues:");

        if (unitName.IsNullOrEmpty())
            issues.Add("Unit name is empty.");

        if (unitDescription.IsNullOrEmpty())
            issues.Add("Unit description is empty.");

        if (unitEntryMessage.IsNullOrEmpty())
            issues.Add("Unit entry message is empty.");

        if (questIds.IsNullOrEmpty())
            issues.Add("Unit has no quests.");

        if (personas.IsNullOrEmpty())
            issues.Add("Unit has no personas.");

        for (int i = 0; i < personas.Count; i++)
        {
            if (personas[i].ValidateContent(out string personaValidationMessage) == false)
                issues.Add($"{i} {personas[i].personaID}: \n- {personaValidationMessage}");
        }

        validationMessage = string.Join("\n - ", issues);

        return issues.Count <= 1;
    }

    [RuntimeInspectorButton("Add Persona", false, ButtonVisibility.InitializedObjects)]
    public void AddPersona()
    {
        personas.Add(new Persona());
    }

}
