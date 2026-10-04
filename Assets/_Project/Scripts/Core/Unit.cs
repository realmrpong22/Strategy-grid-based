using System;
using UnityEngine;

namespace Tactics.Core
{
    /// <summary>
    /// A unit in a battle. Position is owned by BattleState (set when added or moved), so the
    /// occupancy lookup can never disagree with the unit.
    /// </summary>
    public sealed class Unit
    {
        public UnitClassDefinition Class { get; }
        public Faction Faction { get; }
        public Vector2Int Position { get; internal set; }
        public int CurrentHp { get; private set; }

        public Unit(UnitClassDefinition unitClass, Faction faction)
        {
            Class = unitClass ?? throw new ArgumentNullException(nameof(unitClass));
            Faction = faction;
            CurrentHp = unitClass.MaxHp;
        }

        /// <summary>
        /// The one place that decides who is an enemy. Different factions are hostile for now;
        /// a friendly third faction (e.g. allied NPCs) would only change this method.
        /// </summary>
        public bool IsHostileTo(Unit other)
        {
            if (other == null) throw new ArgumentNullException(nameof(other));
            return Faction != other.Faction;
        }

        public override string ToString() => $"{Faction} {Class.Id} ({Position.x}, {Position.y})";
    }
}
