using System;
using System.Collections.Generic;
using UnityEngine;

namespace Minigames
{
    /// <summary>
    /// What a host represents in the topology puzzle. <see cref="Entry"/> is where the
    /// attacker already has a foothold, <see cref="Target"/> is the crown-jewel asset to
    /// protect, <see cref="Pivot"/> / <see cref="Decoy"/> are the hosts in between the
    /// player has to reason about. A valid graph has exactly one Entry and one Target.
    /// </summary>
    public enum TraceNodeRole
    {
        Pivot = 0,
        Entry = 1,
        Target = 2,
        Decoy = 3,
    }

    /// <summary>
    /// One host in the network topology. <see cref="position"/> is a normalised 0..1
    /// point used to lay the node out inside the graph area (0,0 = bottom-left).
    /// </summary>
    [Serializable]
    public class TraceNode
    {
        [Tooltip("Unique id used by links to reference this node, e.g. \"ws-01\".")]
        public string id = "node";

        [Tooltip("Hostname / label shown on the node, e.g. \"WKS-FINANCE\".")]
        public string label = "host";

        public TraceNodeRole role = TraceNodeRole.Pivot;

        [TextArea(1, 3)]
        [Tooltip("Exposed services / notes shown when the node is inspected.")]
        public string detail = "";

        [Tooltip("Normalised 0..1 position inside the graph area (0,0 = bottom-left, 1,1 = top-right).")]
        public Vector2 position = new Vector2(0.5f, 0.5f);
    }

    /// <summary>
    /// A network link (undirected) between two hosts. The attacker can only pivot along a
    /// link when <see cref="attackerCanTraverse"/> is true; the player infers that from
    /// <see cref="evidence"/>. A link flagged <see cref="isContainmentPoint"/> is an
    /// author-designated segmentation cut &mdash; optional: leave every link false and the
    /// minigame accepts any cut that provably isolates the target (graph reachability).
    /// </summary>
    [Serializable]
    public class TraceLink
    {
        [Tooltip("Id of one endpoint host.")]
        public string fromId = "";

        [Tooltip("Id of the other endpoint host.")]
        public string toId = "";

        [Tooltip("Ground truth: can the attacker pivot along this link? The player infers it from the evidence, it is never shown directly.")]
        public bool attackerCanTraverse = false;

        [TextArea(1, 3)]
        [Tooltip("Why the link is / isn't pivotable, e.g. \"shared local-admin creds\" or \"segmented VLAN, no route\".")]
        public string evidence = "";

        [Tooltip("Marks this link as the intended containment cut. Optional - leave all false to let the puzzle accept any cut that isolates the target.")]
        public bool isContainmentPoint = false;

        /// <summary>True when this link joins the two given node ids, in either direction.</summary>
        public bool Connects(string a, string b)
        {
            return (fromId == a && toId == b) || (fromId == b && toId == a);
        }

        /// <summary>Returns the endpoint on the far side of <paramref name="nodeId"/>, or null when the link doesn't touch it.</summary>
        public string Other(string nodeId)
        {
            if (fromId == nodeId) return toId;
            if (toId == nodeId) return fromId;
            return null;
        }
    }

    /// <summary>
    /// One authored topology puzzle. The minigame keeps a pool of these and picks one at
    /// random per play, so repeated plays don't feel identical.
    /// <see cref="recommendedDifficulty"/> tags which difficulty the scenario is tuned
    /// for; <see cref="MinigameDifficulty.Default"/> means "eligible at any difficulty".
    /// </summary>
    [Serializable]
    public class TraceScenario
    {
        [Tooltip("Author-facing name, also shown on the result screen.")]
        public string scenarioName = "Scenario";

        [Tooltip("Difficulty this scenario is tuned for. Default = usable at any difficulty.")]
        public MinigameDifficulty recommendedDifficulty = MinigameDifficulty.Default;

        public List<TraceNode> nodes = new List<TraceNode>();
        public List<TraceLink> links = new List<TraceLink>();

        /// <summary>True when the scenario actually contains a playable graph.</summary>
        public bool HasGraph => nodes != null && nodes.Count > 0 && links != null && links.Count > 0;
    }

    /// <summary>Turn-based phase the puzzle is in.</summary>
    public enum TracePhase
    {
        Trace = 0,   // Player is reconstructing the attacker's lateral path.
        Contain = 1, // A valid Entry->Target path exists; player must pick the cut.
        Done = 2,    // Solved (or finished) - input is locked.
    }

    /// <summary>Outcome of a <see cref="TraceMinigame.TrySelectNode"/> call, so the UI can react + explain.</summary>
    public enum TraceSelectOutcome
    {
        Extended,             // Path grew by one valid hop.
        PathCompleted,        // The hop reached the target - phase switches to Contain.
        Truncated,            // Player clicked a node already in the path - undo back to it.
        RejectedAlreadyTip,   // Clicked the current tip - no-op.
        RejectedNotAdjacent,  // No direct link between the tip and the clicked node.
        RejectedNotPivotable, // Link exists but the attacker can't traverse it.
        Ignored,              // Not in the Trace phase.
    }

    /// <summary>Result of proposing a node during the trace phase.</summary>
    public struct TraceSelectResult
    {
        public TraceSelectOutcome outcome;
        public string message;
        public bool IsRejection =>
            outcome == TraceSelectOutcome.RejectedNotAdjacent ||
            outcome == TraceSelectOutcome.RejectedNotPivotable;
    }

    /// <summary>Result of proposing a containment cut during the contain phase.</summary>
    public struct TraceCutResult
    {
        public bool handled;   // False when called outside the Contain phase.
        public bool correct;   // True when the cut isolates the target -> win.
        public string message;
    }
}
