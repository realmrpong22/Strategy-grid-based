using System.Collections.Generic;
using System.Linq;
using Tactics.Core;

namespace Tactics.Tests.EditMode
{
    /// <summary>Shared fixtures. Terrain numbers here are test values, not game balance.</summary>
    internal static class TestGrids
    {
        public static readonly TerrainDefinition Plain = new TerrainDefinition("P", 1, true, 0, 0);
        public static readonly TerrainDefinition Forest = new TerrainDefinition("F", 2, true, 1, 20);
        public static readonly TerrainDefinition Wall = new TerrainDefinition("W", 0, false, 0, 0);

        public static readonly IReadOnlyList<TerrainDefinition> Terrains = new[] { Plain, Forest, Wall };

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
    }
}
