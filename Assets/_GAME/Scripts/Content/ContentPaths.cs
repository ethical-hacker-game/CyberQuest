public static class ContentPaths
{
    // The dev editor sets this to the unit currently being edited. Gameplay leaves it
    // null and resolves the active unit from GameController.SelectedUnit. Without this,
    // editor content screens (apps/commands/web/Cyberpedia) fall back to the whole units
    // root and show content from every unit instead of the one being edited.
    public static string EditorActiveUnitID { get; set; }

    private static string ActiveUnitID =>
        GameController.SelectedUnit != null
            ? GameController.SelectedUnit.unitID
            : EditorActiveUnitID;

    public static bool HasActiveUnit => string.IsNullOrEmpty(ActiveUnitID) == false;

    public static string UnitRoot(string unitID)
    {
        return ContentLoader.CombinePath(ContentConstValues.FOLDER_UNITS, unitID);
    }

    public static string UnitFolder(string unitID, string folder)
    {
        return ContentLoader.CombinePath(UnitRoot(unitID), folder);
    }

    public static string ActiveUnitRoot()
    {
        if (HasActiveUnit == false)
            return ContentConstValues.FOLDER_UNITS;

        return UnitRoot(ActiveUnitID);
    }

    public static string ActiveUnitFolder(string folder)
    {
        if (HasActiveUnit == false)
            return ContentConstValues.FOLDER_UNITS;

        return UnitFolder(ActiveUnitID, folder);
    }

    public static string UnitFolderOrAll(string unitID, string folder)
    {
        if (string.IsNullOrEmpty(unitID) == false)
            return UnitFolder(unitID, folder);

        if (HasActiveUnit)
            return ActiveUnitFolder(folder);

        return ContentConstValues.FOLDER_UNITS;
    }
}
