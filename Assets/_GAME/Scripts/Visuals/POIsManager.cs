using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WPM;
using static Quest;

public class POIsManager : GameSystemComponent
{
    public static POIsManager Instance { get; private set; }

    [Header("Line Settings")]
    [SerializeField] private float arcElevetion = 0.2f;
    [SerializeField] private float lineWidth = 0.002f;

    private POIs poisObject;
    private WorldMapGlobe globe;

    private List<POI_Place> places => poisObject.places;
    private List<POI_Line> lines => poisObject.lines;

    public void ClickPOI(POI_Place poi_place) 
    {
        if (places.IsNullOrEmpty())
            return;

        for (int i = 0; i < places.Count; i++)
        {
            if (places[i] != poi_place)
                continue;

            globe.FlyToCity(poi_place.cityIndex, 1);
            //Debug.Log("poi_place.action" + poi_place.action);
            //switch (poi_place.action)
            //{
            //    case POI_Place.ClickAction.ShowNotificationWindow:
            //        GameUI.Instance.SpawnNotificationWindow(poi_place.labelText, poi_place.extraText);
            //        break;
            //    default:
            //        break;
            //}

            if (string.IsNullOrEmpty(poi_place.extraText) == false)
                GameUI.Instance.SpawnNotificationWindow(poi_place.labelText, poi_place.extraText);

            return;
        }
    }

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        globe = WorldMapGlobe.instance;
        LoadPOIs();
        SpawnLines();
        SpawnStickers();
    }

    private void LoadPOIs()
    {
        if (GameController.SelectedUnit == null)
            return;

        string unitID = GameController.SelectedUnit.unitID;
        string poisPath = ContentLoader.CombinePath(
            ContentPaths.UnitFolder(unitID, ContentConstValues.FOLDER_POIS),
            $"pois.{ContentConstValues.EXTENSION_POIS}");

        poisObject = ContentLoader.ReturnObjectOfType<POIs>(poisPath, false);

        if (poisObject == null)
            poisObject = new POIs();
    }

    private void SpawnLines()
    {
        if (lines.IsNullOrEmpty())
            return;

        for (int i = 0; i < lines.Count; i++)
        {
            POI_Line line = lines[i];
            Vector2 startLatLon = globe.GetCityLatLon(line.startCityIndex);
            Vector2 endLatLon = globe.GetCityLatLon(line.endCityIndex);
            //globe.AddLine(startLatLon, endLatLon, line.lineColor, arcElevetion, 0, lineWidth, 0);
            globe.AddLine(startLatLon, endLatLon, new Color(255, 255, 0, 1), arcElevetion, 0, lineWidth, 0);
        }
    }

    private void SpawnStickers()
    {
        if (places.IsNullOrEmpty())
            return;

        for (int i = 0; i < places.Count; i++)
        {
            StickerManager.Instance.SpawnSticker(places[i]);
        }
    }
}

[Serializable]
public class POIs : IValidateContent
{
    [RuntimeInspectorNamespace.LockedField]
    public string unitID;
    public List<POI_Place> places = new List<POI_Place>();
    public List<POI_Line> lines = new List<POI_Line>();

    public bool ValidateContent(out string validationMessage)
    {
        List<string> issues = new List<string>();
        issues.Add("Issues:");

        if (places.IsNullOrEmpty())
            issues.Add("POIs places is empty.");

        if (lines.IsNullOrEmpty())
            issues.Add("POIs lines is empty.");

        validationMessage = string.Join("\n - ", issues);
        validationMessage = "<color=#ffb300>(possible issues)\n" + validationMessage + "</color>";

        return issues.Count <= 1;
    }
}

[Serializable]
public class POI 
{
    public string devNote;
}

[Serializable]
public class POI_Place : POI
{
    public int cityIndex;
    public ClickAction action;
    public string labelText;
    [TextArea] public string extraText;
    [RuntimeInspectorNamespace.LockedField]
    public string poiIconID;

    public Sprite GetIcon()
    {
        return PicturesManager.GetPicture(poiIconID, PicturesManager.PictureType.POI);
    }

    public override string ToString()
    {
        return $"POI_PLACE:\n" +
            $"{devNote}\n" +
            $"{labelText}\n" +
            $"{cityIndex}";
    }

    [Serializable]
    public enum ClickAction
    {
        None,
        ShowNotificationWindow,
    }
}

[Serializable]
public class POI_Line : POI
{
    public int startCityIndex;
    public int endCityIndex;
    //public Color lineColor;

    public override string ToString()
    {
        return $"POI_LINE:\n" +
            $"{devNote}\n" +
            $"{startCityIndex}-{endCityIndex}";
    }
}
