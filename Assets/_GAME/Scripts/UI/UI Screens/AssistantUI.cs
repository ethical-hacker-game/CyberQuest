using Michsky.UI.Beam;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

public class AssistantUI : UI_Screen
{
    public static AssistantUI Instance => GameplayUIManager.Instance.AssistantUI;

    [Header("Notifications")]
    [SerializeField] private Transform notificationsParent;
    [SerializeField] private FeedNotification infoNotificationPrefab;
    [SerializeField] private FeedNotification errorNotificationPrefab;

    private float targetAionState = 0;
    private float currentAionState = 0;

    public override void ShowScreen()
    {
        base.ShowScreen();
        FixNotifications();
    }

    public override void HideScreen()
    {
        base.HideScreen();
    }

    public void SpawnInfoNotification(string text)
    {
        SpawnNotification(infoNotificationPrefab, text);
    }

    public void SpawnErrorNotifiaction(string text)
    {
        SpawnNotification(errorNotificationPrefab, text);
    }

    private void SpawnNotification(FeedNotification notificationPrefab, string text)
    {
        FeedNotification notification = Instantiate(notificationPrefab, notificationsParent);
        notification.notificationText = text;
        notification.ExpandNotification();
    }

    private void FixNotifications()
    {
        List<FeedNotification> notifications = notificationsParent.GetComponentsInChildren<FeedNotification>().ToList();
        notifications.ForEach(notification => notification.ExpandNotification());
    }
}
