using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WPM;
using UnityEngine.UI;
using UnityEngine.Events;

namespace DevTools
{
    public class TextSelectionUI : UI_Screen
    {
        public static TextSelectionUI Instance => DevEditorUIManager.Instance.TextSelectionUI;

        [SerializeField] private GameObject imageButtonPrefab;
        [SerializeField] private Transform imagesButtonsParent;

        [Space]
        [SerializeField] private UnityEvent<string> selectStringEvent;
        [SerializeField] private UnityEvent backButtonEvent;
        public static UnityEvent<string> SelectStringEvent { get => Instance.selectStringEvent; set => Instance.selectStringEvent = value; }
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

        public static void SetupTextSelectionUI(List<string> strings, string defaultString)
        {
            DevEditorUIManager.ShowTextSelectionUI();

            Instance.backButtonEvent.RemoveAllListeners();
            Instance.selectStringEvent.RemoveAllListeners();

            Instance.AddTextButton(defaultString);

            for (int i = 0; i < strings.Count; i++)
            {
                Instance.AddTextButton(strings[i]);
            }
        }

        public void SelectText(string id)
        {
            Instance.selectStringEvent.Invoke(id);
            Back();
        }

        private void AddTextButton(string id)
        {
            TextSelectionUIButton newButton = Instantiate(imageButtonPrefab, imagesButtonsParent)
                .GetComponent<TextSelectionUIButton>();

            newButton.SetupButton(id);
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
