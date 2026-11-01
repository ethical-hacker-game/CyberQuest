using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DevTools;

public class DevEditorUIManager : GameSystemComponent
{
    public static DevEditorUIManager Instance;

    [SerializeField] private ToolsUI toolsUI;

    [SerializeField] private UnitsUI unitsUI;
    [SerializeField] private UnitEditorUI unitEditorUI;
    [SerializeField] private QuestsTreeEditorUI questsTreeEditorUI;
    [SerializeField] private QuestEditorUI questEditorUI;
    [SerializeField] private TaskEditorUI taskEditorUI;
    [SerializeField] private MailEditorUI mailEditorUI;

    [SerializeField] private CommandsUI commandsUI;
    [SerializeField] private CommandEditorUI commandEditorUI;

    [SerializeField] private POIsEditorUI poisEditorUI;
    [SerializeField] private SinglePOIEditorUI singlePOIEditorUI;

    [SerializeField] private AppsUI appsUI;
    [SerializeField] private AppEditorUI appEditorUI;
    [SerializeField] private AppScenarioEditorUI appScenarioEditorUI;
    [SerializeField] private AppScenarioStageEditorUI appScenarioStageEditorUI;

    [SerializeField] private SettingsUI settingsUI;
    [SerializeField] private ContentUI contentUI;
    [SerializeField] private InputTextUI inputTextUI;
    [SerializeField] private PictureSelectionUI pictureSelectionUI;
    [SerializeField] private TextSelectionUI textSelectionUI;
    [SerializeField] private LoadingUI loadingUI;

    [Space]
    [SerializeField] private List<GameObject> uiUnderCoreUI = new List<GameObject>();
    [SerializeField] private List<GameObject> uiAboveCoreUI = new List<GameObject>();
    [Space]
    [SerializeField] private Button invisibleButton;


    private RectTransform rectTransform;

    public ToolsUI ToolsUI { get; private set; }

    public UnitsUI UnitsUI { get; private set; }
    public UnitEditorUI UnitEditorUI { get; private set; }
    public QuestsTreeEditorUI QuestsTreeEditorUI { get; private set; }
    public QuestEditorUI QuestEditorUI { get; private set; }
    public TaskEditorUI TaskEditorUI { get; private set; }
    public MailEditorUI MailEditorUI { get; private set; }

    public CommandsUI CommandsUI { get; private set; }
    public CommandEditorUI CommandEditorUI { get; private set; }

    public POIsEditorUI POIsEditorUI { get; private set; }
    public SinglePOIEditorUI SinglePOIEditorUI { get; private set; }

    public AppsUI AppsUI { get; private set; }
    public AppEditorUI AppEditorUI { get; private set; }
    public AppScenarioEditorUI AppScenarioEditorUI { get; private set; }
    public AppScenarioStageEditorUI AppScenarioStageEditorUI { get; private set; }

    public SettingsUI SettingsUI { get; private set; }
    public ContentUI ContentUI { get; private set; }
    public InputTextUI InputTextUI { get; private set; }
    public PictureSelectionUI PictureSelectionUI { get; private set; }
    public TextSelectionUI TextSelectionUI { get; private set; }
    public LoadingUI LoadingUI { get; private set; }

    public Vector2 CanvasScale => rectTransform.localScale;
    public Vector2 CanvasSize => rectTransform.sizeDelta;

    public UI_Screen CurrentUIScreen
    {
        get
        {
            if (PictureSelectionUI.gameObject.activeSelf)
                return PictureSelectionUI;

            if (TextSelectionUI.gameObject.activeSelf)
                return TextSelectionUI;

            if (InputTextUI.gameObject.activeSelf)
                return InputTextUI;

            if (ToolsUI.gameObject.activeSelf)
                return ToolsUI;

            if (UnitsUI.gameObject.activeSelf)
                return UnitsUI;

            if (UnitEditorUI.gameObject.activeSelf)
                return UnitEditorUI;

            if (QuestsTreeEditorUI.gameObject.activeSelf)
                return QuestsTreeEditorUI;

            if (QuestEditorUI.gameObject.activeSelf)
                return QuestEditorUI;

            if (TaskEditorUI.gameObject.activeSelf)
                return TaskEditorUI;

            if (MailEditorUI.gameObject.activeSelf)
                return MailEditorUI;

            if (CommandsUI.gameObject.activeSelf)
                return CommandsUI;

            if (POIsEditorUI.gameObject.activeSelf)
                return POIsEditorUI;

            if (SinglePOIEditorUI.gameObject.activeSelf)
                return SinglePOIEditorUI;

            if (AppsUI.gameObject.activeSelf)
                return AppsUI;

            if (AppEditorUI.gameObject.activeSelf)
                return AppEditorUI;

            if (AppScenarioEditorUI.gameObject.activeSelf)
                return AppScenarioEditorUI;

            if (AppScenarioStageEditorUI.gameObject.activeSelf)
                return AppScenarioStageEditorUI;

            if (LoadingUI.gameObject.activeSelf)
                return LoadingUI;

            return null;
        }
    }

    public static void ShowToolsUI()
    {
        HideAll();
        Instance.ToolsUI.ShowScreen();
    }

    public static void ShowUnitsUI()
    {
        HideAll();
        Instance.UnitsUI.ShowScreen();
    }

    public static void ShowUnitEditorUI()
    {
        HideAll();
        Instance.UnitEditorUI.ShowScreen();
    }

    public static void ShowQuestsTreeEditorUI()
    {
        HideAll();
        Instance.QuestsTreeEditorUI.ShowScreen();
    }

    public static void ShowQuestEditorUI()
    {
        HideAll();
        Instance.QuestEditorUI.ShowScreen();
    }

    public static void ShowTaskEditorUI()
    {
        HideAll();
        Instance.TaskEditorUI.ShowScreen();
    }

    public static void ShowMailEditorUI()
    {
        HideAll();
        Instance.MailEditorUI.ShowScreen();
    }

    public static void ShowCommandsUI()
    {
        HideAll();
        Instance.CommandsUI.ShowScreen();
    }

    public static void ShowCommandEditorUI()
    {
        HideAll();
        Instance.CommandEditorUI.ShowScreen();
    }

    public static void ShowPOIsEditorUI()
    {
        HideAll();
        Instance.POIsEditorUI.ShowScreen();
    }

    public static void ShowSinglePOIEditorUI()
    {
        HideAll();
        Instance.SinglePOIEditorUI.ShowScreen();
    }

    public static void ShowAppsUI()
    {
        HideAll();
        Instance.AppsUI.ShowScreen();
    }

    public static void ShowAppEditorUI()
    {
        HideAll();
        Instance.AppEditorUI.ShowScreen();
    }

    public static void ShowAppScenarioEditorUI()
    {
        HideAll();
        Instance.AppScenarioEditorUI.ShowScreen();
    }

    public static void ShowAppScenarioStageEditorUI()
    {
        HideAll();
        Instance.AppScenarioStageEditorUI.ShowScreen();
    }

    public static void ShowSettingsUI()
    {
        HideAll();
        Instance.SettingsUI.ShowScreen();
    }

    public static void ShowContentUI()
    {
        Instance.ContentUI.ShowScreen();
    }

    public static void ShowInputTextUI()
    {
        Instance.InputTextUI.ShowScreen();
    }

    public static void ShowPictureSelectionUI()
    {
        Instance.PictureSelectionUI.ShowScreen();
    }

    public static void ShowTextSelectionUI()
    {
        Instance.TextSelectionUI.ShowScreen();
    }

    public static void ShowLoadingUI()
    {
        HideAll();
        Instance.LoadingUI.ShowScreen();
    }

    public static void SelectInvisibleButton()
    {
        Instance.invisibleButton.Select();
    }

    public static void HideAll()
    {
        Instance.ToolsUI.HideScreen();

        Instance.UnitsUI.HideScreen();
        Instance.UnitEditorUI.HideScreen();
        Instance.QuestsTreeEditorUI.HideScreen();
        Instance.QuestEditorUI.HideScreen();
        Instance.TaskEditorUI.HideScreen();
        Instance.MailEditorUI.HideScreen();

        Instance.CommandsUI.HideScreen();
        Instance.CommandEditorUI.HideScreen();

        Instance.POIsEditorUI.HideScreen();
        Instance.SinglePOIEditorUI.HideScreen();

        Instance.AppsUI.HideScreen();
        Instance.AppEditorUI.HideScreen();
        Instance.AppScenarioEditorUI.HideScreen();
        Instance.AppScenarioStageEditorUI.HideScreen();

        Instance.SettingsUI.HideScreen();
        Instance.ContentUI.HideScreen();
        Instance.InputTextUI.HideScreen();
        Instance.PictureSelectionUI.HideScreen();
        Instance.TextSelectionUI.HideScreen();
        Instance.LoadingUI.HideScreen();

        CursorHintsManager.ForceClearHint();
    }

    private void Awake()
    {
        Instance = this;
        rectTransform = GetComponent<RectTransform>();
        PrepareAllScreens();
    }

    private void Start()
    {
        ShowToolsUI();
    }

    private void PrepareAllScreens()
    {
        // Instantiate all UI screens under core UI
        for (int i = 0; i < uiUnderCoreUI.Count; i++)
        {
            Instantiate(uiUnderCoreUI[i], transform).name = uiUnderCoreUI[i].name;
        }

        ToolsUI = Instantiate(toolsUI, transform);

        UnitsUI = Instantiate(unitsUI, transform);
        UnitEditorUI = Instantiate(unitEditorUI, transform);
        QuestsTreeEditorUI = Instantiate(questsTreeEditorUI, transform);
        QuestEditorUI = Instantiate(questEditorUI, transform);
        TaskEditorUI = Instantiate(taskEditorUI, transform);
        MailEditorUI = Instantiate(mailEditorUI, transform);

        CommandsUI = Instantiate(commandsUI, transform);
        CommandEditorUI = Instantiate(commandEditorUI, transform);

        POIsEditorUI = Instantiate(poisEditorUI, transform);
        SinglePOIEditorUI = Instantiate(singlePOIEditorUI, transform);

        AppsUI = Instantiate(appsUI, transform);
        AppEditorUI = Instantiate(appEditorUI, transform);
        AppScenarioEditorUI = Instantiate(appScenarioEditorUI, transform);
        AppScenarioStageEditorUI = Instantiate(appScenarioStageEditorUI, transform);

        SettingsUI = Instantiate(settingsUI, transform);
        ContentUI = Instantiate(contentUI, transform);
        InputTextUI = Instantiate(inputTextUI, transform);
        PictureSelectionUI = Instantiate(pictureSelectionUI, transform);
        TextSelectionUI = Instantiate(textSelectionUI, transform);
        LoadingUI = Instantiate(loadingUI, transform);


        ToolsUI.gameObject.name = toolsUI.name;

        UnitsUI.gameObject.name = unitsUI.name;
        UnitEditorUI.gameObject.name = unitEditorUI.name;
        QuestsTreeEditorUI.gameObject.name = questsTreeEditorUI.name;
        QuestEditorUI.gameObject.name = questEditorUI.name;
        TaskEditorUI.gameObject.name = taskEditorUI.name;
        MailEditorUI.gameObject.name = mailEditorUI.name;

        CommandsUI.gameObject.name = commandsUI.name;
        CommandEditorUI.gameObject.name = commandEditorUI.name;

        POIsEditorUI.gameObject.name = poisEditorUI.name;
        SinglePOIEditorUI.gameObject.name = singlePOIEditorUI.name;

        AppsUI.gameObject.name = appsUI.name;
        AppEditorUI.gameObject.name = appEditorUI.name;
        AppScenarioEditorUI.gameObject.name = appScenarioEditorUI.name;
        AppScenarioStageEditorUI.gameObject.name = appScenarioStageEditorUI.name;

        SettingsUI.gameObject.name = settingsUI.name;
        ContentUI.gameObject.name = contentUI.name;
        InputTextUI.gameObject.name = inputTextUI.name;
        PictureSelectionUI.gameObject.name = pictureSelectionUI.name;
        TextSelectionUI.gameObject.name = textSelectionUI.name;
        LoadingUI.gameObject.name = loadingUI.name;

        for (int i = 0; i < uiAboveCoreUI.Count; i++)
        {
            Instantiate(uiAboveCoreUI[i], transform).name = uiAboveCoreUI[i].name;
        }

        SettingsUI.OnBack.AddListener(ShowToolsUI);
    }
}
