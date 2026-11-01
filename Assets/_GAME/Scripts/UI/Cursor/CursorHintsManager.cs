using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CursorHintsManager : GameSystemComponent
{
    public static CursorHintsManager Instance;

    [SerializeField] private RectTransform hintObject;
    [SerializeField] private UILayoutRebuilder layoutRebuilder;
    [SerializeField] private TextMeshProUGUI hintTMP;
    [SerializeField] private float worldVerticalOffset = -20;

    private HintLocations currentHintLocation = HintLocations.None;
    private Vector3 currentWorldPosition;
    private Vector2 currentScreenPosition;
    private Object currentHintParent;

    private Vector2 CanvasScale { get => CommonUISolver.CanvasScale; }


    public static void ForceClearHint()
    {
        if (Instance == null)
            return;

        Instance.ClearHint(true);
    }

    public void ShowHint(string hintText, Vector3 worldPosition, Object hintParent)
    {
        if (hintText == hintTMP.text && currentWorldPosition == worldPosition && currentHintLocation == HintLocations.WorldPosition)
            return;

        currentHintLocation = HintLocations.WorldPosition;
        currentWorldPosition = worldPosition;
        currentHintParent = hintParent;
        ShowHint(hintText);
    }

    public void ShowHint(string hintText, Vector2 screenPosition, Object hintParent)
    {
        if (hintText == hintTMP.text && currentScreenPosition == screenPosition && currentHintLocation == HintLocations.ScreenPosition)
            return;

        currentHintLocation = HintLocations.ScreenPosition;
        currentScreenPosition = screenPosition;
        currentHintParent = hintParent;
        ShowHint(hintText);
    }

    private void ShowHint(string hintText)
    {
        hintTMP.text = hintText;
        layoutRebuilder.ForceRebuild();

        hintObject.gameObject.SetActiveOptimized(true);
        SetTextLocation();
    }

    public void ClearHint(Object hintParent)
    {
        if (currentHintLocation != HintLocations.ScreenPosition && currentHintLocation != HintLocations.WorldPosition)
            return;

        if (hintParent != currentHintParent)
            return;

        ClearHint();
    }

    private void ClearHint(bool force = false)
    {
        if (currentHintLocation == HintLocations.None && !force)
            return;

        currentHintLocation = HintLocations.None;
        hintTMP.text = string.Empty;
        layoutRebuilder.ForceRebuild();
        currentHintParent = null;
        hintObject.transform.position = new Vector3(-100, -100);
    }

    private void SetTextLocation()
    {
        Vector3 position = Vector3.zero;

        switch (currentHintLocation)
        {
            case HintLocations.WorldPosition:
                hintObject.pivot = new Vector2(0.5f, 0);
                position = currentWorldPosition;
                position.y += worldVerticalOffset;
                position = Camera.main.WorldToScreenPoint(position);
                break;

            case HintLocations.ScreenPosition:
                hintObject.pivot = new Vector2(0, 0);
                position = currentScreenPosition;
                FixPivot_Screen(currentScreenPosition);
                break;
        }

        hintObject.transform.position = position;
    }

    private void FixPivot_Screen(Vector3 currentScreenPosition)
    {
        Vector2 newPivot = Vector2.zero;
        Vector2 corner = (Vector2)currentScreenPosition + hintObject.sizeDelta;

        if (corner.x * CanvasScale.x > Screen.width)
            newPivot.x = 1;

        if(corner.y * CanvasScale.y > Screen.height)
            newPivot.y = 1;

        hintObject.pivot = newPivot;
    }

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        ClearHint(true);
        AttachToEvents();
    }

    private void LateUpdate()
    {
        if (currentHintLocation == HintLocations.None)
            return;

        SetTextLocation();
    }

    private void OnDestroy()
    {
        UnattachFromEvents();
    }

    private void AttachToEvents()
    {
        //TimeController.Instance.OnPaused.AddListener(HandlePause);
        //TimeController.Instance.OnUnpaused.AddListener(HnadleUnpause);
    }

    private void UnattachFromEvents()
    {
        //TimeController.Instance.OnPaused.RemoveListener(HandlePause);
        //TimeController.Instance.OnUnpaused.RemoveListener(HnadleUnpause);
    }

    private void HandlePause()
    {
        hintObject.gameObject.SetActiveOptimized(false);
    }

    private void HnadleUnpause()
    {
        hintObject.gameObject.SetActiveOptimized(true);
    }

    public enum HintLocations
    {
        None,
        WorldPosition,
        ScreenPosition
    }
}
