using System;
using System.Collections.Generic;
using Tactics.Core;
using UnityEngine;

namespace Tactics.Editor
{
    public sealed class UnitClassRow
    {
        public UnitClassDefinition Definition { get; }
        public string DisplayName { get; }
        public Color Color { get; }
        public string Id => Definition.Id;

        public UnitClassRow(UnitClassDefinition definition, string displayName, Color color)
        {
            Definition = definition;
            DisplayName = displayName;
            Color = color;
        }
    }

    /// <summary>Parses unit_classes.csv (header-named columns, see CsvTable).</summary>
    public static class UnitClassCsvParser
    {
        public const string IdColumn = "id";
        public const string NameColumn = "name";
        public const string MaxHpColumn = "maxHp";
        public const string MoveColumn = "move";
        public const string JumpColumn = "jump";
        public const string FliesColumn = "flies";
        public const string ColorColumn = "color";

        private static readonly string[] RequiredColumns =
        {
            IdColumn, NameColumn, MaxHpColumn, MoveColumn, JumpColumn, FliesColumn, ColorColumn,
        };

        /// <summary>A letter, then letters, digits or underscores. Ids become asset file names.</summary>
        public static bool IsValidClassId(string id)
        {
            if (string.IsNullOrEmpty(id) || !char.IsLetter(id[0]))
                return false;
            foreach (char c in id)
            {
                if (!char.IsLetterOrDigit(c) && c != '_')
                    return false;
            }
            return true;
        }

        public static CsvParseResult<IReadOnlyList<UnitClassRow>> Parse(string text, string sourceName)
        {
            var errors = new List<string>();
            var classes = new List<UnitClassRow>();

            CsvTable table = CsvTable.Parse(text, sourceName, RequiredColumns, errors);
            if (table == null)
                return new CsvParseResult<IReadOnlyList<UnitClassRow>>(null, errors);

            // Case-insensitive because ids become asset file names; references from deployments are exact.
            var seenIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (CsvTableRow row in table.Rows)
            {
                int errorsBefore = errors.Count;

                string id = row.Text(IdColumn);
                if (!IsValidClassId(id))
                    row.Error(IdColumn, $"id '{id}' must start with a letter and contain only letters, digits or '_'.");
                else if (!seenIds.Add(id))
                    row.Error(IdColumn, $"duplicate id '{id}' (ids are case-insensitive).");

                int maxHp = row.Int(MaxHpColumn);
                int move = row.Int(MoveColumn);
                int jump = row.Int(JumpColumn);
                bool flies = row.Bool(FliesColumn);
                Color color = row.Color(ColorColumn);

                if (errors.Count > errorsBefore)
                    continue;

                try
                {
                    var definition = new UnitClassDefinition(id, maxHp, move, jump, flies);
                    classes.Add(new UnitClassRow(definition, row.Text(NameColumn), color));
                }
                catch (ArgumentException e)
                {
                    row.Error(e.Message);
                }
            }

            if (errors.Count == 0 && classes.Count == 0)
                errors.Add($"{sourceName}: no unit class rows.");

            return new CsvParseResult<IReadOnlyList<UnitClassRow>>(errors.Count == 0 ? classes : null, errors);
        }
    }
}
