using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Minigames
{
    /// <summary>
    /// One row in the traffic panel: the request's context plus a verdict badge that
    /// lights up after the player presses "Test".
    /// </summary>
    public class FirewallMinigameUI_TrafficRow : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI labelTMP;
        [SerializeField] private TextMeshProUGUI detailsTMP;

        [Header("Verdict badge")]
        [SerializeField] private Image verdictIcon;
        [SerializeField] private TextMeshProUGUI verdictTMP;
        [SerializeField] private Color allowedColor = new Color(0.42f, 0.85f, 0.45f);
        [SerializeField] private Color blockedColor = new Color(0.90f, 0.36f, 0.36f);
        [SerializeField] private Color pendingColor = new Color(0.55f, 0.58f, 0.62f);
        [SerializeField] private Color correctColor = new Color(0.42f, 0.85f, 0.45f);
        [SerializeField] private Color wrongColor = new Color(0.90f, 0.36f, 0.36f);

        public void Setup(FirewallTrafficEntry entry)
        {
            if (labelTMP != null)
                labelTMP.text = entry.label;

            if (detailsTMP != null)
                detailsTMP.text = entry.Details;

            ClearVerdict();
        }

        public void ClearVerdict()
        {
            if (verdictTMP != null)
            {
                verdictTMP.text = "?";
                verdictTMP.color = pendingColor;
            }

            if (verdictIcon != null)
                verdictIcon.color = pendingColor;
        }

        /// <summary>
        /// Paints the badge with what the ruleset decided and whether that was correct.
        /// </summary>
        public void SetVerdict(bool allowed, bool correct)
        {
            if (verdictTMP != null)
            {
                verdictTMP.text = allowed ? "ALLOW" : "BLOCK";
                verdictTMP.color = allowed ? allowedColor : blockedColor;
            }

            if (verdictIcon != null)
                verdictIcon.color = correct ? correctColor : wrongColor;
        }
    }
}
