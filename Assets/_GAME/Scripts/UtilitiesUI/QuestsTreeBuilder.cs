using DevTools;
using GIGA.AutoRadialLayout;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class QuestsTreeBuilder : MonoBehaviour
{
    public static List<QuestNode_Visuals> PrepareQuestView(RadialLayout radialLayout, List<string> questIDs, bool isEditor)
    {
        DeleteAllNodes(radialLayout);

        //RadialLayoutNode previousNode = null;

        //for (int i = 0; i < questIDs.Count; i++)
        //{
        //    int tempI = i;
        //    string newID = questIDs[i];

        //    RadialLayoutNode newNode = radialLayout.AddNode(newID, previousNode).GetComponent<RadialLayoutNode>();
        //    newNode.transform.Find("Icon").GetComponent<Image>().sprite = QuestManager.GetQuestByID(newID).GetIcon();

        //    Button newButton = newNode.GetComponent<Button>();
        //}

        if (isEditor == false)
            questIDs = GetStartedQuests(questIDs);

        List<string> questToAddAsNodes = new List<string>(questIDs);
        List<QuestNode_Visuals> node_Visuals = new List<QuestNode_Visuals>();
        RadialLayoutNode previousNode = null;
        QuestNode_Visuals previousNodeVisuals = null;

        for (int x = 0; x < questIDs.Count; x++)
        {
            string newID = questToAddAsNodes[x];
            Quest newQuest = QuestManager.GetQuestByID(newID);

            RadialLayoutNode newNode = radialLayout.AddNode(newID, previousNode).GetComponent<RadialLayoutNode>();
            Quest quest = QuestManager.GetQuestByID(newID);
            Sprite questIcon = quest.GetIcon();
            QuestNode_Visuals newNodeVisuals = newNode.GetComponent<QuestNode_Visuals>();

            if (quest.decisionQuest)
            {
                int decisionQuestCount = quest.GetDecisionsCount() - 1;

                if (isEditor == false)
                {
                    QuestInfo questInfo = PlayerController.Instance.GetQuestInfoByID(newID);

                    if (questInfo == null)
                        decisionQuestCount++;
                    else if (questInfo.IsDone == false)
                        decisionQuestCount++;
                }

                for (int d = 0; d < decisionQuestCount; d++)
                {
                    RadialLayoutNode newPathNode = radialLayout.AddNode($"{newID}_{d}", newNode).GetComponent<RadialLayoutNode>();
                    QuestNode_Visuals newPathNodeVisuals = newPathNode.GetComponent<QuestNode_Visuals>();
                    newPathNodeVisuals.SetupAlternativePlotBg();
                }
            }

            newNodeVisuals.SetupNode(questIcon, newID, isEditor, previousNodeVisuals);
            node_Visuals.Add(newNodeVisuals);

            //newNode.fanOffset = newQuest.fanOffset;
            newNode.overrideFanSpan = newQuest.overrideFanSpan;

            if (newNode.overrideFanSpan)
                newNode.fanSpanOverride = newQuest.fanSpan;

            Button newButton = newNode.GetComponent<Button>();
            newButton.onClick.AddListener(() =>
            {
                if (isEditor)
                {
                    QuestsTreeEditorUI.Instance.ShowQuestDetails(newID);
                }
                else
                {
                    QuestsUI.Instance.ShowQuestDetails(newNodeVisuals);
                }

                CommonUISolver.SelectInvisibleButton();
            });

            previousNode = newNode;
            previousNodeVisuals = newNodeVisuals;
        }

        if (isEditor == false)
            ReorderDecisionNodes(node_Visuals);

        radialLayout.Rebuild();

        FixRotations(radialLayout, node_Visuals);

        radialLayout.Rebuild();

        if (questToAddAsNodes.Count > 0)
            Debug.Log("Some quest couldnt be attached:" + string.Join("\n", questToAddAsNodes));

        return node_Visuals;
    }

    private static void ReorderDecisionNodes(List<QuestNode_Visuals> node_Visuals)
    {
        for (int i = 0; i < node_Visuals.Count; i++)
        {
            RadialLayoutNode node = node_Visuals[i].LayoutNode;
            QuestNode_Visuals parentNodeVisuals = node_Visuals[i].ParentNodeVisuals;

            if (parentNodeVisuals == null)
                continue;

            Quest parentQuest = QuestManager.GetQuestByID(parentNodeVisuals.QuestID);

            if (parentQuest == null)
                continue;

            if (parentQuest.decisionQuest == false)
                continue;

            QuestInfo decision = PlayerController.Instance.GetQuestInfoByID(parentQuest.questID);

            if (decision == null)
                continue;

            if (decision.IsDone == false)
                continue;

            node.transform.SetSiblingIndex(node.GetSiblingsCount() - decision.questDecision);
        }
    }

    private static void FixRotations(RadialLayout radialLayout, List<QuestNode_Visuals> node_Visuals)
    {
        for (int i = 0; i < node_Visuals.Count; i++)
        {
            RadialLayoutNode node = node_Visuals[i].LayoutNode;

            if (node.ParentNode == null)
                continue;

            List<RadialLayoutNode> siblingNodes = node.ParentNode.GetChildNodes().ToList();

            if (siblingNodes.Count <= 1)
                continue;

            Vector2 nodePositon = node.transform.localPosition;

            if (Mathf.Abs(nodePositon.y) < 5)
                continue;

            float angle = Mathf.Atan2(nodePositon.y, nodePositon.x) * Mathf.Rad2Deg;

            node.fanOffset = -angle;
            radialLayout.Rebuild();
        }
    }

    private static void DeleteAllNodes(RadialLayout radialLayout)
    {
        if (radialLayout.Nodes == null)
            return;

        if (radialLayout.Nodes.Count == 0)
            return;

        GameObject garbage = new GameObject("TempGarbage");
        List<RadialLayoutNode> nodesToDelete = new List<RadialLayoutNode>(radialLayout.Nodes);

        for (int i = 0; i < nodesToDelete.Count; i++)
        {
            nodesToDelete[i].transform.SetParent(garbage.transform);
        }

        foreach (RadialLayoutNode node in nodesToDelete)
        {
            if (node != null)
                radialLayout.DeleteNode(node, true);
        }

        garbage.Destroy();
    }

    private static List<string> GetStartedQuests(List<string> questIDs)
    {
        List<string> startedQuests = new List<string>();
        foreach (string questID in questIDs)
        {
            QuestInfo questInfo = PlayerController.Instance.GetQuestInfoByID(questID);
            
            if (questInfo != null)
                startedQuests.Add(questID);
        }
        return startedQuests;
    }
}
