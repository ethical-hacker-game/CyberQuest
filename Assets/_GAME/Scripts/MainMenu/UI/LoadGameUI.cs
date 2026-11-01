using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

namespace MainMenu
{
    public class LoadGameUI : UI_Screen
    {
        [SerializeField] private RectTransform gamesParent;
        [SerializeField] private GameObject gameButtonPrefab;
        [SerializeField] private GameObject noGamesObject;

        public void StartNewGame()
        {
            MainMenuUIManager.ShowUnitsUI();
        }

        public override void Back()
        {
            MainMenuUIManager.ShowHomeUI();
            base.Back();
        }

        public override void ShowScreen()
        {
            base.ShowScreen();
            PrepareGamesButtons();
        }

        public override void HideScreen()
        {
            base.HideScreen();
        }

        private void PrepareGamesButtons()
        {
            List<SaveInfo> saves = SaveLoad.GetAllSaveInfo();

            noGamesObject.SetActiveOptimized(saves.IsNullOrEmpty());

            for (int i = gamesParent.childCount - 1; i >= 0; i--)
            {
                Destroy(gamesParent.GetChild(i).gameObject);
            }

            for (int i = 0; i < saves.Count; i++)
            {
                LoadGameUI_Button button = Instantiate(gameButtonPrefab, gamesParent).GetComponent<LoadGameUI_Button>();
                button.SetupButton(saves[i]);
            }
        }

        private void Update()
        {
            if (Input.GetKey(KeyCode.LeftShift))
            {
                if (Input.GetKeyDown(KeyCode.O))
                {
                    Process.Start(SaveLoad.SavesPath);
                }
            }
        }
    }
}
