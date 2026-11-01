using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using WPM;

public class StickerPOI : MonoBehaviour
{
    [Header("Main Settings")]
    [SerializeField] private Image stickeImage;

    [Header("Label Settings")]
    [SerializeField] private float stickerScalingSpeed = 8;
    [SerializeField] private Transform labelHolder;
    [SerializeField] private TextMeshProUGUI label;

    private float targetUIScale = 0;
    private POI_Place poi;

    public void SetupSticker(string tempText)
    {
        label.text = tempText;
    }

    public void SetupSticker(POI_Place _poi)
    {
        poi = _poi;
        stickeImage.sprite = poi.GetIcon();
        label.text = poi.labelText;
    }

    public void ClickSticker()
    {
        POIsManager.Instance.ClickPOI(poi);
    }

    public void SetUIScale(float value)
    {
        targetUIScale = value;
    }

    private void Update()
    {
        float tempScale = Mathf.MoveTowards(labelHolder.localScale.x, targetUIScale, Time.deltaTime * stickerScalingSpeed);
        labelHolder.localScale = new Vector3(tempScale, tempScale, tempScale);
    }
}
