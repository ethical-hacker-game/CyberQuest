using System.Collections.Generic;
using UnityEngine;

namespace Minigames
{
    /// <summary>
    /// A hand-authored Data Rover arena: an ASCII map (readable in code, easy to author)
    /// plus explicit patrol routes. Map legend: <c>#</c> = wall, <c>.</c> = floor,
    /// <c>S</c> = shard slot, <c>R</c> = rover start. Patrol routes are strings of
    /// "col,row" waypoints; consecutive waypoints (including last-&gt;first, so a 2-point
    /// route is a ping-pong shuttle) must be axis-aligned and unobstructed.
    ///
    /// Fairness is validated at AUTHORING time, not at runtime (see
    /// <c>Docs/Minigames/DataRover.md</c> and <c>Docs/Minigames/tools/validate_rover_layouts.py</c>):
    /// every shard is reachable, every patrol segment is wall-free, and every
    /// shard has a safe time window at Normal patrol speed.
    /// </summary>
    public class RoverLayout
    {
        public readonly string name;
        public readonly int cols;
        public readonly int rows;
        public readonly Vector2Int startCell;
        public readonly List<Vector2Int> shardCells = new List<Vector2Int>();
        public readonly List<List<Vector2Int>> patrolRoutes = new List<List<Vector2Int>>();
        public readonly List<Vector2Int> hardExtraRoute;

        private readonly bool[,] walls; // [col, row]

        public int ShardCapacity => shardCells.Count;

        public RoverLayout(string layoutName, string[] map, string[] routes, string hardRoute)
        {
            name = layoutName;
            rows = map.Length;
            cols = map[0].Length;
            walls = new bool[cols, rows];

            for (int r = 0; r < rows; r++)
            {
                for (int c = 0; c < cols; c++)
                {
                    char cell = map[r][c];
                    walls[c, r] = cell == '#';

                    if (cell == 'S')
                        shardCells.Add(new Vector2Int(c, r));
                    else if (cell == 'R')
                        startCell = new Vector2Int(c, r);
                }
            }

            foreach (string route in routes)
                patrolRoutes.Add(ParseRoute(route));

            hardExtraRoute = ParseRoute(hardRoute);
        }

        /// <summary>Out-of-bounds counts as wall, so nothing can ever leave the arena.</summary>
        public bool IsWall(int col, int row)
        {
            if (col < 0 || row < 0 || col >= cols || row >= rows)
                return true;

            return walls[col, row];
        }

        private static List<Vector2Int> ParseRoute(string route)
        {
            List<Vector2Int> points = new List<Vector2Int>();

            foreach (string token in route.Split(' '))
            {
                string[] parts = token.Split(',');
                points.Add(new Vector2Int(int.Parse(parts[0]), int.Parse(parts[1])));
            }

            return points;
        }
    }

    /// <summary>
    /// The built-in arena pool. <c>layoutIndex</c> in the task settings picks one of
    /// these (-1 = random). All layouts are 15x11 with a full wall border, at least
    /// 9 shard slots and 3 patrol routes plus one extra route used only on Hard.
    /// </summary>
    public static class RoverLayouts
    {
        public static readonly RoverLayout[] BuiltIn =
        {
            new RoverLayout(
                "Server Racks",
                new[]
                {
                    "###############",
                    "#R...S.......S#",
                    "#.##..###..##.#",
                    "#.##S.###.S##.#",
                    "#......S......#",
                    "#S..##...##...#",
                    "#...##.S.##..S#",
                    "#.............#",
                    "#.##.S###..##.#",
                    "#........S....#",
                    "###############",
                },
                new[]
                {
                    "5,1 9,1 9,4 5,4",   // loop around the top-middle rack
                    "8,4 11,4 11,7 8,7", // loop around the mid-right rack
                    "1,7 13,7",          // long shuttle along the lower corridor
                },
                "13,2 13,6"),            // Hard only: right-flank shuttle

            new RoverLayout(
                "Archive Vault",
                new[]
                {
                    "###############",
                    "#S.....S.....S#",
                    "#.#.........#.#",
                    "#....##.##....#",
                    "#....#...#....#",
                    "#R.S.#.S.#.S..#",
                    "#....#...#....#",
                    "#....#####....#",
                    "#.#.........#.#",
                    "#S.....S.....S#",
                    "###############",
                },
                new[]
                {
                    "4,2 10,2 10,8 4,8", // ring around the vault (passes the north gate)
                    "2,1 12,1",          // top corridor shuttle
                    "2,9 12,9",          // bottom corridor shuttle
                },
                "3,2 3,8"),              // Hard only: west-flank shuttle

            new RoverLayout(
                "Quarantine Maze",
                new[]
                {
                    "###############",
                    "#R..S.......S.#",
                    "#..####..####.#",
                    "#.S...........#",
                    "#........S...S#",
                    "#####..####...#",
                    "#S.....S......#",
                    "#...........S.#",
                    "#..####..####.#",
                    "#.S.....S.....#",
                    "###############",
                },
                new[]
                {
                    "1,3 13,3 13,4 1,4", // flat loop through the upper double corridor
                    "13,6 1,6 1,7 13,7", // counter-rotating loop through the lower double corridor
                    "5,3 5,7",           // vertical shuttle guarding the middle gap
                },
                "7,6 7,9"),              // Hard only: shuttle guarding the bottom gap
        };

        /// <summary>Smallest shard capacity across the pool - the safe upper bound for a random layout.</summary>
        public static int MinShardCapacity
        {
            get
            {
                int min = int.MaxValue;

                foreach (RoverLayout layout in BuiltIn)
                    min = Mathf.Min(min, layout.ShardCapacity);

                return min;
            }
        }
    }
}
