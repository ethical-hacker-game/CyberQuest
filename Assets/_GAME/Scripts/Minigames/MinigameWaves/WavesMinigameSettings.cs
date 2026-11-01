using RuntimeInspectorNamespace;
using System;

namespace Minigames
{
    /// <summary>
    /// Optional, task-authored overrides for the Waves minigame.
    /// A value of 0 keeps the prefab default, so content that does not provide this
    /// block is unaffected.
    /// </summary>
    [Serializable, ExpandArray]
    public class WavesMinigameSettings
    {
        // How close a player wave must match the target to count as solved.
        // Higher = more forgiving. 0 = keep the prefab value.
        public float tolerance = 0f;
    }
}
