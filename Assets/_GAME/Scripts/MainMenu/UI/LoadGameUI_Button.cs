using Michsky.UI.Beam;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

namespace MainMenu
{
    [RequireComponent(typeof(GrayscaleUI))]
    public class LoadGameUI_Button : MonoBehaviour
    {
        [SerializeField] private SpotButtonManager buttonManager;
        [SerializeField] private TextMeshProUGUI timeText;
        [SerializeField] private TextMeshProUGUI unitText;
        [SerializeField] private TextMeshProUGUI missionText;
        [SerializeField] private Image unitImage;

        private SaveInfo saveInfo;
        private bool isLocked = false;

        public void SetupButton(SaveInfo saveInfo)
        {
            this.saveInfo = saveInfo;
            timeText.SetText(saveInfo.saveTime);
            unitText.SetText(saveInfo.unitName);

            string missionString = saveInfo.currentMission + $"\n<size=50%>{saveInfo.saveId}</size>";
            missionText.SetText(missionString);

            Unit unit = CourseManager.GetUnitByID(saveInfo.unitID);

            if (unit == null)
            {
                LockButton();
                return;
            }

            Sprite picture = unit.GetPicture();

            if (picture == null)
                unitImage.gameObject.SetActiveOptimized(false);
            else
                unitImage.sprite = picture;
        }

        public void Click()
        {
            if (isLocked)
                return;

            MainMenuController.StartLoadGame(saveInfo);
        }

        public void DeleteSave()
        {
            ContentUI.SetupContentUI("Delete Game?", null,
                $"Are you sure you want to delete this game?\nThis action cannot be undone.\n\n<size=50%>{saveInfo.saveId}</size>",
                "Delete", "Cancel");
            ContentUI.ButtonEvents[0].AddListener(() =>
            {
                SaveLoad.Delete(saveInfo.saveId);
                MainMenuUIManager.ShowLoadGameUI();
            });
            ContentUI.ButtonEvents[1].AddListener(() =>
            {
                MainMenuUIManager.ShowLoadGameUI();
            });
        }

        private void LockButton()
        {
            isLocked = true;
            buttonManager.SetActionText("ERROR - This game cannot be loaded.");
            unitImage.gameObject.SetActiveOptimized(false);

            GrayscaleUI grayscaleUI = GetComponent<GrayscaleUI>();

            if (grayscaleUI == null)
                return;

            grayscaleUI.ToGrayscale();
        }
    }
}
