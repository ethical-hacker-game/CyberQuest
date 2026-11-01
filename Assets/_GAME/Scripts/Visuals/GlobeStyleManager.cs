using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WPM;

public class GlobeStyleManager : GameSystemComponent
{
    public static GlobeStyleManager Instance { get; private set; }

    [SerializeField, ColorUsage(true, true)] private Color blueCoutryFrontiers;
    [SerializeField, ColorUsage(true, true)] private Color blueCoutryNames;
    [SerializeField, ColorUsage(true, true)] private Color blueProvinces;

    [Space]
    [SerializeField, ColorUsage(true, true)] private Color redCoutryFrontiers;
    [SerializeField, ColorUsage(true, true)] private Color redCoutryNames;
    [SerializeField, ColorUsage(true, true)] private Color redProvinces;

    private WorldMapGlobe globe;

    public void GlobeBlue()
    {
        globe.frontiersColor = blueCoutryFrontiers;
        globe.inlandFrontiersColor = blueCoutryFrontiers;
        globe.countryLabelsColor = blueCoutryNames;
        globe.provincesColor = blueProvinces;
    }

    public void GlobeRed()
    {
        globe.frontiersColor = redCoutryFrontiers;
        globe.inlandFrontiersColor = redCoutryFrontiers;
        globe.countryLabelsColor = redCoutryNames;
        globe.provincesColor = redProvinces;
    }

    public void GlobeRealistic()
    {
        globe.showCities = true;
        globe.earthStyle = EARTH_STYLE.NaturalHighRes16KScenic;
        globe.showFrontiers = true;
        globe.showInlandFrontiers = true;
        globe.showCountryNames = true;
    }

    public void GlobeNight()
    {
        globe.showCities = false;
        globe.earthStyle = EARTH_STYLE.NaturalHighRes16KScenicScatter;
        globe.showFrontiers = false;
        globe.showInlandFrontiers = false;
        globe.showCountryNames = false;
    }

    public void GlobeHacker()
    {
        globe.showCities = false;
        globe.earthStyle = EARTH_STYLE.SolidColor;
        globe.showFrontiers = true;
        globe.showInlandFrontiers = true;
        globe.showCountryNames = true;
    }

    public void CloudsOn()
    {
        globe.cloudsAlpha = 1.0f;
        globe.cloudsSpeed = -0.1f;
    }

    public void CloudsOff()
    {
        globe.cloudsAlpha = 0f;
    }

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        globe = WorldMapGlobe.instance;
        //GlobeHacker();
    }
}
