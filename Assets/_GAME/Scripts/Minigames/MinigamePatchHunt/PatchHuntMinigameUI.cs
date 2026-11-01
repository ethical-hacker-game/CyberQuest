using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Minigames
{
    /// <summary>
    /// Transparent overlay for the 3D Patch Hunt: just the HUD strip (integrity pips +
    /// text, patched/combo counters, feedback line) - there are no buttons, the input
    /// is the mouse aimed straight into the 3D world rendered by
    /// <see cref="PatchHuntMinigameWorld"/>. This class also keeps driving
    /// <see cref="PatchHuntMinigame.AdvanceSpawning"/> from its Update - the path
    /// proven to tick reliably - while the world only consumes the spawn queue.
    /// Timer and score are drawn by the shared MinigameMainUI.
    /// </summary>
    public class PatchHuntMinigameUI : MinigameUI
    {
        [Header("Readout")]
        [SerializeField, Tooltip("Row the integrity pips are spawned into.")]
        private RectTransform integrityPipsRow;
        [SerializeField] private TextMeshProUGUI statusTMP;
        [SerializeField] private TextMeshProUGUI feedbackTMP;

        [Header("Colours")]
        [SerializeField] private Color pipFullColor = new Color(0.13f, 0.96f, 0.88f);
        [SerializeField] private Color pipEmptyColor = new Color(0.16f, 0.25f, 0.34f);
        [SerializeField] private Color infoColor = new Color(0.55f, 0.90f, 1f);
        [SerializeField] private Color warnColor = new Color(1f, 0.35f, 0.60f);

        [Header("Debug")]
        [SerializeField, Tooltip("Overlays spawned/live counts on the feedback line.")]
        private bool showDebugCounters = false;

        private readonly List<Image> pips = new List<Image>();

        private int lastPipsIntegrity = -1;
        private int lastPipsMax = -1;
        private bool initialised;
        private PatchHuntMinigameWorld world;

        private PatchHuntMinigame Minigame => PatchHuntMinigame.MinigameInstance;

        private void Start()
        {
            if (Minigame == null)
                return;

            // Loud reference audit - a broken hand-authored binding must never fail silently.
            ReportIfMissing(integrityPipsRow, "integrityPipsRow");
            ReportIfMissing(statusTMP, "statusTMP");
            ReportIfMissing(feedbackTMP, "feedbackTMP");

            world = Minigame.GetComponent<PatchHuntMinigameWorld>();

            initialised = true;
            ShowFeedback($"Click the red {Minigame.BreachLabel} eruptions before their ring closes - green {Minigame.HoneypotLabel} decoys are traps. Look before you click.", false);
            RefreshHud();
        }

        private void ReportIfMissing(Object reference, string fieldName)
        {
            if (reference == null)
                Debug.LogError($"[PatchHunt] UI reference '{fieldName}' is not assigned - the HUD will misbehave. Fix the PatchHuntMinigameUI prefab.");
        }

        private void Update()
        {
            if (initialised == false || Minigame == null)
                return;

            if (Minigame.IsPaused || Minigame.IsFinished)
                return;

            Minigame.AdvanceSpawning(Time.deltaTime);
            RefreshHud();
        }

        /// <summary>Event line pushed by the world view (patches, leaks, decoy clicks, game over).</summary>
        public void ShowFeedback(string message, bool isWarning)
        {
            if (feedbackTMP == null)
                return;

            feedbackTMP.text = message;
            feedbackTMP.color = isWarning ? warnColor : infoColor;
        }

        private void RefreshHud()
        {
            // Integrity is mirrored in plain text so the HUD survives even a broken
            // pips row - the room state must always be readable somewhere.
            if (statusTMP != null)
                statusTMP.text = $"INTEGRITY {Minigame.CurrentIntegrity}/{Minigame.MaxIntegrity}   •   PATCHED: {Minigame.Patched}   •   COMBO x{Minigame.ComboMultiplier:0.#} ({Minigame.Combo})";

            RefreshIntegrityPips();

            if (showDebugCounters && feedbackTMP != null)
            {
                int spawned = world != null ? world.SpawnedTotal : -1;
                int live = world != null ? world.LiveCount : -1;
                feedbackTMP.text = $"[debug] spawned:{spawned} live:{live}";
                feedbackTMP.color = infoColor;
            }
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
