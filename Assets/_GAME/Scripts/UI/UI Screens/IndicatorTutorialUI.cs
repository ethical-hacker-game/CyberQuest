using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class IndicatorTutorialUI : UI_Screen
{
    public static IndicatorTutorialUI Instance => CommonUISolver.IndicatorTutorialUI;

    [Header("UI Elements")]
    [SerializeField] private RectTransform indicatorArea;
    [SerializeField] private TextMeshProUGUI textTMP;
    [SerializeField] private HorizontalLayoutGroup horizontalLayoutGroup;

    [Header("UI Arrows")]
    [SerializeField] private GameObject arrowUp;
    [SerializeField] private GameObject arrowDown;

    public override void Back()
    {
        HideScreen();
        IndicatorManager.Instance.TutorialClosed();
    }

    public override void ShowScreen()
    {
        base.ShowScreen();
    }

    public override void HideScreen()
    {
        base.HideScreen();
    }

    public static void SetupIndicatorTutorialUI(IndicatorTag indicator)
    {
        CommonUISolver.ShowIndicatorTutorialUI();
        Instance.SetAreaPositionAndSize(indicator);
        Instance.textTMP.text = indicator.IndicatorText;
    }

    private void SetAreaPositionAndSize(IndicatorTag indicator)
    {
        Vector3 indicatorPosition = indicator.RectTransform.position;

        float screenMiddleX = Screen.width / 2f;

        switch (indicator.IndicatorPosition)
        {
            case IndicatorTag.IndicatorPositions.Left:
                horizontalLayoutGroup.childAlignment = TextAnchor.MiddleLeft;
                break;
            case IndicatorTag.IndicatorPositions.Middle:
                horizontalLayoutGroup.childAlignment = TextAnchor.MiddleCenter;
                break;
            case IndicatorTag.IndicatorPositions.Right:
                horizontalLayoutGroup.childAlignment = TextAnchor.MiddleRight;
                break;
            default:
                if (indicatorPosition.x < screenMiddleX)
                    horizontalLayoutGroup.childAlignment = TextAnchor.MiddleRight;
                else
                    horizontalLayoutGroup.childAlignment = TextAnchor.MiddleLeft;
                break;
        }



        float screenMiddleY = Screen.height / 2f;
        bool indicatorOnTop = indicatorPosition.y > screenMiddleY;

        arrowUp.SetActiveOptimized(!indicatorOnTop);
        arrowDown.SetActiveOptimized(indicatorOnTop);

        Vector2 pivot = indicator.RectTransform.pivot;
        pivot.x = Mathf.Clamp01(pivot.x);
        pivot.y = Mathf.Clamp01(pivot.y);

        float horizontalOffset = (pivot.x - 0.5f) * indicator.RectTransform.sizeDelta.x * CommonUISolver.CanvasScale.x;
        float verticalOffset = (pivot.y - 0.5f) * indicator.RectTransform.sizeDelta.y * CommonUISolver.CanvasScale.y;

        Debug.Log($"horizontalOffset {horizontalOffset}, verticalOffset {verticalOffset}", indicator.gameObject);

        indicatorPosition.x -= horizontalOffset;
        indicatorPosition.y -= verticalOffset;



        indicatorArea.position = indicatorPosition;
        indicatorArea.GetComponent<RectTransform>().sizeDelta = indicator.IndicatorArea;
    }
}
