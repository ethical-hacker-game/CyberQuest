using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DevTools
{
    public class AppScenarioStageEditorUI_FrameButton : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI imageNameTMP;
        [SerializeField] private Image frameImage;

        private AppScenario.Frame frame;

        public void SetupButton(AppScenario.Frame frame)
        {
            this.frame = frame;
            imageNameTMP.text = $"{frame.imageName} <size=50%>({frame.delayMs})ms</size>";

            if (frame.Image != null)
                frameImage.sprite = frame.Image;
        }

        public void ShowFrame()
        {
            AppScenarioStageEditorUI.Instance.ShowFrame(transform.GetSiblingIndex());
        }

        public void RemoveFrames()
        {
            AppScenarioStageEditorUI.Instance.RemoveFrames(transform.GetSiblingIndex());
        }

        public void StartNewStage()
        {
            AppScenarioStageEditorUI.Instance.StartNewStageFromFrame(transform.GetSiblingIndex());
        }
    }
}
