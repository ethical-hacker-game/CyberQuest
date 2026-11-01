using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class WindowCommandLine : WindowBase
{
    [Space]
    [SerializeField] private TextMeshProUGUI cmdMainTMP;
    [Space]
    [SerializeField] private GameObject inputFieldRow;
    [SerializeField] private TextMeshProUGUI cmdUsernameTMP;
    [SerializeField] private TMP_InputField inputField;
    [Space]
    [SerializeField] private ScrollRect scrollRect;
    [SerializeField] private ContentSizeFitter scrollSizeFitter;
    [SerializeField] private ContentSizeFitter textSizeFitter;
    [Space]
    [SerializeField] private UILayoutRebuilder layoutRebuilder;
    [Space]
    [SerializeField] private float lineDelay = 0.1f;

    private string usernameLineTemplate = "";
    private float delayTimer = 0;
    private Queue<string> linesToShow = new Queue<string>();
    private Command activeTool = null;

    private const string INITIAL_LINE = "Cyber Quest Command Line [Version 0.1]";
    private const string HIDDEN_STRING = "<color=#00000000>-</color>";

    public void EnterCommand()
    {
        inputField.Select();

        if (Input.GetKeyDown(KeyCode.Return) == false || Input.GetKey(KeyCode.Return) == false)
            return;

        string newCommand = inputField.text;

        if (string.IsNullOrWhiteSpace(newCommand))
            return;

        inputField.SetTextWithoutNotify("");

        (string output, Command tool) commandResult = CommandLineManager.GetCommandResult(newCommand, activeTool);
        
        AddLines($"{usernameLineTemplate}{newCommand}\n{commandResult.output}\n{HIDDEN_STRING}");
        activeTool = commandResult.tool;
    }

    public override void ClickClose()
    {
        Clear();
        base.ClickClose();
    }

    public void Clear()
    {
        cmdMainTMP.text = INITIAL_LINE;
        inputField.SetTextWithoutNotify("");
        linesToShow.Clear();

        for (int i = cmdMainTMP.transform.parent.childCount - 3; i >= 0; i--)
        {
            cmdMainTMP.transform.parent.GetChild(i).gameObject.Destroy();
        }
    }

    private void Start()
    {
        usernameLineTemplate = "root > "; //$"{AccountSystem.LoggedUser.username}>";
        cmdMainTMP.text = INITIAL_LINE;
        cmdUsernameTMP.text = usernameLineTemplate;

        StartCoroutine(FixCMD());

        WindowFlexibilityComponent.OnActiveEvent.AddListener(() =>
        {
            inputField.Select();
            inputField.ActivateInputField();
            StartCoroutine(MoveTextEnd_NextFrame());
        });
    }

    private void Update()
    {
        if(linesToShow.Count > 0)
        {
            if (inputFieldRow.activeSelf)
                inputFieldRow.SetActiveOptimized(false);

            delayTimer += Time.deltaTime;

            if(delayTimer > lineDelay)
            {
                delayTimer = 0;
                cmdMainTMP.text += $"\n{linesToShow.Dequeue()}";
                inputField.Select();
                CheckTextLimits();
                StartCoroutine(FixCMD());
            }

        } 
        else if (inputFieldRow.activeSelf == false)
        {
            inputFieldRow.SetActiveOptimized(true);
            StartCoroutine(FixCMD());
        }
    }

    private void CheckTextLimits()
    {
        string tempString = cmdMainTMP.text;
        string[] tempLines = tempString.Split("\\n");

        if (tempLines.Length < 10)
            return;

        cmdMainTMP.text = string.Join("", tempLines, 0, 10);

        GameObject tempNewText = Instantiate(cmdMainTMP.gameObject, cmdMainTMP.transform.parent);
        tempNewText.transform.SetSiblingIndex(cmdMainTMP.transform.GetSiblingIndex() + 1);
        cmdMainTMP = tempNewText.GetComponent<TextMeshProUGUI>();
        textSizeFitter = tempNewText.GetComponent<ContentSizeFitter>();
        cmdMainTMP.text = string.Join("", tempLines, 10, tempLines.Length - 10);

        CheckTextLimits();
    }

    private void AddLines(string lines)
    {
        foreach (string line in lines.Split('\n'))
        {
            linesToShow.Enqueue(line);
        }

        delayTimer = lineDelay;
    }

    private IEnumerator FixCMD()
    {
        layoutRebuilder.ForceRebuild();
        yield return null;
        scrollRect.verticalNormalizedPosition = 0;
        yield return null;
        inputField.Select();
        inputField.ActivateInputField();
    }

    private IEnumerator MoveTextEnd_NextFrame()
    {
        yield return 0;
        inputField.MoveTextEnd(false);
    }
}
