using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class GameUI : UI_Screen
{
    public static GameUI Instance => GameplayUIManager.Instance.GameUI;
    
    [SerializeField] private RectTransform windowsArea;
    [SerializeField] private RectTransform questArea;

    [Header("Windows")]
    [SerializeField] private WindowNotification notificationWindowPrefab;
    [SerializeField] private WindowCommandLine commandLineWindowPrefab;
    [SerializeField] private WindowWebBrowser webBrowserWindowPrefab;
    [SerializeField] private WindowApp windowAppPrefab;

    [Header("Quest Elements")]
    [SerializeField] private GameUI_QuestElement_SingleChoice questSingleChoiceWindowPrefab;
    [SerializeField] private GameUI_QuestElement_TrueFalse questTrueFalseWindowPrefab;
    [SerializeField] private GameUI_QuestElement_Input questInputWindowPrefab;
    [SerializeField] private GameUI_QuestElement_Website questWebsiteWindowPrefab;
    [SerializeField] private GameUI_QuestElement_Action questActionWindowPrefab;
    [SerializeField] private GameUI_QuestElement_Decision questDecisionWindowPrefab;
    [SerializeField] private GameUI_QuestElement_Minigame questMinigameWindowPrefab;
    [SerializeField] private GameUI_QuestElement_Dialogue questDialogueWindowPrefab;

    private GameUI_QuestElementBase currentQuestWindow;
    private Dictionary<WindowBase, ButtonExtended> windowsAndButtons = new Dictionary<WindowBase, ButtonExtended>();

    public WindowCommandLine CommandLineWindow { get; private set; }
    public WindowWebBrowser WebBrowserWindow { get; private set; }

    public static RectTransform WindowsArea => Instance.windowsArea;
    public static float MarginLeft => WindowsArea.offsetMin.x;
    public static float MarginRight => WindowsArea.offsetMax.x;
    public static float MarginTop => WindowsArea.offsetMax.y;
    public static float MarginBottom => WindowsArea.offsetMin.y;

    public override void Back()
    {
        try
        {
            windowsArea.GetComponentsInChildren<WindowBase>(false)?.Last().ClickClose();
        }
        catch
        {
            GameplayUIManager.Instance.ActionsUI.Back();
        }
    }

    public WindowNotification SpawnNotificationWindow(string title, string content)
    {
        WindowNotification tempWindow = Instantiate(notificationWindowPrefab, windowsArea).GetComponent<WindowNotification>();
        tempWindow.SetupWindow(title, content);

        ButtonExtended windowButton = ActionsUI.Instance.AddWindowButton(title, tempWindow.Icon).GetComponent<ButtonExtended>();
        windowButton.onClick.AddListener(tempWindow.WindowFlexibilityComponent.MaximizeWindow);
        windowsAndButtons.Add(tempWindow, windowButton);

        return tempWindow;
    }

    public WindowApp SpawnAppWindow(AppReferences appReferences)
    {
        WindowApp tempWindow = Instantiate(windowAppPrefab, windowsArea).GetComponent<WindowApp>();
        tempWindow.SetupAppWindow(appReferences);
        ButtonExtended windowButton = 
            ActionsUI.Instance.AddWindowButton(appReferences.appCore.header, appReferences.appCore.Icon)
            .GetComponent<ButtonExtended>();
        windowButton.onClick.AddListener(tempWindow.WindowFlexibilityComponent.MaximizeWindow);
        windowsAndButtons.Add(tempWindow, windowButton);
        return tempWindow;
    }

    public GameUI_QuestElementBase SpawnQuestWindow(QuestReferences questRef, TaskReferences taskRef, GameUI_QuestElementBase previousQuestWindow = null)
    {
        if(currentQuestWindow != null)
        {
            currentQuestWindow.QuestRef.window = null;
            Destroy(currentQuestWindow.gameObject);
        }

        GameUI_QuestElementBase questWindowPrefab = GetQuestWindowOfType(taskRef.data.TaskType);

        GameUI_QuestElementBase tempWindow = Instantiate(questWindowPrefab, questArea).GetComponent<GameUI_QuestElementBase>();
        tempWindow.SetupQuestUIElement(questRef, taskRef, previousQuestWindow);

        currentQuestWindow = tempWindow;

        //obsolete - preiously quest windows had their own buttons
        //ButtonExtended windowButton = ActionsUI.Instance.AddWindowButton(questRef.data.GetQuestName(questRef.info.persona), tempWindow.Icon).GetComponent<ButtonExtended>();
        //windowButton.onClick.AddListener(tempWindow.WindowFlexibilityComponent.MaximizeWindow);
        //windowsAndButtons.Add(tempWindow, windowButton);

        return tempWindow;
    }

    public void MinimizeAllWindows()
    {
        for (int i = 0; i < windowsAndButtons.Count; i++)
        {
            windowsAndButtons.ElementAt(i).Key.WindowFlexibilityComponent.MinimizeWindow();
        }
    }

    public void MaximizeAllWindows()
    {
        for (int i = 0; i < windowsAndButtons.Count; i++)
        {
            windowsAndButtons.ElementAt(i).Key.WindowFlexibilityComponent.MaximizeWindow();
        }
    }

    private GameUI_QuestElementBase GetQuestWindowOfType(TaskBase.TASK_TYPE questType)
    {
        switch (questType)
        {
            case TaskBase.TASK_TYPE.SingleChoice:
                return questSingleChoiceWindowPrefab;

            case TaskBase.TASK_TYPE.TrueFalse:
                return questTrueFalseWindowPrefab;

            case TaskBase.TASK_TYPE.Input:
                return questInputWindowPrefab;

            case TaskBase.TASK_TYPE.Website:
                return questWebsiteWindowPrefab;

            case TaskBase.TASK_TYPE.Action:
                return questActionWindowPrefab;

            case TaskBase.TASK_TYPE.Decision:
                return questDecisionWindowPrefab;

            case TaskBase.TASK_TYPE.Minigame:
                return questMinigameWindowPrefab;

            case TaskBase.TASK_TYPE.Dialogue:
                return questDialogueWindowPrefab;

            default:
                throw new System.NotImplementedException();
        }
    }

    public WindowCommandLine ShowCmdWindow()
    {
        CommandLineWindow.WindowFlexibilityComponent.MaximizeWindow();
        return CommandLineWindow;
    }

    public WindowWebBrowser ShowWebBrowser()
    {
        WebBrowserWindow.WindowFlexibilityComponent.MaximizeWindow();
        return WebBrowserWindow;
    }

    public bool CloseWindow(WindowBase windowToRemove)
    {
        if (windowsAndButtons.ContainsKey(windowToRemove) == false)
            return false;

        windowsAndButtons[windowToRemove].gameObject.Destroy();
        windowsAndButtons.Remove(windowToRemove);

        return true;
    }

    private void Start()
    {
        PrepareSpecialWindows();
    }

    private void PrepareSpecialWindows()
    {
        CommandLineWindow = Instantiate(commandLineWindowPrefab, windowsArea);
        CommandLineWindow.WindowFlexibilityComponent.MinimizeWindow();

        WebBrowserWindow = Instantiate(webBrowserWindowPrefab, windowsArea);
        WebBrowserWindow.WindowFlexibilityComponent.MinimizeWindow();
    }
}
