using UnityEditor;
using UnityEngine;
using System.Linq;

namespace BuildScripts
{
    public class BuildProfilesWindow : EditorWindow
    {
        [MenuItem("Build/Build Profiles Window")]
        public static void ShowWindow()
        {
            GetWindow<BuildProfilesWindow>("Build Profiles");
        }

        BuildProfile[] profiles;
        Vector2 scroll;

        // Filters
        BuildTarget? platformFilter = null;
        BuildProfile.BuildProfileType? typeFilter = null;

        void OnEnable()
        {
            ReloadProfiles();
        }

        void ReloadProfiles()
        {
            profiles = Resources.LoadAll<BuildProfile>("BuildProfiles/");
        }

        void CopyContentFolder()
        {
            ReloadProfiles();

            for (int i = 0; i < profiles.Length; i++)
            {
                profiles[i].CopyContentFolder();
            }
        }

        void BuildAll(BuildProfile.BuildProfileType types)
        {
            if (types == 0)
                return;

            if (BuildProfilesMenu.RejectIfDemoBuild(types))
                types &= ~BuildProfile.BuildProfileType.Demo;

            if (types == 0)
                return;

            ReloadProfiles();
            IncreaseBuildNumber.ChangeBuildNumber(true);

            for (int i = 0; i < profiles.Length; i++)
            {
                if (types.HasFlag(profiles[i].buildProfileType) == false)
                    continue;

                IncreaseBuildNumber.ChangeBuildNumber(false);
                BuildProfile profile = profiles[i];
                BuildProfilesMenu.BuildWithProfile(profile);
            }
        }

        void OnGUI()
        {
            GUILayout.Space(5);
            RenderTopLine();
            GUILayout.Space(5);

            EditorGUILayout.BeginHorizontal();
            RenderBuildProfileList();
            GUILayout.Space(5);
            RenderBuildButtons();
            EditorGUILayout.EndHorizontal();
        }

        private void RenderTopLine()
        {
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Reload Profiles")) ReloadProfiles();
            if (GUILayout.Button("Copy CONTENT to builds")) CopyContentFolder();

            EditorGUILayout.LabelField(
                "v" + Application.version,
                new GUIStyle(EditorStyles.label) { alignment = TextAnchor.MiddleRight },
                GUILayout.ExpandWidth(true)
            );

            if (GUILayout.Button("--")) ChangeGameVersionNumber.ChangeVersionNumber(-2, -1);
            if (GUILayout.Button("++")) ChangeGameVersionNumber.ChangeVersionNumber(-2, 1);
            if (GUILayout.Button("-")) ChangeGameVersionNumber.ChangeVersionNumber(-1, -1);
            if (GUILayout.Button("+")) ChangeGameVersionNumber.ChangeVersionNumber(-1, 1);
            if (GUILayout.Button("Decrease build number")) IncreaseBuildNumber.ChangeBuildNumber(false);
            EditorGUILayout.EndHorizontal();
        }

        private void RenderBuildButtons()
        {
            EditorGUILayout.BeginVertical();

            //if (GUILayout.Button("BUILD ALL"))
            //    BuildAll(BuildProfile.BuildProfileType.Game | BuildProfile.BuildProfileType.Editor | BuildProfile.BuildProfileType.Demo);
            
            //GUILayout.Space(5);

            if (GUILayout.Button("BUILD GAME"))
                BuildAll(BuildProfile.BuildProfileType.Game);
            if (GUILayout.Button("BUILD EDITOR"))
                BuildAll(BuildProfile.BuildProfileType.Editor);
            if (GUILayout.Button("BUILD DEMO"))
                BuildAll(BuildProfile.BuildProfileType.Demo);

            EditorGUILayout.EndVertical();
        }

        private void RenderBuildProfileList()
        {
            float listWidth = position.width - 250;
            EditorGUILayout.BeginVertical(GUILayout.Width(listWidth));
            RenderFiltersPanel();
            GUILayout.Space(5);

            scroll = EditorGUILayout.BeginScrollView(scroll);

            var filtered = profiles.AsEnumerable();

            if (platformFilter.HasValue)
                filtered = filtered.Where(p => p != null && p.targetPlatform == platformFilter.Value);

            if (typeFilter.HasValue)
                filtered = filtered.Where(p => p != null && p.buildProfileType == typeFilter.Value);

            if (filtered == null || filtered.Count() == 0)
            {
                EditorGUILayout.HelpBox("No Build Profiles found!", MessageType.Warning);
            }
            else
            {
                foreach (var p in filtered)
                {
                    if (p != null)
                        ShowProfileUI(p);
                }
            }

            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        private void RenderFiltersPanel()
        {
            EditorGUILayout.BeginHorizontal();

            // Platform filter
            EditorGUILayout.LabelField("Platform", GUILayout.Width(60));
            BuildTarget[] platformValues;
            string[] platformNames = GetBuildTargetPopupOptions(out platformValues);

            // Index 0 == All, 1..N == platformValues[0..N-1]
            int platformIndex = platformFilter.HasValue
                ? (System.Array.IndexOf(platformValues, platformFilter.Value) + 1)
                : 0;
            if (platformIndex < 0)
                platformIndex = 0;

            int chosenPlatformIndex = EditorGUILayout.Popup(platformIndex, platformNames, GUILayout.MinWidth(180));
            platformFilter = chosenPlatformIndex == 0 ? (BuildTarget?)null : platformValues[chosenPlatformIndex - 1];

            GUILayout.Space(10);

            // Type filter
            EditorGUILayout.LabelField("Type", GUILayout.Width(40));
            BuildProfile.BuildProfileType[] typeValues;
            string[] typeNames = GetBuildProfileTypePopupOptions(out typeValues);

            // Index 0 == All, 1..N == typeValues[0..N-1]
            int typeIndex = typeFilter.HasValue
                ? (System.Array.IndexOf(typeValues, typeFilter.Value) + 1)
                : 0;
            if (typeIndex < 0)
                typeIndex = 0;

            int chosenTypeIndex = EditorGUILayout.Popup(typeIndex, typeNames, GUILayout.MinWidth(190));
            typeFilter = chosenTypeIndex == 0 ? (BuildProfile.BuildProfileType?)null : typeValues[chosenTypeIndex - 1];

            GUILayout.FlexibleSpace();

            if (GUILayout.Button("Clear", GUILayout.Width(60)))
            {
                platformFilter = null;
                typeFilter = null;
                GUI.FocusControl(null);
            }

            EditorGUILayout.EndHorizontal();
        }

        private static string[] GetBuildTargetPopupOptions(out BuildTarget[] targets)
        {
            targets = new[]
            {
                BuildTarget.WebGL,
                BuildTarget.StandaloneWindows64,
                BuildTarget.StandaloneOSX,
                BuildTarget.StandaloneLinux64,
            };

            // 0 == All
            var names = new string[targets.Length + 1];
            names[0] = "All";
            for (int i = 0; i < targets.Length; i++)
                names[i + 1] = targets[i].ToString();

            return names;
        }

        private static string[] GetBuildProfileTypePopupOptions(out BuildProfile.BuildProfileType[] types)
        {
            types = new[]
            {
                BuildProfile.BuildProfileType.Game,
                BuildProfile.BuildProfileType.Editor,
                BuildProfile.BuildProfileType.Demo,
            };

            // 0 == All
            var names = new string[types.Length + 1];
            names[0] = "All";
            for (int i = 0; i < types.Length; i++)
                names[i + 1] = types[i].ToString();

            return names;
        }

        private void ShowProfileUI(BuildProfile profile)
        {
            float width = position.width - 250;

            EditorGUILayout.BeginVertical("box", GUILayout.Width(width - 180));

            //header
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.BeginVertical(GUILayout.Width(width * 0.7f - 60));
            EditorGUILayout.LabelField(profile.BuildName, new GUIStyle(EditorStyles.boldLabel) { fontSize = 18 });
            EditorGUILayout.EndVertical();

            EditorGUILayout.BeginVertical(GUILayout.Width(width * 0.1f - 60));
            if (GUILayout.Button("Edit Profile")) Selection.activeObject = profile;
            EditorGUILayout.EndVertical();

            EditorGUILayout.BeginVertical(GUILayout.Width(width * 0.2f - 60));
            var boldButtonStyle = new GUIStyle(GUI.skin.button);
            boldButtonStyle.fontStyle = FontStyle.Bold;
            if (GUILayout.Button("Build " + profile.BuildName, boldButtonStyle)) BuildProfilesMenu.BuildWithProfile(profile);
            EditorGUILayout.EndVertical();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            //build options
            EditorGUILayout.BeginVertical(GUILayout.Width(width * 0.3f - 20));
            var options = profile.buildOptions
                .ToString()
                .Split(',')
                .Select(opt => opt.Trim())
                .Where(opt => !string.IsNullOrEmpty(opt));
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Keep Content Repository Files:");
            EditorGUILayout.LabelField(profile.keepRepositoryFiles.ToString(), EditorStyles.boldLabel);
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.LabelField("Build Options:", EditorStyles.boldLabel);
            foreach (var opt in options)
            {
                EditorGUILayout.LabelField("  - " + opt);
            }
            EditorGUILayout.EndVertical();

            //scenes
            EditorGUILayout.BeginVertical(GUILayout.Width(width * 0.7f - 20));
            EditorGUILayout.LabelField(profile.gameName, EditorStyles.boldLabel);
            EditorGUILayout.LabelField(profile.targetPlatform.ToString());
            EditorGUILayout.LabelField(profile.outputFolder);
            EditorGUILayout.LabelField("Scenes:", EditorStyles.boldLabel);
            if (profile.scenes != null && profile.scenes.Count > 0)
            {
                foreach (var scene in profile.scenes)
                {
                    if (scene != null)
                        EditorGUILayout.LabelField("  - " + scene.name);
                }
            }
            else
            {
                EditorGUILayout.LabelField("NO SCENES!");
            }
            EditorGUILayout.EndVertical();
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
            GUILayout.Space(10);
        }
    }
}
