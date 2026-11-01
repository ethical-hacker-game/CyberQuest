using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Minigames
{
    public class ConnectionsMinigameUI : MinigameUI
    {
        [SerializeField] private Image safetyBar;

        [Space]
        [SerializeField] private Image vignette;
        [SerializeField] private Color positiveColor;
        [SerializeField] private Color negativeColor;
        [SerializeField] private AnimationCurve vignetteColorCurve;

        private bool isVignetteActive = false;
        private float vignetteTimer = 0;
        private float vignetteDuration = 1f;
        private Color vignetteTargetColor = Color.clear;

        public override void UpdateUI()
        {
            if (ConnectionsMinigame.MinigameInstance == null || safetyBar == null)
                return;

            float targetFill = (float)ConnectionsMinigame.MinigameInstance.CurrentStrength / ConnectionsMinigame.MinigameInstance.GetRealInitialStrenght;
            safetyBar.fillAmount = Mathf.MoveTowards(safetyBar.fillAmount, targetFill, Time.deltaTime);
        }

        public void ShowVignette(bool isPositive, float timer)
        {
            isVignetteActive = true;
            vignetteTimer = 0f;
            vignetteDuration = timer;
            vignetteTargetColor = isPositive? positiveColor : negativeColor;
        }


        private void Update()
        {
            UpdateUI();
            UpdateVignetteColor();
        }

        private void UpdateVignetteColor()
        {
            if (isVignetteActive == false)
                return;

            vignetteTimer += Time.deltaTime;
            float timerProgress = vignetteTimer / vignetteDuration;

            Color tempColor = new Color(
                vignetteTargetColor.r,
                vignetteTargetColor.g,
                vignetteTargetColor.b,
                vignetteTargetColor.a * vignetteColorCurve.Evaluate(timerProgress)
                );

            vignette.color = tempColor;

            if (timerProgress >= 1f)
            {
                isVignetteActive = false;
                vignette.color = Color.clear;
            }
        }
    }
}