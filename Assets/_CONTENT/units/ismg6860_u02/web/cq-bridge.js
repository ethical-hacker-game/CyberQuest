/*
 * CyberQuest content-page bridge.
 *
 * Include this script on every game-authored web page that is shown inside the
 * in-game browser iframe, using a RELATIVE path. The content host serves the unit
 * web folder from a deep sub-path (never an origin root), so a root-absolute
 * "/cq-bridge.js" would 404. cq-bridge.js lives at the unit web root, so use as
 * many "../" as the page is folders deep:
 *
 *   <script src="../cq-bridge.js"></script>      (page one folder below web root)
 *   <script src="../../cq-bridge.js"></script>   (two folders below, and so on)
 *
 * The game page (Unity WebGL host) and the content pages are served from
 * different origins, so the game cannot read this document directly. This
 * bridge cooperates with the game over postMessage:
 *
 *  - on load it reports location.href, which drives the in-game URL bar and
 *    quest answer checking,
 *  - on click of a same-origin <a> to an image (.jpg/.jpeg/.png/.gif/.webp) it
 *    reports the destination URL via cq-page-loaded before the browser navigates,
 *    because a raw image document cannot run this script (WebGL Website quests
 *    that accept a .jpg URL fragment, e.g. u01 q03). Top-level CEF / standalone
 *    loads skip this IIFE entirely (parent === window), so OnUrlChanged stays
 *    authoritative there,
 *  - on "cq-snapshot-request" it captures the visible viewport with
 *    html2canvas and posts the raw RGBA pixels back, so the game can show a
 *    still of the page while another game window covers the browser.
 *
 * html2canvas is loaded lazily on the first snapshot request, preferring (in
 * order): an already-present window.html2canvas, window.CQ_HTML2CANVAS_URL,
 * "html2canvas.min.js" next to this script, then the jsDelivr CDN.
 *
 * Optional hardening: set window.CQ_GAME_ORIGINS = ["https://game.example.com"]
 * before this script to only answer snapshot requests from those origins.
 */
(function () {
  if (window.parent === window) return; // not embedded, nothing to do

  var bridgeBaseUrl = document.currentScript && document.currentScript.src
    ? document.currentScript.src.replace(/[^\/]*$/, "")
    : "";
  var loadCallbacks = null;

  function post(message, transfer) {
    // The content is public authored material, so "*" is acceptable here; the
    // game side additionally filters by event.source and origin.
    window.parent.postMessage(message, "*", transfer || []);
  }

  function reportLoaded() {
    post({ type: "cq-page-loaded", url: window.location.href });
  }

  // Raw image navigations never execute scripts, so report the destination URL
  // from the linking page before the iframe leaves. Left-click, same-origin only;
  // do not preventDefault — the image must still load for the player.
  var IMAGE_EXT = /\.(?:jpe?g|png|gif|webp)$/i;

  document.addEventListener("click", function (event) {
    if (event.defaultPrevented || event.button !== 0 || event.metaKey || event.ctrlKey || event.shiftKey || event.altKey) return;

    var anchor = event.target && event.target.closest ? event.target.closest("a[href]") : null;
    if (!anchor || anchor.target === "_blank") return;

    var href = anchor.getAttribute("href");
    if (!href || href.charAt(0) === "#") return;

    var url;
    try { url = new URL(href, window.location.href); } catch (e) { return; }
    if (url.origin !== window.location.origin) return;
    if (!IMAGE_EXT.test(url.pathname)) return;

    post({ type: "cq-page-loaded", url: url.href });
  }, false);

  function originAllowed(origin) {
    var allowed = window.CQ_GAME_ORIGINS;
    if (!allowed || !allowed.length) return true;
    return allowed.indexOf(origin) !== -1;
  }

  function ensureHtml2Canvas(callback) {
    if (window.html2canvas) {
      callback(true);
      return;
    }

    if (loadCallbacks) {
      loadCallbacks.push(callback);
      return;
    }

    loadCallbacks = [callback];

    var sources = [];
    if (window.CQ_HTML2CANVAS_URL) sources.push(window.CQ_HTML2CANVAS_URL);
    if (bridgeBaseUrl) sources.push(bridgeBaseUrl + "html2canvas.min.js");
    sources.push("https://cdn.jsdelivr.net/npm/html2canvas@1.4.1/dist/html2canvas.min.js");

    var finish = function (loaded) {
      if (!loaded) {
        console.warn("[cq-bridge] html2canvas could not be loaded, snapshots disabled. Tried: " + sources.join(", "));
      }
      var callbacks = loadCallbacks || [];
      loadCallbacks = null;
      for (var i = 0; i < callbacks.length; i++) callbacks[i](loaded);
    };

    var tryNext = function (index) {
      if (index >= sources.length) {
        finish(false);
        return;
      }

      var script = document.createElement("script");
      script.src = sources[index];
      script.onload = function () {
        if (window.html2canvas) finish(true);
        else tryNext(index + 1);
      };
      script.onerror = function () { tryNext(index + 1); };
      document.head.appendChild(script);
    };

    tryNext(0);
  }

  function captureSnapshot(maxWidth) {
    ensureHtml2Canvas(function (loaded) {
      if (!loaded) {
        post({ type: "cq-snapshot-failed" });
        return;
      }

      var width = Math.max(1, window.innerWidth);
      var height = Math.max(1, window.innerHeight);
      var scale = Math.min(1, (maxWidth || 1280) / width);

      window.html2canvas(document.documentElement, {
        x: window.pageXOffset,
        y: window.pageYOffset,
        width: width,
        height: height,
        windowWidth: width,
        windowHeight: height,
        scale: scale,
        useCORS: true,
        logging: false,
        backgroundColor: "#ffffff"
      }).then(function (canvas) {
        var pixels = canvas.getContext("2d").getImageData(0, 0, canvas.width, canvas.height);
        post(
          { type: "cq-snapshot", width: canvas.width, height: canvas.height, pixels: pixels.data.buffer },
          [pixels.data.buffer]
        );
      }).catch(function (error) {
        console.warn("[cq-bridge] snapshot capture failed:", error);
        post({ type: "cq-snapshot-failed" });
      });
    });
  }

  window.addEventListener("message", function (event) {
    var data = event.data;
    if (!data || data.type !== "cq-snapshot-request") return;
    if (!originAllowed(event.origin)) return;
    captureSnapshot(data.maxWidth);
  });

  function onReady() {
    reportLoaded();
    // Warm up html2canvas in the background so the first snapshot request does
    // not have to wait for the script download.
    var idle = window.requestIdleCallback || function (fn) { setTimeout(fn, 1000); };
    idle(function () { ensureHtml2Canvas(function () { }); });
  }

  if (document.readyState === "loading") {
    document.addEventListener("DOMContentLoaded", onReady);
  } else {
    onReady();
  }
})();

/*
 * In-game navigation normalizer.
 *
 * The in-game browser's content host serves EXACT file paths only:
 *   - the same-origin "/cq-web/" service worker matches exact cache keys, and
 *   - the cross-origin blob host is plain object storage.
 * Neither maps a directory URL ("/whois.netx/") to its "index.html" the way a
 * classic web server does. The game itself appends "index.html" when it opens a
 * page, but links and scripts inside a page navigate the iframe directly, so a
 * clean directory-style href ("security/", "../", a "site.com" folder) would 404.
 *
 * This shim rewrites same-origin navigations that land on a directory (or an
 * extensionless path such as a domain-named folder) to the concrete index.html
 * before they happen, so authored pages can keep using clean directory links and
 * the browser URL bar still shows the tidy directory form (the game strips the
 * trailing index.html again for display). It runs in every context so the same
 * pages behave identically in the editor, in standalone, and on the live host.
 */
(function () {
  var DOC_EXT = /\.(?:html?|css|js|json|xml|txt|csv|svg|png|jpe?g|gif|webp|ico|pdf|mp4|webm|ogv|mp3|wav|ogg|woff2?|ttf|otf|eot)$/i;

  // For a directory/extensionless pathname returns the index.html-resolved pathname; null when it already points at a real file.
  function toDocument(pathname) {
    if (pathname.charAt(pathname.length - 1) === "/") return pathname + "index.html";
    var lastSlash = pathname.lastIndexOf("/");
    var last = lastSlash >= 0 ? pathname.substring(lastSlash + 1) : pathname;
    if (DOC_EXT.test(last)) return null;
    return pathname + "/index.html";
  }

  // Resolve any href to a concrete, host-servable URL. Exposed so page scripts that
  // navigate programmatically (location.href = ...) can resolve targets the same way.
  window.cqResolveUrl = function (href) {
    var url;
    try { url = new URL(href, window.location.href); } catch (e) { return href; }
    if (url.origin !== window.location.origin) return href;
    var fixed = toDocument(url.pathname);
    if (fixed) url.pathname = fixed;
    return url.href;
  };

  // Bubble phase + defaultPrevented check: page-specific handlers (e.g. a SPA's
  // own routing) that call preventDefault win; we only catch plain directory links.
  document.addEventListener("click", function (event) {
    if (event.defaultPrevented || event.button !== 0 || event.metaKey || event.ctrlKey || event.shiftKey || event.altKey) return;

    var anchor = event.target && event.target.closest ? event.target.closest("a[href]") : null;
    if (!anchor || anchor.target === "_blank") return;

    var href = anchor.getAttribute("href");
    if (!href || href.charAt(0) === "#") return; // same-page anchor / SPA hash route

    var url;
    try { url = new URL(href, window.location.href); } catch (e) { return; }
    if (url.origin !== window.location.origin) return; // external link

    var fixed = toDocument(url.pathname);
    if (!fixed) return; // already a concrete file

    event.preventDefault();
    url.pathname = fixed;
    window.location.href = url.href;
  }, false);
})();
