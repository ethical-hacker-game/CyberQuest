using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class LogPrompter
{
    public static void ShowError(string message, Object obj = null)
    {
        if (obj == null)
            Debug.LogError(message);
        else
            Debug.LogError(message, obj);

        if (GameplayUIManager.Instance == null)
            return;
        
        if (GameUI.Instance == null)
            return;
        
        GameUI.Instance.SpawnNotificationWindow("Error", message);
    }
}