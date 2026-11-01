using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEngine;

public class WebBrowserManager : GameSystemComponent
{
    public static WebBrowserManager Instance;

    [SerializeField] private string homeWebsite = "cybersearch.com";
    [SerializeField] private string errorWebsite = "error";

    private readonly LocalWebContentServer localWebContentServer = new LocalWebContentServer();

    public string HomeWebsite => BuildContentUrl(homeWebsite);
    public string ErrorWebsite => BuildContentUrl(errorWebsite);

    /// <summary>
    /// Root the browser serves authored pages from: the <c>web</c> folder of the currently loaded unit,
    /// exposed through a local loopback HTTP server. Always ends with a trailing slash.
    /// </summary>
    private string WebRoot => GetWebRoot();

    public string GetFakeLink(string url)
    {
        string tempUrl = StripWebRoot(url);

        // Webtool hosts that open on a port are stored on disk with ';' instead of ':'
        // (folders cannot contain ':'). Show the player the real "host:port" form.
        tempUrl = PortSeparatorToColon(tempUrl);

        if (tempUrl.StartsWith(errorWebsite) == false)
        {
            // Tor onion services are typically reached over http:// (Tor already encrypts
            // end-to-end); clearnet hosts keep the https:// player-facing scheme.
            string scheme = IsOnionHost(tempUrl) ? "http://" : "https://";
            tempUrl = scheme + tempUrl;
        }

        if (tempUrl.EndsWith("index.html"))
            tempUrl = tempUrl.Replace("index.html", "");

        return tempUrl;
    }

    /// <summary>
    /// True when the site-relative path's host ends with <c>.onion</c> (optional <c>:port</c> ignored).
    /// </summary>
    private static bool IsOnionHost(string siteRelative)
    {
        if (string.IsNullOrEmpty(siteRelative))
            return false;

        int firstSlash = siteRelative.IndexOf('/');
        string authority = firstSlash >= 0 ? siteRelative.Substring(0, firstSlash) : siteRelative;

        int colon = authority.IndexOf(':');
        string host = colon >= 0 ? authority.Substring(0, colon) : authority;

        return host.EndsWith(".onion", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// True when <paramref name="contentUrl"/> targets a <c>.onion</c> host and the player has not
    /// unlocked the <c>anonsurf</c> skill (anonymous browser). Callers should show the error page
    /// instead of loading the onion site.
    /// </summary>
    public bool IsOnionAccessBlocked(string contentUrl)
    {
        if (string.IsNullOrEmpty(contentUrl))
            return false;

        string siteRelative = StripQueryAndFragment(StripWebRoot(contentUrl));
        siteRelative = PortSeparatorToColon(siteRelative);

        if (IsOnionHost(siteRelative) == false)
            return false;

        return SkillsManager.IsUnlocked("anonsurf") == false;
    }

    public string GetTransormedLink(string url)
    {
        string pattern = @"^.*://";
        string siteRelative = Regex.Replace(url, pattern, "");

        // Map the typed/launched "host:port" back onto the on-disk "host;port" folder.
        siteRelative = PortSeparatorToSemicolon(siteRelative);

        return BuildContentUrl(siteRelative);
    }

    // Folder names cannot contain ':' on disk, so a webtool host that opens on a port is
    // authored as "host;port" (for example "192.168.20.10;8834"). Players still type and
    // see a normal "host:port"; these helpers translate between the two, touching only the
    // authority segment (before the first '/') so colons elsewhere are never changed.
    private static string PortSeparatorToSemicolon(string siteRelative)
    {
        return ConvertAuthoritySeparator(siteRelative, ':', ';');
    }

    private static string PortSeparatorToColon(string siteRelative)
    {
        return ConvertAuthoritySeparator(siteRelative, ';', ':');
    }

    private static string ConvertAuthoritySeparator(string path, char from, char to)
    {
        if (string.IsNullOrEmpty(path))
            return path;

        int firstSlash = path.IndexOf('/');
        int authorityEnd = firstSlash >= 0 ? firstSlash : path.Length;
        int separator = path.IndexOf(from);

        if (separator < 0 || separator >= authorityEnd)
            return path;

        return path.Substring(0, separator) + to + path.Substring(separator + 1);
    }

    public bool CanOpenWebsite(string url)
    {
        return url.StartsWith(WebRoot, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// True when <paramref name="contentUrl"/> resolves to an authored page that actually exists. A bogus
    /// or mistyped address still sits under the unit web root (so <see cref="CanOpenWebsite"/> passes), but
    /// the local server has no such file and would answer with its raw 404 body; callers use this to show the
    /// authored error page instead. Existence is the file on disk.
    /// </summary>
    public bool PageExists(string contentUrl)
    {
        if (string.IsNullOrEmpty(contentUrl))
            return false;

        string siteRelative = StripQueryAndFragment(StripWebRoot(contentUrl));

        if (string.IsNullOrEmpty(siteRelative))
            siteRelative = "index.html";

        // The error page itself must always be loadable, otherwise a missing page could not be reported.
        if (siteRelative.StartsWith(errorWebsite, StringComparison.OrdinalIgnoreCase))
            return true;

        string webKey = ContentLoader.CombinePath(ContentPaths.ActiveUnitFolder(ContentConstValues.FOLDER_WEB), siteRelative);

        // The browser loads the same files over loopback HTTP; existence is the file on disk.
        return File.Exists(ContentLoader.GetContentPath(webKey));
    }

    /// <summary>True when the URL points at the authored error page (ignoring any query/fragment).</summary>
    public bool IsErrorWebsite(string url)
    {
        string siteRelative = StripQueryAndFragment(StripWebRoot(url));
        return siteRelative.StartsWith(errorWebsite, StringComparison.OrdinalIgnoreCase);
    }

    private static string StripQueryAndFragment(string value)
    {
        if (string.IsNullOrEmpty(value))
            return value;

        int cut = value.IndexOfAny(new[] { '?', '#' });
        return cut >= 0 ? value.Substring(0, cut) : value;
    }

    private void Awake()
    {
        Instance = this;
    }

    private void OnDestroy()
    {
        localWebContentServer.Dispose();
    }

    /// <summary>
    /// Combines the unit web root with a site-relative path ("cybersearch.com", "site/page.html") into a concrete
    /// content URL, appending <c>index.html</c> when the path points at a folder so it resolves under local HTTP
    /// and plain object hosts that do not serve directory indexes.
    /// </summary>
    private string BuildContentUrl(string siteRelative)
    {
        if (string.IsNullOrEmpty(siteRelative))
            siteRelative = string.Empty;

        siteRelative = siteRelative.Replace('\\', '/').Trim('/');

        return EnsureDocument(WebRoot + siteRelative);
    }

    // Real document extensions authored pages use, so site folders named like domains ("cybersearch.com")
    // are not mistaken for files because of the dot in their name.
    private static readonly HashSet<string> DocumentExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "html", "htm", "jpg", "jpeg", "png", "gif", "svg", "webp", "ico",
        "css", "js", "json", "xml", "txt", "pdf", "mp4", "webm", "ogv",
        "woff", "woff2", "ttf",
    };

    private static string EnsureDocument(string url)
    {
        int lastSlash = url.LastIndexOf('/');
        string lastSegment = lastSlash >= 0 ? url.Substring(lastSlash + 1) : url;

        // A folder URL needs an explicit document: local HTTP and plain object hosts do not serve a directory index.
        if (lastSegment.Length == 0)
            return url + "index.html";

        int dotIndex = lastSegment.LastIndexOf('.');
        bool isDocument = dotIndex >= 0 && DocumentExtensions.Contains(lastSegment.Substring(dotIndex + 1));

        if (isDocument == false)
            return url + "/index.html";

        return url;
    }

    private string StripWebRoot(string url)
    {
        string root = WebRoot;

        if (url.StartsWith(root, StringComparison.OrdinalIgnoreCase))
            return url.Substring(root.Length);

        return url;
    }

    private string GetWebRoot()
    {
        string webFolder = ContentPaths.ActiveUnitFolder(ContentConstValues.FOLDER_WEB);

        // Serve the pages over loopback HTTP so browser fetch() calls are not blocked by file:// CORS.
        string diskPath = ContentLoader.GetContentPath(webFolder);
        return EnsureTrailingSlash(localWebContentServer.ServeDirectory(diskPath));
    }

    private static string EnsureTrailingSlash(string url)
    {
        if (string.IsNullOrEmpty(url))
            return "/";

        return url.EndsWith("/") ? url : url + "/";
    }
}
