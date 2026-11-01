using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Michsky.UI.Beam;
using UnityEngine.Video;

public class CyberpediaUI : UI_Screen
{
    private const string EmptyLibraryMessage = "No articles available.";

    public static CyberpediaUI Instance => GameplayUIManager.Instance.CyberpediaUI;

    [Header("Buttons List")]
    [SerializeField] private GameObject buttonPrefab;
    [SerializeField] private Transform buttonsParent;

    [Header("Content")]
    [SerializeField] private LayoutGroupFix layoutFixer;
    [SerializeField] private GameObject videoPlayerParent;
    [SerializeField] private TextMeshProUGUI descriptionTMP;

    [Header("Video Player Elements")]
    [SerializeField] private RectTransform videoPlayerContent;
    [SerializeField] private Transform videoSmallParent;
    [SerializeField] private Transform videoFullParent;

    [Header("Video Player Controls")]
    [SerializeField] private Slider volumeSlider;
    [SerializeField] private Slider videoProgressSlider;
    [SerializeField] private GameObject playButton;
    [SerializeField] private GameObject pauseButton;

    private bool fullScreen = false;
    private VideoPlayer VideoPlayer => CyberpediaManager.VideoPlayer;
    private string currentArticleID;

    public static void ShowArticle(CyberpediaArticle article)
    {
        GameplayUIManager.ShowCyberpediaUI();
        Instance.descriptionTMP.text = article?.ToRichText() ?? EmptyLibraryMessage;
        Instance.layoutFixer.FixLayout();
        Instance.currentArticleID = article?.articleID;
        Instance.videoPlayerParent.SetActiveOptimized(article != null && article.HasVideo);
        Instance.MarkArticleAsRead();
        Instance.RefreshListButtons();
    }

    public override void ShowScreen()
    {
        base.ShowScreen();

        fullScreen = true;
        volumeSlider.value = SettingsManager.SettingsInfo.volumeGeneral;
        ResizeVideoPlayer();
        UpdatePlayerButtons();
        RefreshListButtons();
    }

    public override void HideScreen()
    {
        if (gameObject.activeSelf && CyberpediaManager.Instance != null)
            CyberpediaManager.Instance.StopVideo();

        base.HideScreen();
    }

    public override void Back()
    {
        if (fullScreen)
        {
            ResizeVideoPlayer();
            return;
        }

        GameplayUIManager.ShowGameUI();
        CyberpediaManager.Instance.StopVideo();
        base.Back();
    }

    public void SpawnButtons()
    {
        for (int i = 0; i < CyberpediaManager.Instance.Articles.Count; i++)
        {
            ButtonManager newButton = Instantiate(buttonPrefab, buttonsParent).GetComponent<ButtonManager>();
            newButton.SetText(CyberpediaManager.Instance.Articles[i].title);
            newButton.onClick.AddListener(() =>
            {
                CyberpediaManager.Instance.OpenArticle(newButton.transform.GetSiblingIndex());
            });
        }
    }

    public void ResizeVideoPlayer()
    {
        fullScreen = !fullScreen;

        if (fullScreen)
        {
            videoPlayerContent.SetParent(videoFullParent);
        }
        else
        {
            videoPlayerContent.SetParent(videoSmallParent);
        }

        videoPlayerContent.offsetMin = Vector2.zero;
        videoPlayerContent.offsetMax = Vector2.zero;
    }

    public void PlayVideo()
    {
        CyberpediaManager.Instance.PlayVideo();
        UpdatePlayerButtons();
    }

    public void PauseVideo()
    {
        CyberpediaManager.Instance.PauseVideo();
        UpdatePlayerButtons();
    }

    public void StopVideo()
    {
        CyberpediaManager.Instance.StopVideo();
        UpdatePlayerButtons();
    }

    public void UpdateVolume()
    {
        VideoPlayer.SetDirectAudioVolume(0, volumeSlider.value);
    }

    public void UpdateVideoProgress()
    {
        if (VideoPlayer.frameCount <= 0)
        {
            videoProgressSlider.SetValueWithoutNotify(0);
            return;
        }

        VideoPlayer.frame = (long)(VideoPlayer.frameCount * videoProgressSlider.value);
    }

    private void Start()
    {
        VideoPlayer.loopPointReached += new ((VideoPlayer source) =>
        {
            CyberpediaManager.Instance.StopVideo();
            fullScreen = true;
            ResizeVideoPlayer();
            UpdatePlayerButtons();
        });

        VideoPlayer.started += new((VideoPlayer source) =>
        {
            UpdatePlayerButtons();
        });
    }

    private void Update()
    {
        float progress = VideoPlayer.frameCount > 0
            ? (float)VideoPlayer.frame / VideoPlayer.frameCount
            : 0;

        videoProgressSlider.SetValueWithoutNotify(progress);
    }

    private void UpdatePlayerButtons()
    {
        playButton.gameObject.SetActiveOptimized(!VideoPlayer.isPlaying);
        pauseButton.gameObject.SetActiveOptimized(VideoPlayer.isPlaying);
    }

    private void RefreshListButtons()
    {
        if (CyberpediaManager.Instance == null)
            return;

        for (int i = 0; i < CyberpediaManager.Instance.Articles.Count && i < buttonsParent.childCount; i++)
        {
            string articleID = CyberpediaManager.Instance.Articles[i].articleID;
            ButtonManager tempButton = buttonsParent.GetChild(i).GetComponent<ButtonManager>();

            tempButton.enableIcon = PlayerController.CurrentUnitInfo != null
                && PlayerController.CurrentUnitInfo.readArticleIDs != null
                && PlayerController.CurrentUnitInfo.readArticleIDs.Contains(articleID);
            tempButton.UpdateUI();
        }
    }

    private void MarkArticleAsRead()
    {
        if (string.IsNullOrEmpty(currentArticleID) || PlayerController.CurrentUnitInfo == null)
            return;

        PlayerController.CurrentUnitInfo.readArticleIDs ??= new System.Collections.Generic.List<string>();

        if (PlayerController.CurrentUnitInfo.readArticleIDs.Contains(currentArticleID) == false)
            PlayerController.CurrentUnitInfo.readArticleIDs.Add(currentArticleID);

        if (ProgressUI.Instance != null)
            ProgressUI.Instance.RefreshCyberpediaProgress();
    }
}
