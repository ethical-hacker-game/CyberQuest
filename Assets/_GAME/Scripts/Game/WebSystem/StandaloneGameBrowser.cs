using System;
using UnityEngine;
using VoltstroStudios.UnityWebBrowser;
using VoltstroStudios.UnityWebBrowser.Core;
using Resolution = VoltstroStudios.UnityWebBrowser.Shared.Resolution;

public class StandaloneGameBrowser : MonoBehaviour, IGameBrowser
{
    [SerializeField] private WebBrowserUIBasic browserView;

    public event Action<string> UrlChanged;
    public event Action<string> LoadStarted;
    public event Action<string> LoadFinished;

    private WebBrowserClient browserClient;
    private string currentUrl = "";
    private bool initialized = false;

    public bool CanGoBack => true;
    public string CurrentUrl => currentUrl;

    public void Initialize(GameBrowserContext context)
    {
        if (initialized)
            return;

        browserClient = browserView.browserClient;
        browserClient.initialUrl = context.initialUrl;
        currentUrl = context.initialUrl;

        browserClient.OnUrlChanged += HandleUrlChanged;
        browserClient.OnLoadStart += HandleLoadStarted;
        browserClient.OnLoadFinish += HandleLoadFinished;

        browserView.gameObject.SetActiveOptimized(true);
        initialized = true;
    }

    public void LoadUrl(string url)
    {
        if (initialized == false)
            return;

        browserClient.LoadUrl(url);
    }

    public void GoBack()
    {
        if (initialized == false)
            return;

        browserClient.GoBack();
    }

    public void Reload()
    {
        if (initialized == false)
            return;

        browserClient.Refresh();
    }

    public void SetVisible(bool visible)
    {
        // Rendering follows the Unity window hierarchy, nothing extra to do here.
    }

    public void SetInteractive(bool interactive)
    {
        // The CEF view is a regular Unity UI element, input already follows the canvas.
    }

    public void SetOccluded(bool occluded)
    {
        // The CEF view renders inside the canvas, so other windows can cover it naturally.
    }

    public void SetBounds(Rect screenRect)
    {
        if (initialized == false)
            return;

        uint width = (uint)screenRect.width;
        uint height = (uint)screenRect.height;

        if (width == 0 || height == 0)
            return;

        browserClient.Resize(new Resolution(width, height));
    }

    public void Dispose()
    {
        if (initialized == false)
            return;

        browserClient.OnUrlChanged -= HandleUrlChanged;
        browserClient.OnLoadStart -= HandleLoadStarted;
        browserClient.OnLoadFinish -= HandleLoadFinished;
        initialized = false;
    }

    private void HandleUrlChanged(string url)
    {
        currentUrl = url;
        UrlChanged?.Invoke(url);
    }

    private void HandleLoadStarted(string url)
    {
        currentUrl = url;
        LoadStarted?.Invoke(url);
    }

    private void HandleLoadFinished(string url)
    {
        currentUrl = url;
        LoadFinished?.Invoke(url);
    }
}
