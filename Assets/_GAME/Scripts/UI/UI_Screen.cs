using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class UI_Screen : MonoBehaviour
{
    [Space(5)]
    [SerializeField, FoldoutGroup("UI Screen Config")] private bool playSoundOnShow = false;
    [SerializeField, FoldoutGroup("UI Screen Config")] private bool playSoundOnHide = false;
    [InfoBox("Leave it null to use default sound.")]
    [SerializeField, FoldoutGroup("UI Screen Config")] private AudioClip customClip;
    [Space]
    [SerializeField, FoldoutGroup("UI Screen Config")] private string tutorialTag;

    [Space]
    [FoldoutGroup("UI Screen Config")] public UnityEvent OnShowScreen;
    [FoldoutGroup("UI Screen Config")] public UnityEvent OnHideScreen;
    [FoldoutGroup("UI Screen Config")] public UnityEvent OnBack;

    public string TutorialTag { get => tutorialTag; }

    [Button, PropertyOrder(-1), PropertySpace]
    public virtual void ShowScreen()
    {
        OnShowScreen.Invoke();
        bool changed = gameObject.SetActiveOptimized(true);

        if (playSoundOnShow && changed)
            PlaySound();
    }

    [Button, PropertyOrder(-1)]
    public virtual void HideScreen()
    {
        OnHideScreen.Invoke();
        bool changed = gameObject.SetActiveOptimized(false);

        if (playSoundOnHide && changed)
            PlaySound();
    }

    public virtual void Back()
    {
        OnBack.Invoke();
    }

    private void PlaySound()
    {
        if (Time.timeSinceLevelLoad < 1f)
            return;

        if (customClip == null)
            AudioManager.PlayScreenSound();
        else
            AudioManager.PlayUISound(customClip);
    }
}