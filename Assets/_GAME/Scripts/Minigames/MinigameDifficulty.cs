using System;

namespace Minigames
{
    /// <summary>
    /// Task-authored difficulty knob for minigames.
    /// <see cref="Default"/> means "use the prefab values as-is" and is the backward
    /// compatible fallback for content that does not set a difficulty. Each minigame
    /// decides how (and whether) it reacts to the non-default levels.
    /// </summary>
    [Serializable]
    public enum MinigameDifficulty
    {
        Default = 0,
        Easy = 1,
        Normal = 2,
        Hard = 3,
    }
}
