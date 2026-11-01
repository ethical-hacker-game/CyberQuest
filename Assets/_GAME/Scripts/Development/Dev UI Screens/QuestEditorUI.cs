using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using RuntimeInspectorNamespace;
using WPM;
using UnityEngine.UI;
using TMPro;

namespace DevTools
{
    public class QuestEditorUI : UI_Screen
    {
        [Space]
        [SerializeField] private RectTransform tasksParent;
        [SerializeField] private GameObject tasksButton;

        [Space]
        [SerializeField] private TextMeshProUGUI issuesTMP;

        [Space]
        [SerializeField] private RuntimeInspector runtimeInspector;
        [SerializeField] private Image iconImage;
        [SerializeField] private Image characterImage;
        [SerializeField] private List<GameObject> uiElemetns = new List<GameObject>();

        private WorldMapGlobe globe;
        private bool uiActive = true;

        private Quest ActiveQuest => DevToolsManager.ActiveQuest;

        public override void Back()
        {
            DevContentUtilities.SaveQuest(ActiveQuest, DevToolsManager.ActiveUnit.unitID);
            DevEditorUIManager.ShowUnitEditorUI();
            DevToolsManager.Instance.SetActvieQuest("null");
            base.Back();
        }

        public override void ShowScreen()
        {
            UpdateTasksList();
            runtimeInspector.Inspect(ActiveQuest);
            iconImage.sprite = ActiveQuest.GetIcon();
            //characterImage.sprite = ActiveQuest.GetCharacter();

            uiActive = false;
            ToggleUIElements();

            base.ShowScreen();

            if (CanAttachActionToGlobe())
                globe.OnCityClick += Globe_OnCityClick;
        }

        public override void HideScreen()
        {
            base.HideScreen();

            if (CanAttachActionToGlobe())
                globe.OnCityClick -= Globe_OnCityClick;
        }

        public void ToggleUIElements()
        {
            uiActive = !uiActive;

            for (int i = 0; i < uiElemetns.Count; i++)
            {
                uiElemetns[i].SetActiveOptimized(uiActive);
            }
        }

        public void ChangeIcon()
        {
            // Quest icons are built-in per primary task type (game Resources), not unit content.
            ActiveQuest.questIconID = ActiveQuest.GetPrimaryTaskType().ToString();
            iconImage.sprite = ActiveQuest.GetIcon();
        }

        public void AddTaskSingleChoice()
        {
            AddTask(TaskBase.TASK_TYPE.SingleChoice);
        }

        public void AddTaskTrueFalse()
        {
            AddTask(TaskBase.TASK_TYPE.TrueFalse);
        }

        public void AddTaskInput()
        {
            AddTask(TaskBase.TASK_TYPE.Input);
        }

        public void AddTaskWebsite()
        {
            AddTask(TaskBase.TASK_TYPE.Website);
        }

        public void AddTaskAction()
        {
            AddTask(TaskBase.TASK_TYPE.Action);
        }

        public void AddTaskMinigame()
        {
            AddTask(TaskBase.TASK_TYPE.Minigame);
        }

        public void AddTaskDialogue()
        {
            AddTask(TaskBase.TASK_TYPE.Dialogue);
        }

        public void AddTask(TaskBase.TASK_TYPE taskType)
        {
            string prefix = $"{ActiveQuest.questID}_";
            InputTextUI.SetupInputTextUI("New Task", $"New Task ID:\n{prefix}");
            InputTextUI.BackButtonEvent.AddListener(DevEditorUIManager.ShowQuestEditorUI);
            InputTextUI.ContinueButtonEvent.AddListener((string taskID) =>
            {
                taskID = prefix + taskID;
                TaskBase task = DevToolsManager.Instance.CreateTask(taskID, taskType, ActiveQuest.questID);

                if (task != null)
                {
                    task.universalPersonaContent = ActiveQuest.universalPersonaContent;
                    ActiveQuest.taskIDs.Add(taskID);
                    DevEditorUIManager.ShowQuestEditorUI();
                }
                else
                {
                    DevEditorUIManager.HideAll();
                    ContentUI.SetupContentUI("Error", null, $"Task with this id already exists\n{taskID}", "OK");
                    ContentUI.ButtonEvents[0].AddListener(DevEditorUIManager.ShowQuestEditorUI);
                }
            });
        }

        public void LoadTask(string taskID)
        {
            DevToolsManager.Instance.SetActvieTask(taskID);
            DevEditorUIManager.ShowTaskEditorUI();
        }

        private void UpdateTasksList()
        {
            int currentButtonsCount = tasksParent.childCount;

            for (int i = 0; i < ActiveQuest.taskIDs.Count; i++)
            {
                DevContentEditorButton button;
                string taskID = ActiveQuest.taskIDs[i];
                TaskBase tempTask = QuestManager.GetTaskByID(taskID);

                if (i >= currentButtonsCount)
                    Instantiate(tasksButton, tasksParent);

                button = tasksParent.GetChild(i).GetComponent<DevContentEditorButton>();

                if (tempTask == null)
                {
                    button.SetupButton($"Error\nTask {taskID} cannot be found.", taskID);
                    button.CustomDeleteEvent.AddListener(() =>
                    {
                        ActiveQuest.taskIDs.RemoveAt(button.transform.GetSiblingIndex());
                        DevEditorUIManager.ShowQuestEditorUI();
                    });
                }
                else
                {
                    button.SetupButton(tempTask, ActiveQuest);
                }
            }

            for (int i = currentButtonsCount; i > ActiveQuest.taskIDs.Count; i--)
            {
                tasksParent.GetChild(i - 1).gameObject.Destroy();
            }
        }

        private void Globe_OnCityClick(int cityIndex, int buttonIndex)
        {
            if (buttonIndex != 0)
                return;

            if (uiActive)
                return;

            ActiveQuest.cityIndex = cityIndex;
            ToggleUIElements();

            CursorHintsManager.Instance.ShowHint("", Vector2.zero, this);
            CursorHintsManager.Instance.ClearHint(this);
        }

        private bool CanAttachActionToGlobe()
        {
            if (globe == null)
                globe = WorldMapGlobe.instance;

            return globe != null;
        }

        private void FixedUpdate()
        {
            bool validationGood = ActiveQuest.ValidateContent(out string message);
            issuesTMP.gameObject.SetActiveOptimized(!validationGood);

            if (issuesTMP.text == message)
                issuesTMP.text = message + "<size=1>.</size>";

            if (validationGood == false)
                issuesTMP.text = message;
        }
    }
}
