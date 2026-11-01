using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

public class FullScreenControlPanel : MonoBehaviour
{
    private int currentFullScreenMode = 0;

    public void ChangeFullScreen()
    {
#if UNITY_EDITOR
        return;
#endif

        currentFullScreenMode++;

        if (currentFullScreenMode >= 4)
            currentFullScreenMode = 0;

        switch(currentFullScreenMode)
        {
            case 0:
                Screen.fullScreenMode = FullScreenMode.ExclusiveFullScreen;
                break;

            case 1:
                Screen.fullScreenMode = FullScreenMode.FullScreenWindow;
                break;

            case 2:
                Screen.fullScreenMode = FullScreenMode.MaximizedWindow;
                break;

            case 3:
                Screen.fullScreenMode = FullScreenMode.Windowed;
                break;

            default:
                Screen.fullScreenMode = FullScreenMode.Windowed;
                break;
            }
        }
}
