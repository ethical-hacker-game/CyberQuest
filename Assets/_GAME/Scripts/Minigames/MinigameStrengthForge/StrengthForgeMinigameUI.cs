using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Minigames
{
    /// <summary>
    /// The interactive surface for <see cref="StrengthForgeMinigame"/>. Owns the round
    /// header, the token pool grid, the forge bar and the live attack meters, and
    /// drives everything through the minigame's public actions. All gameplay is
    /// UI-driven (no world-space objects), like <see cref="FirewallMinigameUI"/>.
    /// </summary>
    public class StrengthForgeMinigameUI : MinigameUI
    {
        [Header("Round header")]
        [SerializeField] private TextMeshProUGUI roundLabelTMP;
        [SerializeField] private TextMeshProUGUI roundDescriptionTMP;
        [SerializeField] private TextMeshProUGUI objectiveTMP;

        [Header("Token pool")]
        [SerializeField] private RectTransform tokenPoolContent;
        [SerializeField] private StrengthForgeMinigameUI_TokenButton tokenButtonPrefab;

        [Header("Forge bar")]
        [SerializeField] private RectTransform forgedContent;
        [SerializeField] private StrengthForgeMinigameUI_ForgedToken forgedTokenPrefab;
        [SerializeField] private TextMeshProUGUI passwordPreviewTMP;
        [SerializeField] private TextMeshProUGUI budgetTMP;

        [Header("Attack meters")]
        [SerializeField] private RectTransform metersContent;
        [SerializeField] private StrengthForgeMinigameUI_AttackMeter attackMeterPrefab;

        [Header("Controls")]
        [SerializeField] private Button forgeButton;
        [SerializeField] private Button clearButton;
        [SerializeField] private TextMeshProUGUI feedbackTMP;

        [Header("Feedback colors")]
        [SerializeField] private Color infoColor = new Color(0.75f, 0.78f, 0.82f);
        [SerializeField] private Color successColor = new Color(0.42f, 0.85f, 0.45f);
        [SerializeField] private Color failColor = new Color(0.90f, 0.36f, 0.36f);

        private readonly List<StrengthForgeMinigameUI_TokenButton> tokenButtons = new List<StrengthForgeMinigameUI_TokenButton>();
        private readonly List<StrengthForgeMinigameUI_ForgedToken> forgedChips = new List<StrengthForgeMinigameUI_ForgedToken>();
        private readonly List<StrengthForgeMinigameUI_AttackMeter> meters = new List<StrengthForgeMinigameUI_AttackMeter>();

        private StrengthForgeMinigame Minigame => StrengthForgeMinigame.MinigameInstance;
        private bool initialised;

        private void Start()
        {
            if (Minigame == null)
                return;

            ClearAuthoringPlaceholders();

            if (forgeButton != null)
                forgeButton.onClick.AddListener(OnForge);
            if (clearButton != null)
                clearButton.onClick.AddListener(OnClear);

            BuildTokenPool();
            BuildRound();

            initialised = true;
        }

        /// <summary>
        /// The dynamic containers may hold design-time placeholder rows (kept in the
        /// prefab to visualise the layout). Everything inside them is spawned from code
        /// at runtime, so wipe them before the first build. A referenced prefab that
        /// happens to live inside its own container (template-as-child pattern) is kept
        /// but hidden, since Instantiate still needs it as the source.
        /// </summary>
        private void ClearAuthoringPlaceholders()
        {
            ClearContainer(tokenPoolContent, tokenButtonPrefab != null ? tokenButtonPrefab.transform : null);
            ClearContainer(forgedContent, forgedTokenPrefab != null ? forgedTokenPrefab.transform : null);
            ClearContainer(metersContent, attackMeterPrefab != null ? attackMeterPrefab.transform : null);
        }

        private static void ClearContainer(RectTransform container, Transform keepTemplate)
        {
            if (container == null)
                return;

            for (int i = container.childCount - 1; i >= 0; i--)
            {
                Transform child = container.GetChild(i);

                if (keepTemplate != null && child == keepTemplate)
                {
                    child.gameObject.SetActive(false);
                    continue;
                }

                Destroy(child.gameObject);
            }
        }

        /// <summary>
        /// Rebuilds everything that changes between rounds: header, meters, forge bar.
        /// The token pool stays - only its availability states refresh.
        /// </summary>
        private void BuildRound()
        {
            StrengthForgeRound round = Minigame.CurrentRound;

            if (round == null)
                return;

            if (roundLabelTMP != null)
                roundLabelTMP.text = $"Account {Minigame.RoundIndex + 1}/{Minigame.RoundsTotal}: {round.label}";

            if (roundDescriptionTMP != null)
                roundDescriptionTMP.text = round.description;

            if (objectiveTMP != null)
                objectiveTMP.text = $"Must survive ≥ {StrengthForgeMinigame.FormatDuration(round.requiredCrackSeconds)}" +
                                    $" at {StrengthForgeMinigame.FormatRate(round.guessesPerSecond)}";

            BuildMeters(round);
            RefreshForge();
        }

        private void BuildTokenPool()
        {
            ClearAll(tokenButtons);

            if (tokenButtonPrefab == null || tokenPoolContent == null)
                return;

            foreach (StrengthToken token in Minigame.TokenPool)
            {
                StrengthForgeMinigameUI_TokenButton tokenButton = Instantiate(tokenButtonPrefab, tokenPoolContent);
                tokenButton.gameObject.SetActive(true);
                tokenButton.Setup(token, OnTokenClicked);
                tokenButtons.Add(tokenButton);
            }
        }

        private void BuildMeters(StrengthForgeRound round)
        {
            ClearAll(meters);

            if (attackMeterPrefab == null || metersContent == null)
                return;

            foreach (StrengthAttackType attack in round.ActiveAttacks)
            {
                StrengthForgeMinigameUI_AttackMeter meter = Instantiate(attackMeterPrefab, metersContent);
                meter.gameObject.SetActive(true);
                meter.Setup(attack);
                meters.Add(meter);
            }
        }

        private void OnTokenClicked(StrengthToken token)
        {
            if (Minigame.AddToken(token) == false)
                return;

            RefreshForge();
        }

        private void OnForgedChipClicked(int index)
        {
            Minigame.RemoveForgedAt(index);
            RefreshForge();
        }

        private void OnClear()
        {
            Minigame.ClearForge();
            RefreshForge();
        }

        private void OnForge()
        {
            StrengthForgeForgeResult result = Minigame.Forge();

            SetFeedback(result.message, result.passed ? successColor : failColor);

            if (result.passed && Minigame.CurrentRound != null)
            {
                // Next account: new header, new meters, empty forge.
                BuildRound();
            }
            else if (result.passed == false)
            {
                RefreshForge();
            }
        }

        /// <summary>
        /// Redraws the forge bar, availability states and the live meters after any
        /// edit. This is the heart of the game loop: every click shows its consequence
        /// on the attack meters immediately.
        /// </summary>
        private void RefreshForge()
        {
            StrengthForgeRound round = Minigame.CurrentRound;

            if (round == null)
                return;

            // Forge chips.
            ClearAll(forgedChips);

            if (forgedTokenPrefab != null && forgedContent != null)
            {
                IReadOnlyList<StrengthToken> forged = Minigame.ForgedTokens;
                for (int i = 0; i < forged.Count; i++)
                {
                    StrengthForgeMinigameUI_ForgedToken chip = Instantiate(forgedTokenPrefab, forgedContent);
                    chip.gameObject.SetActive(true);
                    chip.Setup(forged[i], i, OnForgedChipClicked);
                    forgedChips.Add(chip);
                }
            }

            if (passwordPreviewTMP != null)
            {
                string preview = Minigame.PasswordPreview;
                passwordPreviewTMP.text = string.IsNullOrEmpty(preview) ? "<i>empty</i>" : preview;
            }

            if (budgetTMP != null)
            {
                budgetTMP.text = $"Memorability: {Minigame.UsedBudget}/{round.memorabilityBudget}" +
                                 $"   •   Tokens: {Minigame.ForgedTokens.Count}/{round.maxTokens}";
            }

            // Pool availability.
            for (int i = 0; i < tokenButtons.Count && i < Minigame.TokenPool.Count; i++)
            {
                StrengthToken token = Minigame.TokenPool[i];
                tokenButtons[i].SetAvailable(Minigame.CanAddToken(token));
            }

            // Live meters + hint.
            StrengthForgeEvaluation eval = Minigame.Evaluate();

            for (int i = 0; i < meters.Count && i < eval.attackResults.Count; i++)
                meters[i].UpdateMeter(eval.attackResults[i].crackSeconds, round.requiredCrackSeconds);

            if (forgeButton != null)
                forgeButton.interactable = Minigame.ForgedTokens.Count > 0;

            SetFeedback(eval.weaknessHint, eval.Passed ? successColor : infoColor);
        }

        private void SetFeedback(string message, Color color)
        {
            if (feedbackTMP == null)
                return;

            feedbackTMP.text = message;
            feedbackTMP.color = color;
        }

        private static void ClearAll<T>(List<T> items) where T : Component
        {
            foreach (T item in items)
            {
                if (item != null)
                    Destroy(item.gameObject);
            }

            items.Clear();
        }

        public override void UpdateUI()
        {
            // Called once on the source prefab before the live copy exists, and it is
            // safe to no-op until this instance has run its own Start.
            if (initialised)
                RefreshForge();
        }
    }
}
