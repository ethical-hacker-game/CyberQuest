using UnityEngine;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using System;

namespace BuildScripts
{
    public class IncreaseBuildNumber : IPreprocessBuildWithReport, IPostprocessBuildWithReport
    {
        public int callbackOrder => 1;

        public void OnPreprocessBuild(BuildReport report)
        {
            ChangeBuildNumber(true);
        }

        public void OnPostprocessBuild(BuildReport report)
        {
            if (report.summary.result == BuildResult.Cancelled)
                ChangeBuildNumber(false);
        }

        public static void ChangeBuildNumber(bool increase)
        {
            try
            {
                string versionString = PlayerSettings.bundleVersion;
                string[] versionElements = versionString.Split(":");
                int buildVersion = int.Parse(versionElements[1]);

                if (increase)
                    buildVersion++;
                else
                    buildVersion--;

                PlayerSettings.bundleVersion = $"{versionElements[0]}:{buildVersion}";
            }
            catch (Exception ex)
            {
                Debug.LogError("Cannot increase build number.\n" + ex.Message);
            }
        }
    }
}
