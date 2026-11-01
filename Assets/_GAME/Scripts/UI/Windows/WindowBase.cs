using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

[RequireComponent(typeof(WindowFlexibility))]
public class WindowBase : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TextMeshProUGUI headerTMP;
    [SerializeField] private Image iconImage;

    private WindowFlexibility windowFlexibilityComponent;

    public WindowFlexibility WindowFlexibilityComponent => windowFlexibilityComponent;

    public Sprite Icon => iconImage.sprite;

    public virtual void ClickClose()
    {
        bool removedWindow = GameUI.Instance.CloseWindow(this);

        if (removedWindow == false)
        {
            windowFlexibilityComponent.MinimizeWindow();
            return;
        }

        DestroyWindow();
    }

    public void SetHeader(string header)
    {
        headerTMP.text = header;
    }

    public void SetIcon(Sprite icon)
    {
        iconImage.sprite = icon;
    }

    public void DestroyWindow()
    {
        Destroy(gameObject);
    }

    protected virtual void Awake()
    {
        windowFlexibilityComponent = GetComponent<WindowFlexibility>();
    }
}
