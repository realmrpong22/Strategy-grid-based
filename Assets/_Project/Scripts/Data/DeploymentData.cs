using System;
using System.Collections.Generic;
using Tactics.Core;
using UnityEngine;

namespace Tactics.Data
{
    /// <summary>One starting unit in a deployment.</summary>
    [Serializable]
    public struct UnitPlacement
    {
        [SerializeField] private UnitClassData _unitClass;
        [SerializeField] private Faction _faction;
        [SerializeField] private Vector2Int _position;

        public UnitClassData UnitClass => _unitClass;
        public Faction Faction => _faction;
        public Vector2Int Position => _position;

        public UnitPlacement(UnitClassData unitClass, Faction faction, Vector2Int position)
        {
            _unitClass = unitClass;
            _faction = faction;
            _position = position;
        }
    }

    public sealed class BattleBuildResult
    {
        /// <summary>Null when building failed.</summary>
        public BattleState Battle { get; }
        public IReadOnlyList<string> Errors { get; }
        public bool Success => Errors.Count == 0;

        public BattleBuildResult(BattleState battle, IReadOnlyList<string> errors)
        {
            Battle = battle;
            Errors = errors;
        }
    }

    /// <summary>
    /// A map plus the units that start on it, generated from Deployments/&lt;map&gt;.csv by the importer.
    /// Kept separate from MapData so the same map can host different setups (and, later, a sortie
    /// where the player picks units for the deployment slots).
    /// </summary>
    public sealed class DeploymentData : ScriptableObject
    {
        [SerializeField] private MapData _map;
        [SerializeField] private UnitPlacement[] _units = Array.Empty<UnitPlacement>();

        public MapData Map => _map;
        public IReadOnlyList<UnitPlacement> Units => _units;

        /// <summary>Builds a fresh grid and places every unit. Check Success / Errors on the result.</summary>
        public BattleBuildResult BuildBattle()
        {
            var errors = new List<string>();
            if (_map == null)
            {
                errors.Add($"{name}: no map assigned.");
                return new BattleBuildResult(null, errors);
            }

            GridBuildResult grid = _map.BuildGrid();
            if (!grid.Success)
            {
                errors.AddRange(grid.Errors);
                return new BattleBuildResult(null, errors);
            }

            var battle = new BattleState(grid.Map);
            // One definition per class asset, so units of the same class share it.
            var definitions = new Dictionary<UnitClassData, UnitClassDefinition>();

            for (int i = 0; i < _units.Length; i++)
            {
                UnitPlacement placement = _units[i];
                if (placement.UnitClass == null)
                {
                    errors.Add($"{name} unit {i + 1}: missing unit class reference.");
                    continue;
                }

                if (!definitions.TryGetValue(placement.UnitClass, out UnitClassDefinition definition))
                {
                    definition = placement.UnitClass.ToDefinition();
                    definitions.Add(placement.UnitClass, definition);
                }

                if (!battle.TryAddUnit(new Unit(definition, placement.Faction), placement.Position, out string error))
                    errors.Add($"{name} unit {i + 1}: {error}");
            }

            return new BattleBuildResult(errors.Count == 0 ? battle : null, errors);
        }

        /// <summary>Lets Views find presentation data (e.g. greybox colour) for a Core class id.</summary>
        public bool TryGetUnitClass(string id, out UnitClassData unitClass)
        {
            foreach (UnitPlacement placement in _units)
            {
                if (placement.UnitClass != null && placement.UnitClass.Id == id)
                {
                    unitClass = placement.UnitClass;
                    return true;
                }
            }

            unitClass = null;
            return false;
        }

        internal void SetValues(MapData map, UnitPlacement[] units)
        {
            _map = map;
            _units = units;
        }
    }
}
