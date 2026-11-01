using System.Collections.Generic;
using UnityEngine;

namespace Minigames
{
    /// <summary>
    /// 3D view for <see cref="RoverMinigame"/>: a top-down arena (camera above, slightly
    /// tilted) where the player drives a physical rover with WASD / arrows - smooth
    /// acceleration, hull turning toward the travel direction and a light bank into
    /// turns. Patrol bots glide along their waypoint loops projecting translucent vision
    /// cones whose fan mesh is rebuilt every frame with per-ray occlusion, so the visual
    /// cone IS the detection truth - it never "sees" through walls.
    ///
    /// Everything - camera, light, floor, walls, shards, rover, bots, cones, materials -
    /// is built procedurally at runtime. Meshes are loaded from <c>Resources/DataRover/</c>
    /// by path (the shard falls back to the Packet Rush crystal, then to a primitive), so
    /// a missing FBX degrades to a Unity primitive + a loud warning, never to an empty
    /// arena. Collisions and detection are deterministic math (cell AABBs, distance and
    /// angle tests) - no physics, colliders are stripped from loaded models.
    /// Sits on the same GameObject as <see cref="RoverMinigame"/> in the behaviour prefab.
    /// </summary>
    public class RoverMinigameWorld : MonoBehaviour
    {
        [Header("Arena (world units)")]
        [SerializeField, Tooltip("Edge length of one arena cell.")]
        private float cellSize = 1.5f;
        [SerializeField, Tooltip("Height of the wall blocks.")]
        private float wallHeight = 0.85f;

        [Header("Rover")]
        [SerializeField, Tooltip("Height of the rover visual above the floor.")]
        private float roverY = 0.35f;
        [SerializeField, Tooltip("Collision half-extent of the rover (world units, must stay below cellSize/2).")]
        private float roverHalfWidth = 0.42f;
        [SerializeField, Tooltip("How quickly the hull turns toward the travel direction.")]
        private float yawSmoothing = 12f;
        [SerializeField, Tooltip("Bank angle per degree/second of turn rate.")]
        private float bankPerYawRate = 0.05f;
        [SerializeField] private float maxBankAngle = 14f;
        [SerializeField, Tooltip("How quickly the bank eases in and out.")]
        private float bankSmoothing = 10f;

        [Header("Patrols & pickups (world units)")]
        [SerializeField, Tooltip("Height of the patrol bot visual above the floor.")]
        private float botY = 0.5f;
        [SerializeField, Tooltip("Gentle vertical bob of bots and shards.")]
        private float bobAmplitude = 0.07f;
        [SerializeField, Tooltip("Rover-shard distance that counts as a pickup.")]
        private float shardPickupRadius = 0.9f;
        [SerializeField, Tooltip("Rover-bot distance that counts as a detection (body contact).")]
        private float contactRadius = 0.85f;
        [SerializeField, Tooltip("Rays in the vision cone fan (more = smoother occlusion edge).")]
        private int coneRayCount = 22;
        [SerializeField, Tooltip("Height of the cone fan above the floor.")]
        private float coneY = 0.06f;

        // Same neon language as Packet Rush; colors are given by code only.
        [Header("Palette")]
        [SerializeField] private Color roverColor = new Color(0.13f, 0.96f, 0.88f);
        [SerializeField] private Color shardColor = new Color(0.21f, 0.88f, 1f);
        [SerializeField] private Color patrolColor = new Color(1f, 0.3f, 0.26f);
        [SerializeField] private Color coneColor = new Color(1f, 0.72f, 0.2f);
        [SerializeField, Range(0f, 1f)] private float coneAlpha = 0.22f;
        [SerializeField] private Color coneAlertColor = new Color(1f, 0.25f, 0.35f);
        [SerializeField] private Color wallColor = new Color(0.1f, 0.16f, 0.3f);
        [SerializeField] private Color floorColor = new Color(0.03f, 0.055f, 0.1f);
        [SerializeField] private Color gridLineColor = new Color(0.08f, 0.35f, 0.45f);
        [SerializeField] private Color startZoneColor = new Color(0.24f, 1f, 0.53f);
        [SerializeField] private Color warnColor = new Color(1f, 0.35f, 0.6f);
        [SerializeField] private Color backgroundColor = new Color(0.024f, 0.043f, 0.078f);
        [SerializeField, Tooltip("Multiplier for the emission tint of the neon materials.")]
        private float emissionIntensity = 1.5f;

        [Header("Diagnostics")]
        [SerializeField, Tooltip("Log every pickup and detection to the console.")]
        private bool logEvents = false;

        private class ShardPickup
        {
            public Transform transform;
            public float bobPhase;
        }

        private class PatrolBot
        {
            public Transform rig;
            public Transform visual;
            public List<Vector3> waypoints;
            public int nextWaypoint;
            public float yaw;
            public Mesh coneMesh;
            public Material coneMaterial;
            public float alertTimer;
            public float bobPhase;
        }

        private RoverMinigame minigame;
        private RoverLayout layout;
        private Transform worldRoot;
        private Transform roverRig;
        private Transform roverVisual;
        private Material roverMaterial;

        private Vector3 roverVelocity;
        private float roverYaw;
        private float currentBank;
        private float hitFlashTimer;

        private readonly List<ShardPickup> shards = new List<ShardPickup>();
        private readonly List<PatrolBot> bots = new List<PatrolBot>();
        private readonly List<Material> createdMaterials = new List<Material>();
        private readonly List<Mesh> createdMeshes = new List<Mesh>();

        private void Awake()
        {
            minigame = GetComponent<RoverMinigame>();
        }

        private void Start()
        {
            if (minigame == null)
            {
                Debug.LogError("[DataRover] RoverMinigameWorld needs a RoverMinigame on the same GameObject.");
                return;
            }

            layout = minigame.Layout;
            BuildWorld();
            Debug.Log($"[DataRover] world ready - layout '{layout.name}', {shards.Count} shards, " +
                $"{bots.Count} patrols; arena/cones procedural, meshes from Resources/DataRover with primitive fallbacks.");
        }

        private void OnDestroy()
        {
            foreach (Material material in createdMaterials)
            {
                if (material != null)
                    Destroy(material);
            }

            foreach (Mesh mesh in createdMeshes)
            {
                if (mesh != null)
                    Destroy(mesh);
            }
        }

        private void Update()
        {
            if (minigame == null || worldRoot == null)
                return;

            float deltaTime = Time.deltaTime;

            minigame.TickTimers(deltaTime);
            UpdateRoverGlow();

            if (minigame.IsPaused || minigame.IsFinished)
                return;

            UpdateRover(deltaTime);
            UpdateShards(deltaTime);
            UpdatePatrols(deltaTime);
        }

        // ------------------------------------------------------------------ build

        private void BuildWorld()
        {
            worldRoot = new GameObject("RoverWorld").transform;
            worldRoot.SetParent(transform, false);

            BuildCamera();
            BuildLight();
            BuildArena();
            BuildShards();
            BuildRover();
            BuildPatrols();
        }

        private void BuildCamera()
        {
            float arenaDepth = layout.rows * cellSize;

            GameObject camGo = new GameObject("RoverCamera", typeof(Camera));
            camGo.transform.SetParent(worldRoot, false);
            camGo.transform.localPosition = new Vector3(0f, arenaDepth * 1.03f, -arenaDepth * 0.545f);
            camGo.transform.localEulerAngles = new Vector3(70f, 0f, 0f);

            Camera cam = camGo.GetComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = backgroundColor;
            cam.fieldOfView = 56f;
            cam.nearClipPlane = 0.3f;
            cam.farClipPlane = 90f;
            cam.depth = 10f;
        }

        private void BuildLight()
        {
            GameObject lightGo = new GameObject("RoverLight", typeof(Light));
            lightGo.transform.SetParent(worldRoot, false);
            lightGo.transform.localEulerAngles = new Vector3(55f, -32f, 0f);

            Light light = lightGo.GetComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.15f;
            light.color = new Color(0.9f, 0.95f, 1f);
        }

        private void BuildArena()
        {
            float arenaWidth = layout.cols * cellSize;
            float arenaDepth = layout.rows * cellSize;

            CreateBox("Floor", worldRoot,
                new Vector3(0f, -0.05f, 0f),
                new Vector3(arenaWidth + 0.8f, 0.1f, arenaDepth + 0.8f),
                CreateNeonMaterial(floorColor, 0.35f));

            // Faint cell grid - it lets the player read patrol distances and time windows.
            Material gridMat = CreateNeonMaterial(gridLineColor * 0.55f, 0.55f);

            for (int c = 1; c < layout.cols; c++)
            {
                float x = (c - layout.cols * 0.5f) * cellSize;
                CreateBox($"GridV{c}", worldRoot,
                    new Vector3(x, 0.012f, 0f),
                    new Vector3(0.03f, 0.012f, arenaDepth),
                    gridMat);
            }

            for (int r = 1; r < layout.rows; r++)
            {
                float z = (layout.rows * 0.5f - r) * cellSize;
                CreateBox($"GridH{r}", worldRoot,
                    new Vector3(0f, 0.012f, z),
                    new Vector3(arenaWidth, 0.012f, 0.03f),
                    gridMat);
            }

            Material wallMat = CreateNeonMaterial(wallColor, 0.9f);

            for (int r = 0; r < layout.rows; r++)
            {
                for (int c = 0; c < layout.cols; c++)
                {
                    if (layout.IsWall(c, r) == false)
                        continue;

                    CreateBox($"Wall_{c}_{r}", worldRoot,
                        new Vector3(WorldX(c), wallHeight * 0.5f, WorldZ(r)),
                        new Vector3(cellSize * 0.98f, wallHeight, cellSize * 0.98f),
                        wallMat);
                }
            }

            CreateBox("StartZone", worldRoot,
                new Vector3(WorldX(layout.startCell.x), 0.02f, WorldZ(layout.startCell.y)),
                new Vector3(cellSize * 0.9f, 0.02f, cellSize * 0.9f),
                CreateNeonMaterial(startZoneColor * 0.7f, 0.8f));
        }

        private void BuildShards()
        {
            // Random subset of the authored slots (every slot is validated fair, so any
            // subset is fair). Patrols stay fully deterministic.
            List<Vector2Int> slots = new List<Vector2Int>(layout.shardCells);
            int count = minigame.TotalShards;

            for (int i = slots.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (slots[i], slots[j]) = (slots[j], slots[i]);
            }

            Material shardMat = CreateNeonMaterial(shardColor, emissionIntensity);

            for (int i = 0; i < count && i < slots.Count; i++)
            {
                GameObject go = LoadShardModel();
                go.name = $"Shard_{slots[i].x}_{slots[i].y}";

                Transform shardTransform = go.transform;
                shardTransform.SetParent(worldRoot, false);
                shardTransform.localPosition = new Vector3(WorldX(slots[i].x), 0.55f, WorldZ(slots[i].y));
                shardTransform.localScale = Vector3.one * 0.8f;
                ApplyMaterial(go, shardMat);

                shards.Add(new ShardPickup
                {
                    transform = shardTransform,
                    bobPhase = Random.Range(0f, Mathf.PI * 2f),
                });
            }
        }

        private void BuildRover()
        {
            roverRig = new GameObject("RoverRig").transform;
            roverRig.SetParent(worldRoot, false);
            roverRig.localPosition = new Vector3(WorldX(layout.startCell.x), 0f, WorldZ(layout.startCell.y));

            roverMaterial = CreateNeonMaterial(roverColor, emissionIntensity);

            GameObject rover = LoadModel("rover", PrimitiveType.Cube);
            rover.name = "RoverVisual";
            roverVisual = rover.transform;
            roverVisual.SetParent(roverRig, false);
            roverVisual.localPosition = new Vector3(0f, roverY, 0f);
            roverVisual.localScale = Vector3.one * 0.95f;
            ApplyMaterial(rover, roverMaterial);
        }

        private void BuildPatrols()
        {
            List<List<Vector2Int>> routes = new List<List<Vector2Int>>(layout.patrolRoutes);

            if (minigame.UseExtraPatrol)
                routes.Add(layout.hardExtraRoute);

            foreach (List<Vector2Int> route in routes)
            {
                List<Vector3> waypoints = new List<Vector3>();

                foreach (Vector2Int cell in route)
                    waypoints.Add(new Vector3(WorldX(cell.x), 0f, WorldZ(cell.y)));

                PatrolBot bot = new PatrolBot
                {
                    waypoints = waypoints,
                    nextWaypoint = 1 % waypoints.Count,
                    bobPhase = Random.Range(0f, Mathf.PI * 2f),
                };

                bot.rig = new GameObject($"PatrolRig_{bots.Count}").transform;
                bot.rig.SetParent(worldRoot, false);
                bot.rig.localPosition = waypoints[0];
                bot.yaw = YawToward(waypoints[0], waypoints[bot.nextWaypoint], 0f);
                bot.rig.localRotation = Quaternion.Euler(0f, bot.yaw, 0f);

                GameObject visual = LoadModel("patrol_bot", PrimitiveType.Sphere);
                visual.name = "BotVisual";
                bot.visual = visual.transform;
                bot.visual.SetParent(bot.rig, false);
                bot.visual.localPosition = new Vector3(0f, botY, 0f);
                bot.visual.localScale = Vector3.one * 0.85f;
                ApplyMaterial(visual, GetPatrolMaterial());

                // Vision cone: a translucent fan rebuilt every frame with per-ray
                // occlusion, so what the player sees is exactly what detects them.
                GameObject coneGo = new GameObject("VisionCone", typeof(MeshFilter), typeof(MeshRenderer));
                coneGo.transform.SetParent(bot.rig, false);

                bot.coneMesh = new Mesh { name = "VisionConeMesh" };
                bot.coneMesh.MarkDynamic();
                createdMeshes.Add(bot.coneMesh);
                coneGo.GetComponent<MeshFilter>().sharedMesh = bot.coneMesh;

                bot.coneMaterial = CreateConeMaterial();
                coneGo.GetComponent<MeshRenderer>().sharedMaterial = bot.coneMaterial;

                bots.Add(bot);
            }
        }

        // ------------------------------------------------------------ per-frame

        private void UpdateRover(float deltaTime)
        {
            Vector3 input = ReadMoveInput();
            float maxSpeed = minigame.RoverSpeedCells * cellSize;
            float acceleration = minigame.RoverAccelerationCells * cellSize;

            roverVelocity = Vector3.MoveTowards(roverVelocity, input * maxSpeed, acceleration * deltaTime);

            // Axis-separated movement so the rover slides along walls instead of sticking.
            Vector3 position = roverRig.localPosition;
            float newX = position.x + roverVelocity.x * deltaTime;

            if (BoxOverlapsWall(newX, position.z))
            {
                newX = position.x;
                roverVelocity.x = 0f;
            }

            float newZ = position.z + roverVelocity.z * deltaTime;

            if (BoxOverlapsWall(newX, newZ))
            {
                newZ = position.z;
                roverVelocity.z = 0f;
            }

            roverRig.localPosition = new Vector3(newX, 0f, newZ);

            // Hull turns toward the travel direction; the bank into the turn plus the
            // ease back to level is the same "flow" juice as the Packet Rush cube.
            float speed = roverVelocity.magnitude;
            float previousYaw = roverYaw;

            if (speed > maxSpeed * 0.12f)
            {
                float targetYaw = Mathf.Atan2(roverVelocity.x, roverVelocity.z) * Mathf.Rad2Deg;
                float ease = 1f - Mathf.Exp(-yawSmoothing * deltaTime);
                roverYaw = Mathf.LerpAngle(roverYaw, targetYaw, ease);
            }

            float yawRate = deltaTime > 0f ? Mathf.DeltaAngle(previousYaw, roverYaw) / deltaTime : 0f;
            float targetBank = Mathf.Clamp(-yawRate * bankPerYawRate, -maxBankAngle, maxBankAngle);
            float bankEase = 1f - Mathf.Exp(-bankSmoothing * deltaTime);
            currentBank = Mathf.Lerp(currentBank, targetBank, bankEase);

            roverRig.localRotation = Quaternion.Euler(0f, roverYaw, 0f);
            roverVisual.localRotation = Quaternion.Euler(0f, 0f, currentBank);
        }

        private Vector3 ReadMoveInput()
        {
            Vector3 input = Vector3.zero;

            if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow))
                input.z += 1f;

            if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow))
                input.z -= 1f;

            if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))
                input.x -= 1f;

            if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow))
                input.x += 1f;

            return input.sqrMagnitude > 1f ? input.normalized : input;
        }

        private void UpdateShards(float deltaTime)
        {
            Vector3 roverPosition = roverRig.localPosition;

            for (int i = shards.Count - 1; i >= 0; i--)
            {
                ShardPickup shard = shards[i];

                if (shard.transform == null)
                {
                    shards.RemoveAt(i);
                    continue;
                }

                shard.transform.Rotate(0f, 90f * deltaTime, 0f, Space.Self);
                Vector3 position = shard.transform.localPosition;
                position.y = 0.55f + Mathf.Sin(Time.time * 2.4f + shard.bobPhase) * bobAmplitude;
                shard.transform.localPosition = position;

                if (FlatDistance(position, roverPosition) <= shardPickupRadius)
                {
                    string message = minigame.CollectShard();
                    shards.RemoveAt(i);
                    Destroy(shard.transform.gameObject);
                    PushFeedback(message, false);

                    if (logEvents)
                        Debug.Log($"[DataRover] shard collected ({minigame.Collected}/{minigame.TotalShards})");

                    if (minigame.IsFinished)
                        return;
                }
            }
        }

        private void UpdatePatrols(float deltaTime)
        {
            float patrolSpeed = minigame.PatrolSpeedCells * cellSize;
            float visionRange = minigame.VisionRangeCells * cellSize;
            float visionHalfAngle = minigame.VisionHalfAngleDegrees;
            Vector3 roverPosition = roverRig.localPosition;

            foreach (PatrolBot bot in bots)
            {
                // March along the loop - deterministic, so windows can be learned.
                Vector3 position = bot.rig.localPosition;
                Vector3 target = bot.waypoints[bot.nextWaypoint];
                position = Vector3.MoveTowards(position, target, patrolSpeed * deltaTime);
                bot.rig.localPosition = position;

                if ((position - target).sqrMagnitude < 0.0001f)
                    bot.nextWaypoint = (bot.nextWaypoint + 1) % bot.waypoints.Count;

                bot.yaw = YawToward(position, bot.waypoints[bot.nextWaypoint], bot.yaw, 1f - Mathf.Exp(-8f * deltaTime));
                bot.rig.localRotation = Quaternion.Euler(0f, bot.yaw, 0f);

                Vector3 visualPosition = bot.visual.localPosition;
                visualPosition.y = botY + Mathf.Sin(Time.time * 2.1f + bot.bobPhase) * bobAmplitude;
                bot.visual.localPosition = visualPosition;

                RebuildConeMesh(bot, visionRange, visionHalfAngle);
                UpdateConeMaterial(bot, deltaTime);

                if (minigame.IsInvulnerable || minigame.IsFinished)
                    continue;

                bool caught = FlatDistance(position, roverPosition) <= contactRadius
                    || ConeSeesRover(bot, roverPosition, visionRange, visionHalfAngle);

                if (caught)
                {
                    string message = minigame.OnDetected();

                    if (string.IsNullOrEmpty(message))
                        continue;

                    hitFlashTimer = 0.4f;
                    bot.alertTimer = 0.6f;
                    PushFeedback(message, true);

                    if (logEvents)
                        Debug.Log($"[DataRover] detected by {bot.rig.name} (integrity {minigame.CurrentIntegrity}/{minigame.MaxIntegrity})");

                    if (minigame.IsFinished)
                        return;
                }
            }
        }

        private bool ConeSeesRover(PatrolBot bot, Vector3 roverPosition, float range, float halfAngle)
        {
            Vector3 origin = bot.rig.localPosition;
            Vector3 toRover = roverPosition - origin;
            toRover.y = 0f;
            float distance = toRover.magnitude;

            if (distance > range)
                return false;

            Vector3 forward = Quaternion.Euler(0f, bot.yaw, 0f) * Vector3.forward;

            if (distance > 0.001f && Vector3.Angle(forward, toRover) > halfAngle)
                return false;

            // Line of sight: same sampling as the cone mesh, so vision = visuals.
            return CastVisionRay(origin, toRover.normalized, distance) >= distance - cellSize * 0.1f;
        }

        /// <summary>
        /// Steps along a ray until it hits a wall cell; returns the free distance.
        /// Used both for detection line-of-sight and for clipping the cone fan.
        /// </summary>
        private float CastVisionRay(Vector3 origin, Vector3 direction, float maxDistance)
        {
            float step = cellSize * 0.12f;

            for (float t = step; t < maxDistance; t += step)
            {
                Vector3 point = origin + direction * t;

                if (layout.IsWall(ColAt(point.x), RowAt(point.z)))
                    return t - step * 0.5f;
            }

            return maxDistance;
        }

        private void RebuildConeMesh(PatrolBot bot, float range, float halfAngle)
        {
            int rays = Mathf.Max(6, coneRayCount);
            Vector3 origin = bot.rig.localPosition;

            Vector3[] vertices = new Vector3[rays + 2];
            int[] triangles = new int[rays * 3];
            vertices[0] = new Vector3(0f, coneY, 0f);

            for (int i = 0; i <= rays; i++)
            {
                float angle = -halfAngle + (halfAngle * 2f) * i / rays;
                Vector3 worldDir = Quaternion.Euler(0f, bot.yaw + angle, 0f) * Vector3.forward;
                float distance = CastVisionRay(origin, worldDir, range);
                Vector3 local = Quaternion.Euler(0f, angle, 0f) * Vector3.forward * distance;
                vertices[i + 1] = new Vector3(local.x, coneY, local.z);
            }

            for (int i = 0; i < rays; i++)
            {
                triangles[i * 3] = 0;
                triangles[i * 3 + 1] = i + 1;
                triangles[i * 3 + 2] = i + 2;
            }

            bot.coneMesh.Clear();
            bot.coneMesh.vertices = vertices;
            bot.coneMesh.triangles = triangles;
            bot.coneMesh.RecalculateBounds();
        }

        private void UpdateConeMaterial(PatrolBot bot, float deltaTime)
        {
            if (bot.alertTimer > 0f)
                bot.alertTimer -= deltaTime;

            float alert = Mathf.Clamp01(bot.alertTimer / 0.6f);
            Color color = Color.Lerp(coneColor, coneAlertColor, alert);
            color.a = coneAlpha + alert * 0.18f;
            bot.coneMaterial.color = color;

            if (bot.coneMaterial.HasProperty("_EmissionColor"))
                bot.coneMaterial.SetColor("_EmissionColor", color * (0.8f + alert));
        }

        private void UpdateRoverGlow()
        {
            if (roverMaterial == null)
                return;

            if (hitFlashTimer > 0f)
            {
                hitFlashTimer -= Time.deltaTime;
                float blend = Mathf.Clamp01(hitFlashTimer / 0.4f);
                Color color = Color.Lerp(roverColor, warnColor, blend);
                roverMaterial.color = color;
                roverMaterial.SetColor("_EmissionColor", color * emissionIntensity);
                return;
            }

            if (minigame.IsInvulnerable)
            {
                // Cloak-scramble blink while invulnerable - readable "you are safe" state.
                float pulse = 0.55f + 0.45f * Mathf.Sin(Time.time * 24f);
                roverMaterial.color = roverColor;
                roverMaterial.SetColor("_EmissionColor", roverColor * (emissionIntensity * pulse));
                return;
            }

            roverMaterial.color = roverColor;
            roverMaterial.SetColor("_EmissionColor", roverColor * emissionIntensity);
        }

        // ------------------------------------------------------------- helpers

        private float WorldX(int col) => (col - (layout.cols - 1) * 0.5f) * cellSize;

        private float WorldZ(int row) => ((layout.rows - 1) * 0.5f - row) * cellSize;

        private int ColAt(float x) => Mathf.RoundToInt(x / cellSize + (layout.cols - 1) * 0.5f);

        private int RowAt(float z) => Mathf.RoundToInt((layout.rows - 1) * 0.5f - z / cellSize);

        private static float FlatDistance(Vector3 a, Vector3 b)
        {
            float dx = a.x - b.x;
            float dz = a.z - b.z;
            return Mathf.Sqrt(dx * dx + dz * dz);
        }

        private float YawToward(Vector3 from, Vector3 to, float currentYaw, float ease = 1f)
        {
            Vector3 direction = to - from;
            direction.y = 0f;

            if (direction.sqrMagnitude < 0.0001f)
                return currentYaw;

            float targetYaw = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
            return Mathf.LerpAngle(currentYaw, targetYaw, ease);
        }

        private bool BoxOverlapsWall(float centerX, float centerZ)
        {
            int colMin = ColAt(centerX - roverHalfWidth);
            int colMax = ColAt(centerX + roverHalfWidth);
            int rowMin = RowAt(centerZ + roverHalfWidth); // +z = smaller row index
            int rowMax = RowAt(centerZ - roverHalfWidth);

            for (int c = colMin; c <= colMax; c++)
            {
                for (int r = rowMin; r <= rowMax; r++)
                {
                    if (layout.IsWall(c, r))
                        return true;
                }
            }

            return false;
        }

        private void PushFeedback(string message, bool warn)
        {
            if (string.IsNullOrEmpty(message))
                return;

            RoverMinigameUI ui = MinigamesUIManager.Instance != null
                ? MinigamesUIManager.Instance.ActiveMinigameUI as RoverMinigameUI
                : null;

            if (ui != null)
                ui.ShowFeedback(message, warn);
        }

        /// <summary>
        /// Loads a model from Resources/DataRover by name; degrades to a Unity primitive
        /// so a missing/not-yet-imported mesh can never empty the arena. Colliders are
        /// stripped - all contact detection is deterministic distance math.
        /// </summary>
        private GameObject LoadModel(string modelName, PrimitiveType fallback)
        {
            GameObject prefab = Resources.Load<GameObject>("DataRover/" + modelName);
            GameObject go;

            if (prefab != null)
            {
                go = Instantiate(prefab);
            }
            else
            {
                Debug.LogWarning($"[DataRover] Model 'Resources/DataRover/{modelName}' not found - using {fallback} primitive fallback.");
                go = GameObject.CreatePrimitive(fallback);
            }

            foreach (Collider collider in go.GetComponentsInChildren<Collider>())
                Destroy(collider);

            return go;
        }

        private GameObject LoadShardModel()
        {
            GameObject prefab = Resources.Load<GameObject>("DataRover/shard");

            // Design-doc reuse: the Packet Rush data crystal is the default shard mesh.
            if (prefab == null)
                prefab = Resources.Load<GameObject>("PacketRush/crystal");

            GameObject go;

            if (prefab != null)
            {
                go = Instantiate(prefab);
            }
            else
            {
                Debug.LogWarning("[DataRover] Neither 'Resources/DataRover/shard' nor 'Resources/PacketRush/crystal' found - using Capsule primitive fallback.");
                go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                go.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);
            }

            foreach (Collider collider in go.GetComponentsInChildren<Collider>())
                Destroy(collider);

            return go;
        }

        private void CreateBox(string boxName, Transform parent, Vector3 position, Vector3 scale, Material material)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = boxName;
            Destroy(go.GetComponent<Collider>());
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            go.transform.localScale = scale;
            ApplyMaterial(go, material);
        }

        private void ApplyMaterial(GameObject go, Material material)
        {
            if (material == null)
                return;

            foreach (Renderer renderer in go.GetComponentsInChildren<Renderer>())
                renderer.sharedMaterial = material;
        }

        private Material patrolMaterial;

        private Material GetPatrolMaterial()
        {
            if (patrolMaterial == null)
                patrolMaterial = CreateNeonMaterial(patrolColor, emissionIntensity);

            return patrolMaterial;
        }

        private Material CreateNeonMaterial(Color color, float emission)
        {
            Shader shader = Shader.Find("Standard");

            if (shader == null)
                shader = Shader.Find("Legacy Shaders/Diffuse");

            if (shader == null)
            {
                Debug.LogError("[DataRover] Neither Standard nor Legacy Diffuse shader found - materials will be missing.");
                return null;
            }

            Material material = new Material(shader);
            material.color = color;

            if (material.HasProperty("_EmissionColor"))
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", color * emission);
            }

            createdMaterials.Add(material);
            return material;
        }

        private Material CreateConeMaterial()
        {
            Material material = CreateNeonMaterial(coneColor, 0.8f);

            if (material == null)
                return null;

            // Standard shader "Fade" mode so the fan reads as light, not as a solid slab.
            Color color = coneColor;
            color.a = coneAlpha;
            material.color = color;
            material.SetFloat("_Mode", 2f);
            material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            material.SetInt("_ZWrite", 0);
            material.DisableKeyword("_ALPHATEST_ON");
            material.EnableKeyword("_ALPHABLEND_ON");
            material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            material.renderQueue = 3000;

            return material;
        }
    }
}
