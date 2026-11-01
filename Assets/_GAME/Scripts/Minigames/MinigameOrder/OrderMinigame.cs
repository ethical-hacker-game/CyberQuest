using System.Collections.Generic;
using UnityEngine;

namespace Minigames
{
    /// <summary>
    /// "Sequence Order" &mdash; a turn-based recall puzzle. A canonical cyber-security
    /// sequence (OSI layers, the Cyber Kill Chain, incident-response phases...) is shown
    /// as a shuffled pool of terms on the left; the player clicks terms to place them
    /// into the numbered slots on the right, then presses CHECK ORDER. Correct positions
    /// LOCK in place, wrong ones bounce back to the pool &mdash; but every failed check
    /// burns one of a small budget, so brute-forcing slot by slot loses. Nothing runs on
    /// Update: the game rewards actually knowing the ordering, not reflexes, in line
    /// with Firewall, Trace and Decrypt.
    /// </summary>
    public class OrderMinigame : MinigameBehaviour
    {
        private static OrderMinigame minigameInstance;

        [Header("Scenario pool (one is picked at random - a task can override via OrderMinigameSettings)")]
        [SerializeField] private List<OrderScenario> scenarios = new List<OrderScenario>();

        [Header("Attempts")]
        [SerializeField, Tooltip("How many CHECK ORDER attempts are allowed before the console locks out (= loss). Difficulty adjusts this by +2 (Easy) / -1 (Hard).")]
        private int checksAllowed = 3;

        [Header("Scoring")]
        [SerializeField, Tooltip("Points awarded for each position that locks in correctly.")]
        private int pointsPerCorrectSlot = 100;
        [SerializeField, Tooltip("Points removed for a failed check (magnitude, subtracted; scaled by difficulty).")]
        private int failedCheckPenalty = 75;
        [SerializeField, Tooltip("Bonus for reconstructing the whole sequence.")]
        private int solveBonus = 300;
        [SerializeField, Tooltip("Bonus per second left on the clock when the sequence is solved.")]
        private int timeBonusPerSecond = 5;
        [SerializeField, Tooltip("Bonus per unused check at the moment of victory (rewards deliberate play).")]
        private int bonusPerCheckLeft = 100;

        // The active puzzle. items is the CORRECT order, so item i belongs in slot i.
        private string[] items = new string[0];
        private readonly List<int> poolOrder = new List<int>(); // Shuffled display order of the pool.
        private int[] slotToItem = new int[0];                  // Item index per slot, -1 = empty.
        private bool[] slotLocked = new bool[0];
        private bool[] itemPlaced = new bool[0];

        private int checksLeft;
        private int failedChecks;
        private bool solved;
        private bool failed;
        private float penaltyMultiplier = 1f;

        private string activeScenarioName;
        private string headline = string.Empty;
        private string lesson = string.Empty;

        // Previous pool pick, so back-to-back plays avoid repeating a scenario. Static on
        // purpose: minigame instances are destroyed between plays.
        private static int lastScenarioIndex = -1;

        public static OrderMinigame MinigameInstance
        {
            get
            {
                if (minigameInstance == null)
                    Debug.LogError("minigameInstance does not exist");

                return minigameInstance;
            }
        }

        public int ItemCount => items.Length;
        public IReadOnlyList<int> PoolOrder => poolOrder;
        public int ChecksLeft => checksLeft;
        public string Headline => headline;
        public bool IsDone => solved || failed;

        public override string Summary => BuildSummary();

        public string GetItemText(int itemIndex) => items[itemIndex];
        public bool IsItemPlaced(int itemIndex) => itemPlaced[itemIndex];
        public int GetSlotItem(int slotIndex) => slotToItem[slotIndex];
        public bool IsSlotLocked(int slotIndex) => slotLocked[slotIndex];

        public int LockedCount
        {
            get
            {
                int locked = 0;

                for (int i = 0; i < slotLocked.Length; i++)
                {
                    if (slotLocked[i])
                        locked++;
                }

                return locked;
            }
        }

        public bool AllSlotsFilled
        {
            get
            {
                for (int i = 0; i < slotToItem.Length; i++)
                {
                    if (slotToItem[i] == -1)
                        return false;
                }

                return slotToItem.Length > 0;
            }
        }

        private void Awake()
        {
            minigameInstance = this;
            checksLeft = checksAllowed;

            // Seed from the prefab pool so the test path (SetupMinigameTest, which never
            // calls ApplyConfig) still has a working puzzle. ApplyConfig re-seeds on override.
            SeedFromPool(MinigameDifficulty.Default);
        }

        private void OnDestroy()
        {
            if (minigameInstance == this)
                minigameInstance = null;
        }

        public override void ApplyConfig(TaskMinigameVariant config)
        {
            // Runs after Awake (prefab pool already seeded) but before Start and before
            // the UI is instantiated, so replacing the puzzle and re-tuning difficulty
            // here is safe - nothing has read the state yet.
            base.ApplyConfig(config);

            OrderMinigameSettings settings = config != null ? config.orderSettings : null;

            if (settings != null && settings.checksAllowed > 0)
                checksAllowed = settings.checksAllowed;

            if (settings != null && settings.HasScenarioPool)
            {
                // Task-authored pool replaces the prefab pool; the usual random pick
                // (filtered by the task's difficulty) then runs on top of it.
                scenarios = settings.scenarios;
                SeedFromPool(Difficulty);
            }
            else if (Difficulty != MinigameDifficulty.Default)
            {
                // Re-pick so the difficulty filter can steer which scenario is used.
                SeedFromPool(Difficulty);
            }

            ApplyDifficulty();
        }

        private void ApplyDifficulty()
        {
            switch (Difficulty)
            {
                case MinigameDifficulty.Easy:
                    checksLeft = checksAllowed + 2;
                    penaltyMultiplier = 0.5f;
                    LockAnchor(); // First position pre-locked as a free reference point.
                    break;
                case MinigameDifficulty.Hard:
                    checksLeft = Mathf.Max(1, checksAllowed - 1);
                    penaltyMultiplier = 1.5f;
                    break;
                case MinigameDifficulty.Normal:
                    checksLeft = checksAllowed;
                    penaltyMultiplier = 1f;
                    break;
                default: // Default - keep the prefab values.
                    checksLeft = checksAllowed;
                    penaltyMultiplier = 1f;
                    break;
            }
        }

        /// <summary>Easy-mode hint: places and locks the first item so the player has an anchor.</summary>
        private void LockAnchor()
        {
            if (items.Length == 0 || slotLocked[0])
                return;

            slotToItem[0] = 0;
            slotLocked[0] = true;
            itemPlaced[0] = true;
        }

        /// <summary>
        /// Picks a random scenario from the pool - preferring ones tagged for the given
        /// difficulty and avoiding an immediate repeat when possible - and seeds the puzzle.
        /// </summary>
        private void SeedFromPool(MinigameDifficulty filter)
        {
            List<int> eligible = new List<int>();

            for (int i = 0; i < scenarios.Count; i++)
            {
                OrderScenario scenario = scenarios[i];

                if (scenario == null || scenario.HasSequence == false)
                    continue;

                if (filter == MinigameDifficulty.Default
                    || scenario.recommendedDifficulty == MinigameDifficulty.Default
                    || scenario.recommendedDifficulty == filter)
                {
                    eligible.Add(i);
                }
            }

            // Nothing matches the difficulty tag -> fall back to every playable scenario.
            if (eligible.Count == 0)
            {
                for (int i = 0; i < scenarios.Count; i++)
                {
                    if (scenarios[i] != null && scenarios[i].HasSequence)
                        eligible.Add(i);
                }
            }

            if (eligible.Count == 0)
            {
                Debug.LogError("[OrderMinigame] Scenario pool is empty - author at least one scenario on the prefab.");
                return;
            }

            // Don't serve the same scenario twice in a row (unless it is the only one).
            if (eligible.Count > 1)
                eligible.Remove(lastScenarioIndex);

            int pick = eligible[Random.Range(0, eligible.Count)];
            lastScenarioIndex = pick;

            SeedScenario(scenarios[pick]);
        }

        /// <summary>
        /// Rebuilds the working state from a scenario: copies the correct order, shuffles
        /// the pool (never into the already-correct arrangement) and resets the board.
        /// Idempotent - safe to call from both Awake and ApplyConfig.
        /// </summary>
        private void SeedScenario(OrderScenario scenario)
        {
            activeScenarioName = scenario.scenarioName;
            headline = scenario.headline != null ? scenario.headline.Trim() : string.Empty;
            lesson = scenario.lesson != null ? scenario.lesson.Trim() : string.Empty;

            items = new string[scenario.items.Count];

            for (int i = 0; i < items.Length; i++)
                items[i] = scenario.items[i].Trim();

            slotToItem = new int[items.Length];
            slotLocked = new bool[items.Length];
            itemPlaced = new bool[items.Length];

            for (int i = 0; i < slotToItem.Length; i++)
                slotToItem[i] = -1;

            solved = false;
            failed = false;
            failedChecks = 0;

            ShufflePool();
        }

        /// <summary>Fisher-Yates over the pool display order, retried so the pool never spells out the answer top-to-bottom.</summary>
        private void ShufflePool()
        {
            poolOrder.Clear();

            for (int i = 0; i < items.Length; i++)
                poolOrder.Add(i);

            for (int attempt = 0; attempt < 20; attempt++)
            {
                for (int i = poolOrder.Count - 1; i > 0; i--)
                {
                    int j = Random.Range(0, i + 1);
                    (poolOrder[i], poolOrder[j]) = (poolOrder[j], poolOrder[i]);
                }

                bool isIdentity = true;

                for (int i = 0; i < poolOrder.Count; i++)
                {
                    if (poolOrder[i] != i)
                    {
                        isIdentity = false;
                        break;
                    }
                }

                if (isIdentity == false)
                    return;
            }
        }

        // --- Player actions (called from the UI) ---

        /// <summary>Places a pool term into the lowest empty slot. Free - only checks cost.</summary>
        public bool PlaceItem(int itemIndex)
        {
            if (IsDone || IsPaused)
                return false;

            if (itemIndex < 0 || itemIndex >= items.Length || itemPlaced[itemIndex])
                return false;

            for (int slot = 0; slot < slotToItem.Length; slot++)
            {
                if (slotToItem[slot] == -1)
                {
                    slotToItem[slot] = itemIndex;
                    itemPlaced[itemIndex] = true;
                    return true;
                }
            }

            return false;
        }

        /// <summary>Returns the term in the given slot to the pool (locked slots stay). Free.</summary>
        public bool RemoveFromSlot(int slotIndex)
        {
            if (IsDone || IsPaused)
                return false;

            if (slotIndex < 0 || slotIndex >= slotToItem.Length)
                return false;

            if (slotLocked[slotIndex] || slotToItem[slotIndex] == -1)
                return false;

            itemPlaced[slotToItem[slotIndex]] = false;
            slotToItem[slotIndex] = -1;
            return true;
        }

        /// <summary>Returns every unlocked term to the pool. Free.</summary>
        public void ResetUnlocked()
        {
            if (IsDone || IsPaused)
                return;

            for (int slot = 0; slot < slotToItem.Length; slot++)
            {
                if (slotLocked[slot] == false && slotToItem[slot] != -1)
                {
                    itemPlaced[slotToItem[slot]] = false;
                    slotToItem[slot] = -1;
                }
            }
        }

        /// <summary>
        /// Grades the board. Correct positions LOCK (scored once), wrong ones bounce back
        /// to the pool. A check that does not finish the sequence costs one attempt from
        /// the budget - run out and the console locks (loss).
        /// </summary>
        public OrderCheckResult CheckOrder()
        {
            OrderCheckResult result = new OrderCheckResult();

            if (IsDone || IsPaused)
                return result;

            if (AllSlotsFilled == false)
            {
                result.handled = true;
                result.message = "Place every term into a slot before checking the order.";
                return result;
            }

            result.handled = true;

            int lockedNow = 0;
            int bouncedNow = 0;

            for (int slot = 0; slot < slotToItem.Length; slot++)
            {
                if (slotLocked[slot])
                    continue;

                // items is stored in the correct order, so slot i expects item i.
                if (slotToItem[slot] == slot)
                {
                    slotLocked[slot] = true;
                    lockedNow++;
                    ChangeScore(pointsPerCorrectSlot);
                }
                else
                {
                    itemPlaced[slotToItem[slot]] = false;
                    slotToItem[slot] = -1;
                    bouncedNow++;
                }
            }

            result.lockedNew = lockedNow > 0;

            if (LockedCount >= items.Length)
            {
                solved = true;
                result.solved = true;

                int timeBonus = Mathf.RoundToInt(MinigamesController.GetTimerPositive) * timeBonusPerSecond;
                ChangeScore(solveBonus + timeBonus + checksLeft * bonusPerCheckLeft);

                result.message = "Sequence reconstructed - every position is correct.";
                FinishMinigame(true, Score, Summary);
                return result;
            }

            checksLeft--;
            failedChecks++;
            ChangeScore(-Mathf.RoundToInt(failedCheckPenalty * penaltyMultiplier));

            if (checksLeft <= 0)
            {
                failed = true;
                result.failed = true;
                result.message = $"{lockedNow} locked, {bouncedNow} wrong - and that was the last check. The console locked out.";
                FinishMinigame(false, Score, Summary);
                return result;
            }

            result.message = $"{lockedNow} newly locked, {bouncedNow} returned to the pool. " +
                $"{LockedCount} of {items.Length} positions are set. ({ChecksText(checksLeft)} left.)";
            return result;
        }

        // --- Helpers ---

        private static string ChecksText(int checks) => checks == 1 ? "1 check" : $"{checks} checks";

        private string BuildSummary()
        {
            // Neon palette, kept in sync with OrderMinigameUI: mint = success,
            // hot magenta = failure, cyan = accents.
            string okColor = ColorUtility.ToHtmlStringRGB(new Color(0.15f, 0.95f, 0.70f));
            string badColor = ColorUtility.ToHtmlStringRGB(new Color(1f, 0.30f, 0.60f));
            string accent = ColorUtility.ToHtmlStringRGB(new Color(0.20f, 0.90f, 1f));

            string summary = string.IsNullOrEmpty(activeScenarioName)
                ? string.Empty
                : $"<size=85%><i>Scenario: {activeScenarioName}</i></size>\n\n";

            if (solved)
            {
                summary += $"<color=#{okColor}><b>Sequence reconstructed:</b></color>\n";

                for (int i = 0; i < items.Length; i++)
                    summary += $"{i + 1}. {items[i]}\n";

                summary += $"\nFailed checks: {failedChecks}, checks left: {checksLeft}.";
            }
            else
            {
                summary += $"<color=#{badColor}><b>The sequence was not reconstructed - " +
                    $"{LockedCount} of {items.Length} positions were locked in.</b></color>\n";
                summary += "\n<b>Correct order:</b>\n";

                for (int i = 0; i < items.Length; i++)
                {
                    string marker = slotLocked[i] ? $" <color=#{accent}>(you had this one)</color>" : string.Empty;
                    summary += $"{i + 1}. {items[i]}{marker}\n";
                }
            }

            if (string.IsNullOrEmpty(lesson) == false)
                summary += $"\n<size=85%>Lesson: {lesson}</size>";

            return summary;
        }
    }
}
