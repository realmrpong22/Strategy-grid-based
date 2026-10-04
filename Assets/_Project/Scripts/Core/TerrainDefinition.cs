using System;

namespace Tactics.Core
{
    /// <summary>
    /// Immutable gameplay definition of a terrain type. Built from TerrainData (Data layer);
    /// Core never sees the ScriptableObject.
    /// </summary>
    public sealed class TerrainDefinition
    {
        public string Id { get; }
        public int MoveCost { get; }
        /// <summary>Ground units can enter.</summary>
        public bool IsPassable { get; }
        /// <summary>Flying units can enter. Independent of IsPassable: fliers check only this.</summary>
        public bool IsFlyable { get; }
        public int DefenseBonus { get; }
        public int AvoidBonus { get; }

        public TerrainDefinition(
            string id, int moveCost, bool isPassable, bool isFlyable, int defenseBonus, int avoidBonus)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("Terrain id must not be empty.", nameof(id));
            if ((isPassable || isFlyable) && moveCost < 1)
                throw new ArgumentOutOfRangeException(nameof(moveCost), moveCost,
                    $"Terrain '{id}' can be entered, so it must have a move cost of at least 1.");

            Id = id;
            MoveCost = moveCost;
            IsPassable = isPassable;
            IsFlyable = isFlyable;
            DefenseBonus = defenseBonus;
            AvoidBonus = avoidBonus;
        }

        public override string ToString() => Id;
    }
}
