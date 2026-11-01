using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Minigames
{
    public class MinigameMainUI : UI_Screen
    {
        [SerializeField] private TextMeshProUGUI topLabel;
        [Space]
        [SerializeField] private TextMeshProUGUI timerLabel;
        [SerializeField] private Image timerImage;
        [Space]
        [SerializeField] private TextMeshProUGUI scoreLabel;
        [SerializeField] private float increaseScale = 2;
        [SerializeField] private Color increaseColor = Color.green;
        [SerializeField] private float decreaseScale = 0.5f;
        [SerializeField] private Color decreaseColor = Color.red;
        [SerializeField] private float scoreAnimDuration = 0.3f;

        private Color defaultColor;

        public void Setup(string labelText)
        {
            topLabel.text = labelText;
        }

        public void UpdateTimerLabel(string timeString)
        {
            timerLabel.text = timeString;
        }

        public void UpdateTimerImage(float fillAmount)
        {
            timerImage.fillAmount = fillAmount;
        }

        public void Pause()
        {
            MinigamesController.Instance.ActiveMinigame.PauseMinigame();

            ContentUI.SetupContentUI("Simulation Paused", null, 
                "The simulation is temporarily paused. You can continue from this moment, or abandon the task and leave the simulation.", 
                "Resume Simulation", "Abandon Mission");
            ContentUI.ButtonEvents[0].AddListener(() =>
            {
                GameplayUIManager.HideAll();
                MinigamesController.Instance.ActiveMinigame.ResumeMinigame();
            });
            ContentUI.ButtonEvents[1].AddListener(() =>
            {
                GameplayUIManager.HideAll();
                MinigamesController.Instance.ActiveMinigame.ResumeMinigame();
                MinigamesController.Instance.FinishMinigame(false, 0, MinigamesController.Instance.ActiveMinigame.Summary);
            });
            ContentUI.DefaultButtonIndex = 0;
        }

        public void UpdateScore(int score, bool increase)
        {
            scoreLabel.text = $"Arcade score: {score}";

            if (increase)
            {
                scoreLabel.color = increaseColor;
                scoreLabel.transform.localScale = Vector3.one * increaseScale;
            }
            else
            {
                scoreLabel.color = decreaseColor;
                scoreLabel.transform.localScale = Vector3.one * decreaseScale;
            }
        }

        private void Awake()
        {
            defaultColor = scoreLabel.color;
        }

        private void Update()
        {
            UpdateScoreLabel();
        }

        private void UpdateScoreLabel()
        {
            float adjustedDeltaTIme = Time.deltaTime  / scoreAnimDuration;

            scoreLabel.color = Color.Lerp(scoreLabel.color, defaultColor, adjustedDeltaTIme);
            scoreLabel.transform.localScale = Vector3.Lerp(scoreLabel.transform.localScale, Vector3.one, adjustedDeltaTIme);
        }
    }
}
