using System.Collections.Generic;
using UnityEngine;

namespace Minigames
{
    /// <summary>
    /// "Trace the Intruder" &mdash; a deliberate topology puzzle against a live intruder.
    /// The player reconstructs an attacker's lateral-movement path across a network graph
    /// (from the compromised <b>Entry</b> host to the crown-jewel <b>Target</b>), moving
    /// only along links the evidence shows are pivotable, and then <b>cuts</b> a link that
    /// isolates the target &mdash; but the intruder keeps advancing along their route:
    /// every committed player move (hop attempt, backtrack, wrong cut) ticks an action
    /// clock, and every <see cref="attackerStepEvery"/> ticks the intruder hops one node
    /// closer. If they reach the target first, the game is lost, and a cut is only valid
    /// if it isolates the target from the intruder's CURRENT position &mdash; so the set
    /// of winning cuts shrinks while the player hesitates. Inspecting link evidence is
    /// free; guessing is not. Everything is turn-based &mdash; nothing runs on Update &mdash;
    /// so the game rewards reading and planning over both reflexes and brute force.
    ///
    /// This is the deliberate, gradeable counterpart to the old twitch-based Network
    /// minigame, and (like Firewall) is fully UI-driven with no world-space objects.
    /// </summary>
    public class TraceMinigame : MinigameBehaviour
    {
        private static TraceMinigame minigameInstance;

        [Header("Scenario pool (one is picked at random - a task can override via TraceMinigameSettings)")]
        [SerializeField] private List<TraceScenario> scenarios = new List<TraceScenario>();
        [SerializeField, Tooltip("When true the picked graph may get mirrored horizontally / vertically, so even a repeated scenario is laid out differently.")]
        private bool randomMirror = true;

        [Header("Live intruder")]
        [SerializeField, Tooltip("The intruder advances one hop along their route for every N committed player moves (accepted hops, backtracks, wrong hops, wrong cuts). Inspecting links is free. Difficulty adjusts this by +1 (Easy) / -1 (Hard).")]
        private int attackerStepEvery = 3;

        [Header("Presentation")]
        [SerializeField, Tooltip("When true the link evidence text is shown up-front. Difficulty overrides this: Easy shows it, Normal/Hard hide it until a link is inspected.")]
        private bool evidenceVisibleByDefault = true;

        [Header("Scoring")]
        [SerializeField, Tooltip("Points awarded for each correct pivot the player adds to the path.")]
        private int pointsPerHop = 120;
        [SerializeField, Tooltip("Points removed for proposing a hop the attacker could not make (magnitude, subtracted).")]
        private int wrongHopPenalty = 40;
        [SerializeField, Tooltip("Points removed for a cut that fails to isolate the target (magnitude, subtracted).")]
        private int wrongCutPenalty = 80;
        [SerializeField, Tooltip("Bonus per second left on the clock when the target is isolated.")]
        private int timeBonusPerSecond = 6;
        [SerializeField, Tooltip("Bonus for choosing the correct containment cut.")]
        private int containmentBonus = 400;
        [SerializeField, Tooltip("Bonus per hop still separating the intruder from the target at the moment of the cut (rewards efficient play).")]
        private int bonusPerAttackerDistance = 60;
        [SerializeField, Tooltip("Extra bonus when the cut link is the author-designated optimal one (isContainmentPoint).")]
        private int optimalCutBonus = 200;

        private readonly List<TraceNode> nodes = new List<TraceNode>();
        private readonly List<TraceLink> links = new List<TraceLink>();
        private readonly Dictionary<string, TraceNode> nodesById = new Dictionary<string, TraceNode>();
        private readonly List<string> path = new List<string>();

        private TracePhase phase = TracePhase.Trace;
        private bool evidenceVisible = true;
        private float penaltyMultiplier = 1f;

        private TraceLink lastCutLink;
        private bool lastCutCorrect;

        // Which authored scenario is active this play (used by the result summary), plus
        // the previous pool pick so back-to-back plays avoid repeating a scenario. The
        // index is static on purpose: minigame instances are destroyed between plays.
        private string activeScenarioName;
        private static int lastScenarioIndex = -1;

        // Live-intruder state: the route they follow (entry -> target over traversable
        // links), how far along it they are, and the committed-move counter that drives
        // their advance. All reset by SeedGraph.
        private readonly List<string> attackerRoute = new List<string>();
        private int attackerIndex;
        private int committedMoves;
        private bool attackerReachedTarget;

        public static TraceMinigame MinigameInstance
        {
            get
            {
                if (minigameInstance == null)
                    Debug.LogError("minigameInstance does not exist");

                return minigameInstance;
            }
        }

        public IReadOnlyList<TraceNode> Nodes => nodes;
        public IReadOnlyList<TraceLink> Links => links;
        public IReadOnlyList<string> Path => path;
        public TracePhase Phase => phase;
        public bool EvidenceVisible => evidenceVisible;

        public TraceNode EntryNode { get; private set; }
        public TraceNode TargetNode { get; private set; }

        public string TipId => path.Count > 0 ? path[path.Count - 1] : null;

        /// <summary>Node the intruder currently occupies (entry at game start).</summary>
        public string AttackerNodeId => attackerIndex < attackerRoute.Count ? attackerRoute[attackerIndex] : null;

        /// <summary>How many hops still separate the intruder from the target.</summary>
        public int AttackerHopsFromTarget => Mathf.Max(0, attackerRoute.Count - 1 - attackerIndex);

        /// <summary>
        /// The intruder is only rendered once the player's traced path covers the host
        /// they are on - you can't see what you haven't reconstructed from the logs.
        /// </summary>
        public bool AttackerVisible => AttackerNodeId != null && path.Contains(AttackerNodeId);

        public override string Summary => BuildSummary();

        private void Awake()
        {
            minigameInstance = this;
            evidenceVisible = evidenceVisibleByDefault;

            // Seed from the prefab pool so the test path (SetupMinigameTest, which never
            // calls ApplyConfig) still has a working graph. ApplyConfig re-seeds on override.
            SeedFromPool(MinigameDifficulty.Default);
        }

        private void OnDestroy()
        {
            if (minigameInstance == this)
                minigameInstance = null;
        }

        public override void ApplyConfig(TaskMinigameVariant config)
        {
            // Runs after Awake (prefab pool already seeded) but before Start and before
            // the UI is instantiated, so replacing the graph and re-tuning difficulty here
            // is safe - nothing has read the topology yet.
            base.ApplyConfig(config);

            TraceMinigameSettings settings = config != null ? config.traceSettings : null;

            if (settings != null && settings.attackerStepEvery > 0)
                attackerStepEvery = settings.attackerStepEvery;

            if (settings != null && settings.HasScenarioPool)
            {
                // Task-authored pool replaces the prefab pool; the usual random pick
                // (filtered by the task's difficulty) then runs on top of it.
                scenarios = settings.scenarios;
                SeedFromPool(Difficulty);
            }
            else if (settings != null && settings.HasGraph)
            {
                // Task pins one exact graph - no scenario randomisation (the layout
                // mirror still applies; it never changes the puzzle itself).
                activeScenarioName = null;
                SeedGraph(settings.nodes, settings.links);
            }
            else if (Difficulty != MinigameDifficulty.Default)
            {
                // Re-pick so the difficulty filter can steer which scenario is used.
                SeedFromPool(Difficulty);
            }

            ApplyDifficulty();
        }

        private void ApplyDifficulty()
        {
            switch (Difficulty)
            {
                case MinigameDifficulty.Easy:
                    evidenceVisible = true;   // Read the map with the answers on show.
                    penaltyMultiplier = 0.5f;
                    attackerStepEvery += 1;   // Slower intruder.
                    break;
                case MinigameDifficulty.Hard:
                    evidenceVisible = false;  // Inspect each link deliberately.
                    penaltyMultiplier = 1.5f;
                    attackerStepEvery = Mathf.Max(1, attackerStepEvery - 1); // Faster intruder.
                    break;
                case MinigameDifficulty.Normal:
                    evidenceVisible = false;
                    penaltyMultiplier = 1f;
                    break;
                default: // Default - keep the prefab-authored presentation.
                    evidenceVisible = evidenceVisibleByDefault;
                    penaltyMultiplier = 1f;
                    break;
            }
        }

        /// <summary>
        /// Picks a random scenario from the pool - preferring ones tagged for the given
        /// difficulty and avoiding an immediate repeat when possible - and seeds the graph.
        /// </summary>
        private void SeedFromPool(MinigameDifficulty filter)
        {
            List<int> eligible = new List<int>();

            for (int i = 0; i < scenarios.Count; i++)
            {
                TraceScenario scenario = scenarios[i];

                if (scenario == null || scenario.HasGraph == false)
                    continue;

                if (filter == MinigameDifficulty.Default
                    || scenario.recommendedDifficulty == MinigameDifficulty.Default
                    || scenario.recommendedDifficulty == filter)
                {
                    eligible.Add(i);
                }
            }

            // Nothing matches the difficulty tag -> fall back to every playable scenario.
            if (eligible.Count == 0)
            {
                for (int i = 0; i < scenarios.Count; i++)
                {
                    if (scenarios[i] != null && scenarios[i].HasGraph)
                        eligible.Add(i);
                }
            }

            if (eligible.Count == 0)
            {
                Debug.LogError("[TraceMinigame] Scenario pool is empty - author at least one scenario on the prefab.");
                return;
            }

            // Don't serve the same scenario twice in a row (unless it is the only one).
            if (eligible.Count > 1)
                eligible.Remove(lastScenarioIndex);

            int pick = eligible[Random.Range(0, eligible.Count)];
            lastScenarioIndex = pick;

            activeScenarioName = scenarios[pick].scenarioName;
            SeedGraph(scenarios[pick].nodes, scenarios[pick].links);
        }

        /// <summary>
        /// Rebuilds the working graph from a nodes/links source, refreshes the id index,
        /// locates the Entry/Target hosts and resets the path back to the entry point.
        /// Idempotent - safe to call from both Awake and ApplyConfig.
        /// </summary>
        private void SeedGraph(List<TraceNode> sourceNodes, List<TraceLink> sourceLinks)
        {
            nodes.Clear();
            links.Clear();
            nodesById.Clear();

            // Deep-copy the graph so the layout mirror (and any future mutation) never
            // touches the prefab scenarios or the task-authored settings object - both
            // are shared and must survive this play unchanged.
            if (sourceNodes != null)
            {
                foreach (TraceNode node in sourceNodes)
                {
                    if (node != null)
                        nodes.Add(CloneNode(node));
                }
            }

            if (sourceLinks != null)
            {
                foreach (TraceLink link in sourceLinks)
                {
                    if (link != null)
                        links.Add(CloneLink(link));
                }
            }

            ApplyRandomMirror();

            EntryNode = null;
            TargetNode = null;

            foreach (TraceNode node in nodes)
            {
                if (node == null || string.IsNullOrEmpty(node.id))
                    continue;

                if (nodesById.ContainsKey(node.id) == false)
                    nodesById.Add(node.id, node);

                if (node.role == TraceNodeRole.Entry && EntryNode == null)
                    EntryNode = node;
                else if (node.role == TraceNodeRole.Target && TargetNode == null)
                    TargetNode = node;
            }

            ResetPath();
            ComputeAttackerRoute();
            WarnIfUnsolvable();
        }

        /// <summary>
        /// Precomputes the intruder's route: the shortest traversable path from the entry
        /// to the target (BFS parent chain). Also resets the live-intruder state.
        /// </summary>
        private void ComputeAttackerRoute()
        {
            attackerRoute.Clear();
            attackerIndex = 0;
            committedMoves = 0;
            attackerReachedTarget = false;

            if (EntryNode == null || TargetNode == null)
                return;

            Dictionary<string, string> parent = new Dictionary<string, string> { { EntryNode.id, null } };
            Queue<string> frontier = new Queue<string>();
            frontier.Enqueue(EntryNode.id);

            while (frontier.Count > 0)
            {
                string current = frontier.Dequeue();

                if (current == TargetNode.id)
                    break;

                foreach (TraceLink link in links)
                {
                    if (link.attackerCanTraverse == false)
                        continue;

                    string other = link.Other(current);
                    if (other == null || parent.ContainsKey(other))
                        continue;

                    parent.Add(other, current);
                    frontier.Enqueue(other);
                }
            }

            if (parent.ContainsKey(TargetNode.id) == false)
                return; // Unsolvable graph - WarnIfUnsolvable reports it.

            for (string step = TargetNode.id; step != null; step = parent[step])
                attackerRoute.Insert(0, step);
        }

        private static TraceNode CloneNode(TraceNode source) => new TraceNode
        {
            id = source.id,
            label = source.label,
            role = source.role,
            detail = source.detail,
            position = source.position,
        };

        private static TraceLink CloneLink(TraceLink source) => new TraceLink
        {
            fromId = source.fromId,
            toId = source.toId,
            attackerCanTraverse = source.attackerCanTraverse,
            evidence = source.evidence,
            isContainmentPoint = source.isContainmentPoint,
        };

        /// <summary>
        /// Randomly flips the layout horizontally and/or vertically. Pure presentation -
        /// the topology (and therefore the puzzle) is untouched, but a repeated scenario
        /// stops looking like a memorised picture.
        /// </summary>
        private void ApplyRandomMirror()
        {
            if (randomMirror == false)
                return;

            bool mirrorX = Random.value < 0.5f;
            bool mirrorY = Random.value < 0.5f;

            if (mirrorX == false && mirrorY == false)
                return;

            foreach (TraceNode node in nodes)
            {
                if (mirrorX)
                    node.position.x = 1f - node.position.x;
                if (mirrorY)
                    node.position.y = 1f - node.position.y;
            }
        }

        private void WarnIfUnsolvable()
        {
            if (EntryNode == null || TargetNode == null)
            {
                Debug.LogError("[TraceMinigame] Graph needs exactly one Entry and one Target node.");
                return;
            }

            if (TargetReachableFrom(EntryNode.id, null) == false)
                Debug.LogError("[TraceMinigame] No traversable Entry->Target path exists - the puzzle is unsolvable. Check attackerCanTraverse flags.");
        }

        // --- Queries used by the UI ---

        public TraceNode GetNode(string id)
        {
            if (id != null && nodesById.TryGetValue(id, out TraceNode node))
                return node;

            return null;
        }

        public bool IsInPath(string id) => path.Contains(id);

        public bool IsTip(string id) => TipId == id;

        /// <summary>Returns the link joining two node ids, in either direction, or null.</summary>
        public TraceLink GetLink(string a, string b)
        {
            foreach (TraceLink link in links)
            {
                if (link.Connects(a, b))
                    return link;
            }

            return null;
        }

        /// <summary>True when the link's two endpoints are adjacent in the current path (i.e. it is on the traced route).</summary>
        public bool IsLinkOnPath(TraceLink link)
        {
            for (int i = 0; i < path.Count - 1; i++)
            {
                if (link.Connects(path[i], path[i + 1]))
                    return true;
            }

            return false;
        }

        // --- Player actions (called from the UI) ---

        public void ResetPath()
        {
            path.Clear();
            phase = TracePhase.Trace;
            lastCutLink = null;

            if (EntryNode != null)
                path.Add(EntryNode.id);
        }

        /// <summary>
        /// Attempts to grow (or undo) the traced path with the clicked node. Returns a
        /// result describing what happened plus a short message the UI can surface.
        /// </summary>
        public TraceSelectResult TrySelectNode(string id)
        {
            if (phase != TracePhase.Trace)
                return Result(TraceSelectOutcome.Ignored, "The path is already traced - now choose a link to cut.");

            if (path.Count == 0 || EntryNode == null)
                return Result(TraceSelectOutcome.Ignored, "No entry point defined.");

            string tip = TipId;

            if (id == tip)
                return Result(TraceSelectOutcome.RejectedAlreadyTip, string.Empty);

            // Clicking a node already in the path rewinds the route back to it.
            // Backtracking is a committed move - undoing waste is not free.
            if (IsInPath(id))
            {
                int index = path.IndexOf(id);
                path.RemoveRange(index + 1, path.Count - index - 1);
                return Result(TraceSelectOutcome.Truncated, $"Backtracked to {LabelOf(id)}." + RegisterMove());
            }

            TraceLink link = GetLink(tip, id);

            // A click with no link at all is a plain misclick (the links are visible on
            // screen, so it reveals nothing) - warn for free, don't tick the clock.
            if (link == null)
                return Result(TraceSelectOutcome.RejectedNotAdjacent,
                    $"No direct link from {LabelOf(tip)} to {LabelOf(id)}.");

            if (link.attackerCanTraverse == false)
            {
                ChangeScore(-ScaledPenalty(wrongHopPenalty));
                string why = string.IsNullOrEmpty(link.evidence) ? "the attacker has no way across this link." : link.evidence;
                return Result(TraceSelectOutcome.RejectedNotPivotable, $"Dead end: {why}" + RegisterMove());
            }

            // Valid pivot.
            path.Add(id);
            ChangeScore(pointsPerHop);

            if (id == TargetNode.id)
            {
                phase = TracePhase.Contain;
                return Result(TraceSelectOutcome.PathCompleted,
                    "Path reconstructed. Now cut a link AHEAD of the intruder to isolate the target." + RegisterMove());
            }

            return Result(TraceSelectOutcome.Extended, $"Pivoted to {LabelOf(id)}." + RegisterMove());
        }

        /// <summary>
        /// Proposes a containment cut. A cut is correct when it provably isolates the
        /// target from the intruder's CURRENT position (BFS over the traversable
        /// sub-graph without that link) - a choke the intruder already passed no longer
        /// helps. On success the minigame ends; a wrong cut costs points and ticks the
        /// intruder's clock like any other committed move.
        /// </summary>
        public TraceCutResult TryCutLink(TraceLink link)
        {
            if (phase != TracePhase.Contain || link == null)
                return new TraceCutResult { handled = false };

            if (link.attackerCanTraverse == false)
            {
                ChangeScore(-ScaledPenalty(wrongCutPenalty));
                return new TraceCutResult
                {
                    handled = true,
                    correct = false,
                    message = "The attacker never used that link - cutting it changes nothing." + RegisterMove(),
                };
            }

            bool correct = IsCorrectCut(link);
            lastCutLink = link;
            lastCutCorrect = correct;

            if (correct == false)
            {
                ChangeScore(-ScaledPenalty(wrongCutPenalty));
                return new TraceCutResult
                {
                    handled = true,
                    correct = false,
                    message = "Not enough - the intruder can still reach the target from where they are now." + RegisterMove(),
                };
            }

            // Win: containment + time bonus, plus rewards for cutting the intruder off
            // early (distance bonus) and for finding the author-designated optimal link.
            int distance = AttackerHopsFromTarget;
            int timeBonus = Mathf.RoundToInt(MinigamesController.GetTimerPositive) * timeBonusPerSecond;
            int distanceBonus = distance * bonusPerAttackerDistance;
            int designatedBonus = link.isContainmentPoint ? optimalCutBonus : 0;
            ChangeScore(containmentBonus + timeBonus + distanceBonus + designatedBonus);

            phase = TracePhase.Done;
            FinishMinigame(true, Score, Summary);

            return new TraceCutResult
            {
                handled = true,
                correct = true,
                message = $"Target isolated with the intruder {HopsText(distance)} out. Contained.",
            };
        }

        /// <summary>
        /// Counts a committed player move; every <see cref="attackerStepEvery"/> moves the
        /// intruder advances one hop. Returns feedback text when they moved (empty
        /// otherwise) and finishes the game as a loss when they reach the target.
        /// </summary>
        private string RegisterMove()
        {
            if (phase == TracePhase.Done || attackerRoute.Count < 2)
                return string.Empty;

            committedMoves++;

            if (committedMoves % attackerStepEvery != 0)
                return string.Empty;

            if (attackerIndex >= attackerRoute.Count - 1)
                return string.Empty;

            attackerIndex++;

            if (attackerIndex >= attackerRoute.Count - 1)
            {
                attackerReachedTarget = true;
                phase = TracePhase.Done;
                FinishMinigame(false, Score, Summary);
                return $" The intruder reached {TargetNode.label}!";
            }

            return $" The intruder advanced - now {HopsText(AttackerHopsFromTarget)} from {TargetNode.label}.";
        }

        private bool IsCorrectCut(TraceLink link)
        {
            string start = AttackerNodeId ?? (EntryNode != null ? EntryNode.id : null);

            if (start == null)
                return false;

            // A cut wins when it is a genuine choke point AHEAD of the intruder: without
            // that link the target must be unreachable from where the intruder is now.
            return TargetReachableFrom(start, link) == false;
        }

        /// <summary>
        /// BFS from the given start node over the traversable sub-graph, optionally
        /// pretending one link has been removed. Returns whether the target is reachable.
        /// </summary>
        private bool TargetReachableFrom(string startId, TraceLink removed)
        {
            if (startId == null || TargetNode == null)
                return false;

            HashSet<string> visited = new HashSet<string> { startId };
            Queue<string> frontier = new Queue<string>();
            frontier.Enqueue(startId);

            while (frontier.Count > 0)
            {
                string current = frontier.Dequeue();

                if (current == TargetNode.id)
                    return true;

                foreach (TraceLink link in links)
                {
                    if (link == removed || link.attackerCanTraverse == false)
                        continue;

                    string other = link.Other(current);
                    if (other == null)
                        continue;

                    if (visited.Add(other))
                        frontier.Enqueue(other);
                }
            }

            return false;
        }

        // --- Helpers ---

        private int ScaledPenalty(int magnitude) => Mathf.RoundToInt(magnitude * penaltyMultiplier);

        private string LabelOf(string id)
        {
            TraceNode node = GetNode(id);
            return node != null ? node.label : id;
        }

        private static string HopsText(int hops) => hops == 1 ? "1 hop" : $"{hops} hops";

        private TraceSelectResult Result(TraceSelectOutcome outcome, string message) =>
            new TraceSelectResult { outcome = outcome, message = message };

        private string BuildSummary()
        {
            if (EntryNode == null || TargetNode == null)
                return string.Empty;

            // Neon palette, kept in sync with TraceMinigameUI: mint = success,
            // hot magenta = failure/attacker, cyan = accents.
            string okColor = ColorUtility.ToHtmlStringRGB(new Color(0.15f, 0.95f, 0.70f));
            string badColor = ColorUtility.ToHtmlStringRGB(new Color(1f, 0.30f, 0.60f));
            string accent = ColorUtility.ToHtmlStringRGB(new Color(0.20f, 0.90f, 1f));

            string summary = string.IsNullOrEmpty(activeScenarioName)
                ? string.Empty
                : $"<size=85%><i>Scenario: {activeScenarioName}</i></size>\n\n";

            summary += "<b>Attacker's lateral-movement path:</b>\n";

            for (int i = 0; i < path.Count; i++)
            {
                summary += LabelOf(path[i]);

                if (i < path.Count - 1)
                {
                    TraceLink link = GetLink(path[i], path[i + 1]);
                    string via = link != null && string.IsNullOrEmpty(link.evidence) == false
                        ? $" <size=80%><i>({link.evidence})</i></size>"
                        : string.Empty;
                    summary += $"  <color=#{accent}>&#8594;</color>{via}\n";
                }
            }

            if (lastCutLink != null)
            {
                string color = lastCutCorrect ? okColor : badColor;
                string verdict = lastCutCorrect ? "isolated the target" : "did not isolate the target";
                summary += $"\n\n<b>Containment:</b> cut " +
                           $"<b>{LabelOf(lastCutLink.fromId)} &#8212; {LabelOf(lastCutLink.toId)}</b> " +
                           $"<color=#{color}>&#8594; {verdict}</color>.";
            }

            if (attackerReachedTarget)
            {
                summary += $"\n\n<color=#{badColor}><b>The intruder reached {TargetNode.label} " +
                           "before you cut them off - data exfiltrated.</b></color>";
            }
            else if (lastCutCorrect)
            {
                summary += $"\n<color=#{okColor}>Intruder stopped {HopsText(AttackerHopsFromTarget)} " +
                           $"short of {TargetNode.label}.</color>";
            }

            summary += "\n\n<size=85%>Lesson: an attacker only moves where trust / credentials let them - " +
                       "and they keep moving while you investigate. Read the evidence, plan, then segment " +
                       "at a choke point AHEAD of them; everything downstream is cut off.</size>";

            return summary;
        }
    }
}
