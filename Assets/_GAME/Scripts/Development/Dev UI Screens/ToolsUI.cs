using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DevTools
{
    public class ToolsUI : UI_Screen
    {
        public override void Back()
        {
            if(SceneLoader.GetActiveSceneIndex == 0)
            {
#if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
#endif
                Application.Quit();
            }

            SceneLoader.LoadSceneAsync(SceneLoader.MAIN_MENU_SCENE_INDEX);
        }

        public void ClickCourses()
        {
            DevEditorUIManager.ShowUnitsUI();
        }

        public void ClickCommands()
        {
            DevEditorUIManager.ShowCommandsUI();
        }

        public void ClickAppsEditor()
        {
            DevEditorUIManager.ShowAppsUI();
        }

        public void OpenContentDirectory()
        {
            UtilityMethods.OpenPath(ContentLoader.GetContentPath());
        }

        public void OpenMinigamesScene()
        {
            SceneManager.LoadScene("DevMinigameTests");
        }
    }
}
