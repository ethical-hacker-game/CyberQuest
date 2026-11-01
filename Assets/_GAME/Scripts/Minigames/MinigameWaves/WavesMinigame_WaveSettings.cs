using System;
using UnityEngine;

namespace Minigames.Waves
{
    [Serializable]
    public class WavesMinigame_WaveSettings
    {
        [SerializeField] private float speed = 1f;
        [SerializeField] private float height = 1f;
        [SerializeField] private float width = 1f;
        [SerializeField] private float horizontalShift = 1f;

        public float Speed { get => speed; set => speed = value; }
        public float Height { get => height; set => height = value; }
        public float Width { get => width; set => width = value; }
        public float HorizontalShift { get => horizontalShift; set => horizontalShift = value; }
        private float tollerance => WavesMinigame.MinigameInstance.Tollerance;

        public void Clamp(float min, float max)
        {
            speed.ClampFloat(min, max);
            height.ClampFloat(min, max);
            width.ClampFloat(min, max);
            horizontalShift.ClampFloat(min, max);
        }

        public void SetValues(WavesMinigame_WaveSettings newValues)
        {
            height = newValues.Height;
            width = newValues.Width;
            horizontalShift = newValues.HorizontalShift;
        }

        public float GetSyncValue(WavesMinigame_WaveSettings other)
        {
            float heightDiff = 1 - (Mathf.Abs(height - other.Height) / 3) + tollerance;
            float widthDiff = 1 - (Mathf.Abs(width - other.Width) / 3) + tollerance;
            float shiftDiff = 1 - (Mathf.Abs(horizontalShift - other.HorizontalShift) / 3) + tollerance;

            heightDiff.ClampFloat(0f, 1f);
            widthDiff.ClampFloat(0f, 1f);
            shiftDiff.ClampFloat(0f, 1f);

            return (heightDiff + widthDiff + shiftDiff) / 3f;
        }
    }
}
