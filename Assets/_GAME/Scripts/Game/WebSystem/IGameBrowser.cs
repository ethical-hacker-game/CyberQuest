using System;
using UnityEngine;

public class GameBrowserContext
{
    public string browserId;
    public string initialUrl;
    public RectTransform viewTransform;
}

public interface IGameBrowser
{
    event Action<string> UrlChanged;
    event Action<string> LoadStarted;
    event Action<string> LoadFinished;

    bool CanGoBack { get; }
    string CurrentUrl { get; }

    void Initialize(GameBrowserContext context);
    void LoadUrl(string url);
    void GoBack();
    void Reload();
    void SetVisible(bool visible);
    void SetInteractive(bool interactive);
    void SetOccluded(bool occluded);
    void SetBounds(Rect screenRect);
    void Dispose();
}
