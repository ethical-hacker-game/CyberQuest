using GIGA.AutoRadialLayout;
using Newtonsoft.Json.Bson;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DevTools
{
    public class QuestsTreeEditorUI : UI_Screen
    {
        public static QuestsTreeEditorUI Instance => DevEditorUIManager.Instance.QuestsTreeEditorUI;

        [Header("Quest Tree")]
        [SerializeField] private RectTransform questTreeArea;
        [SerializeField] private RadialLayout radialLayout;

        [Header("Quest Tree Settings")]
        [SerializeField] private Transform questTreeSettingsPanel;
        [SerializeField] private TMP_InputField inputSizeX;
        [SerializeField] private TMP_InputField inputSizeY;
        [SerializeField] private TMP_InputField inputOffset;

        [Header("Quest Settings")]
        [SerializeField] private Transform questSettingsPanel;
        [SerializeField] private TextMeshProUGUI labelQuestName;
        [SerializeField] private TextMeshProUGUI labelQuestID;
        [SerializeField] private TextMeshProUGUI labelRequiredQuestID;
        [SerializeField] private TextMeshProUGUI labelFanOffset;
        [SerializeField] private Slider sliderFanOffset;
        [SerializeField] private Toggle toggleOvverideFanSpan;
        [SerializeField] private TextMeshProUGUI labelFanSpan;
        [SerializeField] private Slider sliderFanSpan;

        private Unit ActiveUnit => DevToolsManager.ActiveUnit;
        private Quest ActiveQuest => DevToolsManager.ActiveQuest;

        public override void Back()
        {
            DevEditorUIManager.ShowUnitEditorUI();
            base.Back();
        }

        public override void ShowScreen()
        {
            base.ShowScreen();
            inputSizeX.text = ActiveUnit.questsAreaSize.x.ToString();
            inputSizeY.text = ActiveUnit.questsAreaSize.y.ToString();
            inputOffset.text = ActiveUnit.questTreeOffset.ToString();

            if(ActiveQuest == null)
            {
                questTreeSettingsPanel.gameObject.SetActiveOptimized(true);
                questSettingsPanel.gameObject.SetActiveOptimized(false);
            }
            else
            {
                ShowQuestDetails(ActiveQuest.questID);
            }

            RebuildQuestTree();
        }

        public void ShowQuestDetails(string questID)
        {
            questTreeSettingsPanel.gameObject.SetActiveOptimized(false);
            questSettingsPanel.gameObject.SetActiveOptimized(true);

            DevToolsManager.Instance.SetActvieQuest(questID);

            labelQuestName.text = ActiveQuest.GetQuestName();
            labelQuestID.text = ActiveQuest.questID;
            labelRequiredQuestID.text = "";
            LoadQuestUIValues();
            UpdateQuestUI();
        }

        public void SaveQuest()
        {
            ActiveQuest.fanOffset = sliderFanOffset.value;
            ActiveQuest.overrideFanSpan = toggleOvverideFanSpan.isOn;

            if(ActiveQuest.overrideFanSpan)
                ActiveQuest.fanSpan = sliderFanSpan.value;

            DevContentUtilities.SaveQuest(ActiveQuest, ActiveUnit.unitID);
            UpdateQuestUI();
            RebuildQuestTree();
        }

        public void SaveQuestAndBack()
        {
            SaveQuest();
            DevToolsManager.Instance.SetActvieQuest("null");

            questTreeSettingsPanel.gameObject.SetActiveOptimized(true);
            questSettingsPanel.gameObject.SetActiveOptimized(false);
        }

        public void ChangeRequiredQuest()
        {
            TextSelectionUI.SetupTextSelectionUI(ActiveUnit.questIds, "");
            TextSelectionUI.BackButtonEvent.AddListener(DevEditorUIManager.ShowQuestsTreeEditorUI);
            TextSelectionUI.SelectStringEvent.AddListener((selectedId) =>
            {
                if (selectedId != ActiveQuest.questID)
                {
                    string activeQuestId = ActiveQuest.questID;
                    int insertIndex = 0;

                    ActiveUnit.questIds.Remove(activeQuestId);

                    if (ActiveUnit.questIds.Contains(selectedId))
                        insertIndex = ActiveUnit.questIds.IndexOf(selectedId) + 1;

                    ActiveUnit.questIds.Insert(insertIndex, activeQuestId);
                }

                //DevContentUtilities.SaveQuest(ActiveQuest, ActiveUnit.unitID);
                DevContentUtilities.SaveUnit(ActiveUnit);
                DevEditorUIManager.ShowQuestsTreeEditorUI();
            });
        }

        public void SaveUnitAndRefresh()
        {
            ActiveUnit.questsAreaSize = new Vector2(int.Parse(inputSizeX.text), int.Parse(inputSizeY.text));
            ActiveUnit.questTreeOffset = int.Parse(inputOffset.text);
            DevContentUtilities.SaveUnit(ActiveUnit);

            RebuildQuestTree();
        }

        public void RebuildQuestTree()
        {
            questTreeArea.sizeDelta = ActiveUnit.questsAreaSize;

            radialLayout.GetComponent<RectTransform>().anchoredPosition = 
                new Vector2(ActiveUnit.questTreeOffset, 
                radialLayout.transform.localPosition.y);

            QuestsTreeBuilder.PrepareQuestView(radialLayout, ActiveUnit.questIds, true);
        }

        public void UpdateQuestUI()
        {
            labelFanOffset.text = $"Fan offset: {sliderFanOffset.value}";
            labelFanSpan.text = $"Fan span: {sliderFanSpan.value}";
        }

        private void LoadQuestUIValues()
        {
            sliderFanOffset.SetValueWithoutNotify(ActiveQuest.fanOffset);
            sliderFanSpan.SetValueWithoutNotify(ActiveQuest.fanSpan);
            toggleOvverideFanSpan.isOn = ActiveQuest.overrideFanSpan;
        }
    }
}
