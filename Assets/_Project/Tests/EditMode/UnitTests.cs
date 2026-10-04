using System;
using NUnit.Framework;
using Tactics.Core;

namespace Tactics.Tests.EditMode
{
    public class UnitTests
    {
        [Test]
        public void Constructor_StartsAtFullHp()
        {
            var unit = new Unit(TestGrids.Soldier, Faction.Player);

            Assert.AreSame(TestGrids.Soldier, unit.Class);
            Assert.AreEqual(Faction.Player, unit.Faction);
            Assert.AreEqual(TestGrids.Soldier.MaxHp, unit.CurrentHp);
        }

        [Test]
        public void Constructor_NullClass_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => new Unit(null, Faction.Player));
        }

        [Test]
        public void IsHostileTo_DifferentFactionOnly()
        {
            var player = new Unit(TestGrids.Soldier, Faction.Player);
            var ally = new Unit(TestGrids.Flier, Faction.Player);
            var enemy = new Unit(TestGrids.Soldier, Faction.Enemy);

            Assert.IsFalse(player.IsHostileTo(ally));
            Assert.IsTrue(player.IsHostileTo(enemy));
            Assert.IsTrue(enemy.IsHostileTo(player));
        }
    }
}
