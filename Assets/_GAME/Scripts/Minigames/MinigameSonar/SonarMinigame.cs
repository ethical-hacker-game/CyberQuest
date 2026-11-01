using UnityEngine;

namespace Minigames
{
    /// <summary>
    /// "Sonar Sweep" &mdash; a dark top-down arena with a scanner station in the middle whose
    /// rotating sonar fan is the only source of light. Blips are invisible until the beam
    /// sweeps over them; then they flash and fade over <see cref="AfterglowSeconds"/>.
    /// Genuine DATA CACHES hold a steady, sharp signal; FALSE ECHOES flicker and wobble.
    /// In the fading window the player clicks the ones they judge to be caches. Marking a
    /// cache is permanent and advances the <c>X/N</c> counter; clicking a false echo
    /// overloads the scanner (&minus;1 integrity, and the echo burns off). Marking every
    /// cache wins immediately with a time bonus; running out of time or integrity loses
    /// (<c>resultOnTimeout = false</c>, like Data Rover).
    ///
    /// The rules (counts, integrity, scoring, the sweep tuning the view reads, the echo
    /// ramp) live here with zero view code; <see cref="SonarMinigameWorld"/> owns the
    /// arena, the fan mesh, the deterministic reveal math and the ray-sphere clicks, and
    /// reports back through <see cref="MarkCache"/> and <see cref="RegisterFalseHit"/>.
    ///
    /// Didactic hook: false positives cost. You only ever see what you just scanned, and
    /// you must decide on fading, incomplete information - so verify the signal (watch how
    /// it pulses) before you react. Reflexive clicks on noise are exactly the alert
    /// fatigue that real SOC work has to resist.
    /// </summary>
    public class SonarMinigame : MinigameBehaviour
    {
        private static SonarMinigame minigameInstance;

        [Header("Scanner")]
        [SerializeField, Tooltip("How many false-echo clicks the scanner survives. Difficulty adds +1 (Easy) / -1 (Hard).")]
        private int scanIntegrity = 3;

        [Header("Signals")]
        [SerializeField, Tooltip("How many genuine caches must be marked to win.")]
        private int cacheCount = 6;
        [SerializeField, Tooltip("How many caches are already on the arena when the run starts; the rest fade in over time so one sweep can never reveal them all.")]
        private int initialCacheCount = 2;
        [SerializeField, Tooltip("Seconds between caches fading in, until every cache is present.")]
        private float cacheSpawnInterval = 7f;
        [SerializeField, Tooltip("How many false echoes are present at the start.")]
        private int falseEchoCount = 6;
        [SerializeField, Tooltip("Fairness cap: the arena never holds more live echoes than this at once.")]
        private int maxConcurrentEchoes = 10;
        [SerializeField, Tooltip("Seconds between the extra echoes that ramp in as pressure over the run.")]
        private float extraEchoInterval = 9f;

        [Header("Sweep")]
        [SerializeField, Tooltip("Seconds the sonar fan takes for one full revolution (smaller = faster).")]
        private float secondsPerRevolution = 6f;
        [SerializeField, Tooltip("Seconds a swept blip keeps glowing before it fades back to invisible.")]
        private float afterglowSeconds = 2.5f;
        [SerializeField, Tooltip("Fan half-angle (degrees) at the start of the run.")]
        private float fanHalfAngleStart = 26f;
        [SerializeField, Tooltip("Fan half-angle (degrees) the beam narrows down to as pressure rises.")]
        private float fanHalfAngleEnd = 15f;
        [SerializeField, Tooltip("Seconds of play over which the fan narrows from start to end angle.")]
        private float fanNarrowSeconds = 55f;

        [Header("Scoring")]
        [SerializeField, Tooltip("Points for marking a genuine cache.")]
        private int pointsPerCache = 130;
        [SerializeField, Tooltip("Extra points for marking a cache while its signal is still bright (fast, confident reads).")]
        private int freshSweepBonus = 60;
        [SerializeField, Tooltip("Points removed for clicking a false echo (on top of the integrity loss).")]
        private int falseEchoPenalty = 90;
        [SerializeField, Tooltip("Win bonus: points per second left on the clock.")]
        private int timeBonusPerSecond = 12;

        [Header("Story skin (overridable per task)")]
        [SerializeField, Tooltip("What a genuine signal is called in feedback and the summary.")]
        private string cacheLabel = "DATA CACHE";
        [SerializeField, Tooltip("What the interference noise is called in feedback and the summary.")]
        private string echoLabel = "FALSE ECHO";

        // Difficulty / settings multipliers, folded in during ApplyConfig.
        private float sweepSpeedMultiplier = 1f;
        private float afterglowMultiplier = 1f;
        private float fanWidthMultiplier = 1f;
        private float echoCountMultiplier = 1f;
        private float blipDriftMultiplier = 1f;

        private int maxIntegrity;
        private int currentIntegrity;
        private int resolvedInitialEchoCount;
        private int resolvedInitialCacheCount;

        private int marked;
        private int freshMarks;
        private int falseHits;
        private int liveEchoes;
        private int pendingExtraEchoes;
        private int extraEchoesSpawned;
        private int cachesSpawned;
        private int pendingCaches;

        private float elapsed;
        private float extraEchoTimer;
        private float cacheSpawnTimer;
        private bool finished;

        public static SonarMinigame MinigameInstance
        {
            get
            {
                if (minigameInstance == null)
                    Debug.LogError("minigameInstance does not exist");

                return minigameInstance;
            }
        }

        public int TotalCaches => Mathf.Max(1, cacheCount);
        public int InitialCacheCount => resolvedInitialCacheCount;
        public int InitialEchoCount => resolvedInitialEchoCount;
        public int MaxIntegrity => maxIntegrity;
        public int CurrentIntegrity => currentIntegrity;
        public int Marked => marked;
        public int FalseHits => falseHits;
        public bool IsFinished => finished;
        public string CacheLabel => cacheLabel;
        public string EchoLabel => echoLabel;

        /// <summary>Sweep rotation speed the view spins the fan at, degrees per second.</summary>
        public float SweepAngularSpeedDegPerSec =>
            secondsPerRevolution > 0f ? (360f / secondsPerRevolution) * sweepSpeedMultiplier : 0f;

        /// <summary>Afterglow length the view fades a swept blip over.</summary>
        public float AfterglowSeconds => Mathf.Max(0.2f, afterglowSeconds * afterglowMultiplier);

        /// <summary>
        /// Multiplier the view applies to how fast contacts drift around the arena.
        /// Difficulty and task settings fold into it; 0 would pin the blips still.
        /// </summary>
        public float BlipDriftMultiplier => Mathf.Max(0f, blipDriftMultiplier);

        /// <summary>
        /// Current fan half-angle: it narrows from the start angle to the end angle over
        /// the ramp, so the visibility windows shorten as the run wears on.
        /// </summary>
        public float CurrentFanHalfAngle
        {
            get
            {
                float ramp = fanNarrowSeconds > 0f ? Mathf.Clamp01(elapsed / fanNarrowSeconds) : 1f;
                float baseAngle = Mathf.Lerp(fanHalfAngleStart, fanHalfAngleEnd, ramp);
                return Mathf.Clamp(baseAngle * fanWidthMultiplier, 4f, 80f);
            }
        }

        /// <summary>A cache counts as marked "fresh" (bonus) when its glow is at least this bright.</summary>
        public float FreshGlowThreshold => 0.6f;

        public override string Summary => BuildSummary();

        private void Awake()
        {
            minigameInstance = this;
            maxIntegrity = Mathf.Max(1, scanIntegrity);
            currentIntegrity = maxIntegrity;
            resolvedInitialEchoCount = Mathf.Max(0, falseEchoCount);
            liveEchoes = resolvedInitialEchoCount;
            resolvedInitialCacheCount = Mathf.Clamp(initialCacheCount, 1, Mathf.Max(1, cacheCount));
            cachesSpawned = resolvedInitialCacheCount;
        }

        private void OnDestroy()
        {
            minigameInstance = null;
        }

        public override void ApplyConfig(TaskMinigameVariant config)
        {
            // Runs after Awake but before Start/Update, so counts, integrity and pacing are
            // re-derived before the world places the blips or the UI reads them.
            base.ApplyConfig(config);

            ApplyDifficulty();

            if (config != null && config.sonarSettings != null)
            {
                SonarMinigameSettings settings = config.sonarSettings;

                if (settings.scanIntegrity > 0)
                    scanIntegrity = settings.scanIntegrity;

                if (settings.cacheCount > 0)
                    cacheCount = settings.cacheCount;

                if (settings.falseEchoCount > 0)
                    falseEchoCount = settings.falseEchoCount;

                if (settings.sweepSpeedMultiplier > 0f)
                    sweepSpeedMultiplier *= settings.sweepSpeedMultiplier;

                if (settings.afterglowSeconds > 0f)
                    afterglowSeconds = settings.afterglowSeconds;

                if (settings.blipDriftMultiplier > 0f)
                    blipDriftMultiplier *= settings.blipDriftMultiplier;

                if (string.IsNullOrEmpty(settings.cacheLabel) == false)
                    cacheLabel = settings.cacheLabel;

                if (string.IsNullOrEmpty(settings.echoLabel) == false)
                    echoLabel = settings.echoLabel;
            }

            maxIntegrity = Mathf.Max(1, scanIntegrity);
            currentIntegrity = maxIntegrity;

            // Echo count reacts to difficulty, but the concurrency cap always wins so the
            // arena can never be over-packed (fairness: blips stay clickable-apart).
            int scaledEchoes = Mathf.RoundToInt(Mathf.Max(0, falseEchoCount) * echoCountMultiplier);
            resolvedInitialEchoCount = Mathf.Clamp(scaledEchoes, 0, Mathf.Max(0, maxConcurrentEchoes));
            liveEchoes = resolvedInitialEchoCount;

            // Only a few caches are present at the start; the rest fade in over the run so a
            // single sweep can never reveal (and clear) every cache at once.
            resolvedInitialCacheCount = Mathf.Clamp(initialCacheCount, 1, TotalCaches);
            cachesSpawned = resolvedInitialCacheCount;
        }

        private void ApplyDifficulty()
        {
            if (Difficulty == MinigameDifficulty.Default)
                return;

            switch (Difficulty)
            {
                case MinigameDifficulty.Easy:
                    scanIntegrity += 1;
                    fanWidthMultiplier *= 1.3f;   // wider beam = longer, clearer windows
                    afterglowMultiplier *= 1.3f;  // signals linger longer
                    echoCountMultiplier *= 0.7f;  // less noise to sort through
                    blipDriftMultiplier *= 0.6f;  // contacts drift slower - easier to re-find
                    break;
                case MinigameDifficulty.Hard:
                    scanIntegrity = Mathf.Max(1, scanIntegrity - 1);
                    fanWidthMultiplier *= 0.75f;
                    afterglowMultiplier *= 0.75f;
                    echoCountMultiplier *= 1.4f;
                    blipDriftMultiplier *= 1.45f; // contacts scatter faster
                    cacheCount += 1;
                    break;
            }
        }

        /// <summary>
        /// Advances the run clock and ramps extra echoes into the queue. Driven from the
        /// world's Update (guaranteed to tick while the game is live), the same proven
        /// path Data Rover uses for its own timers.
        /// </summary>
        public void Tick(float deltaTime)
        {
            if (isPaused || finished)
                return;

            elapsed += deltaTime;
            extraEchoTimer += deltaTime;

            if (extraEchoTimer >= extraEchoInterval && extraEchoInterval > 0f)
            {
                extraEchoTimer = 0f;

                if (liveEchoes + pendingExtraEchoes < maxConcurrentEchoes)
                    pendingExtraEchoes++;
            }

            // Fade the remaining caches in on a schedule until every cache is present.
            if (cachesSpawned + pendingCaches < TotalCaches && cacheSpawnInterval > 0f)
            {
                cacheSpawnTimer += deltaTime;

                if (cacheSpawnTimer >= cacheSpawnInterval)
                {
                    cacheSpawnTimer = 0f;
                    pendingCaches++;
                }
            }
        }

        /// <summary>The world drains this each frame and fades a new cache onto the arena.</summary>
        public bool TryDequeueCache()
        {
            if (finished || pendingCaches <= 0)
                return false;

            pendingCaches--;
            cachesSpawned++;
            return true;
        }

        /// <summary>The world drains this each frame and drops a fresh echo onto the arena.</summary>
        public bool TryDequeueExtraEcho()
        {
            if (finished || pendingExtraEchoes <= 0)
                return false;

            pendingExtraEchoes--;
            liveEchoes++;
            extraEchoesSpawned++;
            return true;
        }

        /// <summary>
        /// Called by the world when the player clicks a genuine cache. <paramref name="freshGlow"/>
        /// is true when the signal was still bright (a fast, confident read) and earns the
        /// bonus. Marking the last cache finishes the game with a time bonus.
        /// </summary>
        public string MarkCache(bool freshGlow)
        {
            if (finished)
                return string.Empty;

            marked++;

            int points = pointsPerCache;
            string bonusNote = string.Empty;

            if (freshGlow)
            {
                freshMarks++;
                points += freshSweepBonus;
                bonusNote = $"   •   FRESH READ +{freshSweepBonus}";
            }

            ChangeScore(points);

            if (marked >= TotalCaches)
            {
                int bonus = Mathf.RoundToInt(MinigamesController.GetTimerPositive * timeBonusPerSecond);
                ChangeScore(bonus);
                finished = true;
                FinishMinigame(true, Score, Summary);
                return $"All caches located - sweep complete! Time bonus +{bonus}";
            }

            return $"{cacheLabel} marked ({marked}/{TotalCaches})  +{points}{bonusNote}";
        }

        /// <summary>
        /// Called by the world when the player clicks a false echo. Costs 1 integrity and a
        /// point penalty, and the echo burns off (it never fools twice). At zero integrity
        /// the scanner is dead and the run is lost.
        /// </summary>
        public string RegisterFalseHit()
        {
            if (finished)
                return string.Empty;

            falseHits++;
            liveEchoes = Mathf.Max(0, liveEchoes - 1);
            currentIntegrity--;
            ChangeScore(-falseEchoPenalty);

            if (currentIntegrity <= 0)
            {
                currentIntegrity = 0;
                finished = true;
                FinishMinigame(false, Score, Summary);
                return "SCANNER BURNED OUT - too many false positives.";
            }

            return $"That was a {echoLabel} - scanner overload! Integrity -1 (-{falseEchoPenalty})";
        }

        private string BuildSummary()
        {
            return "<b><color=#35E0FF>Sweep report:</color></b>" +
                $"\nCaches marked: {marked}/{TotalCaches}   •   Clean reads: {freshMarks}   •   False positives: {falseHits}" +
                "\n\n<b>Signal legend:</b>" +
                $"\n<color=#36E0FF>{cacheLabel}</color> - a genuine signal: steady pulse, sharp shape. Mark it." +
                $"\n<color=#FF5A47>{echoLabel}</color> - interference: nervous flicker, wobbling shape. Leave it." +
                "\n\n<b>Lesson:</b> false positives cost. You only ever saw what the last sweep lit up, " +
                "and the glow faded before you were sure - so the discipline was to read how a blip behaved " +
                "before reacting, not to click on reflex. Real SOC work is the same: alerts arrive as partial, " +
                "decaying signals mixed with noise, and reflexive response to every one is exactly the alert " +
                "fatigue attackers rely on. Verify, then act.";
        }
    }
}
