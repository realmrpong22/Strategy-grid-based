using System;
using System.Collections.Generic;
using UnityEngine;

namespace Tactics.Core
{
    /// <summary>
    /// Dijkstra flood fill from a unit's tile, bounded by its Move stat. Step legality and cost come
    /// from MovementRules. One search gives both the range highlight and every path in it.
    /// </summary>
    public static class Pathfinder
    {
        public static MoveRange ComputeMoveRange(BattleState battle, Unit unit)
        {
            if (battle == null) throw new ArgumentNullException(nameof(battle));
            if (unit == null) throw new ArgumentNullException(nameof(unit));
            if (!battle.TryGetUnitAt(unit.Position, out Unit atPosition) || atPosition != unit)
                throw new InvalidOperationException($"{unit} is not in this battle.");

            GridMap map = battle.Map;
            Vector2Int origin = unit.Position;
            int budget = unit.Class.Move;

            var costs = new Dictionary<Vector2Int, int> { [origin] = 0 };
            var parents = new Dictionary<Vector2Int, Vector2Int>();
            var frontier = new MinHeap<Vector2Int>();
            var neighbors = new List<Tile>(4);
            frontier.Push(origin, 0);

            while (frontier.Count > 0)
            {
                Vector2Int position = frontier.Pop(out int cost);
                // Stale entry: a cheaper route to this tile was already expanded.
                if (cost > costs[position])
                    continue;

                Tile tile = map.GetTile(position);
                map.GetNeighbors(position, neighbors);
                foreach (Tile next in neighbors)
                {
                    if (!MovementRules.CanStep(battle, unit, tile, next))
                        continue;

                    int nextCost = cost + MovementRules.StepCost(unit, next);
                    if (nextCost > budget)
                        continue;
                    if (costs.TryGetValue(next.Position, out int known) && known <= nextCost)
                        continue;

                    costs[next.Position] = nextCost;
                    parents[next.Position] = position;
                    frontier.Push(next.Position, nextCost);
                }
            }

            var destinations = new List<Vector2Int>(costs.Count);
            foreach (Vector2Int position in costs.Keys)
            {
                if (MovementRules.CanStop(battle, unit, map.GetTile(position)))
                    destinations.Add(position);
            }

            return new MoveRange(unit, origin, costs, parents, destinations);
        }
    }
}
