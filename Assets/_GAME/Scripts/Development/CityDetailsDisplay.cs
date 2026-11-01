using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WPM;

public class CityDetailsDisplay : GameSystemComponent
{
    private WorldMapGlobe globe;

    private void Start()
    {
        globe = WorldMapGlobe.instance;
        globe.OnCityEnter += ShowCityInfo;
        globe.OnCityExit += HideCityInfo;
    }

    private void ShowCityInfo(int cityIndex)
    {
        string cityInfo = $"index: {cityIndex}\n" +
            $"<b>{globe.cities[cityIndex].name}</b>\n" +
            $"{globe.cities[cityIndex].province}, {globe.countries[globe.cities[cityIndex].countryIndex].name}\n" +
            $"population: {globe.cities[cityIndex].population}\n";

        CursorHintsManager.Instance.ShowHint(cityInfo, new Vector2(InputManager.mousePosition.x, InputManager.mousePosition.y), this);
    }

    private void HideCityInfo(int cityIndex)
    {
        CursorHintsManager.Instance.ClearHint(this);
    }
}
