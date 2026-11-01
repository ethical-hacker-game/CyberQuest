using System;
using Michsky.UI.Beam;
using TMPro;
using UnityEngine;

public class WindowWebBrowser : WindowBase
{
    [Header("Web Browser")]
    [SerializeField] private StandaloneGameBrowser standaloneBrowser;
    [SerializeField] private RectTransform browserArea;
    [SerializeField] private ButtonManager backButton;
    [SerializeField] private ButtonManager homeButton;
    [SerializeField] private TMP_InputField urlInputField;

    private IGameBrowser gameBrowser;
    private bool browserInitialized = false;

    private bool preventClosing = false;

    // The address the player tried to reach when it turned out not to exist; kept so the URL bar keeps
    // showing it (instead of the internal "error/" location) while the authored error page is displayed.
    private string deadLinkUrl = null;


    public override void ClickClose()
    {
        if (preventClosing)
            return;

        ClientLoadURL(WebBrowserManager.Instance.HomeWebsite);
        base.ClickClose();
    }

    public void ClickBack()
    {
        gameBrowser.GoBack();
    }

    public void ClickHomeWebsite()
    {
        ClientLoadURL(WebBrowserManager.Instance.HomeWebsite);
    }

    public void OpenWebsite(string targetWebsite)
    {
        targetWebsite = WebBrowserManager.Instance.GetTransormedLink(targetWebsite);
        ClientLoadURL(targetWebsite);
    }

    public void OpenWebsite()
    {
        OpenWebsite(urlInputField.text);
    }

    public string GetActiveWebsite()
    {
        return urlInputField.text;
    }

    public void ToggleNavigationElements(bool isEnabled)
    {
        backButton.Interactable(isEnabled);
        homeButton.Interactable(isEnabled);
        urlInputField.interactable = isEnabled;
    }

    public void TogglePreventingClosing(bool preventClosing)
    {
        this.preventClosing = preventClosing;
    }

    protected override void Awake()
    {
        gameBrowser = standaloneBrowser;
        base.Awake();
    }

    private void OnEnable()
    {
        EnsureBrowserInitialized();
        gameBrowser.SetVisible(true);
        UpdateAnonSurfHeader();
    }

    private void Update()
    {
        if (browserInitialized == false)
            return;

        gameBrowser.SetOccluded(IsTopmostWindow() == false);
    }

    private void OnDisable()
    {
        gameBrowser?.SetVisible(false);
    }

    private void OnDestroy()
    {
        gameBrowser?.Dispose();
    }

    private void EnsureBrowserInitialized()
    {
        if (browserInitialized)
            return;

        GameBrowserContext context = new GameBrowserContext
        {
            browserId = "game-browser",
            initialUrl = WebBrowserManager.Instance.HomeWebsite,
            viewTransform = browserArea,
        };

        gameBrowser.Initialize(context);
        gameBrowser.UrlChanged += UpdateUrl;
        gameBrowser.LoadStarted += UpdateUrl;
        gameBrowser.LoadFinished += UpdateUrl;

        // Dragging and resizing disable browser input, then restore it and resync the view.
        WindowFlexibilityComponent.OnDraggingStartEvent.AddListener(DisableBrowserInteraction);
        WindowFlexibilityComponent.OnDraggingEndEvent.AddListener(RestoreBrowserInteraction);
        WindowFlexibilityComponent.OnResizeingStartEvent.AddListener(DisableBrowserInteraction);
        WindowFlexibilityComponent.OnResizeingEndEvent.AddListener(RestoreBrowserInteraction);

        browserInitialized = true;
    }

    private void DisableBrowserInteraction()
    {
        gameBrowser.SetInteractive(false);
    }

    private void RestoreBrowserInteraction()
    {
        gameBrowser.SetInteractive(true);
        SyncBrowserBounds();
    }

    private bool IsTopmostWindow()
    {
        Transform parent = transform.parent;

        if (parent == null)
            return true;

        for (int i = parent.childCount - 1; i >= 0; i--)
        {
            Transform sibling = parent.GetChild(i);

            if (sibling.gameObject.activeSelf == false)
                continue;

            if (sibling == transform)
                return true;

            if (sibling.GetComponent<WindowBase>() != null)
                return false;
        }

        return true;
    }

    private void UpdateAnonSurfHeader()
    {
        if (PlayerController.CurrentUnitInfo == null)
            return;

        if (PlayerController.CurrentUnitInfo.skills.IsNullOrEmpty())
            return;

        if (PlayerController.CurrentUnitInfo.skills.Contains("anonsurf") == false)
            return;

        SetHeader("AnonSurf - Web Browser");
    }

    private void SyncBrowserBounds()
    {
        gameBrowser.SetBounds(GetBrowserScreenRect());
    }

    private Rect GetBrowserScreenRect()
    {
        Vector3[] corners = new Vector3[4];
        browserArea.GetWorldCorners(corners);
        return Rect.MinMaxRect(corners[0].x, corners[0].y, corners[2].x, corners[2].y);
    }

    private void ClientLoadURL(string urlToOpen)
    {
        EnsureBrowserInitialized();

        // .onion hosts require the AnonSurf skill. Without it, keep the attempted address in the URL
        // bar and show the authored error page (same pattern as a dead link).
        if (WebBrowserManager.Instance.IsOnionAccessBlocked(urlToOpen))
        {
            deadLinkUrl = WebBrowserManager.Instance.GetFakeLink(urlToOpen);
            urlToOpen = WebBrowserManager.Instance.ErrorWebsite + "#u=" + Uri.EscapeDataString(deadLinkUrl);
        }
        // A mistyped or dead address still sits under the web root, so the host would answer with its raw
        // 404 body. Detect it up front and load the authored error page, remembering the address the player
        // tried so both the URL bar and the error page can keep showing it.
        else if (WebBrowserManager.Instance.PageExists(urlToOpen))
        {
            deadLinkUrl = null;
        }
        else
        {
            deadLinkUrl = WebBrowserManager.Instance.GetFakeLink(urlToOpen);
            urlToOpen = WebBrowserManager.Instance.ErrorWebsite + "#u=" + Uri.EscapeDataString(deadLinkUrl);
        }

        gameBrowser.LoadUrl(urlToOpen);
    }

    private void UpdateUrl(string newUrl)
    {
        if (WebBrowserManager.Instance.CanOpenWebsite(newUrl) == false)
        {
            ClientLoadURL(WebBrowserManager.Instance.ErrorWebsite);
            return;
        }

        // In-page navigation can reach a .onion host without going through OpenWebsite; enforce AnonSurf here too.
        if (WebBrowserManager.Instance.IsOnionAccessBlocked(newUrl))
        {
            ClientLoadURL(newUrl);
            return;
        }

        bool isErrorPage = WebBrowserManager.Instance.IsErrorWebsite(newUrl);

        if (isErrorPage == false)
            deadLinkUrl = null;

        string fakeUrl = (isErrorPage && deadLinkUrl != null)
            ? deadLinkUrl
            : WebBrowserManager.Instance.GetFakeLink(newUrl);

        urlInputField.SetTextWithoutNotify(fakeUrl);
    }
}
