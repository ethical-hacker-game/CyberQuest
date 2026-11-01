using Minigames.Waves;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Minigames
{
    public class WavesMinigame : MinigameBehaviour
    {
        private static WavesMinigame minigameInstance;

        [SerializeField] private float tollerance = 0.05f;
        [SerializeField] private int linesResolution = 20;
        [SerializeField] private Gradient waveGradientSuccess;

        [Header("Settings")]
        [SerializeField] private WavesMinigame_WaveSettings rangeMin;
        [SerializeField] private WavesMinigame_WaveSettings change;

        [Header("Waves")]
        [SerializeField] private List<WavesMinigame_Wave> waves = new List<WavesMinigame_Wave>();

        private int doneLines = 0;
        private WavesMinigameUI minigameUI;

        public float Tollerance { get => tollerance; }
        public int LinesResolution { get => linesResolution; }
        private Gradient WaveGradientSuccess { get => waveGradientSuccess; }
        public WavesMinigame_WaveSettings RangeMin { get => rangeMin; }
        public WavesMinigame_WaveSettings Change { get => change; }
        public List<WavesMinigame_Wave> Waves { get => waves; }
        public int DoneLines { get => doneLines; }

        public static WavesMinigame MinigameInstance
        {
            get
            {
                if (minigameInstance == null)
                {
                    UnityEngine.Debug.LogError("minigameInstance does not exist");
                }

                return minigameInstance;
            }
        }

        public override void ApplyConfig(TaskMinigameVariant config)
        {
            // Runs after Awake (RandomizeLines already ran) but before Start and before
            // the player can move a slider, so re-tuning the tolerance here is safe.
            base.ApplyConfig(config);

            ApplyDifficulty();

            if (config != null && config.wavesSettings != null && config.wavesSettings.tolerance > 0f)
                tollerance = config.wavesSettings.tolerance;
        }

        private void ApplyDifficulty()
        {
            if (Difficulty == MinigameDifficulty.Default)
                return;

            float toleranceMultiplier;

            switch (Difficulty)
            {
                case MinigameDifficulty.Easy:
                    toleranceMultiplier = 2f;
                    break;
                case MinigameDifficulty.Hard:
                    toleranceMultiplier = 0.5f;
                    break;
                default: // Normal
                    toleranceMultiplier = 1f;
                    break;
            }

            tollerance *= toleranceMultiplier;
        }

        private void Awake()
        {
            minigameInstance = this;
            RandomizeLines();
        }

        protected override void Start()
        {
            base.Start();
            minigameUI = (WavesMinigameUI)MinigamesUIManager.Instance.ActiveMinigameUI;
            minigameUI.SetSliderValues(waves.Select(x => x.PlayerLineSettings).ToList());
            waves[0].SetTargetVisibility(true);
        }

        private void OnDestroy()
        {
            minigameInstance = null;
        }

        public List<float> ChangeLineSettings(List<WavesMinigame_WaveSettings> waveSettings)
        {
            if (MinigameInstance == null)
                return null;

            List<float> syncValues = new List<float>();

            for (int i = 0; i < waveSettings.Count; i++)
            {
                waves[i].PlayerLineSettings.Height = waveSettings[i].Height;
                waves[i].PlayerLineSettings.Width = waveSettings[i].Width;
                waves[i].PlayerLineSettings.HorizontalShift = waveSettings[i].HorizontalShift;
                float syncValue = waves[i].BgLineSettings.GetSyncValue(waves[i].PlayerLineSettings);
                syncValues.Add(syncValue);
            }

            CheckAnswers(syncValues);
            return syncValues;
        }

        private void RandomizeLines()
        {
            for (int i = 0; i < waves.Count; i++)
            {
                waves[i].BgLineSettings.Speed = Random.Range(rangeMin.Speed, change.Speed);
                waves[i].PlayerLineSettings.Speed = waves[i].BgLineSettings.Speed;

                Vector2 heightRandom = GetRandomValues1to5;
                waves[i].BgLineSettings.Height = heightRandom.x;
                waves[i].PlayerLineSettings.Height = i >= 0 ? heightRandom.y : heightRandom.x;

                Vector2 widthRandom = GetRandomValues1to5;
                waves[i].BgLineSettings.Width = widthRandom.x;
                waves[i].PlayerLineSettings.Width = i >= 1 ? widthRandom.y : widthRandom.x;

                Vector2 shiftRandom = GetRandomValues1to5;
                waves[i].BgLineSettings.HorizontalShift = shiftRandom.x;
                waves[i].PlayerLineSettings.HorizontalShift = i >= 2 ? shiftRandom.y : shiftRandom.x;
            }
        }

        private Vector2 GetRandomValues1to5
        {
            get
            {
                float step = 0.25f;
                float bgValue = Mathf.Round(Random.Range(1f, 5f) / step) * step;
                float playerValue;
                do
                {
                    playerValue = Mathf.Round(Random.Range(1f, 5f) / step) * step;
                } while (Mathf.Abs(bgValue - playerValue) < 1f);

                return new Vector2(bgValue, playerValue);
            }
        }

        private void Update()
        {
            UpdateAllLines();
        }

        private void UpdateAllLines()
        {
            for (int i = 0; i < waves.Count; i++)
            {
                waves[i].UpdateWave();
            }
        }

        private void CheckAnswers(List<float> syncValues)
        {
            int newDoneLines = 0;

            for (int i = 0; i < syncValues.Count; i++)
            {
                if (syncValues[i] >= 1f)
                {
                    waves[i].SetLineGradient(waves[i].PlayerLine, WaveGradientSuccess);
                    newDoneLines++;
                }
            }

            if (newDoneLines > doneLines)
            {
                doneLines = newDoneLines;

                int scoreToAdd = ((int)MinigamesController.GetTimerPositive + 34) * 3;
                ChangeScore(scoreToAdd);

                for (int i = 0; i < waves.Count; i++)
                {
                    waves[i].SetTargetVisibility(i <= doneLines);
                }
                //SoundSystem.Instance.PlayActionSound();
            }


            for (int i = 0; i < waves.Count; i++)
            {
                waves[i].SetTargetVisibility(i <= doneLines);
            }

            if (newDoneLines >= waves.Count)
                base.FinishMinigame(true, Score, Summary);
        }
    }
}