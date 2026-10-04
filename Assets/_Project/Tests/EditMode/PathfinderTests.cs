using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Tactics.Core;
using UnityEngine;

namespace Tactics.Tests.EditMode
{
    public class PathfinderTests
    {
        private static readonly Vector2Int Origin = new Vector2Int(0, 0);

        private static UnitClassDefinition Mover(int move, int jump = 1, bool flies = false) =>
            new UnitClassDefinition("Mover", 10, move, jump, flies);

        private static (BattleState Battle, Unit Unit) Setup(GridMap map, UnitClassDefinition unitClass, Vector2Int at)
        {
            var battle = new BattleState(map);
            var unit = new Unit(unitClass, Faction.Player);
            battle.AddUnit(unit, at);
            return (battle, unit);
        }

        private static int Cost(MoveRange range, int x, int z)
        {
            Assert.IsTrue(range.TryGetCost(new Vector2Int(x, z), out int cost), $"({x}, {z}) not reachable");
            return cost;
        }

        [Test]
        public void Flat_RangeIsDiamond()
        {
            var (battle, unit) = Setup(TestGrids.Flat(7, 7), Mover(2), new Vector2Int(3, 3));

            MoveRange range = Pathfinder.ComputeMoveRange(battle, unit);

            Assert.AreEqual(13, range.Destinations.Count); // 1 + 4 + 8
            Assert.IsTrue(range.Destinations.All(p => Mathf.Abs(p.x - 3) + Mathf.Abs(p.y - 3) <= 2));
            Assert.AreEqual(0, Cost(range, 3, 3));
            Assert.AreEqual(2, Cost(range, 4, 4));
        }

        [Test]
        public void ZeroMove_OnlyOrigin()
        {
            var (battle, unit) = Setup(TestGrids.Flat(3, 3), Mover(0), new Vector2Int(1, 1));

            MoveRange range = Pathfinder.ComputeMoveRange(battle, unit);

            CollectionAssert.AreEqual(new[] { new Vector2Int(1, 1) }, range.Destinations);
        }

        [Test]
        public void TerrainCost_SpendsBudget()
        {
            // Forest costs 2: Move 3 reaches x=1 (cost 2) and x=2 (cost 3), not x=3.
            var (battle, unit) = Setup(TestGrids.FromRows("P0 F0 P0 P0"), Mover(3), Origin);

            MoveRange range = Pathfinder.ComputeMoveRange(battle, unit);

            Assert.AreEqual(2, Cost(range, 1, 0));
            Assert.AreEqual(3, Cost(range, 2, 0));
            Assert.IsFalse(range.IsReachable(new Vector2Int(3, 0)));
        }

        [Test]
        public void PrefersCheaperRouteAroundForest()
        {
            // To (2,0): through the forest costs 3, around it 4. To (1,1): around costs 2, through the forest 3.
            var map = TestGrids.FromRows(
                "P0 P0 P0",
                "P0 F0 P0");
            var (battle, unit) = Setup(map, Mover(5), Origin);

            MoveRange range = Pathfinder.ComputeMoveRange(battle, unit);

            Assert.AreEqual(3, Cost(range, 2, 0)); // P->F->P = 2 + 1
            Assert.AreEqual(2, Cost(range, 1, 1));
        }

        [Test]
        public void Wall_BlocksAndForcesDetour()
        {
            var map = TestGrids.FromRows(
                "P0 P0 P0",
                "P0 W0 P0");
            var (battle, unit) = Setup(map, Mover(4), Origin);

            MoveRange range = Pathfinder.ComputeMoveRange(battle, unit);

            Assert.IsFalse(range.IsReachable(new Vector2Int(1, 0)));
            Assert.AreEqual(4, Cost(range, 2, 0));
        }

        [Test]
        public void Jump_LimitsClimbAndDrop()
        {
            // Jump 1: can climb 0 -> 1 but not 1 -> 3, so the tile beyond the cliff is cut off too.
            // (Drops use the same limit; MovementRulesTests covers the downward case.)
            var (battle, unit) = Setup(TestGrids.FromRows("P0 P1 P3 P1"), Mover(9, jump: 1), Origin);

            MoveRange range = Pathfinder.ComputeMoveRange(battle, unit);

            Assert.IsTrue(range.CanMoveTo(new Vector2Int(1, 0)));
            Assert.IsFalse(range.IsReachable(new Vector2Int(2, 0)));
            Assert.IsFalse(range.IsReachable(new Vector2Int(3, 0)));
        }

        [Test]
        public void JumpZero_CannotChangeHeight()
        {
            var (battle, unit) = Setup(TestGrids.FromRows("P0 P0 P1"), Mover(5, jump: 0), Origin);

            MoveRange range = Pathfinder.ComputeMoveRange(battle, unit);

            Assert.IsTrue(range.CanMoveTo(new Vector2Int(1, 0)));
            Assert.IsFalse(range.IsReachable(new Vector2Int(2, 0)));
        }

        [Test]
        public void Flier_IgnoresHeightAndCrossesLake()
        {
            var (battle, unit) = Setup(TestGrids.FromRows("P0 P5 L0 P0"), Mover(3, jump: 0, flies: true), Origin);

            MoveRange range = Pathfinder.ComputeMoveRange(battle, unit);

            Assert.IsTrue(range.CanMoveTo(new Vector2Int(1, 0)));
            Assert.IsTrue(range.CanMoveTo(new Vector2Int(2, 0))); // may stop on the lake
            Assert.IsTrue(range.CanMoveTo(new Vector2Int(3, 0)));
        }

        [Test]
        public void Ground_CannotEnterLake()
        {
            var (battle, unit) = Setup(TestGrids.FromRows("P0 L0 P0"), Mover(5), Origin);

            MoveRange range = Pathfinder.ComputeMoveRange(battle, unit);

            Assert.IsFalse(range.IsReachable(new Vector2Int(1, 0)));
            Assert.IsFalse(range.IsReachable(new Vector2Int(2, 0)));
        }

        [Test]
        public void Flier_StillCannotEnterUnflyableTerrain()
        {
            var (battle, unit) = Setup(TestGrids.FromRows("P0 W0 P0"), Mover(5, flies: true), Origin);

            MoveRange range = Pathfinder.ComputeMoveRange(battle, unit);

            Assert.IsFalse(range.IsReachable(new Vector2Int(1, 0)));
        }

        [Test]
        public void Ally_PassThroughButNotDestination()
        {
            var (battle, unit) = Setup(TestGrids.FromRows("P0 P0 P0"), Mover(2), Origin);
            battle.AddUnit(new Unit(TestGrids.Soldier, Faction.Player), new Vector2Int(1, 0));

            MoveRange range = Pathfinder.ComputeMoveRange(battle, unit);

            Assert.IsTrue(range.IsReachable(new Vector2Int(1, 0)));
            Assert.IsFalse(range.CanMoveTo(new Vector2Int(1, 0)));
            Assert.IsTrue(range.CanMoveTo(new Vector2Int(2, 0)));
        }

        [Test]
        public void Enemy_BlocksPassage()
        {
            var (battle, unit) = Setup(TestGrids.FromRows("P0 P0 P0"), Mover(5), Origin);
            battle.AddUnit(new Unit(TestGrids.Soldier, Faction.Enemy), new Vector2Int(1, 0));

            MoveRange range = Pathfinder.ComputeMoveRange(battle, unit);

            Assert.IsFalse(range.IsReachable(new Vector2Int(1, 0)));
            Assert.IsFalse(range.IsReachable(new Vector2Int(2, 0)));
        }

        [Test]
        public void EnemyUnit_TreatsPlayersAsBlockers()
        {
            var battle = new BattleState(TestGrids.FromRows("P0 P0 P0"));
            var enemy = new Unit(Mover(5), Faction.Enemy);
            battle.AddUnit(enemy, Origin);
            battle.AddUnit(new Unit(TestGrids.Soldier, Faction.Player), new Vector2Int(1, 0));

            MoveRange range = Pathfinder.ComputeMoveRange(battle, enemy);

            Assert.IsFalse(range.IsReachable(new Vector2Int(2, 0)));
        }

        [Test]
        public void Origin_IsDestinationAtCostZero()
        {
            var (battle, unit) = Setup(TestGrids.Flat(2, 2), Mover(1), Origin);

            MoveRange range = Pathfinder.ComputeMoveRange(battle, unit);

            Assert.IsTrue(range.CanMoveTo(Origin));
            Assert.AreEqual(0, Cost(range, 0, 0));
            Assert.AreEqual(Origin, range.Origin);
            Assert.AreSame(unit, range.Unit);
        }

        [Test]
        public void TryGetPath_IsContiguousCheapestAndEndsCorrectly()
        {
            var map = TestGrids.FromRows(
                "P0 P0 P0 P0",
                "P0 W0 F0 P0",
                "P0 P0 P0 P0");
            var (battle, unit) = Setup(map, Mover(10), Origin);
            MoveRange range = Pathfinder.ComputeMoveRange(battle, unit);
            var path = new List<Vector2Int>();
            var destination = new Vector2Int(3, 2);

            Assert.IsTrue(range.TryGetPath(destination, path));

            Assert.AreEqual(Origin, path[0]);
            Assert.AreEqual(destination, path[path.Count - 1]);
            int total = 0;
            for (int i = 1; i < path.Count; i++)
            {
                Assert.AreEqual(1, Mathf.Abs(path[i].x - path[i - 1].x) + Mathf.Abs(path[i].y - path[i - 1].y),
                    $"Step {i} is not adjacent.");
                total += map.GetTile(path[i]).Terrain.MoveCost;
            }
            Assert.AreEqual(Cost(range, destination.x, destination.y), total);
            Assert.AreEqual(5, total);
        }

        [Test]
        public void TryGetPath_ToOrigin_IsSingleTile()
        {
            var (battle, unit) = Setup(TestGrids.Flat(2, 2), Mover(1), Origin);
            var path = new List<Vector2Int> { new Vector2Int(9, 9) };

            Assert.IsTrue(Pathfinder.ComputeMoveRange(battle, unit).TryGetPath(Origin, path));
            CollectionAssert.AreEqual(new[] { Origin }, path);
        }

        [Test]
        public void TryGetPath_NotADestination_ReturnsFalseAndEmpty()
        {
            var (battle, unit) = Setup(TestGrids.FromRows("P0 P0 P0"), Mover(2), Origin);
            battle.AddUnit(new Unit(TestGrids.Soldier, Faction.Player), new Vector2Int(1, 0));
            MoveRange range = Pathfinder.ComputeMoveRange(battle, unit);
            var path = new List<Vector2Int>();

            Assert.IsFalse(range.TryGetPath(new Vector2Int(1, 0), path)); // ally tile: pass-through only
            CollectionAssert.IsEmpty(path);
        }

        [Test]
        public void UnitNotInBattle_Throws()
        {
            var battle = new BattleState(TestGrids.Flat(2, 2));
            var stranger = new Unit(TestGrids.Soldier, Faction.Player);

            Assert.Throws<InvalidOperationException>(() => Pathfinder.ComputeMoveRange(battle, stranger));
        }
    }
}
