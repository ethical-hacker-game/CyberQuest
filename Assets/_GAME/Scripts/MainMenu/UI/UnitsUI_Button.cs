using Michsky.UI.Beam;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

namespace MainMenu
{
    [RequireComponent(typeof(GrayscaleUI))]
    public class UnitsUI_Button : MonoBehaviour
    {
        [SerializeField] private SpotButtonManager buttonManager;
        [Space]
        [SerializeField] private string unlockedText = "Select Unit";
        [SerializeField] private string lockedText = "Locked";

        private GrayscaleUI grayscaleUI;
        private Unit unit;
        private int index;

        public void SetupButton(Unit unit, int index)
        {
            this.unit = unit;
            this.index = index;

            grayscaleUI = GetComponent<GrayscaleUI>();

            buttonManager.SetTitle(unit.unitName);

            Sprite picture = unit.GetPicture();

            if (picture != null)
            {
                buttonManager.SetBackground(picture);
            }

            if (unit.placeholder == false)
            {
                grayscaleUI.ToDefault();
                buttonManager.SetActionText(unlockedText);
            }
            else
            {
                grayscaleUI.ToGrayscale();
                buttonManager.SetActionText(lockedText);
            }
        }

        public void Click()
        {
            MainMenuUIManager.Instance.UnitsUI.ShowUnit(index);
        }
    }
}