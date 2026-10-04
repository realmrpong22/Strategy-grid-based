namespace Tactics.Core
{
    /// <summary>Side a unit fights for. Hostility is decided in Unit.IsHostileTo, not by comparing factions directly.</summary>
    public enum Faction
    {
        Player,
        Enemy,
    }
}
