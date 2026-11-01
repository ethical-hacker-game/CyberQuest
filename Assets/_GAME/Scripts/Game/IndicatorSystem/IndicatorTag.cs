using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IndicatorTag : MonoBehaviour
{
    [SerializeField] private string indicatorID;
    [SerializeField] private IndicatorPositions indicatorPosition = IndicatorPositions.Auto;
    [SerializeField, TextArea(5,20)] private string indicatorText;
    [SerializeField] private Vector2 indicatorArea = new Vector2(100, 100);

    public string IndicatorID { get => indicatorID; }
    public IndicatorPositions IndicatorPosition { get => indicatorPosition; }
    public string IndicatorText { get => indicatorText; }
    public Vector2 IndicatorArea { get => indicatorArea; }

    public RectTransform RectTransform => GetComponent<RectTransform>();

    private void Start()
    {
        IndicatorManager.AddIndicator(this);
    }

    public enum IndicatorPositions
    {
        Auto,
        Left,
        Middle,
        Right
    }
}
