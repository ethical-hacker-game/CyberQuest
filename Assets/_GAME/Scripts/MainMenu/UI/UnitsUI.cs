using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using Michsky.UI.Beam;
using Sirenix.OdinInspector;
using UnityEngine.UI;

namespace MainMenu
{
    public class UnitsUI : UI_Screen
    {
        [SerializeField] private ChapterIdentifier chapterIdentifier;
        [SerializeField] private LayoutGroupFix layoutGroupFix;
        [SerializeField] private GrayscaleUI contentGrayscaleUI;
        [Space]
        [SerializeField] private RectTransform unitsParent;
        [SerializeField] private GameObject unitButtonPrefab;
        [Space]
        [SerializeField] private Image unitsBar;

        private int selectedUnitIndex = 0;

        public override void Back()
        {
            MainMenuUIManager.ShowHomeUI();
            MainMenuController.SelectUnit(null);
            base.Back();
        }

        public override void ShowScreen()
        {
            base.ShowScreen();
            PrepareUnitInfo();
            PrepareUnitButtons();
        }

        public override void HideScreen()
        {
            base.HideScreen();
        }

        public void StartGame()
        {
            MainMenuController.StartNewGame();
        }

        public void ChangeUnit(int change)
        {
            if (CourseManager.Units.IsNullOrEmpty())
                return;

            selectedUnitIndex += change;
            selectedUnitIndex.ClampInt(0, CourseManager.Units.Count - 1);
            ShowUnit(selectedUnitIndex);
        }

        public void ShowUnit(int unitIndex)
        {
            if (CourseManager.Units.IsNullOrEmpty())
                return;

            unitIndex.ClampInt(0, CourseManager.Units.Count - 1);
            MainMenuController.SelectUnit(unitIndex);
            selectedUnitIndex = unitIndex;
            PrepareUnitInfo();
        }

        private void PrepareUnitInfo()
        {
            if (MainMenuController.SelectedUnit == null)
            {
                if (CourseManager.Units.IsNullOrEmpty())
                    return;

                MainMenuController.SelectUnit(0);
                selectedUnitIndex = 0;
            }

            unitsBar.fillAmount = (selectedUnitIndex + 1f) / CourseManager.Units.Count;

            chapterIdentifier.titleObject.text = MainMenuController.SelectedUnit.unitName;
            chapterIdentifier.descriptionObject.text = MainMenuController.SelectedUnit.GetUnitDescription();

            Sprite sprite = MainMenuController.SelectedUnit.GetPicture();

            if (sprite != null)
            {
                chapterIdentifier.backgroundImage.sprite = sprite;
            }

            if (MainMenuController.SelectedUnit.placeholder == false)
            {
                chapterIdentifier.SetUnlocked();
                contentGrayscaleUI.ToDefault();
            }
            else
            {
                chapterIdentifier.SetLocked();
                contentGrayscaleUI.ToGrayscale();
            }

            layoutGroupFix.FixLayout();
        }

        private void PrepareUnitButtons()
        {
            for (int i = unitsParent.childCount - 1; i >= 0; i--)
            {
                unitsParent.GetChild(i).gameObject.Destroy();
            }

            for (int i = 0; i < CourseManager.Units.Count; i++)
            {
                int tempI = i;
                UnitsUI_Button button = Instantiate(unitButtonPrefab, unitsParent).GetComponent<UnitsUI_Button>();
                button.SetupButton(CourseManager.Units[i], tempI);
            }
        }
    }
}
