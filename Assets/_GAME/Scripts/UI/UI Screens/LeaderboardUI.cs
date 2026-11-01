using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LeaderboardUI : UI_Screen
{
    [SerializeField] private GameObject leaderboardItemPrefab;
    [SerializeField] private Transform leaderboardContent;

    private List<string> playerNames = new List<string> { "John", "Kyle", "Merrill", "Jose", "Dawn", "Scott", "Xin", "Alex", "Chris", "Jordan", "Taylor", "Morgan", "Casey", "Pat", "Jamie", "Skyler", "Blake" };

    public override void Back()
    {
        GameplayUIManager.ShowGameUI();
        base.Back();
    }

    private void Start()
    {
        GenerateFakeScores();
    }

    private void GenerateFakeScores()
    {
        int score = 900;
        float time = 0.7f;
        MinMaxRange scoreDecrement = new MinMaxRange(10, 30);
        MinMaxRange timeIncrement = new MinMaxRange(0.02f, 0.5f);

        for (int i = 1; i <= 30; i++)
        {
            string playerName = playerNames[i % playerNames.Count] + "_" + Random.Range(1,100);

            score -= (int)scoreDecrement.Random;
            time += timeIncrement.Random;

            SpawnLeaderboardItem(i, playerName, score, time);
        }
    }

    private void SpawnLeaderboardItem(int number, string playerName, int score, float time)
    {
        LeaderboardUI_Item item = Instantiate(leaderboardItemPrefab, leaderboardContent).GetComponent<LeaderboardUI_Item>();
        item.SetupLeaderboardItem(number, playerName, score, time);
    }
}
