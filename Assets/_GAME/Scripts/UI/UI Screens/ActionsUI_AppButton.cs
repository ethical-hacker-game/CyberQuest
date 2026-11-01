using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ActionsUI_AppButton : MonoBehaviour
{
    [SerializeField] private Image iconImage;
    [SerializeField] private TextMeshProUGUI labelTMP;

    private AppCore app;

    private bool iconLoaded = false;

    public void SetupButton(AppCore app)
    {
        this.app = app;
        labelTMP.text = app.header;
        LoadImage();
    }

    public void Click()
    {
        AppsManager.Instance.OpenApp(app.appID);
    }

    private void OnEnable()
    {
        LoadImage();
    }

    private void LoadImage()
    {
        if (iconLoaded)
            return;

        if (app == null)
            return;

        if (app.Icon == null)
            return;

        iconImage.sprite = app.Icon;
        iconLoaded = true;
    }
}
