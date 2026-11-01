using System;
using System.Collections.Generic;

[Serializable]
public class CyberpediaArticle
{
    public string articleID;
    public string title;
    public string content;
    public string videoURL;
    public List<string> links = new List<string>();

    public bool HasVideo =>
        Uri.TryCreate(videoURL, UriKind.Absolute, out Uri uri)
        && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);

    public static CyberpediaArticle Example => new CyberpediaArticle
    {
        articleID = "article_id",
        title = "Article title",
        content = "Article content",
        videoURL = null,
        links = new List<string>()
    };

    public string ToRichText()
    {
        return $"<b>{title}</b>\n{content}";
    }
}
