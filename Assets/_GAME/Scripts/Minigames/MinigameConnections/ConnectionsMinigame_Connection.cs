using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace Minigames
{
    public class ConnectionsMinigame_Connection : MonoBehaviour
    {
        [SerializeField] private int rewardPointsOnClick = 77;
        [SerializeField] private int rewardPointsOnPass = 11;
        [SerializeField] private int penaltyPointsOnClick = -33;
        [SerializeField] private int penaltyPointsOnMiss = -55;

        [Space]
        [SerializeField] private float initialScale = 1.2f;
        [SerializeField] private int initialStrength = 3;
        [SerializeField] private float scallingSppeed = 3;
        [SerializeField] private float disconnectionSpeed = 8;

        [Space]
        [SerializeField] private List<string> positiveConnections;
        [SerializeField] private List<string> negativeConnections;

        [Space]
        [SerializeField] private Color positiveColor = Color.green;
        [SerializeField] private Color negativeColor = Color.red;

        [Space]
        [SerializeField] private Gradient positiveGradient;
        [SerializeField] private Gradient negativeGradient;

        [Space]
        [SerializeField] private TextMeshProUGUI connectionTMP;

        public List<string> PositiveConnections { get => positiveConnections; }
        public List<string> NegativeConnections { get => negativeConnections; }

        /// <summary>
        /// Overrides the traffic pools with task-authored lists. Empty/null lists are
        /// ignored so the prefab defaults survive when a task only overrides one side.
        /// </summary>
        public void SetConnections(List<string> positive, List<string> negative)
        {
            if (positive.IsNullOrEmpty() == false)
                positiveConnections = positive;

            if (negative.IsNullOrEmpty() == false)
                negativeConnections = negative;
        }

        public Color PositiveColor { get => positiveColor; }
        public Color NegativeColor { get => negativeColor; }

        private float initialDistance;
        private bool isPositiveConnection;
        private int currentStrength;
        private bool destroing = false;
        private bool playerClicked = false;
        private Gradient currentGradient;

        private void Start()
        {
            currentStrength = initialStrength;

            isPositiveConnection = (Random.Range(0, 2) == 0);

            if (isPositiveConnection)
            {
                connectionTMP.text = positiveConnections.GetRandom();
                currentGradient = positiveGradient;
            }
            else
            {
                connectionTMP.text = negativeConnections.GetRandom();
                currentGradient = negativeGradient;
            }

            transform.localEulerAngles= -transform.parent.localEulerAngles;
            initialDistance = transform.localPosition.x;
        }

        private void Update()
        {
            float newScale = Mathf.MoveTowards(transform.localScale.x, initialScale / initialStrength * currentStrength, Time.deltaTime * scallingSppeed);
            transform.localScale = new Vector3(newScale, newScale, newScale);

            if (transform.localPosition.x > 0)
            {
                float direction = playerClicked ? -disconnectionSpeed : 1;
                transform.localPosition = new Vector3(transform.localPosition.x - Time.deltaTime * direction * ConnectionsMinigame.MinigameInstance.ConnectionSpeed, 0, 0);

                if (playerClicked == false)
                {
                    Color targetColor = currentGradient.Evaluate(1 - (transform.localPosition.x / initialDistance));
                    connectionTMP.color = targetColor;
                }

                if (!destroing && transform.localPosition.x < ConnectionsMinigame.MinigameInstance.ConnectionAttackDistance)
                {
                    if (isPositiveConnection == false)
                        ConnectionsMinigame.MinigameInstance.AttackCenter(true);

                    int scoreChange = isPositiveConnection ? rewardPointsOnPass : penaltyPointsOnMiss;
                    ConnectionsMinigame.MinigameInstance.ChangeScore(scoreChange);

                    currentStrength = 0;
                    destroing = true;
                    Destroy(transform.parent.gameObject, 1);
                }
            }
            else
            {
                Destroy(transform.parent.gameObject);
            }

        }

        private void OnMouseDown()
        {
            currentStrength--;

            if (currentStrength > 0)
                return;

            currentStrength = 0;

            if (destroing)
                return;

            destroing = true;
            playerClicked = true;
            Destroy(transform.parent.gameObject, 1);
            connectionTMP.color = isPositiveConnection ? positiveColor : negativeColor;

            if (isPositiveConnection)
            {
                ConnectionsMinigame.MinigameInstance.ChangeScore(penaltyPointsOnClick);
                ConnectionsMinigame.MinigameInstance.AttackCenter(false);
                ConnectionsMinigame.MinigameInstance.SpawnOutput(false, transform.position);
            }
            else
            {
                //((ConnectionsMinigameUI)MinigamesUIManager.Instance.ActiveMinigameUI).ShowVignette(true, 0.3f);
                ConnectionsMinigame.MinigameInstance.ChangeScore(rewardPointsOnClick);
                ConnectionsMinigame.MinigameInstance.SpawnOutput(true, transform.position);
            }

            //if (!MinigamesController.Instance.ShowingResult)
            //    SoundSystem.Instance.PlayActionSound();
        }
    }

}