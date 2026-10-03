using System;
using UnityEngine;

namespace Tactics.Core
{
    /// <summary>
    /// Grid ↔ local-space conversion. Tile (x, z) is centred at (x * CellSize, _, z * CellSize);
    /// a tile's top surface sits at Height * HeightStep. Positions are local to the grid root,
    /// so the View applies its own transform.
    /// </summary>
    public readonly struct GridMetrics
    {
        public float CellSize { get; }
        public float HeightStep { get; }

        public GridMetrics(float cellSize, float heightStep)
        {
            if (cellSize <= 0f) throw new ArgumentOutOfRangeException(nameof(cellSize), cellSize, "Must be positive.");
            if (heightStep <= 0f) throw new ArgumentOutOfRangeException(nameof(heightStep), heightStep, "Must be positive.");
            CellSize = cellSize;
            HeightStep = heightStep;
        }

        /// <summary>Centre of the tile's top surface.</summary>
        public Vector3 GridToLocal(Vector2Int position, int height) =>
            new Vector3(position.x * CellSize, height * HeightStep, position.y * CellSize);

        /// <summary>
        /// Nearest tile coordinate (ignores y). May be out of bounds; check with GridMap.InBounds.
        /// Uses floor(v + 0.5) rather than Mathf.RoundToInt, which rounds .5 to even and would
        /// make tile borders inconsistent.
        /// </summary>
        public Vector2Int LocalToGrid(Vector3 localPosition) =>
            new Vector2Int(
                Mathf.FloorToInt(localPosition.x / CellSize + 0.5f),
                Mathf.FloorToInt(localPosition.z / CellSize + 0.5f));
    }
}
