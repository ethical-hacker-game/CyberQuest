using UnityEngine;
using UnityEngine.SceneManagement;

public class CommonUISolver : MonoBehaviour
{
    public static Vector2 CanvasScale
    {
        get
        {
            switch (InstanceType)
            {
                case UIManagerTypes.MainMenuUIManager:
                    return MainMenuUIManager.Instance.CanvasScale;

                case UIManagerTypes.GameplayUIManager:
                    return GameplayUIManager.Instance.CanvasScale;

                case UIManagerTypes.DevEditorUIManager:
                    return DevEditorUIManager.Instance.CanvasScale;
            }

            return Vector2.one;
        }
    }

    public static Vector2 CanvasSize
    {
        get
        {
            switch (InstanceType)
            {
                case UIManagerTypes.MainMenuUIManager:
                    return MainMenuUIManager.Instance.CanvasSize;

                case UIManagerTypes.GameplayUIManager:
                    return GameplayUIManager.Instance.CanvasSize;

                case UIManagerTypes.DevEditorUIManager:
                    return DevEditorUIManager.Instance.CanvasSize;
            }

            return Vector2.one * 1000;
        }
    }

    public static ContentUI ContentUI
    {
        get
        {
            switch (InstanceType)
            {
                case UIManagerTypes.MainMenuUIManager:
                    return MainMenuUIManager.Instance.ContentUI;

                case UIManagerTypes.GameplayUIManager:
                    return GameplayUIManager.Instance.ContentUI;

                case UIManagerTypes.DevEditorUIManager:
                    return DevEditorUIManager.Instance.ContentUI;
            }

            return null;
        }
    }

    public static InputTextUI InputTextUI
    {
        get
        {
            switch (InstanceType)
            {
                case UIManagerTypes.MainMenuUIManager:
                    return MainMenuUIManager.Instance.InputTextUI;

                case UIManagerTypes.GameplayUIManager:
                    return GameplayUIManager.Instance.InputTextUI;

                case UIManagerTypes.DevEditorUIManager:
                    return DevEditorUIManager.Instance.InputTextUI;
            }

            return null;
        }
    }

    public static IndicatorTutorialUI IndicatorTutorialUI
    {
        get
        {
            switch (InstanceType)
            {
                case UIManagerTypes.MainMenuUIManager:
                    return MainMenuUIManager.Instance.IndicatorTutorialUI;

                case UIManagerTypes.GameplayUIManager:
                    return GameplayUIManager.Instance.IndicatorTutorialUI;
            }

            return null;
        }
    }

    public static SettingsUI SettingsUI
    {
        get
        {
            switch (InstanceType)
            {
                case UIManagerTypes.MainMenuUIManager:
                    return MainMenuUIManager.Instance.SettingsUI;

                case UIManagerTypes.GameplayUIManager:
                    return GameplayUIManager.Instance.SettingsUI;

                case UIManagerTypes.DevEditorUIManager:
                    return DevEditorUIManager.Instance.SettingsUI;

            }

            return null;
        }
    }

    public static LoadingUI LoadingUI
    {
        get
        {
            switch (InstanceType)
            {
                case UIManagerTypes.MainMenuUIManager:
                    return MainMenuUIManager.Instance.LoadingUI;

                case UIManagerTypes.GameplayUIManager:
                    return GameplayUIManager.Instance.LoadingUI;

                case UIManagerTypes.DevEditorUIManager:
                    return DevEditorUIManager.Instance.LoadingUI;
            }

            return null;
        }
    }

    public static UI_Screen CurrentUIScreen
    {
        get
        {
            switch (InstanceType)
            {
                case UIManagerTypes.MainMenuUIManager:
                    return MainMenuUIManager.Instance.CurrentUIScreen;

                case UIManagerTypes.GameplayUIManager:
                    return GameplayUIManager.Instance.CurrentUIScreen;

                case UIManagerTypes.DevEditorUIManager:
                    return DevEditorUIManager.Instance.CurrentUIScreen;
            }

            return null;
        }
    }

    public static void ShowContentUI()
    {
        switch (InstanceType)
        {
            case UIManagerTypes.MainMenuUIManager:
                MainMenuUIManager.ShowContentUI();
                break;

            case UIManagerTypes.GameplayUIManager:
                GameplayUIManager.ShowContentUI();
                break;

            case UIManagerTypes.DevEditorUIManager:
                DevEditorUIManager.ShowContentUI();
                break;
        }
    }

    public static void ShowInputTextUI()
    {
        switch (InstanceType)
        {
            case UIManagerTypes.MainMenuUIManager:
                MainMenuUIManager.ShowInputTextUI();
                break;

            case UIManagerTypes.GameplayUIManager:
                GameplayUIManager.ShowInputTextUI();
                break;

            case UIManagerTypes.DevEditorUIManager:
                DevEditorUIManager.ShowInputTextUI();
                break;
        }
    }

    public static void ShowIndicatorTutorialUI()
    {
        switch (InstanceType)
        {
            case UIManagerTypes.MainMenuUIManager:
                MainMenuUIManager.ShowIndicatorTutorialUI();
                break;

            case UIManagerTypes.GameplayUIManager:
                GameplayUIManager.ShowIndicatorTutorialUI();
                break;
        }
    }

    public static void ShowSettingsUI()
    {
        switch (InstanceType)
        {
            case UIManagerTypes.MainMenuUIManager:
                MainMenuUIManager.ShowSettingsUI();
                break;

            case UIManagerTypes.GameplayUIManager:
                GameplayUIManager.ShowSettingsUI();
                break;

            case UIManagerTypes.DevEditorUIManager:
                DevEditorUIManager.ShowSettingsUI();
                break;
        }
    }

    public static void ShowLoadingUI()
    {
        switch (InstanceType)
        {
            case UIManagerTypes.MainMenuUIManager:
                MainMenuUIManager.ShowLoadingUI();
                break;

            case UIManagerTypes.GameplayUIManager:
                GameplayUIManager.ShowLoadingUI();
                break;

            case UIManagerTypes.DevEditorUIManager:
                DevEditorUIManager.ShowLoadingUI();
                break;
        }
    }

    public static void SelectInvisibleButton()
    {
        switch (InstanceType)
        {
            case UIManagerTypes.MainMenuUIManager:
                MainMenuUIManager.SelectInvisibleButton();
                break;

            case UIManagerTypes.GameplayUIManager:
                GameplayUIManager.SelectInvisibleButton();
                break;

            case UIManagerTypes.DevEditorUIManager:
                DevEditorUIManager.SelectInvisibleButton();
                break;
        }
    }

    public static void HideAll()
    {
        switch (InstanceType)
        {
            case UIManagerTypes.MainMenuUIManager:
                MainMenuUIManager.HideAll();
                break;

            case UIManagerTypes.GameplayUIManager:
                GameplayUIManager.HideAll();
                break;

            case UIManagerTypes.DevEditorUIManager:
                DevEditorUIManager.HideAll();
                break;
        }
    }

    public static UIManagerTypes InstanceType
    {
        get
        {
            if (SceneManager.GetActiveScene().name.Contains("Menu"))
                if (MainMenuUIManager.Instance != null)
                    return UIManagerTypes.MainMenuUIManager;

            if (SceneManager.GetActiveScene().name.StartsWith("Dev"))
                if (DevEditorUIManager.Instance != null)
                    return UIManagerTypes.DevEditorUIManager;

            if (GameplayUIManager.Instance != null)
                return UIManagerTypes.GameplayUIManager;

            throw new UnassignedReferenceException("There is no UI Manager!!");
        }
    }

    public enum UIManagerTypes
    {
        MainMenuUIManager,
        GameplayUIManager,
        DevEditorUIManager,
    }
}
