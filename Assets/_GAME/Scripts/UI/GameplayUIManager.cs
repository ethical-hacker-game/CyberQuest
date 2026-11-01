using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GameplayUIManager : GameSystemComponent
{
    private enum SettingsBackTarget
    {
        Game,
        Pause
    }

    public static GameplayUIManager Instance;

    [SerializeField] private GameUI gameUI;
    [SerializeField] private StatsUI statsUI;
    [SerializeField] private ActionsUI actionsUI;
    [SerializeField] private AssistantUI assistantUI;

    [SerializeField] private QuestsUI questsUI;
    [SerializeField] private ProgressUI progressUI;
    [SerializeField] private MailsUI mailsUI;
    [SerializeField] private CyberpediaUI cyberpediaUI;
    [SerializeField] private LeaderboardUI leaderboardUI;
    [SerializeField] private MarketplaceUI marketplaceUI;

    [SerializeField] private ContentUI contentUI;
    [SerializeField] private InputTextUI inputTextUI;
    [SerializeField] private IndicatorTutorialUI indicatorTutorialUI;

    [SerializeField] private PauseUI pauseUI;
    [SerializeField] private SettingsUI settingsUI;

    [SerializeField] private LoadingUI loadingUI;
    [Space]
    [SerializeField] private List<GameObject> uiUnderCoreUI = new List<GameObject>();
    [SerializeField] private List<GameObject> uiAboveCoreUI = new List<GameObject>();
    [Space]
    [SerializeField] private Button invisibleButton;


    private RectTransform rectTransform;
    private SettingsBackTarget settingsBackTarget = SettingsBackTarget.Game;

    public GameUI GameUI { get; set; }
    public StatsUI StatsUI { get; set; }
    public ActionsUI ActionsUI { get; set; }
    public AssistantUI AssistantUI { get; set; }

    public QuestsUI QuestsUI { get; set; }
    public ProgressUI ProgressUI { get; set; }
    public MailsUI MailsUI { get; set; }
    public CyberpediaUI CyberpediaUI { get; set; }
    public LeaderboardUI LeaderboardUI { get; set; }
    public MarketplaceUI MarketplaceUI { get; set; }

    public ContentUI ContentUI { get; set; }
    public InputTextUI InputTextUI { get; set; }
    public IndicatorTutorialUI IndicatorTutorialUI { get; set; }

    public PauseUI PauseUI { get; set; }
    public SettingsUI SettingsUI { get; set; }

    public LoadingUI LoadingUI { get; set; }


    public Vector2 CanvasScale { get => rectTransform.localScale; }
    public Vector2 CanvasSize { get => rectTransform.sizeDelta; }

    public UI_Screen CurrentUIScreen
    {
        get
        {
            if (IndicatorTutorialUI.gameObject.activeSelf)
                return IndicatorTutorialUI;

            if (PauseUI.gameObject.activeSelf)
                return PauseUI;

            if (ContentUI.gameObject.activeSelf)
                return ContentUI;

            if (InputTextUI.gameObject.activeSelf)
                return InputTextUI;

            if (GameUI.gameObject.activeSelf)
                return GameUI;

            if (QuestsUI.gameObject.activeSelf)
                return QuestsUI;

            if (ProgressUI.gameObject.activeSelf)
                return ProgressUI;

            if (MailsUI.gameObject.activeSelf)
                return MailsUI;

            if (CyberpediaUI.gameObject.activeSelf)
                return CyberpediaUI;

            if (LeaderboardUI.gameObject.activeSelf)
                return LeaderboardUI;

            if (MarketplaceUI.gameObject.activeSelf)
                return MarketplaceUI;

            if (SettingsUI.gameObject.activeSelf)
                return SettingsUI;

            return null;
        }
    }

    public static void ShowGameUI()
    {
        HideAll();
        Instance.GameUI.ShowScreen();
        Instance.StatsUI.ShowScreen();
        Instance.StatsUI.ToggleButtons(true);
        Instance.ActionsUI.ShowScreen();
        Instance.AssistantUI.ShowScreen();
    }

    public static void ShowQuestsUI()
    {
        HideAll();
        Instance.StatsUI.ShowScreen();
        Instance.StatsUI.ToggleButtons(false);
        Instance.QuestsUI.ShowScreen();
    }

    public static void ShowProgressUI()
    {
        HideAll();
        Instance.StatsUI.ShowScreen();
        Instance.StatsUI.ToggleButtons(false);
        Instance.ProgressUI.ShowScreen();
    }

    public static void ShowMailsUI()
    {
        HideAll();
        Instance.StatsUI.ShowScreen();
        Instance.StatsUI.ToggleButtons(false);
        Instance.MailsUI.ShowScreen();
    }

    public static void ShowCyberpediaUI()
    {
        HideAll();
        Instance.StatsUI.ShowScreen();
        Instance.StatsUI.ToggleButtons(false);
        Instance.CyberpediaUI.ShowScreen();
    }

    public static void ShowLeaderboardUI()
    {
        HideAll();
        Instance.LeaderboardUI.ShowScreen();
    }

    public static void ShowMarketplaceUI()
    {
        HideAll();
        Instance.StatsUI.ShowScreen();
        Instance.StatsUI.ToggleButtons(false);
        Instance.MarketplaceUI.ShowScreen();
    }

    public static void ShowContentUI()
    {
        HideAll();
        Instance.ContentUI.ShowScreen();
    }

    public static void ShowInputTextUI()
    {
        HideAll();
        Instance.InputTextUI.ShowScreen();
    }

    public static void ShowIndicatorTutorialUI()
    {
        Instance.IndicatorTutorialUI.ShowScreen();
    }

    public static void ShowPauseUI()
    {
        ShowGameUI();

        ContentUI.SetupContentUI(
            "Game Paused",
            null,
            "The game is currently paused. You can return to the game, open settings, or exit to the main menu.",
            "Resume Game",
            "Settings",
            "Main Menu");

        ContentUI.ButtonEvents[0].AddListener(ShowGameUI);
        ContentUI.ButtonEvents[1].AddListener(ShowSettingsFromPauseUI);
        ContentUI.ButtonEvents[2].AddListener(() =>
        {
            SceneLoader.LoadSceneAsync(SceneLoader.MAIN_MENU_SCENE_INDEX);
        });

        ContentUI.DefaultButtonIndex = 0;
    }

    public static void ShowSettingsUI()
    {
        ShowSettingsUI(SettingsBackTarget.Game);
    }

    private static void ShowSettingsFromPauseUI()
    {
        ShowSettingsUI(SettingsBackTarget.Pause);
    }

    private static void ShowSettingsUI(SettingsBackTarget backTarget)
    {
        HideAll();
        Instance.settingsBackTarget = backTarget;
        Instance.SettingsUI.ShowScreen();
    }

    private static void BackFromSettingsUI()
    {
        switch (Instance.settingsBackTarget)
        {
            case SettingsBackTarget.Pause:
                ShowPauseUI();
                break;
            case SettingsBackTarget.Game:
            default:
                ShowGameUI();
                break;
        }
    }

    public static void ShowLoadingUI()
    {
        Instance.LoadingUI.ShowScreen();
    }

    public static void SelectInvisibleButton()
    {
        Instance.invisibleButton.Select();
    }

    public static void HideAll()
    {
        Instance.GameUI.HideScreen();
        Instance.StatsUI.HideScreen();
        Instance.ActionsUI.HideScreen();
        Instance.AssistantUI.HideScreen();

        Instance.QuestsUI.HideScreen();
        Instance.ProgressUI.HideScreen();
        Instance.MailsUI.HideScreen();
        Instance.CyberpediaUI.HideScreen();
        Instance.LeaderboardUI.HideScreen();
        Instance.MarketplaceUI.HideScreen();

        Instance.ContentUI.HideScreen();
        Instance.InputTextUI.HideScreen();
        Instance.IndicatorTutorialUI.HideScreen();

        Instance.PauseUI.HideScreen();
        Instance.SettingsUI.HideScreen();

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
        ShowGameUI();
    }

    private void PrepareAllScreens()
    {
        for (int i = 0; i < uiUnderCoreUI.Count; i++)
        {
            Instantiate(uiUnderCoreUI[i], transform).name = uiUnderCoreUI[i].name;
        }

        GameUI = Instantiate(gameUI, transform);
        ActionsUI = Instantiate(actionsUI, transform);
        AssistantUI = Instantiate(assistantUI, transform);

        QuestsUI = Instantiate(questsUI, transform);
        ProgressUI = Instantiate(progressUI, transform);
        MailsUI = Instantiate(mailsUI, transform);
        CyberpediaUI = Instantiate(cyberpediaUI, transform);
        LeaderboardUI = Instantiate(leaderboardUI, transform);
        MarketplaceUI = Instantiate(marketplaceUI, transform);

        ContentUI = Instantiate(contentUI, transform);
        InputTextUI = Instantiate(inputTextUI, transform);

        StatsUI = Instantiate(statsUI, transform);
        PauseUI = Instantiate(pauseUI, transform);
        SettingsUI = Instantiate(settingsUI, transform);

        IndicatorTutorialUI = Instantiate(indicatorTutorialUI, transform);
        LoadingUI = Instantiate(loadingUI, transform);


        StatsUI.gameObject.name = statsUI.name;
        GameUI.gameObject.name = gameUI.name;
        ActionsUI.gameObject.name = actionsUI.name;
        AssistantUI.gameObject.name = assistantUI.name;

        QuestsUI.gameObject.name = questsUI.name;
        ProgressUI.gameObject.name = progressUI.name;
        MailsUI.gameObject.name = mailsUI.name;
        CyberpediaUI.gameObject.name = cyberpediaUI.name;
        LeaderboardUI.gameObject.name = leaderboardUI.name;
        MarketplaceUI.gameObject.name = marketplaceUI.name;

        ContentUI.gameObject.name = contentUI.name;
        InputTextUI.gameObject.name = inputTextUI.name;

        PauseUI.gameObject.name = pauseUI.name;
        SettingsUI.gameObject.name = settingsUI.name;

        IndicatorTutorialUI.gameObject.name = indicatorTutorialUI.name;
        LoadingUI.gameObject.name = loadingUI.name;


        for (int i = 0; i < uiAboveCoreUI.Count; i++)
        {
            Instantiate(uiAboveCoreUI[i], transform).name = uiAboveCoreUI[i].name;
        }

        SettingsUI.OnBack.AddListener(BackFromSettingsUI);
    }
}
