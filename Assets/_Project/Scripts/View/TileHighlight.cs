namespace Tactics.View
{
    /// <summary>Visual highlight state of a tile. Attack range arrives with combat.</summary>
    public enum TileHighlight
    {
        None,
        Hover,
        Selected,
        /// <summary>Where the selected player unit can move.</summary>
        MoveRange,
        /// <summary>Where an inspected enemy unit can move (read-only).</summary>
        EnemyRange,
    }
}
