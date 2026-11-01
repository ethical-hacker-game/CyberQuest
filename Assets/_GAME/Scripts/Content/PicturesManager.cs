using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Linq;
using System;

public class PicturesManager : GameSystemComponent
{
    public const string QUEST_TASK_ICONS_RESOURCES_PATH = "QuestTaskIcons";
    public const string CHARACTERS_PICTURES_PATH = "Characters";
    public const string POIS_PICTURES_PATH = "POIsIcons";
    public const string PERSONA_PICTURES_PATH = "PersonaIcons";
    public const string SKILLS_PICTURES_PATH = "SkillsIcons";

    public static PicturesManager Instance { get; private set; }

    [Header("Defaults")]
    [SerializeField, SpritePreview] private Sprite defaultPicture;
    [SerializeField, SpritePreview] private Sprite defaultQuestPicture;
    [SerializeField, SpritePreview] private Sprite defaultCharacterPicture;
    [SerializeField, SpritePreview] private Sprite defaultPOIPicture;
    [SerializeField, SpritePreview] private Sprite defaultPersonaPicture;
    [SerializeField, SpritePreview] private Sprite defaultSkillPicture;
    [SerializeField, SpritePreview] private Sprite defaultAppIcon;

    [Header("Lists")]

    [ShowInInspector]
    public static List<PictureReferences> QuestPictures { get; private set; } = new List<PictureReferences>();
    [ShowInInspector]
    public static List<PictureReferences> CharacterPictures { get; private set; } = new List<PictureReferences>();
    [ShowInInspector]
    public static List<PictureReferences> POIsPictures { get; private set; } = new List<PictureReferences>();
    [ShowInInspector]
    public static List<PictureReferences> PersonaPictures { get; private set; } = new List<PictureReferences>();
    [ShowInInspector]
    public static List<PictureReferences> SkillPictures { get; private set; } = new List<PictureReferences>();

    private static readonly Dictionary<TaskBase.TASK_TYPE, Sprite> questTaskIconCache = new Dictionary<TaskBase.TASK_TYPE, Sprite>();

    public static Sprite DefaultPicture => Instance.defaultPicture;
    public static Sprite DefaultQuestPicture => Instance.defaultQuestPicture;
    public static Sprite DefaultCharacterPicture => Instance.defaultCharacterPicture;
    public static Sprite DefaultPOIPicture => Instance.defaultPOIPicture;
    public static Sprite DefaultPersonaPicture => Instance.defaultPersonaPicture;
    public static Sprite DefaultSkillPicture => Instance.defaultSkillPicture != null ? Instance.defaultSkillPicture : Instance.defaultPicture;
    public static Sprite DefaultAppIcon => Instance.defaultAppIcon;

    /// <summary>
    /// Built-in white-on-transparent quest icons shipped with the game (not unit content).
    /// One icon per task type under Resources/QuestTaskIcons/{TaskType}.
    /// </summary>
    public static Sprite GetQuestIconForTaskType(TaskBase.TASK_TYPE taskType)
    {
        if (taskType == TaskBase.TASK_TYPE.None)
            return Instance != null ? DefaultQuestPicture : null;

        if (questTaskIconCache.TryGetValue(taskType, out Sprite cached) && cached != null)
            return cached;

        Sprite loaded = Resources.Load<Sprite>($"{QUEST_TASK_ICONS_RESOURCES_PATH}/{taskType}");
        if (loaded != null)
        {
            questTaskIconCache[taskType] = loaded;
            return loaded;
        }

        return Instance != null ? DefaultQuestPicture : null;
    }

    public static void RefreshQuestTaskIconList()
    {
        QuestPictures.Clear();
        questTaskIconCache.Clear();

        Sprite[] icons = Resources.LoadAll<Sprite>(QUEST_TASK_ICONS_RESOURCES_PATH);
        if (icons == null || icons.Length == 0)
            return;

        for (int i = 0; i < icons.Length; i++)
        {
            Sprite icon = icons[i];
            if (icon == null)
                continue;

            QuestPictures.Add(new PictureReferences(icon.name, icon));

            if (Enum.TryParse(icon.name, out TaskBase.TASK_TYPE taskType) && taskType != TaskBase.TASK_TYPE.None)
                questTaskIconCache[taskType] = icon;
        }
    }

    public static PictureReferences GetPictureReferences(string pictureID, PictureType pictureType)
    {
        List<PictureReferences> selectedList;

        switch (pictureType)
        {
            case PictureType.Quest:
                selectedList = QuestPictures;
                break;
            case PictureType.Character:
                selectedList = CharacterPictures;
                break;
            case PictureType.POI:
                selectedList = POIsPictures;
                break;
            case PictureType.Persona:
                selectedList = PersonaPictures;
                break;
            case PictureType.Skill:
                selectedList = SkillPictures;
                break;
            default:
                selectedList = new List<PictureReferences>();
                break;
        }

        for (int i = 0; i < selectedList.Count; i++)
        {
            if (selectedList[i].pictureID == pictureID)
                return selectedList[i];
        }

        return null;
    }

    public static Sprite GetPicture(string pictureID, PictureType pictureType)
    {
        PictureReferences picRef = GetPictureReferences(pictureID, pictureType);

        if (picRef != null)
            return picRef.Picture;

        switch (pictureType)
        {
            case PictureType.Quest:
                if (Enum.TryParse(pictureID, out TaskBase.TASK_TYPE taskType))
                    return GetQuestIconForTaskType(taskType);
                return Instance.defaultQuestPicture;

            case PictureType.Character:
                return Instance.defaultCharacterPicture;

            case PictureType.POI:
                return Instance.defaultPOIPicture;

            case PictureType.Persona:
                return Instance.defaultPersonaPicture;

            case PictureType.Skill:
                return DefaultSkillPicture;

            default:
                return Instance.defaultPicture;
        }
    }

    public static void LoadPictures(List<PictureReferences> selectedList, string path)
    {
        selectedList.Clear();

        Dictionary<string, Texture2D> questPNGs = ContentLoader.ReturnPictures(path);

        for (int i = 0; i < questPNGs.Count; i++)
        {
            KeyValuePair<string, Texture2D> png = questPNGs.ElementAt(i);

            PictureReferences picture = new PictureReferences(png.Key, png.Value);
            selectedList.Add(picture);
        }
    }

    public static void LoadUnitPictures(string unitID)
    {
        if (Instance == null)
            return;

        Instance.LoadAllPictures(unitID);
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            gameObject.Destroy();
            return;
        }

        Instance = this;
        gameObject.DontDestroyOnLoadImproved();

        LoadAllPictures();
    }

    private void LoadAllPictures()
    {
        if (GameController.SelectedUnit != null)
        {
            LoadAllPictures(GameController.SelectedUnit.unitID);
            return;
        }

        QuestPictures.Clear();
        CharacterPictures.Clear();
        POIsPictures.Clear();
        PersonaPictures.Clear();
        SkillPictures.Clear();
        questTaskIconCache.Clear();
        RefreshQuestTaskIconList();
    }

    private void LoadAllPictures(string unitID)
    {
        string graphicsPath = ContentPaths.UnitFolder(unitID, ContentConstValues.FOLDER_GRAPHICS);

        RefreshQuestTaskIconList();
        LoadPictures(CharacterPictures, ContentLoader.CombinePath(graphicsPath, CHARACTERS_PICTURES_PATH));
        LoadPictures(POIsPictures, ContentLoader.CombinePath(graphicsPath, POIS_PICTURES_PATH));
        LoadPictures(PersonaPictures, ContentLoader.CombinePath(graphicsPath, PERSONA_PICTURES_PATH));
        LoadPictures(SkillPictures, ContentLoader.CombinePath(graphicsPath, SKILLS_PICTURES_PATH));
    }

    [Serializable]
    public enum PictureType
    {
        None,
        Quest,
        Character,
        POI,
        Persona,
        Skill
    }
}
