using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class UtilityMethods
{
    public static void OpenLink(string url)
    {
        Application.OpenURL(url);
    }

    public static void OpenPath(string path)
    {
        if (Directory.Exists(path))
            System.Diagnostics.Process.Start(path, "explorer.exe");
        else
            Debug.LogError("Invalid path:" + path);
    }

    public static string GetTimestamp(DateTime dateTime)
    {
        return dateTime.ToString("yyyy-MM-dd_HH-mm-ss_ffff");
    }

    public static string GetTimeDifference(DateTime startDate, DateTime endDate)
    {
        TimeSpan difference = endDate - startDate;

        //if (difference.TotalHours > 6)
            return $"<b>{difference.Days:D2}</b> days<br><b>{difference.Hours:D2}</b> hours <b>{difference.Minutes:D2}</b> minutes <b>{difference.Seconds:D2}</b> seconds";

        //return "<b>Just a few hours</b>";
    }

    public static string GetCurrentTimestamp()
    {
        return GetTimestamp(DateTime.Now);
    }

    public static string WrapText(string text, int maxLineLength)
    {
        if (string.IsNullOrWhiteSpace(text) || maxLineLength <= 0) return text ?? "";

        var words = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var sb = new System.Text.StringBuilder();
        int lineLen = 0;

        foreach (var word in words)
        {
            int extra = (lineLen == 0 ? 0 : 1) + word.Length;
            if (lineLen + extra > maxLineLength && lineLen > 0)
            {
                sb.Append('\n');
                sb.Append(word);
                lineLen = word.Length;
            }
            else
            {
                if (lineLen > 0) { sb.Append(' '); lineLen++; }
                sb.Append(word);
                lineLen += word.Length;
            }
        }

        return sb.ToString();
    }


    public static void CopyDirecoryRecirsively(string sourcePath, string targetPath)
    {
        CopyDirecoryRecirsively(sourcePath, targetPath, null);
    }

    public static void CopyDirecoryRecirsively(string sourcePath, string targetPath, IEnumerable<string> skipDirectoryNames)
    {
        if (string.IsNullOrWhiteSpace(sourcePath) || string.IsNullOrWhiteSpace(targetPath))
            return;

        if (Directory.Exists(sourcePath) == false)
        {
            Debug.LogError($"Copy source does not exist: {sourcePath}");
            return;
        }

        // Path.GetFullPath normalizes mixed Unity/Windows separators so destination
        // paths are built with Path.Combine instead of string.Replace.
        sourcePath = Path.GetFullPath(sourcePath);
        targetPath = Path.GetFullPath(targetPath);

        HashSet<string> skip = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (skipDirectoryNames != null)
        {
            foreach (string name in skipDirectoryNames)
            {
                if (string.IsNullOrWhiteSpace(name) == false)
                    skip.Add(name);
            }
        }

        CopyDirectoryRecursiveInternal(sourcePath, targetPath, skip);
    }

    private static void CopyDirectoryRecursiveInternal(string sourcePath, string targetPath, HashSet<string> skipDirectoryNames)
    {
        Directory.CreateDirectory(targetPath);

        string[] files = Directory.GetFiles(sourcePath);
        for (int i = 0; i < files.Length; i++)
        {
            string fileName = Path.GetFileName(files[i]);
            if (ShouldSkipMetaForDirectory(fileName, skipDirectoryNames))
                continue;

            File.Copy(files[i], Path.Combine(targetPath, fileName), true);
        }

        string[] dirs = Directory.GetDirectories(sourcePath);
        for (int i = 0; i < dirs.Length; i++)
        {
            string dirName = Path.GetFileName(dirs[i]);
            if (skipDirectoryNames.Contains(dirName))
                continue;

            CopyDirectoryRecursiveInternal(dirs[i], Path.Combine(targetPath, dirName), skipDirectoryNames);
        }
    }

    private static bool ShouldSkipMetaForDirectory(string fileName, HashSet<string> skipDirectoryNames)
    {
        const string metaSuffix = ".meta";
        if (fileName.EndsWith(metaSuffix, StringComparison.OrdinalIgnoreCase) == false)
            return false;

        string dirName = fileName.Substring(0, fileName.Length - metaSuffix.Length);
        return skipDirectoryNames.Contains(dirName);
    }

    public static void ForceDeleteDirectory(string dir)
    {
        try
        {
            var di = new DirectoryInfo(dir);
            foreach (var info in di.GetFileSystemInfos("*", SearchOption.AllDirectories))
            {
                try { info.Attributes = FileAttributes.Normal; }
                catch { }
            }
            Directory.Delete(dir, true);
            Debug.Log($"Directory force deleted : {dir}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"Failed to force delete directory: {dir}\n{ex.Message}");
        }
    }

}
