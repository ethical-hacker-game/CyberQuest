using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class SettingsQualityInfo
{
    [Range(0, 3), LabelText("@\"Textures \"+this.GetTexturesQualityString().ToUpper()")]
    public int texturesQuality = 0;
    [Range(0, 1), LabelText("@\"Textures \"+this.GetTexturesQualityString().ToUpper()")]
    public int mapQuality = 0;
    //[Range(0, 1), LabelText("@\"Models \"+this.GetModelsQualityString().ToUpper()")]
    //public int modelsQuality = 0;
    //[Range(0, 3), LabelText("@\"Shadows \"+this.GetShadowsQualityString().ToUpper()")]
    //public int shadowsQuality = 0;
    //[Range(0, 3), LabelText("@\"Vegetation \"+this.GetVegetationQualityString().ToUpper()")]
    //public int vegetationQuality = 0;
    public bool postProcessing = true;

    public string GetTexturesQualityString()
    {
        string[] qualityStrings = SettingsManager.QualityStringsAll.ToArray();

        if (qualityStrings.Length <= texturesQuality)
            return qualityStrings[qualityStrings.Length - 1];

        return qualityStrings[texturesQuality];
    }

    public string GetMapQualityString()
    {
        string[] qualityStrings = SettingsManager.QualityStringsUltraLow.ToArray();

        if (qualityStrings.Length <= mapQuality)
            return qualityStrings[qualityStrings.Length - 1];

        return qualityStrings[texturesQuality];
    }

    //public string GetModelsQualityString()
    //{
    //    string[] qualityStrings = new string[] { "ultra", "medium", "low" };

    //    if (qualityStrings.Length <= modelsQuality)
    //        return qualityStrings[qualityStrings.Length - 1];

    //    return qualityStrings[modelsQuality];
    //}

    //public string GetShadowsQualityString()
    //{
    //    string[] qualityStrings = new string[] { "ultra", "medium", "low", "off" };

    //    if (qualityStrings.Length <= shadowsQuality)
    //        return qualityStrings[qualityStrings.Length - 1];

    //    return qualityStrings[shadowsQuality];
    //}

    //public string GetVegetationQualityString()
    //{
    //    string[] qualityStrings = new string[] { "ultra", "high", "medium", "low" };

    //    if (qualityStrings.Length <= vegetationQuality)
    //        return qualityStrings[qualityStrings.Length - 1];

    //    return qualityStrings[vegetationQuality];
    //}

    //public float GetVegetationDistance()
    //{
    //    float highDistance = 100;
    //    float lowDistance = 40;
    //    float currentValue = Mathf.Clamp01(vegetationQuality / 3f);
    //    return Mathf.Lerp(highDistance, lowDistance, currentValue);
    //}

    //public float GetVegetationDensity()
    //{
    //    float highDensity = 1;
    //    float lowDensity = 0.2f;
    //    float currentValue = Mathf.Clamp01(vegetationQuality / 3f);
    //    return Mathf.Lerp(highDensity, lowDensity, currentValue);
    //}
}