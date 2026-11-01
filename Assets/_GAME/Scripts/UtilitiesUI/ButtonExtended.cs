using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using Sirenix.OdinInspector;

public class ButtonExtended : Button
{
    public TextMeshProUGUI TextTMP
    {
        get => GetComponentInChildren<TextMeshProUGUI>();
        set
        {
            TextMeshProUGUI textTMP = GetComponentInChildren<TextMeshProUGUI>();

            if (textTMP == null)
                return;

            textTMP = value;
        }
    }

    public override void OnPointerClick(PointerEventData eventData)
    {
        base.OnPointerClick(eventData);
        CommonUISolver.SelectInvisibleButton();
        AudioManager.PlayClickSound();
    }

    public void Back()
    {
        CommonUISolver.CurrentUIScreen?.Back();
    }
}
