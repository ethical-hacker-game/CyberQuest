using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WPM;

public class StickerManager : GameSystemComponent
{
    public static StickerManager Instance;

    [SerializeField] private GameObject questStickerPrefab;
    [SerializeField] private GameObject poiStickerPrefab;

    private WorldMapGlobe globe;
    private List<StickerQuest> questStickers = new List<StickerQuest>();
    private List<StickerPOI> poiStickers = new List<StickerPOI>();

    public List<StickerQuest> QuestStickers => questStickers;
    public List<StickerPOI> PoiStickers => poiStickers;

    public StickerQuest SpawnSticker(QuestReferences questRef)
    {
        if(globe == null)
            globe = WorldMapGlobe.instance;

        // Assign a fresh location for this gameplay session, ignoring the authored city index.
        questRef.data.cityIndex = Random.Range(0, globe.cities.Count);

        GameObject newStickerObject = Instantiate(questStickerPrefab);
        StickerQuest newSticker = newStickerObject.GetComponent<StickerQuest>();
        globe.AddMarker(newStickerObject, globe.cities[questRef.data.cityIndex].localPosition, 0.0001f, true);
        questStickers.Add(newSticker);
        newSticker.SetupSticker(questRef);
        newSticker.UpdateSticker();
        return newSticker;
    }

    public StickerPOI SpawnSticker(POI_Place poi_place)
    {
        if (globe == null)
            globe = WorldMapGlobe.instance;

        GameObject newStickerObject = Instantiate(poiStickerPrefab);
        StickerPOI newSticker = newStickerObject.GetComponent<StickerPOI>();
        globe.AddMarker(newStickerObject, globe.cities[poi_place.cityIndex].localPosition, 0.0001f, true);
        poiStickers.Add(newSticker);
        newSticker.SetupSticker(poi_place);
        return newSticker;
    }

    public void UpdateAllQuestStickers()
    {
        for (int i = 0; i < QuestStickers.Count; i++)
        {
            QuestStickers[i].UpdateSticker();
        }
    }

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        globe = WorldMapGlobe.instance;
    }
}
