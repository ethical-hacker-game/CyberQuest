using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class UnreadEmailsIndicatorUI : MonoBehaviour
{
    [SerializeField] private GameObject indicatorObject;
    [SerializeField] private TextMeshProUGUI countLabel;

    private void UpdateIndicator()
    {
        if(PlayerController.PlayerInfo == null)
        {
            indicatorObject.SetActiveOptimized(false);
            return;
        }

        if (PlayerController.Instance == null)
        {
            indicatorObject.SetActiveOptimized(false);
            return;
        }

        if (PlayerController.Instance.ActiveUnit == null)
        {
            indicatorObject.SetActiveOptimized(false);
            return;
        }

        if(PlayerController.Instance.ActiveUnit.info.mails.IsNullOrEmpty())
        {
            indicatorObject.SetActiveOptimized(false);
            return;
        }

        if (PlayerController.Instance.ActiveUnit.info.HasUnreadEmails == false)
        {
            indicatorObject.SetActiveOptimized(false);
            return;
        }

        indicatorObject.SetActiveOptimized(true);
        countLabel.text = PlayerController.Instance.ActiveUnit.info.UnreadEmailsCount.ToString();
    }

    private void Start()
    {
        MailsManager.Instance.OnEmailRecived.AddListener(UpdateIndicator);
    }

    private void OnEnable()
    {
        if (Application.isPlaying == false)
            return;

        UpdateIndicator();
    }
}
