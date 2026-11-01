using RuntimeInspectorNamespace;
using System;

namespace Minigames
{
    /// <summary>
    /// Optional, task-authored overrides for the Sonar Sweep minigame. Same
    /// "zero/empty = keep the prefab value" contract as the other minigames' settings
    /// blocks. Overrides are applied AFTER the difficulty multipliers, so an explicit
    /// value authored here wins verbatim.
    /// </summary>
    [Serializable, ExpandArray]
    public class SonarMinigameSettings
    {
        // How many false-echo clicks the scanner survives before it burns out. 0 = prefab value.
        public int scanIntegrity = 0;

        // How many genuine caches must be marked to win. 0 = prefab value.
        public int cacheCount = 0;

        // How many false echoes are present at the start. 0 = prefab value.
        // The run may still ramp more echoes in up to the prefab concurrency cap.
        public int falseEchoCount = 0;

        // Multiplies the sonar sweep rotation speed. 0 = prefab value (1x).
        public float sweepSpeedMultiplier = 0f;

        // Seconds a swept blip keeps glowing before it fades back to invisible. 0 = prefab value.
        public float afterglowSeconds = 0f;

        // Multiplies how fast the blips drift around the arena (harder to re-find a contact).
        // 0 = prefab value (1x). Difficulty also scales this.
        public float blipDriftMultiplier = 0f;

        // Story skin per unit: what a genuine signal is called in feedback and the summary
        // (e.g. "Blue Lantern beacon", "Exfil trail"). Empty = prefab value.
        public string cacheLabel;

        // Story skin per unit: what the interference noise is called (e.g. "Jammer echo",
        // "Background chatter"). Empty = prefab value.
        public string echoLabel;
    }
}
