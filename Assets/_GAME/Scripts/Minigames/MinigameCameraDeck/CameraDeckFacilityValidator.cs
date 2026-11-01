using System;
using System.Collections.Generic;
using UnityEngine;

namespace Minigames
{
    /// <summary>
    /// Canonical author-time validation. It consumes the same facility definitions as the
    /// runtime, eliminating the previous copy of every layout in a Python script.
    /// </summary>
    public static class CameraDeckFacilityValidator
    {
        private const float CellSize = 1f;
        private const float RouteTimeStep = 0.125f;
        private const float RouteHorizonSeconds = 120f;

        private struct DifficultyProfile
        {
            public string name;
            public int budget;
            public int rating;
            public float range;
            public float halfAngle;
            public float speedMultiplier;

            public DifficultyProfile(string name, int budget, int rating, float range,
                float halfAngle, float speedMultiplier)
            {
                this.name = name;
                this.budget = budget;
                this.rating = rating;
                this.range = range;
                this.halfAngle = halfAngle;
                this.speedMultiplier = speedMultiplier;
            }
        }

        private readonly struct AuditCoverageOption
        {
            public readonly int routeMask;
            public readonly int targetMask;

            public AuditCoverageOption(int routeMask, int targetMask)
            {
                this.routeMask = routeMask;
                this.targetMask = targetMask;
            }
        }

        private static readonly DifficultyProfile[] AuditProfiles =
        {
            new DifficultyProfile("Easy", 3, 75, 10.5f, 45f, 1f),
            new DifficultyProfile("Normal", 3, 80, 10.5f, 45f, 1f),
            new DifficultyProfile("Hard", 3, 90, 10.5f, 45f, 1f),
        };

        private static readonly DifficultyProfile[] RouteProfiles =
        {
            new DifficultyProfile("Easy", 0, 0, 8.5f, 34f, 0.8f),
            new DifficultyProfile("Normal", 0, 0, 9f, 38f, 1f),
            new DifficultyProfile("Hard", 0, 0, 9.5f, 42f, 1.2f),
        };

        public static bool ValidateAll(out string report)
        {
            List<string> issues = new List<string>();
            foreach (CameraDeckFacilityDefinition facility in CameraDeckFacilityCatalog.All)
                ValidateFacility(facility, issues);

            report = issues.Count == 0
                ? "All Camera Deck facilities passed structural and gameplay validation."
                : string.Join("\n", issues);
            return issues.Count == 0;
        }

        private static void ValidateFacility(CameraDeckFacilityDefinition facility, List<string> issues)
        {
            string prefix = $"[{facility.FacilityId}]";
            if (facility.Map == null || facility.Map.Length < 3)
            {
                issues.Add($"{prefix} map is missing or too small.");
                return;
            }

            int width = facility.Map[0].Length;
            for (int row = 0; row < facility.Map.Length; row++)
            {
                if (facility.Map[row].Length != width)
                    issues.Add($"{prefix} row {row} has length {facility.Map[row].Length}; expected {width}.");

                for (int col = 0; col < facility.Map[row].Length; col++)
                {
                    bool border = row == 0 || row == facility.Map.Length - 1 || col == 0 || col == width - 1;
                    if (border && facility.Map[row][col] != '#')
                        issues.Add($"{prefix} border cell {col},{row} is not a wall.");
                }
            }

            DeckLayout layout;
            try
            {
                layout = new DeckLayout(facility);
            }
            catch (Exception exception)
            {
                issues.Add($"{prefix} could not build: {exception.Message}");
                return;
            }

            if (layout.entryCells.Count == 0)
                issues.Add($"{prefix} has no entry.");
            if (facility.Mode != CameraDeckMode.CleanDeskSweep && layout.assetCells.Count == 0)
                issues.Add($"{prefix} has no target asset.");

            foreach (DeckCameraMount mount in layout.mounts)
            {
                if (layout.IsWalkable(mount.cell) == false)
                    issues.Add($"{prefix} camera '{mount.label}' is placed in a wall.");
            }

            if (facility.Mode == CameraDeckMode.BlindSpotAudit)
                ValidateAudit(layout, issues);
            else if (facility.Mode == CameraDeckMode.CleanDeskSweep)
                ValidateSweep(layout, issues);
            else
                ValidateRoute(layout, issues);
        }

        private static void ValidateAudit(DeckLayout layout, List<string> issues)
        {
            string prefix = $"[{layout.facilityId}]";
            if (layout.entryCells.Count != 3 || layout.assetCells.Count != 3
                || layout.mounts.Count != 10 || layout.intrusionRoutes.Count != 6)
                issues.Add($"{prefix} audit requires 3 entries, 3 assets, 10 mounts and 6 routes.");

            foreach (DeckCameraMount mount in layout.mounts)
            {
                if (HasFullAuditRotation(mount) == false)
                    issues.Add($"{prefix} camera '{mount.label}' must support all eight 45-degree orientations.");
            }

            foreach (DeckIntrusionRoute route in layout.intrusionRoutes)
            {
                if (route.waypoints.Count < 2)
                {
                    issues.Add($"{prefix} route '{route.label}' has fewer than two waypoints.");
                    continue;
                }

                if (layout.entryCells.Contains(route.waypoints[0]) == false)
                    issues.Add($"{prefix} route '{route.label}' does not start at an entry.");
                if (layout.assetCells.Contains(route.waypoints[route.waypoints.Count - 1]) == false)
                    issues.Add($"{prefix} route '{route.label}' does not end at an asset.");

                for (int leg = 0; leg < route.waypoints.Count - 1; leg++)
                {
                    if (IsWalkableRouteLeg(layout, route.waypoints[leg], route.waypoints[leg + 1]) == false)
                        issues.Add($"{prefix} route '{route.label}' crosses a wall or uses a diagonal leg.");
                }
            }

            foreach (DifficultyProfile profile in AuditProfiles)
            {
                AuditCoverageOption[][] options = BuildAuditCoverageOptions(layout, profile);
                int minimumPassingCameras = FindMinimumPassingAuditCameras(layout, profile, options);
                const int requiredMinimum = 3;

                if (minimumPassingCameras == int.MaxValue)
                    issues.Add($"{prefix} has no passing {profile.name} audit configuration.");
                else if (minimumPassingCameras < requiredMinimum)
                    issues.Add($"{prefix} {profile.name} can pass with {minimumPassingCameras} cameras; "
                        + $"at least {requiredMinimum} must be required.");
            }
        }

        private static bool HasFullAuditRotation(DeckCameraMount mount)
        {
            if (mount.allowedYaws.Length != 8)
                return false;

            for (int step = 0; step < 8; step++)
            {
                float expected = step * 45f;
                bool found = false;
                foreach (float yaw in mount.allowedYaws)
                {
                    if (Mathf.Abs(Mathf.DeltaAngle(yaw, expected)) < 0.01f)
                    {
                        found = true;
                        break;
                    }
                }

                if (found == false)
                    return false;
            }

            return true;
        }

        private static bool IsWalkableRouteLeg(DeckLayout layout, Vector2Int from, Vector2Int to)
        {
            if (from.x != to.x && from.y != to.y)
                return false;

            Vector2Int direction = new Vector2Int(Math.Sign(to.x - from.x), Math.Sign(to.y - from.y));
            Vector2Int current = from;
            while (true)
            {
                if (layout.IsWalkable(current) == false)
                    return false;
                if (current == to)
                    return true;
                current += direction;
            }
        }

        private static int FindMinimumPassingAuditCameras(DeckLayout layout,
            DifficultyProfile profile, AuditCoverageOption[][] options)
        {
            const int routeShift = 3;
            int targetShift = routeShift + layout.intrusionRoutes.Count;
            int routeMaskBits = (1 << layout.intrusionRoutes.Count) - 1;
            HashSet<int> states = new HashSet<int> { 0 };

            for (int mountIndex = 0; mountIndex < options.Length; mountIndex++)
            {
                HashSet<int> next = new HashSet<int>(states);
                foreach (int state in states)
                {
                    int installed = state & 0x7;
                    if (installed >= profile.budget)
                        continue;

                    int routeMask = (state >> routeShift) & routeMaskBits;
                    int targetMask = state >> targetShift;
                    foreach (AuditCoverageOption option in options[mountIndex])
                    {
                        int packed = (installed + 1)
                            | ((routeMask | option.routeMask) << routeShift)
                            | ((targetMask | option.targetMask) << targetShift);
                        next.Add(packed);
                    }
                }

                states = next;
            }

            int minimum = int.MaxValue;
            foreach (int state in states)
            {
                int installed = state & 0x7;
                if (installed == 0 || installed >= minimum)
                    continue;

                int routeMask = (state >> routeShift) & routeMaskBits;
                int targetMask = state >> targetShift;
                int rating = Mathf.RoundToInt(
                    CountBits(routeMask) / (float)layout.intrusionRoutes.Count * 80f
                    + CountBits(targetMask) / (float)(layout.assetCells.Count + layout.entryCells.Count) * 20f);
                if (rating >= profile.rating)
                    minimum = installed;
            }

            return minimum;
        }

        private static AuditCoverageOption[][] BuildAuditCoverageOptions(
            DeckLayout layout, DifficultyProfile profile)
        {
            CameraDeckVisibilityService visibility = new CameraDeckVisibilityService(layout, CellSize);
            List<Vector2Int> targets = new List<Vector2Int>(layout.assetCells);
            targets.AddRange(layout.entryCells);
            AuditCoverageOption[][] options = new AuditCoverageOption[layout.mounts.Count][];

            for (int mountIndex = 0; mountIndex < layout.mounts.Count; mountIndex++)
            {
                DeckCameraMount mount = layout.mounts[mountIndex];
                options[mountIndex] = new AuditCoverageOption[mount.allowedYaws.Length];

                for (int yawIndex = 0; yawIndex < mount.allowedYaws.Length; yawIndex++)
                {
                    float yaw = mount.allowedYaws[yawIndex];
                    int routeMask = 0;
                    for (int routeIndex = 0; routeIndex < layout.intrusionRoutes.Count; routeIndex++)
                    {
                        if (AuditRouteIsCovered(visibility, mount.cell, yaw,
                            layout.intrusionRoutes[routeIndex], profile))
                            routeMask |= 1 << routeIndex;
                    }

                    int targetMask = 0;
                    for (int targetIndex = 0; targetIndex < targets.Count; targetIndex++)
                    {
                        if (visibility.ConeCovers(mount.cell, yaw,
                            visibility.CellCenter(targets[targetIndex]), profile.range, profile.halfAngle))
                            targetMask |= 1 << targetIndex;
                    }

                    options[mountIndex][yawIndex] = new AuditCoverageOption(routeMask, targetMask);
                }
            }

            return options;
        }

        private static bool AuditRouteIsCovered(CameraDeckVisibilityService visibility,
            Vector2Int cameraCell, float yaw, DeckIntrusionRoute route, DifficultyProfile profile)
        {
            for (int leg = 0; leg < route.waypoints.Count - 1; leg++)
            {
                Vector3 from = visibility.CellCenter(route.waypoints[leg]);
                Vector3 to = visibility.CellCenter(route.waypoints[leg + 1]);
                int samples = Mathf.Max(1, Mathf.CeilToInt(Vector3.Distance(from, to) * 4f));
                for (int sample = 0; sample <= samples; sample++)
                {
                    Vector3 point = Vector3.Lerp(from, to, sample / (float)samples);
                    if (visibility.ConeCovers(cameraCell, yaw, point, profile.range, profile.halfAngle))
                        return true;
                }
            }

            return false;
        }

        private static void ValidateSweep(DeckLayout layout, List<string> issues)
        {
            string prefix = $"[{layout.facilityId}]";
            if (layout.mounts.Count != 4 || layout.inspectionAnchors.Count != 14)
                issues.Add($"{prefix} sweep requires 4 cameras and 14 inspection anchors.");
            if (string.IsNullOrWhiteSpace(layout.policyText))
                issues.Add($"{prefix} has no policy text.");

            CameraDeckVisibilityService visibility = new CameraDeckVisibilityService(layout, CellSize);
            foreach (DeckInspectionAnchor anchor in layout.inspectionAnchors)
            {
                bool visible = false;
                foreach (DeckCameraMount mount in layout.mounts)
                {
                    if (visibility.ConeCovers(mount.cell, mount.allowedYaws[0],
                        visibility.CellCenter(anchor.cell), 9f, 45f))
                    {
                        visible = true;
                        break;
                    }
                }

                if (visible == false)
                    issues.Add($"{prefix} {anchor.prop} anchor at {anchor.cell} is not camera-visible.");
                if (string.IsNullOrWhiteSpace(anchor.genuineFacts) || string.IsNullOrWhiteSpace(anchor.compliantFacts))
                    issues.Add($"{prefix} {anchor.prop} anchor at {anchor.cell} lacks contextual facts.");
            }
        }

        private static void ValidateRoute(DeckLayout layout, List<string> issues)
        {
            string prefix = $"[{layout.facilityId}]";
            if (layout.mounts.Count != 8)
                issues.Add($"{prefix} route facility requires exactly 8 active room cameras.");

            foreach (DeckCameraMount camera in layout.mounts)
            {
                if (camera.routeActive == false || camera.continuousRotation == false)
                    issues.Add($"{prefix} route camera '{camera.label}' must be active and rotate continuously.");
            }

            if (layout.deskCells.Count > 0 || layout.rackCells.Count > 0 || layout.plantCells.Count > 0)
                issues.Add($"{prefix} route rooms must not contain decorative props.");

            if (layout.safePocketCells.Count < 6)
                issues.Add($"{prefix} route facility requires at least 6 authored safe pockets.");

            CameraDeckVisibilityService visibility = new CameraDeckVisibilityService(layout, CellSize);
            foreach (Vector2Int pocket in layout.safePocketCells)
            {
                if (MaximumSweepMask(layout, visibility, pocket, RouteProfiles[2]) != 0)
                    issues.Add($"{prefix} safe pocket {pocket} is covered by a maximum Hard camera sweep.");
            }

            foreach (DifficultyProfile profile in RouteProfiles)
            {
                if (HasPermanentSafePath(layout, profile))
                    issues.Add($"{prefix} has a permanently safe entry-to-target path on {profile.name}.");
                if (EveryPathCrossesCameraSweeps(layout, profile, 2) == false)
                    issues.Add($"{prefix} has a {profile.name} route crossing fewer than two maximum camera sweeps.");
                if (HasTimedRoute(layout, profile) == false)
                    issues.Add($"{prefix} has no time-valid entry-to-target route on {profile.name}.");
            }
        }

        private static bool EveryPathCrossesCameraSweeps(DeckLayout layout,
            DifficultyProfile profile, int requiredCameras)
        {
            CameraDeckVisibilityService visibility = new CameraDeckVisibilityService(layout, CellSize);
            Queue<Tuple<Vector2Int, int>> queue = new Queue<Tuple<Vector2Int, int>>();
            HashSet<int> visited = new HashSet<int>();
            int initialMask = MaximumSweepMask(layout, visibility, layout.EntryCell, profile);
            queue.Enqueue(Tuple.Create(layout.EntryCell, initialMask));
            visited.Add(RouteMaskKey(layout, layout.EntryCell, initialMask));
            Vector2Int[] directions = { Vector2Int.up, Vector2Int.right, Vector2Int.down, Vector2Int.left };

            while (queue.Count > 0)
            {
                Tuple<Vector2Int, int> state = queue.Dequeue();
                if (state.Item1 == layout.PrimaryAssetCell)
                {
                    if (CountBits(state.Item2) < requiredCameras)
                        return false;
                    continue;
                }

                foreach (Vector2Int direction in directions)
                {
                    Vector2Int next = state.Item1 + direction;
                    if (layout.IsWalkable(next) == false)
                        continue;

                    int mask = state.Item2 | MaximumSweepMask(layout, visibility, next, profile);
                    int key = RouteMaskKey(layout, next, mask);
                    if (visited.Add(key))
                        queue.Enqueue(Tuple.Create(next, mask));
                }
            }

            return true;
        }

        private static int MaximumSweepMask(DeckLayout layout,
            CameraDeckVisibilityService visibility, Vector2Int cell, DifficultyProfile profile)
        {
            int mask = 0;
            for (int index = 0; index < layout.mounts.Count; index++)
            {
                DeckCameraMount camera = layout.mounts[index];
                float maximumHalfAngle = camera.continuousRotation
                    ? 180f
                    : profile.halfAngle + camera.sweepArcDegrees;
                if (visibility.ConeCovers(camera.cell, camera.allowedYaws[0],
                    visibility.CellCenter(cell), profile.range, maximumHalfAngle))
                {
                    mask |= 1 << index;
                }
            }
            return mask;
        }

        private static int RouteMaskKey(DeckLayout layout, Vector2Int cell, int mask)
        {
            return mask * layout.cols * layout.rows + cell.y * layout.cols + cell.x;
        }

        private static int CountBits(int value)
        {
            int count = 0;
            while (value != 0)
            {
                value &= value - 1;
                count++;
            }
            return count;
        }

        private static bool HasPermanentSafePath(DeckLayout layout, DifficultyProfile profile)
        {
            CameraDeckVisibilityService visibility = new CameraDeckVisibilityService(layout, CellSize);
            HashSet<Vector2Int> safe = new HashSet<Vector2Int>();
            for (int row = 0; row < layout.rows; row++)
            {
                for (int col = 0; col < layout.cols; col++)
                {
                    Vector2Int cell = new Vector2Int(col, row);
                    if (layout.IsWalkable(cell) == false)
                        continue;

                    bool everCovered = false;
                    foreach (DeckCameraMount camera in layout.mounts)
                    {
                        float unionHalfAngle = camera.continuousRotation
                            ? 180f
                            : profile.halfAngle + camera.sweepArcDegrees;
                        if (visibility.ConeCovers(camera.cell, camera.allowedYaws[0],
                            visibility.CellCenter(cell), profile.range, unionHalfAngle))
                        {
                            everCovered = true;
                            break;
                        }
                    }
                    if (everCovered == false)
                        safe.Add(cell);
                }
            }

            return Reachable(layout.EntryCell, layout.PrimaryAssetCell, safe);
        }

        private static bool HasTimedRoute(DeckLayout layout, DifficultyProfile profile)
        {
            int steps = Mathf.CeilToInt(RouteHorizonSeconds / RouteTimeStep);
            int moveSteps = Mathf.Max(1, Mathf.CeilToInt(1f / (2.6f * RouteTimeStep)));
            CameraDeckVisibilityService visibility = new CameraDeckVisibilityService(layout, CellSize);
            Queue<Tuple<Vector2Int, int>> queue = new Queue<Tuple<Vector2Int, int>>();
            HashSet<int> visited = new HashSet<int>();
            queue.Enqueue(Tuple.Create(layout.EntryCell, 0));
            visited.Add(StateKey(layout, layout.EntryCell, 0));

            Vector2Int[] directions = { Vector2Int.up, Vector2Int.right, Vector2Int.down, Vector2Int.left };
            while (queue.Count > 0)
            {
                Tuple<Vector2Int, int> state = queue.Dequeue();
                if (state.Item1 == layout.PrimaryAssetCell)
                    return true;
                if (state.Item2 >= steps)
                    continue;

                TryQueueTimedState(layout, visibility, profile, state.Item1, state.Item1,
                    state.Item2, 1, queue, visited);
                foreach (Vector2Int direction in directions)
                {
                    Vector2Int next = state.Item1 + direction;
                    if (layout.IsWalkable(next))
                        TryQueueTimedState(layout, visibility, profile, state.Item1, next,
                            state.Item2, moveSteps, queue, visited);
                }
            }

            return false;
        }

        private static void TryQueueTimedState(DeckLayout layout,
            CameraDeckVisibilityService visibility, DifficultyProfile profile,
            Vector2Int from, Vector2Int to, int startStep, int durationSteps,
            Queue<Tuple<Vector2Int, int>> queue, HashSet<int> visited)
        {
            Vector3 fromPosition = visibility.CellCenter(from);
            Vector3 toPosition = visibility.CellCenter(to);
            for (int sample = 1; sample <= durationSteps; sample++)
            {
                float t = (startStep + sample) * RouteTimeStep;
                Vector3 position = Vector3.Lerp(fromPosition, toPosition, sample / (float)durationSteps);
                if (SeenAt(layout, visibility, position, t, profile))
                    return;
            }

            int nextStep = startStep + durationSteps;
            int key = StateKey(layout, to, nextStep);
            if (visited.Add(key))
                queue.Enqueue(Tuple.Create(to, nextStep));
        }

        private static bool SeenAt(DeckLayout layout, CameraDeckVisibilityService visibility,
            Vector3 position, float time, DifficultyProfile profile)
        {
            foreach (DeckCameraMount camera in layout.mounts)
            {
                float yaw;
                if (camera.continuousRotation)
                {
                    yaw = camera.allowedYaws[0]
                        + time * 26f * profile.speedMultiplier + camera.sweepPhase;
                }
                else
                {
                    float arc = camera.sweepArcDegrees;
                    if (arc <= 0f)
                        continue;
                    float phase = time * (26f * profile.speedMultiplier) / arc + camera.sweepPhase;
                    float offset = (Mathf.PingPong(phase, 2f) - 1f) * arc;
                    yaw = camera.allowedYaws[0] + offset;
                }

                if (visibility.ConeCovers(camera.cell, yaw,
                    position, profile.range, profile.halfAngle))
                    return true;
            }
            return false;
        }

        private static int StateKey(DeckLayout layout, Vector2Int cell, int timeStep)
        {
            return timeStep * layout.cols * layout.rows + cell.y * layout.cols + cell.x;
        }

        private static bool Reachable(Vector2Int start, Vector2Int goal, HashSet<Vector2Int> allowed)
        {
            if (allowed.Contains(start) == false || allowed.Contains(goal) == false)
                return false;

            Queue<Vector2Int> queue = new Queue<Vector2Int>();
            HashSet<Vector2Int> seen = new HashSet<Vector2Int> { start };
            queue.Enqueue(start);
            Vector2Int[] directions = { Vector2Int.up, Vector2Int.right, Vector2Int.down, Vector2Int.left };
            while (queue.Count > 0)
            {
                Vector2Int current = queue.Dequeue();
                if (current == goal)
                    return true;
                foreach (Vector2Int direction in directions)
                {
                    Vector2Int next = current + direction;
                    if (allowed.Contains(next) && seen.Add(next))
                        queue.Enqueue(next);
                }
            }
            return false;
        }
    }
}
