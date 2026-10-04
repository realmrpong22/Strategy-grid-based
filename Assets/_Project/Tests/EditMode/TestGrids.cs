using System.Collections.Generic;
using System.Linq;
using Tactics.Core;

namespace Tactics.Tests.EditMode
{
    /// <summary>Shared fixtures. Terrain and class numbers here are test values, not game balance.</summary>
    internal static class TestGrids
    {
        public static readonly TerrainDefinition Plain = new TerrainDefinition("P", 1, true, true, 0, 0);
        public static readonly TerrainDefinition Forest = new TerrainDefinition("F", 2, true, true, 1, 20);
        public static readonly TerrainDefinition Wall = new TerrainDefinition("W", 0, false, false, 0, 0);
        /// <summary>Fliers only.</summary>
        public static readonly TerrainDefinition Lake = new TerrainDefinition("L", 1, false, true, 0, 0);

        public static readonly IReadOnlyList<TerrainDefinition> Terrains = new[] { Plain, Forest, Wall, Lake };

        public static readonly UnitClassDefinition Soldier = new UnitClassDefinition("Soldier", 20, 5, 1, false);
        public static readonly UnitClassDefinition Flier = new UnitClassDefinition("Flier", 18, 7, 0, true);

        /// <summary>Flat grid of plains, all height 0.</summary>
        public static GridMap Flat(int width, int depth)
        {
            int count = width * depth;
            GridBuildResult result = GridMapBuilder.Build(
                width, depth,
                Enumerable.Repeat("P", count).ToArray(),
                new int[count],
                Terrains);
            return result.Map;
        }

        /// <summary>
        /// Grid from space-separated map-style cells ("P0 F1 L0"). Like the map CSV, the first row is
        /// the far edge (max z), so tests read the way the map looks from the default camera.
        /// </summary>
        public static GridMap FromRows(params string[] rows)
        {
            string[][] cells = rows.Select(r => r.Split(new[] { ' ' }, System.StringSplitOptions.RemoveEmptyEntries)).ToArray();
            int width = cells[0].Length;
            int depth = cells.Length;
            var terrainIds = new string[width * depth];
            var heights = new int[width * depth];

            for (int row = 0; row < depth; row++)
            {
                int z = depth - 1 - row;
                for (int x = 0; x < width; x++)
                {
                    string cell = cells[row][x];
                    int letters = cell.TakeWhile(char.IsLetter).Count();
                    terrainIds[z * width + x] = cell.Substring(0, letters);
                    heights[z * width + x] = int.Parse(cell.Substring(letters));
                }
            }

            GridBuildResult result = GridMapBuilder.Build(width, depth, terrainIds, heights, Terrains);
            if (!result.Success)
                throw new System.ArgumentException(string.Join("\n", result.Errors));
            return result.Map;
        }

        /// <summary>Single row (depth 1, height 0) of the given terrain ids, x ascending.</summary>
        public static GridMap Row(params string[] terrainIds)
        {
            GridBuildResult result = GridMapBuilder.Build(
                terrainIds.Length, 1, terrainIds, new int[terrainIds.Length], Terrains);
            return result.Map;
        }
    }
}
