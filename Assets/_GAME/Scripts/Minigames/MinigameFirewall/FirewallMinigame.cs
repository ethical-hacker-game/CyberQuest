using System.Collections.Generic;
using UnityEngine;

namespace Minigames
{
    /// <summary>
    /// "Firewall Ruleset" &mdash; a deliberate, turn-based puzzle. The player builds an
    /// ordered allow/deny ruleset that must let every benign request through and block
    /// every malicious one, within a rule budget. Traffic is evaluated top-down (first
    /// matching rule wins) with a configurable fall-through policy, exactly like a real
    /// firewall. Nothing here runs on Update &mdash; the game only reacts to the player
    /// editing rules and pressing "Test", so it rewards reasoning over reflexes.
    /// </summary>
    public class FirewallMinigame : MinigameBehaviour
    {
        private static FirewallMinigame minigameInstance;

        [Header("Firewall Settings")]
        [SerializeField, Tooltip("Default connection requests. A task can override these through FirewallMinigameSettings.")]
        private List<FirewallTrafficEntry> defaultTraffic = new List<FirewallTrafficEntry>();
        [SerializeField, Tooltip("Maximum number of rules the player may create.")]
        private int ruleBudget = 5;
        [SerializeField, Tooltip("Action applied to a request that no rule matches. Default-deny is the recommended lesson.")]
        private FirewallRuleAction defaultPolicy = FirewallRuleAction.Deny;

        [Header("Scoring")]
        [SerializeField] private int pointsPerCorrect = 100;
        [SerializeField, Tooltip("Bonus per second left on the clock when the ruleset is solved.")]
        private int timeBonusPerSecond = 5;
        [SerializeField, Tooltip("Bonus for each unused rule slot when the ruleset is solved (rewards concise rules).")]
        private int ruleEfficiencyBonus = 60;

        private List<FirewallTrafficEntry> traffic = new List<FirewallTrafficEntry>();
        private readonly List<FirewallRule> rules = new List<FirewallRule>();
        private readonly List<FirewallSourceOption> sourcePool = new List<FirewallSourceOption>();
        private readonly List<FirewallPortOption> portPool = new List<FirewallPortOption>();

        private FirewallEvaluation lastEvaluation;

        public static FirewallMinigame MinigameInstance
        {
            get
            {
                if (minigameInstance == null)
                    Debug.LogError("minigameInstance does not exist");

                return minigameInstance;
            }
        }

        public IReadOnlyList<FirewallTrafficEntry> Traffic => traffic;
        public IReadOnlyList<FirewallRule> Rules => rules;
        public IReadOnlyList<FirewallSourceOption> SourcePool => sourcePool;
        public IReadOnlyList<FirewallPortOption> PortPool => portPool;
        public int RuleBudget => ruleBudget;
        public FirewallRuleAction DefaultPolicy => defaultPolicy;
        public FirewallEvaluation LastEvaluation => lastEvaluation;
        public bool CanAddRule => rules.Count < ruleBudget;

        public override string Summary => BuildSummary();

        private void Awake()
        {
            minigameInstance = this;
            traffic = new List<FirewallTrafficEntry>(defaultTraffic);

            // Build the dropdown pools from the prefab traffic right away so the game
            // is playable even on paths that never call ApplyConfig (SetupMinigameTest).
            // ApplyConfig rebuilds them after task overrides; the build is idempotent.
            BuildOptionPools();
        }

        private void OnDestroy()
        {
            if (minigameInstance == this)
                minigameInstance = null;
        }

        public override void ApplyConfig(TaskMinigameVariant config)
        {
            // Runs after Awake (traffic already seeded from the prefab) but before Start
            // and before the UI is instantiated, so overriding the traffic set and then
            // rebuilding the option pools here is safe.
            base.ApplyConfig(config);

            if (config != null && config.firewallSettings != null)
            {
                FirewallMinigameSettings s = config.firewallSettings;

                if (s.traffic != null && s.traffic.Count > 0)
                    traffic = new List<FirewallTrafficEntry>(s.traffic);

                if (s.ruleBudget > 0)
                    ruleBudget = s.ruleBudget;

                if (s.overrideDefaultPolicy)
                    defaultPolicy = s.defaultPolicy;
            }

            ApplyDifficulty();
            BuildOptionPools();
        }

        private void ApplyDifficulty()
        {
            if (Difficulty == MinigameDifficulty.Default)
                return;

            // The rule budget is the tension knob: a tighter budget forces the player to
            // generalise with wildcards / ranges instead of one rule per request.
            float budgetMultiplier;

            switch (Difficulty)
            {
                case MinigameDifficulty.Easy:
                    budgetMultiplier = 1.5f;
                    break;
                case MinigameDifficulty.Hard:
                    budgetMultiplier = 0.7f;
                    break;
                default: // Normal
                    budgetMultiplier = 1f;
                    break;
            }

            ruleBudget = Mathf.Max(1, Mathf.RoundToInt(ruleBudget * budgetMultiplier));
        }

        /// <summary>
        /// Derives the source / port dropdown pools from the active traffic set so the
        /// player only ever picks conditions that are meaningful for this scenario.
        /// </summary>
        private void BuildOptionPools()
        {
            sourcePool.Clear();
            portPool.Clear();

            sourcePool.Add(new FirewallSourceOption("Any source", "any"));

            List<string> exactIps = new List<string>();
            List<string> wildcards = new List<string>();

            foreach (FirewallTrafficEntry entry in traffic)
            {
                if (entry == null || string.IsNullOrEmpty(entry.sourceIp))
                    continue;

                if (exactIps.Contains(entry.sourceIp) == false)
                    exactIps.Add(entry.sourceIp);

                string wildcard = ToSlash24Wildcard(entry.sourceIp);
                if (wildcard != null && wildcards.Contains(wildcard) == false)
                    wildcards.Add(wildcard);
            }

            foreach (string wildcard in wildcards)
                sourcePool.Add(new FirewallSourceOption(wildcard, wildcard));

            foreach (string ip in exactIps)
                sourcePool.Add(new FirewallSourceOption(ip, ip));

            portPool.Add(FirewallPortOption.Any("Any port"));

            List<int> exactPorts = new List<int>();
            foreach (FirewallTrafficEntry entry in traffic)
            {
                if (entry == null)
                    continue;

                if (exactPorts.Contains(entry.port) == false)
                    exactPorts.Add(entry.port);
            }
            exactPorts.Sort();

            foreach (int port in exactPorts)
                portPool.Add(FirewallPortOption.Exact(port.ToString(), port));

            portPool.Add(FirewallPortOption.Range("System (1-1023)", 1, 1023));
            portPool.Add(FirewallPortOption.Range("Ephemeral (1024-65535)", 1024, 65535));
        }

        private static string ToSlash24Wildcard(string ip)
        {
            if (string.IsNullOrEmpty(ip))
                return null;

            string[] octets = ip.Split('.');
            if (octets.Length != 4)
                return null;

            return $"{octets[0]}.{octets[1]}.{octets[2]}.*";
        }

        // --- Player actions (called from the UI) ---

        public FirewallRule AddRule()
        {
            if (CanAddRule == false)
                return null;

            FirewallRule rule = new FirewallRule();
            rules.Add(rule);
            return rule;
        }

        public void RemoveRule(FirewallRule rule)
        {
            rules.Remove(rule);
        }

        /// <summary>
        /// Drops the last evaluation. Called by the UI whenever the player edits the
        /// ruleset, so the status line goes back to the "press Test" hint instead of
        /// showing a result that no longer reflects the current rules.
        /// </summary>
        public void InvalidateEvaluation()
        {
            lastEvaluation = null;
        }

        public void MoveRule(FirewallRule rule, int direction)
        {
            int index = rules.IndexOf(rule);
            if (index < 0)
                return;

            int target = index + direction;
            if (target < 0 || target >= rules.Count)
                return;

            rules.RemoveAt(index);
            rules.Insert(target, rule);
        }

        /// <summary>
        /// Evaluates the current ruleset against every request, updates the score and,
        /// when everything is correctly classified, finishes the minigame as a win.
        /// Returns the evaluation so the UI can paint per-request verdicts.
        /// </summary>
        public FirewallEvaluation TestRuleset()
        {
            FirewallEvaluation eval = Evaluate();
            lastEvaluation = eval;

            int baseScore = eval.Correct * pointsPerCorrect;

            if (eval.IsPerfect)
            {
                int timeBonus = Mathf.RoundToInt(MinigamesController.GetTimerPositive) * timeBonusPerSecond;
                int efficiencyBonus = Mathf.Max(0, ruleBudget - rules.Count) * ruleEfficiencyBonus;

                SetScore(baseScore + timeBonus + efficiencyBonus);
                FinishMinigame(true, Score, Summary);
            }
            else
            {
                SetScore(baseScore);
            }

            return eval;
        }

        private FirewallEvaluation Evaluate()
        {
            FirewallEvaluation eval = new FirewallEvaluation { Total = traffic.Count };

            foreach (FirewallTrafficEntry entry in traffic)
            {
                bool allowed = ResolveDecision(entry);
                bool correct = allowed == entry.shouldAllow;

                eval.Decisions.Add(allowed);
                eval.Correctness.Add(correct);

                if (correct)
                    eval.Correct++;
            }

            return eval;
        }

        private bool ResolveDecision(FirewallTrafficEntry entry)
        {
            foreach (FirewallRule rule in rules)
            {
                if (RuleMatches(rule, entry))
                    return rule.action == FirewallRuleAction.Allow;
            }

            return defaultPolicy == FirewallRuleAction.Allow;
        }

        private bool RuleMatches(FirewallRule rule, FirewallTrafficEntry entry)
        {
            if (sourcePool.Count == 0 || portPool.Count == 0)
                return false;

            FirewallSourceOption source = sourcePool[Mathf.Clamp(rule.sourceIndex, 0, sourcePool.Count - 1)];
            FirewallPortOption port = portPool[Mathf.Clamp(rule.portIndex, 0, portPool.Count - 1)];

            bool protocolOk = rule.protocol == FirewallProtocol.Any || rule.protocol == entry.protocol;

            return source.Matches(entry.sourceIp) && port.Matches(entry.port) && protocolOk;
        }

        private void SetScore(int target)
        {
            ChangeScore(target - Score);
        }

        private string BuildSummary()
        {
            if (traffic == null || traffic.Count == 0)
                return string.Empty;

            string okColor = ColorUtility.ToHtmlStringRGB(new Color(0.42f, 0.85f, 0.45f));
            string badColor = ColorUtility.ToHtmlStringRGB(new Color(0.90f, 0.36f, 0.36f));

            string summary = "<b>How your ruleset handled the traffic:</b>";

            for (int i = 0; i < traffic.Count; i++)
            {
                FirewallTrafficEntry entry = traffic[i];
                bool hasVerdict = lastEvaluation != null && i < lastEvaluation.Correctness.Count;
                bool correct = hasVerdict && lastEvaluation.Correctness[i];

                string expected = entry.shouldAllow ? "ALLOW" : "BLOCK";
                string mark = hasVerdict ? (correct ? "OK" : "MISS") : "-";
                string color = correct ? okColor : badColor;

                summary += $"\n<color=#{color}><b>[{mark}]</b></color> {entry.label} " +
                           $"<i>({entry.Details})</i> -> expected {expected}";

                if (string.IsNullOrEmpty(entry.rationale) == false)
                    summary += $"\n    <size=80%>{entry.rationale}</size>";
            }

            summary += $"\n\n<size=85%>Default policy for unmatched traffic: " +
                       $"<b>{defaultPolicy}</b>. Order matters - the first matching rule wins.</size>";

            return summary;
        }
    }
}
