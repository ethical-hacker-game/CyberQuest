[System.Serializable]
public class SaveInfo
{
    public string saveId = "game";
    public string userName = "Alex";
    public string unitName = "Unit001";
    public string gameVersion;
    public int saveFileVersion = 0;
    public string currentMission;
    public string saveTime;
    public string unitID;

    public SaveInfo(string saveId)
    {
        this.saveId = saveId;
    }
}
