using Sirenix.OdinInspector;
using UnityEngine;

[CreateAssetMenu(menuName = "Quality Preset")]
public class SettingsQualityPreset : ScriptableObject
{
    //0 best -> example: 10 worst
    public string id => name;
    public string gameName;
    [OnStateUpdate("@$property.State.Expanded = true")]
    public SettingsQualityInfo qualitySettings;
}
