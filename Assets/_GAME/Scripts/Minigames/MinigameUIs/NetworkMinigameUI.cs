using Minigames;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class NetworkMinigameUI : MinigameUI
{
    [SerializeField] private GameObject distanceElement;
    [SerializeField] private Image distanceBar;
    [SerializeField] private GameObject integrityElement;
    [SerializeField] private Image integrityBar;
    [SerializeField] private GameObject devicesElement;
    [SerializeField] private Image devicesBar;
    [SerializeField] private GameObject reconMovesElement;
    [SerializeField] private Image reconMovesBar;

    private bool NetworkDistance => NetworkMinigame.MinigameInstance.NetworkDistance;
    private bool NetworkDevices => NetworkMinigame.MinigameInstance.NetworkDevices;
    private bool NetworkTrace => NetworkMinigame.MinigameInstance.NetworkTrace;

    private void Start()
    {
        distanceElement.SetActiveOptimized(NetworkDistance);
        integrityElement.SetActiveOptimized(true);
        devicesElement.SetActiveOptimized(NetworkDevices);
        reconMovesElement.SetActiveOptimized(NetworkTrace);

        distanceBar.fillAmount = 0;
        integrityBar.fillAmount = 0;
        devicesBar.fillAmount = 0;
        reconMovesBar.fillAmount = 0;
    }

    public override void UpdateUI()
    {
        if (NetworkMinigame.MinigameInstance == null)
            return;

        float dataIntegrity01 = NetworkMinigame.MinigameInstance.DataIntegrity01;

        if (integrityBar != null)
            integrityBar.fillAmount =
                Mathf.MoveTowards(integrityBar.fillAmount, dataIntegrity01, Time.deltaTime);

        if (NetworkDistance)
            UpdateUI_Distance();
        else if (NetworkDevices)
            UpdateUI_Devices();
        else if (NetworkTrace)
            UpdateUI_ReconMoves();
    }

    private void Update()
    {
        UpdateUI();
    }

    private void UpdateUI_Distance()
    {
        float traceDistance01 = NetworkMinigame.MinigameInstance.TraceDistance01;

        if (distanceBar != null)
            distanceBar.fillAmount =
                Mathf.MoveTowards(distanceBar.fillAmount, traceDistance01, Time.deltaTime);

    }

    private void UpdateUI_Devices()
    {
        float devicesHacked01 = NetworkMinigame.MinigameInstance.HackedDevices01;

        if (devicesBar != null)
            devicesBar.fillAmount =
                Mathf.MoveTowards(devicesBar.fillAmount, devicesHacked01, Time.deltaTime);
    }

    private void UpdateUI_ReconMoves()
    {
        float reconMoves01 = NetworkMinigame.MinigameInstance.ReconMoves01;

        if (reconMovesBar != null)
            reconMovesBar.fillAmount =
                Mathf.MoveTowards(reconMovesBar.fillAmount, reconMoves01, Time.deltaTime);
    }
}
