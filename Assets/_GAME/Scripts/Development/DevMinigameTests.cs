using Minigames;
using Sirenix.OdinInspector;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class DevMinigameTests : MonoBehaviour
{
    [SerializeField] private bool testMode = false;
    [SerializeField, ShowIf("testMode")] private GameObject buttonsObject;
    [SerializeField, ShowIf("testMode")] private Transform buttonsArea;
    [SerializeField, ShowIf("testMode")] private GameObject buttonPrefab;

    [Space]
    [SerializeField] private MinigamesController.MinigameType minigameType = MinigamesController.MinigameType.Connections;

    private bool buttonsPanelActive = true;

    [Button("Start Minigame")]
    private void PlayMinigame()
    {
        if(PlayerController.Instance != null)
            MinigamesController.Instance.SetupMinigame(minigameType);
        else
            MinigamesController.Instance.SetupMinigameTest(minigameType);
    }

    public void OpenEditorScene()
    {
        SceneManager.LoadScene("DevContentEditorScene");
    }

    public void SelectAndStartMinigame(MinigamesController.MinigameType minigameType)
    {
        this.minigameType = minigameType;
        PlayMinigame();
    }

    private void Start()
    {
        if (testMode == false)
            return;


        ButtonExtended button = Instantiate(buttonPrefab, buttonsArea).GetComponent<ButtonExtended>();
        button.TextTMP.text = "Back";
        button.onClick.AddListener(() => OpenEditorScene());

        foreach (MinigamesController.MinigameType minigame in Enum.GetValues(typeof(MinigamesController.MinigameType)))
        {
            if(minigame == MinigamesController.MinigameType.None)
                continue;

            button = Instantiate(buttonPrefab, buttonsArea).GetComponent<ButtonExtended>();
            button.TextTMP.text = minigame.ToString();
            button.onClick.AddListener(() => SelectAndStartMinigame(minigame));
        }

        buttonsPanelActive = buttonsObject.activeSelf;
    }

    private void Update()
    {
        if (testMode == false)
            return;

        if(MinigamesController.isAnyMinigameActive && buttonsPanelActive)
        {
            buttonsPanelActive = false;
            buttonsObject.SetActiveOptimized(false);
        }
        else if(MinigamesController.isAnyMinigameActive == false && buttonsPanelActive == false)
        {
            buttonsPanelActive = true;
            buttonsObject.SetActiveOptimized(true);
        }
    }
}
