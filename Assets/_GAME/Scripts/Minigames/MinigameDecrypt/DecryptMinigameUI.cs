using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Minigames
{
    /// <summary>
    /// Interactive surface for <see cref="DecryptMinigame"/>. Shows the intercepted text,
    /// the free signal-analysis panel, one button per decode tool and the Caesar key
    /// picker; every decode goes through <see cref="DecryptMinigame.ApplyTool"/> and the
    /// panels are repainted after each action. Fully UI-driven, like Firewall and Trace.
    /// </summary>
    public class DecryptMinigameUI : MinigameUI
    {
        [Header("Cipher display")]
        [SerializeField, Tooltip("The intercepted message in its current (partially decoded) form.")]
        private TextMeshProUGUI cipherTMP;
        [SerializeField, Tooltip("Free signature-analysis observations about the current text.")]
        private TextMeshProUGUI analysisTMP;

        [Header("Tools")]
        [SerializeField, Tooltip("Row the one-click decode tool buttons are spawned into.")]
        private RectTransform toolsContainer;
        [SerializeField] private DecryptMinigameUI_ToolButton toolButtonPrefab;

        [Header("Caesar controls")]
        [SerializeField] private Button caesarMinusButton;
        [SerializeField] private Button caesarPlusButton;
        [SerializeField] private Button caesarApplyButton;
        [SerializeField] private TextMeshProUGUI caesarShiftTMP;

        [Header("Readout")]
        [SerializeField, Tooltip("Attempts / layers line.")]
        private TextMeshProUGUI statusTMP;
        [SerializeField, Tooltip("Feedback on the last decode attempt.")]
        private TextMeshProUGUI feedbackTMP;

        // Neon / cyberpunk palette matching the game's look. Semantics: mint = recovered
        // plaintext / success, cyan = analysis and info, hot magenta = wrong decode.
        [Header("Colours")]
        [SerializeField] private Color cipherColor = new Color(0.15f, 0.95f, 0.70f);
        [SerializeField] private Color infoColor = new Color(0.55f, 0.90f, 1f);
        [SerializeField] private Color warnColor = new Color(1f, 0.35f, 0.60f);

        private readonly List<DecryptMinigameUI_ToolButton> toolButtons = new List<DecryptMinigameUI_ToolButton>();
        private int caesarShift = 1;
        private bool initialised;

        private DecryptMinigame Minigame => DecryptMinigame.MinigameInstance;

        private void Start()
        {
            if (Minigame == null)
                return;

            BuildToolButtons();

            if (caesarMinusButton != null)
                caesarMinusButton.onClick.AddListener(() => ChangeShift(-1));

            if (caesarPlusButton != null)
                caesarPlusButton.onClick.AddListener(() => ChangeShift(1));

            if (caesarApplyButton != null)
                caesarApplyButton.onClick.AddListener(OnCaesarApply);

            initialised = true;
            SetFeedback("Read the analysis panel, then pick the decoder matching the OUTERMOST layer.", false);
            RefreshAll();
        }

        private void BuildToolButtons()
        {
            toolButtons.Clear();

            if (toolButtonPrefab == null || toolsContainer == null)
                return;

            SpawnToolButton(DecryptStepType.Base64, "BASE64 DECODE");
            SpawnToolButton(DecryptStepType.Hex, "HEX DECODE");
            SpawnToolButton(DecryptStepType.Reverse, "REVERSE");
            SpawnToolButton(DecryptStepType.Atbash, "ATBASH MIRROR");
        }

        private void SpawnToolButton(DecryptStepType type, string label)
        {
            DecryptMinigameUI_ToolButton view = Instantiate(toolButtonPrefab, toolsContainer);
            view.gameObject.SetActive(true);
            view.Setup(type, label, OnToolClicked);
            toolButtons.Add(view);
        }

        private void OnToolClicked(DecryptStepType type)
        {
            DecryptApplyResult result = Minigame.ApplyTool(type);

            if (result.handled)
                SetFeedback(result.message, result.accepted == false);

            RefreshAll();
        }

        private void OnCaesarApply()
        {
            DecryptApplyResult result = Minigame.ApplyTool(DecryptStepType.Caesar, caesarShift);

            if (result.handled)
                SetFeedback(result.message, result.accepted == false);

            RefreshAll();
        }

        private void ChangeShift(int delta)
        {
            // Wraps within 1..MaxCaesarShift - a handful of keys, not the whole alphabet
            // (shift 0 would be a no-op decode, so it is excluded).
            int max = DecryptCipher.MaxCaesarShift;
            caesarShift = ((caesarShift - 1 + delta + max) % max) + 1;
            RefreshCaesarLabel();
        }

        private void RefreshAll()
        {
            RefreshCipher();
            RefreshAnalysis();
            RefreshStatus();
            RefreshCaesarLabel();
            RefreshInteractable();
        }

        private void RefreshCipher()
        {
            if (cipherTMP == null)
                return;

            // The working text is data, not markup - keep TMP from parsing stray '<'.
            cipherTMP.richText = false;
            cipherTMP.text = Minigame.WorkingText;
            cipherTMP.color = cipherColor;
        }

        private void RefreshAnalysis()
        {
            if (analysisTMP != null)
                analysisTMP.text = Minigame.GetAnalysis();
        }

        private void RefreshStatus()
        {
            if (statusTMP == null)
                return;

            if (Minigame.IsDone)
            {
                statusTMP.text = "Transmission closed.";
                return;
            }

            string layers = Minigame.LayersVisible
                ? $"   •   Layers remaining: {Minigame.LayersRemaining}"
                : string.Empty;

            statusTMP.text = $"Wrong-decode attempts left: {Minigame.AttemptsLeft}{layers}";
        }

        private void RefreshCaesarLabel()
        {
            if (caesarShiftTMP != null)
                caesarShiftTMP.text = $"CAESAR SHIFT: {caesarShift}";
        }

        private void RefreshInteractable()
        {
            bool active = Minigame.IsDone == false;

            foreach (DecryptMinigameUI_ToolButton view in toolButtons)
                view.SetInteractable(active);

            if (caesarMinusButton != null) caesarMinusButton.interactable = active;
            if (caesarPlusButton != null) caesarPlusButton.interactable = active;
            if (caesarApplyButton != null) caesarApplyButton.interactable = active;
        }

        private void SetFeedback(string message, bool isWarning)
        {
            if (feedbackTMP == null)
                return;

            feedbackTMP.text = message;
            feedbackTMP.color = isWarning ? warnColor : infoColor;
        }

        public override void UpdateUI()
        {
            // Called once on the source prefab before the live copy exists; safe to no-op
            // until this instance has run its own Start.
            if (initialised)
                RefreshAll();
        }
    }
}
