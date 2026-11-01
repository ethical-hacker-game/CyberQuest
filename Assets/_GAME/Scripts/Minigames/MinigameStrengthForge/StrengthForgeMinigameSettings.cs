using RuntimeInspectorNamespace;
using System;
using System.Collections.Generic;

namespace Minigames
{
    /// <summary>
    /// Optional, task-authored overrides for the Strength Forge minigame.
    /// Empty lists keep the pool / rounds baked into the prefab, so content that does
    /// not provide this block is unaffected (same contract as the other minigames).
    /// </summary>
    [Serializable, ExpandArray]
    public class StrengthForgeMinigameSettings
    {
        // The token pool the player forges from. Empty = keep the prefab pool.
        public List<StrengthToken> tokens = new List<StrengthToken>();

        // The account rounds to clear, in order. Empty = keep the prefab rounds.
        public List<StrengthForgeRound> rounds = new List<StrengthForgeRound>();
    }
}
