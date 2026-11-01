using System.Collections.Generic;
using UnityEngine;

namespace Minigames
{
    /// <summary>Allocation-light four-direction A* used only when the player clicks a destination.</summary>
    public static class CameraDeckRoutePathfinder
    {
        private static readonly Vector2Int[] Directions =
        {
            Vector2Int.up,
            Vector2Int.right,
            Vector2Int.down,
            Vector2Int.left,
        };

        public static bool TryFindPath(DeckLayout layout, Vector2Int start, Vector2Int goal,
            List<Vector2Int> result)
        {
            result.Clear();
            if (layout.IsWalkable(start) == false || layout.IsWalkable(goal) == false)
                return false;

            List<Vector2Int> open = new List<Vector2Int> { start };
            HashSet<Vector2Int> closed = new HashSet<Vector2Int>();
            Dictionary<Vector2Int, Vector2Int> cameFrom = new Dictionary<Vector2Int, Vector2Int>();
            Dictionary<Vector2Int, int> gScore = new Dictionary<Vector2Int, int> { [start] = 0 };

            while (open.Count > 0)
            {
                int bestIndex = 0;
                int bestScore = Score(open[0], goal, gScore);
                for (int i = 1; i < open.Count; i++)
                {
                    int score = Score(open[i], goal, gScore);
                    if (score < bestScore)
                    {
                        bestIndex = i;
                        bestScore = score;
                    }
                }

                Vector2Int current = open[bestIndex];
                open.RemoveAt(bestIndex);
                if (current == goal)
                {
                    Reconstruct(cameFrom, current, result);
                    return true;
                }

                closed.Add(current);
                foreach (Vector2Int direction in Directions)
                {
                    Vector2Int next = current + direction;
                    if (closed.Contains(next) || layout.IsWalkable(next) == false)
                        continue;

                    int tentative = gScore[current] + 1;
                    if (gScore.TryGetValue(next, out int previous) == false || tentative < previous)
                    {
                        cameFrom[next] = current;
                        gScore[next] = tentative;
                        if (open.Contains(next) == false)
                            open.Add(next);
                    }
                }
            }

            return false;
        }

        private static int Score(Vector2Int cell, Vector2Int goal,
            Dictionary<Vector2Int, int> gScore)
        {
            return gScore[cell] + Mathf.Abs(goal.x - cell.x) + Mathf.Abs(goal.y - cell.y);
        }

        private static void Reconstruct(Dictionary<Vector2Int, Vector2Int> cameFrom,
            Vector2Int current, List<Vector2Int> result)
        {
            result.Add(current);
            while (cameFrom.TryGetValue(current, out current))
                result.Add(current);
            result.Reverse();
        }
    }
}
