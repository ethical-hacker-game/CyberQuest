using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace BuildScripts
{
    public static class BuildOutputPaths
    {
        private static string ProjectRoot => Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        private static StringComparison PathComparison =>
            Path.DirectorySeparatorChar == '\\' ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;

        public static string ResolveDirectory(string relativePath)
        {
            if (string.IsNullOrWhiteSpace(relativePath) || Path.IsPathRooted(relativePath) || relativePath.Contains(":"))
                throw new ArgumentException("Build output must be a project-relative folder under Builds.");

            string normalized = relativePath.Replace('\\', '/');
            foreach (string segment in normalized.Split('/'))
                if (segment == ".." || segment == ".")
                    throw new ArgumentException("Build output cannot contain parent or current-directory segments.");

            string fullPath = Path.GetFullPath(Path.Combine(ProjectRoot, normalized));
            RequireBuildDirectory(fullPath);
            return fullPath;
        }

        public static string ResolvePlayerPath(BuildProfile profile)
        {
            string name = profile.gameName;
            if (string.IsNullOrWhiteSpace(name) || name == "." || name == ".."
                || name.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0
                || name.Contains("/") || name.Contains("\\") || name.Contains(":"))
                throw new ArgumentException("Game name must be a valid filename without directory separators.");

            string extension;
            switch (profile.targetPlatform)
            {
                case BuildTarget.StandaloneWindows:
                case BuildTarget.StandaloneWindows64:
                    extension = ".exe";
                    break;
                case BuildTarget.StandaloneOSX:
                    extension = ".app";
                    break;
                case BuildTarget.StandaloneLinux64:
                    extension = "";
                    break;
                default:
                    throw new ArgumentException("Only Windows, macOS and Linux standalone builds are supported.");
            }

            return Path.Combine(ResolveDirectory(profile.outputFolder), name + extension);
        }

        public static void RequireBuildDirectory(string directory)
        {
            string fullPath = Path.GetFullPath(directory);
            string buildsRoot = Path.Combine(ProjectRoot, "Builds") + Path.DirectorySeparatorChar;
            if (!fullPath.StartsWith(buildsRoot, PathComparison))
                throw new ArgumentException("Build output must be inside a subfolder of the project's Builds folder.");

            for (DirectoryInfo current = new DirectoryInfo(fullPath); current != null; current = current.Parent)
            {
                if (current.Exists && (current.Attributes & FileAttributes.ReparsePoint) != 0)
                    throw new IOException("Build paths cannot pass through symbolic links or junctions.");
                if (string.Equals(current.FullName, ProjectRoot, PathComparison))
                    break;
            }
        }

        public static void RejectLinkedEntries(string directory)
        {
            RequireBuildDirectory(directory);
            if (!Directory.Exists(directory))
                return;

            var pending = new Stack<string>();
            pending.Push(directory);
            while (pending.Count > 0)
            {
                foreach (string entry in Directory.GetFileSystemEntries(pending.Pop()))
                {
                    FileAttributes attributes = File.GetAttributes(entry);
                    if ((attributes & FileAttributes.ReparsePoint) != 0)
                        throw new IOException("Build content cannot contain symbolic links or junctions.");
                    if ((attributes & FileAttributes.Directory) != 0)
                        pending.Push(entry);
                }
            }
        }
    }
}
