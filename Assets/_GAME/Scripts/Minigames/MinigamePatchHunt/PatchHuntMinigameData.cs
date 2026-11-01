namespace Minigames
{
    /// <summary>
    /// What kind of target erupted from a rack. A Breach must be clicked before its
    /// timer ring closes; a Honeypot is a decoy that must be left alone.
    /// </summary>
    public enum PatchHuntTargetKind
    {
        Breach,
        Honeypot,
    }

    /// <summary>
    /// One spawn order produced by <see cref="PatchHuntMinigame"/> and consumed by
    /// <see cref="PatchHuntMinigameWorld"/>: which rack erupts, what kind of target,
    /// and how long it stays alive. The rack is already reserved by the logic when
    /// the order is enqueued, so two targets can never share a rack.
    /// </summary>
    public class PatchHuntSpawn
    {
        public readonly PatchHuntTargetKind kind;
        public readonly int rackIndex;
        public readonly float lifetime;

        public PatchHuntSpawn(PatchHuntTargetKind kind, int rackIndex, float lifetime)
        {
            this.kind = kind;
            this.rackIndex = rackIndex;
            this.lifetime = lifetime;
        }
    }
}
