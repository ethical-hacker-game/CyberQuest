using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class AppImageLink : MonoBehaviour
{
    [SerializeField] private ButtonExtended button;
    [SerializeField] private TMP_InputField inputField;
    [SerializeField] private TextMeshProUGUI backgroundText;

    private RectTransform rectTransform;
    private AppImage appImage;
    private AppScenario.Link link;

    private bool requiresInput = false;

    public void SetupLink(AppImage appImage, AppScenario.Link link)
    {
        rectTransform = GetComponent<RectTransform>();

        this.appImage = appImage;
        this.link = link;

        requiresInput = link.textToInput.IsNullOrEmpty() == false;
        inputField.gameObject.SetActiveOptimized(requiresInput);
        rectTransform.anchoredPosition = link.linkPoistion * new Vector2(1, -1);
        rectTransform.sizeDelta = link.linkSize;

        if (requiresInput)
        {
            inputField.placeholder.GetComponent<TextMeshProUGUI>().text = link.textToInput;
            backgroundText.SetText(link.textToInput);
            inputField.SetTextWithoutNotify("");
        }
    }

    public void DisableInteractivity()
    {
        button.interactable = false;
        inputField.interactable = false;
    }

    public void Click()
    {
        if (requiresInput)
            return;

        appImage.StartStage(link.nextStageID);
    }

    public void InputText(string value)
    {
        if(value != link.textToInput)
            return;

        appImage.StartStage(link.nextStageID);
    }
}
