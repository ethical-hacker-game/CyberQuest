using RuntimeInspectorNamespace;
using System;

namespace Minigames
{
    /// <summary>
    /// Optional, task-authored overrides for the Network minigame family.
    /// Each field targets one variant's win condition; a value of 0 keeps the prefab
    /// default. The NetworkDistance variant has no count goal and is tuned through
    /// <see cref="MinigameDifficulty"/> only.
    /// </summary>
    [Serializable, ExpandArray]
    public class NetworkMinigameSettings
    {
        // NetworkDevices: how many devices must be hacked to win. 0 = keep prefab value.
        public int devicesToHack = 0;

        // NetworkTrace: how many recon moves (node hops) win the run. 0 = keep prefab value.
        public int reconMovesToWin = 0;
    }
}
