using RuntimeInspectorNamespace;
using System;
using System.Collections.Generic;

namespace Minigames
{
    /// <summary>
    /// Optional, task-authored overrides for the Firewall Ruleset minigame.
    /// Empty / zero fields keep the values baked into the prefab, so content that does
    /// not provide this block is unaffected (same contract as the other minigames).
    /// </summary>
    [Serializable, ExpandArray]
    public class FirewallMinigameSettings
    {
        // The connection requests the player must sort. Empty = keep the prefab set.
        public List<FirewallTrafficEntry> traffic = new List<FirewallTrafficEntry>();

        // Maximum number of rules the player may create. 0 = keep the prefab value.
        public int ruleBudget = 0;

        // Set true to override the fall-through policy applied when no rule matches.
        public bool overrideDefaultPolicy = false;

        // Fall-through action used when overrideDefaultPolicy is true. Default-deny is
        // the intended lesson, so leave this on Deny unless a task deliberately teaches
        // an allow-list-with-explicit-denies pattern.
        public FirewallRuleAction defaultPolicy = FirewallRuleAction.Deny;
    }
}
