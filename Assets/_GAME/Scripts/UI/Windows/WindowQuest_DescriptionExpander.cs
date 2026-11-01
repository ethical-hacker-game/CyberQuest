using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class WindowQuest_DescriptionExpander : UIInteractions
{
    [SerializeField] private GameUI_QuestElementBase questWindow;
    
    private TextMeshProUGUI descriptionTMP;

    public void ShowHintOnOverflow()
    {
        if (CanPerform() == false)
            return;

        if (descriptionTMP.isTextOverflowing == false)
            return;

        Vector2 mousePosition = InputManager.mousePosition;
        CursorHintsManager.Instance.ShowHint("Display full instructions.", mousePosition, this);
    }

    public void ShowFullInstructions()
    {
        if (CanPerform() == false)
            return;

        if (descriptionTMP.isTextOverflowing == false)
            return;

        string taskID = questWindow.TaskRef.taskID;
        int personaIndex = questWindow.QuestRef.info.persona;
        string header = questWindow.QuestRef.data.GetQuestNameWithProgress(taskID, personaIndex);
        string content = questWindow.TaskRef.data.GetTaskDescription();


        ContentUI.SetupContentUI(header, null, content, "Back to the game");
        ContentUI.ButtonEvents[0].AddListener(GameplayUIManager.ShowGameUI);
        HideHint();
    }

    public void HideHint()
    {
        if (CanPerform() == false)
            return;

        CursorHintsManager.Instance.ClearHint(this);
    }

    private void Awake()
    {
        descriptionTMP = GetComponent<TextMeshProUGUI>();

        if (CanPerform() == false)
            return;

        OnMouseMove.AddListener(ShowHintOnOverflow);
        OnMouseExit.AddListener(HideHint);
        OnMouseDown.AddListener(ShowFullInstructions);
    }

    private bool CanPerform()
    {
        if (descriptionTMP == null)
            return false;

        if (questWindow == null)
            return false;

        return true;
    }
}
