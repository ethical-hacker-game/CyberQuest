using System.Collections.Generic;

/// <summary>
/// Persistence backend for the player progress. <see cref="SaveLoad"/> picks the implementation:
/// local JSON files (<see cref="FileSaveStorage"/>).
/// </summary>
public interface ISaveStorage
{
    List<SaveInfo> GetAllSaveInfo();
    void Save(PlayerInfo playerInfo, SaveInfo saveInfo);
    PlayerInfo Load(string saveId);
    void Delete(string saveId);
}
