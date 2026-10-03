using System;
using UnityEngine;

namespace Tactics.Core
{
    /// <summary>
    /// One cell of the grid. Position is (x, z); height is in whole steps (see GridMetrics.HeightStep).
    /// </summary>
    public sealed class Tile
    {
        public Vector2Int Position { get; }
        public int Height { get; }
        public TerrainDefinition Terrain { get; }

        public Tile(Vector2Int position, int height, TerrainDefinition terrain)
        {
            Position = position;
            Height = height;
            Terrain = terrain ?? throw new ArgumentNullException(nameof(terrain));
        }

        public override string ToString() => $"Tile({Position.x}, {Position.y}) h{Height} {Terrain.Id}";
    }
}
