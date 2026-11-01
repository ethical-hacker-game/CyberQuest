using System.Collections.Generic;
using UnityEngine;

namespace Minigames
{
    /// <summary>
    /// "Decrypt the Intercept" &mdash; a turn-based codebreaking puzzle. An intercepted
    /// message has been wrapped in a chain of classic encodings (Caesar, Base64, hex,
    /// reversal, Atbash); the player peels the layers by applying the matching DECODE
    /// tools until the plaintext emerges. The free "signal analysis" panel teaches the
    /// real skill: recognising encoding signatures (Base64 charset + padding, hex digits,
    /// letter frequency for substitution ciphers) instead of guessing. Guessing is priced:
    /// every wrong decode costs points and one attempt from a small budget &mdash; run out
    /// and the intercept locks out (loss). Nothing runs on Update, so the game rewards
    /// reading and reasoning over reflexes, in line with Firewall and Trace.
    ///
    /// A decode is accepted when it matches one of the layers still on the message AND
    /// the remaining layers can still be peeled to reach the plaintext afterwards (checked
    /// by simulation over the remaining permutations). That makes mathematically
    /// equivalent orders legal &mdash; e.g. Caesar and Reverse commute, so decrypting a
    /// reversed Caesar text before un-reversing it is accepted &mdash; while peeling in a
    /// genuinely impossible order (Caesar "through" a Base64 wrapper) is rejected.
    /// </summary>
    public class DecryptMinigame : MinigameBehaviour
    {
        private static DecryptMinigame minigameInstance;

        [Header("Scenario pool (one is picked at random - a task can override via DecryptMinigameSettings)")]
        [SerializeField] private List<DecryptScenario> scenarios = new List<DecryptScenario>();

        [Header("Attempts")]
        [SerializeField, Tooltip("How many WRONG decode attempts are allowed before the intercept locks out (= loss). Difficulty adjusts this by +2 (Easy) / -2 (Hard).")]
        private int wrongAttemptsAllowed = 5;

        [Header("Presentation")]
        [SerializeField, Tooltip("When true the analysis panel shows how many encoding layers remain. Difficulty overrides this: Easy/Normal show it, Hard hides it.")]
        private bool layersVisibleByDefault = true;

        [Header("Scoring")]
        [SerializeField, Tooltip("Points awarded for each correctly removed encoding layer.")]
        private int pointsPerLayer = 150;
        [SerializeField, Tooltip("Points removed for a wrong decode attempt (magnitude, subtracted; scaled by difficulty).")]
        private int wrongAttemptPenalty = 60;
        [SerializeField, Tooltip("Bonus for fully recovering the plaintext.")]
        private int decryptBonus = 300;
        [SerializeField, Tooltip("Bonus per second left on the clock when the message is decrypted.")]
        private int timeBonusPerSecond = 6;
        [SerializeField, Tooltip("Bonus per unused wrong-attempt at the moment of victory (rewards deliberate play).")]
        private int bonusPerAttemptLeft = 50;

        private string plaintext = string.Empty;
        private string workingText = string.Empty;
        private readonly List<DecryptStep> remaining = new List<DecryptStep>();
        private readonly List<string> peeledLog = new List<string>();

        private int layersTotal;
        private int attemptsLeft;
        private int wrongAttemptsUsed;
        private bool solved;
        private bool failed;

        private bool layersVisible = true;
        private bool showSuggestedShift;
        private float penaltyMultiplier = 1f;

        // Which authored scenario is active this play (used by the result summary), plus
        // the previous pool pick so back-to-back plays avoid repeating a scenario. The
        // index is static on purpose: minigame instances are destroyed between plays.
        private string activeScenarioName;
        private static int lastScenarioIndex = -1;

        public static DecryptMinigame MinigameInstance
        {
            get
            {
                if (minigameInstance == null)
                    Debug.LogError("minigameInstance does not exist");

                return minigameInstance;
            }
        }

        public string WorkingText => workingText;
        public int AttemptsLeft => attemptsLeft;
        public int LayersRemaining => remaining.Count;
        public bool LayersVisible => layersVisible;
        public bool IsDone => solved || failed;

        public override string Summary => BuildSummary();

        private void Awake()
        {
            minigameInstance = this;
            attemptsLeft = wrongAttemptsAllowed;
            layersVisible = layersVisibleByDefault;

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

            DecryptMinigameSettings settings = config != null ? config.decryptSettings : null;

            if (settings != null && settings.wrongAttemptsAllowed > 0)
                wrongAttemptsAllowed = settings.wrongAttemptsAllowed;

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
                    attemptsLeft = wrongAttemptsAllowed + 2;
                    penaltyMultiplier = 0.5f;
                    layersVisible = true;
                    showSuggestedShift = true;   // Analysis panel computes the Caesar key for you.
                    break;
                case MinigameDifficulty.Hard:
                    attemptsLeft = Mathf.Max(2, wrongAttemptsAllowed - 2);
                    penaltyMultiplier = 1.5f;
                    layersVisible = false;       // No layer counter - read the signatures.
                    showSuggestedShift = false;
                    break;
                case MinigameDifficulty.Normal:
                    attemptsLeft = wrongAttemptsAllowed;
                    penaltyMultiplier = 1f;
                    layersVisible = true;
                    showSuggestedShift = false;
                    break;
                default: // Default - keep the prefab-authored presentation.
                    attemptsLeft = wrongAttemptsAllowed;
                    penaltyMultiplier = 1f;
                    layersVisible = layersVisibleByDefault;
                    showSuggestedShift = false;
                    break;
            }
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
                DecryptScenario scenario = scenarios[i];

                if (scenario == null || scenario.HasPuzzle == false)
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
                    if (scenarios[i] != null && scenarios[i].HasPuzzle)
                        eligible.Add(i);
                }
            }

            if (eligible.Count == 0)
            {
                Debug.LogError("[DecryptMinigame] Scenario pool is empty - author at least one scenario on the prefab.");
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
        /// Rebuilds the working state from a scenario: normalises the chain (Rot13 becomes
        /// Caesar 13), encodes the plaintext through every step and resets the peel state.
        /// Idempotent - safe to call from both Awake and ApplyConfig.
        /// </summary>
        private void SeedScenario(DecryptScenario scenario)
        {
            activeScenarioName = scenario.scenarioName;
            plaintext = scenario.plaintext != null ? scenario.plaintext.Trim() : string.Empty;

            remaining.Clear();
            peeledLog.Clear();
            solved = false;
            failed = false;

            string encoded = plaintext;

            foreach (DecryptStep authored in scenario.steps)
            {
                if (authored == null)
                    continue;

                // ROT13 would need a Caesar key of 13 - outside the small 1..MaxCaesarShift
                // picker - so the layer could never be peeled. Reject it at seed time.
                if (authored.type == DecryptStepType.Rot13)
                {
                    Debug.LogError($"[DecryptMinigame] Scenario '{scenario.scenarioName}' uses ROT13, which is not solvable with the Caesar key range 1-{DecryptCipher.MaxCaesarShift} - skipping the step.");
                    continue;
                }

                // Deep-copy so this play never mutates the authored asset. Clamp the key
                // into the picker's range so an authoring slip never makes the puzzle
                // unsolvable (the error still points at the content to fix).
                int shift = authored.caesarShift;

                if (authored.type == DecryptStepType.Caesar && (shift < 1 || shift > DecryptCipher.MaxCaesarShift))
                {
                    Debug.LogError($"[DecryptMinigame] Scenario '{scenario.scenarioName}' Caesar shift {shift} is outside 1-{DecryptCipher.MaxCaesarShift} - clamping.");
                    shift = Mathf.Clamp(shift, 1, DecryptCipher.MaxCaesarShift);
                }

                DecryptStep step = new DecryptStep
                {
                    type = authored.type,
                    caesarShift = shift,
                };

                string next = DecryptCipher.Encode(step, encoded);

                if (next == null)
                {
                    Debug.LogError($"[DecryptMinigame] Scenario '{scenario.scenarioName}' step {DecryptCipher.ToolLabel(step.type, step.caesarShift)} failed to encode - skipping the step.");
                    continue;
                }

                encoded = next;
                remaining.Add(step);
            }

            if (remaining.Count == 0)
                Debug.LogError($"[DecryptMinigame] Scenario '{scenario.scenarioName}' has no usable encoding steps - the puzzle is already solved.");

            workingText = encoded;
            layersTotal = remaining.Count;
        }

        // --- Player action (called from the UI) ---

        /// <summary>
        /// Applies one decode tool to the current text. Accepted when it matches a layer
        /// still on the message and the rest can still be peeled afterwards; otherwise it
        /// costs one attempt + points. The text NEVER turns into garbage - a rejected
        /// decode leaves it untouched, the price is the attempt.
        /// </summary>
        public DecryptApplyResult ApplyTool(DecryptStepType type, int caesarShift = 0)
        {
            DecryptApplyResult result = new DecryptApplyResult();

            if (IsDone || IsPaused)
                return result;

            result.handled = true;

            // Try every remaining layer this tool could match; accept the first one whose
            // removal still leaves a completable puzzle (permutation check by simulation).
            bool typePresent = false;

            for (int i = 0; i < remaining.Count; i++)
            {
                DecryptStep step = remaining[i];

                if (step.type != type)
                    continue;

                typePresent = true;

                if (type == DecryptStepType.Caesar && step.caesarShift != caesarShift)
                    continue;

                string decoded = DecryptCipher.Decode(step, workingText);

                if (decoded == null)
                    continue;

                List<DecryptStep> rest = new List<DecryptStep>(remaining);
                rest.RemoveAt(i);

                if (CanComplete(decoded, rest) == false)
                {
                    // The tool matches a layer, but not the one on the outside right now.
                    return Reject(result, $"{DecryptCipher.ToolLabel(type, caesarShift)} is part of the chain, but another encoding is layered on top - peel the outermost layer first.");
                }

                // Accepted - peel the layer.
                workingText = decoded;
                remaining.RemoveAt(i);
                peeledLog.Add(DecryptCipher.ToolLabel(step.type, step.caesarShift));
                ChangeScore(pointsPerLayer);

                result.accepted = true;

                if (remaining.Count == 0)
                {
                    solved = true;
                    result.solved = true;

                    int timeBonus = Mathf.RoundToInt(MinigamesController.GetTimerPositive) * timeBonusPerSecond;
                    ChangeScore(decryptBonus + timeBonus + attemptsLeft * bonusPerAttemptLeft);

                    result.message = "Plaintext recovered - intercept decrypted.";
                    FinishMinigame(true, Score, Summary);
                }
                else
                {
                    result.message = $"Layer removed: {DecryptCipher.ToolLabel(step.type, step.caesarShift)}."
                        + (layersVisible ? $" {LayersText(remaining.Count)} left." : string.Empty);
                }

                return result;
            }

            // No acceptable match -> a wrong attempt. Craft feedback that teaches.
            if (typePresent && type == DecryptStepType.Caesar)
                return Reject(result, $"Caesar shift {caesarShift} produced gibberish - wrong key. Check the letter-frequency line in the analysis panel.");

            return Reject(result, $"{DecryptCipher.ToolLabel(type, caesarShift)} decode produced garbage - the outermost layer is not {DecryptCipher.ToolLabel(type)}.");
        }

        private DecryptApplyResult Reject(DecryptApplyResult result, string message)
        {
            attemptsLeft--;
            wrongAttemptsUsed++;
            ChangeScore(-Mathf.RoundToInt(wrongAttemptPenalty * penaltyMultiplier));

            result.accepted = false;
            result.message = message + $" ({AttemptsText(attemptsLeft)} left.)";

            if (attemptsLeft <= 0)
            {
                failed = true;
                result.failed = true;
                result.message = message + " The intercept wiped itself - too many failed decodes.";
                FinishMinigame(false, Score, Summary);
            }

            return result;
        }

        /// <summary>
        /// True when SOME order of the given layers decodes the text down to the plaintext.
        /// Depth is the chain length (small), so trying the permutations is trivial.
        /// </summary>
        private bool CanComplete(string text, List<DecryptStep> steps)
        {
            if (text == null)
                return false;

            if (steps.Count == 0)
                return text == plaintext;

            for (int i = 0; i < steps.Count; i++)
            {
                string decoded = DecryptCipher.Decode(steps[i], text);

                if (decoded == null)
                    continue;

                List<DecryptStep> rest = new List<DecryptStep>(steps);
                rest.RemoveAt(i);

                if (CanComplete(decoded, rest))
                    return true;
            }

            return false;
        }

        // --- Analysis (free, always available - this is the educational core) ---

        /// <summary>
        /// Signature observations about the CURRENT text, difficulty-gated. The player
        /// reads these instead of brute-forcing tools.
        /// </summary>
        public string GetAnalysis()
        {
            if (solved)
                return "Plaintext recovered. Signal analysis complete.";

            List<string> lines = new List<string>();
            string text = workingText;

            if (DecryptCipher.LooksLikeHex(text))
            {
                lines.Add("Only hex digits (0-9, A-F), even length - looks like a hexadecimal byte dump.");
            }
            else if (DecryptCipher.LooksLikeBase64(text))
            {
                lines.Add("Charset A-Z a-z 0-9 + / with '=' padding, length divisible by 4 - classic Base64 signature.");
            }
            else if (DecryptCipher.LetterRatio(text) > 0.5f)
            {
                List<KeyValuePair<char, int>> top = DecryptCipher.TopLetters(text, 3);
                string frequency = string.Empty;

                for (int i = 0; i < top.Count; i++)
                    frequency += (i > 0 ? "  " : string.Empty) + $"{top[i].Key}:{top[i].Value}";

                lines.Add($"Letter frequency: {frequency}. English plaintext peaks at E, T, A.");

                if (showSuggestedShift)
                {
                    int shift = DecryptCipher.SuggestedCaesarShift(text);

                    if (shift >= 1 && shift <= DecryptCipher.MaxCaesarShift)
                        lines.Add($"If this is a Caesar cipher, peak letter -> E gives shift {shift}.");
                    else if (shift > 0)
                        lines.Add($"The peak letter does not map to E within the Caesar key range 1-{DecryptCipher.MaxCaesarShift} - probably not a Caesar layer. A peak at V means an Atbash mirror (A<->Z).");
                }
                else
                {
                    lines.Add($"Caesar keys here go only up to {DecryptCipher.MaxCaesarShift}; a peak at V hints at an Atbash mirror instead.");
                }
            }
            else
            {
                lines.Add("Mixed printable characters - no clean digit-charset signature. Consider a letter cipher or a transposition.");
            }

            if (layersVisible)
                lines.Add($"Encoding layers remaining: {remaining.Count}.");

            return string.Join("\n", lines);
        }

        // --- Helpers ---

        private static string LayersText(int layers) => layers == 1 ? "1 layer" : $"{layers} layers";

        private static string AttemptsText(int attempts) => attempts == 1 ? "1 attempt" : $"{attempts} attempts";

        private string BuildSummary()
        {
            // Neon palette, kept in sync with DecryptMinigameUI: mint = success,
            // hot magenta = failure, cyan = accents.
            string okColor = ColorUtility.ToHtmlStringRGB(new Color(0.15f, 0.95f, 0.70f));
            string badColor = ColorUtility.ToHtmlStringRGB(new Color(1f, 0.30f, 0.60f));
            string accent = ColorUtility.ToHtmlStringRGB(new Color(0.20f, 0.90f, 1f));

            string summary = string.IsNullOrEmpty(activeScenarioName)
                ? string.Empty
                : $"<size=85%><i>Scenario: {activeScenarioName}</i></size>\n\n";

            if (peeledLog.Count > 0)
            {
                summary += "<b>Layers peeled:</b> ";

                for (int i = 0; i < peeledLog.Count; i++)
                    summary += (i > 0 ? $" <color=#{accent}>&#8594;</color> " : string.Empty) + peeledLog[i];

                summary += "\n";
            }

            if (solved)
            {
                summary += $"\n<b>Recovered intercept:</b>\n<color=#{okColor}>{plaintext}</color>";
                summary += $"\n\nWrong decodes: {wrongAttemptsUsed}, attempts left: {attemptsLeft}.";
            }
            else
            {
                summary += $"\n<color=#{badColor}><b>The intercept was not fully decrypted - " +
                           $"{LayersText(remaining.Count)} of {layersTotal} still in place.</b></color>";
            }

            summary += "\n\n<size=85%>Lesson: encoding is not encryption - Base64 and hex are reversible by " +
                       "anyone and hide nothing, while Caesar-style substitution falls to letter-frequency " +
                       "analysis. Recognise the signature first (charset, padding, length, frequencies), " +
                       "then apply the matching decoder - never trust \"scrambled\" data to be safe.</size>";

            return summary;
        }
    }
}
