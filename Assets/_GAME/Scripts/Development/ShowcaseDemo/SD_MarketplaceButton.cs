using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using Sirenix.OdinInspector;

public class SD_MarketplaceButton : MonoBehaviour
{
    [SerializeField] private Sprite sprite;
    [SerializeField] private string labelText;
    [SerializeField, TextArea(3,10)] private string hintText;

    [Space]
    [SerializeField] private Action action;
    [SerializeField] private int price = 90000;

    [Space]
    [SerializeField] private TextMeshProUGUI label;
    [SerializeField] private Image image;
    [SerializeField] private GameObject unlockedBG;
    [SerializeField] private UICursorHint hintObj;

    private bool clicked = false;

    public void Click()
    {
        if (clicked)
            return;

        if (action == Action.None)
            return;

        clicked = true;
        unlockedBG.SetActive(true);
        SD_MoenyLabel.ChangeMoney(-price);

        switch (action)
        {
            case Action.UnlockAION:
                SD_ObjectScaler.ChangeScale("aion", 1);
                break;
            case Action.ChangeGlobe:
                GlobeStyleManager.Instance.GlobeRealistic();
                break;
            default:
                break;
        }
    }

    [Button("Update elements")]
    private void SetElements()
    {
        label.text = labelText;
        image.sprite = sprite;
        hintObj.HintText = hintText;
    }

    private void Start()
    {
        unlockedBG.SetActive(false);
    }

    [Serializable]
    public enum Action
    {
        None,
        UnlockAION,
        ChangeGlobe
    }
}
