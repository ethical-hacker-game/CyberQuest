using UnityEngine;

namespace Minigames
{
    /// <summary>Single source of truth for Camera Deck range, angle and wall occlusion.</summary>
    public sealed class CameraDeckVisibilityService
    {
        private readonly DeckLayout layout;
        private readonly float cellSize;

        public CameraDeckVisibilityService(DeckLayout layout, float cellSize)
        {
            this.layout = layout;
            this.cellSize = cellSize;
        }

        public float CastVisionRay(Vector3 origin, Vector3 direction, float maxDistance)
        {
            float step = cellSize * 0.12f;
            for (float distance = step; distance < maxDistance; distance += step)
            {
                Vector3 point = origin + direction * distance;
                if (layout.IsWall(ColAt(point.x), RowAt(point.z)))
                    return distance - step * 0.5f;
            }

            return maxDistance;
        }

        public bool ConeCovers(Vector2Int mountCell, float yaw, Vector3 position,
            float rangeCells, float halfAngleDegrees)
        {
            Vector3 origin = CellCenter(mountCell);
            Vector3 toTarget = position - origin;
            toTarget.y = 0f;
            float distance = toTarget.magnitude;
            float range = rangeCells * cellSize;

            if (distance > range)
                return false;

            Vector3 forward = Quaternion.Euler(0f, yaw, 0f) * Vector3.forward;
            if (distance > 0.001f && Vector3.Angle(forward, toTarget) > halfAngleDegrees)
                return false;

            return distance <= 0.001f
                || CastVisionRay(origin, toTarget.normalized, distance) >= distance - cellSize * 0.1f;
        }

        public Vector3 CellCenter(Vector2Int cell)
        {
            return new Vector3(
                (cell.x - layout.cols * 0.5f + 0.5f) * cellSize,
                0f,
                (layout.rows * 0.5f - cell.y - 0.5f) * cellSize);
        }

        public Vector2Int CellAt(Vector3 position)
        {
            return new Vector2Int(ColAt(position.x), RowAt(position.z));
        }

        private int ColAt(float x) => Mathf.FloorToInt(x / cellSize + layout.cols * 0.5f);
        private int RowAt(float z) => Mathf.FloorToInt(layout.rows * 0.5f - z / cellSize);
    }
}
