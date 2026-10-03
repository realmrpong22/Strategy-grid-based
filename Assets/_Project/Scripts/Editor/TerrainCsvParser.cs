using System;
using System.Collections.Generic;
using System.Globalization;
using Tactics.Core;
using UnityEngine;

namespace Tactics.Editor
{
    public sealed class TerrainRow
    {
        public TerrainDefinition Definition { get; }
        public string DisplayName { get; }
        public Color Color { get; }
        public string Id => Definition.Id;

        public TerrainRow(TerrainDefinition definition, string displayName, Color color)
        {
            Definition = definition;
            DisplayName = displayName;
            Color = color;
        }
    }

    /// <summary>
    /// Parses terrain.csv. Columns are found by header name (case-insensitive), so the sheet's
    /// column order doesn't matter and extra columns (notes, etc.) are ignored.
    /// </summary>
    public static class TerrainCsvParser
    {
        public const string IdColumn = "id";
        public const string NameColumn = "name";
        public const string MoveCostColumn = "moveCost";
        public const string PassableColumn = "passable";
        public const string DefenseColumn = "defense";
        public const string AvoidColumn = "avoid";
        public const string ColorColumn = "color";

        private static readonly string[] RequiredColumns =
        {
            IdColumn, NameColumn, MoveCostColumn, PassableColumn, DefenseColumn, AvoidColumn, ColorColumn,
        };

        public static CsvParseResult<IReadOnlyList<TerrainRow>> Parse(string text, string sourceName)
        {
            var errors = new List<string>();
            var terrains = new List<TerrainRow>();

            List<string[]> rows = CsvReader.Parse(text);
            if (rows.Count == 0)
            {
                errors.Add($"{sourceName}: file is empty.");
                return new CsvParseResult<IReadOnlyList<TerrainRow>>(null, errors);
            }

            var columns = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < rows[0].Length; i++)
            {
                string header = rows[0][i].Trim();
                if (header.Length > 0 && !columns.ContainsKey(header))
                    columns.Add(header, i);
            }

            foreach (string required in RequiredColumns)
            {
                if (!columns.ContainsKey(required))
                    errors.Add($"{sourceName}: missing column '{required}'.");
            }
            if (errors.Count > 0)
                return new CsvParseResult<IReadOnlyList<TerrainRow>>(null, errors);

            // Case-insensitive because ids become asset file names, and Windows/macOS file systems are too.
            var seenIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            for (int r = 1; r < rows.Count; r++)
            {
                string[] row = rows[r];
                if (CsvReader.IsBlank(row))
                    continue;

                string where = $"{sourceName} row {r + 1}";
                int errorsBefore = errors.Count;

                string id = Field(IdColumn);
                if (!MapCsvParser.IsValidTerrainId(id))
                    errors.Add($"{where}: id '{id}' must be letters only, so map cells like 'F2' can be split into id and height.");
                else if (!seenIds.Add(id))
                    errors.Add($"{where}: duplicate id '{id}' (ids are case-insensitive).");

                int moveCost = ParseInt(Field(MoveCostColumn), MoveCostColumn);
                bool isPassable = ParseBool(Field(PassableColumn), PassableColumn);
                int defense = ParseInt(Field(DefenseColumn), DefenseColumn);
                int avoid = ParseInt(Field(AvoidColumn), AvoidColumn);
                Color color = ParseColor(Field(ColorColumn), ColorColumn);

                if (errors.Count > errorsBefore)
                    continue;

                try
                {
                    var definition = new TerrainDefinition(id, moveCost, isPassable, defense, avoid);
                    terrains.Add(new TerrainRow(definition, Field(NameColumn), color));
                }
                catch (ArgumentException e)
                {
                    errors.Add($"{where}: {e.Message}");
                }

                string Field(string column)
                {
                    int index = columns[column];
                    return index < row.Length ? row[index].Trim() : string.Empty;
                }

                int ParseInt(string value, string column)
                {
                    if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int result))
                        return result;
                    errors.Add($"{where}, {column}: '{value}' is not a whole number.");
                    return 0;
                }

                bool ParseBool(string value, string column)
                {
                    if (value.Equals("true", StringComparison.OrdinalIgnoreCase) || value == "1")
                        return true;
                    if (value.Equals("false", StringComparison.OrdinalIgnoreCase) || value == "0")
                        return false;
                    errors.Add($"{where}, {column}: '{value}' is not TRUE/FALSE.");
                    return false;
                }

                Color ParseColor(string value, string column)
                {
                    string html = value.StartsWith("#", StringComparison.Ordinal) ? value : "#" + value;
                    if (value.Length > 0 && ColorUtility.TryParseHtmlString(html, out Color result))
                        return result;
                    errors.Add($"{where}, {column}: '{value}' is not a hex colour like #8DB360.");
                    return Color.magenta;
                }
            }

            if (errors.Count == 0 && terrains.Count == 0)
                errors.Add($"{sourceName}: no terrain rows.");

            return new CsvParseResult<IReadOnlyList<TerrainRow>>(errors.Count == 0 ? terrains : null, errors);
        }
    }
}
