using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using WPM;

public class CountriesColorizer : MonoBehaviour
{
    [SerializeField] private Color color;

    private WorldMapGlobe globe;

    private void Start()
    {
        globe = WorldMapGlobe.instance;
        UpdateColors();
    }

    [Button("Update Colors")]
    private void UpdateColors()
    {
        if (Application.isPlaying == false)
            return;

        Stopwatch sw = Stopwatch.StartNew();

        for (int i = 0; i < globe.countries.Length; i++)
        {
            globe.ToggleCountrySurface(i, true, color);
        }

        sw.Stop();
        UnityEngine.Debug.Log($"Colorization finished in {sw.ElapsedMilliseconds}ms");
    }
}
