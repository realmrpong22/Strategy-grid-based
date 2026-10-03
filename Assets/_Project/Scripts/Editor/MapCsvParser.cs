using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Tactics.Editor
{
    public sealed class MapLayout
    {
        public int Width { get; }
        public int Depth { get; }

        /// <summary>Row-major, index = z * width + x (z = 0 is the near row), matching GridMapBuilder.</summary>
        public string[] TerrainIds { get; }
        public int[] Heights { get; }

        public MapLayout(int width, int depth, string[] terrainIds, int[] heights)
        {
            Width = width;
            Depth = depth;
            TerrainIds = terrainIds;
            Heights = heights;
        }
    }

    /// <summary>
    /// Parses a map CSV: no header, one cell per tile, each cell = terrain id + height (e.g. P0, F1, Wa12).
    /// The sheet is drawn as seen from above with the camera at the bottom, so sheet row 1 is the
    /// far edge (max z) and rows are flipped on import.
    /// </summary>
    public static class MapCsvParser
    {
        public static bool IsValidTerrainId(string id)
        {
            if (string.IsNullOrEmpty(id))
                return false;
            foreach (char c in id)
            {
                if (!char.IsLetter(c))
                    return false;
            }
            return true;
        }

        /// <param name="knownTerrainIds">
        /// Optional. When given, unknown ids are reported here with sheet cell references (e.g. "C4"),
        /// which are easier to find in a spreadsheet than grid coordinates.
        /// </param>
        public static CsvParseResult<MapLayout> Parse(
            string text, string sourceName, ICollection<string> knownTerrainIds = null)
        {
            var errors = new List<string>();
            List<string[]> rows = CsvReader.Parse(text);

            // Spreadsheet exports often pad with blank trailing rows and columns.
            while (rows.Count > 0 && CsvReader.IsBlank(rows[rows.Count - 1]))
                rows.RemoveAt(rows.Count - 1);

            int width = 0;
            foreach (string[] row in rows)
            {
                for (int i = row.Length - 1; i >= 0; i--)
                {
                    if (row[i].Trim().Length > 0)
                    {
                        width = Math.Max(width, i + 1);
                        break;
                    }
                }
            }

            int depth = rows.Count;
            if (width == 0 || depth == 0)
            {
                errors.Add($"{sourceName}: no map cells.");
                return new CsvParseResult<MapLayout>(null, errors);
            }

            var terrainIds = new string[width * depth];
            var heights = new int[width * depth];

            for (int r = 0; r < depth; r++)
            {
                if (CsvReader.IsBlank(rows[r]))
                {
                    errors.Add($"{sourceName} row {r + 1}: blank row inside the map.");
                    continue;
                }

                int z = depth - 1 - r; // sheet row 1 is the far edge of the map
                for (int x = 0; x < width; x++)
                {
                    string cell = x < rows[r].Length ? rows[r][x].Trim() : string.Empty;
                    string where = $"{sourceName} {CellName(x, r)} (x={x}, z={z})";

                    if (cell.Length == 0)
                    {
                        errors.Add($"{where}: empty cell.");
                        continue;
                    }
                    if (!TryParseCell(cell, out string id, out int height))
                    {
                        errors.Add($"{where}: '{cell}' is not a terrain id followed by a height, e.g. P0 or F1.");
                        continue;
                    }
                    if (knownTerrainIds != null && !knownTerrainIds.Contains(id))
                    {
                        errors.Add($"{where}: unknown terrain id '{id}'.");
                        continue;
                    }

                    int index = z * width + x;
                    terrainIds[index] = id;
                    heights[index] = height;
                }
            }

            return errors.Count == 0
                ? new CsvParseResult<MapLayout>(new MapLayout(width, depth, terrainIds, heights), errors)
                : new CsvParseResult<MapLayout>(null, errors);
        }

        /// <summary>Splits "F12" into ("F", 12). Letters then ASCII digits, nothing else.</summary>
        public static bool TryParseCell(string cell, out string id, out int height)
        {
            id = null;
            height = 0;

            int split = 0;
            while (split < cell.Length && char.IsLetter(cell[split]))
                split++;
            if (split == 0 || split == cell.Length)
                return false;

            for (int i = split; i < cell.Length; i++)
            {
                if (cell[i] < '0' || cell[i] > '9')
                    return false;
            }

            if (!int.TryParse(cell.Substring(split), NumberStyles.None, CultureInfo.InvariantCulture, out height))
                return false; // overflow

            id = cell.Substring(0, split);
            return true;
        }

        /// <summary>Spreadsheet-style reference from zero-based indices: (0, 0) → A1, (27, 4) → AB5.</summary>
        public static string CellName(int column, int row)
        {
            var letters = new StringBuilder();
            for (int n = column + 1; n > 0; n = (n - 1) / 26)
                letters.Insert(0, (char)('A' + (n - 1) % 26));
            return letters.Append(row + 1).ToString();
        }
    }
}
