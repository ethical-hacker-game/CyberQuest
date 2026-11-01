using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class GameUI_QuestElement_Input : GameUI_QuestElementBase
{
    [SerializeField] private TMP_InputField answerInputField;

    public void CheckAnswerWithReturnButtonCheck()
    {
        if (Input.GetKeyDown(KeyCode.Return) == false || Input.GetKey(KeyCode.Return) == false)
            return;

        CheckAnswer();
    }

    public void CheckAnswer()
    {
        CheckAnswer(answerInputField.text);
    }
}
