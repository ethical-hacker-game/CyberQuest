using Michsky.UI.Beam;
using Sirenix.OdinInspector;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static SettingsUI_Element;

public class SettingsUI_Element : MonoBehaviour
{
    [SerializeField, EnumToggleButtons] private ElementType elementType;
    [SerializeField] private TextMeshProUGUI label;

    [Header("Beam UI")]
    [SerializeField] private SettingsElement settingsElement;
    [SerializeField] private SettingsDescription settingsDescription;

    [ShowIf(nameof(elementType), ElementType.Slider)]
    [SerializeField] private SliderManager sliderManager;
    [ShowIf(nameof(elementType), ElementType.Slider)]
    [SerializeField] private Slider slider;

    [ShowIf(nameof(elementType), ElementType.Switch)]
    [SerializeField] private SwitchManager switchManager;

    [ShowIf(nameof(elementType), ElementType.HorizontalSelector)]
    [SerializeField] private HorizontalSelector horizontalSelector;


    public void SetupElement_Slider(Config config, float min, float max)
    {
        SetupElement(config);

        if (CheckType(ElementType.Slider) == false)
            return;

        slider.minValue = min;
        slider.maxValue = max;
    }

    public void SetupElement_Switch(Config config)
    {
        SetupElement(config);
    }

    public void SetupElement_HorizontalSelector(Config config, List<string> items)
    {
        SetupElement(config);

        if (CheckType(ElementType.HorizontalSelector) == false)
            return;

        horizontalSelector.items.Clear();

        for (int i = 0; i < items.Count; i++)
        {
            horizontalSelector.CreateNewItem(items[i]);
        }

        horizontalSelector.UpdateUI();
    }

    private void SetupElement(Config config)
    {
        label.text = config.title;
        settingsDescription.Title = config.title;
        settingsDescription.Description = config.description;
        settingsDescription.Cover = config.cover;
        settingsDescription.UpdateManager();
    }

    public void SetValue_Slider(float value)
    {
        if (CheckType(ElementType.Slider) == false)
            return;

        slider.SetValueWithoutNotify(value);
    }

    public void SetValue_Switch(bool value)
    {
        if (CheckType(ElementType.Switch) == false)
            return;

        switchManager.isOn = value;
    }

    public void SetValue_HorizontalSelector(int valueIndex)
    {
        if (CheckType(ElementType.HorizontalSelector) == false)
            return;

        horizontalSelector.index = valueIndex;
        horizontalSelector.defaultIndex = valueIndex;
        horizontalSelector.UpdateUI();
    }

    public float GetValue_Slider()
    {
        if (CheckType(ElementType.Slider) == false)
            return 0;

        return slider.value;
    }

    public bool GetValue_Switch()
    {
        if (CheckType(ElementType.Switch) == false)
            return false;

        return switchManager.isOn;
    }

    public int GetValue_HorizontalSelector()
    {
        if (CheckType(ElementType.HorizontalSelector) == false)
            return 0;

        return horizontalSelector.index;
    }

    private bool CheckType(ElementType expectedType)
    {
        if(expectedType != elementType)
        {
            Debug.LogError("Wrong element type!", gameObject);
            return false;
        }

        return true;
    }

    [Serializable]
    public enum ElementType
    {
        Slider,
        Switch,
        HorizontalSelector
    }

    [Serializable]
    public class Config
    {
        public string title;
        public string description;
        public Sprite cover;
    }
}
