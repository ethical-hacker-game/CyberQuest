using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ChangeAlphaOnHover : UIInteractions
{
    [SerializeField] private ObjectType objecType;
    [SerializeField] private float defaultAlpha = 0.001f;
    [SerializeField] private float hoverAlpha = 0.5f;

    private Image image;
    private CanvasGroup canvasGroup;

    private void Awake()
    {
        switch (objecType)
        {
            case ObjectType.Image:
                image = GetComponent<Image>();
                break;

            case ObjectType.CanvasGroup:
                canvasGroup = GetComponent<CanvasGroup>();
                break;

            default:
                Debug.LogError("Unknown object type!");
                GetComponent<ChangeAlphaOnHover>().enabled = false;
                return;
        }

        OnMouseEnter.AddListener(SetHoverAlhpa);
        OnMouseExit.AddListener(SetDefaultAlpha);
    }

    private void SetDefaultAlpha()
    {
        SetAlpha(defaultAlpha);
    }

    private void SetHoverAlhpa()
    {
        SetAlpha(hoverAlpha);
    }

    private void SetAlpha(float alpha)
    {
        switch (objecType)
        {
            case ObjectType.Image:
                SetAlphaImage(alpha);
                break;
            case ObjectType.CanvasGroup:
                SetAlphaCanvasGroup(alpha);
                break;
            default:
                break;
        }
    }

    private void SetAlphaImage(float alpha)
    {
        Color tempColor = image.color;
        tempColor.a = alpha;
        image.color = tempColor;
    }

    private void SetAlphaCanvasGroup(float alpha)
    {
        canvasGroup.alpha = alpha;
    }

    [Serializable]
    public enum ObjectType
    {
        Image,
        CanvasGroup
    }
}
