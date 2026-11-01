using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;

namespace Minigames
{
    public class NetworkMinigame : MinigameBehaviour
    {
        private static NetworkMinigame minigameInstance;

        [SerializeField, FoldoutGroup("Prefabs & Transforms")] private GameObject nodePrefab;
        [SerializeField, FoldoutGroup("Prefabs & Transforms")] private GameObject redNodePrefab;

        [Space]
        [SerializeField, FoldoutGroup("Prefabs & Transforms")] private Transform spawnPointsHolder;
        [SerializeField, FoldoutGroup("Prefabs & Transforms")] private Transform spawnAreasHolder;
        [SerializeField, FoldoutGroup("Prefabs & Transforms")] private Transform nodesHolder;
        [SerializeField, FoldoutGroup("Prefabs & Transforms")] private Transform redAreasHolder;
        [SerializeField, FoldoutGroup("Prefabs & Transforms")] private Transform greenAreasHolder;

        [SerializeField, FoldoutGroup("Minigame General Settings")] private Camera minigameCamera;
        [SerializeField, FoldoutGroup("Minigame General Settings")] private float maxRedAreaSize = 10f;

        [SerializeField, FoldoutGroup("Line Settings")] private LineRenderer lines;
        [SerializeField, FoldoutGroup("Line Settings")] private LineRenderer choiseLine;
        [SerializeField, FoldoutGroup("Line Settings")] private float lineSpeed = 0.3f;
        [SerializeField, FoldoutGroup("Line Settings")] private float fillingSpeed = 1;

        [SerializeField, FoldoutGroup("Background")] private Transform backgroundDotsHolder;
        [SerializeField, FoldoutGroup("Background")] private Transform backgroundDotPrefab;
        [SerializeField, FoldoutGroup("Background")] private Vector2 backgroundDotsArea;
        [SerializeField, FoldoutGroup("Background")] private int backgroundDotsCount = 1000;
        [SerializeField, FoldoutGroup("Background")] private Gradient backgroundElementsColorGradient;
        [SerializeField, FoldoutGroup("Background")] private MinMaxRange backgroundElementsSizeRange;
        [SerializeField, FoldoutGroup("Background")] private float backgroundElementsTextureSpeed = 1;


        [Header("Network Distance Variant Settings")]
        [SerializeField, ShowIf("NetworkDistance")]
        private float maxGreenAreaSize = 20f;
        [SerializeField, ShowIf("NetworkDistance")]
        private float integrityChangeTime = 5;
        [SerializeField, ShowIf("NetworkDistance")]
        private float minDistance = 10f;
        [SerializeField, ShowIf("NetworkDistance")]
        private float maxDistance = 100f;

        [Header("Network Devices Variant Settings")]
        [SerializeField, ShowIf("NetworkDevices")]
        private int devicesToHack = 10000;
        [SerializeField, ShowIf("NetworkDevices")]
        private MinMaxRange devicesPerNode = new MinMaxRange(50, 750);

        [Header("Network Trace Variant Settings")]
        [SerializeField, ShowIf("NetworkTrace")]
        private int reconMovesToWin = 15;
        [SerializeField, ShowIf("NetworkTrace")] 
        private Transform leftovers;
        [SerializeField, ShowIf("NetworkTrace")] 
        private LineRenderer traceLine;

        private NetworkMinigame_Node mainNode;
        private List<NetworkMinigame_Node> sateliteNodes = new List<NetworkMinigame_Node>();

        private int highestNodeIndex = 0;

        private Vector2 lineOffset;
        private int selectedNode = -1;
        private float chosingProgress = 0f;

        private float redAreaDelay = 0f;

        private float multiplier = 1;

        private int activeGreenAreaIndex = -1;
        private float dataIntegrity = 1f;

        private float distanceScoreTimer = 0f;
        private float distanceScoreInterval = 3f;
        private int distanceScoreChange = -33;

        private float hackedDevices = 0;

        private float reconMoves = 0; 

        public bool UpdateDistanceAvailable { get; set; } = true;
        public float MaxGreenAreaSize => maxGreenAreaSize;
        public float MaxRedAreaSize => maxRedAreaSize;
        public NetworkMinigame_Node MainNode => mainNode;
        public Camera MinigameCamera => minigameCamera;

        public Gradient BackgroundElementsColorGradient => backgroundElementsColorGradient;
        public MinMaxRange BackgroundElementsSizeRange => backgroundElementsSizeRange;
        public float BackgroundElementsTextureSpeed => backgroundElementsTextureSpeed;

        public List<NetworkMinigame_RedArea> AttackingRedAreas { get; set; } = new List<NetworkMinigame_RedArea>();

        public float TraceDistance01 => 1 - Mathf.Clamp01((GetDistanceToGreenArea - minDistance) / maxDistance);
        public float DataIntegrity01
        {
            get => dataIntegrity;
            set
            {
                dataIntegrity = Mathf.Clamp01(value);
            }
        }
        public float HackedDevices01 => Mathf.Clamp01(hackedDevices / devicesToHack);
        public float ReconMoves01 => Mathf.Clamp01(reconMoves / reconMovesToWin);


        public float GetDistanceToGreenArea
        {
            get
            {
                if (activeGreenAreaIndex != -1)
                {
                    Transform tempGreen = greenAreasHolder.GetChild(activeGreenAreaIndex).transform;
                    float tempValue = Vector3.Distance(tempGreen.position, mainNode.transform.position);

                    if (tempValue < 0)
                        tempValue = 0;

                    return tempValue;
                }

                return -1;
            }
        }

        public static NetworkMinigame MinigameInstance
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

        public bool NetworkDistance => MinigameType == MinigamesController.MinigameType.NetworkDistance;
        public bool NetworkDevices => MinigameType == MinigamesController.MinigameType.NetworkDevices;
        public bool NetworkTrace => MinigameType == MinigamesController.MinigameType.NetworkTrace;

        public void HackNode(NetworkMinigame_Node nodeToHack)
        {
            if (selectedNode == -1)
                selectedNode = sateliteNodes.IndexOf(nodeToHack);
        }

        public void GreenAreaWin()
        {
            PauseMinigame();
            FinishMinigame(true, Score, Summary);
            DestroyAllRedAreas();
        }

        public override void ApplyConfig(TaskMinigameVariant config)
        {
            // Runs after Awake but before Start/Update, so the win-condition goals and
            // threat values are re-tuned before any of them are read during play.
            base.ApplyConfig(config);

            ApplyDifficulty();

            if (config != null && config.networkSettings != null)
            {
                if (config.networkSettings.devicesToHack > 0)
                    devicesToHack = config.networkSettings.devicesToHack;

                if (config.networkSettings.reconMovesToWin > 0)
                    reconMovesToWin = config.networkSettings.reconMovesToWin;
            }
        }

        private void ApplyDifficulty()
        {
            if (Difficulty == MinigameDifficulty.Default)
                return;

            float goalMultiplier;       // higher = longer objective
            float redSizeMultiplier;    // higher = larger attack areas
            float integrityTimeMultiplier; // higher = slower data-integrity drain (easier)

            switch (Difficulty)
            {
                case MinigameDifficulty.Easy:
                    goalMultiplier = 0.6f;
                    redSizeMultiplier = 0.8f;
                    integrityTimeMultiplier = 1.4f;
                    break;
                case MinigameDifficulty.Hard:
                    goalMultiplier = 1.5f;
                    redSizeMultiplier = 1.3f;
                    integrityTimeMultiplier = 0.7f;
                    break;
                default: // Normal
                    goalMultiplier = 1f;
                    redSizeMultiplier = 1f;
                    integrityTimeMultiplier = 1f;
                    break;
            }

            devicesToHack = Mathf.Max(1, Mathf.RoundToInt(devicesToHack * goalMultiplier));
            reconMovesToWin = Mathf.Max(1, Mathf.RoundToInt(reconMovesToWin * goalMultiplier));
            maxRedAreaSize *= redSizeMultiplier;
            integrityChangeTime *= integrityTimeMultiplier;
        }

        private void Awake()
        {
            minigameInstance = this;
            lines.transform.position = Vector3.zero;
            choiseLine.transform.position = Vector3.zero;
            traceLine.transform.position = Vector3.zero;
            spawnPointsHolder.gameObject.SetActiveOptimized(false);
            spawnAreasHolder.gameObject.SetActiveOptimized(false);
            traceLine.gameObject.SetActiveOptimized(MinigameType != MinigamesController.MinigameType.NetworkDevices);

            ArrangeSpawnNodes();
        }

        protected override void Start()
        {
            if (NetworkDistance)
                Score = 1000;

            base.Start();

            PopulateBackground();

            mainNode = Instantiate(nodePrefab, nodesHolder).GetComponent<NetworkMinigame_Node>();
            mainNode.transform.localPosition = Vector3.zero;

            traceLine.positionCount = 1;
            traceLine.SetPosition(0, mainNode.transform.position);

            SpawnNodesAround();
            //SpawnNodesRandomized_Old();

            for (int i = 0; i < greenAreasHolder.childCount; i++)
            {
                greenAreasHolder.GetChild(i).gameObject.SetActiveOptimized(false);
            }

            if (NetworkDistance)
            {
                activeGreenAreaIndex = Random.Range(0, greenAreasHolder.childCount - 1);
                greenAreasHolder.GetChild(activeGreenAreaIndex).gameObject.SetActiveOptimized(true);
            }
        }

        private void Update()
        {
            UpdateLines();
            UpdateRedAreas();

            if (selectedNode >= 0)
                UpdateChoiseLine();

            if (DataIntegrity01 <= float.Epsilon)
            {
                PauseMinigame();
                FinishMinigame(false, Score, Summary);
            }

            if(NetworkDistance)
                UpdateNetworkDistanceScore();
        }

        private void LateUpdate()
        {
            if (AttackingRedAreas.Count > 0)
            {
                DataIntegrity01 -= Time.deltaTime / integrityChangeTime;
            }
            else
            {
                DataIntegrity01 += Time.deltaTime / integrityChangeTime;
            }
        }

        private void OnDestroy()
        {
            minigameInstance = null;
        }

        private void ArrangeSpawnNodes()
        {
            int childCount = spawnPointsHolder.childCount;
            if (childCount == 0) return;

            float angleStep = 360f / childCount;

            for (int i = 0; i < childCount; i++)
            {
                Transform child = spawnPointsHolder.GetChild(i);

                float angle = (angleStep * i) * Mathf.Deg2Rad;
                float radius = 2.5f;
                Vector3 localPos = new Vector3(
                    Mathf.Cos(angle) * radius,
                    child.localPosition.y,
                    Mathf.Sin(angle) * radius
                );

                child.localPosition = localPos;
            }
        }

        private void PopulateBackground()
        {
            for (int i = 0; i < backgroundDotsCount; i++)
            {
                Vector3 randomPosition = new Vector3(
                    Random.Range(-backgroundDotsArea.x, backgroundDotsArea.x ),
                    0,
                    Random.Range(-backgroundDotsArea.y, backgroundDotsArea.y));
                Instantiate(backgroundDotPrefab, backgroundDotsHolder).transform.localPosition = randomPosition;
            }
        }

        private void SpawnNodesAround()
        {
            sateliteNodes.Clear();
            for (int i = 0; i < spawnPointsHolder.childCount; i++)
            {
                NetworkMinigame_Node newNode = Instantiate(nodePrefab, spawnPointsHolder.GetChild(i).position,
                    Quaternion.identity, nodesHolder).GetComponent<NetworkMinigame_Node>();

                newNode.NodeIndex = ++highestNodeIndex;
                newNode.DevicesCount = NetworkDevices ? (int)devicesPerNode.Random : 0;
                sateliteNodes.Add(newNode);
            }
        }

        private void SpawnNodesRandomized_Old()
        {
            Debug.LogError("This is old method, do not use it");
            return;

            int nodesCount = spawnPointsHolder.childCount;

            List<int> randomNodes = new List<int>();

            sateliteNodes.Clear();

            for (int i = 0; i < nodesCount; i++)
            {
                int tempNode;

                //do
                //{
                    tempNode = Random.Range(0, spawnPointsHolder.GetChild(i).childCount);
                //}
                //while (randomNodes.Contains(tempNode));

                randomNodes.Add(tempNode);
            }

            for (int i = 0; i < randomNodes.Count; i++)
            {
                NetworkMinigame_Node newNode = Instantiate(nodePrefab,
                    spawnPointsHolder.GetChild(i).GetChild(randomNodes[i]).position,
                    Quaternion.identity, nodesHolder).GetComponent<NetworkMinigame_Node>();

                newNode.NodeIndex = ++highestNodeIndex;
                newNode.DevicesCount = NetworkDevices ? (int)devicesPerNode.Random : 0;
                sateliteNodes.Add(newNode);
            }

        }

        private void UpdateLines()
        {
            lines.positionCount = sateliteNodes.Count * 4;

            lineOffset = lines.material.mainTextureOffset;
            lineOffset.x += Time.deltaTime * lineSpeed;
            lines.material.mainTextureOffset = lineOffset;

            for (int i = 0; i < sateliteNodes.Count; i++)
            {
                lines.SetPosition(i * 4, mainNode.transform.position);
                lines.SetPosition(i * 4 + 1, sateliteNodes[i].transform.position);
                lines.SetPosition(i * 4 + 2, sateliteNodes[i].transform.position);
                lines.SetPosition(i * 4 + 3, mainNode.transform.position);
            }
        }

        private void UpdateChoiseLine()
        {
            Vector3 fillPosition = new Vector3();

            if (selectedNode != -1)
            {
                chosingProgress += Time.deltaTime * fillingSpeed * multiplier;
                chosingProgress = Mathf.Clamp01(chosingProgress);
                fillPosition = mainNode.transform.position + (sateliteNodes[selectedNode].transform.position - mainNode.transform.position) * chosingProgress;
            }

            choiseLine.SetPosition(0, mainNode.transform.position);
            choiseLine.SetPosition(1, fillPosition);

            if (chosingProgress >= 1)
            {
                if (MinigameType == MinigamesController.MinigameType.NetworkTrace)
                {
                    mainNode.transform.SetParent(leftovers);
                    mainNode.NodeIndex = -2;
                }
                else
                {
                    mainNode.DestroyNode();
                }

                mainNode = sateliteNodes[selectedNode];
                mainNode.BecomeMain();

                traceLine.positionCount += 1;
                traceLine.SetPosition(traceLine.positionCount - 1, mainNode.transform.position);

                UpdateDistanceAvailable = true;

                sateliteNodes.RemoveAt(selectedNode);

                for (int i = 0; i < sateliteNodes.Count; i++)
                {
                    if (MinigameType == MinigamesController.MinigameType.NetworkTrace)
                    {
                        sateliteNodes[i].transform.SetParent(leftovers);
                        sateliteNodes[i].NodeIndex = -2;
                    }
                    else
                    {
                        sateliteNodes[i].DestroyNode();
                    }
                }

                sateliteNodes.Clear();

                spawnPointsHolder.position = mainNode.transform.position;
                spawnAreasHolder.position = mainNode.transform.position;

                selectedNode = -1;
                chosingProgress = 0;

                choiseLine.SetPosition(0, mainNode.transform.position);
                choiseLine.SetPosition(1, mainNode.transform.position);

                hackedDevices += mainNode.DevicesCount;
                reconMoves++;

                if (NetworkDevices && HackedDevices01 >= 1)
                {
                    PauseMinigame();
                    FinishMinigame(true, Score, Summary);
                    DestroyAllRedAreas();
                    return;
                }

                if (NetworkTrace && ReconMoves01 >= 1)
                {
                    PauseMinigame();
                    FinishMinigame(true, Score, Summary);
                    DestroyAllRedAreas();
                    return;
                }

                SpawnNodesAround();
                //SpawnNodesRandomized_Old();
            }
        }

        private void UpdateNetworkDistanceScore()
        {
            if(NetworkDistance == false)
                return;

            if (IsPaused)
                return;

            distanceScoreTimer += Time.deltaTime;

            if(distanceScoreTimer < distanceScoreInterval)
                return;

            distanceScoreTimer = 0f;
            ChangeScore(distanceScoreChange);

        }

        private void UpdateRedAreas()
        {
            if (isPaused)
                return;

            redAreaDelay += Time.deltaTime;

            if (redAreaDelay < 3 && redAreasHolder.childCount > 7)
                return;

            redAreaDelay = 0;

            Instantiate(redNodePrefab, spawnAreasHolder.GetChild(Random.Range(0, spawnAreasHolder.childCount - 1)).position,
                Quaternion.identity, redAreasHolder);
        }

        private void DestroyAllRedAreas()
        {
            for (int i = 0; i < redAreasHolder.childCount; i++)
            {
                redAreasHolder.GetChild(i).GetComponent<NetworkMinigame_RedArea>()?.Destroy();
            }
        }
    }
}
