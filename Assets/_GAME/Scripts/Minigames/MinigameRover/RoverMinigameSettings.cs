using RuntimeInspectorNamespace;
using System;

namespace Minigames
{
    /// <summary>
    /// Optional, task-authored overrides for the Data Rover minigame. Same
    /// "zero = keep the prefab value" contract as the other minigames' settings blocks
    /// (for <see cref="layoutIndex"/> the neutral value is -1 = random layout).
    /// Overrides are applied AFTER the difficulty multipliers, so an explicit value
    /// authored here wins verbatim.
    /// </summary>
    [Serializable, ExpandArray]
    public class RoverMinigameSettings
    {
        // How many detections the rover survives. 0 = keep the prefab value.
        public int packetIntegrity = 0;

        // How many shards are placed on the arena. 0 = keep the prefab value.
        // Must not exceed the shard capacity of the chosen layout (validated).
        public int shardCount = 0;

        // Multiplies the patrol bot speed. 0 = keep the prefab value (1x).
        public float patrolSpeedMultiplier = 0f;

        // Which arena layout to use: -1 = random from the built-in pool,
        // 0..n = a specific layout (see RoverLayouts.BuiltIn).
        public int layoutIndex = -1;

        // Story skin for this unit: what the shards represent (e.g. "SIEM log fragment").
        // Used in the HUD feedback and the result Summary. Empty = generic "DATA SHARD".
        public string shardLabel;

        // Story skin for the patrols (e.g. "Blue Lantern security drone").
        // Empty = generic "PATROL BOT".
        public string patrolLabel;
    }
}
