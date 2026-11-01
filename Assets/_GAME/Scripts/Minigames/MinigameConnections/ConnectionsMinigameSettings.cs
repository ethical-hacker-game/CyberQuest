using RuntimeInspectorNamespace;
using System;
using System.Collections.Generic;

namespace Minigames
{
    /// <summary>
    /// Optional, task-authored overrides for the Connections minigame.
    /// When a list is left empty the minigame keeps the values baked into the prefab,
    /// so existing content that does not provide this block is unaffected.
    /// </summary>
    [Serializable, ExpandArray]
    public class ConnectionsMinigameSettings
    {
        // Benign / expected traffic the player should let through.
        public List<string> positiveConnections = new List<string>();

        // Malicious / suspicious traffic the player should block.
        public List<string> negativeConnections = new List<string>();
    }
}
