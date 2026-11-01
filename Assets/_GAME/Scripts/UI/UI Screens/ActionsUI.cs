using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ActionsUI : UI_Screen
{
    public static ActionsUI Instance => GameplayUIManager.Instance.ActionsUI;

    [SerializeField] private RectTransform buttonsParent;
    [SerializeField] private GameObject windowButtonPrefab;

    [Header("App Buttons UI")]
    [SerializeField] private GameObject appButtonPrefab;
    [SerializeField] private Transform appButtonsParent;

    public override void Back()
    {
        GameplayUIManager.ShowPauseUI();
        base.Back();
    }

    public GameObject AddWindowButton(string title, Sprite icon)
    {
        GameObject newButton = Instantiate(windowButtonPrefab, buttonsParent);
        newButton.GetComponent<UICursorHint>().UpdatePosition = true;
        newButton.GetComponent<UICursorHint>().HintText = title;
        newButton.transform.GetChild(1).GetComponent<Image>().sprite = icon;
        
        return newButton;
    }

    public void ShowMenu()
    {
        Back();
    }

    public void ShowCyberpediaUI()
    {
        CyberpediaManager.Instance.OpenLibrary();
    }

    public void ShowWebBrowser()
    {
        GameUI.Instance.ShowWebBrowser();
    }

    public void ShowMails()
    {
        GameplayUIManager.ShowMailsUI();
    }

    public void ShowCommandLine()
    {
        GameUI.Instance.ShowCmdWindow();
    }

    private void Start()
    {
        PrepareAppButtons();
    }

    private void PrepareAppButtons()
    {
        for (int i = appButtonsParent.childCount - 1; i >= 0; i--)
        {
            Destroy(appButtonsParent.GetChild(i).gameObject);
        }

        appButtonsParent.DetachChildren();

        for (int i = 0; i < AppsManager.Instance.Apps.Count; i++)
        {
            AppCore app = AppsManager.Instance.Apps[i];
            ActionsUI_AppButton button = Instantiate(appButtonPrefab, appButtonsParent).GetComponent<ActionsUI_AppButton>();
            button.SetupButton(app);
        }
    }
}
