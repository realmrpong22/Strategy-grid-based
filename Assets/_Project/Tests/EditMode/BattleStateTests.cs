using System;
using NUnit.Framework;
using Tactics.Core;
using UnityEngine;

namespace Tactics.Tests.EditMode
{
    public class BattleStateTests
    {
        // x: 0 P, 1 P, 2 W (wall), 3 L (lake, fliers only)
        private BattleState _battle;
        private Unit _soldier;
        private Unit _flier;

        [SetUp]
        public void SetUp()
        {
            _battle = new BattleState(TestGrids.Row("P", "P", "W", "L"));
            _soldier = new Unit(TestGrids.Soldier, Faction.Player);
            _flier = new Unit(TestGrids.Flier, Faction.Enemy);
        }

        private static Vector2Int At(int x) => new Vector2Int(x, 0);

        [Test]
        public void TryAddUnit_SetsPositionAndOccupancy()
        {
            Assert.IsTrue(_battle.TryAddUnit(_soldier, At(1), out string error), error);

            Assert.AreEqual(At(1), _soldier.Position);
            Assert.IsTrue(_battle.IsOccupied(At(1)));
            Assert.IsTrue(_battle.TryGetUnitAt(At(1), out Unit found));
            Assert.AreSame(_soldier, found);
            CollectionAssert.AreEqual(new[] { _soldier }, _battle.Units);
        }

        [Test]
        public void TryGetUnitAt_Empty_ReturnsFalse()
        {
            Assert.IsFalse(_battle.TryGetUnitAt(At(0), out Unit unit));
            Assert.IsNull(unit);
        }

        [Test]
        public void TryAddUnit_OutOfBounds_Fails()
        {
            Assert.IsFalse(_battle.TryAddUnit(_soldier, At(9), out string error));
            StringAssert.Contains("outside", error);
            CollectionAssert.IsEmpty(_battle.Units);
        }

        [Test]
        public void TryAddUnit_Occupied_Fails()
        {
            _battle.AddUnit(_soldier, At(0));

            Assert.IsFalse(_battle.TryAddUnit(_flier, At(0), out string error));
            StringAssert.Contains("occupied", error);
        }

        [Test]
        public void TryAddUnit_SameUnitTwice_Fails()
        {
            _battle.AddUnit(_soldier, At(0));

            Assert.IsFalse(_battle.TryAddUnit(_soldier, At(1), out string error));
            StringAssert.Contains("already in the battle", error);
            Assert.AreEqual(At(0), _soldier.Position);
        }

        [Test]
        public void TryAddUnit_TerrainClassCannotEnter_Fails()
        {
            Assert.IsFalse(_battle.TryAddUnit(_soldier, At(3), out string lakeError));
            StringAssert.Contains("can't stand", lakeError);
            Assert.IsFalse(_battle.TryAddUnit(_flier, At(2), out _));
        }

        [Test]
        public void TryAddUnit_FlierOnFlyableOnlyTerrain_Succeeds()
        {
            Assert.IsTrue(_battle.TryAddUnit(_flier, At(3), out string error), error);
        }

        [Test]
        public void AddUnit_Invalid_Throws()
        {
            Assert.Throws<InvalidOperationException>(() => _battle.AddUnit(_soldier, At(2)));
        }

        [Test]
        public void MoveUnit_UpdatesPositionAndOccupancy()
        {
            _battle.AddUnit(_soldier, At(0));

            _battle.MoveUnit(_soldier, At(1));

            Assert.AreEqual(At(1), _soldier.Position);
            Assert.IsFalse(_battle.IsOccupied(At(0)));
            Assert.IsTrue(_battle.TryGetUnitAt(At(1), out Unit found));
            Assert.AreSame(_soldier, found);
        }

        [Test]
        public void MoveUnit_ToOwnTile_IsNoOp()
        {
            _battle.AddUnit(_soldier, At(0));

            Assert.DoesNotThrow(() => _battle.MoveUnit(_soldier, At(0)));
            Assert.IsTrue(_battle.IsOccupied(At(0)));
        }

        [Test]
        public void MoveUnit_OntoOccupiedTile_ThrowsAndLeavesStateUnchanged()
        {
            _battle.AddUnit(_soldier, At(0));
            _battle.AddUnit(_flier, At(1));

            Assert.Throws<InvalidOperationException>(() => _battle.MoveUnit(_soldier, At(1)));
            Assert.AreEqual(At(0), _soldier.Position);
            Assert.IsTrue(_battle.TryGetUnitAt(At(1), out Unit found));
            Assert.AreSame(_flier, found);
        }

        [Test]
        public void MoveUnit_OntoTerrainClassCannotEnter_Throws()
        {
            _battle.AddUnit(_soldier, At(1));

            Assert.Throws<InvalidOperationException>(() => _battle.MoveUnit(_soldier, At(2)));
        }

        [Test]
        public void MoveUnit_UnitNotInBattle_Throws()
        {
            Assert.Throws<InvalidOperationException>(() => _battle.MoveUnit(_soldier, At(1)));
        }
    }
}
