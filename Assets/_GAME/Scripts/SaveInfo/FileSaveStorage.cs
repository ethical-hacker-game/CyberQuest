using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

/// <summary>
/// Default persistence: one PlayerInfo and one SaveInfo JSON file per save in <see cref="SaveLoad.SavesPath"/>.
/// </summary>
public class FileSaveStorage : ISaveStorage
{
    public List<SaveInfo> GetAllSaveInfo()
    {
        SaveLoad.CheckSavesPath();

        List<SaveInfo> loadedSaveInfos = new List<SaveInfo>();

        string[] infoPaths = Directory.EnumerateFiles(SaveLoad.SavesPath)
            .Where(x =>
                Path.GetFileNameWithoutExtension(x).StartsWith(SaveLoad.SaveInfoPrefix) &&
                x.EndsWith(SaveLoad.Extension))
            .OrderByDescending(x => File.GetLastWriteTime(x))
            .ToArray();


        for (int i = 0; i < infoPaths.Length; i++)
        {
            try
            {
                string jsonContent = File.ReadAllText(infoPaths[i]);
                SaveInfo loadedInfo = JsonUtility.FromJson<SaveInfo>(jsonContent);
                loadedSaveInfos.Add(loadedInfo);
            }
            catch (Exception e)
            {
                if (e is FileNotFoundException == false)
                    Debug.LogException(e);

            }
        }

        return loadedSaveInfos;
    }

    public void Save(PlayerInfo playerInfo, SaveInfo saveInfo)
    {
        SaveLoad.CheckSavesPath();

        string jsonContent = JsonUtility.ToJson(playerInfo, true);
        File.WriteAllText(SaveLoad.GetPlayerInfoPath(playerInfo.saveId), jsonContent);

        jsonContent = JsonUtility.ToJson(saveInfo, true);
        File.WriteAllText(SaveLoad.GetSaveInfoPath(saveInfo.saveId), jsonContent);
    }

    public PlayerInfo Load(string saveId)
    {
        SaveLoad.CheckSavesPath();

        try
        {
            string jsonContent = File.ReadAllText(SaveLoad.GetPlayerInfoPath(saveId));
            return JsonUtility.FromJson<PlayerInfo>(jsonContent);
        }
        catch (Exception e)
        {
            if (e is FileNotFoundException == false)
                Debug.LogException(e);

            return null;
        }
    }

    public void Delete(string saveId)
    {
        SaveLoad.CheckSavesPath();

        TryDeleteFile(SaveLoad.GetPlayerInfoPath(saveId));
        TryDeleteFile(SaveLoad.GetSaveInfoPath(saveId));
    }

    private static void TryDeleteFile(string path)
    {
        if (File.Exists(path))
            File.Delete(path);
    }
}
