using System;
using System.Collections.Generic;
using UnityEngine;

namespace Tactics.Core
{
    /// <summary>
    /// The battle's mutable state: the grid plus the units on it, with a position → unit lookup.
    /// It enforces placement invariants (in bounds, one unit per tile, terrain the class can enter)
    /// but not movement rules: whether a destination is reachable is MovementRules' job.
    /// </summary>
    public sealed class BattleState
    {
        private readonly List<Unit> _units = new List<Unit>();
        private readonly Dictionary<Vector2Int, Unit> _occupancy = new Dictionary<Vector2Int, Unit>();

        public GridMap Map { get; }
        public IReadOnlyList<Unit> Units => _units;

        public BattleState(GridMap map)
        {
            Map = map ?? throw new ArgumentNullException(nameof(map));
        }

        public bool TryGetUnitAt(Vector2Int position, out Unit unit) => _occupancy.TryGetValue(position, out unit);

        public bool IsOccupied(Vector2Int position) => _occupancy.ContainsKey(position);

        /// <summary>
        /// Places a unit. Returns false with a readable reason instead of throwing, so the importer can
        /// run the same check the game runs and report it against the spreadsheet row.
        /// </summary>
        public bool TryAddUnit(Unit unit, Vector2Int position, out string error)
        {
            if (unit == null) throw new ArgumentNullException(nameof(unit));

            if (_units.Contains(unit))
                error = $"{unit.Class.Id} is already in the battle.";
            else
                error = CheckCanStand(unit, position);

            if (error != null)
                return false;

            unit.Position = position;
            _units.Add(unit);
            _occupancy.Add(position, unit);
            return true;
        }

        /// <summary>Throwing version of TryAddUnit, for code that has already validated its data.</summary>
        public void AddUnit(Unit unit, Vector2Int position)
        {
            if (!TryAddUnit(unit, position, out string error))
                throw new InvalidOperationException(error);
        }

        /// <summary>
        /// Teleports a unit to a free tile it can stand on. Does not check reachability or path;
        /// callers get the destination from a MoveRange.
        /// </summary>
        public void MoveUnit(Unit unit, Vector2Int destination)
        {
            if (unit == null) throw new ArgumentNullException(nameof(unit));
            if (!_occupancy.TryGetValue(unit.Position, out Unit atPosition) || atPosition != unit)
                throw new InvalidOperationException($"{unit} is not in this battle.");
            if (destination == unit.Position)
                return;

            string error = CheckCanStand(unit, destination);
            if (error != null)
                throw new InvalidOperationException(error);

            _occupancy.Remove(unit.Position);
            _occupancy.Add(destination, unit);
            unit.Position = destination;
        }

        private string CheckCanStand(Unit unit, Vector2Int position)
        {
            if (!Map.TryGetTile(position, out Tile tile))
                return $"({position.x}, {position.y}) is outside the {Map.Width}x{Map.Depth} map.";
            if (_occupancy.TryGetValue(position, out Unit occupant))
                return $"({position.x}, {position.y}) is already occupied by {occupant.Class.Id}.";
            if (!unit.Class.CanEnter(tile.Terrain))
                return $"{unit.Class.Id} can't stand on {tile.Terrain.Id} at ({position.x}, {position.y}).";
            return null;
        }
    }
}
