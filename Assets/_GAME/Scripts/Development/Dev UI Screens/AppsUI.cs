using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DevTools
{
    public class AppsUI : UI_Screen
    {
        [SerializeField] private RectTransform appsParent;
        [SerializeField] private GameObject appButtonPrefab;

        public override void Back()
        {
            DevEditorUIManager.ShowUnitEditorUI();
            base.Back();
        }

        public override void ShowScreen()
        {
            UpdateAppsList();
            base.ShowScreen();
        }

        public void AddAppCore()
        {
            InputTextUI.SetupInputTextUI("New App", "New App ID:");
            InputTextUI.BackButtonEvent.AddListener(DevEditorUIManager.ShowAppsUI);
            InputTextUI.ContinueButtonEvent.AddListener((string appID) =>
            {
                AppCore appCore = DevToolsManager.Instance.CreateAppCore(appID);

                if(appCore != null)
                {
                    DevToolsManager.Instance.SetActvieAppCore(appCore);
                    DevEditorUIManager.ShowAppEditorUI();
                }
                else
                {
                    DevEditorUIManager.HideAll();
                    ContentUI.SetupContentUI("Error", null, $"App core with this id already exists\n{appID}", "OK");
                    ContentUI.ButtonEvents[0].AddListener(DevEditorUIManager.ShowAppsUI);
                }
            });
        }

        public void LoadAppCore(string appID)
        {
            DevToolsManager.Instance.SetActvieAppCore(AppsManager.Instance.GetAppByID(appID));
            DevEditorUIManager.ShowAppEditorUI();
        }

        private void UpdateAppsList()
        {
            int currentButtonsCount = appsParent.childCount; 

            for (int i = 0; i < AppsManager.Instance.Apps.Count; i++)
            {
                DevContentEditorButton button;

                if (i >= currentButtonsCount)
                    Instantiate(appButtonPrefab, appsParent);

                button = appsParent.GetChild(i).GetComponent<DevContentEditorButton>();
                button.SetupButton(AppsManager.Instance.Apps[i]);
            }

            for (int i = currentButtonsCount; i > AppsManager.Instance.Apps.Count; i--)
            {
                appsParent.GetChild(i-1).gameObject.Destroy();
            }
        }
    }
}
