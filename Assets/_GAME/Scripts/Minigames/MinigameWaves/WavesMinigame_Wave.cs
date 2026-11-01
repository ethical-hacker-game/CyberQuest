using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Minigames.Waves
{
    public class WavesMinigame_Wave : MonoBehaviour
    {
        [SerializeField] private LineRenderer bgLine;
        [SerializeField] private LineRenderer playerLine;

        [SerializeField] private float lineLenght = 10f;
        [SerializeField] private float lineWeight = 0.05f;
        [SerializeField] private float lineBgWeight = 0.04f;
        [SerializeField] private WavesMinigame_WaveSettings bgLineSettings;
        [SerializeField] private WavesMinigame_WaveSettings playerLineSettings;

        private float targetLineWidth = 0;

        public LineRenderer BgLine => bgLine;
        public LineRenderer PlayerLine => playerLine;
        public float LineLenght => lineLenght;
        public WavesMinigame_WaveSettings BgLineSettings { get => bgLineSettings; set => bgLineSettings = value; }
        public WavesMinigame_WaveSettings PlayerLineSettings { get => playerLineSettings; set => playerLineSettings = value; }

        private int LineResolution => WavesMinigame.MinigameInstance.LinesResolution;
        private WavesMinigame_WaveSettings RangeMin => WavesMinigame.MinigameInstance.RangeMin;
        private WavesMinigame_WaveSettings Change => WavesMinigame.MinigameInstance.Change;

        public void SetTargetVisibility(bool isVisible)
        {
            targetLineWidth = isVisible ? 1 : 0;
        }

        public void SetLineGradient(LineRenderer line, Gradient gradient)
        {
            line.colorGradient = gradient;
        }

        public void UpdateWave()
        {
            if (BgLine != null)
                UpdateLine(BgLine, BgLineSettings);

            if (PlayerLine != null)
            {
                UpdateLine(PlayerLine, PlayerLineSettings);

                //float tempLineWidth = PlayerLine.widthCurve.keys[0].value;
                //tempLineWidth = Mathf.MoveTowards(tempLineWidth, targetLineWidth, Time.deltaTime);
                //PlayerLine.widthCurve = AnimationCurve.Linear(0, tempLineWidth, 1, tempLineWidth);

                float tempLineWidth = PlayerLine.widthMultiplier;
                tempLineWidth = Mathf.MoveTowards(tempLineWidth, targetLineWidth * lineWeight, Time.deltaTime * lineWeight * 0.2f);
                PlayerLine.widthMultiplier = tempLineWidth;

                float tempLineBgWidth = BgLine.widthMultiplier;
                tempLineBgWidth = Mathf.MoveTowards(tempLineBgWidth, targetLineWidth * lineBgWeight, Time.deltaTime * lineBgWeight * 0.2f);
                BgLine.widthMultiplier = tempLineBgWidth;
            }
        }

        private void UpdateLine(LineRenderer line, WavesMinigame_WaveSettings settings)
        {
            line.positionCount = (int)(LineLenght * LineResolution);

            for (int i = 0; i < line.positionCount; i++)
            {
                float x = (float)i / (float)LineResolution - LineLenght * 0.5f;
                float height = RangeMin.Height + settings.Height * Change.Height;
                float width = RangeMin.Width + (6 - settings.Width) * Change.Width;
                float horizontalShift = RangeMin.HorizontalShift + (6 - settings.HorizontalShift) * Change.HorizontalShift;

                float sin = Mathf.Sin(x * width + Time.timeSinceLevelLoad * settings.Speed + horizontalShift);
                line.SetPosition(i, new Vector3(x, 0, sin * height));
            }
        }

        private void Start()
        {
            PlayerLine.widthMultiplier = 0;
            BgLine.widthMultiplier = 0;
        }
    }
}
