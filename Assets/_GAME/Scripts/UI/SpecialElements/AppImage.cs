using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class AppImage : MonoBehaviour, IQuestActionContinue
{
    [SerializeField] private bool devUsage = false;
    [SerializeField, ShowIf(nameof(devUsage))] private GameObject linkObjectPrefab;
    [SerializeField, ShowIf(nameof(devUsage))] private string scenarioID;

    private Image image;
    private RectTransform linksParent;

    private AppReferences appReferences;
    private AppScenario appScenario;

    private bool stageInProgress = false;
    private int currentFrame = 0;
    private float frameTimer = 0f;
    private bool eventsAssigned = false;

    private List<AppScenario.Stage> loadedStages = new List<AppScenario.Stage>();

    public AppScenario.Stage ActiveStage { get; private set; }
    public UnityEvent OnStageCompleted { get; private set; } = new UnityEvent();
    public UnityEvent<AppScenario.Stage> OnStageChanged { get; private set; } = new UnityEvent<AppScenario.Stage>();

    [Button("Initiate"), ShowIf(nameof(devUsage))]
    private void Initiate()
    {
        if (string.IsNullOrEmpty(scenarioID))
        {
            Debug.LogError("Scenario ID is not set.");
            return;
        }
        appScenario = AppsManager.Instance.GetAppScenarioByID(scenarioID);
        if (appScenario == null)
        {
            Debug.LogError($"AppScenario with ID '{scenarioID}' not found.");
            return;
        }
        SetupAppImage(appScenario);
    }

    public void SetupAppImage(AppReferences appReferences)
    {
        this.appReferences = appReferences;
        SetupAppImage(appReferences.appScenario);
    }

    public void SetupAppImage(AppScenario appScenario)
    {
        if (Application.isPlaying == false)
            return;

        image = GetComponent<Image>();
        this.appScenario = appScenario;

        if (appScenario.stages.Count == 0)
        {
            Debug.LogError($"AppScenario with ID '{scenarioID}' has no stages.");
            return;
        }

        StartStage(appScenario.stages[0].stageID);
        image.rectTransform.sizeDelta = appScenario.size;
    }

    public void StartStage(string stageID)
    {
        if (stageInProgress)
            return;

        ClearLinkObjects();

        if (stageID == "exit" || stageID == "quit")
        {
            ActiveStage = null;
            OnStageChanged.Invoke(null);
            UnloadAllStageImages();
            return;
        }

        ActiveStage = appScenario.GetStageByID(stageID);
        OnStageChanged.Invoke(ActiveStage);

        UnloadAllStageImages(ActiveStage);
        LoadStageImages(ActiveStage);

        if(ActiveStage == null)
        {
            Debug.LogError($"Next stage with ID '{stageID}' not found in scenario '{scenarioID}'.");
            return;
        }

        stageInProgress = true;
        currentFrame = 0;

        if (ActiveStage.frames.Count == 0)
        {
            Debug.LogError($"Active stage '{ActiveStage.stageID}' has no images.");
            return;
        }

        image.sprite = ActiveStage.frames[0].Image;

        if (appReferences != null)
            if (appReferences.info != null)
                appReferences.info.stages.Add(stageID);
    }

    public void ForceStop()
    {
        stageInProgress = false;
        currentFrame = 0;
        frameTimer = 0f;
        ClearLinkObjects();
    }

    public void ShowLastFrame()
    {
        ClearLinkObjects();
        image.sprite = ActiveStage.frames.Last().Image;
        currentFrame = 0;
        frameTimer = 0f;
        stageInProgress = false;
        SetupLinkObjects();
        OnStageCompleted.Invoke();
    }

    public void ShowFrame(int frameIndex)
    {
        if (frameIndex < 0 || frameIndex >= ActiveStage.frames.Count)
        {
            Debug.LogError($"Frame index {frameIndex} is out of bounds for stage '{ActiveStage.stageID}'.");
            return;
        }

        ForceStop();

        image.sprite = ActiveStage.frames[frameIndex].Image;
        currentFrame = frameIndex;
    }

    public void Continue()
    {
        if (linksParent != null)
            linksParent.gameObject.SetActiveOptimized(true);
    }

    private void Awake()
    {
        linksParent = new GameObject("Links").AddComponent<RectTransform>();
        linksParent.SetParent(transform);

        linksParent.anchorMin = Vector2.zero;
        linksParent.anchorMax = Vector2.one;
        linksParent.offsetMin = Vector2.zero;
        linksParent.offsetMax = Vector2.zero;
        linksParent.pivot = new Vector2(0.5f, 0.5f);
        linksParent.localScale = Vector3.one;
        linksParent.localPosition = Vector3.zero;

        if (linkObjectPrefab == null)
            linkObjectPrefab = AppsManager.DefaultLinkPrefab;
    }

    private void Update()
    {
        if (stageInProgress == false)
            return;

        if (ActiveStage == null)
            return;

        if (ActiveStage.frames.Count == 0)
            return;

        if (currentFrame >= ActiveStage.frames.Count)
        {
            stageInProgress = false;
            SetupLinkObjects();
            OnStageCompleted.Invoke();
            return;
        }

        frameTimer += Time.deltaTime;

        if (frameTimer < ActiveStage.frames[currentFrame].delayMs * 0.001f)
            return;

        frameTimer = 0f;

        currentFrame++;

        if (currentFrame < ActiveStage.frames.Count)
            image.sprite = ActiveStage.frames[currentFrame].Image;
    }

    private void OnDestroy()
    {
        UnloadAllStageImages();
    }

    private void OnEnable()
    {
        if(eventsAssigned)
            return;

        if (QuestActionsBridge.Instance == null)
            return;

        eventsAssigned = true;
        OnStageChanged.AddListener(CallStageChangedAction);
        OnStageCompleted.AddListener(CallStageCompletedAction);
    }

    private void OnDisable()
    {
        if(eventsAssigned == false)
            return;

        if (QuestActionsBridge.Instance == null)
            return;

        eventsAssigned = false;
        OnStageChanged.RemoveListener(CallStageChangedAction);
        OnStageCompleted.RemoveListener(CallStageCompletedAction);
    }

    private void CallStageChangedAction(AppScenario.Stage stage)
    {
        if (stage == null)
        {
            QuestActionsBridge.CallQuestAction(this,
                QuestActionsBridge.ACTION_TYPE.app_scenario_finished, "", out _);
            return;
        }

        QuestActionsBridge.CallQuestAction(this, 
            QuestActionsBridge.ACTION_TYPE.app_stage_changed, stage.stageID, out _);
    }

    private void CallStageCompletedAction()
    {
        bool waitForAction = false;

        QuestActionsBridge.CallQuestAction(this,
            QuestActionsBridge.ACTION_TYPE.app_stage_completed, ActiveStage.stageID, out waitForAction);

        if (waitForAction)
            linksParent.gameObject.SetActiveOptimized(false);
    }

    private void SetupLinkObjects()
    {
        linksParent.gameObject.SetActiveOptimized(true);
        CommonUISolver.UIManagerTypes UIManagerType;

        try
        {
            UIManagerType = CommonUISolver.InstanceType;
        }
        catch
        {
            UIManagerType = CommonUISolver.UIManagerTypes.GameplayUIManager;
        }

        if (ActiveStage.links.IsNullOrEmpty())
        {
            if (UIManagerType == CommonUISolver.UIManagerTypes.GameplayUIManager)
                StartStage("exit");
            return;
        }

        for (int i = 0; i < ActiveStage.links.Count; i++)
        {
            AppImageLink appImageLink = Instantiate(linkObjectPrefab, linksParent).GetComponent<AppImageLink>();
            appImageLink.SetupLink(this, ActiveStage.links[i]);
            LoadStageImages(ActiveStage.links[i].nextStageID);

            if (UIManagerType != CommonUISolver.UIManagerTypes.GameplayUIManager)
                appImageLink.DisableInteractivity();
        }

    }

    private void ClearLinkObjects()
    {
        for (int i = linksParent.childCount - 1; i >= 0; i--)
            linksParent.GetChild(i).gameObject.Destroy();

        linksParent.DetachChildren();
    }

    private void LoadStageImages(string stageID)
    {
        AppScenario.Stage stage = appScenario.GetStageByID(stageID);
        if (stage == null)
            return;

        LoadStageImages(stage);
    }

    private void LoadStageImages(AppScenario.Stage stage)
    {
        if (loadedStages.Contains(stage))
            return;

        loadedStages.Add(stage);
        stage.LoadImages();
    }

    private void UnloadStageImages(string stageID)
    {
        AppScenario.Stage stage = appScenario.GetStageByID(stageID);

        if (stage == null)
            return;

        UnloadStageImages(stage);
    }

    private void UnloadStageImages(AppScenario.Stage stage)
    {
        stage.UnloadImages();

        if (loadedStages.Contains(stage) == false)
            return;

        loadedStages.Remove(stage);
    }

    private void UnloadAllStageImages(AppScenario.Stage skipStage = null)
    {
        for (int i = loadedStages.Count - 1; i >= 0; i--)
        {
            if (skipStage != null && loadedStages[i] == skipStage)
                continue;

            loadedStages[i].UnloadImages();
            loadedStages.RemoveAt(i);
        }
    }
}
