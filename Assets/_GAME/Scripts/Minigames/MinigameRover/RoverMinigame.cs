using UnityEngine;

namespace Minigames
{
    /// <summary>
    /// "Data Rover" &mdash; a top-down 3D stealth-lite expedition: the player drives a
    /// recovery rover through a neon arena and must collect every data shard before the
    /// mission clock runs out. Patrol bots sweep the floor on 100% predictable waypoint
    /// loops, each projecting a vision cone; touching a bot or its cone costs 1 integrity
    /// and grants a short invulnerability window (the bot never chases - it simply keeps
    /// patrolling). Collecting all shards wins immediately (plus a time bonus); running
    /// out of time or integrity loses (<c>resultOnTimeout = false</c>, unlike Packet Rush).
    ///
    /// The rules (shards, integrity, scoring, difficulty, config) live here with zero
    /// view code; <see cref="RoverMinigameWorld"/> builds the 3D arena and reports
    /// pickups/detections back through <see cref="CollectShard"/> and <see cref="OnDetected"/>.
    ///
    /// Didactic hook: the patrols never improvise, so observing their routines reveals
    /// safe time windows - that is what reconnaissance is. Attackers exploit predictable
    /// routines (guard rounds, scan schedules); good defense randomizes them.
    /// </summary>
    public class RoverMinigame : MinigameBehaviour
    {
        private static RoverMinigame minigameInstance;

        [Header("Rover")]
        [SerializeField, Tooltip("How many detections the rover survives. Difficulty adds +1 (Easy) / -1 (Hard).")]
        private int roverIntegrity = 3;
        [SerializeField, Tooltip("Top speed in arena cells per second.")]
        private float roverSpeed = 2.9f;
        [SerializeField, Tooltip("Acceleration in cells per second squared (also used for braking).")]
        private float roverAcceleration = 14f;
        [SerializeField, Tooltip("Seconds of invulnerability after a detection.")]
        private float invulnerabilitySeconds = 1.5f;

        [Header("Mission")]
        [SerializeField, Tooltip("How many shards are placed on the arena (clamped to the layout capacity).")]
        private int shardCount = 8;
        [SerializeField, Tooltip("Arena layout: -1 = random from the built-in pool, 0..n = specific layout.")]
        private int layoutIndex = -1;
        [SerializeField, Tooltip("Story skin: what the shards represent. Overridable per task.")]
        private string shardLabel = "DATA SHARD";
        [SerializeField, Tooltip("Story skin: what the patrols represent. Overridable per task.")]
        private string patrolLabel = "PATROL BOT";

        [Header("Patrols")]
        [SerializeField, Tooltip("Patrol bot speed in arena cells per second.")]
        private float patrolSpeed = 1.4f;
        [SerializeField, Tooltip("Vision cone range in arena cells.")]
        private float visionRange = 2.6f;
        [SerializeField, Tooltip("Vision cone half-angle in degrees.")]
        private float visionHalfAngle = 34f;

        [Header("Scoring")]
        [SerializeField, Tooltip("Points for every collected shard.")]
        private int pointsPerShard = 120;
        [SerializeField, Tooltip("Win bonus: points per second left on the clock.")]
        private int timeBonusPerSecond = 12;
        [SerializeField, Tooltip("Points removed on a detection (magnitude, subtracted).")]
        private int detectionPenalty = 90;

        private int maxIntegrity;
        private int currentIntegrity;
        private int collected;
        private int detections;
        private float invulnerabilityTimer;
        private bool finished;

        private float patrolSpeedMultiplier = 1f;
        private float visionMultiplier = 1f;
        private bool useExtraPatrol;

        private RoverLayout resolvedLayout;
        private int resolvedLayoutIndex = -1;

        public static RoverMinigame MinigameInstance
        {
            get
            {
                if (minigameInstance == null)
                    Debug.LogError("minigameInstance does not exist");

                return minigameInstance;
            }
        }

        public int MaxIntegrity => maxIntegrity;
        public int CurrentIntegrity => currentIntegrity;
        public int Collected => collected;
        public int Detections => detections;
        public bool IsFinished => finished;
        public bool IsInvulnerable => invulnerabilityTimer > 0f;
        public float InvulnerabilityRemaining => Mathf.Max(0f, invulnerabilityTimer);
        public string ShardLabel => shardLabel;
        public string PatrolLabel => patrolLabel;
        public bool UseExtraPatrol => useExtraPatrol;

        /// <summary>Movement tuning for the world, in arena cells per second.</summary>
        public float RoverSpeedCells => roverSpeed;
        public float RoverAccelerationCells => roverAcceleration;
        public float PatrolSpeedCells => patrolSpeed * patrolSpeedMultiplier;
        public float VisionRangeCells => visionRange * visionMultiplier;
        public float VisionHalfAngleDegrees => Mathf.Min(visionHalfAngle * visionMultiplier, 48f);

        /// <summary>
        /// The arena this run plays on. Resolved lazily so the task-authored
        /// <c>layoutIndex</c> override (applied in <see cref="ApplyConfig"/>) is honored.
        /// </summary>
        public RoverLayout Layout
        {
            get
            {
                if (resolvedLayout == null)
                {
                    resolvedLayoutIndex = layoutIndex >= 0 && layoutIndex < RoverLayouts.BuiltIn.Length
                        ? layoutIndex
                        : Random.Range(0, RoverLayouts.BuiltIn.Length);
                    resolvedLayout = RoverLayouts.BuiltIn[resolvedLayoutIndex];
                }

                return resolvedLayout;
            }
        }

        public int TotalShards => Mathf.Clamp(shardCount, 1, Layout.ShardCapacity);

        public override string Summary => BuildSummary();

        private void Awake()
        {
            minigameInstance = this;
            maxIntegrity = Mathf.Max(1, roverIntegrity);
            currentIntegrity = maxIntegrity;
        }

        private void OnDestroy()
        {
            minigameInstance = null;
        }

        public override void ApplyConfig(TaskMinigameVariant config)
        {
            // Runs after Awake but before Start/Update, so integrity, layout and patrol
            // values are re-derived before the world is built or the UI reads them.
            base.ApplyConfig(config);

            ApplyDifficulty();

            if (config != null && config.dataRoverSettings != null)
            {
                RoverMinigameSettings settings = config.dataRoverSettings;

                if (settings.packetIntegrity > 0)
                    roverIntegrity = settings.packetIntegrity;

                if (settings.shardCount > 0)
                    shardCount = settings.shardCount;

                if (settings.patrolSpeedMultiplier > 0f)
                    patrolSpeedMultiplier *= settings.patrolSpeedMultiplier;

                if (settings.layoutIndex >= 0)
                    layoutIndex = settings.layoutIndex;

                if (string.IsNullOrEmpty(settings.shardLabel) == false)
                    shardLabel = settings.shardLabel;

                if (string.IsNullOrEmpty(settings.patrolLabel) == false)
                    patrolLabel = settings.patrolLabel;
            }

            maxIntegrity = Mathf.Max(1, roverIntegrity);
            currentIntegrity = maxIntegrity;
        }

        private void ApplyDifficulty()
        {
            if (Difficulty == MinigameDifficulty.Default)
                return;

            switch (Difficulty)
            {
                case MinigameDifficulty.Easy:
                    roverIntegrity += 1;
                    patrolSpeedMultiplier *= 0.8f;
                    visionMultiplier *= 0.8f;
                    break;
                case MinigameDifficulty.Hard:
                    roverIntegrity = Mathf.Max(1, roverIntegrity - 1);
                    patrolSpeedMultiplier *= 1.25f;
                    visionMultiplier *= 1.2f;
                    useExtraPatrol = true;
                    break;
            }
        }

        /// <summary>Counts down the invulnerability window. Driven from the world's Update.</summary>
        public void TickTimers(float deltaTime)
        {
            if (isPaused || finished)
                return;

            if (invulnerabilityTimer > 0f)
                invulnerabilityTimer -= deltaTime;
        }

        /// <summary>
        /// Called by the world when the rover touches a shard. Returns the feedback line
        /// to show; collecting the last shard finishes the game with a time bonus.
        /// </summary>
        public string CollectShard()
        {
            if (finished)
                return string.Empty;

            collected++;
            ChangeScore(pointsPerShard);

            if (collected >= TotalShards)
            {
                int bonus = Mathf.RoundToInt(MinigamesController.GetTimerPositive * timeBonusPerSecond);
                ChangeScore(bonus);
                finished = true;
                FinishMinigame(true, Score, Summary);
                return $"All evidence recovered - extraction complete! Time bonus +{bonus}";
            }

            return $"{shardLabel} secured ({collected}/{TotalShards})  +{pointsPerShard}";
        }

        /// <summary>
        /// Called by the world when a patrol (body or vision cone) catches the rover.
        /// Silently ignored during the invulnerability window. Returns the feedback line.
        /// </summary>
        public string OnDetected()
        {
            if (finished || IsInvulnerable)
                return string.Empty;

            detections++;
            currentIntegrity--;
            ChangeScore(-detectionPenalty);
            invulnerabilityTimer = invulnerabilitySeconds;

            if (currentIntegrity <= 0)
            {
                currentIntegrity = 0;
                finished = true;
                FinishMinigame(false, Score, Summary);
                return "ROVER COMPROMISED - the security sweep caught you.";
            }

            return $"Spotted by a {patrolLabel}! Integrity -1";
        }

        private string BuildSummary()
        {
            return "<b><color=#35E0FF>Expedition report:</color></b>" +
                $"\nEvidence recovered: {collected}/{TotalShards}   •   Detections: {detections}   •   Arena: {Layout.name}" +
                "\n\n<b>Field legend:</b>" +
                $"\n<color=#36E0FF>{shardLabel}</color> - the evidence you were sent to recover" +
                $"\n<color=#FF5A47>{patrolLabel}</color> - a sentry sweeping a fixed, repeating patrol loop" +
                "\n<color=#FFB833>VISION CONE</color> - its detection zone; it cannot see through walls" +
                "\n\n<b>Lesson:</b> the patrols never improvised - every loop repeated exactly, so watching " +
                "them revealed safe time windows. That is reconnaissance: attackers study routines (guard " +
                "rounds, backup jobs, scan schedules) and strike between them. Good defense assumes this and " +
                "randomizes patrol timing, staggers monitoring and removes blind spots that can be timed.";
        }
    }
}
