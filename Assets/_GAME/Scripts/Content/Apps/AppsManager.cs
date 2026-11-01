using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AppsManager : GameSystemComponent
{
    public static AppsManager Instance { get; private set; }

    [SerializeField] private GameObject defaultLinkPrefab;

    private List<AppCore> apps = new List<AppCore>();
    private List<AppScenario> scenarios = new List<AppScenario>();
    
    private List<AppReferences> openedApps = new List<AppReferences>();
    private Dictionary<string, string> availableAppsScenarios = new Dictionary<string, string>();

    public static GameObject DefaultLinkPrefab => Instance.defaultLinkPrefab;
    public List<AppCore> Apps => apps;
    public List<AppScenario> Scenarios => scenarios;
    private Dictionary<string, string> AvailableAppsScenarios
    {
        get
        {
            Dictionary<string, string> allAvailableScenarios = new Dictionary<string, string>(availableAppsScenarios);

            if (PlayerController.Instance == null)
                return allAvailableScenarios;
            
            if(PlayerController.Instance.ActiveQuest == null)
                return allAvailableScenarios;

            string questScenario = PlayerController.Instance.ActiveQuest.AppScenario;

            if(questScenario.IsNullOrEmpty())
                return allAvailableScenarios;

            AppScenario tempScenario = GetAppScenarioByID(questScenario);

            if (tempScenario == null)
                return allAvailableScenarios;

            AppCore tempApp = GetAppByID(tempScenario.appID);

            if(tempApp == null)
                return allAvailableScenarios;

            if (allAvailableScenarios.ContainsKey(tempApp.appID))
                allAvailableScenarios[tempApp.appID] = questScenario;
            else
                allAvailableScenarios.Add(tempApp.appID, questScenario);

            return allAvailableScenarios;
        }

        set 
        { 
            AvailableAppsScenarios = value; 
        }
    }

    public AppCore GetAppByID(string appID)
    {
        for (int i = 0; i < apps.Count; i++)
        {
            if (apps[i].appID == appID)
                return apps[i];
        }

        return null;
    }

    public AppCore GetAppByScenarioID(string scenarioID)
    {
        for (int i = 0; i < scenarios.Count; i++)
        {
            if (scenarios[i].scenarioID == scenarioID)
                return GetAppByID(scenarios[i].appID);
        }
        return null;
    }

    public AppScenario GetAppScenarioByID(string scenarioID)
    {
        for (int i = 0; i < scenarios.Count; i++)
        {
            AppScenario scenario = scenarios[i];

            if (scenario.scenarioID == scenarioID)
                return scenario;
        }

        return null;
    }

    public List<AppScenario> GetAppScenariosByAppCoreIDs(List<string> appIDs)
    {
        List<AppScenario> result = new List<AppScenario>();

        for (int i = 0; i < appIDs.Count; i++)
        {
            result.AddRange(GetAppScenariosByAppCoreID(appIDs[i]));
        }

        return result;
    }

    public List<AppScenario> GetAppScenariosByAppCoreID(string appID)
    {
        List<AppScenario> result = new List<AppScenario>();

        for (int i = 0; i < scenarios.Count; i++)
        {
            if (scenarios[i].appID == appID)
                result.Add(scenarios[i]);
        }

        return result;
    }

    [Button("Add Available Scenario"), ShowIf("@UnityEngine.Application.isPlaying")]
    public void AddAvailableScenario(string appID, string scenarioID)
    {
        if (GetAppByID(appID) == null)
        {
            Debug.LogError($"App with ID {appID} does not exist. Cannot add scenario {scenarioID}.");
            return;
        }

        if (AvailableAppsScenarios.ContainsKey(appID))
        {
            Debug.LogError($"App {appID} already has a scenario. Overwriting with {scenarioID}.");
            return;
        }

        if (GetAppScenarioByID(scenarioID) == null)
        {
            Debug.LogError($"Scenario with ID {scenarioID} does not exist. Cannot add to app {appID}.");
            return;
        }

        AvailableAppsScenarios.Add(appID, scenarioID);
    }

    public OpenAppStatus OpenApp(string appID)
    {
        return OpenApp(appID, out _);
    }

    public OpenAppStatus OpenApp(string appID, out AppReferences appReferences)
    {
        appReferences = null;
        AppCore appCore = GetAppByID(appID);
        AppScenario appScenario;
        string scenarioID;

        if (appCore == null)
        {
            return OpenAppStatus.Error;
        }

        //open already opened app
        if (GetAppReferencesByAppID(appID) != null)
        {
            appReferences = GetAppReferencesByAppID(appID);
            appReferences.appWindow.WindowFlexibilityComponent.MaximizeWindow();

            //just maximize opened app if scenario is correct
            if (appReferences.appScenario != null)
                return OpenAppStatus.AlreadyOpenedWithScenario;

            if(AvailableAppsScenarios.TryGetValue(appID, out scenarioID))
            {
                appScenario = GetAppScenarioByID(scenarioID);

                //add scenario if available
                if (appScenario != null)
                {
                    appReferences.appScenario = appScenario;
                    appReferences.appWindow.SetupAppWindow(appReferences);
                    AddAppInfo(appReferences);
                    return OpenAppStatus.AlreadyOpenedAddedScenario;
                }
            }

            return OpenAppStatus.AlreadyOpenedEmpty;
        }

        //open new app with available scenario
        if (AvailableAppsScenarios.TryGetValue(appID, out scenarioID))
        {
            appScenario = GetAppScenarioByID(scenarioID);

            if (appScenario != null)
            {
                appReferences = new AppReferences
                {
                    appID = appID,
                    appCore = appCore,
                    appScenario = appScenario
                };

                appReferences.appWindow = GameUI.Instance.SpawnAppWindow(appReferences);
                AddAppInfo(appReferences);
                openedApps.Add(appReferences);
                return OpenAppStatus.NewAppWithScenario;
            }
        }

        //open new app without scenario
        appReferences = new AppReferences
        {
            appID = appID,
            appCore = appCore
        };

        appReferences.appWindow = GameUI.Instance.SpawnAppWindow(appReferences);
        openedApps.Add(appReferences);
        return OpenAppStatus.NewAppEmpty;
    }

    public void CloseApp(AppReferences appReferences)
    {
        RemoveAppInfo(appReferences);
        openedApps.Remove(appReferences);
        AvailableAppsScenarios.Remove(appReferences.appID);
    }

    public AppReferences GetAppReferencesByAppID(string appID)
    {
        for (int i = 0; i < openedApps.Count; i++)
        {
            if (openedApps[i].appID == appID)
                return openedApps[i];
        }
        return null;
    }

    public AppReferences GetAppReferencesByScenarioID(string scenarioID)
    {
        for (int i = 0; i < openedApps.Count; i++)
        {
            if (openedApps[i].appScenario == null)
                continue;

            if (openedApps[i].appScenario.scenarioID == scenarioID)
                return openedApps[i];
        }
        return null;
    }

    private void Awake()
    {
        Instance = this;
        LoadApps();
    }

    private void Start()
    {
        RemoveUnusedContent();
        LoadAppImages();
    }

    // Reload apps + scenarios for the currently active unit. Used by the dev editor when
    // a unit is selected, so the apps screen shows only that unit's apps (not every unit's).
    public void ReloadApps()
    {
        LoadApps();
        LoadAppImages();
    }

    private void LoadApps()
    {
        apps.Clear();
        scenarios.Clear();
        string appsPath = ContentPaths.ActiveUnitFolder(ContentConstValues.FOLDER_APPS);

        LoadAppCores(appsPath);
        LoadAppScenarios(appsPath);

        foreach (AppScenario scenario in scenarios)
        {
            AppCore core= GetAppByID(scenario.appID);

            if (core == null)
                Debug.LogError($"App with ID {scenario.appID} not found for scenario {scenario.scenarioID}. Skipping scenario.");
            else
                scenario.AssignAppCore(core);

            scenario.PresetupScenario();
        }
    }

    private void LoadAppCores(string appsPath)
    {
        List<(AppCore value, string directory)> loadedApps =
            ContentLoader.ReturnObjectsWithDirectories<AppCore>(appsPath, ContentConstValues.EXTENSION_APP, false);

        foreach ((AppCore app, string directory) in loadedApps)
        {
            app.AssignContentPath(directory);
            apps.Add(app);
        }
    }

    private void LoadAppScenarios(string appsPath)
    {
        List<(AppScenario value, string directory)> loadedScenarios =
            ContentLoader.ReturnObjectsWithDirectories<AppScenario>(appsPath, ContentConstValues.EXTENSION_APPSCENARIO, false);

        foreach ((AppScenario scenario, string directory) in loadedScenarios)
        {
            scenario.AssignContentPath(directory);
            scenarios.Add(scenario);
        }
    }

    private void RemoveUnusedContent()
    {
        if(CommonUISolver.InstanceType != CommonUISolver.UIManagerTypes.GameplayUIManager)
            return;

        List<string> requiredApps = PlayerController.Instance.ActiveUnit.data.appIDs;

        for (int i = apps.Count - 1; i >= 0; i--)
        {
            if (requiredApps.Contains(apps[i].appID) == false)
                apps.RemoveAt(i);
        }

        for (int i = scenarios.Count - 1; i >= 0; i--)
        {
            if (requiredApps.Contains(scenarios[i].appID) == false)
                scenarios.RemoveAt(i);
        }
    }

    private void LoadAppImages()
    {
        foreach (AppCore app in apps)
        {
            app.LoadImages();
        }
    }

    private void AddAppInfo(AppReferences appRef)
    {
        appRef.info = new AppInfo(appRef.appID, appRef.appScenario.scenarioID);
        PlayerController.Instance.ActiveUnit.info.oppenedApps.Add(appRef.info);
    }

    private void RemoveAppInfo(AppReferences appRef)
    {
        PlayerController.Instance.ActiveUnit.info.oppenedApps.Remove(appRef.info);
        appRef.info = null;
    }

    public enum OpenAppStatus
    {
        NewAppEmpty,
        NewAppWithScenario,
        AlreadyOpenedEmpty,
        AlreadyOpenedWithScenario,
        AlreadyOpenedAddedScenario,
        Error
    }
}
