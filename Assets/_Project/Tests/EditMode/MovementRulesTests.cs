using NUnit.Framework;
using Tactics.Core;
using UnityEngine;

namespace Tactics.Tests.EditMode
{
    public class MovementRulesTests
    {
        private static Tile T(BattleState battle, int x, int z = 0) => battle.Map.GetTile(new Vector2Int(x, z));

        [Test]
        public void CanStep_HeightWithinJump_UpAndDown()
        {
            var battle = new BattleState(TestGrids.FromRows("P0 P1 P0"));
            var soldier = new Unit(TestGrids.Soldier, Faction.Player); // Jump 1
            battle.AddUnit(soldier, new Vector2Int(0, 0));

            Assert.IsTrue(MovementRules.CanStep(battle, soldier, T(battle, 0), T(battle, 1)));
            Assert.IsTrue(MovementRules.CanStep(battle, soldier, T(battle, 1), T(battle, 2)));
        }

        [Test]
        public void CanStep_HeightAboveJump_BlockedBothWays()
        {
            var battle = new BattleState(TestGrids.FromRows("P0 P2"));
            var soldier = new Unit(TestGrids.Soldier, Faction.Player);
            battle.AddUnit(soldier, new Vector2Int(0, 0));

            Assert.IsFalse(MovementRules.CanStep(battle, soldier, T(battle, 0), T(battle, 1)));
            Assert.IsFalse(MovementRules.CanStep(battle, soldier, T(battle, 1), T(battle, 0)));
        }

        [Test]
        public void CanStep_Flier_IgnoresHeight()
        {
            var battle = new BattleState(TestGrids.FromRows("P0 P9"));
            var flier = new Unit(TestGrids.Flier, Faction.Player); // Jump 0
            battle.AddUnit(flier, new Vector2Int(0, 0));

            Assert.IsTrue(MovementRules.CanStep(battle, flier, T(battle, 0), T(battle, 1)));
        }

        [Test]
        public void CanStep_AllyAllowed_EnemyBlocked()
        {
            var battle = new BattleState(TestGrids.FromRows("P0 P0 P0"));
            var mover = new Unit(TestGrids.Soldier, Faction.Player);
            battle.AddUnit(mover, new Vector2Int(1, 0));
            battle.AddUnit(new Unit(TestGrids.Soldier, Faction.Player), new Vector2Int(0, 0));
            battle.AddUnit(new Unit(TestGrids.Soldier, Faction.Enemy), new Vector2Int(2, 0));

            Assert.IsTrue(MovementRules.CanStep(battle, mover, T(battle, 1), T(battle, 0)));
            Assert.IsFalse(MovementRules.CanStep(battle, mover, T(battle, 1), T(battle, 2)));
        }

        [Test]
        public void CanStop_OnlyOnEmptyOrOwnTile()
        {
            var battle = new BattleState(TestGrids.FromRows("P0 P0 P0"));
            var mover = new Unit(TestGrids.Soldier, Faction.Player);
            battle.AddUnit(mover, new Vector2Int(1, 0));
            battle.AddUnit(new Unit(TestGrids.Soldier, Faction.Player), new Vector2Int(0, 0));

            Assert.IsTrue(MovementRules.CanStop(battle, mover, T(battle, 1)));
            Assert.IsTrue(MovementRules.CanStop(battle, mover, T(battle, 2)));
            Assert.IsFalse(MovementRules.CanStop(battle, mover, T(battle, 0)));
        }

        [Test]
        public void StepCost_IsTerrainCost_ForFliersToo()
        {
            var battle = new BattleState(TestGrids.FromRows("F0"));
            var flier = new Unit(TestGrids.Flier, Faction.Player);

            Assert.AreEqual(TestGrids.Forest.MoveCost, MovementRules.StepCost(flier, T(battle, 0)));
        }
    }
}
