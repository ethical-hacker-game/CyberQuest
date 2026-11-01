using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ToggleMusicButton : MonoBehaviour
{
    [SerializeField] private Image buttonImage;
    [SerializeField] private Sprite musicOnSprite;
    [SerializeField] private Sprite musicOffSprite;

    public void ToggleMusic()
    {
        if (AudioManager.IsMusicMuted)
            AudioManager.UnmuteMusic();
        else
            AudioManager.MuteMusic();

        UpdateButtonImage();
    }

    private void UpdateButtonImage()
    {
        if (AudioManager.Instance == null)
            return;

        bool isMusicOn = !AudioManager.IsMusicMuted;
        buttonImage.sprite = isMusicOn ? musicOnSprite : musicOffSprite;
    }

    private void OnEnable()
    {
        UpdateButtonImage();
    }
}
