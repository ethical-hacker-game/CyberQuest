using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Minigames
{
    public class MinigameResultUI : UI_Screen
    {
        [SerializeField] private Image resultPanel;
        [SerializeField] private CanvasGroup resultCanvasGroup;
        [Space]
        [SerializeField] private TextMeshProUGUI successTMP;
        [SerializeField] private TextMeshProUGUI defeatTMP;
        [Space]
        [SerializeField] private GameObject summarySection;
        [SerializeField] private TextMeshProUGUI scoreTMP;
        [SerializeField] private TextMeshProUGUI summaryTMP;

        public void ShowResult(bool result, int score, string summary)
        {
            UpdateResultColors(0, 0);
            gameObject.SetActiveOptimized(true);
            resultPanel.gameObject.SetActive(true);

            summaryTMP.SetText("Summary:\n\n" + summary);
            
            if (result)
            {
                successTMP.gameObject.SetActive(true);
                scoreTMP.SetText($"Arcade score:\n<b>{score}</b>");
            }
            else
            {
                defeatTMP.gameObject.SetActive(true);
                scoreTMP.SetText(string.Empty);
            }

            summarySection.SetActiveOptimized(!summary.IsNullOrEmpty());
        }

        public void HideResult()
        {
            gameObject.SetActiveOptimized(false);
            resultPanel.gameObject.SetActive(false);
            successTMP.gameObject.SetActive(false);
            defeatTMP.gameObject.SetActive(false);
        }

        public void UpdateResultColors(float panelAlpha, float textAlpha)
        {
            //resultPanel.color = new Color(resultPanel.color.r, resultPanel.color.g, resultPanel.color.b, panelAlpha);
            //successTMP.color = new Color(successTMP.color.r, successTMP.color.g, successTMP.color.b, textAlpha);
            //defeatTMP.color = new Color(defeatTMP.color.r, defeatTMP.color.g, defeatTMP.color.b, textAlpha);
            resultCanvasGroup.alpha = panelAlpha;
        }
        
        public void CloseMinigame()
        {
            MinigamesController.Instance.CloseMinigame();
        }
    }
}
