using RuntimeInspectorNamespace;
using System;

namespace Minigames
{
    /// <summary>
    /// Optional, task-authored overrides for the Patch Hunt minigame. Same
    /// "zero/empty = keep the prefab value" contract as the other minigames' settings
    /// blocks. Overrides are applied AFTER the difficulty multipliers, so an explicit
    /// value authored here wins verbatim.
    /// </summary>
    [Serializable, ExpandArray]
    public class PatchHuntMinigameSettings
    {
        // How many breaches may slip through before the servers go down. 0 = prefab value.
        public int packetIntegrity = 0;

        // Base seconds between spawns (the pace still ramps up over the run). 0 = prefab value.
        public float spawnInterval = 0f;

        // Seconds a breach stays clickable before it slips through. 0 = prefab value.
        public float breachLifetime = 0f;

        // Chance (0..1] that a spawn is a honeypot decoy instead of a breach. 0 = prefab value.
        public float honeypotChance = 0f;

        // Story skin per unit: what a red target is called in feedback and the summary
        // (e.g. "RANSOMWARE OUTBREAK"). Empty = prefab value.
        public string breachLabel;

        // Story skin per unit: what a green decoy is called (e.g. "IR TEAM DECOY").
        // Empty = prefab value.
        public string honeypotLabel;
    }
}
