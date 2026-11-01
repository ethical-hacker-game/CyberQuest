using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Minigames
{
    public class ConnectionsMinigame : MinigameBehaviour
    {
        private static ConnectionsMinigame minigameInstance;

        [Header("Settings")]
        [SerializeField] private float connectionSpawnDelay = 3;
        [SerializeField] private float connectionSpawnDistance = 5.5f;
        [SerializeField] private float connectionSpeed = 1;
        [SerializeField] private float connectionAttackDistance = 2;

        [Space]
        [SerializeField] private int standardInitialStrength = 5;
        [SerializeField] private int currentStrength;

        [Header("Objects")]
        [SerializeField] private Transform originalConnectionParent;
        [SerializeField] private ConnectionsMinigame_Connection originalConnection;
        [SerializeField] private Transform connectionsHolder;
        [SerializeField] private GameObject outputGoodObj;
        [SerializeField] private GameObject outputBadObj;
        [SerializeField] private CanvasGroup serverDamageShadow;


        private float delayTimer = 0;
        private bool isActive = true;
        private float currentConnectionSpeed;
        private bool spawnFromTop = true;

        public float ConnectionSpeed
        {
            get
            {
                if (IsPaused)
                    return 0;

                return Mathf.Clamp(currentConnectionSpeed, 0, float.MaxValue);
            }
        }
        public float ConnectionAttackDistance => connectionAttackDistance;

        public float StandardInitialStrength => standardInitialStrength;
        public float CurrentStrength => currentStrength;

        public override string Summary 
        { 
            get 
            {
                string positiveColor = ColorUtility.ToHtmlStringRGB(originalConnection.PositiveColor);
                string tempSummary = $"<b><color=#{positiveColor}>Benign (Expected) Traffic:</b>";

                List<string> posStrings = originalConnection.PositiveConnections;

                for (int i = 0; i < posStrings.Count; i++)
                {
                    tempSummary += $"\n{i+1}. {posStrings[i]}";
                }

                string negativeColor = ColorUtility.ToHtmlStringRGB(originalConnection.NegativeColor);
                tempSummary += "</color>\n\n" +
                    $"<b><color=#{negativeColor}>Malicious (Suspicious) Traffic (Active Recon):</b>";

                List<string> negStrings = originalConnection.NegativeConnections;

                for (int i = 0; i < negStrings.Count; i++)
                {
                    tempSummary += $"\n{i + 1}. {negStrings[i]}";
                }

                return tempSummary;
            } 
        }

        private float MinigameTimer => MinigamesController.Instance.Timer;


        public static ConnectionsMinigame MinigameInstance
        {
            get
            {
                if (minigameInstance == null)
                {
                    UnityEngine.Debug.LogError("minigameInstance does not exist");
                }

                return minigameInstance;
            }
        }

        public int GetRealInitialStrenght
        {
            get
            {
                //if (SkillsManager.Instance.GetSkillProgress("def2") == 1f)
                //    return standardInitialStrength * 2;

                return standardInitialStrength;
            }
        }

        public void AttackCenter(bool highlightServer)
        {
            if (!isActive)
                return;

            currentStrength--;
            ((ConnectionsMinigameUI)MinigamesUIManager.Instance.ActiveMinigameUI).ShowVignette(false, 3f);

            if (currentStrength <= 0)
            {
                currentStrength = 0;
                isActive = false;
                FinishMinigame(false, Score, Summary);
            }

            if(highlightServer)
                serverDamageShadow.alpha = 0.8f;

            currentConnectionSpeed = -2;
            //SoundSystem.Instance.PlayAttackSound();
        }

        public void SpawnOutput(bool isGood, Vector3 position)
        {
            GameObject outputObj = isGood ? outputGoodObj : outputBadObj;
            Instantiate(outputObj, position, Quaternion.identity).SetActiveOptimized(true);
        }

        public override void FinishMinigame(bool result, int score, string summary)
        {
            PauseMinigame();
            base.FinishMinigame(result, score, summary);
        }

        public override void ApplyConfig(TaskMinigameVariant config)
        {
            // Sets durationOverride + difficulty. Runs after Awake (which already
            // seeded currentStrength/currentConnectionSpeed) but before Start and
            // before any connection spawns, so re-deriving those values here is safe.
            base.ApplyConfig(config);

            ApplyDifficulty();

            if (config != null && config.connectionsSettings != null)
            {
                originalConnection.SetConnections(
                    config.connectionsSettings.positiveConnections,
                    config.connectionsSettings.negativeConnections);
            }
        }

        private void ApplyDifficulty()
        {
            if (Difficulty == MinigameDifficulty.Default)
                return;

            // Multipliers relative to the prefab (Normal) values.
            float strengthMultiplier;
            float spawnDelayMultiplier;
            float speedMultiplier;

            switch (Difficulty)
            {
                case MinigameDifficulty.Easy:
                    strengthMultiplier = 1.6f;
                    spawnDelayMultiplier = 1.3f;
                    speedMultiplier = 0.8f;
                    break;
                case MinigameDifficulty.Hard:
                    strengthMultiplier = 0.6f;
                    spawnDelayMultiplier = 0.7f;
                    speedMultiplier = 1.35f;
                    break;
                default: // Normal
                    strengthMultiplier = 1f;
                    spawnDelayMultiplier = 1f;
                    speedMultiplier = 1f;
                    break;
            }

            standardInitialStrength = Mathf.Max(1, Mathf.RoundToInt(standardInitialStrength * strengthMultiplier));
            connectionSpawnDelay *= spawnDelayMultiplier;
            connectionSpeed *= speedMultiplier;

            currentStrength = GetRealInitialStrenght;
            currentConnectionSpeed = connectionSpeed;
        }

        private void Awake()
        {
            minigameInstance = this;
            PrepareMinigame();
        }

        private void OnDestroy()
        {
            minigameInstance = null;
        }

        private void PrepareMinigame()
        {
            currentStrength = GetRealInitialStrenght;
            originalConnectionParent.gameObject.SetActive(false);
            currentConnectionSpeed = connectionSpeed;
            serverDamageShadow.alpha = 0;
            spawnFromTop = true;

            for (int i = 0; i < connectionsHolder.childCount; i++)
            {
                Destroy(connectionsHolder.GetChild(0).gameObject);
            }
        }

        void Update()
        {
            if (IsPaused)
                return;

            if (currentConnectionSpeed == connectionSpeed)
                delayTimer -= Time.deltaTime;

            if(currentConnectionSpeed < connectionSpeed)
                currentConnectionSpeed = Mathf.MoveTowards(currentConnectionSpeed, connectionSpeed, Time.deltaTime);

            if(serverDamageShadow.alpha > 0)
                serverDamageShadow.alpha = Mathf.MoveTowards(serverDamageShadow.alpha, 0, Time.deltaTime / 2);

            if (delayTimer <= 0 && MinigameTimer > 4f)
            {
                delayTimer = connectionSpawnDelay;
                SpawnNewConnection();
            }

            if (MinigameTimer <= 0 && isActive)
                isActive = false;
        }

        private void SpawnNewConnection()
        {
            Transform newConnection = Instantiate(originalConnectionParent.gameObject, connectionsHolder).transform;
            // The top-down camera maps world +Z to the top of the screen.
            float newRotation = spawnFromTop ? 270f : 90f;
            spawnFromTop = !spawnFromTop;
            newConnection.eulerAngles = new Vector3(0, newRotation, 0);
            newConnection.gameObject.SetActive(true);
            newConnection.GetChild(0).localPosition = new Vector3(connectionSpawnDistance, 0, 0);
        }
    }
}
