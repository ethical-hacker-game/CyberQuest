using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Minigames
{
    /// <summary>
    /// Transparent overlay for the 3D Data Rover: the HUD strip only - shard counter
    /// (<c>X/N</c>), integrity pips + text mirror, and the feedback line pushed by the
    /// world view. Movement input (WASD / arrows) is read directly by
    /// <see cref="RoverMinigameWorld"/>, which also drives the game timers - so this
    /// class is pure readout. Timer and score are drawn by the shared MinigameMainUI.
    /// </summary>
    public class RoverMinigameUI : MinigameUI
    {
        [Header("Readout")]
        [SerializeField, Tooltip("Shard counter, e.g. 'EVIDENCE 3/8'.")]
        private TextMeshProUGUI shardsTMP;
        [SerializeField, Tooltip("Row the integrity pips are spawned into.")]
        private RectTransform integrityPipsRow;
        [SerializeField, Tooltip("Plain-text mirror of integrity + detections.")]
        private TextMeshProUGUI integrityTMP;
        [SerializeField] private TextMeshProUGUI feedbackTMP;

        [Header("Colours")]
        [SerializeField] private Color pipFullColor = new Color(0.13f, 0.96f, 0.88f);
        [SerializeField] private Color pipEmptyColor = new Color(0.16f, 0.25f, 0.34f);
        [SerializeField] private Color infoColor = new Color(0.55f, 0.90f, 1f);
        [SerializeField] private Color warnColor = new Color(1f, 0.35f, 0.60f);

        private readonly List<Image> pips = new List<Image>();

        private int lastPipsIntegrity = -1;
        private int lastPipsMax = -1;
        private bool initialised;

        private RoverMinigame Minigame => RoverMinigame.MinigameInstance;

        private void Start()
        {
            if (Minigame == null)
                return;

            // Loud reference audit - a broken hand-authored binding must never fail silently.
            ReportIfMissing(shardsTMP, "shardsTMP");
            ReportIfMissing(integrityPipsRow, "integrityPipsRow");
            ReportIfMissing(integrityTMP, "integrityTMP");
            ReportIfMissing(feedbackTMP, "feedbackTMP");

            initialised = true;
            ShowFeedback("Watch a patrol loop, learn its window, then move. Drive with WASD / arrows.", false);
            RefreshHud();
        }

        private void ReportIfMissing(Object reference, string fieldName)
        {
            if (reference == null)
                Debug.LogError($"[DataRover] UI reference '{fieldName}' is not assigned - the HUD will misbehave. Fix the RoverMinigameUI prefab.");
        }

        private void Update()
        {
            if (initialised == false || Minigame == null)
                return;

            if (Minigame.IsPaused || Minigame.IsFinished)
                return;

            RefreshHud();
        }

        /// <summary>Event line pushed by the world view (pickups, detections, game over).</summary>
        public void ShowFeedback(string message, bool isWarning)
        {
            if (feedbackTMP == null)
                return;

            feedbackTMP.text = message;
            feedbackTMP.color = isWarning ? warnColor : infoColor;
        }

        private void RefreshHud()
        {
            if (shardsTMP != null)
                shardsTMP.text = $"EVIDENCE {Minigame.Collected}/{Minigame.TotalShards}";

            // Integrity is mirrored in plain text so the HUD survives even a broken
            // pips row - the rover state must always be readable somewhere.
            if (integrityTMP != null)
                integrityTMP.text = $"INTEGRITY {Minigame.CurrentIntegrity}/{Minigame.MaxIntegrity}   •   DETECTIONS: {Minigame.Detections}";

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
