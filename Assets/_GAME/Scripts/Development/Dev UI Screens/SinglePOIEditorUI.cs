using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using RuntimeInspectorNamespace;
using WPM;
using System;

namespace DevTools
{
    public class SinglePOIEditorUI : UI_Screen
    {
        [SerializeField] private RuntimeInspector runtimeInspector;
        [SerializeField] private Image iconImage;
        [SerializeField] private GameObject changeIconButton;
        [SerializeField] private GameObject placeCityButton;
        [SerializeField] private GameObject startCityButton;
        [SerializeField] private GameObject endCityButton;
        [SerializeField] private List<GameObject> uiElemetns = new List<GameObject>();

        private POI ActivePOI => DevToolsManager.ActivePOI;
        private WorldMapGlobe globe;
        private bool uiActive = true;
        private CityClickResult clickResult;

        public override void Back()
        {
            DevEditorUIManager.Instance.POIsEditorUI.SavePOIs();
            DevToolsManager.Instance.SetActviePOI(null);
            DevEditorUIManager.ShowPOIsEditorUI();
            base.Back();
        }

        public override void ShowScreen()
        {
            if(ActivePOI == null)
            {
                Back();
                return;
            }

            runtimeInspector.Inspect(ActivePOI);

            if (ActivePOI.GetType() == typeof(POI_Line))
            {
                iconImage.gameObject.SetActiveOptimized(false);
                changeIconButton.gameObject.SetActiveOptimized(false);
                placeCityButton.gameObject.SetActiveOptimized(false);
                startCityButton.gameObject.SetActiveOptimized(true);
                endCityButton.gameObject.SetActiveOptimized(true);
            }
            else if (ActivePOI.GetType() == typeof(POI_Place))
            {
                iconImage.gameObject.SetActiveOptimized(true);
                iconImage.sprite = ((POI_Place)ActivePOI).GetIcon();
                changeIconButton.gameObject.SetActiveOptimized(true);
                placeCityButton.gameObject.SetActiveOptimized(true);
                startCityButton.gameObject.SetActiveOptimized(false);
                endCityButton.gameObject.SetActiveOptimized(false);
            }
            else
            {
                Debug.LogError("Unknown POI type");
            }

            ToggleUIElements(true);

            base.ShowScreen();

            if (CanAttachActionToGlobe())
                globe.OnCityClick += Globe_OnCityClick;
        }

        public override void HideScreen()
        {
            base.HideScreen();

            if (CanAttachActionToGlobe())
                globe.OnCityClick -= Globe_OnCityClick;
        }

        public void ChangeIcon()
        {
            if (ActivePOI.GetType() == typeof(POI_Place))
            {
                PictureSelectionUI.SetupPictureSelectionUI(PicturesManager.POIsPictures, PicturesManager.DefaultPOIPicture);
                PictureSelectionUI.BackButtonEvent.AddListener(DevEditorUIManager.ShowSinglePOIEditorUI);
                PictureSelectionUI.SelectImageEvent.AddListener((id) =>
                {
                    ((POI_Place)ActivePOI).poiIconID = id;
                });
            }
        }

        public void ChoseCityPlace()
        {
            ChoseCity(CityClickResult.Place);
        }

        public void ChoseCityLineStart()
        {
            ChoseCity(CityClickResult.LineStart);
        }

        public void ChoseCityLineEnd()
        {
            ChoseCity(CityClickResult.LineEnd);
        }

        private void ChoseCity(CityClickResult clickResult)
        {
            this.clickResult = clickResult;
            ToggleUIElements(false);
        }

        private void ToggleUIElements(bool value)
        {
            uiActive = value;

            for (int i = 0; i < uiElemetns.Count; i++)
            {
                uiElemetns[i].SetActiveOptimized(value);
            }
        }

        private bool CanAttachActionToGlobe()
        {
            if (globe == null)
                globe = WorldMapGlobe.instance;

            return globe != null;
        }

        private void Globe_OnCityClick(int cityIndex, int buttonIndex)
        {
            if (buttonIndex != 0)
                return;

            if (uiActive)
                return;

            switch (clickResult)
            {
                case CityClickResult.Place:
                    ((POI_Place)ActivePOI).cityIndex = cityIndex;
                    break;
                case CityClickResult.LineStart:
                    ((POI_Line)ActivePOI).startCityIndex = cityIndex;
                    break;
                case CityClickResult.LineEnd:
                    ((POI_Line)ActivePOI).endCityIndex = cityIndex;
                    break;
                default:
                    break;
            }

            ToggleUIElements(true);

            CursorHintsManager.Instance.ShowHint("", Vector2.zero, this);
            CursorHintsManager.Instance.ClearHint(this);
        }

        [Serializable]
        public enum CityClickResult
        {
            None,
            Place,
            LineStart,
            LineEnd
        }
    }
}
