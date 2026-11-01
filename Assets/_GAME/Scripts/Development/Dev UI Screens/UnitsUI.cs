using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DevTools
{
    public class UnitsUI : UI_Screen
    {
        [SerializeField] private RectTransform unitsParent;
        [SerializeField] private GameObject unitButtonPrefab;

        public override void Back()
        {
            DevEditorUIManager.ShowToolsUI();
            base.Back();
        }

        public override void ShowScreen()
        {
            UpdateUnitsList();
            base.ShowScreen();
        }

        public void AddUnit()
        {
            InputTextUI.SetupInputTextUI("New Unit", "New Unit ID:");
            InputTextUI.BackButtonEvent.AddListener(DevEditorUIManager.ShowUnitsUI);
            InputTextUI.ContinueButtonEvent.AddListener((string unitID) =>
            {
                Unit unit = DevToolsManager.Instance.CreateUnit(unitID);

                if (unit != null)
                {
                    DevContentUtilities.SaveUnit(unit);
                    DevEditorUIManager.ShowUnitsUI();
                }
                else
                {
                    DevEditorUIManager.HideAll();
                    ContentUI.SetupContentUI("Error", null, $"Unit with this id already exists:\n{unitID}", "OK");
                    ContentUI.ButtonEvents[0].AddListener(DevEditorUIManager.ShowUnitsUI);
                }
            });
        }

        public void LoadUnit(string unitID)
        {
            DevToolsManager.Instance.SetActvieUnit(CourseManager.GetUnitByID(unitID));
            DevEditorUIManager.ShowUnitEditorUI();
        }

        private void UpdateUnitsList()
        {
            int currentButtonsCount = unitsParent.childCount;

            for (int i = 0; i < CourseManager.Units.Count; i++)
            {
                DevContentEditorButton button;

                if (i >= currentButtonsCount)
                    Instantiate(unitButtonPrefab, unitsParent);

                button = unitsParent.GetChild(i).GetComponent<DevContentEditorButton>();
                button.SetupButton(CourseManager.Units[i]);
            }

            for (int i = currentButtonsCount; i > CourseManager.Units.Count; i--)
            {
                unitsParent.GetChild(i - 1).gameObject.Destroy();
            }
        }
    }
}
