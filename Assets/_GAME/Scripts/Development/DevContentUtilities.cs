using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace DevTools
{
    public class DevContentUtilities : MonoBehaviour
    {
        public static readonly JsonSerializerSettings JsonSettings = new JsonSerializerSettings
        {
            ContractResolver = new EnhancedIgnoreContractResolver(),
            Formatting = Formatting.Indented,
            Converters = { new StringEnumConverter() },
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore
        };

        private class EnhancedIgnoreContractResolver : DefaultContractResolver
        {
            protected override IList<JsonProperty> CreateProperties(Type type, MemberSerialization memberSerialization)
            {
                var allProps = base.CreateProperties(type, memberSerialization);

                return allProps.Where(p =>
                {
                    // Exclude properties marked with [DoNotSerialize]
                    if (p.AttributeProvider.GetAttributes(typeof(DoNotSerializeAttribute), true).Any())
                        return false;

                    // Include fields (they're fine)
                    if (p.PropertyType != null && p.UnderlyingName != null)
                    {
                        var member = type.GetMember(p.UnderlyingName).FirstOrDefault();
                        if (member is FieldInfo) return true;
                    }

                    // Allow read-only properties if they have [JsonProperty] attribute
                    if (!p.Writable)
                    {
                        if (p.AttributeProvider.GetAttributes(typeof(JsonPropertyAttribute), true).Any())
                            return true;
                        return false;
                    }

                    return true;
                })
                .ToList();
            }
        }

        #region Units
        public static void SaveUnit(Unit unit)
        {
            string unitID = unit.unitID;
            string jsonText = JsonConvert.SerializeObject(unit, JsonSettings);

            string directoryPath = ContentLoader.GetContentPath(ContentPaths.UnitRoot(unitID));
            string filePath = ContentLoader.CombinePath(directoryPath, $"{unitID}.{ContentConstValues.EXTENSION_UNIT}");

            PreparePath(directoryPath);
            File.WriteAllText(filePath, jsonText);
        }

        public static void DeleteUnit(Unit unit)
        {
            string unitDirectoryPath = ContentLoader.GetContentPath(ContentPaths.UnitRoot(unit.unitID));

            if (Directory.Exists(unitDirectoryPath))
                Directory.Delete(unitDirectoryPath, true);
        }
        #endregion


        #region Quests
        public static void SaveQuest(Quest quest, string unitID)
        {
            string questID = quest.questID;
            string jsonText = JsonConvert.SerializeObject(quest, Formatting.Indented, JsonSettings);

            string directoryPathQuest = UnitFolderPath(unitID, ContentConstValues.FOLDER_QUESTS, questID);
            string filePath = ContentLoader.CombinePath(directoryPathQuest, $"{questID}.{ContentConstValues.EXTENSION_QUEST}");

            PreparePath(directoryPathQuest);
            File.WriteAllText(filePath, jsonText);
        }

        public static void DeleteQuest(Quest quest, string unitID)
        {
            string questID = quest.questID;
            string directoryPath = UnitFolderPath(unitID, ContentConstValues.FOLDER_QUESTS, questID);

            PreparePath(directoryPath);
            Directory.Delete(directoryPath, true);
        }
        #endregion

        #region Tasks
        public static void SaveTask(TaskBase task, string unitID, string questID)
        {
            string taskID = task.taskID;
            string jsonText = JsonConvert.SerializeObject(task, JsonSettings);

            string directoryPathQuest = UnitFolderPath(unitID, ContentConstValues.FOLDER_QUESTS, questID);
            string filePath = ContentLoader.CombinePath(directoryPathQuest, $"{taskID}.{ContentConstValues.EXTENSION_TASK}");

            PreparePath(directoryPathQuest);
            File.WriteAllText(filePath, jsonText);
        }

        public static void DeleteTask(TaskBase task, string unitID, string questID)
        {
            string taskID = task.taskID;
            string directoryPath = UnitFolderPath(unitID, ContentConstValues.FOLDER_QUESTS, questID);
            string filePath = ContentLoader.CombinePath(directoryPath, $"{taskID}.{ContentConstValues.EXTENSION_TASK}");

            if (Directory.Exists(directoryPath))
                File.Delete(filePath);
        }

        #endregion

        #region Mails
        public static void SaveMail(MailData mail, string unitID)
        {
            string mailID = mail.mailID;
            string jsonText = JsonConvert.SerializeObject(mail, Formatting.Indented, JsonSettings);

            string directoryPathUnit = UnitFolderPath(unitID, ContentConstValues.FOLDER_MAILS);
            string filePath = ContentLoader.CombinePath(directoryPathUnit, $"{mailID}.{ContentConstValues.EXTENSION_MAIL}");

            PreparePath(directoryPathUnit);
            File.WriteAllText(filePath, jsonText);
        }

        public static void DeleteMail(MailData mail, string unitID)
        {
            string mailID = mail.mailID;
            string directoryPath = UnitFolderPath(unitID, ContentConstValues.FOLDER_MAILS);
            string filePath = ContentLoader.CombinePath(directoryPath, $"{mailID}.{ContentConstValues.EXTENSION_MAIL}");

            PreparePath(directoryPath);
            File.Delete(filePath);
        }
        #endregion

        #region POIs
        public static void SavePOIs(POIs pois)
        {
            string jsonText = JsonConvert.SerializeObject(pois, Formatting.Indented, JsonSettings);

            string directoryPathPOIs = UnitFolderPath(pois.unitID, ContentConstValues.FOLDER_POIS);
            string filePath = ContentLoader.CombinePath(directoryPathPOIs, $"pois.{ContentConstValues.EXTENSION_POIS}");

            PreparePath(directoryPathPOIs);
            File.WriteAllText(filePath, jsonText);
        }
        #endregion

        #region Commands
        public static void SaveCommand(Command command)
        {
            if (TryGetActiveUnit(out string unitID) == false)
                return;

            string commandID = command.command;
            string jsonText = JsonConvert.SerializeObject(command, Formatting.Indented);

            string directoryPath = UnitFolderPath(unitID, ContentConstValues.FOLDER_COMMANDS);
            string filePath = ContentLoader.CombinePath(directoryPath, $"{commandID}.{ContentConstValues.EXTENSION_COMMAND}");

            PreparePath(directoryPath);
            File.WriteAllText(filePath, jsonText);
        }

        public static void DeleteCommand(Command command)
        {
            if (TryGetActiveUnit(out string unitID) == false)
                return;

            string directoryPath = UnitFolderPath(unitID, ContentConstValues.FOLDER_COMMANDS);
            string filePath = ContentLoader.CombinePath(directoryPath, $"{command.command}.{ContentConstValues.EXTENSION_COMMAND}");

            PreparePath(directoryPath);

            if (File.Exists(filePath))
                File.Delete(filePath);
        }
        #endregion

        #region Apps
        public static void SaveApp(AppCore appCore)
        {
            if (TryGetActiveUnit(out string unitID) == false)
                return;

            string appCoreID = appCore.appID;
            string jsonText = JsonConvert.SerializeObject(appCore, JsonSettings);

            string directoryPath = UnitFolderPath(unitID, ContentConstValues.FOLDER_APPS, appCoreID);
            string filePath = ContentLoader.CombinePath(directoryPath, $"{appCoreID}.{ContentConstValues.EXTENSION_APP}");

            PreparePath(directoryPath);
            File.WriteAllText(filePath, jsonText);
        }

        public static void DeleteApp(AppCore appCore)
        {
            if (TryGetActiveUnit(out string unitID) == false)
                return;

            string appID = appCore.appID;
            string appDirectoryPath = UnitFolderPath(unitID, ContentConstValues.FOLDER_APPS, appID);

            PreparePath(appDirectoryPath);
            Directory.Delete(appDirectoryPath, true);
        }

        public static void SaveAppScenario(AppScenario appScenario)
        {
            if (TryGetActiveUnit(out string unitID) == false)
                return;

            string appCoreID = appScenario.appID;
            string appScenarioID = appScenario.scenarioID;
            string jsonText = JsonConvert.SerializeObject(appScenario, JsonSettings);

            string directoryPath = UnitFolderPath(unitID, ContentConstValues.FOLDER_APPS, appCoreID, appScenarioID);
            string filePath = ContentLoader.CombinePath(directoryPath, $"{appScenarioID}.{ContentConstValues.EXTENSION_APPSCENARIO}");

            PreparePath(directoryPath);
            File.WriteAllText(filePath, jsonText);
        }

        public static void DeleteAppScenario(AppScenario appScenario)
        {
            if (TryGetActiveUnit(out string unitID) == false)
                return;

            string appCoreID = appScenario.appID;
            string appScenarioID = appScenario.scenarioID;
            string directoryPath = UnitFolderPath(unitID, ContentConstValues.FOLDER_APPS, appCoreID, appScenarioID);

            PreparePath(directoryPath);
            Directory.Delete(directoryPath, true);
        }

        public static AppImageCleanupResult DeleteUnusedAppImages(AppCore appCore)
        {
            AppImageCleanupResult result = new AppImageCleanupResult();

            if (appCore == null || string.IsNullOrEmpty(appCore.appID))
                return result;

            if (TryGetActiveUnit(out string unitID) == false)
                return result;

            string appDirectoryPath = UnitFolderPath(unitID, ContentConstValues.FOLDER_APPS, appCore.appID);

            if (Directory.Exists(appDirectoryPath) == false)
                return result;

            HashSet<string> usedImagePaths = GetUsedAppImagePaths(appCore, appDirectoryPath);
            string[] imageFiles = Directory.GetFiles(appDirectoryPath, "*.*", SearchOption.AllDirectories)
                .Where(IsSupportedImageFile)
                .ToArray();

            result.ScannedImages = imageFiles.Length;

            foreach (string imageFilePath in imageFiles)
            {
                string normalizedPath = NormalizeFilePath(imageFilePath);

                if (usedImagePaths.Contains(normalizedPath))
                    continue;

                DeleteFileWithMeta(imageFilePath, result);
            }

            return result;
        }

        private static HashSet<string> GetUsedAppImagePaths(AppCore appCore, string appDirectoryPath)
        {
            HashSet<string> usedImagePaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            List<AppScenario> appScenarios = AppsManager.Instance.GetAppScenariosByAppCoreID(appCore.appID);

            AddMatchingImagePaths(usedImagePaths, appDirectoryPath, $"{appCore.appID}_icon");
            AddMatchingImagePaths(usedImagePaths, appDirectoryPath, $"{appCore.appID}_background");

            for (int scenarioIndex = 0; scenarioIndex < appScenarios.Count; scenarioIndex++)
            {
                AppScenario scenario = appScenarios[scenarioIndex];

                if (scenario == null)
                    continue;

                string scenarioPath = ResolveScenarioPath(scenario);
                string imageExtension = NormalizeExtension(scenario.extension);

                if (scenario.stages == null)
                    continue;

                for (int stageIndex = 0; stageIndex < scenario.stages.Count; stageIndex++)
                {
                    AppScenario.Stage stage = scenario.stages[stageIndex];

                    if (stage == null || stage.frames == null)
                        continue;

                    for (int frameIndex = 0; frameIndex < stage.frames.Count; frameIndex++)
                    {
                        AppScenario.Frame frame = stage.frames[frameIndex];

                        if (frame == null || string.IsNullOrEmpty(frame.imageName))
                            continue;

                        if (string.IsNullOrEmpty(imageExtension))
                            AddMatchingImagePaths(usedImagePaths, scenarioPath, frame.imageName);
                        else
                            usedImagePaths.Add(NormalizeFilePath(Path.Combine(scenarioPath, $"{frame.imageName}.{imageExtension}")));
                    }
                }
            }

            return usedImagePaths;
        }

        private static string ResolveScenarioPath(AppScenario scenario)
        {
            string scenarioPath = scenario.ScenarioPath;

            if (Path.IsPathRooted(scenarioPath))
                return scenarioPath;

            return ContentLoader.GetContentPath(scenarioPath);
        }

        private static void AddMatchingImagePaths(HashSet<string> usedImagePaths, string directoryPath, string imageName)
        {
            foreach (string extension in SupportedImageExtensions)
            {
                usedImagePaths.Add(NormalizeFilePath(Path.Combine(directoryPath, $"{imageName}{extension}")));
            }
        }

        private static bool IsSupportedImageFile(string filePath)
        {
            string extension = Path.GetExtension(filePath);
            return SupportedImageExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase);
        }

        private static string NormalizeExtension(string extension)
        {
            if (string.IsNullOrEmpty(extension))
                return string.Empty;

            return extension.Trim().TrimStart('.');
        }

        private static string NormalizeFilePath(string filePath)
        {
            return Path.GetFullPath(filePath);
        }

        private static void DeleteFileWithMeta(string filePath, AppImageCleanupResult result)
        {
            DeleteExistingFile(filePath);
            result.DeletedImages++;
            result.DeletedImagePaths.Add(filePath);

            string metaFilePath = $"{filePath}.meta";

            if (File.Exists(metaFilePath) == false)
                return;

            DeleteExistingFile(metaFilePath);
            result.DeletedMetaFiles++;
        }

        private static void DeleteExistingFile(string filePath)
        {
            File.SetAttributes(filePath, FileAttributes.Normal);
            File.Delete(filePath);
        }

        private static readonly string[] SupportedImageExtensions =
        {
            ".png",
            ".jpg",
            ".jpeg"
        };

        public class AppImageCleanupResult
        {
            public int ScannedImages { get; set; }
            public int DeletedImages { get; set; }
            public int DeletedMetaFiles { get; set; }
            public List<string> DeletedImagePaths { get; } = new List<string>();

            public string ToSummary()
            {
                return $"Scanned images: {ScannedImages}\n" +
                       $"Deleted unused images: {DeletedImages}\n" +
                       $"Deleted meta files: {DeletedMetaFiles}";
            }
        }
        #endregion

        #region Utilities

        private static void PreparePath(string path)
        {
            if (Directory.Exists(path) == false)
                Directory.CreateDirectory(path);
        }

        private static string UnitFolderPath(string unitID, params string[] pathElements)
        {
            string path = ContentLoader.GetContentPath(ContentPaths.UnitRoot(unitID));

            for (int i = 0; i < pathElements.Length; i++)
            {
                path = ContentLoader.CombinePath(path, pathElements[i]);
            }

            return path;
        }

        private static bool TryGetActiveUnit(out string unitID)
        {
            // Units are standalone content packages; the unit itself is the source of truth.
            unitID = DevToolsManager.ActiveUnit?.unitID;

            if (string.IsNullOrEmpty(unitID) == false)
                return true;

            Debug.LogError("Cannot save unit content. Active unit is not selected.");
            return false;
        }

        #endregion
    }
}

