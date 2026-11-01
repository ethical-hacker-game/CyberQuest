using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Minigames
{
    /// <summary>
    /// Transparent overlay for the 3D Sonar Sweep: the HUD strip only - cache counter
    /// (<c>X/N</c>), scanner-integrity pips + a text mirror, and the feedback line the
    /// world pushes on marks and false hits. There are no buttons: the input is the mouse
    /// aimed straight into the 3D world, and the world view drives the game timers, so this
    /// class is pure readout. Timer and score are drawn by the shared MinigameMainUI.
    /// </summary>
    public class SonarMinigameUI : MinigameUI
    {
        [Header("Readout")]
        [SerializeField, Tooltip("Cache counter, e.g. 'CACHES 3/6'.")]
        private TextMeshProUGUI cachesTMP;
        [SerializeField, Tooltip("Row the integrity pips are spawned into.")]
        private RectTransform integrityPipsRow;
        [SerializeField, Tooltip("Plain-text mirror of scanner integrity + false positives.")]
        private TextMeshProUGUI integrityTMP;
        [SerializeField] private TextMeshProUGUI feedbackTMP;

        [Header("Colours")]
        [SerializeField] private Color pipFullColor = new Color(0.21f, 0.88f, 1f);
        [SerializeField] private Color pipEmptyColor = new Color(0.16f, 0.25f, 0.34f);
        [SerializeField] private Color infoColor = new Color(0.55f, 0.90f, 1f);
        [SerializeField] private Color warnColor = new Color(1f, 0.35f, 0.60f);

        private readonly List<Image> pips = new List<Image>();

        private int lastPipsIntegrity = -1;
        private int lastPipsMax = -1;
        private bool initialised;

        private SonarMinigame Minigame => SonarMinigame.MinigameInstance;

        private void Start()
        {
            if (Minigame == null)
                return;

            // Loud reference audit - a broken hand-authored binding must never fail silently.
            ReportIfMissing(cachesTMP, "cachesTMP");
            ReportIfMissing(integrityPipsRow, "integrityPipsRow");
            ReportIfMissing(integrityTMP, "integrityTMP");
            ReportIfMissing(feedbackTMP, "feedbackTMP");

            initialised = true;
            ShowFeedback($"Sweep for contacts - a steady {Minigame.CacheLabel} is real, a flickering {Minigame.EchoLabel} is noise. New contacts keep fading in, so keep scanning.", false);
            RefreshHud();
        }

        private void ReportIfMissing(Object reference, string fieldName)
        {
            if (reference == null)
                Debug.LogError($"[SonarSweep] UI reference '{fieldName}' is not assigned - the HUD will misbehave. Fix the SonarMinigameUI prefab.");
        }

        private void Update()
        {
            if (initialised == false || Minigame == null)
                return;

            if (Minigame.IsPaused || Minigame.IsFinished)
                return;

            RefreshHud();
        }

        /// <summary>Event line pushed by the world view (marks, false hits, game over).</summary>
        public void ShowFeedback(string message, bool isWarning)
        {
            if (feedbackTMP == null)
                return;

            feedbackTMP.text = message;
            feedbackTMP.color = isWarning ? warnColor : infoColor;
        }

        private void RefreshHud()
        {
            if (cachesTMP != null)
                cachesTMP.text = $"CACHES {Minigame.Marked}/{Minigame.TotalCaches}";

            // Integrity is mirrored in plain text so the HUD survives even a broken pips row -
            // the scanner state must always be readable somewhere.
            if (integrityTMP != null)
                integrityTMP.text = $"SCANNER {Minigame.CurrentIntegrity}/{Minigame.MaxIntegrity}   •   FALSE HITS: {Minigame.FalseHits}";

            RefreshIntegrityPips();
        }

        private void RefreshIntegrityPips()
        {
            int integrity = Minigame.CurrentIntegrity;
            int max = Minigame.MaxIntegrity;

            if (integrity == lastPipsIntegrity && max == lastPipsMax)
                return;

            lastPipsIntegrity = integrity;
            lastPipsMax = max;

            if (integrityPipsRow == null)
                return;

            // Pips are plain procedural Images - no asset dependencies.
            while (pips.Count < max)
            {
                GameObject pipGo = new GameObject("Pip",
                    typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                pipGo.layer = integrityPipsRow.gameObject.layer;

                RectTransform pipRt = (RectTransform)pipGo.transform;
                pipRt.SetParent(integrityPipsRow, false);
                pipRt.sizeDelta = new Vector2(20f, 30f);

                Image pip = pipGo.GetComponent<Image>();
                pip.raycastTarget = false;
                pips.Add(pip);
            }

            for (int i = 0; i < pips.Count; i++)
            {
                pips[i].gameObject.SetActive(i < max);
                pips[i].color = i < integrity ? pipFullColor : pipEmptyColor;
            }
        }

        public override void UpdateUI()
        {
            // Called once on the source prefab before the live copy exists; safe to no-op
            // until this instance has run its own Start.
            if (initialised)
                RefreshHud();
        }
    }
}
