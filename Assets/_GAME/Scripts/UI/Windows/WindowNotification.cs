using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class WindowNotification : WindowBase
{
    [SerializeField] private TextMeshProUGUI notificationTMP;

    public void SetupWindow(string headerText, string notificationText)
    {
        SetHeader(headerText);
        notificationTMP.text = notificationText;
    }
}
