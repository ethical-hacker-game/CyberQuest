using Michsky.UI.Beam;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GameUI_QuestElements : MonoBehaviour
{
    [Header("Quest Areas")]
    [SerializeField] private GameObject questAnswerArea;
    [SerializeField] private GameObject questCompletedArea;

    [Header("Top Panel")]
    [SerializeField] private TextMeshProUGUI headerTMP;
    [SerializeField] private Image iconImage;
    [SerializeField] private ButtonManager articleButton;
    [SerializeField] private List<GameObject> skipElements;

    [Header("Quest UI")]
    [SerializeField] private TextMeshProUGUI descriptionTMP;
    [SerializeField] private TextMeshProUGUI questionLineTMP;
    [SerializeField] private TextMeshProUGUI completeInfoTMP;
    [SerializeField] private TextMeshProUGUI timerTMP;
    [SerializeField] private Image characterImage;
    [SerializeField] private TextMeshProUGUI completeLabelTMP;
    [SerializeField] private ButtonManager nextQuestButton;

    private RectTransform rectTransform;

    public GameObject QuestAnswerArea => questAnswerArea;
    public GameObject QuestCompletedArea => questCompletedArea;

    public ButtonManager ArticleButton => articleButton;
    public List<GameObject> SkipElements => skipElements;

    public TextMeshProUGUI DescriptionTMP => descriptionTMP;
    public TextMeshProUGUI QuestionLineTMP => questionLineTMP;
    public TextMeshProUGUI CompleteInfoTMP => completeInfoTMP;
    public TextMeshProUGUI TimerTMP => timerTMP;
    public Image CharacterImage => characterImage;
    public TextMeshProUGUI CompleteLabelTMP => completeLabelTMP;
    public ButtonManager NextQuestButton => nextQuestButton;

    public RectTransform RectTransform
    {
        get
        {
            if (rectTransform == null)
                rectTransform = GetComponent<RectTransform>();
            return rectTransform;
        }
    }

    public void SetHeader(string header)
    {
        headerTMP.text = header;
    }
    public void SetIcon(Sprite icon)
    {
        iconImage.sprite = icon;
    }
    public void SkipTask()
    {
        GetComponent<GameUI_QuestElementBase>().SkipTask();
    }

    public void NextQuest()
    {
        GetComponent<GameUI_QuestElementBase>().StartNextQuest();
    }
}
