using System;
using System.Collections.Generic;
using UnityEngine;

namespace Tactics.Core
{
    /// <summary>
    /// Result of Pathfinder.ComputeMoveRange: every tile the unit can reach within its Move budget,
    /// with the cheapest cost and a parent link for rebuilding the path. A snapshot: it goes stale
    /// as soon as any unit moves.
    /// </summary>
    public sealed class MoveRange
    {
        private readonly Dictionary<Vector2Int, int> _costs;
        private readonly Dictionary<Vector2Int, Vector2Int> _parents;
        private readonly List<Vector2Int> _destinations;

        public Unit Unit { get; }
        public Vector2Int Origin { get; }

        /// <summary>Tiles the unit can end its move on, origin included. Excludes tiles it can only pass through.</summary>
        public IReadOnlyList<Vector2Int> Destinations => _destinations;

        internal MoveRange(
            Unit unit, Vector2Int origin, Dictionary<Vector2Int, int> costs,
            Dictionary<Vector2Int, Vector2Int> parents, List<Vector2Int> destinations)
        {
            Unit = unit;
            Origin = origin;
            _costs = costs;
            _parents = parents;
            _destinations = destinations;
        }

        /// <summary>Reachable at all, including tiles the unit may only pass through (e.g. under an ally).</summary>
        public bool IsReachable(Vector2Int position) => _costs.ContainsKey(position);

        public bool CanMoveTo(Vector2Int position) => _destinations.Contains(position);

        public bool TryGetCost(Vector2Int position, out int cost) => _costs.TryGetValue(position, out cost);

        /// <summary>
        /// Fills <paramref name="path"/> (cleared first) with the cheapest path from Origin to
        /// <paramref name="destination"/>, both ends included. False if the tile isn't a destination.
        /// </summary>
        public bool TryGetPath(Vector2Int destination, List<Vector2Int> path)
        {
            if (path == null) throw new ArgumentNullException(nameof(path));
            path.Clear();

            if (!CanMoveTo(destination))
                return false;

            Vector2Int current = destination;
            path.Add(current);
            while (current != Origin)
            {
                current = _parents[current];
                path.Add(current);
            }

            path.Reverse();
            return true;
        }
    }
}
