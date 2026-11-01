using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Sounds/_SoundsPack")]
public class SoundsPack : ScriptableObject
{
    [PropertySpace, ShowInInspector, PropertyOrder(-10)]
    public string id => name;

    [Space]
    public List<AudioClip> sounds = new List<AudioClip>();
}