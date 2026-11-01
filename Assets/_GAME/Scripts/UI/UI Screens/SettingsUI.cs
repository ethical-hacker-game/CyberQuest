using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class SettingsUI : UI_Screen
{
    [Header("Panels")]
    [SerializeField] private Transform generalParent;

    [Header("Element Prefabs")]
    [SerializeField] private TextMeshProUGUI categoryLabelPrefab;
    [SerializeField] private SettingsUI_Element elementSliderPrefab;
    [SerializeField] private SettingsUI_Element elementSwitchPrefab;
    [SerializeField] private SettingsUI_Element elementHorizontalSelectorPrefab;

    [Header("Elements Content")]
    [SerializeField] private SettingsUI_Element.Config rotationSensConfig;
    [SerializeField] private SettingsUI_Element.Config zoomSensConfig;
    [Space]
    [SerializeField] private SettingsUI_Element.Config mapConfig;
    [SerializeField] private SettingsUI_Element.Config postProcessingConfig;
    [Space]
    [SerializeField] private SettingsUI_Element.Config volumeGeneralConfig;
    [SerializeField] private SettingsUI_Element.Config volumeMusicConfig;
    [SerializeField] private SettingsUI_Element.Config volumeEffectsConfig;
    [SerializeField] private SettingsUI_Element.Config volumeUiConfig;


    private SettingsUI_Element rotationSensElement;
    private SettingsUI_Element zoomSensElement;

    private SettingsUI_Element mapElement;
    private SettingsUI_Element postProcessingElement;

    private SettingsUI_Element volumeGeneralElement;
    private SettingsUI_Element volumeMusicElement;
    private SettingsUI_Element volumeEffectsElement;
    private SettingsUI_Element volumeUiElement;

    private SettingsInfo tempSettings;

    private bool elementsSpawned = false;

    public override void ShowScreen()
    {
        base.ShowScreen();
        //TimeController.PauseGame();

        if (elementsSpawned == false)
            SpawnElements();

        tempSettings = SettingsManager.SettingsInfo.Clone();
        LoadSettingsValues();
    }

    public override void HideScreen()
    {
        base.HideScreen();
        //TimeController.UnpauseGame();
    }

    public override void Back()
    {
        base.Back();
    }

    public void ClickCancel()
    {
        Back();
    }

    public void ClickSave()
    {
        PrepareTempSettings();
        SettingsManager.Instance.SaveSettings(tempSettings);
        Back();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.J))
            LoadSettingsValues();
    }

    public void SetQualityPreset(int qualityPresetIndex)
    {
        qualityPresetIndex.ClampInt(0, SettingsManager.QualityPresets.Count - 1);
        SettingsQualityPreset preset = SettingsManager.QualityPresets[qualityPresetIndex];
        tempSettings.qualitySettings = preset.qualitySettings.Clone();
        LoadSettingsValues();
    }

    private void LoadSettingsValues()
    {
        rotationSensElement.SetValue_Slider(tempSettings.rotationSpeed);
        zoomSensElement.SetValue_Slider(tempSettings.zoomSpeed);

        mapElement.SetValue_HorizontalSelector(tempSettings.qualitySettings.mapQuality);
        postProcessingElement.SetValue_Switch(tempSettings.qualitySettings.postProcessing);

        volumeGeneralElement.SetValue_Slider(tempSettings.volumeGeneral);
        volumeMusicElement.SetValue_Slider(tempSettings.volumeMusic);
        volumeEffectsElement.SetValue_Slider(tempSettings.volumeEffects);
        volumeUiElement.SetValue_Slider(tempSettings.volumeUI);
    }

    private void PrepareTempSettings()
    {
        tempSettings.rotationSpeed = rotationSensElement.GetValue_Slider();
        tempSettings.zoomSpeed = zoomSensElement.GetValue_Slider();

        tempSettings.qualitySettings.mapQuality = mapElement.GetValue_HorizontalSelector();
        tempSettings.qualitySettings.postProcessing = postProcessingElement.GetValue_Switch();

        tempSettings.volumeGeneral = volumeGeneralElement.GetValue_Slider();
        tempSettings.volumeMusic = volumeMusicElement.GetValue_Slider();
        tempSettings.volumeEffects = volumeEffectsElement.GetValue_Slider();
        tempSettings.volumeUI = volumeUiElement.GetValue_Slider();
    }


    private void SpawnElements()
    {
        //General
        Instantiate(categoryLabelPrefab, generalParent).SetText("Camera");

        rotationSensElement = Instantiate(elementSliderPrefab, generalParent);
        rotationSensElement.SetupElement_Slider(rotationSensConfig, 0.1f, 2f);

        zoomSensElement = Instantiate(elementSliderPrefab, generalParent);
        zoomSensElement.SetupElement_Slider(zoomSensConfig, 0.1f, 2f);

        //Visuals
        Instantiate(categoryLabelPrefab, generalParent).SetText("Visuals");

        mapElement = Instantiate(elementHorizontalSelectorPrefab, generalParent);
        mapElement.SetupElement_HorizontalSelector(mapConfig, SettingsManager.QualityStringsUltraLow);

        postProcessingElement = Instantiate(elementSwitchPrefab, generalParent);
        postProcessingElement.SetupElement_Switch(postProcessingConfig);

        //Audio
        Instantiate(categoryLabelPrefab, generalParent).SetText("Audio");

        volumeGeneralElement = Instantiate(elementSliderPrefab, generalParent);
        volumeGeneralElement.SetupElement_Slider(volumeGeneralConfig, 0, 1);

        volumeMusicElement = Instantiate(elementSliderPrefab, generalParent);
        volumeMusicElement.SetupElement_Slider(volumeMusicConfig, 0, 1);

        volumeEffectsElement = Instantiate(elementSliderPrefab, generalParent);
        volumeEffectsElement.SetupElement_Slider(volumeEffectsConfig, 0, 1);

        volumeUiElement = Instantiate(elementSliderPrefab, generalParent);
        volumeUiElement.SetupElement_Slider(volumeUiConfig, 0, 1);

        elementsSpawned = true;
    }
}
