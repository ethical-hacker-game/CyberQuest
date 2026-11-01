using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WorldMapStrategyKit;

namespace DevTools
{
    public class Map2D_StylesManager : GameSystemComponent
    {
        [SerializeField] private bool removeAntarctica = true;

        private WMSK map;

        private void Start()
        {
            map = WMSK.instance;

            if (removeAntarctica)
                RemoveAntarctica();
        }

        private void RemoveAntarctica()
        {
            string countryName = "Antarctica";
            int index = map.GetCountryIndex(countryName);

            if (index < 0)
                return;

            map.CountryDelete(index, true, true);
            map.RedrawFrontiers(true);
        }
    }
}
