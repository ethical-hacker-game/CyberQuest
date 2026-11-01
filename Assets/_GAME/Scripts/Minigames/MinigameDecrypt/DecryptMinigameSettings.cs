using RuntimeInspectorNamespace;
using System;
using System.Collections.Generic;

namespace Minigames
{
    /// <summary>
    /// Optional, task-authored overrides for the Decrypt the Intercept minigame. Same
    /// "empty / zero = keep the prefab value" contract as the other minigames' settings
    /// blocks.
    /// </summary>
    [Serializable, ExpandArray]
    public class DecryptMinigameSettings
    {
        // Optional scenario pool. When provided it REPLACES the prefab pool and one
        // scenario is picked at random per play (the difficulty filter still applies).
        public List<DecryptScenario> scenarios = new List<DecryptScenario>();

        // How many WRONG decode attempts the player may make before the intercept locks
        // out (= loss). 0 = keep the prefab value (difficulty still adjusts it afterwards).
        public int wrongAttemptsAllowed = 0;

        /// <summary>True when at least one playable scenario is authored, i.e. this block replaces the prefab pool.</summary>
        public bool HasScenarioPool
        {
            get
            {
                if (scenarios == null)
                    return false;

                foreach (DecryptScenario scenario in scenarios)
                {
                    if (scenario != null && scenario.HasPuzzle)
                        return true;
                }

                return false;
            }
        }
    }
}
