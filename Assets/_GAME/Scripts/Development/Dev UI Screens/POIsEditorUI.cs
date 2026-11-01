using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using RuntimeInspectorNamespace;
using WPM;
using TMPro;

namespace DevTools
{
    public class POIsEditorUI : UI_Screen
    {
        [SerializeField] private RectTransform placesParent;
        [SerializeField] private RectTransform linesParent;
        [SerializeField] private GameObject poiButton;

        [Space]
        [SerializeField] private TextMeshProUGUI issuesTMP;

        private POIs poisObject;
        private Unit ActiveUnit => DevToolsManager.ActiveUnit;

        public override void Back()
        {
            SavePOIs();
            DevEditorUIManager.ShowUnitEditorUI();
            base.Back();
            poisObject = null;
        }

        public override void ShowScreen()
        {
            if(ActiveUnit == null)
            {
                Back();
                return;
            }

            if(poisObject == null)
            {
                string unitID = ActiveUnit.unitID;
                string poisPath = ContentLoader.CombinePath(
                    ContentPaths.UnitFolder(unitID, ContentConstValues.FOLDER_POIS),
                    $"pois.{ContentConstValues.EXTENSION_POIS}");

                poisObject = ContentLoader.ReturnObjectOfType<POIs>(poisPath, false);

                if (poisObject == null)
                {
                    poisObject = new POIs();
                    poisObject.unitID = unitID;
                    poisObject.places = new List<POI_Place>();
                    poisObject.lines = new List<POI_Line>();
                    SavePOIs();
                }
            }

            UpdatePlacesList();
            UpdateLinesList();
            base.ShowScreen();
        }

        public override void HideScreen()
        {
            SavePOIs();
            base.HideScreen();
        }

        public void AddPlace()
        {
            POI_Place place = new POI_Place();
            poisObject.places.Add(place);
            DevToolsManager.Instance.SetActviePOI(place);
            DevEditorUIManager.ShowSinglePOIEditorUI();
        }

        public void AddLine()
        {
            POI_Line line = new POI_Line();
            poisObject.lines.Add(line);
            DevToolsManager.Instance.SetActviePOI(line);
            DevEditorUIManager.ShowSinglePOIEditorUI();
        }

        public void SavePOIs()
        {
            if (poisObject != null)
                DevContentUtilities.SavePOIs(poisObject);
        }

        public void DeletePOI(POI poi)
        {
            if (poi.GetType() == typeof(POI_Line))
            {
                poisObject.lines.Remove((POI_Line)poi);
            }
            else if (poi.GetType() == typeof(POI_Place))
            {
                poisObject.places.Remove((POI_Place)poi);
            }
        }

        private void UpdatePlacesList()
        {
            int currentPlacesButtonsCount = placesParent.childCount;

            for (int i = 0; i < poisObject.places.Count; i++)
            {
                DevContentEditorButton button;
                POI poi = poisObject.places[i];

                if (i >= currentPlacesButtonsCount)
                    Instantiate(poiButton, placesParent);

                button = placesParent.GetChild(i).GetComponent<DevContentEditorButton>();
                button.SetupButton(poi);
            }

            for (int i = currentPlacesButtonsCount; i > poisObject.places.Count; i--)
            {
                placesParent.GetChild(i - 1).gameObject.Destroy();
            }
        }


        private void UpdateLinesList()
        {
                int currentLinesButtonsCount = linesParent.childCount;

            for (int i = 0; i < poisObject.lines.Count; i++)
            {
                DevContentEditorButton button;
                POI poi = poisObject.lines[i];

                if (i >= currentLinesButtonsCount)
                    Instantiate(poiButton, linesParent);

                button = linesParent.GetChild(i).GetComponent<DevContentEditorButton>();
                button.SetupButton(poi);
            }

            for (int i = currentLinesButtonsCount; i > poisObject.lines.Count; i--)
            {
                linesParent.GetChild(i - 1).gameObject.Destroy();
            }
        }

        private void FixedUpdate()
        {
            bool validationGood = poisObject.ValidateContent(out string message);
            issuesTMP.gameObject.SetActiveOptimized(!validationGood);

            if (issuesTMP.text == message)
                issuesTMP.text = message + "<size=1>.</size>";

            if (validationGood == false)
                issuesTMP.text = message;
        }
    }
}
