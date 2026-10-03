using System.Collections.Generic;
using UnityEngine;

namespace Tactics.Core
{
    public sealed class GridBuildResult
    {
        public GridMap Map { get; }
        public IReadOnlyList<string> Errors { get; }
        public bool Success => Map != null;

        internal GridBuildResult(GridMap map, IReadOnlyList<string> errors)
        {
            Map = map;
            Errors = errors;
        }
    }

    /// <summary>
    /// Builds a GridMap from a flat layout. Collects every validation error instead of
    /// stopping at the first one, so a bad CSV is reported in a single pass.
    /// </summary>
    public static class GridMapBuilder
    {
        /// <param name="terrainIds">Row-major, index = z * width + x (z = 0 is the near row).</param>
        /// <param name="heights">Same layout as <paramref name="terrainIds"/>.</param>
        /// <param name="terrainTypes">Terrain types the layout may reference; ids must be unique.</param>
        public static GridBuildResult Build(
            int width,
            int depth,
            IReadOnlyList<string> terrainIds,
            IReadOnlyList<int> heights,
            IEnumerable<TerrainDefinition> terrainTypes)
        {
            var errors = new List<string>();

            if (width <= 0 || depth <= 0)
                errors.Add($"Grid size must be positive, got {width}x{depth}.");
            if (terrainIds == null)
                errors.Add("Terrain id list is missing.");
            if (heights == null)
                errors.Add("Height list is missing.");
            if (terrainTypes == null)
                errors.Add("Terrain type list is missing.");
            if (errors.Count > 0)
                return new GridBuildResult(null, errors);

            var terrains = new Dictionary<string, TerrainDefinition>();
            foreach (TerrainDefinition terrain in terrainTypes)
            {
                if (terrain == null)
                    errors.Add("Terrain type list contains a null entry.");
                else if (!terrains.TryAdd(terrain.Id, terrain))
                    errors.Add($"Duplicate terrain id '{terrain.Id}'.");
            }
            if (errors.Count > 0)
                return new GridBuildResult(null, errors);

            int expected = width * depth;
            if (terrainIds.Count != expected)
                errors.Add($"Expected {expected} terrain ids for {width}x{depth}, got {terrainIds.Count}.");
            if (heights.Count != expected)
                errors.Add($"Expected {expected} heights for {width}x{depth}, got {heights.Count}.");
            if (errors.Count > 0)
                return new GridBuildResult(null, errors);

            var tiles = new Tile[width, depth];
            for (int z = 0; z < depth; z++)
            for (int x = 0; x < width; x++)
            {
                int index = z * width + x;
                string id = terrainIds[index];
                int height = heights[index];

                if (height < 0)
                    errors.Add($"({x}, {z}): height {height} is negative.");

                if (id == null || !terrains.TryGetValue(id, out TerrainDefinition terrain))
                {
                    errors.Add($"({x}, {z}): unknown terrain id '{id}'.");
                    continue;
                }

                if (errors.Count == 0)
                    tiles[x, z] = new Tile(new Vector2Int(x, z), height, terrain);
            }

            return errors.Count > 0
                ? new GridBuildResult(null, errors)
                : new GridBuildResult(new GridMap(tiles), errors);
        }
    }
}
