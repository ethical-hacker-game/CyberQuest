using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Minigames
{
    /// <summary>
    /// The interactive surface for <see cref="FirewallMinigame"/>. Owns the traffic
    /// panel and the editable rule list, and drives everything through the minigame's
    /// public actions. All gameplay is UI-driven (no world-space objects), so this is
    /// the closest sibling to <see cref="Waves.WavesMinigameUI"/>.
    /// </summary>
    public class FirewallMinigameUI : MinigameUI
    {
        [Header("Traffic panel")]
        [SerializeField] private RectTransform trafficContent;
        [SerializeField] private FirewallMinigameUI_TrafficRow trafficRowPrefab;

        [Header("Rule list")]
        [SerializeField] private RectTransform rulesContent;
        [SerializeField] private FirewallMinigameUI_RuleRow ruleRowPrefab;

        [Header("Controls")]
        [SerializeField] private Button addRuleButton;
        [SerializeField] private Button testButton;
        [SerializeField] private TextMeshProUGUI statusTMP;
        [SerializeField] private TextMeshProUGUI defaultPolicyTMP;

        private readonly List<FirewallMinigameUI_TrafficRow> trafficRows = new List<FirewallMinigameUI_TrafficRow>();
        private readonly List<FirewallMinigameUI_RuleRow> ruleRows = new List<FirewallMinigameUI_RuleRow>();

        private List<string> sourceOptionLabels = new List<string>();
        private List<string> portOptionLabels = new List<string>();

        private FirewallMinigame Minigame => FirewallMinigame.MinigameInstance;
        private bool initialised;

        private void Start()
        {
            if (Minigame == null)
                return;

            CacheOptionLabels();
            BuildTrafficRows();

            if (addRuleButton != null)
                addRuleButton.onClick.AddListener(OnAddRule);
            if (testButton != null)
                testButton.onClick.AddListener(OnTest);

            if (defaultPolicyTMP != null)
                defaultPolicyTMP.text = $"No match → {Minigame.DefaultPolicy}";

            initialised = true;

            RebuildRuleRows();
            UpdateStatus();
        }

        private void CacheOptionLabels()
        {
            sourceOptionLabels = new List<string>();
            foreach (FirewallSourceOption option in Minigame.SourcePool)
                sourceOptionLabels.Add(option.Label);

            portOptionLabels = new List<string>();
            foreach (FirewallPortOption option in Minigame.PortPool)
                portOptionLabels.Add(option.Label);
        }

        private void BuildTrafficRows()
        {
            ClearRows(trafficRows);

            if (trafficRowPrefab == null || trafficContent == null)
                return;

            foreach (FirewallTrafficEntry entry in Minigame.Traffic)
            {
                FirewallMinigameUI_TrafficRow row = Instantiate(trafficRowPrefab, trafficContent);
                row.gameObject.SetActive(true);
                row.Setup(entry);
                trafficRows.Add(row);
            }
        }

        private void OnAddRule()
        {
            if (Minigame.AddRule() == null)
                return;

            OnRulesetEdited();
        }

        private void OnMoveRule(FirewallRule rule, int direction)
        {
            Minigame.MoveRule(rule, direction);
            OnRulesetEdited();
        }

        private void OnDeleteRule(FirewallRule rule)
        {
            Minigame.RemoveRule(rule);
            OnRulesetEdited();
        }

        private void OnRulesetEdited()
        {
            // The old verdicts no longer reflect the current rules.
            Minigame.InvalidateEvaluation();
            RebuildRuleRows();
            ClearVerdicts();
            UpdateStatus();
        }

        private void OnTest()
        {
            FirewallEvaluation eval = Minigame.TestRuleset();
            PaintVerdicts(eval);
            UpdateStatus();
        }

        private void RebuildRuleRows()
        {
            ClearRows(ruleRows);

            if (ruleRowPrefab == null || rulesContent == null)
                return;

            IReadOnlyList<FirewallRule> rules = Minigame.Rules;
            for (int i = 0; i < rules.Count; i++)
            {
                FirewallMinigameUI_RuleRow row = Instantiate(ruleRowPrefab, rulesContent);
                row.gameObject.SetActive(true);
                row.Setup(rules[i], i, sourceOptionLabels, portOptionLabels, OnMoveRule, OnDeleteRule);
                ruleRows.Add(row);
            }
        }

        private void PaintVerdicts(FirewallEvaluation eval)
        {
            if (eval == null)
                return;

            for (int i = 0; i < trafficRows.Count && i < eval.Decisions.Count; i++)
                trafficRows[i].SetVerdict(eval.Decisions[i], eval.Correctness[i]);
        }

        private void ClearVerdicts()
        {
            foreach (FirewallMinigameUI_TrafficRow row in trafficRows)
                row.ClearVerdict();
        }

        private void UpdateStatus()
        {
            if (addRuleButton != null)
                addRuleButton.interactable = Minigame.CanAddRule;

            if (statusTMP == null)
                return;

            int total = Minigame.Traffic.Count;
            int rulesUsed = Minigame.Rules.Count;

            if (Minigame.LastEvaluation == null)
            {
                // No (valid) test yet — tell the player what the goal actually is.
                statusTMP.text = $"Rules: {rulesUsed}/{Minigame.RuleBudget}   •   Press TEST — you win when all {total} requests are handled correctly";
            }
            else
            {
                statusTMP.text = $"Correct: {Minigame.LastEvaluation.Correct}/{total}   •   Rules: {rulesUsed}/{Minigame.RuleBudget}";
            }
        }

        private static void ClearRows<T>(List<T> rows) where T : Component
        {
            foreach (T row in rows)
            {
                if (row != null)
                    Destroy(row.gameObject);
            }

            rows.Clear();
        }

        public override void UpdateUI()
        {
            // Called once on the source prefab before the live copy exists, and it is
            // safe to no-op until this instance has run its own Start.
            if (initialised)
                UpdateStatus();
        }
    }
}
