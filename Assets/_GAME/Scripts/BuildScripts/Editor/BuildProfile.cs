using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using System;

namespace BuildScripts
{
    [CreateAssetMenu(fileName = "BuildProfile", menuName = "Build Profile", order = 1)]
    public class BuildProfile : ScriptableObject
    {
        [EnumToggleButtons] public BuildProfileType buildProfileType;
        public string gameName;
        public string outputFolder;
        public BuildTarget targetPlatform;
        public BuildOptions buildOptions;
        public bool keepRepositoryFiles = false;
        public List<SceneAsset> scenes;

        public string BuildName 
        { 
            get => 
                $"{buildProfileType.ToString()}" +
                $" - " +
                $"{targetPlatform.ToString().Replace("Standalone", "")}"; 
        }

        [Button("Select Output Folder"), PropertySpace(10)]
        private void SelectOutputFolder()
        {
            string projectRoot = System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath, ".."));
            string path = EditorUtility.OpenFolderPanel("Select Output Folder", projectRoot, "");

            if (!string.IsNullOrEmpty(path))
            {
                try
                {
                    string relative = System.IO.Path.GetRelativePath(projectRoot, path).Replace('\\', '/');
                    BuildOutputPaths.ResolveDirectory(relative);
                    outputFolder = relative;
                    EditorUtility.SetDirty(this);
                }
                catch (Exception exception)
                {
                    EditorUtility.DisplayDialog("Invalid output folder", exception.Message, "OK");
                }
            }
        }

        [Button("Open Output Folder in Explorer"), PropertySpace(10)]
        private void OpenOutputFolderInExplorer()
        {
            if (string.IsNullOrEmpty(outputFolder))
            {
                EditorUtility.DisplayDialog("Error", "Output Folder is not set.", "OK");
                return;
            }

            string folderPath;
            try
            {
                folderPath = BuildOutputPaths.ResolveDirectory(outputFolder);
            }
            catch (Exception exception)
            {
                EditorUtility.DisplayDialog("Invalid output folder", exception.Message, "OK");
                return;
            }

            if (!System.IO.Directory.Exists(folderPath))
            {
                EditorUtility.DisplayDialog("Error", $"Folder does not exist:\n{folderPath}", "OK");
                return;
            }

            EditorUtility.RevealInFinder(folderPath);
        }

        [Button("Copy Content Folder")]
        public void CopyContentFolder()
        {
            try
            {
                string path = BuildOutputPaths.ResolvePlayerPath(this);
                CreateContentFolder.CopyContentFolder(path, targetPlatform, keepRepositoryFiles);
            }
            catch (Exception exception)
            {
                Debug.LogError($"[Build Copy CONTENT] {exception.Message}");
            }
        }

        [Flags]
        public enum BuildProfileType
        {
            None = 0,
            Game = 1,
            Editor = 2,
            Demo = 4
        }
    }
}
