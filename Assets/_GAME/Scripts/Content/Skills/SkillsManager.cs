using System;
using System.Collections.Generic;

/// <summary>
/// Loads per-unit skill definitions (units/&lt;unitID&gt;/skills/*.skill) and owns the unlock logic.
/// Unlocked skill IDs are stored in UnitInfo.skills, so save compatibility with the old
/// hardcoded Marketplace nodes (e.g. "anonsurf") is preserved.
/// </summary>
public static class SkillsManager
{
    public static event Action<SkillData> OnSkillUnlocked;

    private static List<SkillData> skills = new List<SkillData>();
    private static bool IsInitialized { get; set; } = false;

    public static List<SkillData> Skills
    {
        get
        {
            if (IsInitialized == false)
                LoadSkills();

            return skills;
        }
    }

    public static void LoadSkills(string unitPrefix = "", bool forceReload = false)
    {
        if (IsInitialized && forceReload == false)
            return;

        IsInitialized = true;
        string skillsPath = ContentPaths.UnitFolderOrAll(unitPrefix, ContentConstValues.FOLDER_SKILLS);
        skills = ContentLoader.ReturnListOfType<SkillData>(skillsPath, ContentConstValues.EXTENSION_SKILL);
        skills.Sort(CompareSkills);
    }

    public static SkillData GetSkillByID(string skillID)
    {
        if (Skills.IsNullOrEmpty())
            return null;

        for (int i = 0; i < Skills.Count; i++)
        {
            if (Skills[i].skillID == skillID)
                return Skills[i];
        }

        return null;
    }

    public static bool IsUnlocked(string skillID)
    {
        if (PlayerController.CurrentUnitInfo == null)
            return false;

        if (PlayerController.CurrentUnitInfo.skills.IsNullOrEmpty())
            return false;

        return PlayerController.CurrentUnitInfo.skills.Contains(skillID);
    }

    public static SkillUnlockState GetUnlockState(SkillData skill)
    {
        if (skill == null)
            return SkillUnlockState.None;

        if (IsUnlocked(skill.skillID))
            return SkillUnlockState.Unlocked;

        if (skill.lockInDemo)
            return SkillUnlockState.DemoLocked;

        if (IsTaskUnlockAvailable(skill) == false)
            return SkillUnlockState.TaskLocked;

        if (SD_MoenyLabel.Money < skill.price)
            return SkillUnlockState.NotEnoughMoney;

        return SkillUnlockState.Available;
    }

    public static bool TryUnlock(SkillData skill)
    {
        if (GetUnlockState(skill) != SkillUnlockState.Available)
            return false;

        if (PlayerController.CurrentUnitInfo == null)
            return false;

        PlayerController.CurrentUnitInfo.skills ??= new List<string>();
        PlayerController.CurrentUnitInfo.skills.Add(skill.skillID);
        SD_MoenyLabel.ChangeMoney(-skill.price);

        QuestActionsBridge.CallQuestAction(null,
            QuestActionsBridge.ACTION_TYPE.skill_unlocked,
            skill.skillID, out _);

        OnSkillUnlocked?.Invoke(skill);
        return true;
    }

    public static string GetStateDescription(SkillUnlockState state)
    {
        switch (state)
        {
            case SkillUnlockState.Unlocked:
                return "UNLOCKED";

            case SkillUnlockState.DemoLocked:
                return "NOT AVAILABLE IN THE DEMO";

            case SkillUnlockState.TaskLocked:
                return "AVAILABLE AFTER REACHING A CERTAIN STORY POINT";

            case SkillUnlockState.NotEnoughMoney:
                return "NOT ENOUGH MONEY";

            case SkillUnlockState.Available:
                return "READY TO UNLOCK";

            default:
                return string.Empty;
        }
    }

    private static bool IsTaskUnlockAvailable(SkillData skill)
    {
        if (skill.requireTask == false)
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

        if (taskVariant.actionTag != skill.skillID)
            return false;

        return true;
    }

    private static int CompareSkills(SkillData a, SkillData b)
    {
        int orderComparison = a.order.CompareTo(b.order);

        if (orderComparison != 0)
            return orderComparison;

        return string.Compare(a.skillName, b.skillName, StringComparison.OrdinalIgnoreCase);
    }

    [Serializable]
    public enum SkillUnlockState
    {
        None,
        Unlocked,
        DemoLocked,
        TaskLocked,
        NotEnoughMoney,
        Available
    }
}
