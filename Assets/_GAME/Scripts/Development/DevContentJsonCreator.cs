using Newtonsoft.Json.Converters;
using Newtonsoft.Json;
using System.IO;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using DevTools;

public class DevContentJsonCreator : MonoBehaviour
{
#if UNITY_EDITOR
    [MenuItem("Assets/Create/Content/Cyberpedia Article - JSON", false, 10)]
    public static void CreateCyberpediaArticleJSON()
    {
        SaveObjectToJson(CyberpediaArticle.Example, $"Cyberpedia Article.{ContentConstValues.EXTENSION_ARTICLE}");
    }

    [MenuItem("Assets/Create/Content/Mail - JSON", false, 10)]
    public static void CreateMailJSON()
    {
        SaveObjectToJson(MailData.Example, $"Mail.{ContentConstValues.EXTENSION_MAIL}");
    }

    private static void SaveObjectToJson(object objectToSave, string fileName)
    {
        if (ValidatePath(out string folderPath) == false)
            return;

        string jsonText = JsonConvert.SerializeObject(objectToSave, Formatting.Indented, DevContentUtilities.JsonSettings);
        string fullPath = Path.Combine(folderPath, fileName);

        File.WriteAllText(fullPath, jsonText);
        AssetDatabase.Refresh();
    }

    private static bool ValidatePath(out string folderPath)
    {
        folderPath = GetSelectedFolderPath();
        
        if (string.IsNullOrEmpty(folderPath))
        {
            Debug.LogError("Could not find the selected path: " + folderPath);
            return false;
        }

        return true;
    }

    private static string GetSelectedFolderPath()
    {
        string path = AssetDatabase.GetAssetPath(Selection.activeObject);

        if (string.IsNullOrEmpty(path))
            return "Assets";

        if (Directory.Exists(path))
            return path;

        return Path.GetDirectoryName(path);
    }
#endif
}
