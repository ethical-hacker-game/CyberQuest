using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UICursorHint : UIInteractions
{
    [SerializeField] private float hintOffset = 40;
    [SerializeField] private bool updatePosition = false;
    [SerializeField, TextArea(2,5)] private string hintText = "!";
    [SerializeField] private RectTransform customRectTransform;

    private RectTransform myRectTransform;
    private bool hideUntilNextMouseEnter = false;

    public bool UpdatePosition { get => updatePosition; set => updatePosition = value; }
    public string HintText { get => hintText; set => hintText = value; }

    protected override void Start()
    {
        OnMouseEnter.AddListener(ShowHint);
        OnMouseExit.AddListener(HideHint);
        OnMouseDown.AddListener(()=>
        {
            HideHint();
            hideUntilNextMouseEnter = true;
        });

        if (updatePosition)
            OnMouseMove.AddListener(() => 
            {
                if (hideUntilNextMouseEnter == false)
                    ShowHint();
            });

        if (customRectTransform != null)
            myRectTransform = customRectTransform;
        else
            myRectTransform = GetComponent<RectTransform>();

        base.Start();
    }

    private void OnDestroy()
    {
        OnMouseEnter.RemoveAllListeners();
        OnMouseExit.RemoveAllListeners();
        OnMouseDown.RemoveAllListeners();

        if (updatePosition)
            OnMouseMove.RemoveAllListeners();
    }

    protected void ShowHint()
    {
        if (myRectTransform == null)
            return;

        Vector2 hintPosition = new Vector2(myRectTransform.position.x - (myRectTransform.sizeDelta.x * CommonUISolver.CanvasScale.x / 2f),
            myRectTransform.position.y - (myRectTransform.sizeDelta.y * CommonUISolver.CanvasScale.y / 2f) + hintOffset);

        CursorHintsManager.Instance.ShowHint(hintText, hintPosition, this);
    }

    protected void HideHint()
    {
        CursorHintsManager.Instance.ClearHint(this);
    }
}
