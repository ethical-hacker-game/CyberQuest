using System;
using System.Collections.Generic;
using UnityEngine;

namespace Minigames
{
    public enum CameraDeckMode
    {
        BlindSpotAudit = 0,
        CleanDeskSweep = 1,
        BlindRoute = 2,
    }

    public enum DeckInspectionProp
    {
        Monitor,
        Note,
        Printout,
        Badge,
        UsbDrive,
        Rack,
        Door,
    }

    [Serializable]
    public sealed class DeckCameraMount
    {
        public readonly Vector2Int cell;
        public readonly string label;
        public readonly float[] allowedYaws;
        public readonly bool routeActive;
        public readonly float sweepArcDegrees;
        public readonly float sweepPhase;
        public readonly bool continuousRotation;

        public DeckCameraMount(Vector2Int cell, string label, float[] allowedYaws,
            bool routeActive = false, float sweepArcDegrees = 0f, float sweepPhase = 0f,
            bool continuousRotation = false)
        {
            this.cell = cell;
            this.label = label;
            this.allowedYaws = allowedYaws != null && allowedYaws.Length > 0
                ? allowedYaws
                : new[] { 0f };
            this.routeActive = routeActive;
            this.sweepArcDegrees = Mathf.Max(0f, sweepArcDegrees);
            this.sweepPhase = sweepPhase;
            this.continuousRotation = continuousRotation;
        }
    }

    [Serializable]
    public sealed class DeckIntrusionRoute
    {
        public readonly string label;
        public readonly List<Vector2Int> waypoints;

        public DeckIntrusionRoute(string label, params Vector2Int[] waypoints)
        {
            this.label = label;
            this.waypoints = new List<Vector2Int>(waypoints);
        }
    }

    [Serializable]
    public sealed class DeckInspectionAnchor
    {
        public readonly Vector2Int cell;
        public readonly DeckInspectionProp prop;
        public readonly string genuineFacts;
        public readonly string compliantFacts;

        public DeckInspectionAnchor(Vector2Int cell, DeckInspectionProp prop,
            string genuineFacts, string compliantFacts)
        {
            this.cell = cell;
            this.prop = prop;
            this.genuineFacts = genuineFacts;
            this.compliantFacts = compliantFacts;
        }
    }

    public sealed class DeckSpotDefinition
    {
        public readonly string description;
        public readonly bool genuine;
        public readonly DeckInspectionProp prop;

        public DeckSpotDefinition(string description, bool genuine, DeckInspectionProp prop)
        {
            this.description = description;
            this.genuine = genuine;
            this.prop = prop;
        }
    }

    /// <summary>
    /// Canonical facility definition used by both runtime and the C# validator. Definitions
    /// are lightweight ScriptableObjects created by the catalog, so future authored assets can
    /// replace them without changing the runtime contract.
    /// </summary>
    public sealed class CameraDeckFacilityDefinition : ScriptableObject
    {
        public string FacilityId { get; private set; }
        public string ProfileId { get; private set; }
        public string DisplayName { get; private set; }
        public CameraDeckMode Mode { get; private set; }
        public string[] Map { get; private set; }
        public DeckCameraMount[] Mounts { get; private set; }
        public DeckIntrusionRoute[] IntrusionRoutes { get; private set; }
        public DeckInspectionAnchor[] InspectionAnchors { get; private set; }
        public string PolicyText { get; private set; }
        public int PrimaryAssetIndex { get; private set; }

        public void Initialize(string facilityId, string profileId, string displayName,
            CameraDeckMode mode, string[] map, DeckCameraMount[] mounts,
            DeckIntrusionRoute[] intrusionRoutes = null,
            DeckInspectionAnchor[] inspectionAnchors = null,
            string policyText = "", int primaryAssetIndex = 0)
        {
            FacilityId = facilityId;
            ProfileId = profileId;
            DisplayName = displayName;
            Mode = mode;
            Map = map;
            Mounts = mounts ?? Array.Empty<DeckCameraMount>();
            IntrusionRoutes = intrusionRoutes ?? Array.Empty<DeckIntrusionRoute>();
            InspectionAnchors = inspectionAnchors ?? Array.Empty<DeckInspectionAnchor>();
            PolicyText = policyText ?? string.Empty;
            PrimaryAssetIndex = primaryAssetIndex;
        }
    }

    public sealed class DeckLayout
    {
        public readonly string facilityId;
        public readonly string profileId;
        public readonly string name;
        public readonly CameraDeckMode mode;
        public readonly int cols;
        public readonly int rows;
        public readonly List<Vector2Int> entryCells = new List<Vector2Int>();
        public readonly List<Vector2Int> assetCells = new List<Vector2Int>();
        public readonly List<Vector2Int> deskCells = new List<Vector2Int>();
        public readonly List<Vector2Int> rackCells = new List<Vector2Int>();
        public readonly List<Vector2Int> plantCells = new List<Vector2Int>();
        public readonly List<Vector2Int> safePocketCells = new List<Vector2Int>();
        public readonly List<DeckCameraMount> mounts;
        public readonly List<DeckIntrusionRoute> intrusionRoutes;
        public readonly List<DeckInspectionAnchor> inspectionAnchors;
        public readonly string policyText;
        public readonly int primaryAssetIndex;

        private readonly bool[,] walls;

        public int SpotCapacity => inspectionAnchors.Count;
        public Vector2Int EntryCell => entryCells[0];
        public bool HasPrimaryAsset => primaryAssetIndex >= 0 && primaryAssetIndex < assetCells.Count;
        public Vector2Int PrimaryAssetCell
        {
            get
            {
                if (HasPrimaryAsset == false)
                    throw new InvalidOperationException($"Facility '{facilityId}' has no primary asset.");
                return assetCells[primaryAssetIndex];
            }
        }

        public DeckLayout(CameraDeckFacilityDefinition definition)
        {
            facilityId = definition.FacilityId;
            profileId = definition.ProfileId;
            name = definition.DisplayName;
            mode = definition.Mode;
            rows = definition.Map.Length;
            cols = rows > 0 ? definition.Map[0].Length : 0;
            walls = new bool[cols, rows];
            mounts = new List<DeckCameraMount>(definition.Mounts);
            intrusionRoutes = new List<DeckIntrusionRoute>(definition.IntrusionRoutes);
            inspectionAnchors = new List<DeckInspectionAnchor>(definition.InspectionAnchors);
            policyText = definition.PolicyText;
            primaryAssetIndex = definition.PrimaryAssetIndex;

            for (int row = 0; row < rows; row++)
            {
                for (int col = 0; col < cols; col++)
                {
                    char cell = definition.Map[row][col];
                    walls[col, row] = cell == '#';

                    switch (cell)
                    {
                        case 'E': entryCells.Add(new Vector2Int(col, row)); break;
                        case 'A': assetCells.Add(new Vector2Int(col, row)); break;
                        case 'D': deskCells.Add(new Vector2Int(col, row)); break;
                        case 'K': rackCells.Add(new Vector2Int(col, row)); break;
                        case 'P': plantCells.Add(new Vector2Int(col, row)); break;
                        case 'S': safePocketCells.Add(new Vector2Int(col, row)); break;
                    }
                }
            }
        }

        public bool IsWall(int col, int row)
        {
            return col < 0 || row < 0 || col >= cols || row >= rows || walls[col, row];
        }

        public bool IsWalkable(Vector2Int cell) => IsWall(cell.x, cell.y) == false;
    }

    public static class CameraDeckFacilityCatalog
    {
        public const string AuditFacilityId = "archive_depot";
        public const string SweepFacilityId = "night_ops_office";
        public const string RouteFacilityId = "restricted_research_lab";
        public const string DefaultProfileId = "default";

        private static readonly Dictionary<string, CameraDeckFacilityDefinition> facilities =
            new Dictionary<string, CameraDeckFacilityDefinition>(StringComparer.OrdinalIgnoreCase);

        static CameraDeckFacilityCatalog()
        {
            Register(BuildArchiveDepot());
            Register(BuildNightOperationsOffice());
            Register(BuildRestrictedResearchLab());
        }

        public static CameraDeckFacilityDefinition GetDefault(CameraDeckMode mode)
        {
            string id = mode == CameraDeckMode.BlindSpotAudit
                ? AuditFacilityId
                : mode == CameraDeckMode.CleanDeskSweep ? SweepFacilityId : RouteFacilityId;
            return facilities[Key(id, DefaultProfileId)];
        }

        public static bool TryGet(CameraDeckMode mode, string facilityId, string profileId,
            out CameraDeckFacilityDefinition facility)
        {
            if (string.IsNullOrWhiteSpace(facilityId))
            {
                facility = GetDefault(mode);
                return true;
            }

            string resolvedProfile = string.IsNullOrWhiteSpace(profileId) ? DefaultProfileId : profileId;

            if (facilities.TryGetValue(Key(facilityId, resolvedProfile), out facility)
                && facility.Mode == mode)
                return true;

            facility = null;
            return false;
        }

        public static bool Exists(string facilityId, string profileId, out CameraDeckMode mode)
        {
            string resolvedProfile = string.IsNullOrWhiteSpace(profileId) ? DefaultProfileId : profileId;
            if (facilities.TryGetValue(Key(facilityId, resolvedProfile), out CameraDeckFacilityDefinition facility))
            {
                mode = facility.Mode;
                return true;
            }

            mode = default;
            return false;
        }

        public static IEnumerable<CameraDeckFacilityDefinition> All => facilities.Values;

        private static string Key(string facilityId, string profileId) => facilityId + ":" + profileId;

        private static void Register(CameraDeckFacilityDefinition facility)
        {
            facilities.Add(Key(facility.FacilityId, facility.ProfileId), facility);
        }

        private static CameraDeckFacilityDefinition Create(string id, string name, CameraDeckMode mode,
            string[] map, DeckCameraMount[] mounts, DeckIntrusionRoute[] routes = null,
            DeckInspectionAnchor[] anchors = null, string policy = "", int primaryAsset = 0)
        {
            CameraDeckFacilityDefinition facility = ScriptableObject.CreateInstance<CameraDeckFacilityDefinition>();
            facility.hideFlags = HideFlags.HideAndDontSave;
            facility.Initialize(id, DefaultProfileId, name, mode, map, mounts, routes, anchors, policy, primaryAsset);
            return facility;
        }

        private static CameraDeckFacilityDefinition BuildArchiveDepot()
        {
            string[] map =
            {
                "###########################################",
                "#........#..#.........#....#.......#......#",
                "#........#..#.........#....#.......#......#",
                "#...A....#..#.K..A....#....#...K...#......#",
                "#........#..#.........#....#...........A..#",
                "#......K.#..#.........#............#......#",
                "#........#..#.......K.#....#.......#......#",
                "#...........#.........#....#.......#..K...#",
                "#........#..###.####.##....#.......#......#",
                "#........#.................#..............#",
                "#####.####................................#",
                "#..........................#..............#",
                "#..........................########.#######",
                "#.........................................#",
                "####.#########...###.#####................#",
                "#............#...#.......#................#",
                "#............#...#.......#.....######.#####",
                "#............#...#..K....#.....#..........#",
                "#............#...........#.....#..........#",
                "#...K........#...#.............#..........#",
                "#................#.......#.....#...K......#",
                "#............#...######.##.....#..........#",
                "######.##....#............................#",
                "#............#..##.########.##.#..........#",
                "#............#..#............#.#..#####.###",
                "#...............#............#.#..........#",
                "#............#..#............#.#..........#",
                "#............#..#............#........K...#",
                "#............#..#............#.#..........#",
                "#.E..........#..#....E.......#.#........E.#",
                "###########################################",
            };

            DeckCameraMount[] mounts =
            {
                new DeckCameraMount(new Vector2Int(3, 7), "WEST RECEIVING", FullRotationYaws(135f)),
                new DeckCameraMount(new Vector2Int(7, 3), "NORTH ARCHIVE", FullRotationYaws(135f)),
                new DeckCameraMount(new Vector2Int(14, 5), "VAULT WEST", FullRotationYaws(135f)),
                new DeckCameraMount(new Vector2Int(20, 5), "VAULT EAST", FullRotationYaws(180f)),
                new DeckCameraMount(new Vector2Int(30, 6), "DISPATCH DESK", FullRotationYaws(225f)),
                new DeckCameraMount(new Vector2Int(38, 9), "EAST ARCHIVE", FullRotationYaws(270f)),
                new DeckCameraMount(new Vector2Int(4, 18), "WEST RECORDS", FullRotationYaws(90f)),
                new DeckCameraMount(new Vector2Int(21, 18), "SECURE CAGE", FullRotationYaws(0f)),
                new DeckCameraMount(new Vector2Int(35, 20), "SERVICE NORTH", FullRotationYaws(270f)),
                new DeckCameraMount(new Vector2Int(11, 12), "WEST JUNCTION", FullRotationYaws(90f)),
            };

            DeckIntrusionRoute[] routes =
            {
                new DeckIntrusionRoute("WEST ARCHIVE ROUTE",
                    new Vector2Int(2, 29), new Vector2Int(6, 29),
                    new Vector2Int(6, 15), new Vector2Int(4, 15),
                    new Vector2Int(4, 11), new Vector2Int(5, 11),
                    new Vector2Int(5, 3), new Vector2Int(4, 3)),
                new DeckIntrusionRoute("EAST DISPATCH ROUTE",
                    new Vector2Int(21, 29), new Vector2Int(18, 29),
                    new Vector2Int(18, 22), new Vector2Int(26, 22),
                    new Vector2Int(26, 10), new Vector2Int(27, 10),
                    new Vector2Int(34, 10), new Vector2Int(34, 4),
                    new Vector2Int(39, 4)),
                new DeckIntrusionRoute("VAULT TRANSFER ROUTE",
                    new Vector2Int(40, 29), new Vector2Int(32, 29),
                    new Vector2Int(32, 27), new Vector2Int(30, 27),
                    new Vector2Int(30, 13), new Vector2Int(26, 13),
                    new Vector2Int(26, 9), new Vector2Int(20, 9),
                    new Vector2Int(20, 3), new Vector2Int(17, 3)),
                new DeckIntrusionRoute("TRANSFER HALL ROUTE",
                    new Vector2Int(2, 29), new Vector2Int(12, 29),
                    new Vector2Int(12, 25), new Vector2Int(15, 25),
                    new Vector2Int(15, 10), new Vector2Int(27, 10),
                    new Vector2Int(34, 10), new Vector2Int(34, 4),
                    new Vector2Int(39, 4)),
                new DeckIntrusionRoute("CENTRAL VAULT ROUTE",
                    new Vector2Int(21, 29), new Vector2Int(18, 29),
                    new Vector2Int(18, 22), new Vector2Int(15, 22),
                    new Vector2Int(15, 7), new Vector2Int(17, 7),
                    new Vector2Int(17, 3)),
                new DeckIntrusionRoute("CROSS-FACILITY ARCHIVE ROUTE",
                    new Vector2Int(40, 29), new Vector2Int(32, 29),
                    new Vector2Int(32, 27), new Vector2Int(30, 27),
                    new Vector2Int(30, 22), new Vector2Int(14, 22),
                    new Vector2Int(14, 13), new Vector2Int(5, 13),
                    new Vector2Int(5, 9), new Vector2Int(4, 9),
                    new Vector2Int(4, 3)),
            };

            return Create(AuditFacilityId, "Archive & Logistics Depot", CameraDeckMode.BlindSpotAudit,
                map, mounts, routes);
        }

        private static CameraDeckFacilityDefinition BuildNightOperationsOffice()
        {
            string[] map =
            {
                "###################",
                "#1.......#.......2#",
                "#........#........#",
                "#........#........#",
                "#........#........#",
                "####.#########.####",
                "#........E........#",
                "####.#########.####",
                "#........#........#",
                "#........#........#",
                "#........#........#",
                "#3.......#.......4#",
                "###################",
            };

            DeckCameraMount[] mounts =
            {
                new DeckCameraMount(new Vector2Int(1, 1), "LOBBY & PRINT", new[] { 135f }),
                new DeckCameraMount(new Vector2Int(17, 1), "FINANCE WING", new[] { 225f }),
                new DeckCameraMount(new Vector2Int(1, 11), "OPERATIONS ROOM", new[] { 45f }),
                new DeckCameraMount(new Vector2Int(17, 11), "SERVER ROOM", new[] { 315f }),
            };

            DeckInspectionAnchor[] anchors =
            {
                Anchor(3, 2, DeckInspectionProp.Monitor,
                    "FINANCE TERMINAL — USER SESSION ACTIVE, DESK UNATTENDED",
                    "SOC CONSOLE — ASSIGNED OPERATOR PRESENT, ACTIVE SHIFT"),
                Anchor(6, 2, DeckInspectionProp.Note,
                    "HANDWRITTEN RECOVERY CODE BESIDE A SHARED TERMINAL",
                    "ASSET INVENTORY NUMBER ATTACHED TO THE MONITOR FRAME"),
                Anchor(12, 2, DeckInspectionProp.Printout,
                    "PAYROLL EXPORT IN THE OUTPUT TRAY AFTER COLLECTION WINDOW",
                    "BLANK PRINTER CALIBRATION PAGE IN THE OUTPUT TRAY"),
                Anchor(15, 2, DeckInspectionProp.Badge,
                    "VISITOR BADGE ON AN EMPTY DESK, NO ESCORT IN THE AREA",
                    "CONTRACTOR BADGE WORN BY A PERSON WITH AN ACTIVE WORK ORDER"),
                Anchor(3, 4, DeckInspectionProp.Monitor,
                    "HR WORKSTATION DISPLAYING EMPLOYEE RECORDS, CHAIR EMPTY",
                    "PUBLIC DASHBOARD DISPLAYED ON A WALL STATUS SCREEN"),
                Anchor(6, 4, DeckInspectionProp.UsbDrive,
                    "UNREGISTERED USB DRIVE CONNECTED TO A FINANCE TERMINAL",
                    "SEALED BACKUP MEDIA WITH A CURRENT CHECKOUT TAG"),
                Anchor(12, 4, DeckInspectionProp.Monitor,
                    "ADMIN CONSOLE ACTIVE OUTSIDE THE APPROVED SUPPORT WINDOW",
                    "MONITOR SHOWING THE LOCK SCREEN AND ASSET OWNER LABEL"),
                Anchor(15, 4, DeckInspectionProp.Printout,
                    "CUSTOMER EXPORT LEFT ON A MEETING TABLE",
                    "SHREDDING MANIFEST IN THE CONTROLLED DISPOSAL TRAY"),
                Anchor(3, 9, DeckInspectionProp.Badge,
                    "MASTER ACCESS BADGE BESIDE AN UNOCCUPIED WORKSTATION",
                    "TEMPORARY BADGE STORED IN THE STAFFED RECEPTION DRAWER"),
                Anchor(7, 8, DeckInspectionProp.Rack,
                    "SERVER RACK OPEN, NO MAINTENANCE TICKET ON THE SHIFT BOARD",
                    "SERVER RACK OPEN WITH THE APPROVED MAINTENANCE ID DISPLAYED"),
                Anchor(11, 8, DeckInspectionProp.Rack,
                    "NETWORK CABINET UNLATCHED AFTER THE CHANGE WINDOW ENDED",
                    "NETWORK CABINET CLOSED; STATUS LIGHTS VISIBLE THROUGH THE DOOR"),
                Anchor(15, 9, DeckInspectionProp.UsbDrive,
                    "PORTABLE BACKUP DRIVE LEFT BESIDE THE SERVER CAGE",
                    "ENCRYPTED DIAGNOSTIC DRIVE ATTACHED TO THE ACTIVE SERVICE CART"),
                Anchor(6, 10, DeckInspectionProp.Door,
                    "EMERGENCY EXIT HELD OPEN WITH NO EVACUATION IN PROGRESS",
                    "DELIVERY DOOR OPEN WHILE THE SCHEDULED COURIER IS PRESENT"),
                Anchor(12, 10, DeckInspectionProp.Door,
                    "SERVER CAGE DOOR OPEN AND THE ACCESS LOG SHOWS NO ENTRY",
                    "SERVER CAGE DOOR CLOSED WITH THE ACCESS INDICATOR GREEN"),
            };

            const string policy = "OPERATING RULES\n"
                + "• Unattended sensitive sessions must be locked.\n"
                + "• Removable media requires registration or an active checkout tag.\n"
                + "• Sensitive printouts must be collected or placed in controlled disposal.\n"
                + "• Visitors require an escort; maintenance requires a current work order.\n"
                + "• Controlled doors and racks may remain open only during an approved operation.";

            return Create(SweepFacilityId, "Night Operations Office", CameraDeckMode.CleanDeskSweep,
                map, mounts, anchors: anchors, policy: policy);
        }

        private static CameraDeckFacilityDefinition BuildRestrictedResearchLab()
        {
            string[] map =
            {
                "#########################",
                "#....#....#.....#...#..A#",
                "#....#....#.....#...#...#",
                "#....#....#.....#...#...#",
                "#....#....#.....#...#...#",
                "#....#....#.....#...#...#",
                "#....#....#.....#...#...#",
                "####.#.########.#.#####.#",
                "#.......................#",
                "#.......................#",
                "##.########.#.########.##",
                "#SSS..#.....#.....#.....#",
                "#SSS..#.....#.....#.....#",
                "#.....#.....#.....#.....#",
                "#.....#.....#.....#.....#",
                "#E....#.....#.....#.....#",
                "#########################",
            };

            DeckCameraMount[] mounts =
            {
                new DeckCameraMount(new Vector2Int(3, 3), "NORTHWEST LAB", new[] { 0f }, true, 0f, 0f, true),
                new DeckCameraMount(new Vector2Int(8, 3), "NORTH ANALYSIS", new[] { 0f }, true, 0f, 45f, true),
                new DeckCameraMount(new Vector2Int(13, 3), "CENTRAL RESEARCH", new[] { 0f }, true, 0f, 90f, true),
                new DeckCameraMount(new Vector2Int(18, 3), "SPECIMEN ROOM", new[] { 0f }, true, 0f, 135f, true),
                new DeckCameraMount(new Vector2Int(22, 3), "TARGET SUITE", new[] { 0f }, true, 0f, 180f, true),
                new DeckCameraMount(new Vector2Int(9, 13), "SOUTH ANALYSIS", new[] { 0f }, true, 0f, 225f, true),
                new DeckCameraMount(new Vector2Int(15, 13), "SOUTH RESEARCH", new[] { 0f }, true, 0f, 270f, true),
                new DeckCameraMount(new Vector2Int(21, 13), "SOUTHEAST LAB", new[] { 0f }, true, 0f, 315f, true),
            };

            return Create(RouteFacilityId, "Restricted Research Lab", CameraDeckMode.BlindRoute,
                map, mounts);
        }

        private static DeckInspectionAnchor Anchor(int col, int row, DeckInspectionProp prop,
            string genuine, string compliant)
        {
            return new DeckInspectionAnchor(new Vector2Int(col, row), prop, genuine, compliant);
        }

        private static float[] FullRotationYaws(float initialYaw = 0f)
        {
            float[] yaws = new float[8];
            for (int index = 0; index < yaws.Length; index++)
                yaws[index] = Mathf.Repeat(initialYaw + index * 45f, 360f);
            return yaws;
        }
    }
}
