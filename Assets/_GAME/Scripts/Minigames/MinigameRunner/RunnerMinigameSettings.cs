using RuntimeInspectorNamespace;
using System;

namespace Minigames
{
    /// <summary>
    /// Optional, task-authored overrides for the Packet Rush minigame. Same
    /// "zero = keep the prefab value" contract as the other minigames' settings blocks.
    /// Overrides are applied AFTER the difficulty multipliers, so an explicit value
    /// authored here wins verbatim.
    /// </summary>
    [Serializable, ExpandArray]
    public class RunnerMinigameSettings
    {
        // How many hits the packet survives. 0 = keep the prefab value.
        public int packetIntegrity = 0;

        // Multiplies the scroll speed (both the start and the ramped maximum).
        // 0 = keep the prefab value (1x).
        public float speedMultiplier = 0f;

        // Seconds between spawned waves at start speed (the game keeps on-screen
        // density constant as it speeds up). 0 = keep the prefab value.
        public float spawnInterval = 0f;

        // Chance (0..1] that each non-safe lane of a wave actually gets a hazard.
        // 0 = keep the prefab value.
        public float hazardChance = 0f;
    }
}
