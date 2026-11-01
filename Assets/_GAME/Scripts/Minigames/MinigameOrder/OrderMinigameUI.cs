using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Minigames
{
    /// <summary>
    /// Interactive surface for <see cref="OrderMinigame"/>. The shuffled term pool sits
    /// on the left, the numbered sequence slots on the right; clicking a pool term drops
    /// it into the lowest free slot, clicking a placed (unlocked) term takes it back.
    /// CHECK ORDER grades the board through <see cref="OrderMinigame.CheckOrder"/> and
    /// the panels are repainted after each action. Fully UI-driven, like Decrypt.
    /// </summary>
    public class OrderMinigameUI : MinigameUI
    {
        [Header("Headline")]
        [SerializeField, Tooltip("The scenario prompt (states the sequence AND its direction).")]
        private TextMeshProUGUI headlineTMP;

        [Header("Board")]
        [SerializeField, Tooltip("Container the shuffled pool buttons are spawned into (left column).")]
        private RectTransform poolContainer;
        [SerializeField, Tooltip("Container the numbered slot buttons are spawned into (right column).")]
        private RectTransform sequenceContainer;
        [SerializeField] private OrderMinigameUI_WordButton wordButtonPrefab;

        [Header("Actions")]
        [SerializeField] private Button checkButton;
        [SerializeField] private Button resetButton;

        [Header("Readout")]
        [SerializeField, Tooltip("Checks / locked line.")]
        private TextMeshProUGUI statusTMP;
        [SerializeField, Tooltip("Feedback on the last action.")]
        private TextMeshProUGUI feedbackTMP;

        // Neon / cyberpunk palette matching the game's look. Semantics: mint = locked,
        // cyan = info, hot magenta = failed check, steel blue = placed but unverified.
        [Header("Colours")]
        [SerializeField] private Color poolColor = new Color(0.04f, 0.35f, 0.45f);
        [SerializeField] private Color poolUsedColor = new Color(0.05f, 0.12f, 0.20f);
        [SerializeField] private Color slotEmptyColor = new Color(0.02f, 0.07f, 0.18f);
        [SerializeField] private Color slotFilledColor = new Color(0.16f, 0.32f, 0.55f);
        [SerializeField] private Color slotLockedColor = new Color(0.05f, 0.45f, 0.30f);
        [SerializeField] private Color infoColor = new Color(0.55f, 0.90f, 1f);
        [SerializeField] private Color warnColor = new Color(1f, 0.35f, 0.60f);

        // Pool buttons are indexed by ITEM index; slot buttons by SLOT index.
        private readonly Dictionary<int, OrderMinigameUI_WordButton> poolButtons = new Dictionary<int, OrderMinigameUI_WordButton>();
        private readonly List<OrderMinigameUI_WordButton> slotButtons = new List<OrderMinigameUI_WordButton>();
        private bool initialised;

        private OrderMinigame Minigame => OrderMinigame.MinigameInstance;

        private void Start()
        {
            if (Minigame == null)
                return;

            BuildBoard();

            if (checkButton != null)
                checkButton.onClick.AddListener(OnCheckClicked);

            if (resetButton != null)
                resetButton.onClick.AddListener(OnResetClicked);

            initialised = true;
            SetFeedback("Click a term on the left to drop it into the next free slot. Click a placed term to take it back. Checking costs - placing is free.", false);
            RefreshAll();
        }

        private void BuildBoard()
        {
            poolButtons.Clear();
            slotButtons.Clear();

            if (wordButtonPrefab == null || poolContainer == null || sequenceContainer == null)
                return;

            if (headlineTMP != null)
                headlineTMP.text = Minigame.Headline;

            // Pool column in the pre-shuffled display order.
            foreach (int itemIndex in Minigame.PoolOrder)
            {
                OrderMinigameUI_WordButton view = Instantiate(wordButtonPrefab, poolContainer);
                view.gameObject.SetActive(true);
                view.Setup(itemIndex, Minigame.GetItemText(itemIndex), OnPoolClicked);
                poolButtons.Add(itemIndex, view);
            }

            // Numbered slot column.
            for (int slot = 0; slot < Minigame.ItemCount; slot++)
            {
                OrderMinigameUI_WordButton view = Instantiate(wordButtonPrefab, sequenceContainer);
                view.gameObject.SetActive(true);
                view.Setup(slot, string.Empty, OnSlotClicked);
                slotButtons.Add(view);
            }
        }

        private void OnPoolClicked(int itemIndex)
        {
            if (Minigame.PlaceItem(itemIndex))
                SetFeedback("Placed. Fill every slot, then press CHECK ORDER.", false);

            RefreshAll();
        }

        private void OnSlotClicked(int slotIndex)
        {
            if (Minigame.RemoveFromSlot(slotIndex))
                SetFeedback("Returned to the pool.", false);

            RefreshAll();
        }

        private void OnCheckClicked()
        {
            OrderCheckResult result = Minigame.CheckOrder();

            if (result.handled)
                SetFeedback(result.message, result.solved == false && result.lockedNew == false);

            RefreshAll();
        }

        private void OnResetClicked()
        {
            Minigame.ResetUnlocked();
            SetFeedback("Unlocked terms returned to the pool.", false);
            RefreshAll();
        }

        private void RefreshAll()
        {
            RefreshPool();
            RefreshSlots();
            RefreshStatus();
            RefreshInteractable();
        }

        private void RefreshPool()
        {
            foreach (KeyValuePair<int, OrderMinigameUI_WordButton> pair in poolButtons)
            {
                bool placed = Minigame.IsItemPlaced(pair.Key);
                pair.Value.SetBackground(placed ? poolUsedColor : poolColor);
                pair.Value.SetLabelAlpha(placed ? 0.25f : 1f);
                pair.Value.SetInteractable(placed == false && Minigame.IsDone == false);
            }
        }

        private void RefreshSlots()
        {
            for (int slot = 0; slot < slotButtons.Count; slot++)
            {
                OrderMinigameUI_WordButton view = slotButtons[slot];
                int item = Minigame.GetSlotItem(slot);
                bool locked = Minigame.IsSlotLocked(slot);

                if (item == -1)
                {
                    view.SetLabel($"{slot + 1}.  · · ·");
                    view.SetBackground(slotEmptyColor);
                    view.SetLabelAlpha(0.45f);
                }
                else
                {
                    string prefix = locked ? "✓ " : string.Empty;
                    view.SetLabel($"{slot + 1}.  {prefix}{Minigame.GetItemText(item)}");
                    view.SetBackground(locked ? slotLockedColor : slotFilledColor);
                    view.SetLabelAlpha(1f);
                }

                // Locked and empty slots are not clickable - only placed, unverified terms.
                view.SetInteractable(item != -1 && locked == false && Minigame.IsDone == false);
            }
        }

        private void RefreshStatus()
        {
            if (statusTMP == null)
                return;

            if (Minigame.IsDone)
            {
                statusTMP.text = "Console closed.";
                return;
            }

            statusTMP.text = $"Checks left: {Minigame.ChecksLeft}   •   Locked: {Minigame.LockedCount} / {Minigame.ItemCount}";
        }

        private void RefreshInteractable()
        {
            bool active = Minigame.IsDone == false;

            if (checkButton != null)
                checkButton.interactable = active && Minigame.AllSlotsFilled;

            if (resetButton != null)
                resetButton.interactable = active;
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
