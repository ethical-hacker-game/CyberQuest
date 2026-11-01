using UnityEngine;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System;

namespace BuildScripts
{
    public class CreateContentFolder : IPostprocessBuildWithReport
    {
        public int callbackOrder => 0;

        public void OnPostprocessBuild(BuildReport report)
        {
            if (report.summary.result == BuildResult.Cancelled)
                return;

            if (report.summary.result == BuildResult.Failed)
                return;

            CopyContentFolder(report.summary.outputPath, report.summary.platform, BuildProfilesMenu.KeepRepositoryFiles);
        }

        public static void CopyContentFolder(string outputPath, BuildTarget platform, bool keepRepositoryFiles)
        {
            outputPath = Path.GetFullPath(outputPath);
            BuildOutputPaths.RequireBuildDirectory(Path.GetDirectoryName(outputPath));
            string productName = Path.GetFileName(outputPath);
            string buildPath = Path.GetDirectoryName(outputPath);
            string contentFolder = ContentConstValues.FOLDER_CONTENT;
            string buildContentFolderPath;

            string projectPath = Application.dataPath;
            string projectContentPath = Path.Combine(projectPath, contentFolder);

            if (Directory.Exists(projectContentPath) == false)
            {
                Debug.LogError($"[Build Copy CONTENT] Source content folder not found: {projectContentPath}");
                return;
            }

            if (TryGetStandaloneContentDestination(buildPath, productName, platform, contentFolder, out buildContentFolderPath) == false)
                return;

            // Validate the exact destination and its existing subtree before deletion.
            BuildOutputPaths.RejectLinkedEntries(buildContentFolderPath);

            if (Directory.Exists(buildContentFolderPath))
            {
                try
                {
                    Directory.Delete(buildContentFolderPath, true);
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"Cannot delete directory: {buildContentFolderPath}\n{ex.Message}");
                    UtilityMethods.ForceDeleteDirectory(buildContentFolderPath);
                }
            }

            Directory.CreateDirectory(buildContentFolderPath);

            string[] skipDirectoryNames =
            {
                ContentConstValues.FOLDER_AUTHORING,
                ContentConstValues.FOLDER_TEMPLATES,
            };

            UtilityMethods.CopyDirecoryRecirsively(projectContentPath, buildContentFolderPath, skipDirectoryNames);
            Debug.Log($"[Build Copy CONTENT] {platform}: {projectContentPath} -> {buildContentFolderPath}");

            DeleteDirectoriesByName(buildContentFolderPath, skipDirectoryNames);

            if (keepRepositoryFiles == false)
            {
                DeleteGitDirectory(buildContentFolderPath);
                DeleteFilesByExtensions(buildContentFolderPath, new[] { ".meta", ".gitignore", ".gitattributes" });
                DeleteEmptyDirectories(buildContentFolderPath);
            }
        }

        private static bool TryGetStandaloneContentDestination(
            string buildPath,
            string productName,
            BuildTarget platform,
            string contentFolder,
            out string buildContentFolderPath)
        {
            buildContentFolderPath = null;

            if (platform == BuildTarget.StandaloneOSX)
            {
                // Application.dataPath on macOS player is <app>/Contents.
                string macAppFolder = productName;
                if (macAppFolder.EndsWith(".app", StringComparison.OrdinalIgnoreCase) == false)
                    macAppFolder += ".app";

                buildContentFolderPath = Path.Combine(buildPath, macAppFolder, "Contents", contentFolder);
                return true;
            }

            if (platform == BuildTarget.StandaloneWindows
                || platform == BuildTarget.StandaloneWindows64
                || platform == BuildTarget.StandaloneLinux64)
            {
                // Application.dataPath on Windows/Linux player is <exeName>_Data.
                string dataFolder = Path.GetFileNameWithoutExtension(productName) + "_Data";
                buildContentFolderPath = Path.Combine(buildPath, dataFolder, contentFolder);
                return true;
            }

            Debug.LogError($"[Build Copy CONTENT] Unsupported platform: {platform}");
            return false;
        }

        private static void DeleteDirectoriesByName(string rootFolderPath, IEnumerable<string> directoryNames)
        {
            if (string.IsNullOrWhiteSpace(rootFolderPath))
                return;

            if (Directory.Exists(rootFolderPath) == false)
                return;

            if (directoryNames == null)
                return;

            foreach (string directoryName in directoryNames)
            {
                if (string.IsNullOrWhiteSpace(directoryName))
                    continue;

                string[] matchedDirs;
                try
                {
                    matchedDirs = Directory.GetDirectories(
                        rootFolderPath,
                        directoryName,
                        SearchOption.AllDirectories);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Cannot enumerate '{directoryName}' directories in: {rootFolderPath}\n{ex.Message}");
                    continue;
                }

                for (int i = 0; i < matchedDirs.Length; i++)
                {
                    string dir = matchedDirs[i];
                    try
                    {
                        Directory.Delete(dir, true);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning($"Cannot delete directory: {dir}\n{ex.Message}");
                        try
                        {
                            UtilityMethods.ForceDeleteDirectory(dir);
                        }
                        catch (Exception forceEx)
                        {
                            Debug.LogError($"Cannot force delete directory: {dir}\n{forceEx.Message}");
                        }
                    }
                }
            }
        }

        private static void DeleteGitDirectory(string rootFolderPath)
        {
            if (string.IsNullOrWhiteSpace(rootFolderPath))
                return;

            if (Directory.Exists(rootFolderPath) == false)
                return;

            string gitDirPath = Path.Combine(rootFolderPath, ".git");
            if (Directory.Exists(gitDirPath) == false)
                return;

            try
            {
                Directory.Delete(gitDirPath, true);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Cannot delete directory: {gitDirPath}\n{ex.Message}");

                try
                {
                    UtilityMethods.ForceDeleteDirectory(gitDirPath);
                }
                catch (Exception forceEx)
                {
                    Debug.LogError($"Cannot force delete directory: {gitDirPath}\n{forceEx.Message}");
                }
            }
        }

        private static void DeleteFilesByExtensions(string rootFolderPath, IEnumerable<string> extensions)
        {
            if (string.IsNullOrWhiteSpace(rootFolderPath))
                return;

            if (Directory.Exists(rootFolderPath) == false)
                return;

            HashSet<string> extSet = new HashSet<string>(
                extensions
                    .Where(e => string.IsNullOrWhiteSpace(e) == false)
                    .Select(e => e.StartsWith(".") ? e : "." + e),
                StringComparer.OrdinalIgnoreCase);

            if (extSet.Count == 0)
                return;

            DirectoryInfo rootDI = new DirectoryInfo(rootFolderPath);

            FileInfo[] files;
            try
            {
                files = rootDI.GetFiles("*", SearchOption.AllDirectories);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Cannot enumerate files in: {rootFolderPath}\n{ex.Message}");
                return;
            }

            for (int i = 0; i < files.Length; i++)
            {
                FileInfo file = files[i];

                if (extSet.Contains(file.Extension) == false)
                    continue;

                try
                {
                    file.Attributes = FileAttributes.Normal;
                    File.Delete(file.FullName);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Cannot delete file: {file.FullName}\n{ex.Message}");
                }
            }
        }

        private static void DeleteEmptyDirectories(string rootFolderPath)
        {
            if (string.IsNullOrWhiteSpace(rootFolderPath))
                return;

            if (Directory.Exists(rootFolderPath) == false)
                return;

            string[] allDirs;
            try
            {
                allDirs = Directory.GetDirectories(rootFolderPath, "*", SearchOption.AllDirectories);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Cannot enumerate directories in: {rootFolderPath}\n{ex.Message}");
                return;
            }

            // Deleting bottom-up to ensure children are removed before parents.
            Array.Sort(allDirs, (a, b) => b.Length.CompareTo(a.Length));

            for (int i = 0; i < allDirs.Length; i++)
            {
                string dir = allDirs[i];

                try
                {
                    bool hasAnyFiles = Directory.EnumerateFiles(dir, "*", SearchOption.TopDirectoryOnly).Any();
                    if (hasAnyFiles)
                        continue;

                    bool hasAnyDirs = Directory.EnumerateDirectories(dir, "*", SearchOption.TopDirectoryOnly).Any();
                    if (hasAnyDirs)
                        continue;

                    Directory.Delete(dir, false);
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"Cannot delete directory: {dir}\n{ex.Message}");
                }
            }
        }
    }
}
