using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WPM;

public class LinesSimulation : GameSystemComponent
{
    [Range(float.Epsilon, 1)]
    [SerializeField] private float newLineDelay = 0.1f;
    [SerializeField] private List<Color> lineColors = new List<Color>();
    [Space]
    [SerializeField, InlineProperty(LabelWidth = 30)] private MinMaxRange arcElevation = new MinMaxRange();
    [SerializeField] private float duration = 2f;
    [SerializeField] private float lineWidth = 0.002f;
    [SerializeField] private float fadeOutAfter = 0.1f;

    private WorldMapGlobe globe;
    private float currentDelay = 0;

    private void Start()
    {
        globe = WorldMapGlobe.instance;
    }

    private void Update()
    {
        currentDelay += Time.deltaTime;

        if(currentDelay >newLineDelay)
        {
            currentDelay = 0;
            SpawnLine();
        }
    }

    private void SpawnLine()
    {
        Vector2 startLatLon = globe.GetCityLatLon(Random.Range(0, globe.cities.Count));
        Vector2 endLatLon = globe.GetCityLatLon(Random.Range(0, globe.cities.Count));
        Color randomColor = lineColors.GetRandom();

        globe.AddLine(startLatLon, endLatLon, randomColor, arcElevation.Random, duration, lineWidth, fadeOutAfter);
    }
}
