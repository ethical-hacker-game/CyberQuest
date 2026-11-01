using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LeaderboardUI_Item : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI numberTMP;
    [SerializeField] private TextMeshProUGUI playerNameTMP;
    [SerializeField] private TextMeshProUGUI scoreTMP;
    [SerializeField] private TextMeshProUGUI timeTMP;


    public void SetupLeaderboardItem(int number, string playerName, int score, float time)
    {
        numberTMP.text = $"{number}.";
        playerNameTMP.text = playerName;
        scoreTMP.text = score.ToString();
        timeTMP.text = time.MinutesFloatToTimeString();
    }
}
