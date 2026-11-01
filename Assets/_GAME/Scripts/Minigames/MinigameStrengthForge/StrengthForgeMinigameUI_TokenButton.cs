using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Minigames
{
    /// <summary>
    /// One selectable token in the pool grid: its text, its memorability cost and a
    /// category tint. Greys out while the token is forged or cannot be afforded.
    /// </summary>
    public class StrengthForgeMinigameUI_TokenButton : MonoBehaviour
    {
        [SerializeField] private Button button;
        [SerializeField] private TextMeshProUGUI labelTMP;
        [SerializeField] private TextMeshProUGUI costTMP;
        [SerializeField] private Image categoryTint;

        [Header("Category colors")]
        [SerializeField] private Color commonWordColor = new Color(0.75f, 0.72f, 0.55f);
        [SerializeField] private Color breachedColor = new Color(0.85f, 0.45f, 0.45f);
        [SerializeField] private Color leetWordColor = new Color(0.80f, 0.60f, 0.85f);
        [SerializeField] private Color randomWordColor = new Color(0.45f, 0.75f, 0.90f);
        [SerializeField] private Color digitsColor = new Color(0.55f, 0.80f, 0.60f);
        [SerializeField] private Color symbolColor = new Color(0.65f, 0.65f, 0.70f);

        private StrengthToken token;
        private Action<StrengthToken> onClicked;

        public void Setup(StrengthToken token, Action<StrengthToken> onClicked)
        {
            this.token = token;
            this.onClicked = onClicked;

            if (labelTMP != null)
                labelTMP.text = token.text;

            if (costTMP != null)
                costTMP.text = token.EffectiveCost.ToString();

            if (categoryTint != null)
                categoryTint.color = GetCategoryColor(token.category);

            if (button != null)
            {
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(() => this.onClicked?.Invoke(this.token));
            }
        }

        /// <summary>
        /// Reflects whether the token can currently be added (slots, budget, not
        /// already in the forge).
        /// </summary>
        public void SetAvailable(bool available)
        {
            if (button != null)
                button.interactable = available;
        }

        private Color GetCategoryColor(StrengthTokenCategory category)
        {
            switch (category)
            {
                case StrengthTokenCategory.BreachedFragment: return breachedColor;
                case StrengthTokenCategory.LeetWord: return leetWordColor;
                case StrengthTokenCategory.RandomWord: return randomWordColor;
                case StrengthTokenCategory.Digits: return digitsColor;
                case StrengthTokenCategory.Symbol: return symbolColor;
                default: return commonWordColor;
            }
        }
    }
}
