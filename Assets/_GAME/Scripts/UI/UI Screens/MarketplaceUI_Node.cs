using GIGA.AutoRadialLayout;
using Michsky.UI.Beam;
using RuntimeInspectorNamespace;
using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.UI;

public class MarketplaceUI_Node : MonoBehaviour
{
    [SerializeField] private string skillID;
    [SerializeField] private Sprite skillIcon;
    [SerializeField] private string skillName;
    [SerializeField, TextArea(3, 10)] private string skillDescription;

    [Space]
    [SerializeField] private int price = 90000;
    [SerializeField] private bool requireTask;
    [SerializeField] private bool lockInDemo = true;

    [Space]
    [SerializeField] private TextMeshProUGUI label;
    [SerializeField] private Image image;
    [SerializeField] private GameObject unlockedBG;
    [SerializeField] private UICursorHint hintObj;

    private bool Unlocked
    {
        get
        {
            if (PlayerController.CurrentUnitInfo == null)
                return false;

            if (PlayerController.CurrentUnitInfo.skills.IsNullOrEmpty())
                return false;

            return PlayerController.CurrentUnitInfo.skills.Contains(skillID);
        }
    }

    public void Click()
    {
        if (Unlocked)
            return;

        if(SD_MoenyLabel.Money < price)
            return;

        if (CheckTask() == false)
            return;

        PlayerController.CurrentUnitInfo.skills.Add(skillID);
        unlockedBG.SetActive(true);
        SD_MoenyLabel.ChangeMoney(-price);
        QuestActionsBridge.CallQuestAction(null,
            QuestActionsBridge.ACTION_TYPE.skill_unlocked,
            skillID, out _);

        UpdateCursorHint();
    }

#if UNITY_EDITOR
    [Button("Update elements")]
    private void SetElements()
    {
        if (label.text != skillName)
        {
            label.text = skillName;
            EditorUtility.SetDirty(label);
        }

        if (image.sprite != skillIcon)
        {
            image.sprite = skillIcon;
            EditorUtility.SetDirty(image);
        }
    }
#endif

    private void Awake()
    {
        skillDescription = UtilityMethods.WrapText(skillDescription, 55);
    }

    private void Start()
    {
        unlockedBG.SetActive(false);
    }

    private void OnEnable()
    {
        if (Application.isPlaying == false)
            return;

        UpdateCursorHint();
    }

    private void UpdateCursorHint()
    {
        unlockedBG.SetActive(Unlocked);

        string cursorHint = $"<b>{skillName}</b>" +
            $"\n{skillDescription}";

        if (lockInDemo)
        {
            cursorHint = "<color=red><b>Not available in the demo.</b></color>\n\n" + cursorHint;
        }
        else if (Unlocked)
        {
            cursorHint += "\n\n<color=green>Skill unlocked.</color>";
        }
        else
        {
            cursorHint += $"\n\nPrice: {price}";
            
            if (SD_MoenyLabel.Money < price)
                cursorHint += $"\n\n<color=red>Not enough money to unlock skill.</color>";

            if (CheckTask() == false)
                cursorHint += "\n<color=red>Available after reaching a certain story point.</color>";
        }

        hintObj.HintText = cursorHint;
    }

    private bool CheckTask()
    {
        if(requireTask == false)
            return true;

        if (PlayerController.Instance == null)
            return false;

        if (PlayerController.Instance.ActiveTask == null)
            return false;

        if (PlayerController.Instance.ActiveTask.data == null)
            return false;

        TaskBase task = PlayerController.Instance.ActiveTask.data;

        if (task.TaskType != TaskBase.TASK_TYPE.Action)
            return false;

        TaskActionVariant taskVariant = (TaskActionVariant)(task.GetAdaptedTaskVariant());

        if (taskVariant.actionType != QuestActionsBridge.ACTION_TYPE.skill_unlocked)
            return false;

        if (taskVariant.actionTag != skillID)
            return false;

        return true;
    }
}
