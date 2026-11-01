using System.Collections.Generic;
using UnityEngine;

namespace Minigames
{
    /// <summary>
    /// 3D view for <see cref="PatchHuntMinigame"/>: an isometric neon server room built
    /// entirely in code - orthographic camera, light, floor with neon frame, and the
    /// rack grid. Targets erupt from rack tops (scale 0-&gt;1), a shrinking ring drawn
    /// with a LineRenderer shows their remaining life, and the player aims with the
    /// mouse: clicks are resolved with a mathematical ray-sphere test (no physics, no
    /// colliders - the same deterministic pattern as Packet Rush's contact bands).
    ///
    /// Meshes are loaded from <c>Resources/PatchHunt/</c> by name (rack / breach /
    /// honeypot) with Unity-primitive fallbacks, so no hand-authored asset reference
    /// exists on the spawn path and a missing FBX can never empty the room.
    /// Sits on the same GameObject as <see cref="PatchHuntMinigame"/> in the behaviour
    /// prefab; the whole rig is a child of it, so the controller's teardown destroys it.
    /// </summary>
    public class PatchHuntMinigameWorld : MonoBehaviour
    {
        [Header("Rack grid (world units)")]
        [SerializeField, Tooltip("Distance between rack centers along X (columns).")]
        private float rackSpacingX = 2.2f;
        [SerializeField, Tooltip("Distance between rack centers along Z (rows).")]
        private float rackSpacingZ = 2.7f;
        [SerializeField] private float rackWidth = 1.2f;
        [SerializeField, Tooltip("Rack height - also the anchor height targets erupt from.")]
        private float rackHeight = 1.55f;
        [SerializeField] private float rackDepth = 0.95f;

        [Header("Camera")]
        [SerializeField, Tooltip("Isometric pitch - steep enough to read the horizontal timer rings.")]
        private float cameraPitch = 54f;
        [SerializeField, Tooltip("Slight yaw so the racks show two faces.")]
        private float cameraYaw = 27f;
        [SerializeField] private float cameraOrthoSize = 5.1f;
        [SerializeField] private float cameraDistance = 30f;

        [Header("Targets")]
        [SerializeField, Tooltip("Seconds a target takes to grow from the rack (life keeps ticking).")]
        private float growTime = 0.35f;
        [SerializeField, Tooltip("Seconds of the shrink-out after a target resolves.")]
        private float despawnTime = 0.18f;
        [SerializeField, Tooltip("Uniform scale of a fully grown target mesh.")]
        private float targetScale = 0.8f;
        [SerializeField, Tooltip("Height of the target center above the rack top.")]
        private float targetHoverOffset = 0.5f;
        [SerializeField, Tooltip("Click radius of a fully grown target (world units).")]
        private float aimRadius = 0.7f;
        [SerializeField, Tooltip("Click radius never shrinks below this while the target grows.")]
        private float minAimRadius = 0.45f;

        [Header("Timer ring")]
        [SerializeField, Tooltip("Ring radius with full life left.")]
        private float ringMaxRadius = 0.9f;
        [SerializeField, Tooltip("Ring radius the moment life runs out.")]
        private float ringMinRadius = 0.15f;
        [SerializeField] private float ringWidth = 0.05f;
        [SerializeField] private int ringSegments = 40;

        // Same neon language as Packet Rush; colors come only from this palette.
        [Header("Palette")]
        [SerializeField] private Color breachColor = new Color(1f, 0.24f, 0.35f);
        [SerializeField] private Color honeypotColor = new Color(0.24f, 1f, 0.53f);
        [SerializeField, Tooltip("One ring color for BOTH kinds - the ring must never be the tell.")]
        private Color ringColor = new Color(1f, 0.82f, 0.29f);
        [SerializeField] private Color rackColor = new Color(0.07f, 0.12f, 0.2f);
        [SerializeField, Tooltip("Neon status strip on each rack front.")]
        private Color ledColor = new Color(0.13f, 0.96f, 0.88f);
        [SerializeField, Tooltip("Rack flash on a successful patch.")]
        private Color patchFlashColor = new Color(0.13f, 0.96f, 0.88f);
        [SerializeField, Tooltip("Rack flash on a leak or a honeypot click.")]
        private Color warnColor = new Color(1f, 0.35f, 0.6f);
        [SerializeField] private Color floorColor = new Color(0.03f, 0.055f, 0.1f);
        [SerializeField] private Color frameColor = new Color(0.08f, 0.35f, 0.45f);
        [SerializeField] private Color backgroundColor = new Color(0.024f, 0.043f, 0.078f);
        [SerializeField, Tooltip("Multiplier for the emission tint of the neon materials.")]
        private float emissionIntensity = 1.5f;

        [Header("Diagnostics")]
        [SerializeField, Tooltip("Log every spawned target to the console.")]
        private bool logSpawns = false;

        private class WorldTarget
        {
            public PatchHuntTargetKind kind;
            public int rackIndex;
            public Transform root;
            public Transform visual;
            public LineRenderer ring;
            public float lifetime;
            public float life;
            public float age;
            public bool resolved;
            public float despawnTimer;
        }

        private PatchHuntMinigame minigame;
        private Transform worldRoot;
        private Camera worldCamera;

        private readonly List<WorldTarget> targets = new List<WorldTarget>();
        private readonly List<Material> createdMaterials = new List<Material>();

        private Material[] rackMaterials;
        private float[] rackFlashTimers;
        private Color[] rackFlashColors;

        private Material breachMaterial;
        private Material honeypotMaterial;
        private Material ringMaterial;

        public int SpawnedTotal { get; private set; }
        public int LiveCount => targets.Count;

        private void Awake()
        {
            minigame = GetComponent<PatchHuntMinigame>();
        }

        private void Start()
        {
            if (minigame == null)
            {
                Debug.LogError("[PatchHunt] PatchHuntMinigameWorld needs a PatchHuntMinigame on the same GameObject.");
                return;
            }

            BuildWorld();
            Debug.Log("[PatchHunt] 3D world ready - server room grid, camera and materials built procedurally; target meshes from Resources/PatchHunt with primitive fallbacks.");
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

            UpdateRackFlashes(Time.deltaTime);

            if (minigame.IsPaused || minigame.IsFinished)
                return;

            SpawnPendingTargets();
            UpdateTargets(Time.deltaTime);
            HandleClick();
        }

        // ------------------------------------------------------------------ build

        private void BuildWorld()
        {
            worldRoot = new GameObject("PatchHuntWorld").transform;
            worldRoot.SetParent(transform, false);

            BuildCamera();
            BuildLight();
            BuildFloor();
            BuildRacks();

            breachMaterial = CreateNeonMaterial(breachColor, emissionIntensity);
            honeypotMaterial = CreateNeonMaterial(honeypotColor, emissionIntensity);
            ringMaterial = CreateRingMaterial();
        }

        private void BuildCamera()
        {
            GameObject camGo = new GameObject("PatchHuntCamera", typeof(Camera));
            camGo.transform.SetParent(worldRoot, false);

            Quaternion rotation = Quaternion.Euler(cameraPitch, cameraYaw, 0f);
            Vector3 focus = new Vector3(0f, rackHeight * 0.5f, 0f);
            camGo.transform.localRotation = rotation;
            camGo.transform.localPosition = focus - rotation * Vector3.forward * cameraDistance;

            worldCamera = camGo.GetComponent<Camera>();
            worldCamera.orthographic = true;
            worldCamera.orthographicSize = cameraOrthoSize;
            worldCamera.clearFlags = CameraClearFlags.SolidColor;
            worldCamera.backgroundColor = backgroundColor;
            worldCamera.nearClipPlane = 0.3f;
            worldCamera.farClipPlane = cameraDistance * 3f;
            worldCamera.depth = 10f;
        }

        private void BuildLight()
        {
            GameObject lightGo = new GameObject("PatchHuntLight", typeof(Light));
            lightGo.transform.SetParent(worldRoot, false);
            lightGo.transform.localEulerAngles = new Vector3(55f, -32f, 0f);

            Light light = lightGo.GetComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.15f;
            light.color = new Color(0.9f, 0.95f, 1f);
        }

        private void BuildFloor()
        {
            float floorWidth = (minigame.GridColumns - 1) * rackSpacingX + rackWidth + 3.2f;
            float floorDepth = (minigame.GridRows - 1) * rackSpacingZ + rackDepth + 3.2f;

            CreateBox("Floor", worldRoot,
                new Vector3(0f, -0.05f, 0f),
                new Vector3(floorWidth, 0.1f, floorDepth),
                CreateNeonMaterial(floorColor, 0.35f));

            // Neon frame around the room - the "dark room, glowing edges" look.
            Material frameMat = CreateNeonMaterial(frameColor * 1.6f, 1.4f);
            float halfW = floorWidth / 2f;
            float halfD = floorDepth / 2f;

            CreateBox("FrameNorth", worldRoot, new Vector3(0f, 0.03f, halfD), new Vector3(floorWidth, 0.06f, 0.12f), frameMat);
            CreateBox("FrameSouth", worldRoot, new Vector3(0f, 0.03f, -halfD), new Vector3(floorWidth, 0.06f, 0.12f), frameMat);
            CreateBox("FrameEast", worldRoot, new Vector3(halfW, 0.03f, 0f), new Vector3(0.12f, 0.06f, floorDepth), frameMat);
            CreateBox("FrameWest", worldRoot, new Vector3(-halfW, 0.03f, 0f), new Vector3(0.12f, 0.06f, floorDepth), frameMat);

            // Dim aisle lines between the rack rows sell the grid.
            Material aisleMat = CreateNeonMaterial(frameColor, 1.1f);

            for (int row = 0; row < minigame.GridRows - 1; row++)
            {
                float z = (RackZ(row) + RackZ(row + 1)) / 2f;
                CreateBox($"Aisle{row}", worldRoot, new Vector3(0f, 0.02f, z), new Vector3(floorWidth - 1.4f, 0.04f, 0.07f), aisleMat);
            }
        }

        private void BuildRacks()
        {
            int rackCount = minigame.RackCount;
            rackMaterials = new Material[rackCount];
            rackFlashTimers = new float[rackCount];
            rackFlashColors = new Color[rackCount];

            Material ledMat = CreateNeonMaterial(ledColor, 1.6f);

            for (int index = 0; index < rackCount; index++)
            {
                int column = index % minigame.GridColumns;
                int row = index / minigame.GridColumns;
                Vector3 basePosition = new Vector3(RackX(column), 0f, RackZ(row));

                GameObject rack = LoadModel("rack", PrimitiveType.Cube, out bool usedFallback);
                rack.name = $"Rack_{index}";
                rack.transform.SetParent(worldRoot, false);

                if (usedFallback)
                {
                    // Primitive cube pivots at its center; the authored FBX pivots at its base.
                    rack.transform.localScale = new Vector3(rackWidth, rackHeight, rackDepth);
                    rack.transform.localPosition = basePosition + new Vector3(0f, rackHeight / 2f, 0f);
                }
                else
                {
                    rack.transform.localPosition = basePosition;
                }

                rackMaterials[index] = CreateNeonMaterial(rackColor, 0.5f);
                ApplyMaterial(rack, rackMaterials[index]);

                // Thin status strip near the top front edge - every rack reads as "alive".
                // Front = -Z: the isometric camera sits on the -X/-Z side of the room.
                CreateBox($"RackLed_{index}", worldRoot,
                    basePosition + new Vector3(0f, rackHeight - 0.18f, -(rackDepth / 2f + 0.02f)),
                    new Vector3(rackWidth * 0.62f, 0.05f, 0.03f),
                    ledMat);
            }
        }

        // ------------------------------------------------------------ per-frame

        private void SpawnPendingTargets()
        {
            while (minigame.TryDequeueSpawn(out PatchHuntSpawn spawn))
                SpawnTarget(spawn);
        }

        private void SpawnTarget(PatchHuntSpawn spawn)
        {
            int column = spawn.rackIndex % minigame.GridColumns;
            int row = spawn.rackIndex / minigame.GridColumns;

            Transform root = new GameObject($"Target_{spawn.kind}_{spawn.rackIndex}").transform;
            root.SetParent(worldRoot, false);
            root.localPosition = new Vector3(RackX(column), rackHeight + targetHoverOffset, RackZ(row));

            // Both kinds share one model slot per kind but identical scale, spin, hover
            // and ring - color is the only (deliberate) tell.
            string modelName = spawn.kind == PatchHuntTargetKind.Breach ? "breach" : "honeypot";
            GameObject visual = LoadModel(modelName, PrimitiveType.Sphere, out _);
            visual.name = "Visual";
            visual.transform.SetParent(root, false);
            visual.transform.localScale = Vector3.zero;
            visual.transform.localRotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
            ApplyMaterial(visual, spawn.kind == PatchHuntTargetKind.Breach ? breachMaterial : honeypotMaterial);

            LineRenderer ring = BuildRing(root);

            targets.Add(new WorldTarget
            {
                kind = spawn.kind,
                rackIndex = spawn.rackIndex,
                root = root,
                visual = visual.transform,
                ring = ring,
                lifetime = spawn.lifetime,
                life = spawn.lifetime,
            });

            SpawnedTotal++;

            if (logSpawns)
                Debug.Log($"[PatchHunt] spawn #{SpawnedTotal}: {spawn.kind} on rack {spawn.rackIndex} (live: {targets.Count})");
        }

        private LineRenderer BuildRing(Transform parent)
        {
            GameObject ringGo = new GameObject("Ring", typeof(LineRenderer));
            ringGo.transform.SetParent(parent, false);

            LineRenderer ring = ringGo.GetComponent<LineRenderer>();
            ring.useWorldSpace = false;
            ring.loop = true;
            ring.positionCount = ringSegments;
            ring.widthMultiplier = ringWidth;
            ring.sharedMaterial = ringMaterial;
            ring.startColor = ringColor;
            ring.endColor = ringColor;
            ring.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            ring.receiveShadows = false;

            return ring;
        }

        private void UpdateTargets(float deltaTime)
        {
            for (int i = targets.Count - 1; i >= 0; i--)
            {
                WorldTarget target = targets[i];

                if (target.root == null)
                {
                    targets.RemoveAt(i);
                    continue;
                }

                if (target.resolved)
                {
                    // Shrink-out animation, then remove.
                    target.despawnTimer -= deltaTime;
                    float blend = Mathf.Clamp01(target.despawnTimer / despawnTime);
                    target.visual.localScale = Vector3.one * (targetScale * blend);

                    if (target.despawnTimer <= 0f)
                    {
                        targets.RemoveAt(i);
                        Destroy(target.root.gameObject);
                    }

                    continue;
                }

                target.age += deltaTime;
                target.life -= deltaTime;

                float growth = growTime > 0f ? Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(target.age / growTime)) : 1f;
                target.visual.localScale = Vector3.one * (targetScale * growth);
                target.visual.Rotate(0f, 60f * deltaTime, 0f, Space.Self);

                UpdateRing(target);

                if (target.life <= 0f)
                    ResolveTarget(target, clicked: false);
            }
        }

        private void UpdateRing(WorldTarget target)
        {
            if (target.ring == null)
                return;

            float lifeFraction = target.lifetime > 0f ? Mathf.Clamp01(target.life / target.lifetime) : 0f;
            float radius = Mathf.Lerp(ringMinRadius, ringMaxRadius, lifeFraction);

            for (int s = 0; s < ringSegments; s++)
            {
                float angle = s * Mathf.PI * 2f / ringSegments;
                target.ring.SetPosition(s, new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius));
            }
        }

        private void HandleClick()
        {
            if (Input.GetMouseButtonDown(0) == false || worldCamera == null)
                return;

            Ray ray = worldCamera.ScreenPointToRay(Input.mousePosition);
            WorldTarget best = null;
            float bestDistance = float.MaxValue;

            foreach (WorldTarget target in targets)
            {
                if (target.resolved || target.root == null)
                    continue;

                float radius = Mathf.Max(minAimRadius, aimRadius * (target.visual.localScale.x / Mathf.Max(0.0001f, targetScale)));

                if (RayHitsSphere(ray, target.root.position, radius, out float distance) && distance < bestDistance)
                {
                    bestDistance = distance;
                    best = target;
                }
            }

            if (best != null)
                ResolveTarget(best, clicked: true);
        }

        /// <summary>Deterministic aiming - no physics, no colliders, just ray-sphere math.</summary>
        private static bool RayHitsSphere(Ray ray, Vector3 center, float radius, out float distance)
        {
            Vector3 toCenter = center - ray.origin;
            float projection = Vector3.Dot(toCenter, ray.direction);
            distance = projection;

            if (projection < 0f)
                return false;

            float sqDistToRay = toCenter.sqrMagnitude - projection * projection;
            return sqDistToRay <= radius * radius;
        }

        private void ResolveTarget(WorldTarget target, bool clicked)
        {
            target.resolved = true;
            target.despawnTimer = despawnTime;

            if (target.ring != null)
                target.ring.gameObject.SetActive(false);

            float lifeFraction = target.lifetime > 0f ? Mathf.Clamp01(target.life / target.lifetime) : 0f;
            string message = minigame.ResolveTarget(target.kind, target.rackIndex, clicked, lifeFraction);

            bool warn = (target.kind == PatchHuntTargetKind.Breach && clicked == false)
                || (target.kind == PatchHuntTargetKind.Honeypot && clicked);

            if (target.kind == PatchHuntTargetKind.Breach && clicked)
                FlashRack(target.rackIndex, patchFlashColor);
            else if (warn)
                FlashRack(target.rackIndex, warnColor);

            if (string.IsNullOrEmpty(message) == false)
            {
                PatchHuntMinigameUI ui = MinigamesUIManager.Instance != null
                    ? MinigamesUIManager.Instance.ActiveMinigameUI as PatchHuntMinigameUI
                    : null;

                if (ui != null)
                    ui.ShowFeedback(message, warn);
            }
        }

        private void FlashRack(int rackIndex, Color color)
        {
            if (rackMaterials == null || rackIndex < 0 || rackIndex >= rackMaterials.Length)
                return;

            rackFlashTimers[rackIndex] = 0.45f;
            rackFlashColors[rackIndex] = color;
        }

        private void UpdateRackFlashes(float deltaTime)
        {
            if (rackMaterials == null)
                return;

            for (int i = 0; i < rackMaterials.Length; i++)
            {
                if (rackFlashTimers[i] <= 0f || rackMaterials[i] == null)
                    continue;

                rackFlashTimers[i] -= deltaTime;
                float blend = Mathf.Clamp01(rackFlashTimers[i] / 0.45f);
                Color color = Color.Lerp(rackColor, rackFlashColors[i], blend);
                rackMaterials[i].color = color;

                if (rackMaterials[i].HasProperty("_EmissionColor"))
                    rackMaterials[i].SetColor("_EmissionColor", color * emissionIntensity * blend);
            }
        }

        // ------------------------------------------------------------- helpers

        private float RackX(int column) => (column - (minigame.GridColumns - 1) / 2f) * rackSpacingX;

        private float RackZ(int row) => (row - (minigame.GridRows - 1) / 2f) * rackSpacingZ;

        /// <summary>
        /// Loads a Blender/Meshy-authored model from Resources/PatchHunt; degrades to a
        /// Unity primitive so a missing/not-yet-imported mesh can never empty the room.
        /// Colliders are stripped - aiming is deterministic ray-sphere math.
        /// </summary>
        private GameObject LoadModel(string modelName, PrimitiveType fallback, out bool usedFallback)
        {
            GameObject prefab = Resources.Load<GameObject>("PatchHunt/" + modelName);
            GameObject go;

            if (prefab != null)
            {
                go = Instantiate(prefab);
                usedFallback = false;
            }
            else
            {
                Debug.LogWarning($"[PatchHunt] Model 'Resources/PatchHunt/{modelName}' not found - using {fallback} primitive fallback.");
                go = GameObject.CreatePrimitive(fallback);
                usedFallback = true;
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

        private Material CreateNeonMaterial(Color color, float emission)
        {
            Shader shader = Shader.Find("Standard");

            if (shader == null)
                shader = Shader.Find("Legacy Shaders/Diffuse");

            if (shader == null)
            {
                Debug.LogError("[PatchHunt] Neither Standard nor Legacy Diffuse shader found - materials will be missing.");
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

        private Material CreateRingMaterial()
        {
            // LineRenderers read vertex colors with Sprites/Default; the neon material
            // is only the fallback if that shader is somehow stripped from the build.
            Shader shader = Shader.Find("Sprites/Default");

            if (shader == null)
                return CreateNeonMaterial(ringColor, 2f);

            Material material = new Material(shader);
            createdMaterials.Add(material);
            return material;
        }
    }
}
