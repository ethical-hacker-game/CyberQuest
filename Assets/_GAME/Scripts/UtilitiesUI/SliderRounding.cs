using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Slider))]
public class SliderRounding : MonoBehaviour
{
    [SerializeField] private float step = 0.25f;
    private Slider slider;

    public void UpdateValue()
    {
        //float factor = Mathf.Pow(10, decimalPlaces);
        //slider.value = Mathf.Round(slider.value * factor) / factor;
        slider.value = Mathf.Round(slider.value / step) * step;
    }

    private void Awake()
    {
        slider = GetComponent<Slider>();
    }
}
