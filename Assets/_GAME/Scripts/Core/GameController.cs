using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameController : GameSystemComponent
{
    public static GameController Instance { get; private set; }

    public static Unit SelectedUnit { get; private set; }

    public static PlayerInfo CurrentPlayerInfo { get; private set; }

    public static void SetupNewGameplay(Unit unit)
    {
        SetupNewGameplayWithoutReload(unit);
        SceneLoader.LoadSceneAsync(SceneLoader.GAME_SCENE_NAME);
    }

    public static void SetupNewGameplayWithoutReload(Unit unit)
    {
        if (unit == null)
            return;

        string newSaveId = $"{unit.unitID}_{DateTime.Now.ToString("yyyyMMdd_HHmmss")}";

        SelectedUnit = unit;
        CurrentPlayerInfo = new PlayerInfo(newSaveId, unit.unitID);
        PlayerText.ApplyForNewGame(CurrentPlayerInfo, unit);

        PlayerController.SetupNewGame = PlayerController.SetupGameType.New;
        QuestManager.LoadQuestsAndTasks(unit.unitID, true);
        MailsManager.LoadMails(unit.unitID, true);
        SkillsManager.LoadSkills(unit.unitID, true);
        PicturesManager.LoadUnitPictures(unit.unitID);
    }

    public static void SetupLoadedGameplay(PlayerInfo playerInfo)
    {
        if (SetupLoadedGameplayWithoutReload(playerInfo) == false)
            return;

        SceneLoader.LoadSceneAsync(SceneLoader.GAME_SCENE_NAME);
    }

    public static bool SetupLoadedGameplayWithoutReload(PlayerInfo playerInfo)
    {
        SelectedUnit = CourseManager.GetUnitByID(playerInfo.unitID);

        if (SelectedUnit == null)
            return false;

        CurrentPlayerInfo = playerInfo;
        PlayerController.SetupNewGame = PlayerController.SetupGameType.Load;
        QuestManager.LoadQuestsAndTasks(SelectedUnit.unitID, true);
        MailsManager.LoadMails(SelectedUnit.unitID, true);
        SkillsManager.LoadSkills(SelectedUnit.unitID, true);
        PicturesManager.LoadUnitPictures(SelectedUnit.unitID);
        return true;
    }

    public static void SaveGame()
    {
        if (PlayerController.Instance == null)
            return;

        if (CurrentPlayerInfo == null)
            return;

        CurrentPlayerInfo.unitName = SelectedUnit.unitName;
        CurrentPlayerInfo.unitID = SelectedUnit.unitID;

        SaveLoad.Save(CurrentPlayerInfo);
    }

    private void Awake()
    {
        if(Instance != null)
        {
            gameObject.Destroy();
            return;
        }

        Instance = this;
        gameObject.DontDestroyOnLoadImproved();
    }

    private void Start()
    {

    }
}
