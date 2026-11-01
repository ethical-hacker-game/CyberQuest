using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

public class WindowFlexibility : MonoBehaviour
{
    [SerializeField] private Vector2 minimalWindowSize = new Vector2(250, 250);
    [SerializeField] private Vector2 maximalWindowSize = new Vector2(1000, 1000);
    [SerializeField] private GameObject draggableArea;
    [SerializeField] private GameObject resizableArea;
    [SerializeField] private SpawnPosition spawnPosition = SpawnPosition.ScreenCenter;

    [FoldoutGroup("Events")] public UnityEvent OnDraggingStartEvent;
    [FoldoutGroup("Events")] public UnityEvent OnDraggingEvent;
    [FoldoutGroup("Events")] public UnityEvent OnDraggingEndEvent;

    [Space]
    [FoldoutGroup("Events")] public UnityEvent OnResizeingStartEvent;
    [FoldoutGroup("Events")] public UnityEvent OnResizeingEvent;
    [FoldoutGroup("Events")] public UnityEvent OnResizeingEndEvent;

    [Space]
    [FoldoutGroup("Events")] public UnityEvent OnActiveEvent;

    private RectTransform rectTransform;
    private bool dragging = false;
    private bool resizeing = false;
    private Vector2 mouseOffset;
    private Vector2 windowSize;

    private UIInteractions mainInteractionsArea;
    private UIInteractions dragInteractionArea;
    private UIInteractions resizeInteractionArea;
    private bool eventsAttached = false;

    private Vector2 CanvasScale { get => GameplayUIManager.Instance.CanvasScale; }
    private Vector2 CanvasSize { get => GameplayUIManager.Instance.CanvasSize; }

    private RectTransform availableArea;

    private bool clone = false;
    private Vector3 positionClone;
    private Vector2 sizeDeltaClone;

    private float MarginLeft => GameUI.MarginLeft;
    private float MarginRight => GameUI.MarginRight;
    private float MarginTop => GameUI.MarginTop;
    private float MarginBottom => GameUI.MarginBottom;

    public bool IsMaximized { get => gameObject.activeSelf; }

    public void MaximizeWindow()
    {
        gameObject.SetActiveOptimized(true);
        SetHigherSortOrder();
    }

    public void MinimizeWindow()
    {
        gameObject.SetActiveOptimized(false);
    }

    public void FullyExpandWindow()
    {
        Drag(new Vector2(-10000, 10000));
        Resize(new Vector2(10000, -10000), true);

        StopDragging();
        StopResizeing();
    }

    public void SetHigherSortOrder()
    {
        transform.SetAsLastSibling();
        OnActiveEvent.Invoke();
    }

    public void MoveWindowToCenter()
    {
        Vector2 newPosition;
        newPosition = new Vector2((CanvasSize.x - windowSize.x) * 0.5f * CanvasScale.x,
                    (CanvasSize.y + windowSize.y) * 0.5f * CanvasScale.y);

        newPosition.x = GetFixedPositionX(newPosition.x);
        newPosition.y = GetFixedPositionY(newPosition.y);

        rectTransform.position = newPosition;
    }

    public void LockSizeOfWindow(Vector2 size)
    {
        minimalWindowSize = size;
        maximalWindowSize = size;
        SetSizeOfWindow(size);
    }

    public void SetSizeOfWindow(Vector2 size)
    {
        rectTransform.sizeDelta = size;
        windowSize = size;

        Vector2 newPosition;
        newPosition.x = GetFixedPositionX(rectTransform.position.x);
        newPosition.y = GetFixedPositionY(rectTransform.position.y);

        rectTransform.position = newPosition;
    }

    public void SetMinimalSizeOfWindow()
    {
        SetSizeOfWindow(minimalWindowSize);
    }

    public void CloneFlexibilitySettings(WindowFlexibility flexibilityToCopy)
    {
        clone = true;
        positionClone = flexibilityToCopy.rectTransform.position;
        sizeDeltaClone = flexibilityToCopy.rectTransform.sizeDelta;
    }

    private void Awake()
    {
        InitializeInteractionsAreas();
        IntializeWindow();

        availableArea = rectTransform.parent.GetComponent<RectTransform>();
    }

    private void Start()
    {
        AttachEvents();
        SetupWindowPosition();
        SetHigherSortOrder();
        ApplyCloneSettings();
    }

    private void OnEnable()
    {
        if (rectTransform != null)
        {
            rectTransform.pivot = Vector2.up;
        }
        AttachEvents();
    }

    private void OnDisable()
    {
        DetachEvents();
    }

    private void LateUpdate()
    {
        if (rectTransform == null)
            return;

        if (dragging)
        {
            Drag(Input.mousePosition);
        }
        else if (resizeing)
        {
            Resize(Input.mousePosition);
        }
    }

    private void InitializeInteractionsAreas()
    {
        mainInteractionsArea = gameObject.AddComponent<UIInteractions>();

        dragInteractionArea = draggableArea.GetComponent<UIInteractions>();

        if (dragInteractionArea == null)
            dragInteractionArea = draggableArea.AddComponent<UIInteractions>();

        if (resizableArea == null)
            return;

        resizeInteractionArea = resizableArea.GetComponent<UIInteractions>();

        if (resizeInteractionArea == null)
            resizeInteractionArea = resizableArea.AddComponent<UIInteractions>();
    }

    private void IntializeWindow()
    {
        rectTransform = GetComponent<RectTransform>();
        windowSize = rectTransform.sizeDelta;

        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.zero;
        rectTransform.pivot = Vector2.up;
    }

    private void SetupWindowPosition()
    {
        Vector2 newPosition;
        switch (spawnPosition)
        {
            case SpawnPosition.CursorPosition:
                newPosition = new Vector2(Input.mousePosition.x - (windowSize.x * 0.5f * CanvasScale.x),
                    Input.mousePosition.y + (45 * CanvasScale.y));

                newPosition.x = GetFixedPositionX(newPosition.x);
                newPosition.y = GetFixedPositionY(newPosition.y);

                rectTransform.position = newPosition;
                break;

            case SpawnPosition.ScreenCenter:
                MoveWindowToCenter();
                break;
            case SpawnPosition.RandomPosition:
                MoveWindowToCenter();

                newPosition.x = rectTransform.position.x + Random.Range((CanvasSize.x - windowSize.x) * -0.25f * CanvasScale.x, (CanvasSize.x - windowSize.x) * 0.25f * CanvasScale.x);
                newPosition.y = rectTransform.position.y + Random.Range((CanvasSize.y - windowSize.y) * -0.25f * CanvasScale.y, (CanvasSize.y - windowSize.y) * 0.25f * CanvasScale.y);

                newPosition.x = GetFixedPositionX(newPosition.x);
                newPosition.y = GetFixedPositionY(newPosition.y);

                rectTransform.position = newPosition;
                break;
        }
    }

    private void Drag(Vector2 mousePosition)
    {
        Vector2 positionDelta = new Vector2(mousePosition.x - mouseOffset.x,
            mousePosition.y - mouseOffset.y);

        positionDelta.x = GetFixedPositionX(positionDelta.x);
        positionDelta.y = GetFixedPositionY(positionDelta.y);

        rectTransform.position = positionDelta;

        OnDraggingEvent.Invoke();
    }

    private void Resize(Vector2 mousePositionDelta, bool ignoreMaximalWindowSize = false)
    {
        Vector2 newSize = new Vector2((rectTransform.position.x - mousePositionDelta.x) * -1f,
            rectTransform.position.y - mousePositionDelta.y);

        if (mousePositionDelta.x / CanvasScale.x > CanvasSize.x)
        {
            newSize.x = CanvasSize.x * CanvasScale.x - rectTransform.position.x;
        }
        if (mousePositionDelta.y / CanvasScale.y < 0)
        {
            newSize.y = rectTransform.position.y;
        }

        Vector2 maxWindowSize = ignoreMaximalWindowSize? new Vector2(10000, 10000) : maximalWindowSize;

        newSize.x = Mathf.Clamp(newSize.x, minimalWindowSize.x * CanvasScale.x, maxWindowSize.x * CanvasScale.x) / CanvasScale.x;
        newSize.y = Mathf.Clamp(newSize.y, minimalWindowSize.y * CanvasScale.y, maxWindowSize.y * CanvasScale.y) / CanvasScale.y;

        newSize.x = GetFixedWidth(newSize.x);
        newSize.y = GetFixedHeight(newSize.y);

        rectTransform.sizeDelta = newSize;

        windowSize = rectTransform.GetComponent<RectTransform>().sizeDelta;

        OnResizeingEvent.Invoke();
    }

    private void StartDragging()
    {
        SetHigherSortOrder();
        dragging = true;
        mouseOffset = new Vector2(Input.mousePosition.x - rectTransform.position.x,
            Input.mousePosition.y - rectTransform.position.y);
        OnDraggingStartEvent.Invoke();
    }

    private void StopDragging()
    {
        dragging = false;
        OnDraggingEndEvent.Invoke();
    }

    private void StartResizeing()
    {
        SetHigherSortOrder();
        resizeing = true;
        OnResizeingStartEvent.Invoke();
    }

    private void StopResizeing()
    {
        resizeing = false;
        OnResizeingEndEvent.Invoke();
    }

    private void AttachEvents()
    {
        if (eventsAttached)
            return;

        mainInteractionsArea.OnMouseDown.AddListener(SetHigherSortOrder);

        if (dragInteractionArea != null)
        {
            dragInteractionArea.OnMouseDown.AddListener(StartDragging);
            dragInteractionArea.OnMouseUp.AddListener(StopDragging);
        }

        if (resizeInteractionArea != null)
        {
            resizeInteractionArea.OnMouseDown.AddListener(StartResizeing);
            resizeInteractionArea.OnMouseUp.AddListener(StopResizeing);
        }

        eventsAttached = true;
    }

    private void DetachEvents()
    {
        if (eventsAttached == false)
            return;

        mainInteractionsArea.OnMouseDown.RemoveListener(SetHigherSortOrder);

        if (dragInteractionArea != null)
        {
            dragInteractionArea.OnMouseDown.RemoveListener(StartDragging);
            dragInteractionArea.OnMouseUp.RemoveListener(StopDragging);
        }


        if (resizeInteractionArea != null)
        {
            resizeInteractionArea.OnMouseDown.RemoveListener(StartResizeing);
            resizeInteractionArea.OnMouseUp.RemoveListener(StopResizeing);
        }

        eventsAttached = false;
    }

    private float GetFixedWidth(float value)
    {
        if (rectTransform.position.x / CanvasScale.x + value > Screen.width / CanvasScale.x + MarginRight)
        {
            value = (Screen.width - rectTransform.position.x) / CanvasScale.x + MarginRight;
        }

        return value;
    }

    private float GetFixedHeight(float value)
    {
        if (rectTransform.position.y / CanvasScale.y - value < MarginBottom)
        {
            value = rectTransform.position.y / CanvasScale.y - MarginBottom;
        }

        return value;
    }

    private float GetFixedPositionX(float value)
    {
        if (value < MarginLeft * CanvasScale.x)
        {
            value = MarginLeft * CanvasScale.x;
        }
        else if (value > Screen.width - (windowSize.x - MarginRight) * CanvasScale.x)
        {
            value = Screen.width - (windowSize.x - MarginRight) * CanvasScale.x;
        }

        return value;
    }

    private float GetFixedPositionY(float value)
    {
        if (value < (windowSize.y + MarginBottom) * CanvasScale.y)
        {
            value = (windowSize.y + MarginBottom) * CanvasScale.y;
        }
        else if (value > Screen.height + MarginTop * CanvasScale.y)
        {
            value = Screen.height + MarginTop * CanvasScale.y;
        }

        return value;
    }

    private void ApplyCloneSettings()
    {
        if (clone == false)
            return;

        rectTransform.position = positionClone;
        rectTransform.sizeDelta = sizeDeltaClone;
        windowSize = rectTransform.sizeDelta;
    }

    [Button(DirtyOnClick = true, Name = "Use current size as minimal"), PropertyOrder(-1)]
    private void UseCurrentSizeAsMinimal()
    {
        minimalWindowSize = transform.GetComponent<RectTransform>().sizeDelta;
    }

    public enum SpawnPosition
    {
        Prefab,
        CursorPosition,
        ScreenCenter,
        RandomPosition,
    }
}
