using System;

namespace Tactics.Core
{
    /// <summary>
    /// Immutable gameplay definition of a unit class. Built from UnitClassData (Data layer).
    /// Combat stats arrive with the combat milestone.
    /// </summary>
    public sealed class UnitClassDefinition
    {
        public string Id { get; }
        public int MaxHp { get; }
        /// <summary>Movement budget per move, spent on terrain move cost.</summary>
        public int Move { get; }
        /// <summary>Largest height difference (in steps, up or down) between neighbouring tiles. Ignored by fliers.</summary>
        public int Jump { get; }
        /// <summary>Fliers enter terrain by IsFlyable instead of IsPassable and ignore Jump.</summary>
        public bool Flies { get; }

        public UnitClassDefinition(string id, int maxHp, int move, int jump, bool flies)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("Unit class id must not be empty.", nameof(id));
            if (maxHp < 1)
                throw new ArgumentOutOfRangeException(nameof(maxHp), maxHp, $"Class '{id}' must have at least 1 max HP.");
            if (move < 0)
                throw new ArgumentOutOfRangeException(nameof(move), move, $"Class '{id}' must not have negative move.");
            if (jump < 0)
                throw new ArgumentOutOfRangeException(nameof(jump), jump, $"Class '{id}' must not have negative jump.");

            Id = id;
            MaxHp = maxHp;
            Move = move;
            Jump = jump;
            Flies = flies;
        }

        /// <summary>Whether this class may stand on or pass through the terrain at all (ignores height).</summary>
        public bool CanEnter(TerrainDefinition terrain)
        {
            if (terrain == null) throw new ArgumentNullException(nameof(terrain));
            return Flies ? terrain.IsFlyable : terrain.IsPassable;
        }

        public override string ToString() => Id;
    }
}
