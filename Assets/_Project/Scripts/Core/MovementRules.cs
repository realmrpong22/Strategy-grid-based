using System;

namespace Tactics.Core
{
    /// <summary>
    /// Per-step movement rules, kept separate from the search so the rules can change (e.g. a lenient
    /// drop limit) without touching Pathfinder.
    /// </summary>
    public static class MovementRules
    {
        /// <summary>
        /// Whether <paramref name="unit"/> may move from one tile onto an adjacent one, possibly only
        /// passing through. Blocked by terrain the class can't enter, by a height difference above Jump
        /// (ground units only), and by hostile units. Allies don't block passing through.
        /// </summary>
        public static bool CanStep(BattleState battle, Unit unit, Tile from, Tile to)
        {
            if (battle == null) throw new ArgumentNullException(nameof(battle));
            if (unit == null) throw new ArgumentNullException(nameof(unit));
            if (from == null) throw new ArgumentNullException(nameof(from));
            if (to == null) throw new ArgumentNullException(nameof(to));

            if (!unit.Class.CanEnter(to.Terrain))
                return false;

            // Symmetric for now (same limit up and down); a separate drop limit may come later.
            if (!unit.Class.Flies && Math.Abs(to.Height - from.Height) > unit.Class.Jump)
                return false;

            if (battle.TryGetUnitAt(to.Position, out Unit occupant) && occupant.IsHostileTo(unit))
                return false;

            return true;
        }

        /// <summary>Move points spent entering the tile. One cost for every class, fliers included.</summary>
        public static int StepCost(Unit unit, Tile to)
        {
            if (to == null) throw new ArgumentNullException(nameof(to));
            return to.Terrain.MoveCost;
        }

        /// <summary>Whether the unit may end its move here: no other unit on the tile.</summary>
        public static bool CanStop(BattleState battle, Unit unit, Tile tile)
        {
            if (battle == null) throw new ArgumentNullException(nameof(battle));
            if (tile == null) throw new ArgumentNullException(nameof(tile));
            return !battle.TryGetUnitAt(tile.Position, out Unit occupant) || occupant == unit;
        }
    }
}
