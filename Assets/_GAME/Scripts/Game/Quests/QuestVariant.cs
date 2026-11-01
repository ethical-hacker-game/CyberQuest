using DevTools;
using RuntimeInspectorNamespace;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[Serializable, ExpandArray]
public class QuestVariant : IValidateContent
{
    [LockedField] public string persona;
    public string questName;
    [TextArea] public string questDescription;
    [TextArea] public string questCompleteText;
    [LockedField] public string questCharacterID;

    public Sprite GetCharacter()
    {
        return PicturesManager.GetPicture(questCharacterID, PicturesManager.PictureType.Character);
    }

    [RuntimeInspectorButton("Change Character", false, ButtonVisibility.InitializedObjects)]
    public void ChangeCharacter()
    {
        PictureSelectionUI.SetupPictureSelectionUI(PicturesManager.CharacterPictures, PicturesManager.DefaultCharacterPicture);
        PictureSelectionUI.BackButtonEvent.AddListener(DevEditorUIManager.ShowQuestEditorUI);
        PictureSelectionUI.SelectImageEvent.AddListener((id) =>
        {
            questCharacterID = id;
        });
    }

    [LockedField] public string appScenarioID;

    [RuntimeInspectorButton("Select Scenario", false, ButtonVisibility.InitializedObjects)]
    public void SelectScenario()
    {
        if (CommonUISolver.InstanceType != CommonUISolver.UIManagerTypes.DevEditorUIManager)
            return;

        List<string> appIDs = DevToolsManager.ActiveUnit.appIDs;
        List<AppScenario> appScenarios = AppsManager.Instance.GetAppScenariosByAppCoreIDs(appIDs);
        List<string> scenarioIDs = appScenarios.Select(app_s => app_s.scenarioID).ToList();

        TextSelectionUI.SetupTextSelectionUI(scenarioIDs, "-");
        TextSelectionUI.BackButtonEvent.AddListener(DevEditorUIManager.ShowQuestEditorUI);
        TextSelectionUI.SelectStringEvent.AddListener((id) =>
        {
            if (id == "-")
                appScenarioID = "";
            else
                appScenarioID = id;

            DevEditorUIManager.ShowQuestEditorUI();
        });
    }

    public bool ValidateContent(out string validationMessage)
    {
        List<string> issues = new List<string>();
        issues.Add($"{persona} Variant:");

        if (questName.IsNullOrEmpty())
            issues.Add("Quest name is empty.");

        if (questDescription.IsNullOrEmpty())
            issues.Add("Quest description is empty.");

        if (questCompleteText.IsNullOrEmpty())
            issues.Add("Quest complete text is empty.");

        if (questCharacterID.IsNullOrEmpty())
            issues.Add("Default quest character.");

        validationMessage = string.Join("\n - ", issues);

        return issues.Count <= 1;
    }
}
