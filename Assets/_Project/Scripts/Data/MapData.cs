using System;
using System.Collections.Generic;
using Tactics.Core;
using UnityEngine;

namespace Tactics.Data
{
    /// <summary>
    /// A map layout, generated from a map CSV by the importer. Layout arrays are flat and
    /// row-major (index = z * width + x, z = 0 is the near row) because Unity can't serialize 2D arrays.
    /// </summary>
    public sealed class MapData : ScriptableObject
    {
        [SerializeField] private int _width;
        [SerializeField] private int _depth;
        [SerializeField] private string[] _terrainIds = Array.Empty<string>();
        [SerializeField] private int[] _heights = Array.Empty<int>();
        [SerializeField] private TerrainTypeData[] _terrainTypes = Array.Empty<TerrainTypeData>();

        public int Width => _width;
        public int Depth => _depth;
        public IReadOnlyList<TerrainTypeData> TerrainTypes => _terrainTypes;

        /// <summary>Builds a fresh Core grid. Check Success / Errors on the result.</summary>
        public GridBuildResult BuildGrid()
        {
            var definitions = new List<TerrainDefinition>(_terrainTypes.Length);
            foreach (TerrainTypeData terrainType in _terrainTypes)
            {
                // A missing reference shows up as "unknown terrain id" errors from the builder.
                if (terrainType != null)
                    definitions.Add(terrainType.ToDefinition());
            }

            return GridMapBuilder.Build(_width, _depth, _terrainIds, _heights, definitions);
        }

        /// <summary>Lets Views find presentation data (e.g. greybox colour) for a Core terrain id.</summary>
        public bool TryGetTerrainType(string id, out TerrainTypeData terrainType)
        {
            foreach (TerrainTypeData candidate in _terrainTypes)
            {
                if (candidate != null && candidate.Id == id)
                {
                    terrainType = candidate;
                    return true;
                }
            }

            terrainType = null;
            return false;
        }

        internal void SetLayout(
            int width, int depth, string[] terrainIds, int[] heights, TerrainTypeData[] terrainTypes)
        {
            _width = width;
            _depth = depth;
            _terrainIds = terrainIds;
            _heights = heights;
            _terrainTypes = terrainTypes;
        }
    }
}
