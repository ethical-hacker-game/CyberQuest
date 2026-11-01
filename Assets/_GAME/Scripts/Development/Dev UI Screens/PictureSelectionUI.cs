using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WPM;
using UnityEngine.UI;
using UnityEngine.Events;

namespace DevTools
{
    public class PictureSelectionUI : UI_Screen
    {
        public static PictureSelectionUI Instance => DevEditorUIManager.Instance.PictureSelectionUI;

        [SerializeField] private GameObject imageButtonPrefab;
        [SerializeField] private Transform imagesButtonsParent;

        [Space]
        [SerializeField] private UnityEvent<string> selectImageEvent;
        [SerializeField] private UnityEvent backButtonEvent;
        public static UnityEvent<string> SelectImageEvent { get => Instance.selectImageEvent; set => Instance.selectImageEvent = value; }
        public static UnityEvent BackButtonEvent { get => Instance.backButtonEvent; set => Instance.backButtonEvent = value; }

        public override void Back()
        {
            base.Back();
            Instance.backButtonEvent.Invoke();
        }

        public override void ShowScreen()
        {
            base.ShowScreen();
        }

        public override void HideScreen()
        {
            DeleteButtons();
            base.HideScreen();
        }

        public static void SetupPictureSelectionUI(List<PictureReferences> pictures, Sprite defaultPicture)
        {
            DevEditorUIManager.ShowPictureSelectionUI();

            Instance.backButtonEvent.RemoveAllListeners();
            Instance.selectImageEvent.RemoveAllListeners();

            Instance.AddImageButton("default", defaultPicture);

            for (int i = 0; i < pictures.Count; i++)
            {
                Instance.AddImageButton(pictures[i].pictureID, pictures[i].Picture);
            }
        }

        public void SelectPicture(string id)
        {
            Instance.selectImageEvent.Invoke(id);
            Back();
        }

        private void AddImageButton(string id, Sprite sprite)
        {
            PictureSelectionUIButton newButton = Instantiate(imageButtonPrefab, imagesButtonsParent)
                .GetComponent<PictureSelectionUIButton>();

            newButton.SetupButton(id, sprite);
        }

        private void DeleteButtons()
        {
            for (int i = imagesButtonsParent.childCount - 1; i >= 0; i--)
            {
                imagesButtonsParent.GetChild(i).gameObject.Destroy();
            }
        }
    }
}
