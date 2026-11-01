using System;
using System.Collections.Generic;
using UnityEngine;

namespace Minigames
{
    /// <summary>
    /// What kind of building block a token is. The category drives both the attack
    /// model (how cheap the token is for a cracker) and the default memorability cost
    /// (how expensive it is for the player's budget).
    /// </summary>
    public enum StrengthTokenCategory
    {
        CommonWord = 0,       // top of every cracking dictionary
        BreachedFragment = 1, // straight from breach dumps - the first guesses tried
        LeetWord = 2,         // "P@ssw0rd" - looks strong, but mangling rules undo it
        RandomWord = 3,       // rare wordlist word - diceware-style entropy
        Digits = 4,           // digit run; 4-digit years are treated as near-free
        Symbol = 5,           // punctuation; widens the brute-force charset a little
    }

    /// <summary>
    /// The attack models a round can activate. Dictionary only cracks all-word
    /// passwords, Hybrid adds mangling rules (leet, digit/symbol decorations) so it
    /// cracks everything word-based, BruteForce only cares about length and charset.
    /// </summary>
    public enum StrengthAttackType
    {
        Dictionary = 0,
        Hybrid = 1,
        BruteForce = 2,
    }

    /// <summary>
    /// One selectable building block in the forge. <see cref="cost"/> is the
    /// memorability price the player pays for using it; 0 falls back to the category
    /// default so authored pools only need to fill the text and category.
    /// </summary>
    [Serializable]
    public class StrengthToken
    {
        [Tooltip("The literal text appended to the password, e.g. \"lantern\" or \"!\".")]
        public string text = "";

        public StrengthTokenCategory category = StrengthTokenCategory.CommonWord;

        [Tooltip("Memorability cost charged against the round budget. 0 = category default.")]
        public int cost = 0;

        public int EffectiveCost => cost > 0 ? cost : GetDefaultCost(category);

        /// <summary>
        /// Default memorability costs. Rare random words are the strongest tokens, so
        /// they are also the hardest to remember - that asymmetry is the whole puzzle.
        /// </summary>
        public static int GetDefaultCost(StrengthTokenCategory category)
        {
            switch (category)
            {
                case StrengthTokenCategory.RandomWord: return 3;
                case StrengthTokenCategory.LeetWord: return 2;
                default: return 1;
            }
        }
    }

    /// <summary>
    /// One account the player must protect. Each round defines who is attacking
    /// (active attack models + guess rate) and what "safe" means (required crack time),
    /// plus the composition constraints (token slots and memorability budget).
    /// </summary>
    [Serializable]
    public class StrengthForgeRound
    {
        [Tooltip("Account name shown as the round header, e.g. \"Company VPN\".")]
        public string label = "New account";

        [TextArea(1, 3)]
        [Tooltip("Short attacker fiction shown under the header.")]
        public string description = "";

        [Header("Attacker profile")]
        public bool dictionaryAttack = true;
        public bool hybridAttack = true;
        public bool bruteForceAttack = false;

        [Tooltip("Attacker guesses per second. ~1e3 = throttled online, ~1e10 = offline GPU, ~1e12 = GPU farm.")]
        public float guessesPerSecond = 1e10f;

        [Tooltip("The password must survive at least this many seconds against every active attack. 86400 = 1 day, 2592000 = 30 days, 31536000 = 1 year.")]
        public float requiredCrackSeconds = 86400f;

        [Header("Composition constraints")]
        [Tooltip("Maximum number of tokens the player may combine.")]
        public int maxTokens = 5;

        [Tooltip("Memorability budget - the sum of used token costs may not exceed it.")]
        public int memorabilityBudget = 10;

        /// <summary>
        /// The enabled attacks, in display order. Falls back to Hybrid when an author
        /// disabled everything, because a round with no attacks would auto-win.
        /// </summary>
        public List<StrengthAttackType> ActiveAttacks
        {
            get
            {
                List<StrengthAttackType> attacks = new List<StrengthAttackType>();

                if (dictionaryAttack) attacks.Add(StrengthAttackType.Dictionary);
                if (hybridAttack) attacks.Add(StrengthAttackType.Hybrid);
                if (bruteForceAttack) attacks.Add(StrengthAttackType.BruteForce);

                if (attacks.Count == 0)
                    attacks.Add(StrengthAttackType.Hybrid);

                return attacks;
            }
        }
    }

    /// <summary>
    /// How one attack model fares against the current forge.
    /// <see cref="crackSeconds"/> of PositiveInfinity means the attack cannot model
    /// this password at all (e.g. a pure dictionary attack against a symbol token).
    /// </summary>
    public class StrengthAttackResult
    {
        public StrengthAttackType attack;
        public double crackSeconds;
        public bool passed;
    }

    /// <summary>
    /// Live evaluation of the forged password against every active attack of the
    /// current round. Recomputed on every edit so the UI meters react instantly.
    /// </summary>
    public class StrengthForgeEvaluation
    {
        public List<StrengthAttackResult> attackResults = new List<StrengthAttackResult>();

        public double weakestSeconds = double.PositiveInfinity;
        public StrengthAttackType weakestAttack = StrengthAttackType.Hybrid;

        // One short sentence explaining what drags the weakest meter down.
        public string weaknessHint = "";

        public bool Passed
        {
            get
            {
                if (attackResults.Count == 0)
                    return false;

                foreach (StrengthAttackResult result in attackResults)
                {
                    if (result.passed == false)
                        return false;
                }

                return true;
            }
        }
    }

    /// <summary>
    /// Outcome of committing the forge (pressing the Forge button).
    /// </summary>
    public class StrengthForgeForgeResult
    {
        public bool passed;
        public int roundScore;
        public StrengthForgeEvaluation evaluation;
        public string message = "";
    }

    /// <summary>
    /// Snapshot of a cleared round, kept for the result-screen summary.
    /// </summary>
    public class StrengthForgeRoundRecord
    {
        public string label;
        public string password;
        public StrengthAttackType weakestAttack;
        public double weakestSeconds;
    }
}
