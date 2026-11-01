using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Minigames
{
    /// <summary>
    /// One live attack meter: the attack's name, the current estimated crack time and
    /// a log-scaled bar that turns green once the round's requirement is met.
    /// </summary>
    public class StrengthForgeMinigameUI_AttackMeter : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI nameTMP;
        [SerializeField] private TextMeshProUGUI timeTMP;
        [SerializeField] private Image fillImage;

        [Header("Colors")]
        [SerializeField] private Color failColor = new Color(0.90f, 0.36f, 0.36f);
        [SerializeField] private Color passColor = new Color(0.42f, 0.85f, 0.45f);

        // The requirement sits at this fraction of the bar, leaving visible headroom
        // for overshoot so "barely passed" and "obliterated it" read differently.
        private const float ThresholdFillPoint = 0.75f;

        public void Setup(StrengthAttackType attack)
        {
            if (nameTMP != null)
                nameTMP.text = GetAttackLabel(attack);

            UpdateMeter(0, 1);
        }

        public void UpdateMeter(double crackSeconds, double requiredSeconds)
        {
            bool passed = crackSeconds >= requiredSeconds;

            if (timeTMP != null)
            {
                timeTMP.text = StrengthForgeMinigame.FormatDuration(crackSeconds);
                timeTMP.color = passed ? passColor : failColor;
            }

            if (fillImage != null)
            {
                fillImage.fillAmount = GetLogFill(crackSeconds, requiredSeconds);
                fillImage.color = passed ? passColor : failColor;
            }
        }

        /// <summary>
        /// Crack times span dozens of orders of magnitude, so the bar is log-scaled:
        /// the requirement maps to <see cref="ThresholdFillPoint"/> and two extra
        /// orders of magnitude fill the rest.
        /// </summary>
        private static float GetLogFill(double crackSeconds, double requiredSeconds)
        {
            if (double.IsInfinity(crackSeconds))
                return 1f;

            if (crackSeconds < 1 || requiredSeconds <= 1)
                return crackSeconds >= requiredSeconds ? 1f : 0f;

            double logCrack = System.Math.Log10(crackSeconds);
            double logRequired = System.Math.Log10(requiredSeconds);

            if (logCrack <= logRequired)
                return ThresholdFillPoint * (float)(logCrack / logRequired);

            double overshoot = (logCrack - logRequired) / 2.0; // 2 extra orders = full bar
            return ThresholdFillPoint + (1f - ThresholdFillPoint) * Mathf.Clamp01((float)overshoot);
        }

        private static string GetAttackLabel(StrengthAttackType attack)
        {
            switch (attack)
            {
                case StrengthAttackType.Dictionary: return "Dictionary attack";
                case StrengthAttackType.Hybrid: return "Hybrid rules (leet, suffixes)";
                case StrengthAttackType.BruteForce: return "Brute force";
                default: return attack.ToString();
            }
        }
    }
}
