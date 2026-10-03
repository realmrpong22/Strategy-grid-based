using System;
using System.Collections.Generic;
using UnityEngine;

namespace Tactics.Core
{
    /// <summary>
    /// Rectangular grid of tiles. Coordinates are Vector2Int where .x = x and .y = z (world depth).
    /// Construct through GridMapBuilder, which validates the layout.
    /// </summary>
    public sealed class GridMap
    {
        // 4-directional (Fire Emblem style). Order: +z, +x, -z, -x.
        public static readonly IReadOnlyList<Vector2Int> Directions = new[]
        {
            new Vector2Int(0, 1),
            new Vector2Int(1, 0),
            new Vector2Int(0, -1),
            new Vector2Int(-1, 0),
        };

        private readonly Tile[,] _tiles;

        public int Width { get; }
        public int Depth { get; }

        internal GridMap(Tile[,] tiles)
        {
            _tiles = tiles ?? throw new ArgumentNullException(nameof(tiles));
            Width = tiles.GetLength(0);
            Depth = tiles.GetLength(1);
        }

        public bool InBounds(Vector2Int position) =>
            position.x >= 0 && position.x < Width && position.y >= 0 && position.y < Depth;

        public bool TryGetTile(Vector2Int position, out Tile tile)
        {
            if (!InBounds(position))
            {
                tile = null;
                return false;
            }

            tile = _tiles[position.x, position.y];
            return true;
        }

        public Tile GetTile(Vector2Int position)
        {
            if (!InBounds(position))
                throw new ArgumentOutOfRangeException(nameof(position), position, $"Outside {Width}x{Depth} grid.");
            return _tiles[position.x, position.y];
        }

        /// <summary>
        /// Fills <paramref name="results"/> with in-bounds orthogonal neighbors (cleared first).
        /// Takes a buffer so pathfinding can call it in a loop without allocating.
        /// </summary>
        public void GetNeighbors(Vector2Int position, List<Tile> results)
        {
            if (results == null) throw new ArgumentNullException(nameof(results));
            results.Clear();

            foreach (Vector2Int direction in Directions)
            {
                if (TryGetTile(position + direction, out Tile neighbor))
                    results.Add(neighbor);
            }
        }

        /// <summary>All tiles, ordered row by row: z = 0 first, x ascending within a row.</summary>
        public IEnumerable<Tile> AllTiles()
        {
            for (int z = 0; z < Depth; z++)
            for (int x = 0; x < Width; x++)
                yield return _tiles[x, z];
        }
    }
}
