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
        public bool IsPassable { get; }
        public int DefenseBonus { get; }
        public int AvoidBonus { get; }

        public TerrainDefinition(string id, int moveCost, bool isPassable, int defenseBonus, int avoidBonus)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("Terrain id must not be empty.", nameof(id));
            if (isPassable && moveCost < 1)
                throw new ArgumentOutOfRangeException(nameof(moveCost), moveCost,
                    $"Passable terrain '{id}' must have a move cost of at least 1.");

            Id = id;
            MoveCost = moveCost;
            IsPassable = isPassable;
            DefenseBonus = defenseBonus;
            AvoidBonus = avoidBonus;
        }

        public override string ToString() => Id;
    }
}
