using System.Collections.Generic;
using UnityEngine;

namespace Minigames
{
    /// <summary>
    /// 3D view for <see cref="CameraDeckMinigame"/>: one procedurally built building shown
    /// through TWO render-texture cameras at once - a perspective CCTV camera standing on
    /// the selected mount, and an orthographic top-down floor-plan camera. The UI overlay
    /// (<see cref="CameraDeckMinigameUI"/>) displays both feeds and forwards clicks as
    /// normalised UV coordinates; everything here resolves them with deterministic math
    /// (ray vs floor plane on the plan, ray-sphere against visible spots on the CCTV feed)
    /// - no physics, no colliders.
    ///
    /// The camera cones drawn on the floor plan are rebuilt with per-ray wall occlusion
    /// (the Data Rover pattern), and the SAME 2D cone test drives every detection and
    /// visibility decision - what the plan shows IS the truth, for all three modes.
    /// Plan-only visuals (cones, route lines, mount icons) live on the built-in
    /// TransparentFX layer, which the CCTV camera culls, so the in-world feed stays diegetic.
    ///
    /// Everything - building, props, cameras, cones, characters, materials - is procedural;
    /// meshes load from <c>Resources/CameraDeck/</c> by path with primitive fallbacks, so a
    /// missing FBX degrades to a box plus a loud <c>[CameraDeck]</c> warning, never to an
    /// empty site. Sits on the same GameObject as the behaviour in all three prefabs.
    /// </summary>
    public partial class CameraDeckMinigameWorld : MonoBehaviour
    {
        private const int PlanOnlyLayer = 1; // built-in TransparentFX - exists in every project

        [Header("Building (world units)")]
        [SerializeField, Tooltip("Edge length of one map cell.")]
        private float cellSize = 1f;
        [SerializeField, Tooltip("Wall height - tall enough to block camera sight lines.")]
        private float wallHeight = 1.6f;

        [Header("CCTV camera")]
        [SerializeField, Tooltip("Lens height above the floor.")]
        private float camHeight = 1.45f;
        [SerializeField, Tooltip("Downward pitch of the lens in degrees.")]
        private float camPitch = 16f;
        // No FOV field on purpose: the feed's field of view is DERIVED from the cone's
        // half-angle in BuildRenderCameras, so the two can never drift apart.
        [SerializeField, Tooltip("CCTV feed resolution (width; height is 9/16 of it).")]
        private int cctvTextureWidth = 1024;
        [SerializeField, Tooltip("Floor plan feed resolution (square).")]
        private int planTextureSize = 640;

        [Header("Cones")]
        [SerializeField, Tooltip("Rays in a cone fan (more = smoother occlusion edge).")]
        private int coneRayCount = 20;
        [SerializeField, Tooltip("Height of the cone fans above the floor (plan-only layer).")]
        private float coneY = 0.06f;

        [Header("Spots / pickups")]
        [SerializeField, Tooltip("Height of spot markers above the floor.")]
        private float spotY = 0.55f;
        [SerializeField, Tooltip("Ray-sphere click radius of a spot marker on the CCTV feed.")]
        private float spotClickRadius = 0.55f;
        [SerializeField, Tooltip("How close (cells) a plan click must be to a mount to select it.")]
        private float mountClickRadiusCells = 0.95f;
        [SerializeField, Tooltip("How close (world units) the operative must get to the asset to raid it.")]
        private float assetReachRadius = 0.7f;

        // Same neon language as the other 3D minigames; colours come only from this palette.
        [Header("Palette")]
        [SerializeField] private Color floorColor = new Color(0.045f, 0.06f, 0.09f);
        [SerializeField] private Color wallColor = new Color(0.13f, 0.2f, 0.3f);
        [SerializeField] private Color gridLineColor = new Color(0.1f, 0.18f, 0.26f);
        [SerializeField] private Color deskColor = new Color(0.25f, 0.33f, 0.42f);
        [SerializeField] private Color rackColor = new Color(0.16f, 0.24f, 0.36f);
        [SerializeField] private Color plantColor = new Color(0.16f, 0.5f, 0.3f);
        [SerializeField] private Color assetColor = new Color(1f, 0.82f, 0.25f);
        [SerializeField] private Color entryColor = new Color(0.24f, 1f, 0.53f);
        [SerializeField] private Color cameraColor = new Color(0.32f, 0.95f, 1f);
        [SerializeField] private Color cameraOffColor = new Color(0.3f, 0.38f, 0.48f);
        [SerializeField] private Color coneColor = new Color(0.32f, 0.95f, 1f);
        [SerializeField] private Color coneActiveColor = new Color(1f, 1f, 1f);
        [SerializeField, Range(0f, 1f)] private float coneAlpha = 0.14f;
        [SerializeField] private Color spotColor = new Color(1f, 0.72f, 0.2f);
        [SerializeField] private Color spotDoneColor = new Color(0.24f, 1f, 0.53f);
        [SerializeField] private Color intruderColor = new Color(1f, 0.36f, 0.28f);
        [SerializeField] private Color avatarColor = new Color(0.32f, 0.95f, 1f);
        [SerializeField] private Color routeLineColor = new Color(0.32f, 0.95f, 1f);
        [SerializeField] private Color revealLineColor = new Color(1f, 0.36f, 0.28f);
        [SerializeField] private Color backgroundColor = new Color(0.012f, 0.025f, 0.05f);
        [SerializeField, Tooltip("Multiplier for the emission tint of the neon materials.")]
        private float emissionIntensity = 1.5f;

        [Header("Diagnostics")]
        [SerializeField, Tooltip("Log installs, reports, detections and runs to the console.")]
        private bool logEvents = false;

        private class DeckCamera
        {
            public DeckCameraMount mount;
            public int yawIndex;
            public float restYaw;
            public bool installed;
            public Vector3 position;      // lens position (world/local under worldRoot)
            public float currentYaw;      // sweeps in Blind Route, static elsewhere
            public float sweepPhase;
            public Transform rig;
            public Transform visualRoot;  // pole + head; hidden while this camera is the live feed
            public Material bodyMaterial;
            public Mesh coneMesh;
            public Material coneMaterial;
            public GameObject coneGo;
            public Transform mountRing;   // plan-only selection/placement icon
            public Material ringMaterial;
        }

        private class DeckSpot
        {
            public Transform root;
            public Material material;
            public DeckSpotDefinition definition;
            public bool resolved;
            public float pulsePhase;
        }

        private sealed class AuditIntruderRun
        {
            public readonly int routeIndex;
            public readonly Transform figure;
            public readonly List<Vector3> path;
            public int nextWaypoint = 1;
            public bool detected;
            public bool completed;

            public AuditIntruderRun(int routeIndex, Transform figure, List<Vector3> path)
            {
                this.routeIndex = routeIndex;
                this.figure = figure;
                this.path = path;
            }
        }

        private CameraDeckMinigame minigame;
        private DeckLayout layout;
        private CameraDeckVisibilityService visibility;
        private Transform worldRoot;

        private Camera cctvCamera;
        private Camera planCamera;
        private RenderTexture cctvTexture;
        private RenderTexture planTexture;

        private readonly List<DeckCamera> cameras = new List<DeckCamera>();
        private readonly List<DeckSpot> spots = new List<DeckSpot>();
        private readonly List<Vector3> coverageTargets = new List<Vector3>();
        private readonly List<Material> createdMaterials = new List<Material>();
        private readonly List<Mesh> createdMeshes = new List<Mesh>();
        private readonly Dictionary<string, Material> sharedMaterials = new Dictionary<string, Material>();

        private int activeCameraIndex;
        private float switchTimer;
        private bool conesDirty = true;

        // Blind Spot Audit test run.
        private bool testRunning;
        private readonly List<AuditIntruderRun> auditIntruders = new List<AuditIntruderRun>();
        private readonly List<LineRenderer> auditResultLines = new List<LineRenderer>();
        private int detectedAuditRoutes;

        // Blind Route.
        private readonly List<Vector3> routeWaypoints = new List<Vector3>();
        private readonly List<Vector2Int> routeCells = new List<Vector2Int>();
        private bool runExecuting;
        private Transform avatar;
        private int avatarNextWaypoint;
        private LineRenderer routeLine;
        private Vector3 entryPosition;
        private Vector3 primaryAssetPosition;

        private string hoveredDescription = string.Empty;
        private bool hoveredVisible;

        public RenderTexture CctvTexture => cctvTexture;
        public RenderTexture PlanTexture => planTexture;
        public int CameraCount => cameras.Count;
        public int ActiveCameraIndex => activeCameraIndex;
        public int InstalledCount
        {
            get
            {
                int count = 0;

                foreach (DeckCamera cam in cameras)
                {
                    if (cam.installed)
                        count++;
                }

                return count;
            }
        }

        public string ActiveCameraLabel =>
            cameras.Count > 0 ? cameras[activeCameraIndex].mount.label : string.Empty;
        public bool ActiveCameraInstalled =>
            cameras.Count > 0 && cameras[activeCameraIndex].installed;
        public int ActiveCameraYawIndex => cameras.Count > 0 ? cameras[activeCameraIndex].yawIndex : 0;
        public int ActiveCameraYawCount => cameras.Count > 0 ? cameras[activeCameraIndex].mount.allowedYaws.Length : 0;
        public float SwitchNoise01 =>
            minigame != null && minigame.SwitchDelaySeconds > 0f
                ? Mathf.Clamp01(switchTimer / minigame.SwitchDelaySeconds)
                : 0f;
        public string HoveredSpotDescription => hoveredVisible ? hoveredDescription : string.Empty;
        public bool IsRunActive => testRunning;
        public int RouteWaypointCount => routeWaypoints.Count;

        private void Awake()
        {
            minigame = GetComponent<CameraDeckMinigame>();
        }

        [ContextMenu("Validate Camera Deck Facilities")]
        private void ValidateFacilities()
        {
            if (CameraDeckFacilityValidator.ValidateAll(out string report))
                Debug.Log("[CameraDeck] " + report);
            else
                Debug.LogError("[CameraDeck] Facility validation failed:\n" + report);
        }

        private void Start()
        {
            if (minigame == null)
            {
                Debug.LogError("[CameraDeck] CameraDeckMinigameWorld needs a CameraDeckMinigame on the same GameObject.");
                return;
            }

            layout = minigame.Layout;
            visibility = new CameraDeckVisibilityService(layout, cellSize);
            modeController = CreateModeController();
            BuildWorld();
            Debug.Log($"[CameraDeck] world ready - mode {minigame.Mode}, layout '{layout.name}', " +
                $"{cameras.Count} mounts, {spots.Count} spots; building/cones procedural, meshes " +
                "from Resources/CameraDeck with primitive fallbacks.");
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

            if (cctvTexture != null)
            {
                cctvTexture.Release();
                Destroy(cctvTexture);
            }

            if (planTexture != null)
            {
                planTexture.Release();
                Destroy(planTexture);
            }
        }

        private void Update()
        {
            if (minigame == null || worldRoot == null)
                return;

            float deltaTime = Time.deltaTime;

            if (switchTimer > 0f)
                switchTimer -= deltaTime;

            AnimateSpots();

            if (minigame.IsPaused || minigame.IsFinished)
                return;

            modeController.Tick(deltaTime);

            if (conesDirty)
            {
                RebuildAllCones();
                conesDirty = false;
            }

            SyncCctvCamera();
        }

        // ------------------------------------------------------------------ build

        private void BuildWorld()
        {
            worldRoot = new GameObject("CameraDeckWorld").transform;
            worldRoot.SetParent(transform, false);

            entryPosition = CellCenter(layout.EntryCell);
            if (minigame.Mode == CameraDeckMode.BlindRoute)
            {
                if (layout.HasPrimaryAsset == false)
                {
                    Debug.LogError($"[CameraDeck] Route facility '{layout.facilityId}' has no target asset.");
                    return;
                }

                primaryAssetPosition = CellCenter(layout.PrimaryAssetCell);
            }

            if (minigame.Mode == CameraDeckMode.BlindRoute)
                ApplyRedTeamPalette();

            BuildRenderCameras();
            BuildLight();
            BuildBuilding();
            BuildProps();
            BuildMounts();
            BuildSpots();
            BuildRouteLines();

            // Audit starts empty-handed. Sweep uses every fixed feed. Route uses every
            // authored continuously rotating room camera; facilities are never shared across modes.
            for (int i = 0; i < cameras.Count; i++)
            {
                bool live = minigame.Mode == CameraDeckMode.CleanDeskSweep
                    || (minigame.Mode == CameraDeckMode.BlindRoute && cameras[i].mount.routeActive);
                SetInstalled(cameras[i], live);
            }

            RecomputeCoverage();
            SelectCamera(0, silent: true);
        }

        /// <summary>
        /// Blind Route is the one mode where the player is the attacker, so it gets its own
        /// look: warm amber ops board instead of the blue defender desk, and camera cones in
        /// hostile red because there they are the threat, not the tool.
        /// </summary>
        private void ApplyRedTeamPalette()
        {
            floorColor = new Color(0.07f, 0.05f, 0.04f);
            wallColor = new Color(0.3f, 0.19f, 0.1f);
            gridLineColor = new Color(0.26f, 0.16f, 0.08f);
            deskColor = new Color(0.4f, 0.29f, 0.18f);
            rackColor = new Color(0.32f, 0.22f, 0.14f);
            plantColor = new Color(0.35f, 0.3f, 0.12f);
            cameraColor = new Color(1f, 0.35f, 0.25f);
            cameraOffColor = new Color(0.42f, 0.3f, 0.24f);
            coneColor = new Color(1f, 0.3f, 0.2f);
            coneActiveColor = new Color(1f, 0.55f, 0.3f);
            coneAlpha = 0.2f;
            avatarColor = new Color(1f, 0.86f, 0.4f);
            routeLineColor = new Color(1f, 0.78f, 0.3f);
            backgroundColor = new Color(0.05f, 0.03f, 0.02f);
        }

        private void BuildRenderCameras()
        {
            // Blind Route is plan-only and fullscreen, so it needs no CCTV feed at all - and
            // the plan is rendered at a higher resolution because it fills the screen.
            bool planOnly = minigame.Mode == CameraDeckMode.BlindRoute;
            int planSize = planOnly ? Mathf.Max(planTextureSize, 1024) : planTextureSize;

            planTexture = new RenderTexture(planSize, planSize, 16)
            {
                name = "PlanFeed",
            };

            if (planOnly == false)
            {
                cctvTexture = new RenderTexture(cctvTextureWidth, cctvTextureWidth * 9 / 16, 16)
                {
                    name = "CctvFeed",
                };
            }

            // Both feed cameras draw into textures, so without this the display would have
            // NO camera rendering to it - Unity then shows its "No cameras rendering"
            // watermark over the whole game. This one only clears the screen (culling mask
            // Nothing); the opaque UI background is what the player actually sees behind
            // the two feeds.
            GameObject screenGo = new GameObject("DeckScreenCamera", typeof(Camera));
            screenGo.transform.SetParent(worldRoot, false);
            Camera screenCamera = screenGo.GetComponent<Camera>();
            screenCamera.clearFlags = CameraClearFlags.SolidColor;
            screenCamera.backgroundColor = backgroundColor;
            screenCamera.cullingMask = 0;
            screenCamera.nearClipPlane = 0.3f;
            screenCamera.farClipPlane = 5f;
            screenCamera.depth = 10f;

            if (planOnly == false)
                BuildCctvCamera();

            BuildPlanCamera();
        }

        private void BuildCctvCamera()
        {
            GameObject cctvGo = new GameObject("CctvCamera", typeof(Camera));
            cctvGo.transform.SetParent(worldRoot, false);
            cctvCamera = cctvGo.GetComponent<Camera>();
            cctvCamera.clearFlags = CameraClearFlags.SolidColor;
            cctvCamera.backgroundColor = backgroundColor;
            // The feed MUST show exactly what the cone claims, or the plan stops being the
            // truth. Unity's fieldOfView is VERTICAL, so at 16:9 a naive 68 would render
            // ~100 degrees horizontally against a 68-degree cone. Pin the aspect and derive
            // the vertical FOV from the cone's horizontal angle instead.
            cctvCamera.aspect = (float)cctvTexture.width / cctvTexture.height;
            cctvCamera.fieldOfView = Camera.HorizontalToVerticalFieldOfView(
                minigame.VisionHalfAngleDegrees * 2f, cctvCamera.aspect);
            cctvCamera.nearClipPlane = 0.08f;
            // Range truth too: a little past the cone so the wall behind it still draws.
            cctvCamera.farClipPlane = minigame.VisionRangeCells * cellSize + cellSize * 2f;
            cctvCamera.targetTexture = cctvTexture;
            // The in-world feed must not show the plan-only overlay (cones, lines, icons).
            cctvCamera.cullingMask = ~((1 << PlanOnlyLayer) | (1 << 5));
        }

        private void BuildPlanCamera()
        {
            GameObject planGo = new GameObject("PlanCamera", typeof(Camera));
            planGo.transform.SetParent(worldRoot, false);
            planGo.transform.localPosition = new Vector3(0f, 24f, 0f);
            planGo.transform.localEulerAngles = new Vector3(90f, 0f, 0f);
            planCamera = planGo.GetComponent<Camera>();
            planCamera.clearFlags = CameraClearFlags.SolidColor;
            planCamera.backgroundColor = backgroundColor;
            planCamera.orthographic = true;
            // Square feed: the half-size must fit the wider of the two building extents.
            planCamera.orthographicSize = Mathf.Max(layout.rows, layout.cols) * cellSize * 0.5f + 0.6f;
            planCamera.nearClipPlane = 1f;
            planCamera.farClipPlane = 60f;
            planCamera.targetTexture = planTexture;
            planCamera.cullingMask = ~(1 << 5);
        }

        private void BuildLight()
        {
            GameObject lightGo = new GameObject("DeckLight", typeof(Light));
            lightGo.transform.SetParent(worldRoot, false);
            lightGo.transform.localEulerAngles = new Vector3(55f, -30f, 0f);

            Light light = lightGo.GetComponent<Light>();
            light.type = LightType.Directional;
            // Dim night-shift light; the neon emission does the readability work.
            light.intensity = 0.85f;
            light.color = new Color(0.75f, 0.85f, 1f);
        }

        private void BuildBuilding()
        {
            float width = layout.cols * cellSize;
            float depth = layout.rows * cellSize;

            CreateBox("Floor", new Vector3(0f, -0.05f, 0f),
                new Vector3(width + 0.6f, 0.1f, depth + 0.6f),
                GetSharedNeonMaterial(floorColor, 0.3f));

            // Faint cell grid so plan distances are readable.
            Material gridMat = GetSharedNeonMaterial(gridLineColor * 0.6f, 0.5f);

            for (int c = 1; c < layout.cols; c++)
            {
                float x = (c - layout.cols * 0.5f) * cellSize;
                CreateBox($"GridV{c}", new Vector3(x, 0.012f, 0f),
                    new Vector3(0.025f, 0.012f, depth), gridMat);
            }

            for (int r = 1; r < layout.rows; r++)
            {
                float z = (layout.rows * 0.5f - r) * cellSize;
                CreateBox($"GridH{r}", new Vector3(0f, 0.012f, z),
                    new Vector3(width, 0.012f, 0.025f), gridMat);
            }

            Material wallMat = GetSharedNeonMaterial(wallColor, 0.55f);

            for (int r = 0; r < layout.rows; r++)
            {
                for (int c = 0; c < layout.cols; c++)
                {
                    if (layout.IsWall(c, r) == false)
                        continue;

                    CreateBox($"Wall_{c}_{r}",
                        new Vector3(WorldX(c), wallHeight * 0.5f, WorldZ(r)),
                        new Vector3(cellSize * 0.99f, wallHeight, cellSize * 0.99f),
                        wallMat);
                }
            }

            // Entry pads + critical asset pedestals.
            Material entryMaterial = GetSharedNeonMaterial(entryColor * 0.75f, 1f);
            foreach (Vector2Int entry in layout.entryCells)
            {
                CreateBox($"EntryPad_{entry.x}_{entry.y}", CellCenter(entry) + new Vector3(0f, 0.02f, 0f),
                    new Vector3(cellSize * 0.9f, 0.02f, cellSize * 0.9f), entryMaterial);
            }

            Material assetMat = GetSharedNeonMaterial(assetColor, emissionIntensity);
            Material assetBaseMaterial = GetSharedNeonMaterial(rackColor, 0.7f);

            foreach (Vector2Int cell in layout.assetCells)
            {
                Vector3 position = CellCenter(cell);
                CreateBox($"AssetBase_{cell.x}_{cell.y}", position + new Vector3(0f, 0.2f, 0f),
                    new Vector3(0.55f, 0.4f, 0.55f), assetBaseMaterial);

                GameObject crystal = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                crystal.name = $"Asset_{cell.x}_{cell.y}";
                Destroy(crystal.GetComponent<Collider>());
                crystal.transform.SetParent(worldRoot, false);
                crystal.transform.localPosition = position + new Vector3(0f, 0.62f, 0f);
                crystal.transform.localScale = Vector3.one * 0.42f;
                ApplyMaterial(crystal, assetMat);
            }
        }

        private void BuildProps()
        {
            Material deskMat = GetSharedNeonMaterial(deskColor, 0.35f);
            Material rackMat = GetSharedNeonMaterial(rackColor, 0.5f);
            Material plantMat = GetSharedNeonMaterial(plantColor, 0.5f);

            foreach (Vector2Int cell in layout.deskCells)
                PlaceProp("desk", cell, PrimitiveType.Cube, new Vector3(0.85f, 0.45f, 0.55f), 0.225f, deskMat);

            foreach (Vector2Int cell in layout.rackCells)
                PlaceProp("rack", cell, PrimitiveType.Cube, new Vector3(0.6f, 1.15f, 0.6f), 0.575f, rackMat);

            foreach (Vector2Int cell in layout.plantCells)
                PlaceProp("plant", cell, PrimitiveType.Capsule, new Vector3(0.35f, 0.4f, 0.35f), 0.4f, plantMat);
        }

        private void PlaceProp(string modelName, Vector2Int cell, PrimitiveType fallback,
            Vector3 fallbackScale, float fallbackY, Material material)
        {
            GameObject go = LoadModel(modelName, fallback, out bool usedFallback);
            go.name = $"Prop_{modelName}_{cell.x}_{cell.y}";
            go.transform.SetParent(worldRoot, false);

            if (usedFallback)
            {
                go.transform.localScale = fallbackScale;
                go.transform.localPosition = CellCenter(cell) + new Vector3(0f, fallbackY, 0f);
            }
            else
            {
                go.transform.localPosition = CellCenter(cell);
            }

            go.transform.localRotation = Quaternion.Euler(0f, Random.Range(0, 4) * 90f, 0f);
            ApplyMaterial(go, material);
        }

        private void BuildMounts()
        {
            foreach (DeckCameraMount mount in layout.mounts)
            {
                float aim = mount.allowedYaws[0];

                DeckCamera cam = new DeckCamera
                {
                    mount = mount,
                    yawIndex = 0,
                    restYaw = aim,
                    currentYaw = aim,
                    sweepPhase = mount.sweepPhase,
                    position = CellCenter(mount.cell) + new Vector3(0f, camHeight, 0f),
                };

                cam.rig = new GameObject($"Cam_{cameras.Count + 1}_{mount.label}").transform;
                cam.rig.SetParent(worldRoot, false);
                cam.rig.localPosition = CellCenter(mount.cell);
                cam.rig.localRotation = Quaternion.Euler(0f, aim, 0f);

                // Pole + head live under one node so the ACTIVE camera can hide its own body:
                // the lens sits inside this housing, and without this the feed is just a
                // full-screen slab of camera casing.
                cam.visualRoot = new GameObject("Visual").transform;
                cam.visualRoot.SetParent(cam.rig, false);

                cam.bodyMaterial = CreateNeonMaterial(cameraOffColor, 0.8f);
                CreateBox("Pole", new Vector3(0f, camHeight * 0.5f, 0f),
                    new Vector3(0.08f, camHeight, 0.08f), cam.bodyMaterial, cam.visualRoot);

                GameObject head = LoadModel("cctv", PrimitiveType.Cube, out bool usedFallback);
                head.name = "Head";
                head.transform.SetParent(cam.visualRoot, false);
                head.transform.localPosition = new Vector3(0f, camHeight, 0.08f);
                head.transform.localRotation = Quaternion.Euler(camPitch, 0f, 0f);

                if (usedFallback)
                    head.transform.localScale = new Vector3(0.16f, 0.14f, 0.3f);

                ApplyMaterial(head, cam.bodyMaterial);

                // Cone fan (plan-only layer) rebuilt with per-ray occlusion.
                cam.coneGo = new GameObject("Cone", typeof(MeshFilter), typeof(MeshRenderer));
                cam.coneGo.layer = PlanOnlyLayer;
                cam.coneGo.transform.SetParent(worldRoot, false);
                cam.coneGo.transform.localPosition = CellCenter(mount.cell) + new Vector3(0f, coneY, 0f);

                cam.coneMesh = new Mesh { name = "CamConeMesh" };
                cam.coneMesh.MarkDynamic();
                createdMeshes.Add(cam.coneMesh);
                cam.coneGo.GetComponent<MeshFilter>().sharedMesh = cam.coneMesh;
                cam.coneMaterial = CreateConeMaterial();
                cam.coneGo.GetComponent<MeshRenderer>().sharedMaterial = cam.coneMaterial;

                // Plan-only mount ring: placement slot in the Audit, selection halo elsewhere.
                GameObject ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                ring.name = "MountRing";
                ring.layer = PlanOnlyLayer;
                Destroy(ring.GetComponent<Collider>());
                ring.transform.SetParent(worldRoot, false);
                ring.transform.localPosition = CellCenter(mount.cell) + new Vector3(0f, 0.03f, 0f);
                ring.transform.localScale = new Vector3(0.72f, 0.015f, 0.72f);
                cam.mountRing = ring.transform;
                cam.ringMaterial = CreateNeonMaterial(cameraOffColor, 1f);
                ApplyMaterial(ring, cam.ringMaterial);

                cameras.Add(cam);
            }

            // Coverage targets: every asset and every authored entry.
            coverageTargets.Clear();

            foreach (Vector2Int cell in layout.assetCells)
                coverageTargets.Add(CellCenter(cell));

            foreach (Vector2Int entry in layout.entryCells)
                coverageTargets.Add(CellCenter(entry));
        }

        private void BuildSpots()
        {
            if (minigame.Mode != CameraDeckMode.CleanDeskSweep)
                return;

            List<DeckInspectionAnchor> anchors = new List<DeckInspectionAnchor>();
            foreach (DeckInspectionAnchor anchor in layout.inspectionAnchors)
            {
                if (AnyCameraCovers(CellCenter(anchor.cell)))
                    anchors.Add(anchor);
                else
                    Debug.LogError($"[CameraDeck] Inspection anchor {anchor.prop} at {anchor.cell} is not visible from any feed.");
            }

            for (int i = anchors.Count - 1; i > 0; i--)
            {
                int swap = Random.Range(0, i + 1);
                (anchors[i], anchors[swap]) = (anchors[swap], anchors[i]);
            }

            int requested = Mathf.Min(anchors.Count, minigame.TotalViolations + minigame.TotalDecoys);
            for (int i = 0; i < requested; i++)
            {
                DeckInspectionAnchor anchor = anchors[i];
                bool genuine = i < minigame.TotalViolations;
                DeckSpotDefinition definition = new DeckSpotDefinition(
                    genuine ? anchor.genuineFacts : anchor.compliantFacts, genuine, anchor.prop);
                Transform root = BuildInspectionProp(anchor, i);
                Material markerMaterial = CreateNeonMaterial(spotColor * 0.65f, 0.65f);

                GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                marker.name = "NeutralInspectionMarker";
                Destroy(marker.GetComponent<Collider>());
                marker.transform.SetParent(root, false);
                marker.transform.localPosition = new Vector3(0f, spotY, 0f);
                marker.transform.localScale = Vector3.one * 0.16f;
                ApplyMaterial(marker, markerMaterial);

                spots.Add(new DeckSpot
                {
                    root = marker.transform,
                    material = markerMaterial,
                    definition = definition,
                    pulsePhase = Random.Range(0f, Mathf.PI * 2f),
                });
            }
        }

        private Transform BuildInspectionProp(DeckInspectionAnchor anchor, int index)
        {
            Transform root = new GameObject($"Inspection_{index}_{anchor.prop}").transform;
            root.SetParent(worldRoot, false);
            root.localPosition = CellCenter(anchor.cell);
            Material neutral = GetSharedNeonMaterial(deskColor, 0.3f);
            Material detail = GetSharedNeonMaterial(rackColor, 0.45f);

            switch (anchor.prop)
            {
                case DeckInspectionProp.Monitor:
                    CreateBox("Desk", new Vector3(0f, 0.15f, 0f), new Vector3(0.72f, 0.3f, 0.46f), neutral, root);
                    CreateBox("Monitor", new Vector3(0f, 0.43f, -0.08f), new Vector3(0.4f, 0.22f, 0.06f), detail, root);
                    break;
                case DeckInspectionProp.Note:
                    CreateBox("Desk", new Vector3(0f, 0.15f, 0f), new Vector3(0.72f, 0.3f, 0.46f), neutral, root);
                    CreateBox("Note", new Vector3(0.18f, 0.31f, -0.06f), new Vector3(0.18f, 0.02f, 0.14f), detail, root);
                    break;
                case DeckInspectionProp.Printout:
                    CreateBox("Printer", new Vector3(0f, 0.18f, 0f), new Vector3(0.58f, 0.36f, 0.46f), neutral, root);
                    CreateBox("Paper", new Vector3(0f, 0.37f, -0.08f), new Vector3(0.3f, 0.02f, 0.3f), detail, root);
                    break;
                case DeckInspectionProp.Badge:
                    CreateBox("Desk", new Vector3(0f, 0.15f, 0f), new Vector3(0.72f, 0.3f, 0.46f), neutral, root);
                    CreateBox("Badge", new Vector3(0f, 0.32f, 0f), new Vector3(0.16f, 0.025f, 0.22f), detail, root);
                    break;
                case DeckInspectionProp.UsbDrive:
                    CreateBox("ServiceCart", new Vector3(0f, 0.17f, 0f), new Vector3(0.64f, 0.34f, 0.44f), neutral, root);
                    CreateBox("UsbDrive", new Vector3(0.15f, 0.36f, 0f), new Vector3(0.12f, 0.04f, 0.24f), detail, root);
                    break;
                case DeckInspectionProp.Rack:
                    CreateBox("LowServerCabinet", new Vector3(0f, 0.28f, 0f), new Vector3(0.7f, 0.56f, 0.54f), neutral, root);
                    CreateBox("StatusPanel", new Vector3(0f, 0.31f, -0.29f), new Vector3(0.48f, 0.24f, 0.04f), detail, root);
                    break;
                default:
                    CreateBox("AccessConsole", new Vector3(0f, 0.2f, 0f), new Vector3(0.65f, 0.4f, 0.42f), neutral, root);
                    CreateBox("DoorStatus", new Vector3(0f, 0.42f, -0.12f), new Vector3(0.28f, 0.08f, 0.08f), detail, root);
                    break;
            }

            return root;
        }

        private bool AnyCameraCovers(Vector3 position)
        {
            foreach (DeckCamera cam in cameras)
            {
                if (ConeCovers(cam, position))
                    return true;
            }

            return false;
        }

        private void BuildRouteLines()
        {
            if (minigame.Mode != CameraDeckMode.BlindRoute)
                return;

            routeLine = CreatePlanLine("RouteLine", routeLineColor, 0.1f);
            // The operative waits at the entry until the player selects a destination.
            avatar = SpawnFigure("Operative", avatarColor);
            avatar.localPosition = entryPosition;
            RefreshRouteLine();
        }

        private LineRenderer CreatePlanLine(string lineName, Color color, float width)
        {
            GameObject go = new GameObject(lineName, typeof(LineRenderer));
            go.layer = PlanOnlyLayer;
            go.transform.SetParent(worldRoot, false);

            LineRenderer line = go.GetComponent<LineRenderer>();
            line.useWorldSpace = false;
            line.positionCount = 0;
            line.widthMultiplier = width;
            line.sharedMaterial = CreateLineMaterial();
            line.startColor = color;
            line.endColor = color;
            line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            line.receiveShadows = false;
            return line;
        }

        private Transform SpawnFigure(string figureName, Color color)
        {
            GameObject go = LoadModel("figure", PrimitiveType.Capsule, out bool usedFallback);
            go.name = figureName;
            go.transform.SetParent(worldRoot, false);

            if (usedFallback)
                go.transform.localScale = new Vector3(0.42f, 0.55f, 0.42f);

            Vector3 position = go.transform.localPosition;
            position.y = usedFallback ? 0.55f : 0f;
            go.transform.localPosition = position;

            ApplyMaterial(go, CreateNeonMaterial(color, emissionIntensity));

            // Re-root so the figure's ground point is at y=0 regardless of the mesh.
            Transform rig = new GameObject(figureName + "Rig").transform;
            rig.SetParent(worldRoot, false);
            go.transform.SetParent(rig, false);
            return rig;
        }

        // ------------------------------------------------------------ camera control

        private void HandleCameraKeys()
        {
            if (cameras.Count == 0)
                return;

            for (int i = 0; i < cameras.Count && i < 9; i++)
            {
                if (Input.GetKeyDown(KeyCode.Alpha1 + i))
                    SelectCamera(i);
            }

            if (Input.GetKeyDown(KeyCode.Q))
                SelectCamera((activeCameraIndex + cameras.Count - 1) % cameras.Count);

            if (Input.GetKeyDown(KeyCode.E))
                SelectCamera((activeCameraIndex + 1) % cameras.Count);
        }

        public void SelectCamera(int index, bool silent = false)
        {
            if (cameras.Count == 0)
                return;

            index = Mathf.Clamp(index, 0, cameras.Count - 1);

            if (index == activeCameraIndex && silent == false)
                return;

            activeCameraIndex = index;

            // The switch costs a beat of static - hopping blindly between feeds is not free.
            if (silent == false)
                switchTimer = minigame.SwitchDelaySeconds;

            RefreshMountVisuals();
            SyncCctvCamera();
            conesDirty = true;

            if (logEvents)
                Debug.Log($"[CameraDeck] switched to CAM-{index + 1:00} ({cameras[index].mount.label})");
        }

        public void ToggleActiveCameraInstallation()
        {
            ToggleCameraInstallation(activeCameraIndex);
        }

        public void RotateActiveCamera(int direction)
        {
            RotateCamera(activeCameraIndex, direction);
        }

        public bool IsCameraInstalled(int index)
        {
            return index >= 0 && index < cameras.Count && cameras[index].installed;
        }

        public int GetCameraYawDegrees(int index)
        {
            return index >= 0 && index < cameras.Count
                ? Mathf.RoundToInt(Mathf.Repeat(cameras[index].currentYaw, 360f))
                : 0;
        }

        public void ToggleCameraInstallation(int index)
        {
            if (minigame.Mode != CameraDeckMode.BlindSpotAudit || testRunning
                || index < 0 || index >= cameras.Count)
                return;

            SelectCamera(index);
            ToggleInstall(index);
        }

        public void RotateCamera(int index, int direction)
        {
            if (minigame.Mode != CameraDeckMode.BlindSpotAudit || testRunning
                || index < 0 || index >= cameras.Count)
                return;

            SelectCamera(index);
            DeckCamera cam = cameras[index];
            int count = cam.mount.allowedYaws.Length;
            cam.yawIndex = (cam.yawIndex + direction % count + count) % count;
            cam.restYaw = cam.mount.allowedYaws[cam.yawIndex];
            cam.currentYaw = cam.restYaw;
            cam.rig.localRotation = Quaternion.Euler(0f, cam.currentYaw, 0f);
            conesDirty = true;
            RecomputeCoverage();
            SyncCctvCamera();
            PushFeedback($"{cam.mount.label} orientation {Mathf.RoundToInt(cam.currentYaw):000} degrees ({cam.yawIndex + 1}/{count}).", false);
        }

        private void SyncCctvCamera()
        {
            if (cameras.Count == 0 || cctvCamera == null)
                return;

            DeckCamera cam = cameras[activeCameraIndex];
            cctvCamera.transform.localPosition = cam.position;
            cctvCamera.transform.localRotation = Quaternion.Euler(camPitch, cam.currentYaw, 0f);
        }

        private void UpdateSweep(float deltaTime)
        {
            float speed = minigame.SweepSpeedDegPerSec;
            if (speed <= 0f)
                return;

            foreach (DeckCamera cam in cameras)
            {
                if (cam.mount.continuousRotation)
                {
                    cam.currentYaw = cam.restYaw + Time.time * speed + cam.sweepPhase;
                    cam.rig.localRotation = Quaternion.Euler(0f, cam.currentYaw, 0f);
                    continue;
                }

                float arc = cam.mount.sweepArcDegrees;
                if (arc <= 0f)
                    continue;

                // Triangle wave: constant angular speed, hard turnarounds - readable timing.
                float phase = Time.time * speed / arc + cam.sweepPhase;
                float offset = (Mathf.PingPong(phase, 2f) - 1f) * arc;
                cam.currentYaw = cam.restYaw + offset;
                cam.rig.localRotation = Quaternion.Euler(0f, cam.currentYaw, 0f);
            }
        }

        private void SetInstalled(DeckCamera cam, bool installed)
        {
            cam.installed = installed;
            cam.coneGo.SetActive(installed);
            RefreshMountVisuals();
            conesDirty = true;
        }

        private void RefreshMountVisuals()
        {
            for (int i = 0; i < cameras.Count; i++)
            {
                DeckCamera cam = cameras[i];
                bool isActive = i == activeCameraIndex;
                bool emphasizeActive = isActive && minigame.Mode != CameraDeckMode.BlindRoute;

                // The active housing remains visible on the plan but moves to the plan-only
                // layer, so it cannot occlude the feed rendered from inside that housing.
                if (cam.visualRoot != null)
                    SetLayerRecursively(cam.visualRoot.gameObject, isActive ? PlanOnlyLayer : 0);

                Color body = cam.installed ? cameraColor : cameraOffColor;
                cam.bodyMaterial.color = body;

                if (cam.bodyMaterial.HasProperty("_EmissionColor"))
                    cam.bodyMaterial.SetColor("_EmissionColor", body * (cam.installed ? emissionIntensity : 0.35f));

                Color ring = emphasizeActive ? coneActiveColor : (cam.installed ? cameraColor : cameraOffColor);
                cam.ringMaterial.color = ring;

                if (cam.ringMaterial.HasProperty("_EmissionColor"))
                    cam.ringMaterial.SetColor("_EmissionColor", ring * (emphasizeActive ? 1.6f : 0.8f));

                if (cam.coneMaterial != null)
                {
                    Color cone = emphasizeActive ? coneActiveColor : coneColor;
                    cone.a = coneAlpha + (emphasizeActive ? 0.08f : 0f);
                    cam.coneMaterial.color = cone;
                }
            }
        }

        // ------------------------------------------------------------ cones / sight

        private void RebuildAllCones()
        {
            float range = minigame.VisionRangeCells * cellSize;
            float halfAngle = minigame.VisionHalfAngleDegrees;

            foreach (DeckCamera cam in cameras)
            {
                if (cam.installed)
                    RebuildConeMesh(cam, range, halfAngle);
            }
        }

        private void RebuildConeMesh(DeckCamera cam, float range, float halfAngle)
        {
            int rays = Mathf.Max(6, coneRayCount);
            Vector3 origin = cam.coneGo.transform.localPosition;
            origin.y = 0f;

            Vector3[] vertices = new Vector3[rays + 2];
            int[] triangles = new int[rays * 3];
            vertices[0] = Vector3.zero;

            for (int i = 0; i <= rays; i++)
            {
                float angle = -halfAngle + (halfAngle * 2f) * i / rays;
                Vector3 worldDir = Quaternion.Euler(0f, cam.currentYaw + angle, 0f) * Vector3.forward;
                float distance = CastVisionRay(origin, worldDir, range);
                Vector3 local = worldDir * distance;
                vertices[i + 1] = new Vector3(local.x, 0f, local.z);
            }

            for (int i = 0; i < rays; i++)
            {
                triangles[i * 3] = 0;
                triangles[i * 3 + 1] = i + 1;
                triangles[i * 3 + 2] = i + 2;
            }

            cam.coneMesh.Clear();
            cam.coneMesh.vertices = vertices;
            cam.coneMesh.triangles = triangles;
            cam.coneMesh.RecalculateBounds();
        }

        /// <summary>
        /// Steps along a ray until it hits a wall cell; returns the free distance. The one
        /// sight function drives cone fans, coverage, spot visibility and every detection -
        /// so the plan visuals and the rules can never disagree.
        /// </summary>
        private float CastVisionRay(Vector3 origin, Vector3 direction, float maxDistance)
        {
            return visibility.CastVisionRay(origin, direction, maxDistance);
        }

        /// <summary>The single cone test: installed, plus the geometry.</summary>
        private bool ConeSees(DeckCamera cam, Vector3 position)
        {
            return cam.installed && ConeCovers(cam, position);
        }

        /// <summary>
        /// Pure geometry - range + angle + wall line-of-sight, ignoring whether a camera is
        /// installed. Spot placement needs this before anything is installed, so that a
        /// violation is never dealt somewhere no camera could ever see (= unwinnable sweep).
        /// </summary>
        private bool ConeCovers(DeckCamera cam, Vector3 position)
        {
            return visibility.ConeCovers(cam.mount.cell, cam.currentYaw, position,
                minigame.VisionRangeCells, minigame.VisionHalfAngleDegrees);
        }

        private void RecomputeCoverage()
        {
            if (minigame.Mode != CameraDeckMode.BlindSpotAudit)
                return;

            int covered = 0;

            foreach (Vector3 target in coverageTargets)
            {
                foreach (DeckCamera cam in cameras)
                {
                    if (ConeSees(cam, target))
                    {
                        covered++;
                        break;
                    }
                }
            }

            minigame.SetCoverage(covered);
        }

        // ------------------------------------------------------------ plan input

        /// <summary>UI forwards a left-click on the floor-plan feed as a 0..1 UV.</summary>
        public void ClickPlan(Vector2 uv)
        {
            if (minigame.IsPaused || minigame.IsFinished || planCamera == null)
                return;

            if (TryPlanPoint(uv, out Vector3 point) == false)
                return;

            modeController.ClickPlan(point);
        }

        /// <summary>UI forwards a right-click on the plan: undo (Route) / no-op elsewhere.</summary>
        public void RightClickPlan()
        {
            if (minigame.IsPaused || minigame.IsFinished)
                return;

            modeController.RightClickPlan();
        }

        private bool TryPlanPoint(Vector2 uv, out Vector3 point)
        {
            Ray ray = planCamera.ViewportPointToRay(new Vector3(uv.x, uv.y, 0f));
            point = Vector3.zero;

            if (Mathf.Abs(ray.direction.y) < 0.0001f)
                return false;

            float t = -ray.origin.y / ray.direction.y;

            if (t < 0f)
                return false;

            point = ray.origin + ray.direction * t;
            point = worldRoot.InverseTransformPoint(point);
            point.y = 0f;
            return true;
        }

        private int FindMountNear(Vector3 point)
        {
            float best = mountClickRadiusCells * cellSize;
            int bestIndex = -1;

            for (int i = 0; i < cameras.Count; i++)
            {
                float distance = FlatDistance(CellCenter(cameras[i].mount.cell), point);

                if (distance < best)
                {
                    best = distance;
                    bestIndex = i;
                }
            }

            return bestIndex;
        }

        private void ToggleInstall(int index)
        {
            DeckCamera cam = cameras[index];

            if (cam.installed)
            {
                SetInstalled(cam, false);
                RecomputeCoverage();
                PushFeedback($"Camera removed from {cam.mount.label}. BUDGET {InstalledCount}/{minigame.CameraBudget}", false);
            }
            else if (InstalledCount < minigame.CameraBudget)
            {
                SetInstalled(cam, true);
                RecomputeCoverage();
                PushFeedback($"Camera installed at {cam.mount.label}. BUDGET {InstalledCount}/{minigame.CameraBudget}   •   COVERAGE {minigame.CoveredTargets}/{minigame.TotalTargets}", false);
            }
            else
            {
                PushFeedback($"Budget exhausted ({minigame.CameraBudget} cameras). Remove one first - placement beats quantity.", true);
            }

            if (logEvents)
                Debug.Log($"[CameraDeck] install toggle {cam.mount.label} -> {cam.installed}, coverage {minigame.CoveredTargets}/{minigame.TotalTargets}");
        }

        private void SetRouteDestination(Vector3 point)
        {
            Vector2Int destination = new Vector2Int(ColAt(point.x), RowAt(point.z));
            if (layout.IsWalkable(destination) == false)
            {
                PushFeedback("Destination is inside a wall.", true);
                return;
            }

            Vector3 current = avatar != null ? avatar.localPosition : entryPosition;
            Vector2Int start = new Vector2Int(
                Mathf.Clamp(Mathf.RoundToInt(current.x / cellSize + layout.cols * 0.5f - 0.5f), 0, layout.cols - 1),
                Mathf.Clamp(Mathf.RoundToInt(layout.rows * 0.5f - current.z / cellSize - 0.5f), 0, layout.rows - 1));

            if (CameraDeckRoutePathfinder.TryFindPath(layout, start, destination, routeCells) == false)
            {
                PushFeedback("No walkable route to that destination.", true);
                return;
            }

            routeWaypoints.Clear();
            for (int i = 1; i < routeCells.Count; i++)
                routeWaypoints.Add(CellCenter(routeCells[i]));

            avatarNextWaypoint = 0;
            runExecuting = routeWaypoints.Count > 0;
            RefreshRouteLine();
            PushFeedback(runExecuting ? "Moving — right-click or press Space to stop." : "Holding position.", false);
        }

        private void StopRouteMovement(string message)
        {
            runExecuting = false;
            routeWaypoints.Clear();
            routeCells.Clear();
            avatarNextWaypoint = 0;
            RefreshRouteLine();
            if (string.IsNullOrEmpty(message) == false)
                PushFeedback(message, false);
        }

        private void RefreshRouteLine()
        {
            if (routeLine == null)
                return;

            Vector3 origin = avatar != null ? avatar.localPosition : entryPosition;
            routeLine.positionCount = routeWaypoints.Count + 1;
            routeLine.SetPosition(0, origin + new Vector3(0f, coneY + 0.02f, 0f));

            for (int i = 0; i < routeWaypoints.Count; i++)
                routeLine.SetPosition(i + 1, routeWaypoints[i] + new Vector3(0f, coneY + 0.02f, 0f));
        }

        // ------------------------------------------------------------ CCTV input

        /// <summary>UI forwards pointer hover over the CCTV feed; used to read spot labels.</summary>
        public void HoverCctv(Vector2 uv)
        {
            hoveredVisible = false;
            hoveredDescription = string.Empty;

            if (minigame.Mode != CameraDeckMode.CleanDeskSweep || cctvCamera == null)
                return;

            // The UI calls this with an off-rect UV when the pointer leaves the feed.
            if (uv.x < 0f || uv.x > 1f || uv.y < 0f || uv.y > 1f)
                return;

            DeckSpot spot = FindSpotOnFeed(uv);

            if (spot != null)
            {
                hoveredVisible = true;
                hoveredDescription = spot.definition.description;
            }
        }

        /// <summary>UI forwards a left-click on the CCTV feed as a 0..1 UV.</summary>
        public void ClickCctv(Vector2 uv)
        {
            if (minigame.IsPaused || minigame.IsFinished)
                return;

            if (minigame.Mode != CameraDeckMode.CleanDeskSweep)
                return;

            DeckSpot spot = FindSpotOnFeed(uv);

            if (spot == null)
                return;

            string message = minigame.ReportSpot(spot.definition.genuine, spot.definition.description);
            spot.resolved = true;

            Color done = spot.definition.genuine ? spotDoneColor : intruderColor;
            spot.material.color = done;

            if (spot.material.HasProperty("_EmissionColor"))
                spot.material.SetColor("_EmissionColor", done * emissionIntensity);

            PushFeedback(message, spot.definition.genuine == false);

            if (logEvents)
                Debug.Log($"[CameraDeck] reported spot '{spot.definition.description}' genuine={spot.definition.genuine}");
        }

        /// <summary>
        /// Ray-sphere pick against the spots the ACTIVE camera can genuinely see - during
        /// switch static you are blind, and you can never click through a wall.
        /// </summary>
        private DeckSpot FindSpotOnFeed(Vector2 uv)
        {
            if (SwitchNoise01 > 0.2f || cameras.Count == 0)
                return null;

            DeckCamera active = cameras[activeCameraIndex];
            Ray ray = cctvCamera.ViewportPointToRay(new Vector3(uv.x, uv.y, 0f));
            DeckSpot best = null;
            float bestDistance = float.MaxValue;

            foreach (DeckSpot spot in spots)
            {
                if (spot.resolved || spot.root == null)
                    continue;

                Vector3 position = spot.root.position;

                if (ConeSees(active, worldRoot.InverseTransformPoint(position)) == false)
                    continue;

                if (RayHitsSphere(ray, position, spotClickRadius, out float distance)
                    && distance < bestDistance)
                {
                    bestDistance = distance;
                    best = spot;
                }
            }

            return best;
        }

        // ------------------------------------------------------------ action button

        public bool ActionAvailable
        {
            get
            {
                if (minigame.IsFinished || IsRunActive)
                    return false;
                return minigame.Mode == CameraDeckMode.BlindSpotAudit && InstalledCount > 0;
            }
        }

        public string ActionLabel => "RUN AUDIT";

        /// <summary>The Audit action button launches all authored threat routes.</summary>
        public void TriggerAction()
        {
            if (ActionAvailable == false)
                return;

            if (minigame.Mode == CameraDeckMode.BlindSpotAudit)
                StartTestRun();
        }

        // ------------------------------------------------------------ audit test run

        private void StartTestRun()
        {
            ClearAuditResultLines();
            ClearAuditIntruders();
            detectedAuditRoutes = 0;

            for (int routeIndex = 0; routeIndex < layout.intrusionRoutes.Count; routeIndex++)
            {
                DeckIntrusionRoute route = layout.intrusionRoutes[routeIndex];
                List<Vector3> path = new List<Vector3>(route.waypoints.Count);
                foreach (Vector2Int cell in route.waypoints)
                    path.Add(CellCenter(cell));

                Transform figure = SpawnFigure($"TestIntruder_{routeIndex}", intruderColor);
                figure.localPosition = path[0];
                auditIntruders.Add(new AuditIntruderRun(routeIndex, figure, path));
            }

            testRunning = auditIntruders.Count > 0;
            PushFeedback($"RUN AUDIT — {auditIntruders.Count} threat routes active simultaneously.", false);
        }

        private void UpdateTestRun(float deltaTime)
        {
            float speed = minigame.IntruderSpeedCells * cellSize;
            bool allRoutesComplete = true;

            foreach (AuditIntruderRun run in auditIntruders)
            {
                if (run.completed)
                    continue;

                AdvanceAuditIntruder(run, speed * deltaTime);
                if (run.completed == false)
                    allRoutesComplete = false;
            }

            if (allRoutesComplete)
                CompleteAuditRun();
        }

        private void AdvanceAuditIntruder(AuditIntruderRun run, float movementDistance)
        {
            Vector3 position = run.figure.localPosition;
            float remainingDistance = movementDistance;

            while (remainingDistance > 0f && run.nextWaypoint < run.path.Count)
            {
                Vector3 target = run.path[run.nextWaypoint];
                Vector3 previous = position;
                float distance = Vector3.Distance(position, target);

                if (distance <= remainingDistance)
                {
                    position = target;
                    remainingDistance -= distance;
                    run.nextWaypoint++;
                }
                else
                {
                    position = Vector3.MoveTowards(position, target, remainingDistance);
                    remainingDistance = 0f;
                }

                if (run.detected == false && AuditSegmentIsDetected(previous, position))
                {
                    run.detected = true;
                    PushFeedback($"{layout.intrusionRoutes[run.routeIndex].label} detected — tracking to destination.", false);
                }
            }

            run.figure.localPosition = position;
            if (run.nextWaypoint >= run.path.Count)
                CompleteAuditRoute(run);
        }

        private bool AuditSegmentIsDetected(Vector3 from, Vector3 to)
        {
            int samples = Mathf.Max(1, Mathf.CeilToInt(Vector3.Distance(from, to) / (cellSize * 0.25f)));
            for (int sample = 0; sample <= samples; sample++)
            {
                Vector3 position = Vector3.Lerp(from, to, sample / (float)samples);
                foreach (DeckCamera cam in cameras)
                {
                    if (ConeSees(cam, position))
                        return true;
                }
            }

            return false;
        }

        private void CompleteAuditRoute(AuditIntruderRun run)
        {
            run.completed = true;
            if (run.detected)
                detectedAuditRoutes++;

            LineRenderer resultLine = CreatePlanLine(
                $"AuditRoute_{run.routeIndex}",
                run.detected ? spotDoneColor : revealLineColor,
                run.detected ? 0.07f : 0.11f);
            resultLine.positionCount = run.path.Count;
            for (int i = 0; i < run.path.Count; i++)
                resultLine.SetPosition(i, run.path[i] + new Vector3(0f, coneY + 0.03f, 0f));
            auditResultLines.Add(resultLine);

            if (run.figure != null)
                Destroy(run.figure.gameObject);
        }

        private void CompleteAuditRun()
        {
            testRunning = false;
            string message = minigame.CompleteAudit(
                detectedAuditRoutes, layout.intrusionRoutes.Count, InstalledCount);
            PushFeedback(message, minigame.IsFinished == false || minigame.AuditRating < minigame.AuditPassingRating);
            auditIntruders.Clear();

            if (logEvents)
                Debug.Log($"[CameraDeck] audit complete — {detectedAuditRoutes}/{layout.intrusionRoutes.Count}, rating {minigame.AuditRating}.");
        }

        private void ClearAuditIntruders()
        {
            foreach (AuditIntruderRun run in auditIntruders)
            {
                if (run.figure != null)
                    Destroy(run.figure.gameObject);
            }

            auditIntruders.Clear();
        }

        private void ClearAuditResultLines()
        {
            foreach (LineRenderer line in auditResultLines)
            {
                if (line != null)
                    Destroy(line.gameObject);
            }
            auditResultLines.Clear();
        }

        // ------------------------------------------------------------ live route control

        private void UpdateRouteMode(float deltaTime)
        {
            Vector3 position = avatar.localPosition;
            if (runExecuting && avatarNextWaypoint < routeWaypoints.Count)
            {
                float speed = minigame.AvatarSpeedCells * cellSize;
                Vector3 target = routeWaypoints[avatarNextWaypoint];
                position = Vector3.MoveTowards(position, target, speed * deltaTime);
                avatar.localPosition = position;

                if ((position - target).sqrMagnitude < 0.0001f)
                    avatarNextWaypoint++;

                if (avatarNextWaypoint >= routeWaypoints.Count)
                    StopRouteMovement(string.Empty);
            }

            // Detection remains active while moving and while holding in a safe pocket.
            foreach (DeckCamera cam in cameras)
            {
                if (ConeSees(cam, position))
                {
                    StopRouteMovement(string.Empty);
                    avatar.localPosition = entryPosition;
                    PushFeedback(minigame.OnRunCaught($"CAM {cam.mount.label}"), true);

                    if (logEvents)
                        Debug.Log($"[CameraDeck] operative caught by {cam.mount.label}");

                    return;
                }
            }

            // Reached the asset?
            if (FlatDistance(position, primaryAssetPosition) <= assetReachRadius)
            {
                runExecuting = false;
                PushFeedback(minigame.OnRunReachedTarget(), false);
                return;
            }
        }

        // ------------------------------------------------------------- helpers

        private void AnimateSpots()
        {
            float time = Time.time;

            foreach (DeckSpot spot in spots)
            {
                if (spot.resolved || spot.root == null)
                    continue;

                // One shared calm pulse for every spot - deliberately NO per-kind tell.
                float pulse = 0.7f + 0.3f * Mathf.Sin(time * 2.2f + spot.pulsePhase);

                if (spot.material.HasProperty("_EmissionColor"))
                    spot.material.SetColor("_EmissionColor", spotColor * (pulse * emissionIntensity));

                spot.root.Rotate(0f, 35f * Time.deltaTime, 0f, Space.World);
            }
        }

        private void PushFeedback(string message, bool warn)
        {
            if (string.IsNullOrEmpty(message))
                return;

            CameraDeckMinigameUI ui = MinigamesUIManager.Instance != null
                ? MinigamesUIManager.Instance.ActiveMinigameUI as CameraDeckMinigameUI
                : null;

            if (ui != null)
                ui.ShowFeedback(message, warn);
        }

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

        private float WorldX(int col) => (col - layout.cols * 0.5f + 0.5f) * cellSize;
        private float WorldZ(int row) => (layout.rows * 0.5f - row - 0.5f) * cellSize;
        private int ColAt(float x) => Mathf.FloorToInt(x / cellSize + layout.cols * 0.5f);
        private int RowAt(float z) => Mathf.FloorToInt(layout.rows * 0.5f - z / cellSize);
        private Vector3 CellCenter(Vector2Int cell) => new Vector3(WorldX(cell.x), 0f, WorldZ(cell.y));

        private static float FlatDistance(Vector3 a, Vector3 b)
        {
            a.y = 0f;
            b.y = 0f;
            return Vector3.Distance(a, b);
        }

        private static void SetLayerRecursively(GameObject root, int layer)
        {
            root.layer = layer;
            foreach (Transform child in root.transform)
                SetLayerRecursively(child.gameObject, layer);
        }

        private GameObject CreateBox(string boxName, Vector3 position, Vector3 scale, Material material, Transform parent = null)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = boxName;
            Destroy(go.GetComponent<Collider>());
            go.transform.SetParent(parent == null ? worldRoot : parent, false);
            go.transform.localPosition = position;
            go.transform.localScale = scale;
            ApplyMaterial(go, material);
            return go;
        }

        /// <summary>
        /// Loads a model from Resources/CameraDeck by name; degrades to a Unity primitive
        /// so a missing/not-yet-imported mesh can never empty the site. Colliders are
        /// stripped - all picking is deterministic ray math.
        /// </summary>
        private GameObject LoadModel(string modelName, PrimitiveType fallback, out bool usedFallback)
        {
            GameObject prefab = Resources.Load<GameObject>("CameraDeck/" + modelName);
            GameObject go;

            if (prefab != null)
            {
                go = Instantiate(prefab);
                usedFallback = false;
            }
            else
            {
                Debug.LogWarning($"[CameraDeck] Model 'Resources/CameraDeck/{modelName}' not found - using {fallback} primitive fallback.");
                go = GameObject.CreatePrimitive(fallback);
                usedFallback = true;
            }

            foreach (Collider collider in go.GetComponentsInChildren<Collider>())
                Destroy(collider);

            return go;
        }

        private void ApplyMaterial(GameObject go, Material material)
        {
            if (material == null)
                return;

            foreach (Renderer renderer in go.GetComponentsInChildren<Renderer>())
                renderer.sharedMaterial = material;
        }

        private Material GetSharedNeonMaterial(Color color, float emission)
        {
            string key = ColorUtility.ToHtmlStringRGBA(color) + ":" + Mathf.RoundToInt(emission * 100f);
            if (sharedMaterials.TryGetValue(key, out Material material))
                return material;

            material = CreateNeonMaterial(color, emission);
            sharedMaterials.Add(key, material);
            return material;
        }

        private Material CreateNeonMaterial(Color color, float emission)
        {
            Shader shader = Shader.Find("Standard");

            if (shader == null)
                shader = Shader.Find("Legacy Shaders/Diffuse");

            if (shader == null)
            {
                Debug.LogError("[CameraDeck] Neither Standard nor Legacy Diffuse shader found - materials will be missing.");
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

        /// <summary>Translucent material for the cone fans - reads as light on the plan.</summary>
        private Material CreateConeMaterial()
        {
            Shader shader = Shader.Find("Sprites/Default");
            Material material;

            if (shader != null)
            {
                material = new Material(shader);
                createdMaterials.Add(material);
            }
            else
            {
                material = CreateNeonMaterial(coneColor, 0.8f);

                if (material == null)
                    return null;

                material.SetFloat("_Mode", 2f);
                material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                material.SetInt("_ZWrite", 0);
                material.EnableKeyword("_ALPHABLEND_ON");
                material.renderQueue = 3000;
            }

            Color color = coneColor;
            color.a = coneAlpha;
            material.color = color;
            return material;
        }

        private Material CreateLineMaterial()
        {
            Shader shader = Shader.Find("Sprites/Default");

            if (shader == null)
                return CreateNeonMaterial(routeLineColor, 1.5f);

            Material material = new Material(shader);
            createdMaterials.Add(material);
            return material;
        }
    }
}
