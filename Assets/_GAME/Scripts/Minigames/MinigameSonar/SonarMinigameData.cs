namespace Minigames
{
    /// <summary>
    /// What a blip on the sonar arena really is. A <see cref="Cache"/> is a genuine data
    /// stash the player must mark; a <see cref="FalseEcho"/> is interference noise that
    /// only looks like a signal - clicking it is the mistake the minigame teaches against.
    ///
    /// The logic (<see cref="SonarMinigame"/>) owns only the counts and the rules; the
    /// concrete world-space positions and the reveal/flicker visuals live entirely in
    /// <see cref="SonarMinigameWorld"/>, which reports marks and false hits back through
    /// <see cref="SonarMinigame.MarkCache"/> / <see cref="SonarMinigame.RegisterFalseHit"/>.
    /// Extra echoes that ramp in over the run are just a pending counter the world drains
    /// via <see cref="SonarMinigame.TryDequeueExtraEcho"/> - no per-object order struct is
    /// needed because every echo is identical until it is placed.
    /// </summary>
    public enum SonarObjectKind
    {
        Cache,
        FalseEcho,
    }
}
