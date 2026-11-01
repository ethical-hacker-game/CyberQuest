using System;
using System.Collections.Generic;
using UnityEngine;

namespace Minigames
{
    /// <summary>
    /// One authored ordering puzzle: a titled list of terms in their CORRECT order
    /// (index 0 = first slot). The player sees the terms shuffled and must rebuild
    /// the sequence. Ideal for canonical orderings the course wants memorised:
    /// OSI layers, the Cyber Kill Chain, incident-response phases, DHCP DORA...
    /// </summary>
    [Serializable]
    public class OrderScenario
    {
        [Tooltip("Author-facing name, also shown on the result screen.")]
        public string scenarioName = "Scenario";

        [Tooltip("Difficulty this scenario is tuned for. Default = usable at any difficulty.")]
        public MinigameDifficulty recommendedDifficulty = MinigameDifficulty.Default;

        [TextArea(1, 3)]
        [Tooltip("The prompt shown above the board. MUST make the direction unambiguous, e.g. 'from Layer 1 (bottom) to Layer 7 (top)'.")]
        public string headline = "";

        [Tooltip("The terms in their CORRECT order (index 0 is slot 1). 4-8 items play best; 10 is the hard cap.")]
        public List<string> items = new List<string>();

        [TextArea(1, 3)]
        [Tooltip("Optional take-away line appended to the result summary (mnemonic, why the order matters...).")]
        public string lesson = "";

        /// <summary>True when the scenario is actually playable.</summary>
        public bool HasSequence
        {
            get
            {
                if (items == null || items.Count < 2)
                    return false;

                foreach (string item in items)
                {
                    if (string.IsNullOrEmpty(item))
                        return false;
                }

                return true;
            }
        }
    }

    /// <summary>Result of one CHECK ORDER action, so the UI can react + explain.</summary>
    public struct OrderCheckResult
    {
        public bool handled;   // False when the game is over / paused / slots missing - click ignored.
        public bool lockedNew; // True when at least one new position locked this check.
        public bool solved;    // True when every position is locked -> win.
        public bool failed;    // True when the check budget ran out -> loss.
        public string message;
    }
}
