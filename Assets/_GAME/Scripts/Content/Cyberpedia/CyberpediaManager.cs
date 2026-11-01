using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.Video;

public class CyberpediaManager : GameSystemComponent
{
    public static CyberpediaManager Instance { get; private set; }

    private VideoPlayer videoPlayer;
    private readonly List<CyberpediaArticle> articles = new List<CyberpediaArticle>();
    private bool restoreMusicAfterPlayback;

    private static readonly Regex ExistingLinkSpanRegex = new Regex(
        @"<link=""[^""]*"">.*?</link>",
        RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled);

    private static readonly Regex RichTextTagRegex = new Regex(
        @"<[^>]+>",
        RegexOptions.Compiled);

    public IReadOnlyList<CyberpediaArticle> Articles => articles;

    public static VideoPlayer VideoPlayer => Instance.videoPlayer;

    public static string FormatWithArticleLinks(string baseText)
    {
        if (baseText.IsNullOrEmpty() || Instance == null || Instance.Articles.Count == 0)
            return baseText;

        var ordered = Instance.Articles
            .Where(article => article.links != null)
            .SelectMany(article => article.links.Select(keyword => new { article.articleID, keyword }))
            .Where(item => string.IsNullOrWhiteSpace(item.keyword) == false)
            .OrderByDescending(item => item.keyword.Length)
            .ToList();

        if (ordered.Count == 0)
            return baseText;

        // Collect matches on the original text only, then apply from the end.
        // That prevents keywords like "SMB" / "NetBIOS" from matching inside
        // already-inserted article IDs (e.g. u05_netbios_smb) and nesting <link> tags.
        var protectedRanges = new List<(int start, int end)>();
        foreach (Match match in ExistingLinkSpanRegex.Matches(baseText))
            protectedRanges.Add((match.Index, match.Index + match.Length));
        foreach (Match match in RichTextTagRegex.Matches(baseText))
            protectedRanges.Add((match.Index, match.Index + match.Length));

        var replacements = new List<(int start, int length, string articleID, string value)>();

        foreach (var item in ordered)
        {
            foreach (Match match in Regex.Matches(baseText, Regex.Escape(item.keyword), RegexOptions.IgnoreCase))
            {
                int start = match.Index;
                int end = match.Index + match.Length;

                if (IsOverlapping(protectedRanges, start, end))
                    continue;

                if (replacements.Any(replacement => start < replacement.start + replacement.length && end > replacement.start))
                    continue;

                replacements.Add((start, match.Length, item.articleID, match.Value));
            }
        }

        if (replacements.Count == 0)
            return baseText;

        var builder = new StringBuilder(baseText);
        foreach (var replacement in replacements.OrderByDescending(item => item.start))
        {
            builder.Remove(replacement.start, replacement.length);
            builder.Insert(
                replacement.start,
                $@"<link=""{replacement.articleID}""><u>{replacement.value}</u></link>");
        }

        return builder.ToString();
    }

    private static bool IsOverlapping(List<(int start, int end)> ranges, int start, int end)
    {
        for (int i = 0; i < ranges.Count; i++)
        {
            if (start < ranges[i].end && end > ranges[i].start)
                return true;
        }

        return false;
    }

    public CyberpediaArticle GetArticleByID(string articleID)
    {
        return Articles.FirstOrDefault(article => article.articleID == articleID);
    }

    public void OpenLibrary()
    {
        OpenArticle(Articles.FirstOrDefault());
    }

    public void OpenArticle(string articleID)
    {
        CyberpediaArticle article = GetArticleByID(articleID);

        if (article == null)
        {
            LogPrompter.ShowError($"Cannot find Cyberpedia article (id):\n{articleID}");
            return;
        }

        OpenArticle(article);
    }

    public void OpenArticle(int articleIndex)
    {
        if (articleIndex < 0 || articleIndex >= Articles.Count)
        {
            LogPrompter.ShowError($"Cannot find Cyberpedia article (index):\n{articleIndex}");
            return;
        }

        OpenArticle(Articles[articleIndex]);
    }

    public void PlayVideo()
    {
        if (string.IsNullOrWhiteSpace(videoPlayer.url))
            return;

        videoPlayer.Play();
    }

    public void PauseVideo()
    {
        videoPlayer.Pause();
        RestoreMusic();
    }

    public void StopVideo()
    {
        videoPlayer.Stop();
        RestoreMusic();
    }

    private void Awake()
    {
        Instance = this;
        videoPlayer = GetComponent<VideoPlayer>();
        videoPlayer.started += HandleVideoStarted;
        videoPlayer.errorReceived += HandleVideoError;
    }

    private void Start()
    {
        LoadArticles();
        CyberpediaUI.Instance.SpawnButtons();
    }

    private void OpenArticle(CyberpediaArticle article)
    {
        StopVideo();
        videoPlayer.url = article != null && article.HasVideo ? article.videoURL : string.Empty;
        CyberpediaUI.ShowArticle(article);

        if (article != null && article.HasVideo)
            PlayVideo();
    }

    private void LoadArticles()
    {
        articles.AddRange(
            ContentLoader.ReturnListOfType<CyberpediaArticle>(
                ContentPaths.ActiveUnitFolder(ContentConstValues.FOLDER_CYBERPEDIA),
                ContentConstValues.EXTENSION_ARTICLE,
                true
            ).OrderBy(article => article.title)
            .ToArray());
    }

    private void RestoreMusic()
    {
        if (restoreMusicAfterPlayback == false)
            return;

        restoreMusicAfterPlayback = false;
        AudioManager.UnmuteMusic();
    }

    private void HandleVideoStarted(VideoPlayer source)
    {
        if (AudioManager.IsMusicMuted)
            return;

        restoreMusicAfterPlayback = true;
        AudioManager.MuteMusic();
    }

    private void HandleVideoError(VideoPlayer source, string message)
    {
        RestoreMusic();
    }
}
