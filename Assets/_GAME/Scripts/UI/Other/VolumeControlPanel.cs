using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

public class VolumeControlPanel : MonoBehaviour
{
    [SerializeField, Range(float.Epsilon, 2f)] private float chagneTime = 1f;
    [SerializeField] private Slider volumeSlider;
    [SerializeField] private Transform sliderElement;
    [SerializeField] private AudioMixer audioMixer;

    private float targetElementSize = 0;

    public void UpdateVolume()
    {
        float masterVolumeDb = Mathf.Log10(volumeSlider.value) * 20;

        if (volumeSlider.value == 0)
            masterVolumeDb = -80;

        audioMixer.SetFloat("Master", masterVolumeDb);
    }

    public void ToggleSliderElement(float scale)
    {
        targetElementSize = scale;
    }

    private void Start()
    {
        sliderElement.localScale = Vector3.one * targetElementSize;
    }

    private void Update()
    {
        if(targetElementSize != sliderElement.localScale.x)
        {
            float newScale = Mathf.MoveTowards(sliderElement.localScale.x, targetElementSize, Time.deltaTime / chagneTime);
            sliderElement.localScale = new Vector3(newScale, newScale, newScale);
        }
    }
}
