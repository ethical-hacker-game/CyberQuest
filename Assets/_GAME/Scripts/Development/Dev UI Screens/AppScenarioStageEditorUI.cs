using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using RuntimeInspectorNamespace;
using TMPro;

namespace DevTools
{
    public class AppScenarioStageEditorUI : UI_Screen
    {
        public static AppScenarioStageEditorUI Instance => DevEditorUIManager.Instance.AppScenarioStageEditorUI;

        [SerializeField] private RectTransform framesParent;
        [SerializeField] private GameObject frameButton;
        [SerializeField] private RuntimeInspector runtimeInspector;
        [SerializeField] private AppImage appImage;
        [SerializeField] private RectTransform marginBox;
        [SerializeField] private RectTransform marginBoxOutline;
        [SerializeField] private UIInteractions appMouseArea;
        [SerializeField] private TextMeshProUGUI mousePositionDetailsTMP;

        [Space]
        [SerializeField] private TextMeshProUGUI issuesTMP;

        private Vector2? mousePositionAppImage = null;
        private Vector2? mouseClickPositionAppImage = null;

        private AppCore ActiveAppCore => DevToolsManager.ActiveAppCore;
        private AppScenario ActiveAppScenario => DevToolsManager.ActiveAppScenario;
        private AppScenario.Stage ActiveAppScenarioStage => DevToolsManager.ActiveAppScenarioStage;

        public void PlayStage()
        {
            appImage.SetupAppImage(ActiveAppScenario);
            appImage.ForceStop();
            appImage.StartStage(ActiveAppScenarioStage.stageID);
        }

        public override void Back()
        {
            DevContentUtilities.SaveAppScenario(ActiveAppScenario);
            DevEditorUIManager.ShowAppScenarioEditorUI();
            DevToolsManager.Instance.SetActvieAppScenario(ActiveAppScenario);
            base.Back();
        }

        public override void ShowScreen()
        {
            runtimeInspector.Inspect(ActiveAppScenarioStage);
            PlayStage();
            UpdatFramesList();

            RectTransform appImageRect = appImage.GetComponent<RectTransform>();
            marginBox.position = appImageRect.position;
            marginBox.rotation = appImageRect.rotation;
            marginBox.localScale = appImageRect.localScale;
            marginBox.anchorMin = appImageRect.anchorMin;
            marginBox.anchorMax = appImageRect.anchorMax;
            marginBox.anchoredPosition = appImageRect.anchoredPosition;
            marginBox.sizeDelta = appImageRect.sizeDelta;
            marginBox.pivot = appImageRect.pivot;

            Vector4 margin = ActiveAppScenario.margin;
            marginBoxOutline.offsetMin = new Vector2(margin.x, margin.y);
            marginBoxOutline.offsetMax = new Vector2(-margin.z, -margin.w);

            base.ShowScreen();
        }

        public override void HideScreen()
        {
            base.HideScreen();
        }

        public void RemoveFrames(int frameIndex)
        {
            string[] buttonTexts = new string[]
            {
                $"Del. prev. frames (inc. this)",
                "Delete this frame",
                $"Del. next frames (inc. this)",
                "Back"
            };

            string header = "Remove Frames";
            string content = "Choose which frames to remove:";

            ContentUI.SetupContentUI(header, null, content, buttonTexts);

            ContentUI.ButtonEvents[0].AddListener(() =>
            {
                DevToolsManager.Instance.RemoveFramesRangeFromStage(ActiveAppScenarioStage, 0, frameIndex + 1);
                DevEditorUIManager.ShowAppScenarioStageEditorUI();
            });

            ContentUI.ButtonEvents[1].AddListener(() =>
            {
                DevToolsManager.Instance.RemoveFramesRangeFromStage(ActiveAppScenarioStage, frameIndex, 1);
                DevEditorUIManager.ShowAppScenarioStageEditorUI();
            });

            ContentUI.ButtonEvents[2].AddListener(() =>
            {
                List<AppScenario.Frame> frames = ActiveAppScenarioStage.frames;
                int countToRemove = frames.Count - frameIndex;
                DevToolsManager.Instance.RemoveFramesRangeFromStage(ActiveAppScenarioStage, frameIndex, countToRemove);
                DevEditorUIManager.ShowAppScenarioStageEditorUI();
            });

            ContentUI.ButtonEvents[3].AddListener(() =>
            {
                DevEditorUIManager.ShowAppScenarioStageEditorUI();
            });
        }

        public void StartNewStageFromFrame(int frameIndex)
        {
            InputTextUI.SetupInputTextUI("New Stage", $"New Stage ID:");
            InputTextUI.BackButtonEvent.AddListener(DevEditorUIManager.ShowAppScenarioStageEditorUI);
            InputTextUI.ContinueButtonEvent.AddListener((string stageID) =>
            {
                AppScenario.Stage appScenarioStage = DevToolsManager.Instance.DivideAppScenarioStage(ActiveAppScenario, ActiveAppScenarioStage, stageID, frameIndex);

                if (appScenarioStage != null)
                {
                    DevEditorUIManager.ShowAppScenarioEditorUI();
                }
                else
                {
                    DevEditorUIManager.HideAll();
                    ContentUI.SetupContentUI("Error", null, $"Stage with this id already exists:\n{stageID}", "OK");
                    ContentUI.ButtonEvents[0].AddListener(DevEditorUIManager.ShowAppScenarioStageEditorUI);
                }
            });
        }

        public void ShowFrame(int frameIndex)
        {
            appImage.ShowFrame(frameIndex);
        }

        public void ShowLinks()
        {
            appImage.ShowLastFrame();
        }

        private void UpdatFramesList()
        {
            int currentButtonsCount = framesParent.childCount;

            List<AppScenario.Frame> frames = ActiveAppScenarioStage.frames;

            for (int i = 0; i < frames.Count; i++)
            {
                int tempI = i;
                AppScenarioStageEditorUI_FrameButton button;

                if (tempI >= currentButtonsCount)
                    Instantiate(frameButton, framesParent);

                button = framesParent.GetChild(tempI).GetComponent<AppScenarioStageEditorUI_FrameButton>();
                button.SetupButton(frames[tempI]);
            }

            for (int i = currentButtonsCount; i > frames.Count; i--)
            {
                GameObject uselessButton = framesParent.GetChild(i - 1).gameObject;
                uselessButton.transform.SetParent(null);
                uselessButton.Destroy();
            }
        }

        private void ReloadStage()
        {
            appImage.StartStage(ActiveAppScenarioStage.stageID);
        }

        private void Start()
        {
            mousePositionAppImage = null;
            mouseClickPositionAppImage = null;

            appMouseArea.OnMouseMove.AddListener(() =>
            {
                Vector2 localMousePos;
                RectTransform rect = appMouseArea.GetComponent<RectTransform>();
                RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    rect,
                    Input.mousePosition,
                    null,
                    out localMousePos
                );

                Vector2 topLeftOrigin = new Vector2(
                    localMousePos.x + rect.rect.width * rect.pivot.x,
                    rect.rect.height * (1 - rect.pivot.y) - localMousePos.y
                );
                mousePositionAppImage = topLeftOrigin;
            });

            appMouseArea.OnMouseExit.AddListener(() =>
            {
                mousePositionAppImage = null;
                mouseClickPositionAppImage = null;
            });

            appMouseArea.OnMouseDown.AddListener(() =>
            {
                mouseClickPositionAppImage = mousePositionAppImage;
            });
        }

        private void FixedUpdate()
        {
            bool validationGood = ActiveAppScenarioStage.ValidateContent(out string message);
            issuesTMP.gameObject.SetActiveOptimized(!validationGood);

            if (issuesTMP.text == message)
                issuesTMP.text = message + "<size=1>.</size>";

            if (validationGood == false)
                issuesTMP.text = message;

            string mousePos = "";
            string clickSize = "";

            if (mousePositionAppImage != null)
            {
                mousePos = $"x: {mousePositionAppImage.Value.x:F0}, y: {mousePositionAppImage.Value.y:F0}";

                if (mouseClickPositionAppImage != null)
                {
                    Vector2 diff = mousePositionAppImage.Value - mouseClickPositionAppImage.Value;
                    clickSize = $"\n({diff.x:F0}, {diff.y:F0})";
                }
            }

            mousePositionDetailsTMP.text = $"{mousePos}{clickSize}";
        }
    }
}
