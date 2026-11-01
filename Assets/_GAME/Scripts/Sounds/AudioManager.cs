using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

public class AudioManager : GameSystemComponent
{
    public static AudioManager Instance { get; private set; }

    [Header("Audio Sources")]
    [SerializeField] private AudioSource mainAudioSource1;
    [SerializeField] private AudioSource mainAudioSource2;
    [SerializeField] private AudioSource uiAudioSource;

    [Header("UI AudioClips")]
    [SerializeField] private AudioClip clickSound;
    [SerializeField] private AudioClip screenSound;

    [Header("Audio Settings")]
    [SerializeField, Range(0.1f, 20)] private int changingTime = 5;

    [Header("Audio Packs")]
    [SerializeField] private SoundsPack mainMenuAudioPack;
    [SerializeField] private SoundsPack gameAudioPack;

    [Header("Audio Mixers")]
    [SerializeField] private AudioMixer audioMixer;

    private int changeTime = 6;
    private bool gameScene = false;
    private SoundsPack currentPack;

    private bool change = false;
    private bool instanceAlreadyExists = false;

    public static bool IsMusicMuted { get; private set; } = true;

    public static void UpdateVolumeSettings()
    {
        if (Instance == null)
            return;

        Instance.mainAudioSource1.volume = 1;

        float masterVolumeDb = Mathf.Log10(SettingsManager.SettingsInfo.volumeGeneral) * 20;
        float musicVolumeDb = Mathf.Log10(SettingsManager.SettingsInfo.volumeMusic) * 20;
        float effectsVolumeDb = Mathf.Log10(SettingsManager.SettingsInfo.volumeEffects) * 20;
        float uiVolumeDb = Mathf.Log10(SettingsManager.SettingsInfo.volumeUI) * 20;

        if (SettingsManager.SettingsInfo.volumeGeneral == 0)
            masterVolumeDb = -80;

        if (IsMusicMuted || SettingsManager.SettingsInfo.volumeMusic == 0)
            musicVolumeDb = -80;

        if (SettingsManager.SettingsInfo.volumeEffects == 0)
            effectsVolumeDb = -80;

        if (SettingsManager.SettingsInfo.volumeUI == 0)
            uiVolumeDb = -80;

        Instance.audioMixer.SetFloat("Master", masterVolumeDb);
        Instance.audioMixer.SetFloat("Music", musicVolumeDb);
        Instance.audioMixer.SetFloat("SFX", effectsVolumeDb);
        Instance.audioMixer.SetFloat("UI", uiVolumeDb);
    }

    public static void PlayUISound(AudioClip clip)
    {
        Instance.uiAudioSource.PlayOneShot(clip);
    }

    public static void PlayClickSound()
    {
        PlayUISound(Instance.clickSound);
    }

    public static void PlayScreenSound()
    {
        PlayUISound(Instance.screenSound);
    }

    public static void MuteMusic()
    {
        IsMusicMuted = true;
        Instance.audioMixer.SetFloat("Music", -80);
    }

    public static void UnmuteMusic()
    {
        IsMusicMuted = false;
        UpdateVolumeSettings();
    }

    private void Awake()
    {
        if (Instance != null)
        {
            instanceAlreadyExists = true;
            return;
        }

        Instance = this;
        gameObject.DontDestroyOnLoadImproved();
    }

    private void Start()
    {
        if (instanceAlreadyExists)
        {
            Instance.LoadSceneSoundsSettings();
            Destroy(gameObject);
            return;
        }

        LoadSceneSoundsSettings();

        if (IsMusicMuted)
            audioMixer.SetFloat("Music", -80);
    }

    private void Update()
    {
        if (mainAudioSource1 == null || mainAudioSource2 == null) return;
        if (mainAudioSource1.clip == null) return;

        if (change)
        {
            float changeStep = Time.unscaledDeltaTime / changingTime;
            mainAudioSource1.volume = Mathf.MoveTowards(mainAudioSource1.volume, 1, changeStep);
            mainAudioSource2.volume = Mathf.MoveTowards(mainAudioSource2.volume, 0, changeStep);
        }
        else if (mainAudioSource1.time + changeTime >= mainAudioSource1.clip.length)
        {
            PlayRandomSoundtrack();
        }
    }

    private void LoadSceneSoundsSettings()
    {
        if(SceneLoader.ActiveSceneName == SceneLoader.MainMenuSceneName)
        {
            gameScene = false;
            currentPack = mainMenuAudioPack;
        }
        else
        {
            gameScene = true;
            currentPack = gameAudioPack;
        }

        PlayRandomSoundtrack();
    }

    private void PlayRandomSoundtrack()
    {
        int random = 0;

        if (currentPack.sounds.IsNullOrEmpty())
            return;

        if (currentPack.sounds.Count > 2)
        {
            do
            {
                random = Random.Range(0, currentPack.sounds.Count);
            }
            while (currentPack.sounds[random] == mainAudioSource1.clip);
        }

        StartCoroutine(ChangeSound(currentPack.sounds[random]));
    }

    private IEnumerator ChangeSound(AudioClip clip)
    {
        Debug.Log($"Playing: {clip.name}");
        AudioSource tempAudioSource;
        mainAudioSource2.Stop();
        mainAudioSource2.volume = 0;
        mainAudioSource2.clip = clip;
        tempAudioSource = mainAudioSource2;
        mainAudioSource2 = mainAudioSource1;
        mainAudioSource1 = tempAudioSource;
        change = true;
        mainAudioSource1.Play();
        yield return new WaitForSecondsRealtime(changingTime);
        change = false;
        mainAudioSource1.volume = 1;
        mainAudioSource2.volume = 0;
    }
}
