using System;
using NUnit.Framework;
using Tactics.Core;

namespace Tactics.Tests.EditMode
{
    public class UnitClassDefinitionTests
    {
        [Test]
        public void Constructor_StoresValues()
        {
            var unitClass = new UnitClassDefinition("Cavalier", 22, 7, 1, false);

            Assert.AreEqual("Cavalier", unitClass.Id);
            Assert.AreEqual(22, unitClass.MaxHp);
            Assert.AreEqual(7, unitClass.Move);
            Assert.AreEqual(1, unitClass.Jump);
            Assert.IsFalse(unitClass.Flies);
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("  ")]
        public void Constructor_EmptyId_Throws(string id)
        {
            Assert.Throws<ArgumentException>(() => new UnitClassDefinition(id, 20, 5, 1, false));
        }

        [TestCase(0, 5, 1)]
        [TestCase(20, -1, 1)]
        [TestCase(20, 5, -1)]
        public void Constructor_OutOfRangeStat_Throws(int maxHp, int move, int jump)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new UnitClassDefinition("X", maxHp, move, jump, false));
        }

        [Test]
        public void Constructor_ZeroMoveAndJump_Allowed()
        {
            Assert.DoesNotThrow(() => new UnitClassDefinition("Ballista", 30, 0, 0, false));
        }

        [Test]
        public void CanEnter_Ground_UsesPassable()
        {
            Assert.IsTrue(TestGrids.Soldier.CanEnter(TestGrids.Plain));
            Assert.IsFalse(TestGrids.Soldier.CanEnter(TestGrids.Lake));
            Assert.IsFalse(TestGrids.Soldier.CanEnter(TestGrids.Wall));
        }

        [Test]
        public void CanEnter_Flier_UsesFlyable()
        {
            Assert.IsTrue(TestGrids.Flier.CanEnter(TestGrids.Plain));
            Assert.IsTrue(TestGrids.Flier.CanEnter(TestGrids.Lake));
            Assert.IsFalse(TestGrids.Flier.CanEnter(TestGrids.Wall));
        }

        [Test]
        public void CanEnter_PassableButNotFlyable_BlocksFliersOnly()
        {
            var ceiling = new TerrainDefinition("C", 1, true, false, 0, 0);

            Assert.IsTrue(TestGrids.Soldier.CanEnter(ceiling));
            Assert.IsFalse(TestGrids.Flier.CanEnter(ceiling));
        }
    }
}
