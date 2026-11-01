using DevTools;
using RuntimeInspectorNamespace;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

[System.Serializable]
[ExpandArray]
public class Persona : IValidateContent
{
    public string personaID;
    public string personaName;
    [TextArea] public string personaDescription;
    public string messagePlayerJoinedHeader;
    [TextArea] public string messagePlayerJoined;
    public string messagePlayerUnloyalHeader;
    [TextArea] public string messagePlayerUnloyal;
    [LockedField] public string personaPictureID;

    public Persona() { }

    public Persona(string personaID)
    {
        this.personaID = personaID;
    }

    public Sprite GetPersonaPicture()
    {
        return PicturesManager.GetPicture(personaPictureID, PicturesManager.PictureType.Persona);
    }

    [RuntimeInspectorButton("Change Persona Picture", false, ButtonVisibility.InitializedObjects)]
    public void ChangePersonaPicture()
    {
        PictureSelectionUI.SetupPictureSelectionUI(PicturesManager.PersonaPictures, PicturesManager.DefaultPersonaPicture);
        PictureSelectionUI.BackButtonEvent.AddListener(DevEditorUIManager.ShowUnitEditorUI);
        PictureSelectionUI.SelectImageEvent.AddListener((id) =>
        {
            personaPictureID = id;
        });
    }

    public bool ValidateContent(out string validationMessage)
    {
        List<string> issues = new List<string>();

        if (personaID.IsNullOrEmpty())
            issues.Add("Persona ID is empty.");

        if (personaName.IsNullOrEmpty())
            issues.Add("Persona name is empty.");

        if (personaDescription.IsNullOrEmpty())
            issues.Add("Persona description is empty.");

        if (messagePlayerJoinedHeader.IsNullOrEmpty())
            issues.Add("Message player joined header is empty.");

        if (messagePlayerJoined.IsNullOrEmpty())
            issues.Add("Message for player joined is empty.");

        if (messagePlayerUnloyalHeader.IsNullOrEmpty())
            issues.Add("Message player unloyal header is empty.");

        if (messagePlayerUnloyal.IsNullOrEmpty())
            issues.Add("Message for player unloyal is empty.");

        validationMessage = string.Join("\n - ", issues);

        return issues.Count <= 0;
    }
}
