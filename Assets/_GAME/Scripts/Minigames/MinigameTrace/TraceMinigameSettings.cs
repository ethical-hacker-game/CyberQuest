using RuntimeInspectorNamespace;
using System;
using System.Collections.Generic;

namespace Minigames
{
    /// <summary>
    /// Optional, task-authored overrides for the Trace the Intruder minigame. A graph is
    /// nodes + links together, so both lists must be provided to override; when either is
    /// empty the graph baked into the prefab is kept (same "empty = keep prefab" contract
    /// as the other minigames' settings blocks).
    /// </summary>
    [Serializable, ExpandArray]
    public class TraceMinigameSettings
    {
        // Optional scenario pool. When provided it REPLACES the prefab pool and one
        // scenario is picked at random per play (the difficulty filter still applies).
        // Takes precedence over the single nodes/links graph below.
        public List<TraceScenario> scenarios = new List<TraceScenario>();

        // The hosts of the topology. Provide together with links to pin one exact graph
        // (no scenario randomisation).
        public List<TraceNode> nodes = new List<TraceNode>();

        // The links between hosts. Provide together with nodes to pin one exact graph.
        public List<TraceLink> links = new List<TraceLink>();

        // The intruder advances one hop per this many committed player moves.
        // 0 = keep the prefab value (difficulty still adjusts it afterwards).
        public int attackerStepEvery = 0;

        /// <summary>True only when both lists are populated, i.e. this block pins one exact graph.</summary>
        public bool HasGraph => nodes != null && nodes.Count > 0 && links != null && links.Count > 0;

        /// <summary>True when at least one playable scenario is authored, i.e. this block replaces the prefab pool.</summary>
        public bool HasScenarioPool
        {
            get
            {
                if (scenarios == null)
                    return false;

                foreach (TraceScenario scenario in scenarios)
                {
                    if (scenario != null && scenario.HasGraph)
                        return true;
                }

                return false;
            }
        }
    }
}
