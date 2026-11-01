using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Minigames
{
    /// <summary>
    /// One encoding operation the interceptor stacked on the plaintext. The player wins
    /// by applying the matching DECODE operations until the message is readable again.
    /// <see cref="Rot13"/> is authoring sugar - it is normalised to Caesar(13) when the
    /// scenario is seeded, so the ROT13 and Caesar tools are interchangeable for it.
    /// </summary>
    public enum DecryptStepType
    {
        Caesar = 0,
        Base64 = 1,
        Hex = 2,
        Reverse = 3,
        Atbash = 4,
        Rot13 = 5,
    }

    /// <summary>One layer of the encoding chain (type + the Caesar key when relevant).</summary>
    [Serializable]
    public class DecryptStep
    {
        public DecryptStepType type = DecryptStepType.Caesar;

        [Range(1, DecryptCipher.MaxCaesarShift)]
        [Tooltip("Only used when type is Caesar. The forward shift applied when encoding. Kept small (1-4) so the player picks from a handful of keys instead of the whole alphabet.")]
        public int caesarShift = 3;
    }

    /// <summary>
    /// One authored intercept puzzle. <see cref="steps"/> lists the encodings in the order
    /// they were APPLIED (index 0 first), so the outermost layer - the one the player must
    /// peel first - is the LAST entry.
    /// </summary>
    [Serializable]
    public class DecryptScenario
    {
        [Tooltip("Author-facing name, also shown on the result screen.")]
        public string scenarioName = "Scenario";

        [Tooltip("Difficulty this scenario is tuned for. Default = usable at any difficulty.")]
        public MinigameDifficulty recommendedDifficulty = MinigameDifficulty.Default;

        [TextArea(1, 3)]
        [Tooltip("The readable message hidden under the encoding chain. Keep it single-line, ASCII, ideally UPPERCASE (makes the letter-frequency hint teachable).")]
        public string plaintext = "";

        [Tooltip("Encodings in the order they were applied. The LAST one is the outermost layer.")]
        public List<DecryptStep> steps = new List<DecryptStep>();

        /// <summary>True when the scenario is actually playable.</summary>
        public bool HasPuzzle => string.IsNullOrEmpty(plaintext) == false && steps != null && steps.Count > 0;
    }

    /// <summary>Result of one decode attempt, so the UI can react + explain.</summary>
    public struct DecryptApplyResult
    {
        public bool handled;  // False when the game is over / paused - click ignored.
        public bool accepted; // True when a layer was peeled.
        public bool solved;   // True when that peel recovered the plaintext -> win.
        public bool failed;   // True when the attempt budget ran out -> loss.
        public string message;
    }

    /// <summary>
    /// Pure string transforms + signature-analysis helpers for the Decrypt minigame.
    /// Encode/Decode are exact inverses per step; Decode returns null when the input
    /// cannot be that kind of layer (invalid Base64/Hex or unprintable output), which is
    /// what makes wrong tools detectable and the puzzle solvable by reasoning.
    /// </summary>
    public static class DecryptCipher
    {
        /// <summary>
        /// Highest Caesar key the game uses. Kept deliberately tiny so the shift picker
        /// offers a handful of options instead of the whole alphabet - the lesson is
        /// "recognise the cipher and read the frequency hint", not modular arithmetic.
        /// NOTE: this makes ROT13 un-peelable with the Caesar tool, so Rot13 steps are
        /// rejected at seed/validation time.
        /// </summary>
        public const int MaxCaesarShift = 4;

        public static string Encode(DecryptStep step, string input)
        {
            if (input == null)
                return null;

            switch (step.type)
            {
                case DecryptStepType.Caesar: return ShiftLetters(input, step.caesarShift);
                case DecryptStepType.Rot13: return ShiftLetters(input, 13);
                case DecryptStepType.Base64: return Convert.ToBase64String(Encoding.UTF8.GetBytes(input));
                case DecryptStepType.Hex: return ToHex(input);
                case DecryptStepType.Reverse: return ReverseText(input);
                case DecryptStepType.Atbash: return Atbash(input);
                default: return null;
            }
        }

        /// <summary>Inverse of <see cref="Encode"/>. Returns null when the input can't be this layer.</summary>
        public static string Decode(DecryptStep step, string input)
        {
            if (string.IsNullOrEmpty(input))
                return null;

            switch (step.type)
            {
                case DecryptStepType.Caesar: return ShiftLetters(input, 26 - (step.caesarShift % 26));
                case DecryptStepType.Rot13: return ShiftLetters(input, 13);
                case DecryptStepType.Base64: return FromBase64(input);
                case DecryptStepType.Hex: return FromHex(input);
                case DecryptStepType.Reverse: return ReverseText(input);
                case DecryptStepType.Atbash: return Atbash(input);
                default: return null;
            }
        }

        /// <summary>Short tool name used in feedback lines and the result summary.</summary>
        public static string ToolLabel(DecryptStepType type, int caesarShift = 0)
        {
            switch (type)
            {
                case DecryptStepType.Caesar: return caesarShift > 0 ? $"CAESAR (shift {caesarShift})" : "CAESAR";
                case DecryptStepType.Rot13: return "ROT13";
                case DecryptStepType.Base64: return "BASE64";
                case DecryptStepType.Hex: return "HEX";
                case DecryptStepType.Reverse: return "REVERSE";
                case DecryptStepType.Atbash: return "ATBASH";
                default: return type.ToString().ToUpperInvariant();
            }
        }

        // --- Transforms ---

        private static string ShiftLetters(string input, int shift)
        {
            shift = ((shift % 26) + 26) % 26;
            char[] chars = input.ToCharArray();

            for (int i = 0; i < chars.Length; i++)
            {
                char c = chars[i];

                if (c >= 'A' && c <= 'Z')
                    chars[i] = (char)('A' + (c - 'A' + shift) % 26);
                else if (c >= 'a' && c <= 'z')
                    chars[i] = (char)('a' + (c - 'a' + shift) % 26);
            }

            return new string(chars);
        }

        private static string Atbash(string input)
        {
            char[] chars = input.ToCharArray();

            for (int i = 0; i < chars.Length; i++)
            {
                char c = chars[i];

                if (c >= 'A' && c <= 'Z')
                    chars[i] = (char)('Z' - (c - 'A'));
                else if (c >= 'a' && c <= 'z')
                    chars[i] = (char)('z' - (c - 'a'));
            }

            return new string(chars);
        }

        private static string ReverseText(string input)
        {
            char[] chars = input.ToCharArray();
            Array.Reverse(chars);
            return new string(chars);
        }

        private static string ToHex(string input)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(input);
            StringBuilder sb = new StringBuilder(bytes.Length * 2);

            foreach (byte b in bytes)
                sb.Append(b.ToString("X2"));

            return sb.ToString();
        }

        private static string FromBase64(string input)
        {
            try
            {
                byte[] bytes = Convert.FromBase64String(input.Trim());
                return PrintableOrNull(Encoding.UTF8.GetString(bytes));
            }
            catch
            {
                return null;
            }
        }

        private static string FromHex(string input)
        {
            string text = input.Trim();

            if (text.Length < 2 || text.Length % 2 != 0)
                return null;

            byte[] bytes = new byte[text.Length / 2];

            for (int i = 0; i < bytes.Length; i++)
            {
                int hi = HexValue(text[i * 2]);
                int lo = HexValue(text[i * 2 + 1]);

                if (hi < 0 || lo < 0)
                    return null;

                bytes[i] = (byte)(hi * 16 + lo);
            }

            return PrintableOrNull(Encoding.UTF8.GetString(bytes));
        }

        private static int HexValue(char c)
        {
            if (c >= '0' && c <= '9') return c - '0';
            if (c >= 'A' && c <= 'F') return c - 'A' + 10;
            if (c >= 'a' && c <= 'f') return c - 'a' + 10;
            return -1;
        }

        /// <summary>Returns the text unchanged when it is printable ASCII, null otherwise (= "this decode produced garbage").</summary>
        private static string PrintableOrNull(string text)
        {
            if (string.IsNullOrEmpty(text))
                return null;

            foreach (char c in text)
            {
                if (c < 32 || c > 126)
                    return null;
            }

            return text;
        }

        // --- Signature analysis (the free, educational "SIGINT" hints) ---

        /// <summary>Hex signature: only 0-9/A-F digits and an even length. Check BEFORE Base64 - hex is the stricter charset.</summary>
        public static bool LooksLikeHex(string text)
        {
            if (string.IsNullOrEmpty(text) || text.Length < 8 || text.Length % 2 != 0)
                return false;

            foreach (char c in text)
            {
                if (HexValue(c) < 0)
                    return false;
            }

            return true;
        }

        /// <summary>Base64 signature: A-Z a-z 0-9 + / with optional '=' padding and a length divisible by 4.</summary>
        public static bool LooksLikeBase64(string text)
        {
            if (string.IsNullOrEmpty(text) || text.Length < 8 || text.Length % 4 != 0)
                return false;

            int padding = 0;

            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];

                if (c == '=')
                {
                    padding++;

                    if (padding > 2 || i < text.Length - 2)
                        return false;

                    continue;
                }

                bool valid = (c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z') || (c >= '0' && c <= '9') || c == '+' || c == '/';

                if (valid == false)
                    return false;
            }

            return true;
        }

        /// <summary>Fraction of characters that are letters (used to decide whether frequency analysis is meaningful).</summary>
        public static float LetterRatio(string text)
        {
            if (string.IsNullOrEmpty(text))
                return 0f;

            int letters = 0;

            foreach (char c in text)
            {
                if ((c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z'))
                    letters++;
            }

            return (float)letters / text.Length;
        }

        /// <summary>Top letters of the text (case-insensitive), most frequent first.</summary>
        public static List<KeyValuePair<char, int>> TopLetters(string text, int count)
        {
            int[] counts = new int[26];

            foreach (char c in text)
            {
                if (c >= 'A' && c <= 'Z') counts[c - 'A']++;
                else if (c >= 'a' && c <= 'z') counts[c - 'a']++;
            }

            List<KeyValuePair<char, int>> result = new List<KeyValuePair<char, int>>();

            for (int i = 0; i < 26; i++)
            {
                if (counts[i] > 0)
                    result.Add(new KeyValuePair<char, int>((char)('A' + i), counts[i]));
            }

            result.Sort((a, b) => b.Value.CompareTo(a.Value));

            if (result.Count > count)
                result.RemoveRange(count, result.Count - count);

            return result;
        }

        /// <summary>
        /// Caesar key suggested by frequency analysis: assume the most frequent letter is a
        /// shifted E (the most common letter in English). Returns 0 when there is nothing
        /// to suggest (no letters, or the peak already IS E).
        /// </summary>
        public static int SuggestedCaesarShift(string text)
        {
            List<KeyValuePair<char, int>> top = TopLetters(text, 1);

            if (top.Count == 0)
                return 0;

            return (top[0].Key - 'E' + 26) % 26;
        }
    }
}
