using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Events;

namespace DevTools
{
    public class DevContentEditorButton : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI textTMP;
        [SerializeField] private GameObject editButton;

        private ELEMENT_TYPE elementType;
        private Unit unit;
        private Quest quest;
        private TaskBase task;
        private MailData mail;
        private Command command;
        private POI poi;
        private AppCore appCore;
        private AppScenario appScenario;
        private AppScenario.Stage appScenarioStage;
        private string customID;

        public UnityEvent CustomDeleteEvent { get; set; } = new UnityEvent();

        public void SetupButton(Unit unit)
        {
            this.unit = unit;
            textTMP.text = unit.ToFormattedString();
            elementType = ELEMENT_TYPE.Unit;
            editButton.SetActiveOptimized(true);
        }

        public void SetupButton(Quest quest, Unit parentUnit)
        {
            this.quest = quest;
            this.unit = parentUnit;
            textTMP.text = quest.ToFormattedString();
            elementType = ELEMENT_TYPE.Quest;
            editButton.SetActiveOptimized(true);
        }

        public void SetupButton(TaskBase task, Quest parentQuest)
        {
            this.task = task;
            this.quest = parentQuest;
            textTMP.text = task.ToFormattedString();
            elementType = ELEMENT_TYPE.Task;
            editButton.SetActiveOptimized(true);
        }

        public void SetupButton(MailData mail, TaskBase parentTask)
        {
            this.mail = mail;
            this.task = parentTask;
            textTMP.text = mail.ToFormattedString();
            elementType = ELEMENT_TYPE.Mail;
            editButton.SetActiveOptimized(true);
        }

        public void SetupButton(Command command)
        {
            this.command = command;
            textTMP.text = command.ToString();
            elementType = ELEMENT_TYPE.Command;
            editButton.SetActiveOptimized(true);
        }

        public void SetupButton(POI poi)
        {
            this.poi = poi;
            textTMP.text = poi.ToString();
            elementType = ELEMENT_TYPE.POI;
            editButton.SetActiveOptimized(true);
        }

        public void SetupButton(AppCore appCore)
        {
            this.appCore = appCore;
            textTMP.text = appCore.ToFormattedString();
            elementType = ELEMENT_TYPE.App;
            editButton.SetActiveOptimized(true);
        }

        public void SetupButton(AppScenario appScenario, AppCore appCore)
        {
            this.appScenario = appScenario;
            this.appCore = appCore;
            textTMP.text = appScenario.ToFormattedString();
            elementType = ELEMENT_TYPE.AppScenario;
            editButton.SetActiveOptimized(true);
        }

        public void SetupButton(AppScenario appScenario, AppScenario.Stage appScenarioStage)
        {
            this.appScenario = appScenario;
            this.appScenarioStage = appScenarioStage;
            textTMP.text = appScenarioStage.stageID;
            elementType = ELEMENT_TYPE.AppScenarioStage;
            editButton.SetActiveOptimized(true);
        }

        public void SetupButton(string text, string customID)
        {
            this.textTMP.text = text;
            this.customID = customID;
            editButton.SetActiveOptimized(false);
        }

        public void EditElement()
        {
            switch (elementType)
            {
                case ELEMENT_TYPE.Unit:
                    DevToolsManager.Instance.SetActvieUnit(unit);
                    DevEditorUIManager.ShowUnitEditorUI();
                    break;
                case ELEMENT_TYPE.Quest:
                    DevToolsManager.Instance.SetActvieQuest(quest.questID);
                    DevEditorUIManager.ShowQuestEditorUI();
                    break;
                case ELEMENT_TYPE.Task:
                    DevToolsManager.Instance.SetActvieTask(task.taskID);
                    DevEditorUIManager.ShowTaskEditorUI();
                    break;
                case ELEMENT_TYPE.Command:
                    DevToolsManager.Instance.SetActvieCommand(command);
                    DevEditorUIManager.ShowCommandEditorUI();
                    break;
                case ELEMENT_TYPE.POI:
                    DevToolsManager.Instance.SetActviePOI(poi);
                    DevEditorUIManager.ShowSinglePOIEditorUI();
                    break;
                case ELEMENT_TYPE.Mail:
                    DevToolsManager.Instance.SetActvieMail(mail.mailID);
                    DevEditorUIManager.ShowMailEditorUI();
                    break;
                case ELEMENT_TYPE.App:
                    DevToolsManager.Instance.SetActvieAppCore(appCore);
                    DevEditorUIManager.ShowAppEditorUI();
                    break;
                case ELEMENT_TYPE.AppScenario:
                    DevToolsManager.Instance.SetActvieAppScenario(appScenario);
                    DevEditorUIManager.ShowAppScenarioEditorUI();
                    break;
                case ELEMENT_TYPE.AppScenarioStage:
                    DevToolsManager.Instance.SetActiveAppScenarioStage(appScenarioStage);
                    DevEditorUIManager.ShowAppScenarioStageEditorUI();
                    break;
                default:
                    Debug.LogError("Element type not implemented");
                    break;
            }
        }

        public void DeleteElement()
        {
            switch (elementType)
            {
                case ELEMENT_TYPE.None:
                    CustomDeleteEvent.Invoke();
                    break;
                case ELEMENT_TYPE.Unit:
                    DeleteUnit();
                    break;
                case ELEMENT_TYPE.Quest:
                    DeleteQuest();
                    break;
                case ELEMENT_TYPE.Task:
                    DeleteTask();
                    break;
                case ELEMENT_TYPE.Command:
                    DeleteCommand();
                    break;
                case ELEMENT_TYPE.POI:
                    DeletePOI();
                    break;
                case ELEMENT_TYPE.Mail:
                    DeleteMail();
                    break;
                case ELEMENT_TYPE.App:
                    DeleteAppCore();
                    break;
                case ELEMENT_TYPE.AppScenario:
                    DeleteAppScenario();
                    break;
                case ELEMENT_TYPE.AppScenarioStage:
                    DeleteAppScenarioStage();
                    break;
                default:
                    Debug.LogError("Element type not implemented");
                    break;
            }
        }

        private void DeleteUnit()
        {
            ContentUI.SetupContentUI("Delete unit?", null, $"<b>{unit.unitName}</b>\n{unit.unitID}", "Delete unit", "Back");
            ContentUI.ButtonEvents[0].AddListener(() =>
            {
                DevToolsManager.Instance.DeleteUnit(unit.unitID);
                DevEditorUIManager.ShowUnitsUI();
            });
            ContentUI.ButtonEvents[1].AddListener(DevEditorUIManager.ShowUnitsUI);
            ContentUI.DefaultButtonIndex = 1;
        }

        private void DeleteQuest()
        {
            ContentUI.SetupContentUI("Delete quest?", null, $"<b>{quest.GetQuestName()}</b>\n{quest.questID}", "Delete quest", "Back");
            ContentUI.ButtonEvents[0].AddListener(() =>
            {
                DevToolsManager.Instance.DeleteQuest(quest.questID);
                DevEditorUIManager.ShowUnitEditorUI();
            });
            ContentUI.ButtonEvents[1].AddListener(DevEditorUIManager.ShowUnitEditorUI);
            ContentUI.DefaultButtonIndex = 1;
        }

        private void DeleteTask()
        {
            ContentUI.SetupContentUI("Delete task?", null, $"<b>{task.taskID}</b>", "Delete task", "Back");
            ContentUI.ButtonEvents[0].AddListener(() =>
            {
                DevToolsManager.Instance.DeleteTask(task.taskID);
                DevEditorUIManager.ShowQuestEditorUI();
            });
            ContentUI.ButtonEvents[1].AddListener(DevEditorUIManager.ShowQuestEditorUI);
            ContentUI.DefaultButtonIndex = 1;
        }

        private void DeleteMail()
        {
            ContentUI.SetupContentUI("Delete mail?", null, $"<b>{mail.GetSubject()}</b>\n{mail.mailID}", "Delete mail", "Back");
            ContentUI.ButtonEvents[0].AddListener(() =>
            {
                DevToolsManager.Instance.DeleteMail(mail.mailID);
                DevEditorUIManager.ShowQuestEditorUI();
            });
            ContentUI.ButtonEvents[1].AddListener(DevEditorUIManager.ShowQuestEditorUI);
            ContentUI.DefaultButtonIndex = 1;
        }

        private void DeleteCommand()
        {
            ContentUI.SetupContentUI("Delete command?", null, $"<b>{command.ToString()}</b>", "Delete command", "Back");
            ContentUI.ButtonEvents[0].AddListener(() =>
            {
                DevToolsManager.Instance.DeleteCommand(command.command);
                DevEditorUIManager.ShowCommandsUI();
            });
            ContentUI.ButtonEvents[1].AddListener(DevEditorUIManager.ShowCommandsUI);
            ContentUI.DefaultButtonIndex = 1;
        }

        private void DeletePOI()
        {
            ContentUI.SetupContentUI("Delete POI?", null, $"{poi.ToString()}", "Delete POI", "Back");
            ContentUI.ButtonEvents[0].AddListener(() =>
            {
                DevEditorUIManager.Instance.POIsEditorUI.DeletePOI(poi);
                DevEditorUIManager.ShowPOIsEditorUI();
            });
            ContentUI.ButtonEvents[1].AddListener(DevEditorUIManager.ShowPOIsEditorUI);
            ContentUI.DefaultButtonIndex = 1;
        }

        private void DeleteAppCore()
        {
            ContentUI.SetupContentUI("Delete app?", null, $"<b>{appCore.appID}</b>\n{appCore.header}", "Delete app", "Back");
            ContentUI.ButtonEvents[0].AddListener(() =>
            {
                DevToolsManager.Instance.DeleteAppCore(appCore.appID);
                DevEditorUIManager.ShowAppsUI();
            });
            ContentUI.ButtonEvents[1].AddListener(DevEditorUIManager.ShowAppsUI);
            ContentUI.DefaultButtonIndex = 1;
        }

        private void DeleteAppScenario()
        {
            ContentUI.SetupContentUI("Delete app scenario?", null, $"<b>{appScenario.scenarioID}</b>\n{appScenario.header}", "Delete app scenario", "Back");
            ContentUI.ButtonEvents[0].AddListener(() =>
            {
                DevToolsManager.Instance.DeleteAppScenario(appScenario.scenarioID);
                DevEditorUIManager.ShowAppEditorUI();
            });
            ContentUI.ButtonEvents[1].AddListener(DevEditorUIManager.ShowAppEditorUI);
            ContentUI.DefaultButtonIndex = 1;
        }

        private void DeleteAppScenarioStage()
        {
            ContentUI.SetupContentUI("Delete app scenario stage?", null, $"<b>{appScenarioStage.stageID}</b>", "Delete app scenario stage", "Back");
            ContentUI.ButtonEvents[0].AddListener(() =>
            {
                DevToolsManager.Instance.DeleteAppScenarioStage(appScenario.scenarioID, appScenarioStage.stageID);
                DevEditorUIManager.ShowAppScenarioEditorUI();
            });
            ContentUI.ButtonEvents[1].AddListener(DevEditorUIManager.ShowAppScenarioEditorUI);
            ContentUI.DefaultButtonIndex = 1;
        }

        private enum ELEMENT_TYPE
        {
            None,
            Unit,
            Quest,
            Task,
            Command,
            POI,
            Mail,
            App,
            AppScenario,
            AppScenarioStage
        }
    }
}
