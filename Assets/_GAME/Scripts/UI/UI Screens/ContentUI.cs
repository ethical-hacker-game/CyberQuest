using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Events;
using Michsky.UI.Beam;
using Sirenix.OdinInspector;

public class ContentUI : UI_Screen
{
    public static ContentUI Instance => CommonUISolver.ContentUI;

    [Header("UI Elements")]
    [SerializeField] private TextMeshProUGUI headerTMP;
    [SerializeField] private Image contentImage;
    [SerializeField] private TextMeshProUGUI contentTMP;

    [Header("Other")]
    [SerializeField, OnValueChanged("SetImageHeightDefault")] private float contentImageHeight = 300;
    [SerializeField] private Animator animator;
    [SerializeField] private Image gradientBackground;
    [SerializeField] private Color32 defaultGradientBackgroundColor;

    [Header("Buttons")]
    //[SerializeField] private ButtonExtended[] buttons;
    [SerializeField] private ButtonManager[] buttons;

    [Space]
    [SerializeField] private UnityEvent[] buttonEvents = new UnityEvent[4];

    public static UnityEvent[] ButtonEvents { get => Instance.buttonEvents; set => Instance.buttonEvents = value; }
    public static int DefaultButtonIndex { get; set; }

    public override void ShowScreen()
    {
        ClearContentUI();
        base.ShowScreen();
        animator.Play("In");
        //TimeController.PauseGame();
    }

    public override void HideScreen()
    {
        base.HideScreen();
        //TimeController.UnpauseGame();
    }

    public override void Back()
    {
        if (DefaultButtonIndex < 0)
            if (Instance.buttons[1].gameObject.activeSelf == false)
                Instance.buttons[0].onClick.Invoke();
            else
                return;

        if (Instance.buttons[DefaultButtonIndex].gameObject.activeSelf)
            Instance.buttons[DefaultButtonIndex].onClick.Invoke();
    }

    public static void SetupContentUI(string header, Sprite sprite, string content, params string[] buttonsTexts)
    {
        CommonUISolver.ShowContentUI();
        Instance.SetImageHeightDefault();
        DefaultButtonIndex = -1;

        if (string.IsNullOrEmpty(header) == false)
        {
            Instance.headerTMP.gameObject.SetActiveOptimized(true);
            Instance.headerTMP.text = header;
        }

        if (sprite != null)
        {

            Instance.contentImage.gameObject.SetActiveOptimized(true);
            Instance.contentImage.sprite = sprite;
        }

        if (string.IsNullOrEmpty(content) == false)
        {

            Instance.contentTMP.gameObject.SetActiveOptimized(true);
            Instance.contentTMP.text = content;
        }

        int buttonsLength = buttonsTexts.Length;

        if (buttonsLength > Instance.buttons.Length)
            buttonsLength = Instance.buttons.Length;


        for (int i = 0; i < buttonsLength; i++)
        {
            Instance.buttons[i].gameObject.SetActiveOptimized(true);
            Instance.buttons[i].SetText(buttonsTexts[i]);
        }
    }

    public static void ShowGradientBackground(Color32? backgroundColor = null)
    {
        Instance.gradientBackground.gameObject.SetActiveOptimized(true);

        if (backgroundColor == null)
            backgroundColor = Instance.defaultGradientBackgroundColor;

        Instance.gradientBackground.color = backgroundColor.Value;
    }

    public static void SetContentImageHeight(float newHeight) 
    {
        Instance.SetImageHeight(newHeight);
    }

    public void ClickButton(int buttonIndex)
    {
        ButtonEvents[buttonIndex].Invoke();
    }

    private void SetImageHeightDefault()
    {
        SetImageHeight(contentImageHeight);
    }

    private void SetImageHeight(float newHeight)
    {
        if (contentImage == null)
            return;

        Vector2 size = contentImage.rectTransform.sizeDelta;
        size.y = newHeight;
        contentImage.rectTransform.sizeDelta = size;
    }

    private void ClearContentUI()
    {
        Instance.gradientBackground.gameObject.SetActiveOptimized(false);

        headerTMP.gameObject.SetActiveOptimized(false);
        contentImage.gameObject.SetActiveOptimized(false);
        contentTMP.gameObject.SetActiveOptimized(false);

        for (int i = 0; i < buttons.Length; i++)
        {
            buttons[i].gameObject.SetActiveOptimized(false);
        }

        buttonEvents = new UnityEvent[buttons.Length];

        for (int i = 0; i < buttonEvents.Length; i++)
        {
            if (buttonEvents[i] == null)
                buttonEvents[i] = new UnityEvent();
            else
                buttonEvents[i].RemoveAllListeners();
        }
    }
}
