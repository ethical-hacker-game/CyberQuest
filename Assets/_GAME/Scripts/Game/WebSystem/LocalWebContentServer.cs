using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;

public sealed class LocalWebContentServer : IDisposable
{
    private static readonly Dictionary<string, string> ContentTypes = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        { ".html", "text/html; charset=utf-8" },
        { ".htm", "text/html; charset=utf-8" },
        { ".css", "text/css; charset=utf-8" },
        { ".js", "application/javascript; charset=utf-8" },
        { ".json", "application/json; charset=utf-8" },
        { ".txt", "text/plain; charset=utf-8" },
        { ".csv", "text/csv; charset=utf-8" },
        { ".xml", "application/xml; charset=utf-8" },
        { ".svg", "image/svg+xml" },
        { ".png", "image/png" },
        { ".jpg", "image/jpeg" },
        { ".jpeg", "image/jpeg" },
        { ".gif", "image/gif" },
        { ".webp", "image/webp" },
        { ".ico", "image/x-icon" },
        { ".pdf", "application/pdf" },
        { ".mp4", "video/mp4" },
        { ".webm", "video/webm" },
        { ".ogv", "video/ogg" },
        { ".woff", "font/woff" },
        { ".woff2", "font/woff2" },
        { ".ttf", "font/ttf" },
    };

    private readonly object lifecycleLock = new object();

    private TcpListener listener;
    private Thread serverThread;
    private string rootPath;
    private string rootPrefix;
    private string rootUrl;
    private volatile bool isRunning;

    public string ServeDirectory(string directoryPath)
    {
        if (string.IsNullOrEmpty(directoryPath))
            throw new ArgumentException("Directory path cannot be empty.", nameof(directoryPath));

        string normalizedRoot = Path.GetFullPath(directoryPath);

        lock (lifecycleLock)
        {
            if (isRunning && string.Equals(rootPath, normalizedRoot, StringComparison.OrdinalIgnoreCase))
                return rootUrl;

            StopLocked();
            StartLocked(normalizedRoot);
            return rootUrl;
        }
    }

    public void Dispose()
    {
        lock (lifecycleLock)
        {
            StopLocked();
        }
    }

    private void StartLocked(string normalizedRoot)
    {
        if (Directory.Exists(normalizedRoot) == false)
            throw new DirectoryNotFoundException($"Local web content directory was not found: {normalizedRoot}");

        listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();

        int port = ((IPEndPoint)listener.LocalEndpoint).Port;
        rootPath = normalizedRoot.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        rootPrefix = rootPath + Path.DirectorySeparatorChar;
        rootUrl = $"http://127.0.0.1:{port}/";
        isRunning = true;

        serverThread = new Thread(ServeLoop)
        {
            IsBackground = true,
            Name = "Local Web Content Server",
        };
        serverThread.Start();
    }

    private void StopLocked()
    {
        Thread threadToStop = serverThread;

        isRunning = false;
        listener?.Stop();

        if (threadToStop != null && threadToStop.IsAlive)
            threadToStop.Join(1000);

        listener = null;
        serverThread = null;
        rootPath = null;
        rootPrefix = null;
        rootUrl = null;
    }

    private void ServeLoop()
    {
        while (isRunning)
        {
            try
            {
                using TcpClient client = listener.AcceptTcpClient();
                ProcessClient(client);
            }
            catch (SocketException)
            {
                if (isRunning)
                    Debug.LogWarning("[LocalWebContentServer] Socket listener stopped unexpectedly.");
            }
            catch (ObjectDisposedException)
            {
                return;
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"[LocalWebContentServer] Request failed: {exception.Message}");
            }
        }
    }

    private void ProcessClient(TcpClient client)
    {
        client.ReceiveTimeout = 5000;
        client.SendTimeout = 5000;

        using NetworkStream stream = client.GetStream();
        using StreamReader reader = new StreamReader(stream, Encoding.ASCII, false, 1024, true);

        string requestLine = reader.ReadLine();

        if (string.IsNullOrEmpty(requestLine))
            return;

        string[] parts = requestLine.Split(' ');

        if (parts.Length < 2)
        {
            WritePlainResponse(stream, 400, "Bad Request", "Malformed request.");
            return;
        }

        string method = parts[0];

        if (method != "GET" && method != "HEAD")
        {
            WritePlainResponse(stream, 405, "Method Not Allowed", "Only GET and HEAD are supported.");
            return;
        }

        string filePath = ResolveRequestPath(parts[1]);

        if (filePath == null)
        {
            WritePlainResponse(stream, 403, "Forbidden", "Forbidden.");
            return;
        }

        if (Directory.Exists(filePath))
            filePath = Path.Combine(filePath, "index.html");

        if (File.Exists(filePath) == false)
        {
            WritePlainResponse(stream, 404, "Not Found", "Not found.");
            return;
        }

        WriteFileResponse(stream, filePath, method == "HEAD");
    }

    private string ResolveRequestPath(string requestTarget)
    {
        string path = requestTarget;

        if (Uri.TryCreate(requestTarget, UriKind.Absolute, out Uri absoluteUri))
            path = absoluteUri.AbsolutePath;

        int queryIndex = path.IndexOf('?');

        if (queryIndex >= 0)
            path = path.Substring(0, queryIndex);

        path = Uri.UnescapeDataString(path).Replace('/', Path.DirectorySeparatorChar).TrimStart(Path.DirectorySeparatorChar);

        string filePath = Path.GetFullPath(Path.Combine(rootPath, path));

        if (string.Equals(filePath, rootPath, StringComparison.OrdinalIgnoreCase))
            return filePath;

        return filePath.StartsWith(rootPrefix, StringComparison.OrdinalIgnoreCase) ? filePath : null;
    }

    private static void WriteFileResponse(Stream stream, string filePath, bool headersOnly)
    {
        FileInfo fileInfo = new FileInfo(filePath);
        string extension = Path.GetExtension(filePath);
        string contentType = ContentTypes.TryGetValue(extension, out string knownContentType)
            ? knownContentType
            : "application/octet-stream";

        WriteHeaders(stream, 200, "OK", contentType, fileInfo.Length);

        if (headersOnly)
            return;

        using FileStream fileStream = File.OpenRead(filePath);
        fileStream.CopyTo(stream);
    }

    private static void WritePlainResponse(Stream stream, int statusCode, string reason, string message)
    {
        byte[] body = Encoding.UTF8.GetBytes(message);
        WriteHeaders(stream, statusCode, reason, "text/plain; charset=utf-8", body.Length);
        stream.Write(body, 0, body.Length);
    }

    private static void WriteHeaders(Stream stream, int statusCode, string reason, string contentType, long contentLength)
    {
        string headers =
            $"HTTP/1.1 {statusCode} {reason}\r\n" +
            $"Content-Type: {contentType}\r\n" +
            $"Content-Length: {contentLength}\r\n" +
            "Cache-Control: no-store\r\n" +
            "Access-Control-Allow-Origin: *\r\n" +
            "Connection: close\r\n" +
            "\r\n";

        byte[] bytes = Encoding.ASCII.GetBytes(headers);
        stream.Write(bytes, 0, bytes.Length);
    }
}
