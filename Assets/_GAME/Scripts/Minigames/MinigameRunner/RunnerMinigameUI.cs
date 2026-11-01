using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Minigames
{
    /// <summary>
    /// Transparent overlay for the 3D Packet Rush: the two big side buttons (one on each
    /// edge of the screen) plus A/D and the arrow keys for input, and the HUD strip
    /// (integrity pips + text, re-route counter, feedback line). The world itself is
    /// rendered by <see cref="RunnerMinigameWorld"/>; this class also keeps driving
    /// <see cref="RunnerMinigame.AdvanceSpawning"/> from its Update - the path proven
    /// to tick reliably - while the world only consumes the wave queue.
    /// Timer and score are drawn by the shared MinigameMainUI.
    /// </summary>
    public class RunnerMinigameUI : MinigameUI
    {
        [Header("Controls")]
        [SerializeField] private Button leftButton;
        [SerializeField] private Button rightButton;

        [Header("Readout")]
        [SerializeField, Tooltip("Row the integrity pips are spawned into.")]
        private RectTransform integrityPipsRow;
        [SerializeField] private TextMeshProUGUI dodgesTMP;
        [SerializeField] private TextMeshProUGUI feedbackTMP;

        [Header("Colours")]
        [SerializeField] private Color pipFullColor = new Color(0.13f, 0.96f, 0.88f);
        [SerializeField] private Color pipEmptyColor = new Color(0.16f, 0.25f, 0.34f);
        [SerializeField] private Color infoColor = new Color(0.55f, 0.90f, 1f);
        [SerializeField] private Color warnColor = new Color(1f, 0.35f, 0.60f);

        [Header("Debug")]
        [SerializeField, Tooltip("Overlays spawned/live counts and speed on the feedback line.")]
        private bool showDebugCounters = false;

        private readonly List<Image> pips = new List<Image>();

        private int lastPipsIntegrity = -1;
        private int lastPipsMax = -1;
        private bool initialised;
        private RunnerMinigameWorld world;

        private RunnerMinigame Minigame => RunnerMinigame.MinigameInstance;

        private void Start()
        {
            if (Minigame == null)
                return;

            // Loud reference audit - a broken hand-authored binding must never fail silently.
            ReportIfMissing(leftButton, "leftButton");
            ReportIfMissing(rightButton, "rightButton");
            ReportIfMissing(integrityPipsRow, "integrityPipsRow");
            ReportIfMissing(dodgesTMP, "dodgesTMP");
            ReportIfMissing(feedbackTMP, "feedbackTMP");

            if (leftButton != null)
                leftButton.onClick.AddListener(() => OnMovePressed(-1));

            if (rightButton != null)
                rightButton.onClick.AddListener(() => OnMovePressed(1));

            world = Minigame.GetComponent<RunnerMinigameWorld>();

            initialised = true;
            ShowFeedback("Keep the payload on a clear fiber - hostile hops ahead. Steer with the side buttons or A / D.", false);
            RefreshHud();
        }

        private void ReportIfMissing(Object reference, string fieldName)
        {
            if (reference == null)
                Debug.LogError($"[PacketRush] UI reference '{fieldName}' is not assigned - the HUD will misbehave. Fix the RunnerMinigameUI prefab.");
        }

        private void Update()
        {
            if (initialised == false || Minigame == null)
                return;

            if (Minigame.IsPaused || Minigame.IsFinished)
                return;

            HandleKeyboard();
            Minigame.AdvanceSpawning(Time.deltaTime);
            RefreshHud();
        }

        private void HandleKeyboard()
        {
            if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A))
                OnMovePressed(-1);

            if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D))
                OnMovePressed(1);
        }

        private void OnMovePressed(int direction)
        {
            Minigame.TryMove(direction);
        }

        /// <summary>Event line pushed by the world view (hits, collects, game over).</summary>
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
            // pips row - the packet state must always be readable somewhere.
            if (dodgesTMP != null)
                dodgesTMP.text = $"INTEGRITY {Minigame.CurrentIntegrity}/{Minigame.MaxIntegrity}   •   RE-ROUTES: {Minigame.Dodges}";

            RefreshIntegrityPips();

            if (showDebugCounters && feedbackTMP != null)
            {
                int spawned = world != null ? world.SpawnedTotal : -1;
                int live = world != null ? world.LiveCount : -1;
                feedbackTMP.text = $"[debug] spawned:{spawned} live:{live} speed:{Minigame.ScrollSpeed:0.00}";
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
