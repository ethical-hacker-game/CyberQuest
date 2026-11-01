using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Globalization;

public class SD_MoenyLabel : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI moneyLabel;
    [SerializeField] private float changeTime = 2f;

    private int initialMoney = 4000;
    private Color initialColor;

    private int animatedMoney;
    public static int Money { get; private set; }
    private static int lastChange = 100000;

    public static void ChangeMoney(int change)
    {
        Money += change;
        lastChange = Mathf.Abs(change);
    }

    private void Start()
    {
        Money = initialMoney;
        animatedMoney = Money;
        initialColor = moneyLabel.color;
    }

    private void Update()
    {
        if (animatedMoney > Money)
            moneyLabel.color = Color.red;
        else if (animatedMoney < Money)
            moneyLabel.color = Color.green;
        else
            moneyLabel.color = initialColor;

        animatedMoney = (int)Mathf.MoveTowards(animatedMoney, Money, lastChange * changeTime * Time.deltaTime);
        moneyLabel.text = animatedMoney.ToString("C0", CultureInfo.GetCultureInfo("en-US"));
    }
}
