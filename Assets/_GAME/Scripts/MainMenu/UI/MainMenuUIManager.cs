using MainMenu;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MainMenuUIManager : MonoBehaviour
{
    public static MainMenuUIManager Instance;

    [SerializeField] private MainMenuUI mainMenuUI;
    [SerializeField] private HomeUI homeUI;
    [SerializeField] private UnitsUI unitsUI;
    [SerializeField] private AchievementsUI achievementsUI;

    [SerializeField] private LoadGameUI loadGameUI;
    
    [SerializeField] private ContentUI contentUI;
    [SerializeField] private InputTextUI inputTextUI;
    [SerializeField] private IndicatorTutorialUI indicatorTutorialUI;

    [SerializeField] private SettingsUI settingsUI;

    [SerializeField] private LoadingUI loadingUI;
    [Space]
    [SerializeField] private List<GameObject> uiUnderCoreUI = new List<GameObject>();
    [SerializeField] private List<GameObject> uiAboveCoreUI = new List<GameObject>();
    [Space]
    [SerializeField] private Button invisibleButton;

    private RectTransform rectTransform;

    public MainMenuUI MainMenuUI { get; set; }
    public HomeUI HomeUI { get; set; }
    public UnitsUI UnitsUI { get; set; }
    public AchievementsUI AchievementsUI { get; set; }

    public LoadGameUI LoadGameUI { get; set; }

    public ContentUI ContentUI { get; set; }
    public InputTextUI InputTextUI { get; set; }
    public IndicatorTutorialUI IndicatorTutorialUI { get; set; }

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

            if (SettingsUI.gameObject.activeSelf)
                return SettingsUI;

            if (ContentUI.gameObject.activeSelf)
                return ContentUI;

            if (LoadGameUI.gameObject.activeSelf)
                return LoadGameUI;

            if (InputTextUI.gameObject.activeSelf)
                return InputTextUI;

            if (UnitsUI.gameObject.activeSelf)
                return UnitsUI;

            if (MainMenuUI.gameObject.activeSelf)
                return MainMenuUI;

            if (AchievementsUI.gameObject.activeSelf)
                return AchievementsUI;

            return null;
        }
    }

    public static void ShowMainMenuUI()
    {
        HideAll();
        Instance.MainMenuUI.ShowScreen();
    }

    public static void ShowHomeUI()
    {
        ShowMainMenuUI();
        Instance.HomeUI.ShowScreen();
    }

    public static void ShowCoursesUI()
    {
        ShowMainMenuUI();
        Instance.UnitsUI.ShowScreen();
    }

    public static void ShowUnitsUI()
    {
        ShowMainMenuUI();
        Instance.UnitsUI.ShowScreen();
    }

    public static void ShowAchievementsUI()
    {
        ShowMainMenuUI();
        Instance.AchievementsUI.ShowScreen();
    }

    public static void ShowLoadGameUI()
    {
        ShowMainMenuUI();
        Instance.LoadGameUI.ShowScreen();
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

    public static void ShowSettingsUI()
    {
        ShowMainMenuUI();
        Instance.SettingsUI.ShowScreen();
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
        Instance.MainMenuUI.HideScreen();
        Instance.HomeUI.HideScreen();
        Instance.UnitsUI.HideScreen();
        Instance.AchievementsUI.HideScreen();
        Instance.LoadGameUI.HideScreen();
        Instance.ContentUI.HideScreen();
        Instance.InputTextUI.HideScreen();
        Instance.IndicatorTutorialUI.HideScreen();
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
        ShowHomeUI();
    }

    private void PrepareAllScreens()
    {
        for (int i = 0; i < uiUnderCoreUI.Count; i++)
        {
            Instantiate(uiUnderCoreUI[i], transform).name = uiUnderCoreUI[i].name;
        }

        HomeUI = Instantiate(homeUI, transform);
        UnitsUI = Instantiate(unitsUI, transform);
        AchievementsUI = Instantiate(achievementsUI, transform);
        LoadGameUI = Instantiate(loadGameUI, transform);
        MainMenuUI = Instantiate(mainMenuUI, transform);
        ContentUI = Instantiate(contentUI, transform);
        InputTextUI = Instantiate(inputTextUI, transform);
        IndicatorTutorialUI = Instantiate(indicatorTutorialUI, transform);
        SettingsUI = Instantiate(settingsUI, transform);
        LoadingUI = Instantiate(loadingUI, transform);

        HomeUI.gameObject.name = homeUI.name;
        UnitsUI.gameObject.name = unitsUI.name;
        AchievementsUI.gameObject.name = achievementsUI.name;
        LoadGameUI.gameObject.name = loadGameUI.name;
        MainMenuUI.gameObject.name = mainMenuUI.name;
        ContentUI.gameObject.name = contentUI.name;
        InputTextUI.gameObject.name = inputTextUI.name;
        IndicatorTutorialUI.gameObject.name = indicatorTutorialUI.name;
        SettingsUI.gameObject.name = settingsUI.name;
        LoadingUI.gameObject.name = loadingUI.name;

        for (int i = 0; i < uiAboveCoreUI.Count; i++)
        {
            Instantiate(uiAboveCoreUI[i], transform).name = uiAboveCoreUI[i].name;
        }

        SettingsUI.OnBack.AddListener(ShowHomeUI);
    }
}
