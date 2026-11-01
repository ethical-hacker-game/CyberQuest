using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using Debug = UnityEngine.Debug;

[Serializable]
public class AppCore : IValidateContent
{
    public string appID;
    public string header;
    public Vector2Int size;

    private Sprite icon;
    private Sprite defaultBackground;
    private string contentPath;

    public Sprite Icon => icon;
    public Sprite DefaultBackground => defaultBackground;

    public void AssignContentPath(string path)
    {
        contentPath = path;
    }

    public void LoadImages()
    {
        string path = contentPath;

        if (string.IsNullOrEmpty(path))
            path = ContentLoader.CombinePath(ContentPaths.ActiveUnitFolder(ContentConstValues.FOLDER_APPS), appID);

        List<string> requriredPictures = new List<string>
        {
            $"{appID}_icon",
            $"{appID}_background"
        };

        Dictionary<string, Texture2D> imageDictionary = ContentLoader.ReturnPictures(path, requriredPictures, "png", false);

        if (imageDictionary.TryGetValue($"{appID}_icon", out Texture2D iconTexture))
            icon = Sprite.Create(iconTexture, new Rect(0, 0, iconTexture.width, iconTexture.height), new Vector2(0.5f, 0.5f));
        else
            icon = PicturesManager.DefaultAppIcon;

        if (imageDictionary.TryGetValue($"{appID}_background", out Texture2D backgroundTexture))
            defaultBackground = Sprite.Create(backgroundTexture, new Rect(0, 0, backgroundTexture.width, backgroundTexture.height), new Vector2(0.5f, 0.5f));
        else
            defaultBackground = PicturesManager.DefaultPicture;
    }

    public bool ValidateContent(out string validationMessage)
    {
        List<string> issues = new List<string>();
        issues.Add("Issues:");

        if (string.IsNullOrEmpty(appID))
            issues.Add("App ID is not set.");

        if (string.IsNullOrEmpty(header))
            issues.Add("App header is not set.");

        if (size.x <= 0 || size.y <= 0)
            issues.Add("App size must be greater than zero.");

        validationMessage = string.Join("\n", issues);
        return issues.Count <= 1;
    }

    public string ToFormattedString()
    {
        return $"<b>{header}</b>\n" +
        $"<size=40%><i>{appID}</i></size>\n" +
        $"<size=55%>Size: {size.x}x{size.y}</size>";
    }
}

