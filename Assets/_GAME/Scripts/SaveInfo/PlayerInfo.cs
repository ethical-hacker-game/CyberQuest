using System;
using System.Collections.Generic;

[Serializable]
public class PlayerInfo
{
    public string saveId = "game";
    public string userName = "Alex";
    public string unitName = "Unit001";
    public string currentMission;
    public string gameVersion;
    public int saveFileVersion = 0;

    public string unitID;

    public float playTime = 0; //in minutes
    public UnitInfo unit = new UnitInfo();

    public PlayerInfo(string saveId, string unitID)
    {
        this.saveId = saveId;
        this.unitID = unitID;
    }
}
