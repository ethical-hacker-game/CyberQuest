using UnityEngine;
using Sirenix.OdinInspector;

[System.Serializable]
public class MinMaxRange
{
    [SerializeField, HorizontalGroup] private float min = 0;
    [SerializeField, HorizontalGroup] private float max = 0;

    public float Min { get => min; set => min = value; }
    public float Max { get => max; set => max = value; }

    public float Random
    {
        get => UnityEngine.Random.Range(min, max);
    }

    public MinMaxRange() { }
    public MinMaxRange(float min, float max)
    {
        this.min = min;
        this.max = max;
    }
}
