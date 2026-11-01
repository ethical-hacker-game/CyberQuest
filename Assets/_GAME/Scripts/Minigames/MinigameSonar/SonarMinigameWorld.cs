using System.Collections.Generic;
using UnityEngine;

namespace Minigames
{
    /// <summary>
    /// 3D view for <see cref="SonarMinigame"/>: a dark, round top-down arena with a scanner
    /// station in the middle. A translucent sonar fan sweeps around the station; a blip is
    /// invisible until the fan's wedge passes over it, then it glows and fades over the
    /// afterglow window. The reveal is deterministic math - a blip is "lit" when its polar
    /// angle from the centre is inside the current wedge and within range - so, exactly like
    /// Data Rover's vision cones, what the player sees IS the detection truth. Caches hold a
    /// steady, sharp pulse; false echoes flicker and wobble on seeded Perlin noise. Clicks
    /// are resolved with a ray-sphere test against only the currently-visible blips (no
    /// physics, no colliders) - you can never click what you cannot see.
    ///
    /// Everything - camera, light, floor, range rings, station, fan, blips, materials - is
    /// built procedurally. Blip meshes load from <c>Resources/SonarSweep/</c> by path (the
    /// cache falls back to the Packet Rush crystal, then to a primitive), so a missing FBX
    /// degrades to a Unity primitive plus a loud warning, never to an empty arena.
    /// Sits on the same GameObject as <see cref="SonarMinigame"/> in the behaviour prefab.
    /// </summary>
    public class SonarMinigameWorld : MonoBehaviour
    {
        [Header("Arena (world units)")]
        [SerializeField, Tooltip("Radius of the play area the blips are scattered across.")]
        private float arenaRadius = 7f;
        [SerializeField, Tooltip("No blip spawns closer to the station than this (keeps the centre clear).")]
        private float innerClearRadius = 2.2f;
        [SerializeField, Tooltip("Blips stay at least this far outside the arena edge.")]
        private float edgeMargin = 0.7f;
        [SerializeField, Tooltip("Minimum distance between any two blips (so a click is never ambiguous).")]
        private float minBlipSeparation = 1.6f;
        [SerializeField, Tooltip("Number of concentric range rings drawn on the floor.")]
        private int rangeRings = 3;

        [Header("Blips")]
        [SerializeField, Tooltip("Height of the blip centres above the floor.")]
        private float blipY = 0.5f;
        [SerializeField, Tooltip("Uniform scale of a blip mesh at full glow.")]
        private float blipScale = 0.8f;
        [SerializeField, Tooltip("Ray-sphere click radius of a fully lit blip (world units).")]
        private float clickRadius = 0.75f;
        [SerializeField, Range(0f, 1f), Tooltip("A blip must be at least this bright to be clickable (can't click the dark).")]
        private float clickableGlow = 0.12f;
        [SerializeField, Tooltip("How fast false echoes flicker (Perlin sample rate).")]
        private float echoFlickerSpeed = 5.5f;
        [SerializeField, Tooltip("How much a cache's steady pulse breathes (0 = dead steady).")]
        private float cachePulseDepth = 0.12f;

        [Header("Blip motion")]
        [SerializeField, Tooltip("Base orbital drift speed of a contact around the station (degrees/second).")]
        private float blipDriftDegPerSec = 11f;
        [SerializeField, Tooltip("How much a contact's distance from the station breathes in and out (world units).")]
        private float blipRadialAmp = 0.75f;
        [SerializeField, Tooltip("Rate of the in/out radial breathing.")]
        private float blipRadialSpeed = 0.45f;
        [SerializeField, Tooltip("Shortest gap before a contact randomly re-vectors (changes drift).")]
        private float blipRevectorMin = 3f;
        [SerializeField, Tooltip("Longest gap before a contact randomly re-vectors.")]
        private float blipRevectorMax = 6.5f;

        [Header("Fan")]
        [SerializeField, Tooltip("Fan reach - should cover the whole arena so nothing stays hidden.")]
        private float fanRadius = 8.2f;
        [SerializeField, Tooltip("Triangles in the fan wedge (more = smoother arc).")]
        private int fanSegments = 20;
        [SerializeField, Tooltip("Height of the fan mesh above the floor.")]
        private float fanY = 0.08f;

        // Same neon language as the other 3D minigames; colours come only from this palette.
        [Header("Palette")]
        [SerializeField] private Color cacheColor = new Color(0.21f, 0.88f, 1f);
        [SerializeField] private Color cacheMarkedColor = new Color(0.24f, 1f, 0.53f);
        [SerializeField] private Color echoColor = new Color(1f, 0.36f, 0.28f);
        [SerializeField] private Color fanColor = new Color(0.32f, 0.95f, 1f);
        [SerializeField, Range(0f, 1f)] private float fanAlpha = 0.16f;
        [SerializeField] private Color stationColor = new Color(0.5f, 0.85f, 1f);
        [SerializeField] private Color floorColor = new Color(0.02f, 0.04f, 0.075f);
        [SerializeField] private Color ringColor = new Color(0.08f, 0.35f, 0.45f);
        [SerializeField] private Color backgroundColor = new Color(0.012f, 0.025f, 0.05f);
        [SerializeField, Tooltip("Multiplier for the emission tint of the neon materials.")]
        private float emissionIntensity = 1.6f;

        [Header("Diagnostics")]
        [SerializeField, Tooltip("Log every mark and false hit to the console.")]
        private bool logEvents = false;

        private class SonarBlip
        {
            public SonarObjectKind kind;
            public Transform root;
            public Transform visual;
            public Material material;
            public Color baseColor;
            public float angleDeg;   // polar angle from centre (recomputed as it drifts)
            public float distance;   // distance from centre (recomputed as it drifts)
            public float seed;
            public float glow;
            public bool marked;

            // Orbital drift state: the contact circles the station at a breathing radius,
            // and periodically re-vectors so its motion is not a perfectly readable orbit.
            public float orbitAngle;   // radians, current polar position
            public float baseRadius;   // centre radius the breathing oscillates around
            public float angularVel;   // rad/s, signed (drift direction + speed)
            public float radialAmp;    // amplitude of the in/out breathing
            public float radialPhase;
            public float revectorTimer;
        }

        private SonarMinigame minigame;
        private Transform worldRoot;
        private Camera worldCamera;

        private Transform fanPivot;
        private Mesh fanMesh;
        private Material fanMaterial;
        private float fanAngle;

        private Transform stationCore;
        private Material stationMaterial;

        private readonly List<SonarBlip> blips = new List<SonarBlip>();
        private readonly List<Vector2> usedPositions = new List<Vector2>();
        private readonly List<Material> createdMaterials = new List<Material>();
        private readonly List<Mesh> createdMeshes = new List<Mesh>();

        private void Awake()
        {
            minigame = GetComponent<SonarMinigame>();
        }

        private void Start()
        {
            if (minigame == null)
            {
                Debug.LogError("[SonarSweep] SonarMinigameWorld needs a SonarMinigame on the same GameObject.");
                return;
            }

            BuildWorld();
            Debug.Log($"[SonarSweep] world ready - {minigame.TotalCaches} caches, {minigame.InitialEchoCount} echoes; " +
                "arena/station/fan procedural, blip meshes from Resources/SonarSweep with primitive fallbacks.");
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

            minigame.Tick(deltaTime);
            AnimateStation(deltaTime);

            if (minigame.IsPaused || minigame.IsFinished)
            {
                // Keep the fan and glow visible but frozen once the run ends.
                UpdateBlipVisuals(0f, freeze: true);
                return;
            }

            SpawnPendingCaches();
            SpawnPendingEchoes();
            UpdateBlipMotion(deltaTime);
            UpdateSweep(deltaTime);
            UpdateBlipVisuals(deltaTime, freeze: false);
            HandleClick();
        }

        // ------------------------------------------------------------------ build

        private void BuildWorld()
        {
            worldRoot = new GameObject("SonarWorld").transform;
            worldRoot.SetParent(transform, false);

            BuildCamera();
            BuildLight();
            BuildArena();
            BuildStation();
            BuildFan();
            BuildBlips();
        }

        private void BuildCamera()
        {
            GameObject camGo = new GameObject("SonarCamera", typeof(Camera));
            camGo.transform.SetParent(worldRoot, false);
            camGo.transform.localPosition = new Vector3(0f, arenaRadius * 1.95f, -arenaRadius * 0.8f);
            camGo.transform.localEulerAngles = new Vector3(68f, 0f, 0f);

            worldCamera = camGo.GetComponent<Camera>();
            worldCamera.clearFlags = CameraClearFlags.SolidColor;
            worldCamera.backgroundColor = backgroundColor;
            worldCamera.fieldOfView = 56f;
            worldCamera.nearClipPlane = 0.3f;
            worldCamera.farClipPlane = 120f;
            worldCamera.depth = 10f;
        }

        private void BuildLight()
        {
            GameObject lightGo = new GameObject("SonarLight", typeof(Light));
            lightGo.transform.SetParent(worldRoot, false);
            lightGo.transform.localEulerAngles = new Vector3(70f, -20f, 0f);

            Light light = lightGo.GetComponent<Light>();
            light.type = LightType.Directional;
            // Dim: the darkness is the point - the fan and the blips are the light sources.
            light.intensity = 0.35f;
            light.color = new Color(0.6f, 0.75f, 0.95f);
        }

        private void BuildArena()
        {
            // Round floor: a flattened cylinder reads as a scanner scope.
            GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            floor.name = "Floor";
            Destroy(floor.GetComponent<Collider>());
            floor.transform.SetParent(worldRoot, false);
            floor.transform.localPosition = new Vector3(0f, -0.05f, 0f);
            floor.transform.localScale = new Vector3((arenaRadius + 0.6f) * 2f, 0.05f, (arenaRadius + 0.6f) * 2f);
            ApplyMaterial(floor, CreateNeonMaterial(floorColor, 0.25f));

            // Bright rim + faint concentric range rings help the player read distance.
            BuildCircle("Rim", arenaRadius + 0.35f, 0.05f, CreateLineMaterial(), ringColor * 1.8f, 96);

            for (int i = 1; i <= rangeRings; i++)
            {
                float t = i / (float)(rangeRings + 1);
                float radius = Mathf.Lerp(innerClearRadius, arenaRadius, t);
                BuildCircle($"Ring{i}", radius, 0.02f, CreateLineMaterial(), ringColor * 0.6f, 72);
            }
        }

        private void BuildStation()
        {
            Transform stationRoot = new GameObject("Station").transform;
            stationRoot.SetParent(worldRoot, false);

            GameObject model = LoadModel("station", PrimitiveType.Cylinder, out bool usedFallback);
            model.name = "StationBody";
            model.transform.SetParent(stationRoot, false);

            if (usedFallback)
            {
                // A short pillar with a wider base plate - a plausible scanner mast.
                model.transform.localScale = new Vector3(0.9f, 0.55f, 0.9f);
                model.transform.localPosition = new Vector3(0f, 0.55f, 0f);
            }

            ApplyMaterial(model, CreateNeonMaterial(stationColor * 0.5f, 0.6f));

            // A pulsing core sphere on top - the "emitter" the fan spins from.
            GameObject core = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            core.name = "StationCore";
            Destroy(core.GetComponent<Collider>());
            core.transform.SetParent(stationRoot, false);
            core.transform.localPosition = new Vector3(0f, usedFallback ? 1.25f : 1.1f, 0f);
            core.transform.localScale = Vector3.one * 0.5f;
            stationMaterial = CreateNeonMaterial(stationColor, emissionIntensity);
            ApplyMaterial(core, stationMaterial);
            stationCore = core.transform;
        }

        private void BuildFan()
        {
            fanPivot = new GameObject("FanPivot").transform;
            fanPivot.SetParent(worldRoot, false);
            fanPivot.localPosition = new Vector3(0f, fanY, 0f);

            GameObject fanGo = new GameObject("SonarFan", typeof(MeshFilter), typeof(MeshRenderer));
            fanGo.transform.SetParent(fanPivot, false);

            fanMesh = new Mesh { name = "SonarFanMesh" };
            fanMesh.MarkDynamic();
            createdMeshes.Add(fanMesh);
            fanGo.GetComponent<MeshFilter>().sharedMesh = fanMesh;

            fanMaterial = CreateBeamMaterial();
            fanGo.GetComponent<MeshRenderer>().sharedMaterial = fanMaterial;

            RebuildFanMesh(minigame.CurrentFanHalfAngle);
        }

        private void BuildBlips()
        {
            usedPositions.Clear();

            for (int i = 0; i < minigame.InitialCacheCount; i++)
                SpawnBlip(SonarObjectKind.Cache);

            for (int i = 0; i < minigame.InitialEchoCount; i++)
                SpawnBlip(SonarObjectKind.FalseEcho);
        }

        private SonarBlip SpawnBlip(SonarObjectKind kind)
        {
            if (TryFindFairPosition(out Vector2 position) == false)
            {
                // Arena saturated (only happens with extreme authored counts) - skip quietly.
                Debug.LogWarning("[SonarSweep] could not place a blip with fair spacing - arena is saturated, skipping one.");
                return null;
            }

            usedPositions.Add(position);

            Transform root = new GameObject($"Blip_{kind}_{blips.Count}").transform;
            root.SetParent(worldRoot, false);
            root.localPosition = new Vector3(position.x, blipY, position.y);

            GameObject visual = kind == SonarObjectKind.Cache
                ? LoadCacheModel()
                : LoadModel("echo", PrimitiveType.Sphere, out _);
            visual.name = "Visual";
            visual.transform.SetParent(root, false);
            visual.transform.localScale = Vector3.zero;
            visual.transform.localRotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);

            Color color = kind == SonarObjectKind.Cache ? cacheColor : echoColor;
            Material material = CreateGlowMaterial(color);
            ApplyMaterial(visual, material);

            float orbitAngle = Mathf.Atan2(position.x, position.y);

            SonarBlip blip = new SonarBlip
            {
                kind = kind,
                root = root,
                visual = visual.transform,
                material = material,
                baseColor = color,
                angleDeg = orbitAngle * Mathf.Rad2Deg,
                distance = position.magnitude,
                seed = Random.Range(0f, 100f),
                glow = 0f,
                marked = false,
                orbitAngle = orbitAngle,
                baseRadius = position.magnitude,
                angularVel = NewAngularVel(),
                radialAmp = Random.Range(0.35f, 1f) * blipRadialAmp,
                radialPhase = Random.Range(0f, Mathf.PI * 2f),
                revectorTimer = Random.Range(blipRevectorMin, blipRevectorMax),
            };

            blips.Add(blip);
            return blip;
        }

        private bool TryFindFairPosition(out Vector2 position)
        {
            float maxRadius = arenaRadius - edgeMargin;

            for (int attempt = 0; attempt < 120; attempt++)
            {
                float radius = Mathf.Sqrt(Random.value) * (maxRadius - innerClearRadius) + innerClearRadius;
                float angle = Random.value * Mathf.PI * 2f;
                Vector2 candidate = new Vector2(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius);

                // Never place a blip where the beam is right now - it must start dark and be
                // discovered by a later sweep, so nothing pops in already lit.
                float candidateAngle = Mathf.Atan2(candidate.x, candidate.y) * Mathf.Rad2Deg;
                if (Mathf.Abs(Mathf.DeltaAngle(fanAngle, candidateAngle)) <= minigame.CurrentFanHalfAngle + 8f)
                    continue;

                bool clear = true;

                foreach (Vector2 used in usedPositions)
                {
                    if ((used - candidate).sqrMagnitude < minBlipSeparation * minBlipSeparation)
                    {
                        clear = false;
                        break;
                    }
                }

                if (clear)
                {
                    position = candidate;
                    return true;
                }
            }

            position = Vector2.zero;
            return false;
        }

        // ------------------------------------------------------------ per-frame

        private void SpawnPendingCaches()
        {
            while (minigame.TryDequeueCache())
            {
                SonarBlip blip = SpawnBlip(SonarObjectKind.Cache);

                // New caches start invisible (glow 0) and are discovered by a later sweep.
                if (logEvents && blip != null)
                    Debug.Log("[SonarSweep] cache faded in (new contact).");
            }
        }

        private void SpawnPendingEchoes()
        {
            while (minigame.TryDequeueExtraEcho())
            {
                SonarBlip blip = SpawnBlip(SonarObjectKind.FalseEcho);

                if (logEvents && blip != null)
                    Debug.Log("[SonarSweep] extra echo ramped in (pressure).");
            }
        }

        /// <summary>
        /// Drifts every un-marked contact around the station on a breathing-radius orbit and
        /// periodically re-vectors it, so a blip you glimpsed is somewhere else by the time
        /// the beam comes back - you have to track it, not just remember a spot. The reveal
        /// stays honest: angle/distance are recomputed here from the live position, so the
        /// beam still lights exactly what it passes over.
        /// </summary>
        private void UpdateBlipMotion(float deltaTime)
        {
            float minRadius = innerClearRadius;
            float maxRadius = arenaRadius - edgeMargin;
            float time = Time.time;

            foreach (SonarBlip blip in blips)
            {
                if (blip.root == null || blip.marked)
                    continue;

                blip.revectorTimer -= deltaTime;

                if (blip.revectorTimer <= 0f)
                {
                    blip.revectorTimer = Random.Range(blipRevectorMin, blipRevectorMax);
                    RevectorBlip(blip);
                }

                blip.orbitAngle += blip.angularVel * deltaTime;

                float radius = Mathf.Clamp(
                    blip.baseRadius + Mathf.Sin(time * blipRadialSpeed + blip.radialPhase) * blip.radialAmp,
                    minRadius, maxRadius);

                float x = Mathf.Sin(blip.orbitAngle) * radius;
                float z = Mathf.Cos(blip.orbitAngle) * radius;
                blip.root.localPosition = new Vector3(x, blipY, z);

                blip.angleDeg = blip.orbitAngle * Mathf.Rad2Deg;
                blip.distance = radius;
            }
        }

        /// <summary>A fresh signed drift speed for a contact, scaled by the difficulty multiplier.</summary>
        private float NewAngularVel()
        {
            float baseSpeed = blipDriftDegPerSec * Mathf.Deg2Rad * minigame.BlipDriftMultiplier;
            float magnitude = baseSpeed * Random.Range(0.6f, 1.4f);
            float sign = Random.value < 0.5f ? -1f : 1f;
            return magnitude * sign;
        }

        /// <summary>Nudges a contact's drift: mostly a speed change, occasionally a reversal.</summary>
        private void RevectorBlip(SonarBlip blip)
        {
            float baseSpeed = blipDriftDegPerSec * Mathf.Deg2Rad * minigame.BlipDriftMultiplier;
            float magnitude = Mathf.Abs(blip.angularVel) * Random.Range(0.7f, 1.3f);
            magnitude = Mathf.Clamp(magnitude, baseSpeed * 0.45f, baseSpeed * 1.6f);
            float sign = Mathf.Sign(blip.angularVel == 0f ? 1f : blip.angularVel);

            if (Random.value < 0.2f)
                sign = -sign;

            blip.angularVel = magnitude * sign;
        }

        private void UpdateSweep(float deltaTime)
        {
            fanAngle += minigame.SweepAngularSpeedDegPerSec * deltaTime;

            if (fanAngle >= 360f)
                fanAngle -= 360f;

            fanPivot.localRotation = Quaternion.Euler(0f, fanAngle, 0f);
            RebuildFanMesh(minigame.CurrentFanHalfAngle);

            // Reveal pass: a blip inside the current wedge is lit to full glow this frame.
            float halfAngle = minigame.CurrentFanHalfAngle;

            foreach (SonarBlip blip in blips)
            {
                if (blip.marked)
                    continue;

                bool inBeam = blip.distance <= fanRadius
                    && Mathf.Abs(Mathf.DeltaAngle(fanAngle, blip.angleDeg)) <= halfAngle;

                if (inBeam)
                    blip.glow = 1f;
            }
        }

        private void UpdateBlipVisuals(float deltaTime, bool freeze)
        {
            float afterglow = minigame.AfterglowSeconds;
            float time = Time.time;

            foreach (SonarBlip blip in blips)
            {
                if (blip.root == null)
                    continue;

                if (blip.marked)
                {
                    // Marked caches stay lit with a calm confirmation pulse.
                    float pulse = 0.8f + 0.2f * Mathf.Sin(time * 3f + blip.seed);
                    SetBlipAppearance(blip, cacheMarkedColor, 1f, blipScale, pulse);
                    continue;
                }

                if (freeze == false && afterglow > 0f)
                    blip.glow = Mathf.Max(0f, blip.glow - deltaTime / afterglow);

                float glow = blip.glow;

                if (blip.kind == SonarObjectKind.Cache)
                {
                    // Steady, sharp: only a shallow breathing pulse, no scale wobble.
                    float pulse = 1f - cachePulseDepth + cachePulseDepth * Mathf.Sin(time * 4f + blip.seed);
                    SetBlipAppearance(blip, blip.baseColor, glow, blipScale, pulse);
                }
                else
                {
                    // Nervous: seeded Perlin noise jitters brightness AND scale, so the shape
                    // visibly wobbles - the tell that separates an echo from a real cache.
                    float noise = Mathf.PerlinNoise(time * echoFlickerSpeed, blip.seed);
                    float flicker = 0.35f + 0.9f * noise;
                    float wobble = blipScale * (0.78f + 0.36f * noise);
                    SetBlipAppearance(blip, blip.baseColor, glow, wobble, flicker);
                }
            }
        }

        /// <summary>Drives one blip's alpha, emission and scale from its glow and per-kind modulation.</summary>
        private void SetBlipAppearance(SonarBlip blip, Color color, float glow, float scale, float modulation)
        {
            float visible = Mathf.Clamp01(glow);
            float shownScale = scale * Mathf.Lerp(0.35f, 1f, visible);
            blip.visual.localScale = Vector3.one * shownScale;
            blip.visual.Rotate(0f, 40f * Time.deltaTime, 0f, Space.Self);

            Color faceColor = color;
            faceColor.a = visible * 0.92f;
            blip.material.color = faceColor;

            if (blip.material.HasProperty("_EmissionColor"))
                blip.material.SetColor("_EmissionColor", color * (visible * modulation * emissionIntensity));
        }

        private void AnimateStation(float deltaTime)
        {
            if (stationMaterial == null || stationCore == null)
                return;

            float pulse = 0.6f + 0.4f * Mathf.Sin(Time.time * 2.4f);
            stationMaterial.SetColor("_EmissionColor", stationColor * (emissionIntensity * pulse));
        }

        private void HandleClick()
        {
            if (Input.GetMouseButtonDown(0) == false || worldCamera == null)
                return;

            Ray ray = worldCamera.ScreenPointToRay(Input.mousePosition);
            SonarBlip best = null;
            float bestDistance = float.MaxValue;

            foreach (SonarBlip blip in blips)
            {
                if (blip.root == null || blip.marked)
                    continue;

                // Only blips currently lit brightly enough are clickable - no blind clicks.
                if (blip.glow < clickableGlow)
                    continue;

                float radius = clickRadius * Mathf.Lerp(0.6f, 1f, Mathf.Clamp01(blip.glow));

                if (RayHitsSphere(ray, blip.root.position, radius, out float distance)
                    && distance < bestDistance)
                {
                    bestDistance = distance;
                    best = blip;
                }
            }

            if (best != null)
                ResolveClick(best);
        }

        private void ResolveClick(SonarBlip blip)
        {
            if (blip.kind == SonarObjectKind.Cache)
            {
                bool fresh = blip.glow >= minigame.FreshGlowThreshold;
                string message = minigame.MarkCache(fresh);
                blip.marked = true;
                blip.glow = 1f;
                PushFeedback(message, false);

                if (logEvents)
                    Debug.Log($"[SonarSweep] cache marked ({minigame.Marked}/{minigame.TotalCaches}), fresh={fresh}");
            }
            else
            {
                string message = minigame.RegisterFalseHit();
                blips.Remove(blip);

                if (blip.root != null)
                    Destroy(blip.root.gameObject);

                PushFeedback(message, true);

                if (logEvents)
                    Debug.Log($"[SonarSweep] false echo clicked (integrity {minigame.CurrentIntegrity}/{minigame.MaxIntegrity})");
            }
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

        private void RebuildFanMesh(float halfAngle)
        {
            int segments = Mathf.Max(4, fanSegments);
            Vector3[] vertices = new Vector3[segments + 2];
            Color[] colors = new Color[segments + 2];
            int[] triangles = new int[segments * 3];

            vertices[0] = Vector3.zero;
            colors[0] = new Color(fanColor.r, fanColor.g, fanColor.b, fanAlpha * 1.6f);

            for (int i = 0; i <= segments; i++)
            {
                float angle = -halfAngle + (halfAngle * 2f) * i / segments;
                Vector3 dir = Quaternion.Euler(0f, angle, 0f) * Vector3.forward;
                vertices[i + 1] = dir * fanRadius;

                // Brighter at the leading edge, faint at the trailing edge - reads as a sweep.
                float edge = (float)i / segments;
                float edgeAlpha = fanAlpha * Mathf.Lerp(1.4f, 0.35f, edge);
                colors[i + 1] = new Color(fanColor.r, fanColor.g, fanColor.b, edgeAlpha);
            }

            for (int i = 0; i < segments; i++)
            {
                triangles[i * 3] = 0;
                triangles[i * 3 + 1] = i + 1;
                triangles[i * 3 + 2] = i + 2;
            }

            fanMesh.Clear();
            fanMesh.vertices = vertices;
            fanMesh.colors = colors;
            fanMesh.triangles = triangles;
            fanMesh.RecalculateBounds();
        }

        // ------------------------------------------------------------- helpers

        private void PushFeedback(string message, bool warn)
        {
            if (string.IsNullOrEmpty(message))
                return;

            SonarMinigameUI ui = MinigamesUIManager.Instance != null
                ? MinigamesUIManager.Instance.ActiveMinigameUI as SonarMinigameUI
                : null;

            if (ui != null)
                ui.ShowFeedback(message, warn);
        }

        /// <summary>
        /// Loads a model from Resources/SonarSweep by name; degrades to a Unity primitive so
        /// a missing/not-yet-imported mesh can never empty the arena. Colliders are stripped -
        /// clicking is deterministic ray-sphere math.
        /// </summary>
        private GameObject LoadModel(string modelName, PrimitiveType fallback, out bool usedFallback)
        {
            GameObject prefab = Resources.Load<GameObject>("SonarSweep/" + modelName);
            GameObject go;

            if (prefab != null)
            {
                go = Instantiate(prefab);
                usedFallback = false;
            }
            else
            {
                Debug.LogWarning($"[SonarSweep] Model 'Resources/SonarSweep/{modelName}' not found - using {fallback} primitive fallback.");
                go = GameObject.CreatePrimitive(fallback);
                usedFallback = true;
            }

            foreach (Collider collider in go.GetComponentsInChildren<Collider>())
                Destroy(collider);

            return go;
        }

        private GameObject LoadCacheModel()
        {
            GameObject prefab = Resources.Load<GameObject>("SonarSweep/cache");

            // Design-doc reuse: the Packet Rush data crystal is the default cache mesh.
            if (prefab == null)
                prefab = Resources.Load<GameObject>("PacketRush/crystal");

            GameObject go;

            if (prefab != null)
            {
                go = Instantiate(prefab);
            }
            else
            {
                Debug.LogWarning("[SonarSweep] Neither 'Resources/SonarSweep/cache' nor 'Resources/PacketRush/crystal' found - using Capsule primitive fallback.");
                go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                go.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);
            }

            foreach (Collider collider in go.GetComponentsInChildren<Collider>())
                Destroy(collider);

            return go;
        }

        private void BuildCircle(string circleName, float radius, float width, Material material, Color color, int segments)
        {
            GameObject go = new GameObject(circleName, typeof(LineRenderer));
            go.transform.SetParent(worldRoot, false);
            go.transform.localPosition = new Vector3(0f, 0.015f, 0f);

            LineRenderer line = go.GetComponent<LineRenderer>();
            line.useWorldSpace = false;
            line.loop = true;
            line.positionCount = segments;
            line.widthMultiplier = width;
            line.sharedMaterial = material;
            line.startColor = color;
            line.endColor = color;
            line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            line.receiveShadows = false;

            for (int i = 0; i < segments; i++)
            {
                float angle = i * Mathf.PI * 2f / segments;
                line.SetPosition(i, new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius));
            }
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
                Debug.LogError("[SonarSweep] Neither Standard nor Legacy Diffuse shader found - materials will be missing.");
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

        /// <summary>Per-blip Fade material so a blip can glow up out of the dark and fade back to invisible.</summary>
        private Material CreateGlowMaterial(Color color)
        {
            Material material = CreateNeonMaterial(color, emissionIntensity);

            if (material == null)
                return null;

            Color faded = color;
            faded.a = 0f;
            material.color = faded;
            material.SetFloat("_Mode", 2f); // Fade
            material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            material.SetInt("_ZWrite", 0);
            material.DisableKeyword("_ALPHATEST_ON");
            material.EnableKeyword("_ALPHABLEND_ON");
            material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            material.renderQueue = 3000;

            return material;
        }

        /// <summary>Additive translucent material for the sweep fan - it reads as light, not a slab.</summary>
        private Material CreateBeamMaterial()
        {
            Shader shader = Shader.Find("Sprites/Default");

            if (shader != null)
            {
                Material spriteMat = new Material(shader);
                createdMaterials.Add(spriteMat);
                return spriteMat;
            }

            // Fallback: an additive-blended Standard material still reads as a beam.
            Material material = CreateNeonMaterial(fanColor, 1.5f);

            if (material == null)
                return null;

            material.SetFloat("_Mode", 3f);
            material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.One);
            material.SetInt("_ZWrite", 0);
            material.EnableKeyword("_ALPHABLEND_ON");
            material.renderQueue = 3000;
            return material;
        }

        private Material CreateLineMaterial()
        {
            Shader shader = Shader.Find("Sprites/Default");

            if (shader == null)
                return CreateNeonMaterial(ringColor, 1.5f);

            Material material = new Material(shader);
            createdMaterials.Add(material);
            return material;
        }
    }
}
