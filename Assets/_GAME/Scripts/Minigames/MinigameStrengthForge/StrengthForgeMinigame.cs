using System;
using System.Collections.Generic;
using UnityEngine;

namespace Minigames
{
    /// <summary>
    /// "Strength Forge" &mdash; a deliberate, round-based puzzle about password strength.
    /// The player forges a password for a series of accounts by combining tokens from a
    /// pool, under two constraints: a token-slot limit and a memorability budget. Live
    /// meters show how long each active attack model (dictionary / hybrid rules / brute
    /// force) would need to crack the current forge; the round is cleared by committing
    /// a password that survives every attack for the required time. Nothing here runs on
    /// Update &mdash; the game only reacts to the player editing the forge and pressing
    /// "Forge", so it rewards reasoning over reflexes.
    ///
    /// The intended lesson: length from rare words beats "clever" character tricks
    /// (leet-speak, digit suffixes), and anything from a breach dump is already lost.
    /// </summary>
    public class StrengthForgeMinigame : MinigameBehaviour
    {
        private static StrengthForgeMinigame minigameInstance;

        [Header("Token pool")]
        [SerializeField, Tooltip("Default forgeable tokens. A task can override these through StrengthForgeMinigameSettings.")]
        private List<StrengthToken> defaultTokens = new List<StrengthToken>
        {
            new StrengthToken { text = "qwerty123", category = StrengthTokenCategory.BreachedFragment },
            new StrengthToken { text = "letmein", category = StrengthTokenCategory.BreachedFragment },
            new StrengthToken { text = "123456", category = StrengthTokenCategory.BreachedFragment },
            new StrengthToken { text = "password1", category = StrengthTokenCategory.BreachedFragment },
            new StrengthToken { text = "iloveyou", category = StrengthTokenCategory.BreachedFragment },
            new StrengthToken { text = "abc123", category = StrengthTokenCategory.BreachedFragment },
            new StrengthToken { text = "dragon", category = StrengthTokenCategory.CommonWord },
            new StrengthToken { text = "summer", category = StrengthTokenCategory.CommonWord },
            new StrengthToken { text = "football", category = StrengthTokenCategory.CommonWord },
            new StrengthToken { text = "monkey", category = StrengthTokenCategory.CommonWord },
            new StrengthToken { text = "sunshine", category = StrengthTokenCategory.CommonWord },
            new StrengthToken { text = "princess", category = StrengthTokenCategory.CommonWord },
            new StrengthToken { text = "shadow", category = StrengthTokenCategory.CommonWord },
            new StrengthToken { text = "master", category = StrengthTokenCategory.CommonWord },
            new StrengthToken { text = "winter", category = StrengthTokenCategory.CommonWord },
            new StrengthToken { text = "coffee", category = StrengthTokenCategory.CommonWord },
            new StrengthToken { text = "hello", category = StrengthTokenCategory.CommonWord },
            new StrengthToken { text = "freedom", category = StrengthTokenCategory.CommonWord },
            new StrengthToken { text = "P@ssw0rd", category = StrengthTokenCategory.LeetWord },
            new StrengthToken { text = "Dr4g0n", category = StrengthTokenCategory.LeetWord },
            new StrengthToken { text = "S3cur3", category = StrengthTokenCategory.LeetWord },
            new StrengthToken { text = "M0nk3y", category = StrengthTokenCategory.LeetWord },
            new StrengthToken { text = "L3tM3in", category = StrengthTokenCategory.LeetWord },
            new StrengthToken { text = "W1nt3r", category = StrengthTokenCategory.LeetWord },
            new StrengthToken { text = "kayak", category = StrengthTokenCategory.RandomWord },
            new StrengthToken { text = "lantern", category = StrengthTokenCategory.RandomWord },
            new StrengthToken { text = "orchid", category = StrengthTokenCategory.RandomWord },
            new StrengthToken { text = "velvet", category = StrengthTokenCategory.RandomWord },
            new StrengthToken { text = "glacier", category = StrengthTokenCategory.RandomWord },
            new StrengthToken { text = "puffin", category = StrengthTokenCategory.RandomWord },
            new StrengthToken { text = "mosaic", category = StrengthTokenCategory.RandomWord },
            new StrengthToken { text = "tundra", category = StrengthTokenCategory.RandomWord },
            new StrengthToken { text = "ember", category = StrengthTokenCategory.RandomWord },
            new StrengthToken { text = "quartz", category = StrengthTokenCategory.RandomWord },
            new StrengthToken { text = "falcon", category = StrengthTokenCategory.RandomWord },
            new StrengthToken { text = "harbor", category = StrengthTokenCategory.RandomWord },
            new StrengthToken { text = "thistle", category = StrengthTokenCategory.RandomWord },
            new StrengthToken { text = "cobalt", category = StrengthTokenCategory.RandomWord },
            new StrengthToken { text = "meadow", category = StrengthTokenCategory.RandomWord },
            new StrengthToken { text = "saffron", category = StrengthTokenCategory.RandomWord },
            new StrengthToken { text = "walrus", category = StrengthTokenCategory.RandomWord },
            new StrengthToken { text = "zephyr", category = StrengthTokenCategory.RandomWord },
            new StrengthToken { text = "bramble", category = StrengthTokenCategory.RandomWord },
            new StrengthToken { text = "fjord", category = StrengthTokenCategory.RandomWord },
            new StrengthToken { text = "obsidian", category = StrengthTokenCategory.RandomWord },
            new StrengthToken { text = "pistachio", category = StrengthTokenCategory.RandomWord },
            new StrengthToken { text = "marmot", category = StrengthTokenCategory.RandomWord },
            new StrengthToken { text = "juniper", category = StrengthTokenCategory.RandomWord },
            new StrengthToken { text = "7", category = StrengthTokenCategory.Digits },
            new StrengthToken { text = "19", category = StrengthTokenCategory.Digits },
            new StrengthToken { text = "42", category = StrengthTokenCategory.Digits },
            new StrengthToken { text = "88", category = StrengthTokenCategory.Digits },
            new StrengthToken { text = "305", category = StrengthTokenCategory.Digits },
            new StrengthToken { text = "512", category = StrengthTokenCategory.Digits },
            new StrengthToken { text = "777", category = StrengthTokenCategory.Digits },
            new StrengthToken { text = "1987", category = StrengthTokenCategory.Digits },
            new StrengthToken { text = "2024", category = StrengthTokenCategory.Digits },
            new StrengthToken { text = "3050", category = StrengthTokenCategory.Digits },
            new StrengthToken { text = "6174", category = StrengthTokenCategory.Digits },
            new StrengthToken { text = "90210", category = StrengthTokenCategory.Digits },
            new StrengthToken { text = "!", category = StrengthTokenCategory.Symbol },
            new StrengthToken { text = "#", category = StrengthTokenCategory.Symbol },
            new StrengthToken { text = "-", category = StrengthTokenCategory.Symbol },
            new StrengthToken { text = "@", category = StrengthTokenCategory.Symbol },
            new StrengthToken { text = "$", category = StrengthTokenCategory.Symbol },
            new StrengthToken { text = "%", category = StrengthTokenCategory.Symbol },
            new StrengthToken { text = "&", category = StrengthTokenCategory.Symbol },
            new StrengthToken { text = "*", category = StrengthTokenCategory.Symbol },
            new StrengthToken { text = "?", category = StrengthTokenCategory.Symbol },
        };

        [Header("Rounds")]
        [SerializeField, Tooltip("Default account rounds. A task can override these through StrengthForgeMinigameSettings.")]
        private List<StrengthForgeRound> defaultRounds = new List<StrengthForgeRound>
        {
            new StrengthForgeRound
            {
                label = "Employee webmail",
                description = "An attacker sprays guesses at the login form. Rate limiting keeps them slow - but patient.",
                dictionaryAttack = true,
                hybridAttack = true,
                bruteForceAttack = false,
                guessesPerSecond = 1e3f,
                requiredCrackSeconds = 86400f, // 1 day
                maxTokens = 4,
                memorabilityBudget = 8,
            },
            new StrengthForgeRound
            {
                label = "Company VPN",
                description = "The password hash leaked. A cracking rig with consumer GPUs is grinding through it offline.",
                dictionaryAttack = true,
                hybridAttack = true,
                bruteForceAttack = true,
                guessesPerSecond = 1e10f,
                requiredCrackSeconds = 2592000f, // 30 days
                maxTokens = 5,
                memorabilityBudget = 12,
            },
            new StrengthForgeRound
            {
                label = "Domain-admin vault",
                description = "Assume a nation-state GPU farm gets the hash. The vault must outlive the secrets inside it.",
                dictionaryAttack = true,
                hybridAttack = true,
                bruteForceAttack = true,
                guessesPerSecond = 1e12f,
                requiredCrackSeconds = 315360000f, // 10 years
                maxTokens = 6,
                memorabilityBudget = 15,
            },
        };

        [Header("Attack model")]
        [SerializeField, Tooltip("How many candidates a cracker tries before hitting a breach-dump fragment.")]
        private double breachedPoolSize = 1e3;
        [SerializeField, Tooltip("Size of the attacker's common-word dictionary.")]
        private double commonWordPoolSize = 2e4;
        [SerializeField, Tooltip("Size of the big wordlist that still contains the 'rare' random words.")]
        private double randomWordPoolSize = 4e4;
        [SerializeField, Tooltip("How much extra work leet-mutations cost the attacker (spoiler: not much).")]
        private double leetRuleMultiplier = 50;
        [SerializeField, Tooltip("How many year-like values an attacker tries before yours (birth years etc.).")]
        private double yearPoolSize = 200;

        [Header("Scoring")]
        [SerializeField] private int pointsPerRound = 250;
        [SerializeField, Tooltip("Bonus per order of magnitude the crack time exceeds the requirement.")]
        private int marginBonusPerOrder = 40;
        [SerializeField, Tooltip("Cap on rewarded orders of magnitude, so overkill does not dwarf the base score.")]
        private int marginBonusOrdersCap = 5;
        [SerializeField, Tooltip("Bonus per second left on the clock when the last round is cleared.")]
        private int timeBonusPerSecond = 5;

        private List<StrengthToken> tokens = new List<StrengthToken>();
        private List<StrengthForgeRound> rounds = new List<StrengthForgeRound>();

        private readonly List<StrengthToken> forged = new List<StrengthToken>();
        private readonly List<StrengthForgeRoundRecord> clearedRounds = new List<StrengthForgeRoundRecord>();
        private int roundIndex = 0;

        public static StrengthForgeMinigame MinigameInstance
        {
            get
            {
                if (minigameInstance == null)
                    Debug.LogError("minigameInstance does not exist");

                return minigameInstance;
            }
        }

        public IReadOnlyList<StrengthToken> TokenPool => tokens;
        public IReadOnlyList<StrengthToken> ForgedTokens => forged;
        public int RoundIndex => roundIndex;
        public int RoundsTotal => rounds.Count;
        public StrengthForgeRound CurrentRound => roundIndex < rounds.Count ? rounds[roundIndex] : null;

        public override string Summary => BuildSummary();

        public string PasswordPreview
        {
            get
            {
                string password = "";
                foreach (StrengthToken token in forged)
                    password += token.text;
                return password;
            }
        }

        public int UsedBudget
        {
            get
            {
                int used = 0;
                foreach (StrengthToken token in forged)
                    used += token.EffectiveCost;
                return used;
            }
        }

        private void Awake()
        {
            minigameInstance = this;
            tokens = new List<StrengthToken>(defaultTokens);
            rounds = new List<StrengthForgeRound>(defaultRounds);
        }

        private void OnDestroy()
        {
            if (minigameInstance == this)
                minigameInstance = null;
        }

        public override void ApplyConfig(TaskMinigameVariant config)
        {
            // Runs after Awake (pool/rounds already seeded from the prefab) but before
            // Start and before the UI is instantiated, so replacing them here is safe.
            base.ApplyConfig(config);

            if (config != null && config.strengthForgeSettings != null)
            {
                StrengthForgeMinigameSettings s = config.strengthForgeSettings;

                if (s.tokens != null && s.tokens.Count > 0)
                    tokens = new List<StrengthToken>(s.tokens);

                if (s.rounds != null && s.rounds.Count > 0)
                    rounds = new List<StrengthForgeRound>(s.rounds);
            }

            ApplyDifficulty();
        }

        private void ApplyDifficulty()
        {
            if (Difficulty == MinigameDifficulty.Default)
                return;

            // Two knobs: how long the password must survive, and how much memorability
            // budget the player gets to buy strong (expensive) tokens with.
            float thresholdMultiplier;
            float budgetMultiplier;

            switch (Difficulty)
            {
                case MinigameDifficulty.Easy:
                    thresholdMultiplier = 0.3f;
                    budgetMultiplier = 1.25f;
                    break;
                case MinigameDifficulty.Hard:
                    thresholdMultiplier = 3f;
                    budgetMultiplier = 0.85f;
                    break;
                default: // Normal
                    thresholdMultiplier = 1f;
                    budgetMultiplier = 1f;
                    break;
            }

            foreach (StrengthForgeRound round in rounds)
            {
                round.requiredCrackSeconds *= thresholdMultiplier;
                round.memorabilityBudget = Mathf.Max(2, Mathf.RoundToInt(round.memorabilityBudget * budgetMultiplier));
            }
        }

        // --- Player actions (called from the UI) ---

        public bool IsTokenForged(StrengthToken token) => forged.Contains(token);

        public bool CanAddToken(StrengthToken token)
        {
            StrengthForgeRound round = CurrentRound;

            if (round == null || token == null)
                return false;

            return forged.Contains(token) == false
                && forged.Count < round.maxTokens
                && UsedBudget + token.EffectiveCost <= round.memorabilityBudget;
        }

        public bool AddToken(StrengthToken token)
        {
            if (CanAddToken(token) == false)
                return false;

            forged.Add(token);
            return true;
        }

        public void RemoveForgedAt(int index)
        {
            if (index < 0 || index >= forged.Count)
                return;

            forged.RemoveAt(index);
        }

        public void ClearForge()
        {
            forged.Clear();
        }

        /// <summary>
        /// Commits the current forge. A passing password clears the round (and the
        /// minigame, when it was the last one); a failing one costs nothing but time -
        /// the returned evaluation tells the player exactly which attack breaks it.
        /// </summary>
        public StrengthForgeForgeResult Forge()
        {
            StrengthForgeForgeResult result = new StrengthForgeForgeResult();
            result.evaluation = Evaluate();

            if (forged.Count == 0)
            {
                result.passed = false;
                result.message = "Forge is empty - pick some tokens first.";
                return result;
            }

            if (result.evaluation.Passed == false)
            {
                result.passed = false;
                result.message = result.evaluation.weaknessHint;
                return result;
            }

            StrengthForgeRound round = CurrentRound;

            int marginOrders = 0;
            if (double.IsInfinity(result.evaluation.weakestSeconds))
            {
                marginOrders = marginBonusOrdersCap;
            }
            else if (round.requiredCrackSeconds > 0f)
            {
                double orders = Math.Log10(result.evaluation.weakestSeconds / round.requiredCrackSeconds);
                marginOrders = Mathf.Clamp((int)orders, 0, marginBonusOrdersCap);
            }

            result.passed = true;
            result.roundScore = pointsPerRound + marginOrders * marginBonusPerOrder;
            result.message = $"{round.label} secured (+{result.roundScore})";

            clearedRounds.Add(new StrengthForgeRoundRecord
            {
                label = round.label,
                password = PasswordPreview,
                weakestAttack = result.evaluation.weakestAttack,
                weakestSeconds = result.evaluation.weakestSeconds,
            });

            ChangeScore(result.roundScore);

            roundIndex++;
            ClearForge();

            if (roundIndex >= rounds.Count)
            {
                int timeBonus = Mathf.RoundToInt(MinigamesController.GetTimerPositive) * timeBonusPerSecond;
                ChangeScore(timeBonus);

                PauseMinigame();
                FinishMinigame(true, Score, Summary);
            }

            return result;
        }

        // --- Attack model ---

        /// <summary>
        /// Evaluates the current forge against every active attack of the current
        /// round. Cheap enough to call on every edit, which is what makes the meters
        /// live and the game readable.
        /// </summary>
        public StrengthForgeEvaluation Evaluate()
        {
            StrengthForgeEvaluation eval = new StrengthForgeEvaluation();
            StrengthForgeRound round = CurrentRound;

            if (round == null)
                return eval;

            foreach (StrengthAttackType attack in round.ActiveAttacks)
            {
                double guesses = GuessesFor(attack);
                double seconds = double.IsInfinity(guesses) ? double.PositiveInfinity : guesses / round.guessesPerSecond;

                StrengthAttackResult result = new StrengthAttackResult
                {
                    attack = attack,
                    crackSeconds = seconds,
                    passed = seconds >= round.requiredCrackSeconds,
                };

                eval.attackResults.Add(result);

                if (seconds < eval.weakestSeconds)
                {
                    eval.weakestSeconds = seconds;
                    eval.weakestAttack = attack;
                }
            }

            eval.weaknessHint = BuildWeaknessHint(eval);
            return eval;
        }

        private double GuessesFor(StrengthAttackType attack)
        {
            switch (attack)
            {
                case StrengthAttackType.Dictionary: return DictionaryGuesses(withRules: false);
                case StrengthAttackType.Hybrid: return DictionaryGuesses(withRules: true);
                case StrengthAttackType.BruteForce: return BruteForceGuesses(PasswordPreview);
                default: return 0;
            }
        }

        /// <summary>
        /// Word-combination attack. Each token multiplies the search space by how many
        /// candidates the attacker tries before reaching it. Without rules the attacker
        /// only knows plain words, so leet / digits / symbols make the password
        /// unreachable for this model (infinity); with rules (hybrid) every category
        /// has a finite - usually embarrassingly small - price.
        /// </summary>
        private double DictionaryGuesses(bool withRules)
        {
            if (forged.Count == 0)
                return 0;

            double total = 1;

            foreach (StrengthToken token in forged)
            {
                double guesses = TokenGuesses(token, withRules);

                if (double.IsInfinity(guesses))
                    return double.PositiveInfinity;

                total *= guesses;
            }

            // Average case: the target is found halfway through the search space.
            return total / 2;
        }

        private double TokenGuesses(StrengthToken token, bool withRules)
        {
            int length = token.text != null ? token.text.Length : 0;

            switch (token.category)
            {
                case StrengthTokenCategory.BreachedFragment:
                    return breachedPoolSize;

                case StrengthTokenCategory.CommonWord:
                    return commonWordPoolSize;

                case StrengthTokenCategory.RandomWord:
                    return randomWordPoolSize;

                case StrengthTokenCategory.LeetWord:
                    // Mangling rules turn "P@ssw0rd" back into "password" for pennies.
                    return withRules ? commonWordPoolSize * leetRuleMultiplier : double.PositiveInfinity;

                case StrengthTokenCategory.Digits:
                    if (withRules == false)
                        return double.PositiveInfinity;
                    return IsYearLike(token.text) ? yearPoolSize : Math.Pow(10, length);

                case StrengthTokenCategory.Symbol:
                    return withRules ? Math.Pow(33, length) : double.PositiveInfinity;

                default:
                    return double.PositiveInfinity;
            }
        }

        private static bool IsYearLike(string text)
        {
            return text != null
                && text.Length == 4
                && int.TryParse(text, out int year)
                && year >= 1900 && year <= 2099;
        }

        /// <summary>
        /// Exhaustive search over the used character classes. Only length and charset
        /// matter here - which is exactly the lesson this meter teaches.
        /// </summary>
        private double BruteForceGuesses(string password)
        {
            if (string.IsNullOrEmpty(password))
                return 0;

            bool lower = false, upper = false, digit = false, symbol = false;

            foreach (char c in password)
            {
                if (char.IsLower(c)) lower = true;
                else if (char.IsUpper(c)) upper = true;
                else if (char.IsDigit(c)) digit = true;
                else symbol = true;
            }

            double charset = 0;
            if (lower) charset += 26;
            if (upper) charset += 26;
            if (digit) charset += 10;
            if (symbol) charset += 33;

            return Math.Pow(charset, password.Length) / 2;
        }

        private string BuildWeaknessHint(StrengthForgeEvaluation eval)
        {
            if (forged.Count == 0)
                return "Pick tokens to start forging.";

            if (eval.Passed)
                return "Strong enough - forge it!";

            if (eval.weakestAttack == StrengthAttackType.BruteForce)
                return "Too short - brute force just tries every combination. Length is the strongest defence.";

            // Dictionary / hybrid weakness: call out the worst offender in the forge.
            foreach (StrengthToken token in forged)
            {
                if (token.category == StrengthTokenCategory.BreachedFragment)
                    return $"\"{token.text}\" is in real breach dumps - crackers try it first, for free.";
            }

            foreach (StrengthToken token in forged)
            {
                if (token.category == StrengthTokenCategory.LeetWord)
                    return $"Leet-speak barely helps: mangling rules turn \"{token.text}\" back into a dictionary word.";
            }

            foreach (StrengthToken token in forged)
            {
                if (token.category == StrengthTokenCategory.Digits && IsYearLike(token.text))
                    return $"\"{token.text}\" looks like a year - one of ~{yearPoolSize:0} obvious guesses.";
            }

            foreach (StrengthToken token in forged)
            {
                if (token.category == StrengthTokenCategory.CommonWord)
                    return $"\"{token.text}\" is a top-dictionary word. Swap it for rarer words.";
            }

            return "Good structure - just not enough of it. Add another rare word.";
        }

        // --- Presentation helpers (shared with the UI) ---

        /// <summary>
        /// Human-friendly crack-time string, e.g. "instantly", "3 hours", "14 years".
        /// </summary>
        public static string FormatDuration(double seconds)
        {
            if (double.IsInfinity(seconds))
                return "resists this attack";

            if (seconds < 1) return "instantly";
            if (seconds < 60) return $"{seconds:0} seconds";
            if (seconds < 3600) return $"{seconds / 60:0} minutes";
            if (seconds < 86400) return $"{seconds / 3600:0.#} hours";
            if (seconds < 2592000) return $"{seconds / 86400:0.#} days";
            if (seconds < 31536000) return $"{seconds / 2592000:0.#} months";

            double years = seconds / 31536000;
            if (years < 1000) return $"{years:0.#} years";
            if (years < 1e6) return $"{years / 1000:0.#} thousand years";
            if (years < 1e9) return $"{years / 1e6:0.#} million years";
            return "practically forever";
        }

        /// <summary>
        /// Compact guess-rate string, e.g. "1 K/s", "10 B/s", "1 T/s".
        /// </summary>
        public static string FormatRate(double guessesPerSecond)
        {
            if (guessesPerSecond >= 1e12) return $"{guessesPerSecond / 1e12:0.#} T/s";
            if (guessesPerSecond >= 1e9) return $"{guessesPerSecond / 1e9:0.#} B/s";
            if (guessesPerSecond >= 1e6) return $"{guessesPerSecond / 1e6:0.#} M/s";
            if (guessesPerSecond >= 1e3) return $"{guessesPerSecond / 1e3:0.#} K/s";
            return $"{guessesPerSecond:0} /s";
        }

        private string BuildSummary()
        {
            string okColor = ColorUtility.ToHtmlStringRGB(new Color(0.42f, 0.85f, 0.45f));
            string dimColor = ColorUtility.ToHtmlStringRGB(new Color(0.55f, 0.58f, 0.62f));

            string summary = "<b>Accounts you secured:</b>";

            foreach (StrengthForgeRoundRecord record in clearedRounds)
            {
                summary += $"\n<color=#{okColor}><b>[OK]</b></color> {record.label} - <b>{record.password}</b>" +
                           $"\n    <size=80%>weakest link: {record.weakestAttack} attack, cracked in {FormatDuration(record.weakestSeconds)}</size>";
            }

            for (int i = clearedRounds.Count; i < rounds.Count; i++)
            {
                summary += $"\n<color=#{dimColor}><b>[--]</b></color> {rounds[i].label} - not reached";
            }

            summary += "\n\n<size=85%>Lesson: length from rare words beats \"clever\" substitutions - " +
                       "and anything from a breach dump is already lost.</size>";

            return summary;
        }
    }
}
