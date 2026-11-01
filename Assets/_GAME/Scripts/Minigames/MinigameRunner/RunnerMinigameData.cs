using System.Collections.Generic;

namespace Minigames
{
    /// <summary>
    /// Everything that can drop into a lane in front of the packet. Values below
    /// <see cref="DataShard"/> are hazards (cost integrity on contact), the rest are
    /// pickups (collected on contact, never punished when missed).
    /// </summary>
    public enum RunnerEntityKind
    {
        Firewall = 0,
        MalwareNode = 1,
        Sniffer = 2,
        DataShard = 10,
        Patch = 11,
    }

    public static class RunnerEntity
    {
        public static bool IsHazard(RunnerEntityKind kind) => kind < RunnerEntityKind.DataShard;
    }

    /// <summary>One thing to spawn: what and on which lane (0..2).</summary>
    public class RunnerSpawn
    {
        public RunnerEntityKind kind;
        public int lane;

        public RunnerSpawn(RunnerEntityKind kind, int lane)
        {
            this.kind = kind;
            this.lane = lane;
        }
    }

    /// <summary>
    /// A single row of simultaneous spawns produced by <see cref="RunnerMinigame"/>.
    /// The generator guarantees at least one hazard-free lane per wave, and that the
    /// free lane shifts by at most one lane between consecutive waves, so a clean
    /// route always exists.
    /// </summary>
    public class RunnerWave
    {
        public readonly List<RunnerSpawn> spawns = new List<RunnerSpawn>();
    }
}
