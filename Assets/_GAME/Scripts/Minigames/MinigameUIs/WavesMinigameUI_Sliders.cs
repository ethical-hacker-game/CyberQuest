using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Minigames.Waves
{
    public class WavesMinigameUI_Sliders : MonoBehaviour
    {
        [SerializeField] private Gradient gradient;
        [SerializeField] private TextMeshProUGUI waveSyncTMP;
        [SerializeField] private CanvasGroup canvasGroup;

        [SerializeField] private Slider heightSlider;
        [SerializeField] private Image heightSliderFill;

        [SerializeField] private Slider widthSlider;
        [SerializeField] private Image widthSliderFill;

        [SerializeField] private Slider shiftSlider;
        [SerializeField] private Image shiftSliderFill;

        [SerializeField] private Color activeSliderColor;
        [SerializeField] private Color inactiveSliderColor;

        private WavesMinigameUI parentUI;
        private bool targetState = false;

        public TextMeshProUGUI WaveSyncTMP { get => waveSyncTMP; }
        public Slider HeightSlider { get => heightSlider; }
        public Slider WidthSlider { get => widthSlider; }
        public Slider ShiftSlider { get => shiftSlider; }

        public void ToggleVisibility(bool state)
        {
            targetState = state;
        }

        public void ToggleSliders(bool state)
        {
            ToggleSlider(0, state);
            ToggleSlider(1, state);
            ToggleSlider(2, state);
        }

        public void ToggleSlider(int sliderIndex, bool state)
        {
            if(transform.GetSiblingIndex() < sliderIndex)
                state = false;

            Color sliderColor = state ? activeSliderColor : inactiveSliderColor;

            switch (sliderIndex)
            {
                case 0:
                    heightSlider.interactable = state;
                    heightSliderFill.color = sliderColor;
                    break;
                case 1:
                    widthSlider.interactable = state;
                    widthSliderFill.color = sliderColor;
                    break;
                case 2:
                    shiftSlider.interactable = state;
                    shiftSliderFill.color = sliderColor;
                    break;
                default:
                    break;
            }
        }

        public void SetWaveValue(float value)
        {
            value.ClampFloat(0f, 1f);
            waveSyncTMP.text = (int)(value * 100) + "%";
            waveSyncTMP.color = gradient.Evaluate(value);
        }

        public void ChangeWave()
        {
            if (parentUI == null)
                return;

            parentUI.ChangeWave();
        }

        private void Awake()
        {
            parentUI = GetComponentInParent<WavesMinigameUI>();

            heightSlider.onValueChanged.AddListener(delegate { ChangeWave(); });
            widthSlider.onValueChanged.AddListener(delegate { ChangeWave(); });
            shiftSlider.onValueChanged.AddListener(delegate { ChangeWave(); });

            canvasGroup.alpha = 0;
        }

        private void Update()
        {
            float targetAlpha = targetState ? 1f : 0f;
            canvasGroup.alpha = Mathf.MoveTowards(canvasGroup.alpha, targetAlpha, Time.deltaTime * 0.4f);
        }
    }
}
