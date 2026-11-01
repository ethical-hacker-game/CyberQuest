using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Events;
using Michsky.UI.Beam;

public class InputTextUI : UI_Screen
{
    public static InputTextUI Instance => CommonUISolver.InputTextUI;

    [Header("UI Elements")]
    [SerializeField] private TextMeshProUGUI headerTMP;
    [SerializeField] private TextMeshProUGUI descriptionTMP;
    [SerializeField] private TMP_InputField inputFieldTMP;
    //[SerializeField] private ButtonExtended continueButton;
    //[SerializeField] private ButtonExtended backButton;
    [SerializeField] private ButtonManager continueButton;
    [SerializeField] private ButtonManager backButton;

    [Header("Other")]
    [SerializeField] private Animator animator;

    [Space]
    [SerializeField] private UnityEvent<string> continueButtonEvent;
    [SerializeField] private UnityEvent backButtonEvent;

    public static UnityEvent<string> ContinueButtonEvent { get => Instance.continueButtonEvent; set => Instance.continueButtonEvent = value; }
    public static UnityEvent BackButtonEvent { get => Instance.backButtonEvent; set => Instance.backButtonEvent = value; }

    private int minLength = 0;

    public override void ShowScreen()
    {
        ClearInputTextUI();
        base.ShowScreen();
        //TimeController.PauseGame();
        inputFieldTMP.Select();
        animator.Play("In");
    }

    public override void HideScreen()
    {
        base.HideScreen();
        //TimeController.UnpauseGame();
    }

    public override void Back()
    {
        Instance.backButtonEvent.Invoke();
    }

    public static void SetupInputTextUI(string header, string description, int minLength = 1, int maxLength = 20, string defaultText = "", string continueButtonLabel = "Continue", string backButtonLabel = "Back")
    {
        CommonUISolver.ShowInputTextUI();

        Instance.continueButtonEvent.RemoveAllListeners();
        Instance.backButtonEvent.RemoveAllListeners();

        if (string.IsNullOrEmpty(header) == false)
        {
            Instance.headerTMP.gameObject.SetActiveOptimized(true);
            Instance.headerTMP.text = header;
        }

        if (string.IsNullOrEmpty(description) == false)
        {

            Instance.descriptionTMP.gameObject.SetActiveOptimized(true);
            Instance.descriptionTMP.text = description;
        }

        if (minLength > maxLength)
        {
            int tempLength = minLength;
            minLength = maxLength;
            maxLength = tempLength;
        }

        Instance.inputFieldTMP.text = defaultText;
        Instance.inputFieldTMP.characterLimit = maxLength;
        Instance.minLength = minLength;

        Instance.continueButton.SetText(continueButtonLabel);
        Instance.backButton.SetText(backButtonLabel);
    }

    public void ClickContinueButton()
    {
        if (inputFieldTMP.text.Length < minLength)
            return;

        ContinueButtonEvent.Invoke(inputFieldTMP.text);
    }

    public void ValidateString()
    {
        continueButton.Interactable(inputFieldTMP.text.Length >= minLength);
    }

    private void ClearInputTextUI()
    {
        headerTMP.gameObject.SetActiveOptimized(false);
        descriptionTMP.gameObject.SetActiveOptimized(false);

        Instance.inputFieldTMP.text = string.Empty;

        Instance.continueButton.SetText("V");
        Instance.backButton.SetText("X");

        continueButton.Interactable(false);
    }
}
