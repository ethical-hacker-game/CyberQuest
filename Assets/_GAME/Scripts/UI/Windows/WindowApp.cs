using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class WindowApp : WindowBase
{
    [Space]
    [SerializeField] private AppImage appImage;
    [SerializeField] private Image image;
    [SerializeField] private GameObject noScenarioText;
    [SerializeField] private GameObject scenarioFinishedText;
    [SerializeField] private RectTransform contentRect;

    private AppReferences appReferences;
    private AppCore app;
    private AppScenario appScenario;
    private bool scenarioFinished = false;

    public AppCore App => app;


    public void SetupAppWindow(AppReferences appReferences)
    {
        this.appReferences = appReferences;
        app = appReferences.appCore;
        appScenario = appReferences.appScenario;
        scenarioFinished = false;

        SetHeader(app.header);
        SetIcon(app.Icon);
        
        image.sprite = app.DefaultBackground;
        noScenarioText.SetActiveOptimized(appScenario == null);
        scenarioFinishedText.SetActiveOptimized(false);

        Vector4 windowUIMargins = new Vector4(
            -contentRect.offsetMin.x, -contentRect.offsetMin.y, 
            -contentRect.offsetMax.x, -contentRect.offsetMax.y);

        Vector2 extraWindowSize =
            new Vector2(windowUIMargins.x + windowUIMargins.z,
            windowUIMargins.y + windowUIMargins.w);

        if (appScenario != null)
        {
            appImage.SetupAppImage(appReferences);

            Vector4 margin = appScenario.margin;

            image.rectTransform.offsetMin = new Vector2(-margin.x, -margin.y);
            image.rectTransform.offsetMax = new Vector2(margin.z, margin.w);

            extraWindowSize += new Vector2(-margin.x + -margin.z, -margin.y + -margin.w);
            WindowFlexibilityComponent.LockSizeOfWindow(appScenario.size + extraWindowSize);
        }
        else
        {
            WindowFlexibilityComponent.LockSizeOfWindow(app.size + extraWindowSize);
            scenarioFinished = true;
        }
    }

    public override void ClickClose()
    {
        if(appScenario == null || scenarioFinished == true)
        {
            AppsManager.Instance.CloseApp(appReferences);
            base.ClickClose();
            return;
        }

        WindowFlexibilityComponent.MinimizeWindow();
    }

    private void Start()
    {
        appImage.OnStageChanged.AddListener(UpdateWindow);
    }

    private void UpdateWindow(AppScenario.Stage stage)
    {
        if (stage == null)
        {
            scenarioFinished = true;
            scenarioFinishedText.SetActiveOptimized(true);
            SetHeader(app.header);
            SetIcon(app.Icon);
            image.sprite = app.DefaultBackground;
            return;
        }

        if (stage.customHeader.IsNullOrEmpty() == false)
            SetHeader(stage.customHeader);
        else if (appScenario.header.IsNullOrEmpty() == false)
            SetHeader(appScenario.header);
        else
            SetHeader(app.header);
    }
}
