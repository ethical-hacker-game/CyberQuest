using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class SettingsInfo
{
    public int settingsFileVersion = 0;

    public int language = 0;

    [Range(0.5f, 1.5f)]
    public float rotationSpeed = 1;
    [Range(0.5f, 1.5f)]
    public float zoomSpeed = 1;

    public SettingsQualityInfo qualitySettings;

    public float volumeGeneral = 1;
    public float volumeMusic = 1;
    public float volumeEffects = 1;
    public float volumeUI = 1;

    public void SetupSettingsInfo()
    {
        settingsFileVersion = SettingsManager.SettingsFileVersion;
    }

    public string GetLanguageString()
    {
        //in future it will be taken from language system
        string[] languageStrings = new string[] { "English" };

        if (languageStrings.Length <= language)
            return languageStrings[languageStrings.Length - 1];

        return languageStrings[language];
    }
}