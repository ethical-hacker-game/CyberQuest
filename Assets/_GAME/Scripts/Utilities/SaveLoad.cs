using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class SaveLoad
{
    public static PlayerInfo LoadedPlayerInfo;
    private static readonly int saveFileVersion = 3;

    private static ISaveStorage storage;

    public static string NewPlayerInfoName => UtilityMethods.GetCurrentTimestamp();
    public const string SaveInfoPrefix = "info_";
    public const string PlayerInfoPrefix = "save_";
    public const string Extension = ".json";

    public static string DevSavesPath => Application.persistentDataPath + "/DevSaves/";
    public static string GameSavesPath => Application.persistentDataPath + "/Saves/";

    public static string SavesPath
#if UNITY_EDITOR
        => DevSavesPath;
#else
        => GameSavesPath;
#endif

    public static string GetSaveInfoPath(string playerInfoId) => SavesPath + SaveInfoPrefix + playerInfoId + Extension;
    public static string GetPlayerInfoPath(string playerInfoId) => SavesPath + PlayerInfoPrefix + playerInfoId + Extension;

    /// <summary>Player progress is stored in local JSON files.</summary>
    public static ISaveStorage Storage
    {
        get
        {
            if (storage == null)
                storage = new FileSaveStorage();

            return storage;
        }
    }

    public static List<SaveInfo> GetAllSaveInfo()
    {
        List<SaveInfo> loadedSaveInfos = Storage.GetAllSaveInfo();

        for (int i = 0; i < loadedSaveInfos.Count; i++)
            CheckFileVersion(loadedSaveInfos[i]);

        return loadedSaveInfos;
    }

    public static void Save(PlayerInfo playerInfo)
    {
        playerInfo.gameVersion = "v" + Application.version;
        playerInfo.saveFileVersion = saveFileVersion;

        Storage.Save(playerInfo, CreateSaveInfo(playerInfo));
    }

    public static SaveInfo CreateSaveInfo(PlayerInfo playerInfo)
    {
        DateTime now = DateTime.Now;

        SaveInfo saveInfo = new SaveInfo(playerInfo.saveId);
        saveInfo.userName = playerInfo.userName;
        saveInfo.unitName = playerInfo.unitName;
        saveInfo.unitID = playerInfo.unitID;
        saveInfo.currentMission = playerInfo.currentMission;
        saveInfo.gameVersion = playerInfo.gameVersion;
        saveInfo.saveFileVersion = playerInfo.saveFileVersion;
        saveInfo.saveTime = $"{now.ToString("yyyy.MM.dd")} {now.ToShortTimeString()}";

        return saveInfo;
    }

    public static PlayerInfo LoadGame(string playerInfoId)
    {
        LoadedPlayerInfo = Storage.Load(playerInfoId);

        if (LoadedPlayerInfo != null)
            CheckFileVersion(LoadedPlayerInfo);

        return LoadedPlayerInfo;
    }

    public static void Delete(string gameInfoId)
    {
        Storage.Delete(gameInfoId);
    }

    public static void CheckSavesPath()
    {
        if (!Directory.Exists(SavesPath))
            Directory.CreateDirectory(SavesPath);
    }

    private static void CheckFileVersion(SaveInfo saveInfo)
    {
        //Examples
        /*if (playerInfo.saveFileVersion < 3)
        {
            playerInfo.askReview = false;
        }

        if (playerInfo.saveFileVersion < 4)
        {
            playerInfo.AnalyticsInfo.started_games++;
        }*/
    }

    private static void CheckFileVersion(PlayerInfo playerInfo)
    {
        if (playerInfo?.unit == null)
            return;

        playerInfo.unit.readArticleIDs ??= new List<string>();

        //Examples
        /*if (playerInfo.saveFileVersion < 3)
        {
            playerInfo.askReview = false;
        }

        if (playerInfo.saveFileVersion < 4)
        {
            playerInfo.AnalyticsInfo.started_games++;
        }*/
    }
}
