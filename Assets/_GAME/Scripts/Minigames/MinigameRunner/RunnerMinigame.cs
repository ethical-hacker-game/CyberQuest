using System.Collections.Generic;
using UnityEngine;

namespace Minigames
{
    /// <summary>
    /// "Packet Rush" &mdash; a three-lane runner where the player IS the packet: an urgent
    /// security patch racing down a backbone route toward a quarantined server. Hostile
    /// infrastructure drops into the fibers &mdash; FIREWALL deny rules, MALWARE-riddled
    /// nodes, SNIFFER taps &mdash; and every lane switch is a re-routing decision. Contact
    /// with a hazard costs 1 packet integrity; at zero the packet is dropped (loss).
    /// Surviving until the delivery window closes delivers the payload
    /// (<c>resultOnTimeout = true</c> like Connections). DATA shards give points and green
    /// PATCH pickups restore integrity.
    ///
    /// The rules (spawning, integrity, scoring) live here; the view
    /// (<see cref="RunnerMinigameUI"/>) instantiates and moves the entity widgets and
    /// reports contacts/passes back through <see cref="ResolveEntity"/>. The wave
    /// generator always keeps one hazard-free lane and only shifts that safe lane by one
    /// step between waves, so a clean route is guaranteed to exist &mdash; losing is always
    /// a routing mistake, never bad luck.
    ///
    /// Didactic hook: packet-switched networks survive damage because traffic can be
    /// re-routed around failed or hostile hops &mdash; what OSPF/BGP and failover links do
    /// automatically.
    /// </summary>
    public class RunnerMinigame : MinigameBehaviour
    {
        public const int LaneCount = 3;

        private static RunnerMinigame minigameInstance;

        [Header("Packet")]
        [SerializeField, Tooltip("How many hazard hits the packet survives. Difficulty adds +1 (Easy) / -1 (Hard).")]
        private int packetIntegrity = 3;

        [Header("Flow (speeds are in lane-field heights per second)")]
        [SerializeField, Tooltip("Scroll speed right after the start.")]
        private float startSpeed = 0.35f;
        [SerializeField, Tooltip("Scroll speed once the ramp is complete.")]
        private float maxSpeed = 0.8f;
        [SerializeField, Tooltip("Seconds of play over which the speed ramps from start to max.")]
        private float speedRampSeconds = 45f;
        [SerializeField, Tooltip("Seconds between waves at start speed. The effective interval shrinks with the speed ramp so on-screen density stays constant.")]
        private float spawnInterval = 1.5f;
        [SerializeField, Tooltip("The effective interval never drops below this.")]
        private float minSpawnInterval = 0.85f;

        [Header("Hazards & pickups")]
        [SerializeField, Range(0f, 1f), Tooltip("Chance that each non-safe lane of a wave actually gets a hazard (at least one always spawns).")]
        private float hazardChance = 0.6f;
        [SerializeField, Range(0f, 1f), Tooltip("Chance that a wave also carries a DATA shard on a hazard-free lane.")]
        private float dataShardChance = 0.35f;
        [SerializeField, Range(0f, 1f), Tooltip("Chance that a wave carries a PATCH pickup (only rolled while integrity is below max).")]
        private float patchChance = 0.12f;

        [Header("Scoring")]
        [SerializeField, Tooltip("Points for every hazard that scrolls past without a hit (one successful re-route).")]
        private int pointsPerDodge = 15;
        [SerializeField, Tooltip("Points for collecting a DATA shard.")]
        private int dataShardPoints = 120;
        [SerializeField, Tooltip("Points for collecting a PATCH (on top of the integrity restore).")]
        private int patchPoints = 60;
        [SerializeField, Tooltip("Points removed on a hazard hit (magnitude, subtracted).")]
        private int hitPenalty = 90;

        private int currentLane = 1;
        private int safeLane = 1;
        private int maxIntegrity;
        private int currentIntegrity;

        private float speedMultiplier = 1f;
        private float elapsed;
        private float spawnTimer;
        private bool spawningStarted;
        private bool finished;

        private int dodges;
        private int hitsTaken;
        private int shardsCollected;
        private int patchesCollected;

        private readonly Queue<RunnerWave> pendingWaves = new Queue<RunnerWave>();

        public static RunnerMinigame MinigameInstance
        {
            get
            {
                if (minigameInstance == null)
                    Debug.LogError("minigameInstance does not exist");

                return minigameInstance;
            }
        }

        public int CurrentLane => currentLane;
        public int MaxIntegrity => maxIntegrity;
        public int CurrentIntegrity => currentIntegrity;
        public int Dodges => dodges;
        public bool IsFinished => finished;

        /// <summary>Current scroll speed in lane-field heights per second.</summary>
        public float ScrollSpeed
        {
            get
            {
                float ramp = speedRampSeconds > 0f ? Mathf.Clamp01(elapsed / speedRampSeconds) : 1f;
                return Mathf.Lerp(startSpeed, maxSpeed, ramp) * speedMultiplier;
            }
        }

        private float CurrentSpawnInterval
        {
            get
            {
                // Keep the on-screen gap between waves constant while the world speeds up.
                float speed = ScrollSpeed;
                float baseline = startSpeed * speedMultiplier;
                float interval = speed > 0f && baseline > 0f ? spawnInterval * (baseline / speed) : spawnInterval;
                return Mathf.Max(minSpawnInterval, interval);
            }
        }

        public override string Summary => BuildSummary();

        private void Awake()
        {
            minigameInstance = this;
            maxIntegrity = Mathf.Max(1, packetIntegrity);
            currentIntegrity = maxIntegrity;
        }

        private void OnDestroy()
        {
            minigameInstance = null;
        }

        public override void ApplyConfig(TaskMinigameVariant config)
        {
            // Runs after Awake but before Start/Update, so integrity and flow values are
            // re-derived before the first wave spawns or the UI reads them.
            base.ApplyConfig(config);

            ApplyDifficulty();

            if (config != null && config.runnerSettings != null)
            {
                RunnerMinigameSettings settings = config.runnerSettings;

                if (settings.packetIntegrity > 0)
                    packetIntegrity = settings.packetIntegrity;

                if (settings.speedMultiplier > 0f)
                    speedMultiplier *= settings.speedMultiplier;

                if (settings.spawnInterval > 0f)
                    spawnInterval = settings.spawnInterval;

                if (settings.hazardChance > 0f)
                    hazardChance = Mathf.Clamp01(settings.hazardChance);
            }

            maxIntegrity = Mathf.Max(1, packetIntegrity);
            currentIntegrity = maxIntegrity;
        }

        private void ApplyDifficulty()
        {
            if (Difficulty == MinigameDifficulty.Default)
                return;

            int integrityBonus;
            float speedFactor;
            float intervalFactor;
            float hazardFactor;

            switch (Difficulty)
            {
                case MinigameDifficulty.Easy:
                    integrityBonus = 1;
                    speedFactor = 0.85f;
                    intervalFactor = 1.2f;
                    hazardFactor = 0.8f;
                    break;
                case MinigameDifficulty.Hard:
                    integrityBonus = -1;
                    speedFactor = 1.2f;
                    intervalFactor = 0.85f;
                    hazardFactor = 1.25f;
                    break;
                default: // Normal
                    integrityBonus = 0;
                    speedFactor = 1f;
                    intervalFactor = 1f;
                    hazardFactor = 1f;
                    break;
            }

            packetIntegrity = Mathf.Max(1, packetIntegrity + integrityBonus);
            speedMultiplier *= speedFactor;
            spawnInterval *= intervalFactor;
            minSpawnInterval *= intervalFactor;
            hazardChance = Mathf.Clamp(hazardChance * hazardFactor, 0.05f, 0.95f);
        }

        /// <summary>
        /// Advances the flow clock and enqueues waves when due. Driven from
        /// <see cref="RunnerMinigameUI"/>'s Update (the view is guaranteed to tick while
        /// the game is live) rather than this component's own Update, so wave generation
        /// keeps exact step with the lane movement the player already sees - no dependence
        /// on a second MonoBehaviour's update order. The first wave is enqueued immediately
        /// so the field is never empty at the start.
        /// </summary>
        public void AdvanceSpawning(float deltaTime)
        {
            if (isPaused || finished)
                return;

            if (spawningStarted == false)
            {
                spawningStarted = true;
                pendingWaves.Enqueue(BuildWave());
                return;
            }

            elapsed += deltaTime;
            spawnTimer += deltaTime;

            if (spawnTimer >= CurrentSpawnInterval)
            {
                spawnTimer = 0f;
                pendingWaves.Enqueue(BuildWave());
            }
        }

        /// <summary>Lane-switch request coming from the UI buttons / keyboard.</summary>
        public bool TryMove(int direction)
        {
            if (isPaused || finished)
                return false;

            int target = Mathf.Clamp(currentLane + (direction < 0 ? -1 : 1), 0, LaneCount - 1);

            if (target == currentLane)
                return false;

            currentLane = target;
            return true;
        }

        /// <summary>The UI drains this queue every frame and materializes the widgets.</summary>
        public bool TryDequeueWave(out RunnerWave wave)
        {
            if (pendingWaves.Count > 0)
            {
                wave = pendingWaves.Dequeue();
                return true;
            }

            wave = null;
            return false;
        }

        /// <summary>
        /// Called by the UI when an entity either touches the packet (<paramref name="collided"/>)
        /// or scrolls past it untouched. Mutates integrity/score and returns the feedback
        /// line to show (empty for silent events like a plain dodge).
        /// </summary>
        public string ResolveEntity(RunnerEntityKind kind, bool collided)
        {
            if (finished)
                return string.Empty;

            if (RunnerEntity.IsHazard(kind))
            {
                if (collided == false)
                {
                    dodges++;
                    ChangeScore(pointsPerDodge);
                    return string.Empty;
                }

                hitsTaken++;
                currentIntegrity--;
                ChangeScore(-hitPenalty);

                if (currentIntegrity <= 0)
                {
                    currentIntegrity = 0;
                    finished = true;
                    FinishMinigame(false, Score, Summary);
                    return "PACKET DROPPED - integrity exhausted.";
                }

                switch (kind)
                {
                    case RunnerEntityKind.Firewall:
                        return "FIREWALL - a deny rule dropped part of the payload! Integrity -1";
                    case RunnerEntityKind.MalwareNode:
                        return "MALWARE NODE - payload corrupted in transit! Integrity -1";
                    default:
                        return "SNIFFER TAP - payload exposed to eavesdropping! Integrity -1";
                }
            }

            // Pickups: collected on contact, silently ignored when missed.
            if (collided == false)
                return string.Empty;

            if (kind == RunnerEntityKind.DataShard)
            {
                shardsCollected++;
                ChangeScore(dataShardPoints);
                return $"Data shard recovered (+{dataShardPoints})";
            }

            patchesCollected++;
            ChangeScore(patchPoints);

            if (currentIntegrity < maxIntegrity)
                currentIntegrity++;

            return "Hot-patch applied - integrity restored";
        }

        /// <summary>
        /// One row of spawns. The safe (hazard-free) lane drifts by at most one lane per
        /// wave, so following it never needs more than one switch between waves.
        /// </summary>
        private RunnerWave BuildWave()
        {
            safeLane = Mathf.Clamp(safeLane + Random.Range(-1, 2), 0, LaneCount - 1);

            RunnerWave wave = new RunnerWave();
            List<int> freeLanes = new List<int> { safeLane };
            List<int> blockedLanes = new List<int>();

            for (int lane = 0; lane < LaneCount; lane++)
            {
                if (lane == safeLane)
                    continue;

                if (Random.value <= hazardChance)
                {
                    wave.spawns.Add(new RunnerSpawn(RandomHazard(), lane));
                    blockedLanes.Add(lane);
                }
                else
                {
                    freeLanes.Add(lane);
                }
            }

            // An all-clear row teaches nothing - always drop at least one hazard.
            if (blockedLanes.Count == 0)
            {
                int lane = freeLanes[Random.Range(1, freeLanes.Count)]; // index 0 is the safe lane
                freeLanes.Remove(lane);
                wave.spawns.Add(new RunnerSpawn(RandomHazard(), lane));
            }

            if (freeLanes.Count > 0 && Random.value <= dataShardChance)
            {
                int lane = freeLanes[Random.Range(0, freeLanes.Count)];
                freeLanes.Remove(lane);
                wave.spawns.Add(new RunnerSpawn(RunnerEntityKind.DataShard, lane));
            }

            if (freeLanes.Count > 0 && currentIntegrity < maxIntegrity && Random.value <= patchChance)
            {
                int lane = freeLanes[Random.Range(0, freeLanes.Count)];
                wave.spawns.Add(new RunnerSpawn(RunnerEntityKind.Patch, lane));
            }

            return wave;
        }

        private RunnerEntityKind RandomHazard()
        {
            int roll = Random.Range(0, 3);

            switch (roll)
            {
                case 0: return RunnerEntityKind.Firewall;
                case 1: return RunnerEntityKind.MalwareNode;
                default: return RunnerEntityKind.Sniffer;
            }
        }

        private string BuildSummary()
        {
            string report = "<b><color=#35E0FF>Delivery report:</color></b>" +
                $"\nRe-routes: {dodges}   •   Hits absorbed: {hitsTaken}   •   Data shards: {shardsCollected}   •   Patches: {patchesCollected}" +
                "\n\n<b>Hazard legend:</b>" +
                "\n<color=#FF3D5A>FIREWALL</color> - a misconfigured deny rule that drops legitimate traffic" +
                "\n<color=#C438FF>MALWARE NODE</color> - a compromised router corrupting packets in transit" +
                "\n<color=#FF9E2C>SNIFFER</color> - an eavesdropping tap capturing unencrypted payloads" +
                "\n\n<b>Lesson:</b> every lane switch was a routing decision. Packet-switched networks survive " +
                "damage because traffic can be re-routed around failed or hostile hops - exactly what dynamic " +
                "routing protocols (OSPF, BGP) and failover links do automatically. Redundant paths and " +
                "segmentation keep the payload moving even while parts of the network are compromised.";

            return report;
        }
    }
}
