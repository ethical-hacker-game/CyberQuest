using System.Collections.Generic;
using UnityEngine;

namespace Minigames
{
    /// <summary>
    /// 3D view for <see cref="RunnerMinigame"/>: the player steers a physical neon cube
    /// down a three-fiber backbone track, rolling forward and banking into lane switches.
    /// Obstacles are real meshes (authored in Blender, loaded from
    /// <c>Resources/PacketRush/</c> by name with primitive fallbacks) scrolling toward
    /// the cube; contact/pass events feed the same <see cref="RunnerMinigame.ResolveEntity"/>
    /// rules as before.
    ///
    /// Everything - camera, light, track, cube, obstacles, materials - is built
    /// procedurally at runtime. No hand-authored asset references exist on the spawn
    /// path (the lesson from the 2D version's silent empty-field failure), so a missing
    /// mesh degrades to a primitive instead of an empty track.
    /// Sits on the same GameObject as <see cref="RunnerMinigame"/> in the behaviour
    /// prefab; the whole rig is a child of it, so the controller's teardown destroys it.
    /// </summary>
    public class RunnerMinigameWorld : MonoBehaviour
    {
        [Header("Track (world units)")]
        [SerializeField, Tooltip("Distance between neighbouring lane centers.")]
        private float laneSpacing = 2.4f;
        [SerializeField, Tooltip("Spawn distance ahead of the cube; also maps the normalized ScrollSpeed to world units/s, so the 2D pacing tuning carries over.")]
        private float runLength = 55f;
        [SerializeField, Tooltip("Entities are destroyed once they pass behind this z.")]
        private float killZ = -9f;

        [Header("Player cube")]
        [SerializeField, Tooltip("Height of the cube center above the track.")]
        private float cubeY = 0.55f;
        [SerializeField, Tooltip("Lane-switch speed in lane-widths per second.")]
        private float laneSwitchSpeed = 8f;
        [SerializeField, Tooltip("Bank angle per unit of horizontal velocity (deg per u/s).")]
        private float bankPerVelocity = 4.5f;
        [SerializeField] private float maxBankAngle = 34f;
        [SerializeField, Tooltip("How quickly the lean eases in and out.")]
        private float bankSmoothing = 14f;
        [SerializeField, Tooltip("Forward roll: degrees per second per world unit of scroll speed.")]
        private float rollPerSpeed = 12f;
        [SerializeField] private float rollMinDegrees = 120f;
        [SerializeField] private float rollMaxDegrees = 460f;

        [Header("Collision (world units)")]
        [SerializeField, Tooltip("Half-window along Z around the cube in which same-lane contact counts.")]
        private float zBand = 1.15f;
        [SerializeField, Tooltip("Horizontal contact half-window as a fraction of lane spacing.")]
        private float xBandFactor = 0.45f;

        // Same neon palette as the HUD.
        [Header("Palette")]
        [SerializeField] private Color playerColor = new Color(0.13f, 0.96f, 0.88f);
        [SerializeField] private Color firewallColor = new Color(1f, 0.24f, 0.35f);
        [SerializeField] private Color malwareColor = new Color(0.77f, 0.22f, 1f);
        [SerializeField] private Color snifferColor = new Color(1f, 0.62f, 0.17f);
        [SerializeField] private Color dataColor = new Color(0.21f, 0.88f, 1f);
        [SerializeField] private Color patchColor = new Color(0.24f, 1f, 0.53f);
        [SerializeField] private Color warnColor = new Color(1f, 0.35f, 0.60f);
        [SerializeField] private Color groundColor = new Color(0.03f, 0.055f, 0.10f);
        [SerializeField] private Color laneLineColor = new Color(0.08f, 0.35f, 0.45f);
        [SerializeField] private Color backgroundColor = new Color(0.024f, 0.043f, 0.078f);
        [SerializeField, Tooltip("Multiplier for the emission tint of the neon materials.")]
        private float emissionIntensity = 1.5f;

        [Header("Diagnostics")]
        [SerializeField, Tooltip("Log every spawned entity to the console.")]
        private bool logSpawns = false;

        private class WorldEntity
        {
            public RunnerEntityKind kind;
            public Transform transform;
            public Vector3 spin;
            public bool resolved;
        }

        private RunnerMinigame minigame;
        private Transform worldRoot;
        private Transform playerRig;
        private Transform cubeVisual;
        private Material cubeMaterial;

        private readonly List<WorldEntity> entities = new List<WorldEntity>();
        private readonly List<Transform> speedMarkers = new List<Transform>();
        private readonly Dictionary<RunnerEntityKind, Material> kindMaterials = new Dictionary<RunnerEntityKind, Material>();
        private readonly List<Material> createdMaterials = new List<Material>();

        private float currentBank;
        private float currentYaw;
        private float hitFlashTimer;

        public int SpawnedTotal { get; private set; }
        public int LiveCount => entities.Count;

        private float PlayerX => playerRig != null ? playerRig.localPosition.x : 0f;

        private void Awake()
        {
            minigame = GetComponent<RunnerMinigame>();
        }

        private void Start()
        {
            if (minigame == null)
            {
                Debug.LogError("[PacketRush] RunnerMinigameWorld needs a RunnerMinigame on the same GameObject.");
                return;
            }

            BuildWorld();
            Debug.Log("[PacketRush] 3D world ready - track, cube and camera built procedurally; obstacle meshes from Resources/PacketRush with primitive fallbacks.");
        }

        private void OnDestroy()
        {
            foreach (Material material in createdMaterials)
            {
                if (material != null)
                    Destroy(material);
            }
        }

        private void Update()
        {
            if (minigame == null || worldRoot == null)
                return;

            UpdateHitFlash();

            if (minigame.IsPaused || minigame.IsFinished)
                return;

            float speed = minigame.ScrollSpeed * runLength;
            float deltaTime = Time.deltaTime;

            SpawnPendingWaves();
            UpdatePlayer(speed, deltaTime);
            UpdateEntities(speed, deltaTime);
            UpdateSpeedMarkers(speed, deltaTime);
        }

        // ------------------------------------------------------------------ build

        private void BuildWorld()
        {
            worldRoot = new GameObject("RunnerWorld").transform;
            worldRoot.SetParent(transform, false);

            BuildCamera();
            BuildLight();
            BuildTrack();
            BuildPlayer();
        }

        private void BuildCamera()
        {
            GameObject camGo = new GameObject("RunnerCamera", typeof(Camera));
            camGo.transform.SetParent(worldRoot, false);
            camGo.transform.localPosition = new Vector3(0f, 4.6f, -7.6f);
            camGo.transform.localEulerAngles = new Vector3(26f, 0f, 0f);

            Camera cam = camGo.GetComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = backgroundColor;
            cam.fieldOfView = 58f;
            cam.nearClipPlane = 0.3f;
            cam.farClipPlane = 140f;
            cam.depth = 10f;
        }

        private void BuildLight()
        {
            GameObject lightGo = new GameObject("RunnerLight", typeof(Light));
            lightGo.transform.SetParent(worldRoot, false);
            lightGo.transform.localEulerAngles = new Vector3(55f, -32f, 0f);

            Light light = lightGo.GetComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.15f;
            light.color = new Color(0.9f, 0.95f, 1f);
        }

        private void BuildTrack()
        {
            float trackLength = runLength + 25f;
            float trackCenterZ = trackLength / 2f - 10f;

            Transform ground = CreateBox("Ground", worldRoot,
                new Vector3(0f, -0.05f, trackCenterZ),
                new Vector3(laneSpacing * 3f + 1.6f, 0.1f, trackLength),
                CreateNeonMaterial(groundColor, 0.35f));
            ground.gameObject.isStatic = false;

            // Lane boundary lines: bright outer edges, dimmer inner dividers.
            Material edgeMat = CreateNeonMaterial(laneLineColor * 1.6f, 1.4f);
            Material innerMat = CreateNeonMaterial(laneLineColor, 1.1f);

            float[] lineXs = { -laneSpacing * 1.5f, -laneSpacing * 0.5f, laneSpacing * 0.5f, laneSpacing * 1.5f };

            for (int i = 0; i < lineXs.Length; i++)
            {
                bool outer = i == 0 || i == lineXs.Length - 1;
                CreateBox($"LaneLine{i}", worldRoot,
                    new Vector3(lineXs[i], 0.02f, trackCenterZ),
                    new Vector3(outer ? 0.12f : 0.07f, 0.04f, trackLength),
                    outer ? edgeMat : innerMat);
            }

            // Short markers gliding along the dividers sell the speed.
            Material markerMat = CreateNeonMaterial(playerColor, 1.6f);
            int markersPerSide = 7;

            for (int side = 0; side < 2; side++)
            {
                float x = (side == 0 ? -0.5f : 0.5f) * laneSpacing;

                for (int i = 0; i < markersPerSide; i++)
                {
                    float z = -6f + (runLength + 12f) * i / markersPerSide;
                    Transform marker = CreateBox($"Marker_{side}_{i}", worldRoot,
                        new Vector3(x, 0.045f, z),
                        new Vector3(0.16f, 0.05f, 1.1f),
                        markerMat);
                    speedMarkers.Add(marker);
                }
            }
        }

        private void BuildPlayer()
        {
            playerRig = new GameObject("PlayerRig").transform;
            playerRig.SetParent(worldRoot, false);
            playerRig.localPosition = new Vector3(LaneX(minigame.CurrentLane), 0f, 0f);

            cubeMaterial = CreateNeonMaterial(playerColor, emissionIntensity);

            GameObject cube = LoadModel("player_cube", PrimitiveType.Cube);
            cube.name = "PlayerCube";
            cubeVisual = cube.transform;
            cubeVisual.SetParent(playerRig, false);
            cubeVisual.localPosition = new Vector3(0f, cubeY, 0f);
            ApplyMaterial(cube, cubeMaterial);
        }

        // ------------------------------------------------------------ per-frame

        private void SpawnPendingWaves()
        {
            while (minigame.TryDequeueWave(out RunnerWave wave))
            {
                for (int i = 0; i < wave.spawns.Count; i++)
                    SpawnEntity(wave.spawns[i]);
            }
        }

        private void SpawnEntity(RunnerSpawn spawn)
        {
            GameObject go = LoadModel(GetModelName(spawn.kind), GetFallbackPrimitive(spawn.kind));
            go.name = $"Entity_{spawn.kind}";

            Transform entityTransform = go.transform;
            entityTransform.SetParent(worldRoot, false);
            entityTransform.localPosition = new Vector3(LaneX(spawn.lane), GetHoverHeight(spawn.kind), runLength);
            entityTransform.localScale = Vector3.one * GetScale(spawn.kind);

            ApplyMaterial(go, GetMaterial(spawn.kind));

            entities.Add(new WorldEntity
            {
                kind = spawn.kind,
                transform = entityTransform,
                spin = GetSpin(spawn.kind),
            });

            SpawnedTotal++;

            if (logSpawns)
                Debug.Log($"[PacketRush] spawn #{SpawnedTotal}: {spawn.kind} on lane {spawn.lane} (live: {entities.Count})");
        }

        private void UpdatePlayer(float speed, float deltaTime)
        {
            if (playerRig == null || cubeVisual == null)
                return;

            float targetX = LaneX(minigame.CurrentLane);
            float oldX = playerRig.localPosition.x;
            float newX = Mathf.MoveTowards(oldX, targetX, laneSpacing * laneSwitchSpeed * deltaTime);
            playerRig.localPosition = new Vector3(newX, 0f, 0f);

            // Lean into the switch: bank (roll toward the target lane) plus a light yaw,
            // both easing back to neutral once the cube settles - the "flow" juice.
            float velocity = deltaTime > 0f ? (newX - oldX) / deltaTime : 0f;
            float targetBank = Mathf.Clamp(-velocity * bankPerVelocity, -maxBankAngle, maxBankAngle);
            float targetYaw = Mathf.Clamp(velocity * 1.6f, -14f, 14f);

            float ease = 1f - Mathf.Exp(-bankSmoothing * deltaTime);
            currentBank = Mathf.Lerp(currentBank, targetBank, ease);
            currentYaw = Mathf.Lerp(currentYaw, targetYaw, ease);
            playerRig.localRotation = Quaternion.Euler(0f, currentYaw, currentBank);

            // Constant forward roll scaled with world speed - the "moving ahead" illusion.
            float rollSpeed = Mathf.Clamp(speed * rollPerSpeed, rollMinDegrees, rollMaxDegrees);
            cubeVisual.Rotate(rollSpeed * deltaTime, 0f, 0f, Space.Self);
        }

        private void UpdateEntities(float speed, float deltaTime)
        {
            float xBand = laneSpacing * xBandFactor;

            for (int i = entities.Count - 1; i >= 0; i--)
            {
                WorldEntity entity = entities[i];

                if (entity.transform == null)
                {
                    entities.RemoveAt(i);
                    continue;
                }

                Vector3 position = entity.transform.localPosition;
                position.z -= speed * deltaTime;
                entity.transform.localPosition = position;

                if (entity.spin != Vector3.zero)
                    entity.transform.Rotate(entity.spin * deltaTime, Space.Self);

                if (entity.resolved == false)
                {
                    bool inZBand = Mathf.Abs(position.z) <= zBand;
                    bool inXBand = Mathf.Abs(position.x - PlayerX) <= xBand;

                    if (inZBand && inXBand)
                    {
                        ResolveEntity(entity, true);

                        // A hit or collect consumes the object - nothing should pass
                        // visibly through the cube.
                        entities.RemoveAt(i);
                        Destroy(entity.transform.gameObject);
                        continue;
                    }

                    if (position.z < -zBand)
                        ResolveEntity(entity, false);
                }

                if (position.z < killZ)
                {
                    entities.RemoveAt(i);
                    Destroy(entity.transform.gameObject);
                }
            }
        }

        private void UpdateSpeedMarkers(float speed, float deltaTime)
        {
            float wrapLength = runLength + 18f;

            foreach (Transform marker in speedMarkers)
            {
                Vector3 position = marker.localPosition;
                position.z -= speed * deltaTime;

                if (position.z < -6f)
                    position.z += wrapLength;

                marker.localPosition = position;
            }
        }

        private void ResolveEntity(WorldEntity entity, bool collided)
        {
            entity.resolved = true;
            string message = minigame.ResolveEntity(entity.kind, collided);

            bool warn = collided && RunnerEntity.IsHazard(entity.kind);

            if (warn)
                hitFlashTimer = 0.4f;

            if (string.IsNullOrEmpty(message) == false)
            {
                RunnerMinigameUI ui = MinigamesUIManager.Instance != null
                    ? MinigamesUIManager.Instance.ActiveMinigameUI as RunnerMinigameUI
                    : null;

                if (ui != null)
                    ui.ShowFeedback(message, warn);
            }
        }

        private void UpdateHitFlash()
        {
            if (cubeMaterial == null)
                return;

            if (hitFlashTimer > 0f)
            {
                hitFlashTimer -= Time.deltaTime;
                float blend = Mathf.Clamp01(hitFlashTimer / 0.4f);
                Color color = Color.Lerp(playerColor, warnColor, blend);
                cubeMaterial.color = color;
                cubeMaterial.SetColor("_EmissionColor", color * emissionIntensity);
            }
        }

        // ------------------------------------------------------------- helpers

        private float LaneX(int lane) => (lane - 1) * laneSpacing;

        /// <summary>
        /// Loads a Blender-authored model from Resources/PacketRush; degrades to a Unity
        /// primitive so a missing/not-yet-imported mesh can never empty the track.
        /// Colliders are stripped - contact detection is deterministic distance math.
        /// </summary>
        private GameObject LoadModel(string modelName, PrimitiveType fallback)
        {
            GameObject prefab = Resources.Load<GameObject>("PacketRush/" + modelName);
            GameObject go;

            if (prefab != null)
            {
                go = Instantiate(prefab);
            }
            else
            {
                Debug.LogWarning($"[PacketRush] Model 'Resources/PacketRush/{modelName}' not found - using {fallback} primitive fallback.");
                go = GameObject.CreatePrimitive(fallback);
            }

            foreach (Collider collider in go.GetComponentsInChildren<Collider>())
                Destroy(collider);

            return go;
        }

        private Transform CreateBox(string boxName, Transform parent, Vector3 position, Vector3 scale, Material material)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = boxName;
            Destroy(go.GetComponent<Collider>());
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            go.transform.localScale = scale;
            ApplyMaterial(go, material);
            return go.transform;
        }

        private void ApplyMaterial(GameObject go, Material material)
        {
            if (material == null)
                return;

            foreach (Renderer renderer in go.GetComponentsInChildren<Renderer>())
                renderer.sharedMaterial = material;
        }

        private Material GetMaterial(RunnerEntityKind kind)
        {
            if (kindMaterials.TryGetValue(kind, out Material cached))
                return cached;

            Material material = CreateNeonMaterial(GetColor(kind), emissionIntensity);
            kindMaterials[kind] = material;
            return material;
        }

        private Material CreateNeonMaterial(Color color, float emission)
        {
            Shader shader = Shader.Find("Standard");

            if (shader == null)
                shader = Shader.Find("Legacy Shaders/Diffuse");

            if (shader == null)
            {
                Debug.LogError("[PacketRush] Neither Standard nor Legacy Diffuse shader found - materials will be missing.");
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

        private string GetModelName(RunnerEntityKind kind)
        {
            switch (kind)
            {
                case RunnerEntityKind.Firewall: return "barrier";
                case RunnerEntityKind.MalwareNode: return "mine";
                case RunnerEntityKind.Sniffer: return "star";
                case RunnerEntityKind.DataShard: return "crystal";
                default: return "patch_cross";
            }
        }

        private PrimitiveType GetFallbackPrimitive(RunnerEntityKind kind)
        {
            switch (kind)
            {
                case RunnerEntityKind.Firewall: return PrimitiveType.Cube;
                case RunnerEntityKind.MalwareNode: return PrimitiveType.Sphere;
                case RunnerEntityKind.Sniffer: return PrimitiveType.Sphere;
                case RunnerEntityKind.DataShard: return PrimitiveType.Capsule;
                default: return PrimitiveType.Cube;
            }
        }

        private float GetHoverHeight(RunnerEntityKind kind)
        {
            switch (kind)
            {
                case RunnerEntityKind.Firewall: return 0.34f;   // wall sits on the track
                case RunnerEntityKind.MalwareNode: return 0.72f; // floating spiked mine
                case RunnerEntityKind.Sniffer: return 0.30f;     // flat sawblade close to the track
                case RunnerEntityKind.DataShard: return 0.75f;
                default: return 0.75f;
            }
        }

        private float GetScale(RunnerEntityKind kind)
        {
            switch (kind)
            {
                case RunnerEntityKind.Firewall: return 1.1f;
                case RunnerEntityKind.MalwareNode: return 1.15f;
                case RunnerEntityKind.Sniffer: return 1.3f;
                case RunnerEntityKind.DataShard: return 1.0f;
                default: return 1.1f;
            }
        }

        private Vector3 GetSpin(RunnerEntityKind kind)
        {
            switch (kind)
            {
                case RunnerEntityKind.Firewall: return Vector3.zero;
                case RunnerEntityKind.MalwareNode: return new Vector3(25f, 40f, 15f);
                case RunnerEntityKind.Sniffer: return new Vector3(0f, 260f, 0f);
                case RunnerEntityKind.DataShard: return new Vector3(0f, 90f, 0f);
                default: return new Vector3(0f, 0f, 80f);
            }
        }

        private Color GetColor(RunnerEntityKind kind)
        {
            switch (kind)
            {
                case RunnerEntityKind.Firewall: return firewallColor;
                case RunnerEntityKind.MalwareNode: return malwareColor;
                case RunnerEntityKind.Sniffer: return snifferColor;
                case RunnerEntityKind.DataShard: return dataColor;
                default: return patchColor;
            }
        }
    }
}
