using System.Collections.Generic;
using UnityEngine;

namespace Minigames
{
    /// <summary>
    /// "Patch Hunt" &mdash; an isometric neon server room where red BREACH eruptions burst
    /// out of a rack grid, each with a shrinking timer ring. The player aims with the
    /// mouse and clicks a breach before its ring closes to patch it. Green HONEYPOT
    /// decoys look almost identical: clicking one costs points and resets the combo
    /// (integrity is untouched &mdash; the penalty teaches, it does not frustrate). A breach
    /// that runs out its ring slips through and costs 1 integrity; at zero the run is
    /// lost. Surviving until the timer ends wins (<c>resultOnTimeout = true</c>).
    ///
    /// The rules (spawning, occupancy, integrity, combo, scoring) live here; the view
    /// (<see cref="PatchHuntMinigameWorld"/>) grows the targets, shrinks the rings and
    /// reports clicks/timeouts back through <see cref="ResolveTarget"/>. Fairness: never
    /// more than <see cref="maxSimultaneous"/> live targets, and no breach ever spawns
    /// with a lifetime shorter than the cursor-travel floor.
    ///
    /// Didactic hook: triage &mdash; look before you click. Incident responders verify an
    /// alert before acting on it, because reflexive reactions are exactly what decoys,
    /// noise floods and alert fatigue exploit.
    /// </summary>
    public class PatchHuntMinigame : MinigameBehaviour
    {
        private const int ComboTier1Streak = 5;
        private const int ComboTier2Streak = 10;
        private const float ComboTier1Multiplier = 1.5f;
        private const float ComboTier2Multiplier = 2f;

        private static PatchHuntMinigame minigameInstance;

        [Header("Server room grid")]
        [SerializeField, Tooltip("Rack columns of the grid (the view lays them out).")]
        private int gridColumns = 4;
        [SerializeField, Tooltip("Rack rows of the grid.")]
        private int gridRows = 3;

        [Header("Integrity")]
        [SerializeField, Tooltip("How many breaches may slip through. Difficulty adds +1 (Easy) / -1 (Hard).")]
        private int integrity = 3;

        [Header("Flow")]
        [SerializeField, Tooltip("Seconds between spawns at the start. Shrinks toward the minimum over the ramp.")]
        private float spawnInterval = 1.7f;
        [SerializeField, Tooltip("The spawn interval never drops below this.")]
        private float minSpawnInterval = 0.75f;
        [SerializeField, Tooltip("Seconds of play over which the interval ramps from start to minimum.")]
        private float spawnRampSeconds = 40f;
        [SerializeField, Tooltip("Seconds a target stays alive before its ring closes. Difficulty scales it.")]
        private float breachLifetime = 2.6f;
        [SerializeField, Tooltip("Fairness floor: no target ever spawns with a shorter life than this (cursor travel time).")]
        private float minBreachLifetime = 1.2f;
        [SerializeField, Range(0f, 1f), Tooltip("Chance that a spawn is a honeypot decoy instead of a breach (the first spawn is always a breach).")]
        private float honeypotChance = 0.25f;
        [SerializeField, Tooltip("Fairness cap: never more live targets than this.")]
        private int maxSimultaneous = 3;

        [Header("Scoring")]
        [SerializeField, Tooltip("Base points for patching a breach.")]
        private int pointsPerPatch = 100;
        [SerializeField, Tooltip("Extra points scaled by the fraction of the ring still left when clicked.")]
        private int speedBonusMax = 60;
        [SerializeField, Tooltip("Points removed when a breach slips through (on top of the integrity loss).")]
        private int missPenalty = 40;
        [SerializeField, Tooltip("Points removed for clicking a honeypot decoy (integrity untouched, combo resets).")]
        private int honeypotPenalty = 120;

        [Header("Story skin (overridable per task)")]
        [SerializeField, Tooltip("What a red target is called in feedback and the summary.")]
        private string breachLabel = "BREACH";
        [SerializeField, Tooltip("What a green decoy is called in feedback and the summary.")]
        private string honeypotLabel = "HONEYPOT";

        private int maxIntegrity;
        private int currentIntegrity;

        private float elapsed;
        private float spawnTimer;
        private bool spawningStarted;
        private bool finished;

        private int combo;
        private int bestCombo;
        private int patched;
        private int leaked;
        private int honeypotsClicked;
        private int honeypotsAvoided;
        private int activeTargets;

        private readonly Queue<PatchHuntSpawn> pendingSpawns = new Queue<PatchHuntSpawn>();
        private readonly HashSet<int> occupiedRacks = new HashSet<int>();

        public static PatchHuntMinigame MinigameInstance
        {
            get
            {
                if (minigameInstance == null)
                    Debug.LogError("minigameInstance does not exist");

                return minigameInstance;
            }
        }

        public int GridColumns => gridColumns;
        public int GridRows => gridRows;
        public int RackCount => gridColumns * gridRows;
        public int MaxIntegrity => maxIntegrity;
        public int CurrentIntegrity => currentIntegrity;
        public int Combo => combo;
        public int Patched => patched;
        public bool IsFinished => finished;
        public string BreachLabel => breachLabel;
        public string HoneypotLabel => honeypotLabel;

        /// <summary>Score multiplier earned by the current clean-patch streak.</summary>
        public float ComboMultiplier
        {
            get
            {
                if (combo >= ComboTier2Streak)
                    return ComboTier2Multiplier;

                if (combo >= ComboTier1Streak)
                    return ComboTier1Multiplier;

                return 1f;
            }
        }

        private float CurrentSpawnInterval
        {
            get
            {
                float ramp = spawnRampSeconds > 0f ? Mathf.Clamp01(elapsed / spawnRampSeconds) : 1f;
                return Mathf.Max(minSpawnInterval, Mathf.Lerp(spawnInterval, minSpawnInterval, ramp));
            }
        }

        public override string Summary => BuildSummary();

        private void Awake()
        {
            minigameInstance = this;
            maxIntegrity = Mathf.Max(1, integrity);
            currentIntegrity = maxIntegrity;
        }

        private void OnDestroy()
        {
            minigameInstance = null;
        }

        public override void ApplyConfig(TaskMinigameVariant config)
        {
            // Runs after Awake but before Start/Update, so integrity and pacing are
            // re-derived before the first target spawns or the UI reads them.
            base.ApplyConfig(config);

            ApplyDifficulty();

            if (config != null && config.patchHuntSettings != null)
            {
                PatchHuntMinigameSettings settings = config.patchHuntSettings;

                if (settings.packetIntegrity > 0)
                    integrity = settings.packetIntegrity;

                if (settings.spawnInterval > 0f)
                {
                    spawnInterval = settings.spawnInterval;
                    minSpawnInterval = Mathf.Min(minSpawnInterval, settings.spawnInterval);
                }

                if (settings.breachLifetime > 0f)
                {
                    // An authored lifetime wins verbatim, so the fairness floor follows it down.
                    breachLifetime = settings.breachLifetime;
                    minBreachLifetime = Mathf.Min(minBreachLifetime, settings.breachLifetime);
                }

                if (settings.honeypotChance > 0f)
                    honeypotChance = Mathf.Clamp01(settings.honeypotChance);

                if (string.IsNullOrEmpty(settings.breachLabel) == false)
                    breachLabel = settings.breachLabel;

                if (string.IsNullOrEmpty(settings.honeypotLabel) == false)
                    honeypotLabel = settings.honeypotLabel;
            }

            maxIntegrity = Mathf.Max(1, integrity);
            currentIntegrity = maxIntegrity;
        }

        private void ApplyDifficulty()
        {
            if (Difficulty == MinigameDifficulty.Default)
                return;

            int integrityBonus;
            float intervalFactor;
            float lifeFactor;
            float honeypotFactor;

            switch (Difficulty)
            {
                case MinigameDifficulty.Easy:
                    // Pace x0.8 means longer gaps between spawns.
                    integrityBonus = 1;
                    intervalFactor = 1.25f;
                    lifeFactor = 1.25f;
                    honeypotFactor = 0.7f;
                    break;
                case MinigameDifficulty.Hard:
                    integrityBonus = -1;
                    intervalFactor = 0.8f;
                    lifeFactor = 0.8f;
                    honeypotFactor = 1.4f;
                    break;
                default: // Normal
                    integrityBonus = 0;
                    intervalFactor = 1f;
                    lifeFactor = 1f;
                    honeypotFactor = 1f;
                    break;
            }

            integrity = Mathf.Max(1, integrity + integrityBonus);
            spawnInterval *= intervalFactor;
            minSpawnInterval *= intervalFactor;
            breachLifetime *= lifeFactor;
            minBreachLifetime *= lifeFactor;
            honeypotChance = Mathf.Clamp(honeypotChance * honeypotFactor, 0.05f, 0.9f);
        }

        /// <summary>
        /// Advances the spawn clock and enqueues spawn orders when due. Driven from
        /// <see cref="PatchHuntMinigameUI"/>'s Update (the view is guaranteed to tick
        /// while the game is live) rather than this component's own Update - the same
        /// proven path Packet Rush uses. The first spawn fires immediately and is always
        /// a breach, so the opening seconds teach the core verb before the first decoy.
        /// </summary>
        public void AdvanceSpawning(float deltaTime)
        {
            if (isPaused || finished)
                return;

            if (spawningStarted == false)
            {
                spawningStarted = true;
                EnqueueSpawn(forceBreach: true);
                return;
            }

            elapsed += deltaTime;
            spawnTimer += deltaTime;

            if (spawnTimer >= CurrentSpawnInterval)
            {
                spawnTimer = 0f;
                EnqueueSpawn(forceBreach: false);
            }
        }

        /// <summary>The world view drains this queue every frame and grows the targets.</summary>
        public bool TryDequeueSpawn(out PatchHuntSpawn spawn)
        {
            if (pendingSpawns.Count > 0)
            {
                spawn = pendingSpawns.Dequeue();
                return true;
            }

            spawn = null;
            return false;
        }

        /// <summary>
        /// Called by the world view when a target is clicked (<paramref name="clicked"/>)
        /// or when its ring runs out. Frees the rack, mutates integrity/combo/score and
        /// returns the feedback line to show (empty for silent events like an expired
        /// honeypot). <paramref name="remainingLifeFraction"/> feeds the speed bonus.
        /// </summary>
        public string ResolveTarget(PatchHuntTargetKind kind, int rackIndex, bool clicked, float remainingLifeFraction)
        {
            if (finished)
                return string.Empty;

            occupiedRacks.Remove(rackIndex);
            activeTargets = Mathf.Max(0, activeTargets - 1);

            if (kind == PatchHuntTargetKind.Breach)
            {
                if (clicked)
                {
                    patched++;
                    combo++;
                    bestCombo = Mathf.Max(bestCombo, combo);

                    int speedBonus = Mathf.RoundToInt(speedBonusMax * Mathf.Clamp01(remainingLifeFraction));
                    int points = Mathf.RoundToInt((pointsPerPatch + speedBonus) * ComboMultiplier);
                    ChangeScore(points);

                    string comboNote = ComboMultiplier > 1f ? $"   •   COMBO x{ComboMultiplier:0.#}" : string.Empty;
                    return $"{breachLabel} patched (+{points}){comboNote}";
                }

                leaked++;
                combo = 0;
                currentIntegrity--;
                ChangeScore(-missPenalty);

                if (currentIntegrity <= 0)
                {
                    currentIntegrity = 0;
                    finished = true;
                    FinishMinigame(false, Score, Summary);
                    return "RACKS DARK - integrity exhausted.";
                }

                return $"{breachLabel} slipped through! Integrity -1";
            }

            // Honeypot: clicking it is the mistake, letting it expire is free.
            if (clicked)
            {
                honeypotsClicked++;
                combo = 0;
                ChangeScore(-honeypotPenalty);
                return $"{honeypotLabel}! That was a decoy (-{honeypotPenalty}, combo reset). Look before you click.";
            }

            honeypotsAvoided++;
            return string.Empty;
        }

        private void EnqueueSpawn(bool forceBreach)
        {
            // Fairness cap counts queued + live targets - the reservation happens here.
            if (activeTargets >= maxSimultaneous)
                return;

            int rack = PickFreeRack();

            if (rack < 0)
                return;

            PatchHuntTargetKind kind = forceBreach == false && Random.value <= honeypotChance
                ? PatchHuntTargetKind.Honeypot
                : PatchHuntTargetKind.Breach;

            float lifetime = Mathf.Max(minBreachLifetime, breachLifetime);

            occupiedRacks.Add(rack);
            activeTargets++;
            pendingSpawns.Enqueue(new PatchHuntSpawn(kind, rack, lifetime));
        }

        private int PickFreeRack()
        {
            int total = RackCount;
            int freeCount = total - occupiedRacks.Count;

            if (freeCount <= 0)
                return -1;

            int pick = Random.Range(0, freeCount);

            for (int rack = 0; rack < total; rack++)
            {
                if (occupiedRacks.Contains(rack))
                    continue;

                if (pick == 0)
                    return rack;

                pick--;
            }

            return -1;
        }

        private string BuildSummary()
        {
            string report = "<b><color=#35E0FF>Incident report:</color></b>" +
                $"\nPatched: {patched}   •   Slipped through: {leaked}   •   Decoys ignored: {honeypotsAvoided}   •   Decoys clicked: {honeypotsClicked}   •   Best combo: {bestCombo}" +
                "\n\n<b>Target legend:</b>" +
                $"\n<color=#FF3D5A>{breachLabel}</color> - a live incident: patch it before its timer ring closes" +
                $"\n<color=#3DFF87>{honeypotLabel}</color> - a decoy planted to bait reflexive clicks" +
                "\n\n<b>Lesson:</b> triage before action - look at what you are clicking. Real incident " +
                "response works the same way: responders verify an alert before reacting, because attackers " +
                "deliberately plant decoys and noise to exploit reflexes and alert fatigue. Recognizing what " +
                "does NOT need a response is as much a skill as patching what does.";

            return report;
        }
    }
}
