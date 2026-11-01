using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Linq;

namespace Minigames.Waves
{
    public class WavesMinigameUI : MinigameUI
    {
        [SerializeField] private List<WavesMinigameUI_Sliders> waveUISettings = new List<WavesMinigameUI_Sliders>();

        private List<float> syncValues = new List<float>();

        public void SetSliderValues(List<WavesMinigame_WaveSettings> settings)
        {
            for (int i = 0; i < settings.Count; i++)
            {
                waveUISettings[i].HeightSlider.SetValueWithoutNotify(settings[i].Height);
                waveUISettings[i].WidthSlider.SetValueWithoutNotify(settings[i].Width);
                waveUISettings[i].ShiftSlider.SetValueWithoutNotify(settings[i].HorizontalShift);

                for (int s = 0; s < 3; s++)
                {
                    waveUISettings[i].ToggleSlider(s, i <= s);
                }
            }

            ChangeWave();
        }

        public void ChangeWave()
        {
            List<WavesMinigame_WaveSettings> newValues = new List<WavesMinigame_WaveSettings>();

            for (int i = 0; i < waveUISettings.Count; i++)
            {
                WavesMinigame_WaveSettings tempSettings = new WavesMinigame_WaveSettings();
                tempSettings.Height = waveUISettings[i].HeightSlider.value;
                tempSettings.Width = waveUISettings[i].WidthSlider.value;
                tempSettings.HorizontalShift = waveUISettings[i].ShiftSlider.value;
                newValues.Add(tempSettings);
            }

            syncValues = WavesMinigame.MinigameInstance.ChangeLineSettings(newValues);
            UpdateUI();
        }

        public override void UpdateUI()
        {
            if (waveUISettings.Count != WavesMinigame.MinigameInstance.Waves.Count)
            {
                Debug.LogWarning("Different buttons and waves counts");
                return;
            }

            for (int i = 0; i < syncValues.Count; i++)
            {
                waveUISettings[i].ToggleSliders(syncValues[i] < 1f);
                waveUISettings[i].SetWaveValue(syncValues[i]);
                waveUISettings[i].ToggleVisibility(WavesMinigame.MinigameInstance.DoneLines >= i);
            }
        }
    }
}