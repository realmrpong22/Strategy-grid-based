using System;
using System.Collections.Generic;
using System.Linq;
using Tactics.Core;
using UnityEngine;

namespace Tactics.Editor
{
    public sealed class DeploymentRow
    {
        public Faction Faction { get; }
        public string ClassId { get; }
        public Vector2Int Position { get; }

        public DeploymentRow(Faction faction, string classId, Vector2Int position)
        {
            Faction = faction;
            ClassId = classId;
            Position = position;
        }
    }

    /// <summary>
    /// Parses a deployment CSV (one row per starting unit: faction, class, cell) and validates it
    /// against its map and the unit classes. The cell is the map sheet's own reference (e.g. "E1"),
    /// so a designer reads it straight off the map CSV without converting to grid coordinates.
    /// Placement uses BattleState.TryAddUnit, the same check the game runs, so a deployment that
    /// imports always loads.
    /// </summary>
    public static class DeploymentCsvParser
    {
        public const string FactionColumn = "faction";
        public const string ClassColumn = "class";
        public const string CellColumn = "cell";

        private static readonly string[] RequiredColumns = { FactionColumn, ClassColumn, CellColumn };

        /// <param name="classes">Known unit classes by id (exact case).</param>
        /// <param name="map">The map this deployment belongs to.</param>
        public static CsvParseResult<IReadOnlyList<DeploymentRow>> Parse(
            string text, string sourceName, IReadOnlyDictionary<string, UnitClassDefinition> classes, GridMap map)
        {
            if (classes == null) throw new ArgumentNullException(nameof(classes));
            if (map == null) throw new ArgumentNullException(nameof(map));

            var errors = new List<string>();
            var placements = new List<DeploymentRow>();

            CsvTable table = CsvTable.Parse(text, sourceName, RequiredColumns, errors);
            if (table == null)
                return new CsvParseResult<IReadOnlyList<DeploymentRow>>(null, errors);

            var battle = new BattleState(map);

            foreach (CsvTableRow row in table.Rows)
            {
                int errorsBefore = errors.Count;

                Faction faction = row.Enum<Faction>(FactionColumn);
                string classId = row.Text(ClassColumn);
                if (!classes.ContainsKey(classId))
                    row.Error(ClassColumn, UnknownClassMessage(classId, classes.Keys));
                string cell = row.Text(CellColumn);
                Vector2Int position = default;
                if (!MapCsvParser.TryParseCellName(cell, out int column, out int sheetRow))
                    row.Error(CellColumn, $"'{cell}' is not a map cell like E1.");
                else if (column >= map.Width || sheetRow >= map.Depth)
                    row.Error(CellColumn,
                        $"map cell {cell} is outside the {map.Width}x{map.Depth} map " +
                        $"(A1 to {MapCsvParser.CellName(map.Width - 1, map.Depth - 1)}).");
                else
                    position = new Vector2Int(column, MapCsvParser.SheetRowToZ(sheetRow, map.Depth));

                if (errors.Count > errorsBefore)
                    continue;

                if (!battle.TryAddUnit(new Unit(classes[classId], faction), position, out string placementError))
                {
                    row.Error($"map cell {cell.ToUpperInvariant()}: {placementError}");
                    continue;
                }

                placements.Add(new DeploymentRow(faction, classId, position));
            }

            if (errors.Count == 0 && placements.Count == 0)
                errors.Add($"{sourceName}: no units.");

            return new CsvParseResult<IReadOnlyList<DeploymentRow>>(errors.Count == 0 ? placements : null, errors);
        }

        private static string UnknownClassMessage(string classId, IEnumerable<string> known)
        {
            string caseMatch = known.FirstOrDefault(k => string.Equals(k, classId, StringComparison.OrdinalIgnoreCase));
            return caseMatch != null
                ? $"unknown class '{classId}' (did you mean '{caseMatch}'? Class ids are case-sensitive here)."
                : $"unknown class '{classId}'.";
        }
    }
}
