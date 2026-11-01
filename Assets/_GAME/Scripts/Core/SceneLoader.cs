using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader : GameSystemComponent
{
    public static SceneLoader Instance;

    private AsyncOperation ao;

    public static int Progress { get; private set; }
    public static int GetActiveSceneIndex => SceneManager.GetActiveScene().buildIndex;

    private static bool IsLoading = false;
    private static bool CanActivateLoadedScene = false;

    public const int MAIN_MENU_SCENE_INDEX = 0;
    public const string TOOLS_SCENE_NAME = "DevContentEditorScene";
    public const string GAME_SCENE_NAME = "GlobeScene_vNeon";


    public static string MainMenuSceneName => GetSceneNameByIndex(MAIN_MENU_SCENE_INDEX);
    public static string ActiveSceneName => SceneManager.GetActiveScene().name;
    public static bool IsDevScene => ActiveSceneName.Contains("Dev");

    public static bool IsSceneInBuild(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName))
            return false;

        int sceneCount = SceneManager.sceneCountInBuildSettings;
        for (int i = 0; i < sceneCount; i++)
        {
            string path = SceneUtility.GetScenePathByBuildIndex(i);
            if (Path.GetFileNameWithoutExtension(path) != sceneName)
                continue;

            return SceneUtility.GetBuildIndexByScenePath(path) >= 0;
        }

        return false;
    }

    public static void LoadSceneAsync(int sceneIndex)
    {
        LoadSceneAsync(GetSceneNameByIndex(sceneIndex));
    }

    public static void LoadSceneAsync(string sceneName)
    {
        if (IsLoading == false)
            InitializeLoadingScene(sceneName);

        CanActivateLoadedScene = true;
    }

    private static void InitializeLoadingScene(string sceneName)
    {
        Instance.StartCoroutine(Instance.LoadSceneAsyncCoroutine(sceneName));
        CanActivateLoadedScene = false;
    }

    private IEnumerator LoadSceneAsyncCoroutine(string sceneName)
    {
        if (IsLoading == true)
            Debug.LogError("loading error");

        IsLoading = true;
        CommonUISolver.ShowLoadingUI();

        yield return null;
        yield return null;

        ao = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
        ao.allowSceneActivation = false;

        while (ao.isDone == false)
        {
            Progress = Mathf.Clamp01(ao.progress / 0.9f).Percentage01ToInt();

            if (ao.progress >= 0.9f && !ao.allowSceneActivation && CanActivateLoadedScene)
            {
                ao.allowSceneActivation = true;
                IsLoading = false;
            }

            yield return null;
        }

        //SceneManager.SetActiveScene(SceneManager.GetSceneByName(sceneName));
        //AsyncOperation unloadingScene = SceneManager.UnloadSceneAsync(oldScene, UnloadSceneOptions.None);
        //yield return unloadingScene;
    }

    private static string GetSceneNameByIndex(int sceneIndex)
    {
        string path = SceneUtility.GetScenePathByBuildIndex(sceneIndex);
        string sceneName = Path.GetFileNameWithoutExtension(path);
        return sceneName;
    }

    private void Awake()
    {
        Instance = this;
        Progress = 0;
    }
}
