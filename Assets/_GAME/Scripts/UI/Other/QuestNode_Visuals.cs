using GIGA.AutoRadialLayout;
using UnityEngine;
using UnityEngine.UI;

public class QuestNode_Visuals : MonoBehaviour
{
    [SerializeField] private RadialLayoutNode layoutNode;
    [SerializeField] private Image iconImage;
    [SerializeField] private GameObject selectionImage;

    [Header("Backgrounds")]
    [SerializeField] private GameObject lockedBg;
    [SerializeField] private GameObject unlockedBg;
    [SerializeField] private GameObject activeBg;
    [SerializeField] private GameObject doneBg;
    [SerializeField] private GameObject alternativePlotBg;

    private QuestReferences questRef;
    private bool isDevEditor = false;
    private bool isAlternativePlot = false;

    public string QuestID { get; private set; }
    public RadialLayoutNode LayoutNode { get => layoutNode; }
    public QuestNode_Visuals ParentNodeVisuals { get; private set; }

    public void SetupAlternativePlotBg()
    {
        iconImage.gameObject.SetActiveOptimized(false); 
        lockedBg.SetActiveOptimized(false);
        unlockedBg.SetActiveOptimized(false);
        activeBg.SetActiveOptimized(false);
        doneBg.SetActiveOptimized(false);

        alternativePlotBg.SetActiveOptimized(true);
        GetComponent<ButtonExtended>().interactable = false;
        isAlternativePlot = true;
    }

    public void SetupNode(Sprite iconSprite, string questID, bool isDevEditor, QuestNode_Visuals parentNodeVisuals)
    {
        iconImage.sprite = iconSprite;
        this.QuestID = questID;
        this.isDevEditor = isDevEditor;
        ParentNodeVisuals = parentNodeVisuals;

        alternativePlotBg.SetActiveOptimized(false);
        iconImage.gameObject.SetActiveOptimized(true);

        UpdateVisuals();
    }

    public void UpdateVisuals()
    {
        if (isAlternativePlot)
            return;

        if (questRef == null && isDevEditor == false)
            questRef = PlayerController.Instance.GetQuestReferencesByID(QuestID);

        bool lockNode = isDevEditor;

        if(lockNode == false)
        {
            if(questRef == null)
                lockNode = true;
            else if(questRef.info == null)
                lockNode = true;
        }

        if (lockNode)
        {
            ShowNode(isDevEditor);
            lockedBg.SetActiveOptimized(true);
            unlockedBg.SetActiveOptimized(false);
            activeBg.SetActiveOptimized(false);
            doneBg.SetActiveOptimized(false);
            UpdateLink(0);
        }
        else if (questRef.info.IsDone)
        {
            ShowNode(true);
            lockedBg.SetActiveOptimized(false);
            unlockedBg.SetActiveOptimized(false);
            activeBg.SetActiveOptimized(false);
            doneBg.SetActiveOptimized(true);
            UpdateLink(1);
        }
        else if (questRef.info.timerStarted == false)
        {
            ShowNode(true);
            lockedBg.SetActiveOptimized(false);
            unlockedBg.SetActiveOptimized(true);
            activeBg.SetActiveOptimized(false);
            doneBg.SetActiveOptimized(false);
            UpdateLink(1);
        }
        else if (questRef.info.IsDone == false)
        {
            ShowNode(true);
            lockedBg.SetActiveOptimized(false);
            unlockedBg.SetActiveOptimized(false);
            activeBg.SetActiveOptimized(true);
            doneBg.SetActiveOptimized(false);
            UpdateLink(1);
        }
    }

    public void ToggleSelection(bool value)
    {
        selectionImage.SetActiveOptimized(value);
    }

    private void UpdateLink(float progress)
    {
        if (layoutNode.ArrivingLink != null)
            layoutNode.ArrivingLink.ProgressValue = progress;
    }

    private void ShowNode(bool value)
    {
        transform.localScale = value ? Vector3.one : Vector3.zero;
    }
}
