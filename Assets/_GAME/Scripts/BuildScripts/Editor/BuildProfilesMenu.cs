using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
using System.IO;
using System.Linq;

namespace BuildScripts
{
    public static class BuildProfilesMenu
    {
        public static bool KeepRepositoryFiles { get; private set; } = false;

        public const string DemoBuildDisabledMessage =
            "Demo builds are disabled. Use a Game or Editor profile instead.";

        public static bool RejectIfDemoBuild(BuildProfile.BuildProfileType types)
        {
            if (types.HasFlag(BuildProfile.BuildProfileType.Demo) == false)
                return false;

            Debug.LogError("[Build] " + DemoBuildDisabledMessage);
            EditorUtility.DisplayDialog("Demo build disabled", DemoBuildDisabledMessage, "OK");
            return true;
        }

        public static bool RejectIfDemoBuild(BuildProfile profile)
        {
            if (profile == null)
                return false;

            return RejectIfDemoBuild(profile.buildProfileType);
        }

        public static BuildReport BuildWithProfile(BuildProfile profile)
        {
            if (profile == null)
                return null;

            if (RejectIfDemoBuild(profile))
                return null;

            string[] scenePaths = profile.scenes.Where(s => s != null)
                .Select(s => AssetDatabase.GetAssetPath(s)).ToArray();

            if (scenePaths.Length == 0)
            {
                EditorUtility.DisplayDialog("Build error", "No scenes assigned to build profile!", "OK");
                return null;
            }

            if (profile.gameName.IsNullOrEmpty() == false)
                PlayerSettings.productName = profile.gameName;
            
            BuildPlayerOptions buildOptions = new BuildPlayerOptions();
            buildOptions.scenes = scenePaths;
            buildOptions.target = profile.targetPlatform;
            buildOptions.options = profile.buildOptions;

            try
            {
                buildOptions.locationPathName = BuildOutputPaths.ResolvePlayerPath(profile);
                Directory.CreateDirectory(Path.GetDirectoryName(buildOptions.locationPathName));
            }
            catch (System.Exception exception)
            {
                Debug.LogError($"[Build] {exception.Message}");
                return null;
            }

            KeepRepositoryFiles = profile.keepRepositoryFiles;
            BuildReport report;
            try
            {
                report = BuildPipeline.BuildPlayer(buildOptions);
            }
            finally
            {
                KeepRepositoryFiles = false;
            }
            Debug.Log($"Build for {profile.BuildName} completed: {buildOptions.locationPathName}");
            return report;
        }
    }
}
