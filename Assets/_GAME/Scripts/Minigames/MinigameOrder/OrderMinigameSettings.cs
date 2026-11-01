using RuntimeInspectorNamespace;
using System;
using System.Collections.Generic;

namespace Minigames
{
    /// <summary>
    /// Optional, task-authored overrides for the Sequence Order minigame. Same
    /// "empty / zero = keep the prefab value" contract as the other minigames' settings
    /// blocks.
    /// </summary>
    [Serializable, ExpandArray]
    public class OrderMinigameSettings
    {
        // Optional scenario pool. When provided it REPLACES the prefab pool and one
        // scenario is picked at random per play (the difficulty filter still applies).
        public List<OrderScenario> scenarios = new List<OrderScenario>();

        // How many CHECK ORDER attempts the player gets before the console locks out
        // (= loss). 0 = keep the prefab value (difficulty still adjusts it afterwards).
        public int checksAllowed = 0;

        /// <summary>True when at least one playable scenario is authored, i.e. this block replaces the prefab pool.</summary>
        public bool HasScenarioPool
        {
            get
            {
                if (scenarios == null)
                    return false;

                foreach (OrderScenario scenario in scenarios)
                {
                    if (scenario != null && scenario.HasSequence)
                        return true;
                }

                return false;
            }
        }
    }
}
