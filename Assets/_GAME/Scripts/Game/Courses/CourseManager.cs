using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class CourseManager : GameSystemComponent
{
    public static CourseManager Instance { get; private set; }

    private static List<Unit> units = new List<Unit>();
    private static Dictionary<string, Texture2D> pictures = new Dictionary<string, Texture2D>();

    [ShowInInspector]
    public static List<Unit> Units => units;
    public static Dictionary<string, Texture2D> Pictures => pictures;

    public static Unit GetUnitByID(string unitID)
    {
        if (Units.IsNullOrEmpty())
            return null;

        for (int i = 0; i < Units.Count; i++)
        {
            if (Units[i].unitID == unitID)
                return Units[i];
        }

        return null;
    }

    private void Awake()
    {
        Instance = this;
        ReloadContent();
    }

    public static void ReloadContent()
    {
        LoadUnits();
        LoadPictures();
    }

    private static void LoadUnits()
    {
        units = ContentLoader.ReturnListOfType<Unit>(ContentConstValues.FOLDER_UNITS, ContentConstValues.EXTENSION_UNIT);
        units = units
            .OrderBy(x => x.order)
            .ThenBy(x => x.unitID)
            .ToList();
    }

    private static void LoadPictures()
    {
        pictures.Clear();

        for (int i = 0; i < units.Count; i++)
        {
            Unit unit = units[i];
            AddPictures(ContentLoader.ReturnPictures(
                ContentPaths.UnitRoot(unit.unitID),
                new List<string> { unit.unitID },
                "png",
                false));
        }
    }

    private static void AddPictures(Dictionary<string, Texture2D> newPictures)
    {
        foreach (KeyValuePair<string, Texture2D> picture in newPictures)
        {
            pictures[picture.Key] = picture.Value;
        }
    }
}
