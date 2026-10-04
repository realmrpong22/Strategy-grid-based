using System;
using System.Collections.Generic;
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

    /// <summary>Parses terrain.csv (header-named columns, see CsvTable).</summary>
    public static class TerrainCsvParser
    {
        public const string IdColumn = "id";
        public const string NameColumn = "name";
        public const string MoveCostColumn = "moveCost";
        public const string PassableColumn = "passable";
        public const string FlyableColumn = "flyable";
        public const string DefenseColumn = "defense";
        public const string AvoidColumn = "avoid";
        public const string ColorColumn = "color";

        private static readonly string[] RequiredColumns =
        {
            IdColumn, NameColumn, MoveCostColumn, PassableColumn, FlyableColumn, DefenseColumn, AvoidColumn, ColorColumn,
        };

        public static CsvParseResult<IReadOnlyList<TerrainRow>> Parse(string text, string sourceName)
        {
            var errors = new List<string>();
            var terrains = new List<TerrainRow>();

            CsvTable table = CsvTable.Parse(text, sourceName, RequiredColumns, errors);
            if (table == null)
                return new CsvParseResult<IReadOnlyList<TerrainRow>>(null, errors);

            // Case-insensitive because ids become asset file names, and Windows/macOS file systems are too.
            var seenIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (CsvTableRow row in table.Rows)
            {
                int errorsBefore = errors.Count;

                string id = row.Text(IdColumn);
                if (!MapCsvParser.IsValidTerrainId(id))
                    row.Error(IdColumn, $"id '{id}' must be letters only, so map cells like 'F2' can be split into id and height.");
                else if (!seenIds.Add(id))
                    row.Error(IdColumn, $"duplicate id '{id}' (ids are case-insensitive).");

                int moveCost = row.Int(MoveCostColumn);
                bool isPassable = row.Bool(PassableColumn);
                bool isFlyable = row.Bool(FlyableColumn);
                int defense = row.Int(DefenseColumn);
                int avoid = row.Int(AvoidColumn);
                Color color = row.Color(ColorColumn);

                if (errors.Count > errorsBefore)
                    continue;

                try
                {
                    var definition = new TerrainDefinition(id, moveCost, isPassable, isFlyable, defense, avoid);
                    terrains.Add(new TerrainRow(definition, row.Text(NameColumn), color));
                }
                catch (ArgumentException e)
                {
                    row.Error(e.Message);
                }
            }

            if (errors.Count == 0 && terrains.Count == 0)
                errors.Add($"{sourceName}: no terrain rows.");

            return new CsvParseResult<IReadOnlyList<TerrainRow>>(errors.Count == 0 ? terrains : null, errors);
        }
    }
}
